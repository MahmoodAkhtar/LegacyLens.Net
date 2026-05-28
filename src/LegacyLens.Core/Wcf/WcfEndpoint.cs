namespace LegacyLens.Core.Wcf;

public sealed class WcfEndpoint
{
    public required string ConfigFilePath { get; init; }
    public string? ServiceName { get; init; }
    public string? Address { get; init; }
    public string? Binding { get; init; }
    public string? Contract { get; init; }

    public string? BindingConfiguration { get; init; }
    public string? BehaviorConfiguration { get; init; }
    public string? SecurityMode { get; init; }
    public string? TransportClientCredentialType { get; init; }
    public string? MessageClientCredentialType { get; init; }
    public bool IsMetadataExchangeEndpoint { get; init; }
}