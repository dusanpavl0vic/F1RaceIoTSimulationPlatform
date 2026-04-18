using F1.RaceState.Service.API.Hubs;
using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Services;
using F1.RaceState.Service.Infrastructure.Configuration;
using F1.RaceState.Service.Infrastructure.Grpc;
using F1.RaceState.Service.Infrastructure.Persistence;
using F1.RaceState.Service.Infrastructure.Workers;
using AnalyticsGrpc = F1.TelemetryAnalytics.Service.Grpc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray()
    ?? ["http://localhost:3000", "http://127.0.0.1:3000"];

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardCors", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));
builder.Services.Configure<StatePersistenceOptions>(builder.Configuration.GetSection(StatePersistenceOptions.SectionName));
builder.Services.Configure<TelemetryAnalyticsGrpcOptions>(builder.Configuration.GetSection(TelemetryAnalyticsGrpcOptions.SectionName));

builder.Services
    .AddGrpcClient<AnalyticsGrpc.TelemetryAnalytics.TelemetryAnalyticsClient>((sp, options) =>
    {
        var grpcOptions = sp.GetRequiredService<IOptions<TelemetryAnalyticsGrpcOptions>>().Value;
        options.Address = new Uri(grpcOptions.Endpoint);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        EnableMultipleHttp2Connections = true
    });

builder.Services.AddSingleton<IRaceStateStore, RaceStateStore>();
builder.Services.AddSingleton<RaceStateViewFactory>();
builder.Services.AddSingleton<RaceStateBroadcaster>();
builder.Services.AddSingleton<TelemetryAnalyticsGateway>();
builder.Services.AddSingleton<TelemetryAnalyticsSignalRBridge>();
builder.Services.AddSingleton<StatePersistenceService>();
builder.Services.AddSingleton<RaceStateWorker>();
builder.Services.AddHostedService<RaceStateRecoveryHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RaceStateWorker>());

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
            FileNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = context.Response.StatusCode >= 500 ? "Internal server error." : "Request failed.",
            Detail = exception?.Message
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseCors("DashboardCors");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapHub<RaceStateHub>("/hubs/race-state").RequireCors("DashboardCors");

app.Run();
