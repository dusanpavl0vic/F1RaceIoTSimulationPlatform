using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;

var arguments = CliArguments.Parse(args);
if (!arguments.IsValid)
{
    CliArguments.PrintUsage();
    return 1;
}

var reader = new JsonStreamReader(arguments.FilePath);

try
{
    return arguments.Mode switch
    {
        DecoderMode.Position => DecodePosition(reader, arguments),
        DecoderMode.Telemetry => DecodeTelemetry(reader, arguments),
        DecoderMode.Raw => DecodeRaw(reader, arguments),
        _ => 1
    };
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Greska: {exception.Message}");
    return 1;
}

static int DecodePosition(JsonStreamReader reader, CliArguments arguments)
{
    var shown = 0;
    var stats = new Dictionary<int, PositionStats>();

    foreach (var entry in reader.ReadDecodedEntries())
    {
        if (entry.Payload["Position"] is not JsonArray packets)
        {
            continue;
        }

        foreach (var packet in packets.OfType<JsonObject>())
        {
            var timestamp = packet["Timestamp"]?.ToString();
            if (packet["Entries"] is not JsonObject entries)
            {
                continue;
            }

            foreach (var driverEntry in entries)
            {
                if (!int.TryParse(driverEntry.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var driverNumber))
                {
                    continue;
                }

                if (arguments.DriverNumber is not null && arguments.DriverNumber != driverNumber)
                {
                    continue;
                }

                if (driverEntry.Value is not JsonObject position)
                {
                    continue;
                }

                var x = TryGetInteger(position["X"]);
                var y = TryGetInteger(position["Y"]);
                var z = TryGetInteger(position["Z"]);
                var status = position["Status"]?.ToString();
                var isZero = x == 0 && y == 0 && z == 0;

                if (!stats.TryGetValue(driverNumber, out var currentStats))
                {
                    currentStats = new PositionStats();
                    stats[driverNumber] = currentStats;
                }

                currentStats.Total++;
                if (isZero)
                {
                    currentStats.ZeroCoordinates++;
                }
                else
                {
                    currentStats.NonZeroCoordinates++;
                }

                if (arguments.SkipZeroCoordinates && isZero)
                {
                    continue;
                }

                if (shown < arguments.Limit)
                {
                    Console.WriteLine(
                        $"driver={driverNumber} time={timestamp} status={status} x={x} y={y} z={z} zero={(isZero ? "da" : "ne")}");
                    shown++;
                }
            }
        }
    }

    PrintPositionSummary(stats, arguments.DriverNumber);
    return 0;
}

static int DecodeTelemetry(JsonStreamReader reader, CliArguments arguments)
{
    var shown = 0;
    var stats = new Dictionary<int, int>();

    foreach (var entry in reader.ReadDecodedEntries())
    {
        if (entry.Payload["Entries"] is not JsonArray packets)
        {
            continue;
        }

        foreach (var packet in packets.OfType<JsonObject>())
        {
            var utc = packet["Utc"]?.ToString();
            if (packet["Cars"] is not JsonObject cars)
            {
                continue;
            }

            foreach (var carEntry in cars)
            {
                if (!int.TryParse(carEntry.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var driverNumber))
                {
                    continue;
                }

                if (arguments.DriverNumber is not null && arguments.DriverNumber != driverNumber)
                {
                    continue;
                }

                if (carEntry.Value?["Channels"] is not JsonObject channels)
                {
                    continue;
                }

                stats[driverNumber] = stats.TryGetValue(driverNumber, out var total) ? total + 1 : 1;

                var rpm = TryGetInteger(channels["0"]);
                var speed = TryGetInteger(channels["2"]);
                var gear = TryGetInteger(channels["3"]);
                var rawThrottle = TryGetInteger(channels["4"]);
                var rawBrake = TryGetInteger(channels["5"]);
                var rawDrs = TryGetInteger(channels["45"]);

                if (shown < arguments.Limit)
                {
                    Console.WriteLine(
                        $"driver={driverNumber} utc={utc} rpm={rpm} speedKph={speed} gear={gear} throttlePct={NormalizeThrottle(rawThrottle)} rawThrottle={rawThrottle} brakeApplied={NormalizeBrake(rawBrake)} rawBrake={rawBrake} drs={rawDrs}");
                    shown++;
                }
            }
        }
    }

    Console.WriteLine();
    Console.WriteLine("Sažetak telemetrije:");
    foreach (var item in stats.OrderBy(item => item.Key))
    {
        Console.WriteLine($"driver={item.Key} packets={item.Value}");
    }

    return 0;
}

