using System.Text.Json;
using F1.RaceState.Service.Domain.Models;
using F1.RaceState.Service.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace F1.RaceState.Service.Infrastructure.Persistence;

public sealed class StatePersistenceService(IOptions<StatePersistenceOptions> options)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly StatePersistenceOptions _options = options.Value;

    public bool IsEnabled => _options.Enabled;
    public bool AutoRestoreOnStartup => _options.AutoRestoreOnStartup;

    public async Task PersistAsync(RaceStateCheckpoint checkpoint, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var path = ResolveSnapshotPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var tempPath = $"{path}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, checkpoint, SerializerOptions, cancellationToken);
        }

        File.Move(tempPath, path, true);
    }

    public async Task<RaceStateCheckpoint?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var path = ResolveSnapshotPath();
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<RaceStateCheckpoint>(stream, SerializerOptions, cancellationToken);
    }

    public string ResolveSnapshotPath()
    {
        var storageDirectory = Path.IsPathRooted(_options.StorageDirectory)
            ? _options.StorageDirectory
            : Path.GetFullPath(_options.StorageDirectory);

        return Path.Combine(storageDirectory, _options.SnapshotFileName);
    }
}
