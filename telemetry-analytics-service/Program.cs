using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Services;
using F1.TelemetryAnalytics.Service.Grpc;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;
using F1.TelemetryAnalytics.Service.Infrastructure.Persistence;
using F1.TelemetryAnalytics.Service.Infrastructure.Workers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2);
});

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddGrpc();
builder.Services.AddHttpClient<IInfluxTelemetryClient, InfluxTelemetryClient>();

builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));
builder.Services.Configure<PostgresOptions>(builder.Configuration.GetSection(PostgresOptions.SectionName));
builder.Services.Configure<InfluxDbOptions>(builder.Configuration.GetSection(InfluxDbOptions.SectionName));
builder.Services.Configure<AnalyticsOptions>(builder.Configuration.GetSection(AnalyticsOptions.SectionName));

builder.Services.AddSingleton<AnalyticsStateStore>();
builder.Services.AddSingleton<TelemetryStreamHub>();
builder.Services.AddSingleton<TelemetryAnalyticsIngestionService>();
builder.Services.AddSingleton<IAnalyticsRepository, PostgresAnalyticsRepository>();
builder.Services.AddSingleton<IAnalyticsQueryService, AnalyticsQueryService>();
builder.Services.AddSingleton<TelemetryAnalyticsWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TelemetryAnalyticsWorker>());

var app = builder.Build();

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

app.MapControllers();
app.MapGrpcService<TelemetryAnalyticsGrpcService>();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/", () => Results.Ok(new { service = "telemetry-analytics-service", grpc = true, rest = true }));

app.Run();