static int DecodeRaw(JsonStreamReader reader, CliArguments arguments)
{
    var shown = 0;
    foreach (var entry in reader.ReadDecodedEntries())
    {
        Console.WriteLine($"line={entry.LineNumber}");
        Console.WriteLine(entry.Payload.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine();

        shown++;
        if (shown >= arguments.Limit)
        {
            break;
        }
    }

    return 0;
}

static void PrintPositionSummary(Dictionary<int, PositionStats> stats, int? driverNumber)
{
    Console.WriteLine();
    Console.WriteLine("Sažetak pozicija:");

    var items = stats
        .OrderBy(item => item.Key)
        .Where(item => driverNumber is null || item.Key == driverNumber.Value);

    foreach (var item in items)
    {
        Console.WriteLine(
            $"driver={item.Key} total={item.Value.Total} nonZero={item.Value.NonZeroCoordinates} zero={item.Value.ZeroCoordinates}");
    }
}

static int? TryGetInteger(JsonNode? node)
{
    if (node is null)
    {
        return null;
    }

    if (node is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var value))
    {
        return value;
    }

    return int.TryParse(node.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
        ? parsed
        : null;
}

static int? NormalizeThrottle(int? rawThrottle)
{
    if (rawThrottle is null)
    {
        return null;
    }

    var normalized = (int)Math.Round(rawThrottle.Value / 104d * 100d, MidpointRounding.AwayFromZero);
    return Math.Clamp(normalized, 0, 100);
}

static bool? NormalizeBrake(int? rawBrake)
    => rawBrake is null ? null : rawBrake.Value > 0;

sealed class JsonStreamReader(string filePath)
{
    public IEnumerable<DecodedJsonEntry> ReadDecodedEntries()
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Feed fajl nije pronadjen.", filePath);
        }

        var lineNumber = 0;
        foreach (var rawLine in File.ReadLines(filePath))
        {
            lineNumber++;
            var line = rawLine.TrimStart('\uFEFF').Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var payloadText = ExtractPayloadText(line);
            var decodedJson = DecodeCompressedPayload(payloadText);
            var jsonNode = JsonNode.Parse(decodedJson) as JsonObject;
            if (jsonNode is null)
            {
                continue;
            }

            yield return new DecodedJsonEntry(lineNumber, jsonNode);
        }
    }

    private static string ExtractPayloadText(string line)
    {
        var payload = line;
        if (LooksLikeTimestampPrefixedLine(line))
        {
            payload = line[12..];
        }

        if (payload.StartsWith('"') && payload.EndsWith('"') && payload.Length >= 2)
        {
            payload = payload[1..^1];
        }

        return payload;
    }

    private static bool LooksLikeTimestampPrefixedLine(string line)
        => line.Length > 12
           && line[2] == ':'
           && line[5] == ':'
           && line[8] == '.';

    private static string DecodeCompressedPayload(string payload)
    {
        var bytes = Convert.FromBase64String(payload);
        using var input = new MemoryStream(bytes);
        using var deflateStream = new DeflateStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(deflateStream);
        return reader.ReadToEnd();
    }
}

sealed record DecodedJsonEntry(int LineNumber, JsonObject Payload);

sealed class PositionStats
{
    public int Total { get; set; }
    public int ZeroCoordinates { get; set; }
    public int NonZeroCoordinates { get; set; }
}

enum DecoderMode
{
    Position,
    Telemetry,
    Raw
}

sealed class CliArguments
{
    public required string FilePath { get; init; }
    public required DecoderMode Mode { get; init; }
    public int? DriverNumber { get; init; }
    public int Limit { get; init; } = 20;
    public bool SkipZeroCoordinates { get; init; }
    public bool IsValid { get; init; }

    public static CliArguments Parse(string[] args)
    {
        string? filePath = null;
        DecoderMode? mode = null;
        int? driverNumber = null;
        var limit = 20;
        var skipZero = false;

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            switch (current)
            {
                case "--file" when index + 1 < args.Length:
                    filePath = args[++index];
                    break;
                case "--mode" when index + 1 < args.Length:
                    mode = args[++index].ToLowerInvariant() switch
                    {
                        "position" => DecoderMode.Position,
                        "telemetry" => DecoderMode.Telemetry,
                        "raw" => DecoderMode.Raw,
                        _ => null
                    };
                    break;
                case "--driver" when index + 1 < args.Length && int.TryParse(args[index + 1], out var parsedDriver):
                    driverNumber = parsedDriver;
                    index++;
                    break;
                case "--limit" when index + 1 < args.Length && int.TryParse(args[index + 1], out var parsedLimit):
                    limit = Math.Max(1, parsedLimit);
                    index++;
                    break;
                case "--skip-zero":
                    skipZero = true;
                    break;
            }
        }

        var isValid = !string.IsNullOrWhiteSpace(filePath) && mode is not null;
        return new CliArguments
        {
            FilePath = filePath ?? string.Empty,
            Mode = mode ?? DecoderMode.Raw,
            DriverNumber = driverNumber,
            Limit = limit,
            SkipZeroCoordinates = skipZero,
            IsValid = isValid
        };
    }

    public static void PrintUsage()
    {
        Console.WriteLine("Upotreba:");
        Console.WriteLine("  dotnet run --project ./tools/f1-feed-decoder -- --mode position --file <putanja> [--driver 3] [--limit 20] [--skip-zero]");
        Console.WriteLine("  dotnet run --project ./tools/f1-feed-decoder -- --mode telemetry --file <putanja> [--driver 44] [--limit 20]");
        Console.WriteLine("  dotnet run --project ./tools/f1-feed-decoder -- --mode raw --file <putanja> [--limit 3]");
        Console.WriteLine();
        Console.WriteLine("Primeri:");
        Console.WriteLine("  dotnet run --project ./tools/f1-feed-decoder -- --mode position --file ./docker/replay-data/2021-09-12-italian-grand-prix-2021-09-12-race/feeds/Position.z.jsonStream --driver 3 --limit 30");
        Console.WriteLine("  dotnet run --project ./tools/f1-feed-decoder -- --mode position --file ./docker/replay-data/2021-09-12-italian-grand-prix-2021-09-12-race/feeds/Position.z.jsonStream --driver 3 --limit 30 --skip-zero");
        Console.WriteLine("  dotnet run --project ./tools/f1-feed-decoder -- --mode telemetry --file ./docker/replay-data/2021-09-12-italian-grand-prix-2021-09-12-race/feeds/CarData.z.jsonStream --driver 44 --limit 20");
    }
}
