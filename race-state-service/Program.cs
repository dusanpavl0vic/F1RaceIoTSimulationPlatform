using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Services;
using F1.RaceState.Service.Infrastructure.Configuration;
using F1.RaceState.Service.Infrastructure.Persistence;
using F1.RaceState.Service.Infrastructure.Workers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));
builder.Services.Configure<StatePersistenceOptions>(builder.Configuration.GetSection(StatePersistenceOptions.SectionName));

builder.Services.AddSingleton<IRaceStateStore, RaceStateStore>();
builder.Services.AddSingleton<RaceStateViewFactory>();
builder.Services.AddSingleton<RaceStateBroadcaster>();
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

app.UseWebSockets();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Map("/ws/race-state", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var broadcaster = context.RequestServices.GetRequiredService<RaceStateBroadcaster>();
    var socket = await context.WebSockets.AcceptWebSocketAsync();
    await broadcaster.AddClientAsync(socket, context.RequestAborted);
});

app.Run();
