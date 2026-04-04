using F1.FeedReplay.Service.Application.Models;
using F1.FeedReplay.Service.Domain.Models;

namespace F1.FeedReplay.Service.Application.Services;

public sealed class ReplayStatusStore
{
    private readonly object _gate = new();
    private string? _sessionId;
    private ReplayRunState _state = ReplayRunState.Idle;
    private double _replaySpeed = 1.0d;
    private long _publishedEvents;
    private long _totalEvents;
    private DateTimeOffset? _simulationStartReference;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _completedAt;
    private string? _lastError;

    public void SetLoaded(string sessionId, double replaySpeed, long totalEvents, DateTimeOffset simulationStartReference)
    {
        lock (_gate)
        {
            _sessionId = sessionId;
            _state = ReplayRunState.Loaded;
            _replaySpeed = replaySpeed;
            _publishedEvents = 0;
            _totalEvents = totalEvents;
            _simulationStartReference = simulationStartReference;
            _startedAt = null;
            _completedAt = null;
            _lastError = null;
        }
    }

    public void SetStarted(DateTimeOffset startedAt)
    {
        lock (_gate)
        {
            _state = ReplayRunState.Running;
            _publishedEvents = 0;
            _startedAt = startedAt;
            _completedAt = null;
            _lastError = null;
        }
    }

    public void SetPaused()
    {
        lock (_gate)
        {
            _state = ReplayRunState.Paused;
        }
    }

    public void SetResumed()
    {
        lock (_gate)
        {
            _state = ReplayRunState.Running;
        }
    }

    public void SetStopped(DateTimeOffset completedAt)
    {
        lock (_gate)
        {
            _state = ReplayRunState.Stopped;
            _completedAt = completedAt;
        }
    }

    public void SetCompleted(DateTimeOffset completedAt)
    {
        lock (_gate)
        {
            if (_state != ReplayRunState.Stopped)
            {
                _state = ReplayRunState.Completed;
            }

            _completedAt = completedAt;
        }
    }

    public void SetFaulted(DateTimeOffset completedAt, string error)
    {
        lock (_gate)
        {
            _state = ReplayRunState.Faulted;
            _completedAt = completedAt;
            _lastError = error;
        }
    }

    public void SetReplaySpeed(double replaySpeed)
    {
        lock (_gate)
        {
            _replaySpeed = replaySpeed;
        }
    }

    public void IncrementPublishedEvents()
    {
        lock (_gate)
        {
            _publishedEvents++;
        }
    }

    public ReplayRunState GetState()
    {
        lock (_gate)
        {
            return _state;
        }
    }

    public string? GetSessionId()
    {
        lock (_gate)
        {
            return _sessionId;
        }
    }

    public double GetReplaySpeed()
    {
        lock (_gate)
        {
            return _replaySpeed;
        }
    }

    public ReplayStatusModel GetSnapshot(TimeSpan currentVirtualTime)
    {
        lock (_gate)
        {
            return new ReplayStatusModel(
                _sessionId,
                _state,
                _replaySpeed,
                _publishedEvents,
                _totalEvents,
                _simulationStartReference,
                currentVirtualTime,
                _startedAt,
                _completedAt,
                _lastError);
        }
    }
}
