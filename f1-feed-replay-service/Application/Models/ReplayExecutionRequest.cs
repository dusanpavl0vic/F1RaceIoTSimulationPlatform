using F1.FeedReplay.Service.Domain.Models;
using F1.FeedReplay.Service.Domain.Services;

namespace F1.FeedReplay.Service.Application.Models;

public sealed record ReplayExecutionRequest(
    ReplaySession Session,
    VirtualClock Clock,
    CancellationToken RunCancellationToken,
    Func<ReplayEvent, Task> OnEventPublishedAsync,
    Func<Task> OnCompletedAsync,
    Func<Exception, Task> OnFaultedAsync);
