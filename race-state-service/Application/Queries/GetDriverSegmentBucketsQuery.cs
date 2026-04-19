namespace F1.RaceState.Service.Application.Queries;

public sealed record GetDriverSegmentBucketsQuery(
    string SessionId,
    int DriverNumber,
    int LapNumber,
    int BucketCount);
