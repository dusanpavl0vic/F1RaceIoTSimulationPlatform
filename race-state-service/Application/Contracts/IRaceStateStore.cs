using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Domain.Models;
using F1.Shared.Models;

namespace F1.RaceState.Service.Application.Contracts;

public interface IRaceStateStore
{
    RaceStateApplyResult Apply(CanonicalEvent canonicalEvent);
    RaceStateSnapshot GetSnapshot();
    RaceStateCheckpoint GetCheckpoint();
    void Restore(RaceStateCheckpoint checkpoint);
}
