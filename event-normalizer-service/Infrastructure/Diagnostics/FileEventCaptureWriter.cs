using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace F1.EventNormalizer.Service.Infrastructure.Diagnostics;

public sealed class FileEventCaptureWriter : IEventCaptureWriter
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CaptureOptions _options;

    public FileEventCaptureWriter(IOptions<CaptureOptions> options)
    {
        _options = options.Value;
    }

    public async Task WriteAsync(string category, string topic, string payload, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var rootPath = ResolveRootPath(_options.StorageRootPath);
        var categoryPath = Path.Combine(rootPath, category);
        Directory.CreateDirectory(categoryPath);

        var safeTopic = topic.Replace('/', '_').Replace(':', '_');
        var topicPath = Path.Combine(categoryPath, $"{safeTopic}.jsonl");
        var allTopicsPath = Path.Combine(categoryPath, "all-topics.log");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(topicPath, payload + Environment.NewLine, cancellationToken);
            await File.AppendAllTextAsync(allTopicsPath, $"{topic} {payload}{Environment.NewLine}", cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string ResolveRootPath(string storageRootPath)
        => Path.IsPathRooted(storageRootPath)
            ? storageRootPath
            : Path.GetFullPath(storageRootPath);
}
