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

        var checkpointPath = ResolveCheckpointPath();
        var currentStatePath = ResolveCurrentStatePath();
        Directory.CreateDirectory(Path.GetDirectoryName(checkpointPath)!);

        await WriteJsonAtomicallyAsync(checkpointPath, checkpoint, cancellationToken);
        await WriteJsonAtomicallyAsync(currentStatePath, checkpoint.Snapshot, cancellationToken);
    }

    public async Task<RaceStateCheckpoint?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var path = ResolveCheckpointPath();
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<RaceStateCheckpoint>(stream, SerializerOptions, cancellationToken);
    }

    public string ResolveCheckpointPath()
    {
        var storageDirectory = Path.IsPathRooted(_options.StorageDirectory)
            ? _options.StorageDirectory
            : Path.GetFullPath(_options.StorageDirectory);

        return Path.Combine(storageDirectory, _options.CheckpointFileName);
    }

    public string ResolveCurrentStatePath()
    {
        var storageDirectory = Path.IsPathRooted(_options.StorageDirectory)
            ? _options.StorageDirectory
            : Path.GetFullPath(_options.StorageDirectory);

        return Path.Combine(storageDirectory, _options.CurrentStateFileName);
    }

    private static async Task WriteJsonAtomicallyAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var tempPath = $"{path}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, SerializerOptions, cancellationToken);
        }

        File.Move(tempPath, path, true);
    }
}
