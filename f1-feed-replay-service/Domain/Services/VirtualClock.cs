namespace F1.FeedReplay.Service.Domain.Services;

public sealed class VirtualClock(IReplayTimeProvider timeProvider)
{
    private readonly object _gate = new();
    private readonly IReplayTimeProvider _timeProvider = timeProvider;

    private TimeSpan _accumulatedVirtualTime = TimeSpan.Zero;
    private DateTimeOffset? _runningStartedAt;
    private double _speed = 1.0d;
    private bool _isPaused = true;
    private bool _isStopped = true;

    public void Start(TimeSpan initialVirtualTime, double speed)
    {
        if (speed <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be greater than zero.");
        }

        lock (_gate)
        {
            _accumulatedVirtualTime = initialVirtualTime;
            _speed = speed;
            _runningStartedAt = _timeProvider.GetUtcNow();
            _isPaused = false;
            _isStopped = false;
        }
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (_isStopped || _isPaused)
            {
                return;
            }

            _accumulatedVirtualTime = GetCurrentVirtualTimeUnsafe(_timeProvider.GetUtcNow());
            _runningStartedAt = null;
            _isPaused = true;
        }
    }

    public void Resume()
    {
        lock (_gate)
        {
            if (_isStopped || !_isPaused)
            {
                return;
            }

            _runningStartedAt = _timeProvider.GetUtcNow();
            _isPaused = false;
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _accumulatedVirtualTime = TimeSpan.Zero;
            _runningStartedAt = null;
            _isPaused = true;
            _isStopped = true;
        }
    }

    public void Reset(double speed)
    {
        lock (_gate)
        {
            _accumulatedVirtualTime = TimeSpan.Zero;
            _runningStartedAt = null;
            _speed = speed;
            _isPaused = true;
            _isStopped = true;
        }
    }

    public void ChangeSpeed(double speed)
    {
        if (speed <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be greater than zero.");
        }

        lock (_gate)
        {
            if (!_isPaused && !_isStopped)
            {
                _accumulatedVirtualTime = GetCurrentVirtualTimeUnsafe(_timeProvider.GetUtcNow());
                _runningStartedAt = _timeProvider.GetUtcNow();
            }

            _speed = speed;
        }
    }

    public TimeSpan GetCurrentVirtualTime()
    {
        lock (_gate)
        {
            return GetCurrentVirtualTimeUnsafe(_timeProvider.GetUtcNow());
        }
    }

    public async Task WaitUntilAsync(TimeSpan targetVirtualTime, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TimeSpan currentVirtualTime;
            double currentSpeed;
            bool paused;
            bool stopped;

            lock (_gate)
            {
                currentVirtualTime = GetCurrentVirtualTimeUnsafe(_timeProvider.GetUtcNow());
                currentSpeed = _speed;
                paused = _isPaused;
                stopped = _isStopped;
            }

            if (stopped)
            {
                throw new OperationCanceledException("The virtual clock was stopped.");
            }

            if (currentVirtualTime >= targetVirtualTime)
            {
                return;
            }

            if (paused)
            {
                await _timeProvider.DelayAsync(TimeSpan.FromMilliseconds(25), cancellationToken);
                continue;
            }

            var remainingVirtualTime = targetVirtualTime - currentVirtualTime;
            var delayMilliseconds = Math.Max(1, remainingVirtualTime.TotalMilliseconds / currentSpeed);
            var delay = TimeSpan.FromMilliseconds(Math.Min(delayMilliseconds, 50));

            await _timeProvider.DelayAsync(delay, cancellationToken);
        }
    }

    private TimeSpan GetCurrentVirtualTimeUnsafe(DateTimeOffset now)
    {
        if (_isStopped || _isPaused || _runningStartedAt is null)
        {
            return _accumulatedVirtualTime;
        }

        var elapsed = now - _runningStartedAt.Value;
        return _accumulatedVirtualTime + TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds * _speed);
    }
}
