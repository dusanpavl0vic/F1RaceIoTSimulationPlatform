using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Infrastructure.Configuration;
using F1.Shared.Models;
using Microsoft.Extensions.Options;

namespace F1.EventNormalizer.Service.Domain.Services;

public sealed class CanonicalTopicMapper : ICanonicalTopicMapper
{
    private readonly string _topicPrefix;

    public CanonicalTopicMapper(IOptions<MqttOptions> options)
    {
        _topicPrefix = (options.Value.OutputTopicPrefix ?? "f1/canonical").Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(_topicPrefix))
        {
            _topicPrefix = "f1/canonical";
        }
    }

    public string Map(CanonicalEvent canonicalEvent)
    {
        var suffix = canonicalEvent.EventType.Replace('.', '/');
        return canonicalEvent.DriverNumber is int driverNumber
            ? $"{_topicPrefix}/{suffix}/{driverNumber}"
            : $"{_topicPrefix}/{suffix}";
    }
}
