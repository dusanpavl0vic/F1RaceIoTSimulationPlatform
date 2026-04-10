namespace F1.RaceState.Service.Infrastructure.Configuration;

public sealed class StatePersistenceOptions
{
    public const string SectionName = "StatePersistence";

    public bool Enabled { get; set; } = true;
    public bool AutoRestoreOnStartup { get; set; } = true;
    public bool ClearFilesOnStartup { get; set; } = false;
    public bool ClearFilesOnShutdown { get; set; } = false;
    public string StorageDirectory { get; set; } = "./runtime-data";
    public string CheckpointFileName { get; set; } = "current-race-state.checkpoint.json";
    public string CurrentStateFileName { get; set; } = "current-race-state.json";
}
