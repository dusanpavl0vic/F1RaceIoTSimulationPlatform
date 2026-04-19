using F1.TelemetryAnalytics.Service.Application.Models;
using F1.TelemetryAnalytics.Service.Domain.Models;

namespace F1.TelemetryAnalytics.Service.Grpc.Mappers;

internal static class TelemetryAnalyticsGrpcMapper
{
    public static SegmentBucket MapSegmentBucket(SegmentBucketDto bucket)
        => new()
        {
            BucketIndex = bucket.BucketIndex,
            StartProgressPct = bucket.StartProgressPct,
            EndProgressPct = bucket.EndProgressPct,
            AvgSpeed = bucket.AverageSpeed,
            MaxSpeed = bucket.MaxSpeed,
            AvgThrottlePct = bucket.AverageThrottlePct,
            BrakeUsagePct = bucket.BrakeUsagePct,
            DrsUsagePct = bucket.DrsUsagePct,
            Behavior = bucket.Behavior
        };

    public static LiveTelemetrySample MapLiveTelemetrySample(TelemetrySampleDto sample)
        => new()
        {
            SessionId = sample.SessionId,
            DriverNumber = sample.DriverNumber,
            LapNumber = sample.LapNumber,
            StintNumber = sample.StintNumber,
            SampleIndex = sample.SampleIndex,
            Timestamp = sample.Timestamp,
            Speed = sample.Speed ?? 0,
            Rpm = sample.Rpm ?? 0,
            ThrottlePct = sample.ThrottlePct ?? 0,
            RawThrottle = sample.RawThrottle ?? 0,
            BrakePct = sample.BrakePct ?? 0d,
            RawBrake = sample.RawBrake ?? 0,
            Gear = sample.Gear ?? 0,
            DrsEnabled = sample.DrsEnabled ?? false
        };

    public static LiveTelemetrySample MapLiveTelemetrySample(TelemetrySampleRecord sample)
        => new()
        {
            SessionId = sample.SessionId,
            DriverNumber = sample.DriverNumber,
            LapNumber = sample.LapNumber,
            StintNumber = sample.StintNumber,
            SampleIndex = sample.SampleIndex,
            Timestamp = sample.Timestamp.ToString("O"),
            Speed = sample.Speed ?? 0,
            Rpm = sample.Rpm ?? 0,
            ThrottlePct = sample.ThrottlePct ?? 0,
            RawThrottle = sample.RawThrottle ?? 0,
            BrakePct = sample.RawBrake ?? (sample.BrakeApplied is true ? 100d : 0d),
            RawBrake = sample.RawBrake ?? 0,
            Gear = sample.Gear ?? 0,
            DrsEnabled = sample.DrsEnabled ?? false
        };
}
