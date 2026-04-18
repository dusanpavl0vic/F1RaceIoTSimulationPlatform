using System.Data;
using F1.TelemetryAnalytics.Service.Application.Models;

namespace F1.TelemetryAnalytics.Service.Infrastructure.Persistence.Mappers;

internal static class PostgresAnalyticsRecordMapper
{
    public static SessionOverviewDto MapSessionOverview(IDataRecord record)
        => new(
            record.GetString(0),
            record.IsDBNull(1) ? null : record.GetString(1),
            record.IsDBNull(2) ? null : record.GetString(2),
            record.IsDBNull(3) ? null : record.GetString(3),
            record.IsDBNull(4) ? null : record.GetString(4),
            record.IsDBNull(5) ? null : record.GetString(5),
            record.IsDBNull(6) ? null : record.GetInt32(6),
            record.IsDBNull(7) ? null : record.GetInt32(7),
            record.GetInt32(8),
            record.GetInt32(9),
            (DateTimeOffset)record.GetValue(10));

    public static DriverSessionOverviewDto MapDriverSessionOverview(IDataRecord record)
        => new(
            record.GetInt32(0),
            record.GetString(1),
            record.IsDBNull(2) ? null : record.GetString(2),
            record.IsDBNull(3) ? null : record.GetString(3),
            record.IsDBNull(4) ? null : record.GetInt32(4),
            record.IsDBNull(5) ? null : record.GetInt32(5),
            record.IsDBNull(6) ? null : record.GetInt32(6),
            record.IsDBNull(7) ? null : record.GetString(7),
            record.IsDBNull(8) ? null : record.GetInt32(8),
            record.IsDBNull(9) ? null : record.GetString(9),
            record.IsDBNull(10) ? null : record.GetInt32(10),
            record.IsDBNull(11) ? null : record.GetString(11),
            record.IsDBNull(12) ? null : record.GetInt32(12),
            record.IsDBNull(13) ? null : record.GetInt32(13),
            record.IsDBNull(14) ? null : record.GetString(14),
            record.IsDBNull(15) ? null : record.GetString(15));

    public static DriverStintDto MapDriverStint(IDataRecord record)
        => new(
            record.GetInt32(1),
            record.IsDBNull(2) ? null : record.GetString(2),
            record.IsDBNull(3) ? null : record.GetBoolean(3),
            record.IsDBNull(4) ? null : record.GetInt32(4),
            record.IsDBNull(5) ? null : record.GetInt32(5),
            record.IsDBNull(6) ? null : record.GetInt32(6),
            (DateTimeOffset)record.GetValue(7));

    public static DriverLapSummaryDto MapDriverLapSummary(IDataRecord record)
        => new(
            record.GetInt32(1),
            record.IsDBNull(2) ? null : record.GetInt32(2),
            record.IsDBNull(3) ? null : record.GetString(3),
            record.IsDBNull(4) ? null : record.GetInt32(4),
            record.IsDBNull(5) ? null : record.GetString(5),
            record.IsDBNull(6) ? null : record.GetInt32(6),
            record.IsDBNull(7) ? null : record.GetString(7),
            record.IsDBNull(8) ? null : record.GetInt32(8),
            record.IsDBNull(9) ? null : record.GetString(9),
            record.IsDBNull(10) ? null : record.GetInt32(10),
            record.IsDBNull(11) ? null : record.GetString(11),
            record.IsDBNull(12) ? null : record.GetInt32(12),
            record.IsDBNull(13) ? null : record.GetString(13),
            record.IsDBNull(14) ? null : record.GetBoolean(14),
            record.IsDBNull(15) ? null : record.GetInt32(15),
            record.GetInt32(16),
            record.GetDouble(17),
            record.GetInt32(18),
            record.GetDouble(19),
            record.GetDouble(20),
            record.GetDouble(21));
}
