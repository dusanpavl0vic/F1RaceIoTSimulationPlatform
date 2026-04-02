using F1.FeedReplay.Service.Application.Commands;
using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Application.Models;
using F1.FeedReplay.Service.Domain.Models;
using F1.FeedReplay.Service.Domain.Services;

namespace F1.FeedReplay.Service.Application.Services;

public sealed class ReplayCoordinator(
    IEnumerable<IFeedParser> feedParsers,
    IReplayConfigurationLoader configurationLoader,
    IReplayExecutionQueue executionQueue,
    VirtualClock virtualClock,
    IReplayTimeProvider timeProvider,
    ILogger<ReplayCoordinator> logger) : IReplayCoordinator
{
    private readonly object _gate = new();
    private readonly IEnumerable<IFeedParser> _feedParsers = feedParsers;
    private readonly IReplayConfigurationLoader _configurationLoader = configurationLoader;
    private readonly IReplayExecutionQueue _executionQueue = executionQueue;
    private readonly VirtualClock _virtualClock = virtualClock;
    private readonly IReplayTimeProvider _timeProvider = timeProvider;
    private readonly ILogger<ReplayCoordinator> _logger = logger;

    private ReplaySession? _session;
    private ReplayRunState _state = ReplayRunState.Idle;
    private CancellationTokenSource? _runCancellationSource;
    private double _replaySpeed = 1.0d;
    private long _publishedEvents;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _completedAt;
    private string? _lastError;

    public async Task<ReplayStatusModel> LoadAsync(LoadReplayCommand command, CancellationToken cancellationToken)
    {
        EnsureNotRunning();

        var configuration = await _configurationLoader.LoadAsync(command.ConfigurationPath, cancellationToken);
        var parsedEvents = new List<ReplayEvent>();

        foreach (var indexedFeed in configuration.Feeds.Select((feed, index) => (feed, index)))
        {
            var parser = _feedParsers.FirstOrDefault(candidate => candidate.CanHandle(indexedFeed.feed.Name))
                ?? throw new InvalidOperationException($"No parser registered for feed '{indexedFeed.feed.Name}'.");

            var events = await parser.ParseAsync(configuration, indexedFeed.feed, indexedFeed.index, cancellationToken);
            parsedEvents.AddRange(events);
        }

        if (parsedEvents.Count == 0)
        {
            throw new InvalidOperationException("No replay events were loaded from the configured feeds.");
        }

        var orderedEvents = parsedEvents
            .OrderBy(evt => evt.EventTime)
            .ThenBy(evt => evt.Sequence)
            .ThenBy(evt => evt.StableOrder)
            .Select((evt, index) => evt with { StableOrder = index })
            .ToArray();

        var simulationStartReference = orderedEvents.Min(evt => evt.EventTime);
        orderedEvents = orderedEvents
            .Select((evt, index) => evt with
            {
                OffsetFromStart = evt.EventTime - simulationStartReference + TimeSpan.FromMilliseconds(configuration.StartOffsetMs),
                StableOrder = index
            })
            .ToArray();

        _virtualClock.Reset(configuration.ReplaySpeed);

        lock (_gate)
        {
            _session = new ReplaySession(
                configuration.SessionId,
                configuration,
                simulationStartReference,
                orderedEvents);

            _state = ReplayRunState.Loaded;
            _replaySpeed = configuration.ReplaySpeed;
            _publishedEvents = 0;
            _startedAt = null;
            _completedAt = null;
            _lastError = null;
        }

        _logger.LogInformation(
            "Loaded replay session {SessionId} with {EventCount} events across {FeedCount} feeds. Simulation reference: {SimulationReference}.",
            configuration.SessionId,
            orderedEvents.Length,
            configuration.Feeds.Count,
            simulationStartReference);

        return GetStatus();
    }

    public async Task<ReplayStatusModel> StartAsync(StartReplayCommand command, CancellationToken cancellationToken)
    {
        if (GetStatus().SessionId is null)
        {
            await LoadAsync(new LoadReplayCommand(string.Empty), cancellationToken);
        }

        ReplaySession session;
        CancellationTokenSource runCancellationSource;

        lock (_gate)
        {
            if (_session is null)
            {
                throw new InvalidOperationException("Load a replay configuration before starting the simulation.");
            }

            if (_state is ReplayRunState.Running or ReplayRunState.Paused)
            {
                throw new InvalidOperationException("Replay simulation is already active.");
            }

            _runCancellationSource?.Cancel();
            _runCancellationSource?.Dispose();
            _runCancellationSource = new CancellationTokenSource();
            runCancellationSource = _runCancellationSource;
            session = _session;
            _publishedEvents = 0;
            _startedAt = _timeProvider.GetUtcNow();
            _completedAt = null;
            _lastError = null;
            _state = ReplayRunState.Running;
        }

        _virtualClock.Start(TimeSpan.FromMilliseconds(session.Configuration.StartOffsetMs), _replaySpeed);

        _logger.LogInformation(
            "Starting replay session {SessionId}. Speed x{ReplaySpeed}. Total events: {EventCount}.",
            session.SessionId,
            _replaySpeed,
            session.Events.Count);

        await _executionQueue.EnqueueAsync(
            new ReplayExecutionRequest(
                session,
                _virtualClock,
                runCancellationSource.Token,
                OnEventPublishedAsync,
                OnCompletedAsync,
                OnFaultedAsync),
            cancellationToken);

        return GetStatus();
    }

    public Task<ReplayStatusModel> PauseAsync(PauseReplayCommand command, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_state != ReplayRunState.Running)
            {
                throw new InvalidOperationException("Replay simulation is not running.");
            }

            _virtualClock.Pause();
            _state = ReplayRunState.Paused;
            _logger.LogInformation("Replay simulation paused at virtual time {VirtualTime}.", _virtualClock.GetCurrentVirtualTime());
        }

        return Task.FromResult(GetStatus());
    }

    public Task<ReplayStatusModel> ResumeAsync(ResumeReplayCommand command, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_state != ReplayRunState.Paused)
            {
                throw new InvalidOperationException("Replay simulation is not paused.");
            }

            _virtualClock.Resume();
            _state = ReplayRunState.Running;
            _logger.LogInformation("Replay simulation resumed at speed x{ReplaySpeed}.", _replaySpeed);
        }

        return Task.FromResult(GetStatus());
    }

    public Task<ReplayStatusModel> StopAsync(StopReplayCommand command, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_state is ReplayRunState.Idle or ReplayRunState.Loaded or ReplayRunState.Completed or ReplayRunState.Stopped)
            {
                _virtualClock.Stop();
                _state = ReplayRunState.Stopped;
                _completedAt = _timeProvider.GetUtcNow();
                return Task.FromResult(GetStatus());
            }

            _runCancellationSource?.Cancel();
            _virtualClock.Stop();
            _state = ReplayRunState.Stopped;
            _completedAt = _timeProvider.GetUtcNow();
            _logger.LogInformation("Replay simulation stopped after publishing {PublishedEvents} events.", _publishedEvents);
        }

        return Task.FromResult(GetStatus());
    }

    public Task<ReplayStatusModel> ChangeReplaySpeedAsync(ChangeReplaySpeedCommand command, CancellationToken cancellationToken)
    {
        if (command.Speed <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command.Speed), "Replay speed must be greater than zero.");
        }

        lock (_gate)
        {
            _replaySpeed = command.Speed;
            _virtualClock.ChangeSpeed(command.Speed);
            _logger.LogInformation("Replay speed changed to x{ReplaySpeed}.", command.Speed);
        }

        return Task.FromResult(GetStatus());
    }

    public ReplayStatusModel GetStatus()
    {
        lock (_gate)
        {
            return new ReplayStatusModel(
                _session?.SessionId,
                _state,
                _replaySpeed,
                _publishedEvents,
                _session?.Events.Count ?? 0,
                _session?.SimulationStartReference,
                _virtualClock.GetCurrentVirtualTime(),
                _startedAt,
                _completedAt,
                _lastError);
        }
    }

    private Task OnEventPublishedAsync(ReplayEvent replayEvent)
    {
        lock (_gate)
        {
            _publishedEvents++;
        }

        return Task.CompletedTask;
    }

    private Task OnCompletedAsync()
    {
        lock (_gate)
        {
            if (_state != ReplayRunState.Stopped)
            {
                _state = ReplayRunState.Completed;
                _virtualClock.Pause();
                _completedAt = _timeProvider.GetUtcNow();
            }

            _logger.LogInformation(
                "Replay simulation completed. Published {PublishedEvents}/{TotalEvents} events.",
                _publishedEvents,
                _session?.Events.Count ?? 0);
        }

        return Task.CompletedTask;
    }

    private Task OnFaultedAsync(Exception exception)
    {
        lock (_gate)
        {
            _state = ReplayRunState.Faulted;
            _completedAt = _timeProvider.GetUtcNow();
            _lastError = exception.Message;
        }

        _logger.LogError(exception, "Replay simulation failed.");
        return Task.CompletedTask;
    }

    private void EnsureNotRunning()
    {
        lock (_gate)
        {
            if (_state is ReplayRunState.Running or ReplayRunState.Paused)
            {
                throw new InvalidOperationException("Stop the active replay before loading a new configuration.");
            }
        }
    }
}
