using F1.RaceState.Service.Domain.Models;

namespace F1.RaceState.Service.Application.Models;

public sealed record RaceStateApplyResult(
    RaceStateSnapshot Snapshot,
    bool Applied,
    string? Reason,
    string? StateKey);
