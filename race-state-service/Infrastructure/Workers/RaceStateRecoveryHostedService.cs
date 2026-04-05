using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Infrastructure.Persistence;

namespace F1.RaceState.Service.Infrastructure.Workers;

public sealed class RaceStateRecoveryHostedService(
    IRaceStateStore raceStateStore,
    StatePersistenceService persistenceService,
    ILogger<RaceStateRecoveryHostedService> logger) : BackgroundService
{
    private readonly IRaceStateStore _raceStateStore = raceStateStore;
    private readonly StatePersistenceService _persistenceService = persistenceService;
    private readonly ILogger<RaceStateRecoveryHostedService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_persistenceService.IsEnabled || !_persistenceService.AutoRestoreOnStartup)
        {
            _logger.LogInformation("Race state recovery is disabled.");
            return;
        }

        var checkpoint = await _persistenceService.LoadAsync(stoppingToken);
        if (checkpoint is null)
        {
            _logger.LogInformation("No persisted race state checkpoint found at {CheckpointPath}.", _persistenceService.ResolveCheckpointPath());
            return;
        }

        _raceStateStore.Restore(checkpoint);
        _logger.LogInformation(
            "Restored race state checkpoint for session {SessionId}. Drivers: {DriverCount}. Last event: {LastEventTime:o} / {LastSequence}.",
            checkpoint.Snapshot.SessionId,
            checkpoint.Snapshot.Drivers.Count,
            checkpoint.Snapshot.LastProcessedEventTime,
            checkpoint.Snapshot.LastProcessedSequence);
    }
}
