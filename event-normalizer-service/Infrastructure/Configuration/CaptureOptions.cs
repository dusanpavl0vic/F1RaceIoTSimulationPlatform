namespace F1.EventNormalizer.Service.Infrastructure.Configuration;

public sealed class CaptureOptions
{
    public const string SectionName = "Capture";

    public bool Enabled { get; set; } = true;
    public string StorageRootPath { get; set; } = "/data/authoritative-capture";
}
