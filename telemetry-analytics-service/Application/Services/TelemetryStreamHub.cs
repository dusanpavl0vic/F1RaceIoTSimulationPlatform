using System.Threading.Channels;
using F1.TelemetryAnalytics.Service.Domain.Models;
using F1.TelemetryAnalytics.Service.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace F1.TelemetryAnalytics.Service.Application.Services;

public sealed class TelemetryStreamHub(IOptions<AnalyticsOptions> analyticsOptions)
{
    private readonly object _gate = new();
    private readonly Dictionary<string, DriverTelemetryStreamState> _streams = new(StringComparer.Ordinal);
    private readonly int _bufferSize = Math.Max(100, analyticsOptions.Value.LiveTelemetryBufferSize);

    public void Publish(TelemetrySampleRecord sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        lock (_gate)
        {
            var state = GetOrCreateState(sample.SessionId, sample.DriverNumber);
            state.RecentSamples.Add(sample);
            if (state.RecentSamples.Count > _bufferSize)
            {
                state.RecentSamples.RemoveRange(0, state.RecentSamples.Count - _bufferSize);
            }

            for (var index = state.Subscribers.Count - 1; index >= 0; index--)
            {
                if (!state.Subscribers[index].TryWrite(sample))
                {
                    state.Subscribers.RemoveAt(index);
                }
            }
        }
    }

    public IReadOnlyList<TelemetrySampleRecord> GetRecent(string sessionId, int driverNumber, int maxSamples)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || driverNumber <= 0 || maxSamples <= 0)
        {
            return [];
        }

        lock (_gate)
        {
            if (!_streams.TryGetValue(BuildKey(sessionId, driverNumber), out var state))
            {
                return [];
            }

            return state.RecentSamples
                .TakeLast(maxSamples)
                .ToArray();
        }
    }

    public async IAsyncEnumerable<TelemetrySampleRecord> Subscribe(
        string sessionId,
        int driverNumber,
        int recentSampleCount,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<TelemetrySampleRecord>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        IReadOnlyList<TelemetrySampleRecord> initial;
        lock (_gate)
        {
            var state = GetOrCreateState(sessionId, driverNumber);
            state.Subscribers.Add(channel.Writer);
            initial = state.RecentSamples
                .TakeLast(Math.Max(0, recentSampleCount))
                .ToArray();
        }

        try
        {
            foreach (var sample in initial)
            {
                yield return sample;
            }

            await foreach (var sample in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return sample;
            }
        }
        finally
        {
            lock (_gate)
            {
                if (_streams.TryGetValue(BuildKey(sessionId, driverNumber), out var state))
                {
                    state.Subscribers.Remove(channel.Writer);
                }
            }

            channel.Writer.TryComplete();
        }
    }

    private DriverTelemetryStreamState GetOrCreateState(string sessionId, int driverNumber)
    {
        var key = BuildKey(sessionId, driverNumber);
        if (!_streams.TryGetValue(key, out var state))
        {
            state = new DriverTelemetryStreamState();
            _streams[key] = state;
        }

        return state;
    }

    private static string BuildKey(string sessionId, int driverNumber)
        => $"{sessionId}:{driverNumber}";

    private sealed class DriverTelemetryStreamState
    {
        public List<TelemetrySampleRecord> RecentSamples { get; } = [];
        public List<ChannelWriter<TelemetrySampleRecord>> Subscribers { get; } = [];
    }
}
