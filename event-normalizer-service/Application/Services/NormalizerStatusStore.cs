using F1.EventNormalizer.Service.Application.Models;

namespace F1.EventNormalizer.Service.Application.Services;

public sealed class NormalizerStatusStore
{
    private readonly object _gate = new();
    private long _consumedRawEvents;
    private long _publishedCanonicalEvents;
    private string? _lastRawTopic;
    private string? _lastCanonicalTopic;
    private string? _lastError;

    public void RecordConsumed(string topic)
    {
        lock (_gate)
        {
            _consumedRawEvents++;
            _lastRawTopic = topic;
        }
    }

    public void RecordPublished(string topic, int count)
    {
        lock (_gate)
        {
            _publishedCanonicalEvents += count;
            _lastCanonicalTopic = topic;
        }
    }

    public void RecordError(string error)
    {
        lock (_gate)
        {
            _lastError = error;
        }
    }

    public NormalizerStatusModel GetSnapshot()
    {
        lock (_gate)
        {
            return new NormalizerStatusModel(
                _consumedRawEvents,
                _publishedCanonicalEvents,
                _lastRawTopic,
                _lastCanonicalTopic,
                _lastError);
        }
    }
}
