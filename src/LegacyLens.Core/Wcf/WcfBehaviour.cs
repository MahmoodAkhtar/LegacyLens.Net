namespace LegacyLens.Core.Wcf;

public sealed class WcfBehaviour
{
    public required WcfBehaviourKind Kind { get; init; }
    public required string ConfigFilePath { get; init; }

    public string? Name { get; init; }

    public bool HasServiceMetadata { get; init; }
    public string? ServiceMetadataHttpGetEnabled { get; init; }
    public string? ServiceMetadataHttpsGetEnabled { get; init; }

    public bool HasServiceDebug { get; init; }
    public string? IncludeExceptionDetailInFaults { get; init; }

    public bool HasServiceThrottling { get; init; }
    public string? MaxConcurrentCalls { get; init; }
    public string? MaxConcurrentSessions { get; init; }
    public string? MaxConcurrentInstances { get; init; }

    public bool HasWebHttp { get; init; }
}