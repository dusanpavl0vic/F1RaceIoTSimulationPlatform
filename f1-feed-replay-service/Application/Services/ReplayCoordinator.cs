using F1.FeedReplay.Service.Application.Commands;
using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Application.Models;
using F1.FeedReplay.Service.Domain.Models;
using F1.FeedReplay.Service.Domain.Services;
using F1.FeedReplay.Service.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace F1.FeedReplay.Service.Application.Services;

public sealed class ReplayCoordinator(
    IFeedParser feedParser,
    IReplayConfigurationLoader configurationLoader,
    IReplayExecutionQueue executionQueue,
    VirtualClock virtualClock,
    IReplayTimeProvider timeProvider,
    IOptions<ReplayServiceOptions> replayOptions,
    ILogger<ReplayCoordinator> logger) : IReplayCoordinator
{
    private static readonly HashSet<string> EmptyExcludedFeeds = new(StringComparer.Ordinal);

    private readonly object _gate = new();
    private readonly IFeedParser _feedParser = feedParser;
    private readonly IReplayConfigurationLoader _configurationLoader = configurationLoader;
    private readonly IReplayExecutionQueue _executionQueue = executionQueue;
    private readonly VirtualClock _virtualClock = virtualClock;
    private readonly IReplayTimeProvider _timeProvider = timeProvider;
    private readonly ReplayServiceOptions _replayOptions = replayOptions.Value;
    private readonly ILogger<ReplayCoordinator> _logger = logger;
    private readonly HashSet<string> _excludedFeeds = replayOptions.Value.ExcludedFeeds.Length == 0
        ? EmptyExcludedFeeds
        : new HashSet<string>(replayOptions.Value.ExcludedFeeds, StringComparer.Ordinal);

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

        var configuration = await _configurationLoader.LoadAsync(command.ConfigurationPath ?? string.Empty, cancellationToken);
        var parsedEvents = new List<ReplayEvent>();
        var activeFeeds = configuration.Feeds
            .Where(feed => !_excludedFeeds.Contains(feed.Name))
            .ToArray();

        var skippedFeedCount = configuration.Feeds.Count - activeFeeds.Length;
        if (skippedFeedCount > 0)
        {
            _logger.LogInformation(
                "Replay loading skipped {SkippedFeedCount} excluded feeds: {ExcludedFeeds}.",
                skippedFeedCount,
                string.Join(", ", configuration.Feeds.Where(feed => _excludedFeeds.Contains(feed.Name)).Select(feed => feed.Name)));
        }

        foreach (var indexedFeed in activeFeeds.Select((feed, index) => (feed, index)))
        {
            var events = await _feedParser.ParseAsync(configuration, indexedFeed.feed, indexedFeed.index, cancellationToken);
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

        if (_replayOptions.StartFromSessionStatusStarted)
        {
            orderedEvents = FilterEventsFromSessionStart(
                orderedEvents,
                activeFeeds.Select(feed => feed.Name).ToHashSet(StringComparer.Ordinal));
        }

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
            "Loaded replay session {SessionId} with {EventCount} events across {FeedCount} feeds. Simulation reference: {SimulationReference}. StartFromSessionStatusStarted: {StartFromSessionStatusStarted}.",
            configuration.SessionId,
            orderedEvents.Length,
            activeFeeds.Length,
            simulationStartReference,
            _replayOptions.StartFromSessionStatusStarted);

        return GetStatus();
    }

    public async Task<ReplayStatusModel> StartAsync(StartReplayCommand command, CancellationToken cancellationToken)
    {
        if (GetStatus().SessionId is null)
        {
            await LoadAsync(new LoadReplayCommand(null), cancellationToken);
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

    private ReplayEvent[] FilterEventsFromSessionStart(
        IReadOnlyList<ReplayEvent> orderedEvents,
        IReadOnlySet<string> activeFeedNames)
    {
        var sessionStartedEvent = orderedEvents.FirstOrDefault(IsSessionStartedEvent);
        if (sessionStartedEvent is null)
        {
            _logger.LogWarning(
                "Replay option StartFromSessionStatusStarted is enabled, but SessionStatus Started was not found. Using the full event stream.");
            return orderedEvents.ToArray();
        }

        var leadIn = TimeSpan.FromSeconds(Math.Max(0, _replayOptions.SessionStartLeadInSeconds));
        var replayStartTime = sessionStartedEvent.EventTime - leadIn;

        var filteredEvents = orderedEvents
            .Where(evt => evt.EventTime >= replayStartTime)
            .Select((evt, index) => evt with { StableOrder = index })
            .ToArray();

        var bootstrapEvents = BuildBootstrapStateEvents(
            orderedEvents,
            activeFeedNames,
            replayStartTime);
        if (bootstrapEvents.Length > 0)
        {
            filteredEvents = bootstrapEvents
                .Concat(filteredEvents)
                .OrderBy(evt => evt.EventTime)
                .ThenBy(evt => evt.Sequence)
                .ThenBy(evt => evt.StableOrder)
                .Select((evt, index) => evt with { StableOrder = index })
                .ToArray();
        }

        var removedEventCount = orderedEvents.Count(evt => evt.EventTime < replayStartTime);

        _logger.LogInformation(
            "Filtered replay events from {ReplayStartTime} using first SessionStatus Started at {SessionStart}. Lead-in: {LeadInSeconds}s. Added {BootstrapEventCount} bootstrap events. Removed {RemovedEventCount} pre-start events. Remaining events: {RemainingEventCount}.",
            replayStartTime,
            sessionStartedEvent.EventTime,
            _replayOptions.SessionStartLeadInSeconds,
            bootstrapEvents.Length,
            removedEventCount,
            filteredEvents.Length);

        if (filteredEvents.Length == 0)
        {
            throw new InvalidOperationException("SessionStatus Started was found, but no replay events remained after filtering.");
        }

        return filteredEvents;
    }

    private static bool IsSessionStartedEvent(ReplayEvent replayEvent)
        => string.Equals(replayEvent.SourceFeed, "SessionStatus", StringComparison.Ordinal)
           && string.Equals(replayEvent.Payload["Status"]?.ToString(), "Started", StringComparison.OrdinalIgnoreCase);

    private static ReplayEvent[] BuildBootstrapStateEvents(
        IReadOnlyList<ReplayEvent> orderedEvents,
        IReadOnlySet<string> activeFeedNames,
        DateTimeOffset replayStartTime)
    {
        var bootstrapEvents = new List<ReplayEvent>();
        var bootstrapSequence = 0L;

        foreach (var sourceFeed in activeFeedNames.OrderBy(name => name, StringComparer.Ordinal))
        {
            var feedEvents = orderedEvents
                .Where(evt => string.Equals(evt.SourceFeed, sourceFeed, StringComparison.Ordinal) && evt.EventTime < replayStartTime)
                .ToArray();

            if (feedEvents.Length == 0)
            {
                continue;
            }

            JsonNode? payload = BuildBootstrapPayloadForFeed(sourceFeed, feedEvents);

            if (payload is null)
            {
                continue;
            }

            var referenceEvent = feedEvents[^1];
            bootstrapEvents.Add(referenceEvent with
            {
                EventTime = replayStartTime,
                Sequence = bootstrapSequence++,
                StableOrder = -1,
                Payload = payload
            });
        }

        return bootstrapEvents.ToArray();
    }

    private static JsonNode? BuildBootstrapPayloadForFeed(
        string sourceFeed,
        IReadOnlyList<ReplayEvent> feedEvents)
        => sourceFeed switch
        {
            "DriverList" => MergeObjects(feedEvents.Select(evt => evt.Payload)),
            "TimingData" => WrapMergedChildObject("Lines", feedEvents.Select(evt => evt.Payload["Lines"])),
            "TimingStats" => WrapMergedChildObject("Lines", feedEvents.Select(evt => evt.Payload["Lines"])),
            "TimingAppData" => WrapMergedChildObject("Lines", feedEvents.Select(evt => evt.Payload["Lines"])),
            "CurrentTyres" => WrapMergedChildObject("Tyres", feedEvents.Select(evt => evt.Payload["Tyres"])),
            "TyreStintSeries" => WrapMergedChildObject("Stints", feedEvents.Select(evt => evt.Payload["Stints"])),
            "PitLaneTimeCollection" => WrapMergedChildObject("PitTimes", feedEvents.Select(evt => evt.Payload["PitTimes"])),
            _ => feedEvents[^1].Payload.DeepClone()
        };

    private static JsonNode? WrapMergedChildObject(string propertyName, IEnumerable<JsonNode?> nodes)
    {
        var merged = MergeObjects(nodes);
        return merged is null ? null : new JsonObject { [propertyName] = merged };
    }

    private static JsonObject? MergeObjects(IEnumerable<JsonNode?> nodes)
    {
        JsonObject? merged = null;

        foreach (var node in nodes)
        {
            if (node is not JsonObject current)
            {
                continue;
            }

            merged = merged is null
                ? current.DeepClone().AsObject()
                : MergeJsonObjects(merged, current);
        }

        return merged;
    }

    private static JsonObject MergeJsonObjects(JsonObject target, JsonObject source)
    {
        var merged = target.DeepClone().AsObject();

        foreach (var entry in source)
        {
            if (entry.Value is JsonObject sourceObject && merged[entry.Key] is JsonObject targetObject)
            {
                merged[entry.Key] = MergeJsonObjects(targetObject, sourceObject);
            }
            else
            {
                merged[entry.Key] = entry.Value?.DeepClone();
            }
        }

        return merged;
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
