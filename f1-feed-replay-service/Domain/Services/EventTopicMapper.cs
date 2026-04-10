using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Domain.Models;

namespace F1.FeedReplay.Service.Domain.Services;

public sealed class EventTopicMapper : IEventTopicMapper
{
    public string MapTopic(ReplayEvent replayEvent)
    {
        return replayEvent.SourceFeed switch
        {
            "SessionInfo" or "DriverList" or "LapCount" => "f1/raw/session",
            "TrackStatus" => "f1/raw/track-status",
            "TimingData" or "TimingStats" => "f1/raw/timing",
            "RaceControlMessages" => "f1/raw/race-control",
            "CarData.z" => "f1/raw/telemetry",
            "Position.z" => "f1/raw/position",
            "CurrentTyres" or "TyreStintSeries" => "f1/raw/tyres",
            "PitLaneTimeCollection" => "f1/raw/pit",
            _ => $"f1/raw/{ToKebabCase(replayEvent.SourceFeed)}"
        };
    }

    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "unknown";
        }

        var builder = new System.Text.StringBuilder(input.Length + 8);
        for (var index = 0; index < input.Length; index++)
        {
            var character = input[index];
            if (char.IsUpper(character) && index > 0)
            {
                builder.Append('-');
            }

            if (character is '.' or '_' or ' ')
            {
                builder.Append('-');
            }
            else
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}
