namespace F1.EventNormalizer.Service.Application.Contracts;

public interface IEventCaptureWriter
{
    Task WriteAsync(string category, string topic, string payload, CancellationToken cancellationToken);
}
