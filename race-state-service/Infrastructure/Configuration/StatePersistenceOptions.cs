namespace F1.RaceState.Service.Infrastructure.Configuration;

public sealed class StatePersistenceOptions
{
    public const string SectionName = "StatePersistence";

    public bool Enabled { get; set; } = true;
    public bool AutoRestoreOnStartup { get; set; } = true;
    public string StorageDirectory { get; set; } = "./runtime-data";
    public string SnapshotFileName { get; set; } = "current-race-state.json";
}
