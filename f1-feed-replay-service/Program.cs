using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Application.Services;
using F1.FeedReplay.Service.Domain.Services;
using F1.FeedReplay.Service.Infrastructure.Bootstrap;
using F1.FeedReplay.Service.Infrastructure.Configuration;
using F1.FeedReplay.Service.Infrastructure.Feeds.Parsing;
using F1.FeedReplay.Service.Infrastructure.Mqtt;
using F1.FeedReplay.Service.Infrastructure.Time;
using F1.FeedReplay.Service.Infrastructure.Workers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

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

builder.Services.Configure<ReplayServiceOptions>(builder.Configuration.GetSection(ReplayServiceOptions.SectionName));
builder.Services.Configure<ReplayBootstrapOptions>(builder.Configuration.GetSection(ReplayBootstrapOptions.SectionName));
builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));

builder.Services.AddHttpClient("ReplayBootstrap");

builder.Services.AddSingleton<IReplayCoordinator, ReplayCoordinator>();
builder.Services.AddSingleton<IReplayExecutionQueue, ReplayExecutionQueue>();
builder.Services.AddSingleton<IReplayConfigurationLoader, ReplayConfigurationLoader>();
builder.Services.AddSingleton<IReplayBootstrapper, ReplayBootstrapper>();
builder.Services.AddSingleton<IFeedParser, GenericFeedParser>();
builder.Services.AddSingleton<IEventTopicMapper, EventTopicMapper>();
builder.Services.AddSingleton<IReplayTimeProvider, SystemReplayTimeProvider>();
builder.Services.AddSingleton<VirtualClock>();
builder.Services.AddSingleton<FeedFileReader>();
builder.Services.AddSingleton<IRawEventPublisher, MqttPublisher>();

builder.Services.AddHostedService<ReplaySchedulerBackgroundService>();
builder.Services.AddHostedService<ReplayBootstrapHostedService>();

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

app.UseHttpsRedirection();
app.UseCors("DashboardCors");

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
