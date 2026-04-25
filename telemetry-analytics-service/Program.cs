using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Services;
using F1.TelemetryAnalytics.Service.Grpc;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;
using F1.TelemetryAnalytics.Service.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray()
    ?? ["http://localhost:3000", "http://127.0.0.1:3000"];

builder.WebHost.ConfigureKestrel(options =>
{
    // Plain HTTP cannot negotiate HTTP/1.1 vs HTTP/2 via ALPN, so expose
    // REST and gRPC on separate ports to keep h2c gRPC reliable in Docker.
    options.ListenAnyIP(8080, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
    options.ListenAnyIP(8081, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
});

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddGrpc();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardCors", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddHttpClient<IInfluxTelemetryClient, InfluxTelemetryClient>();

builder.Services.Configure<PostgresOptions>(builder.Configuration.GetSection(PostgresOptions.SectionName));
builder.Services.Configure<InfluxDbOptions>(builder.Configuration.GetSection(InfluxDbOptions.SectionName));

builder.Services.AddSingleton<IAnalyticsRepository, PostgresAnalyticsRepository>();
builder.Services.AddSingleton<IAnalyticsQueryService, AnalyticsQueryService>();

var app = builder.Build();
await app.Services.GetRequiredService<IAnalyticsRepository>().InitializeAsync(CancellationToken.None);

app.UseExceptionHandler(exceptionHandler =>
{
    exceptionHandler.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");

        if (exception is not null)
        {
            logger.LogError(exception, "Unhandled request failure.");
        }

        context.Response.StatusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = context.Response.StatusCode >= 500 ? "Internal server error." : "Request failed.",
            Detail = exception?.Message
        });
    });
});

app.UseCors("DashboardCors");
app.MapControllers();
app.MapGrpcService<TelemetryAnalyticsGrpcService>();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/", () => Results.Ok(new { service = "telemetry-analytics-service", grpc = true, rest = true }));

app.Run();
