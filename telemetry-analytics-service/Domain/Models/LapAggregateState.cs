namespace F1.TelemetryAnalytics.Service.Domain.Models;

public sealed class LapAggregateState
{
    public required int LapNumber { get; init; }
    public int SampleCount { get; private set; }
    public double SpeedSum { get; private set; }
    public int MaxSpeed { get; private set; }
    public double ThrottleSum { get; private set; }
    public double BrakeSum { get; private set; }
    public double DrsSum { get; private set; }

    public void Absorb(int? speed, int? throttlePct, bool? brakeApplied, bool? drsEnabled)
    {
        SampleCount++;
        SpeedSum += speed ?? 0;
        MaxSpeed = Math.Max(MaxSpeed, speed ?? 0);
        ThrottleSum += throttlePct ?? 0;
        BrakeSum += brakeApplied is true ? 100d : 0d;
        DrsSum += drsEnabled is true ? 100d : 0d;
    }

    public double AverageSpeed => SampleCount == 0 ? 0d : SpeedSum / SampleCount;
    public double AverageThrottlePct => SampleCount == 0 ? 0d : ThrottleSum / SampleCount;
    public double BrakeUsagePct => SampleCount == 0 ? 0d : BrakeSum / SampleCount;
    public double DrsUsagePct => SampleCount == 0 ? 0d : DrsSum / SampleCount;
}
