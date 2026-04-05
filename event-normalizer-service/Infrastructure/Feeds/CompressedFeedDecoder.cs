using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;

namespace F1.EventNormalizer.Service.Infrastructure.Feeds;

public static class CompressedFeedDecoder
{
    public static JsonObject? DecodeRawData(JsonNode? rawData)
    {
        if (rawData is null)
        {
            return null;
        }

        try
        {
            var encodedPayload = rawData.GetValue<string>();
            var json = DecodeBase64RawDeflateJson(encodedPayload);
            return JsonNode.Parse(json) as JsonObject;
        }
        catch
        {
            return rawData as JsonObject;
        }
    }

    public static string DecodeJsonStreamLine(string line)
    {
        var payload = ExtractPayload(line);
        return DecodeBase64RawDeflateJson(payload);
    }

    public static string ExtractPayload(string line)
    {
        var trimmed = line.TrimStart('\uFEFF').Trim();
        if (LooksLikeTimestampPrefixedLine(trimmed))
        {
            trimmed = trimmed[12..];
        }

        if (trimmed.StartsWith('"') && trimmed.EndsWith('"') && trimmed.Length >= 2)
        {
            trimmed = trimmed[1..^1];
        }

        return trimmed;
    }

    public static string DecodeBase64RawDeflateJson(string payload)
    {
        var compressedBytes = Convert.FromBase64String(payload);
        using var input = new MemoryStream(compressedBytes);
        using var deflateStream = new DeflateStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(deflateStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static bool LooksLikeTimestampPrefixedLine(string line)
        => line.Length > 12
           && line[2] == ':'
           && line[5] == ':'
           && line[8] == '.';
}
