namespace F1.RaceState.Service.Domain.Models;

public sealed class RaceStateCheckpoint
{
    public RaceStateSnapshot Snapshot { get; set; } = new();
    public Dictionary<string, AppliedEventVersion> LastAppliedEventVersions { get; set; } = new(StringComparer.Ordinal);
}
