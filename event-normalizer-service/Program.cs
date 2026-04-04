using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Application.Services;
using F1.EventNormalizer.Service.Domain.Services;
using F1.EventNormalizer.Service.Infrastructure.Configuration;
using F1.EventNormalizer.Service.Infrastructure.Mqtt;
using F1.EventNormalizer.Service.Infrastructure.Workers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));

builder.Services.AddSingleton<ICanonicalEventFactory, CanonicalEventFactory>();
builder.Services.AddSingleton<ICanonicalTopicMapper, CanonicalTopicMapper>();
builder.Services.AddSingleton<IRawReplayEventSubscriber, RawReplayEventSubscriber>();
builder.Services.AddSingleton<ICanonicalEventPublisher, CanonicalEventPublisher>();
builder.Services.AddSingleton<NormalizerStatusStore>();
builder.Services.AddSingleton<NormalizerWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NormalizerWorker>());

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

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
