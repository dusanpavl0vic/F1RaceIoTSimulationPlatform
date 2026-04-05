namespace F1.RaceState.Service.Domain.Models;

public sealed record AppliedEventVersion(DateTimeOffset EventTime, long Sequence) : IComparable<AppliedEventVersion>
{
    public int CompareTo(AppliedEventVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var timeComparison = EventTime.CompareTo(other.EventTime);
        if (timeComparison != 0)
        {
            return timeComparison;
        }

        return Sequence.CompareTo(other.Sequence);
    }
}
