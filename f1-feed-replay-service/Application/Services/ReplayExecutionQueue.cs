using System.Threading.Channels;
using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Application.Models;

namespace F1.FeedReplay.Service.Application.Services;

public sealed class ReplayExecutionQueue : IReplayExecutionQueue
{
    private readonly Channel<ReplayExecutionRequest> _channel = Channel.CreateUnbounded<ReplayExecutionRequest>();

    public ValueTask EnqueueAsync(ReplayExecutionRequest request, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(request, cancellationToken);

    public ValueTask<ReplayExecutionRequest> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}
