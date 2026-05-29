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
    public string? OpenTimeout { get; init; }
    public string? CloseTimeout { get; init; }
    public string? SendTimeout { get; init; }
    public string? ReceiveTimeout { get; init; }
    public string? MaxReceivedMessageSize { get; init; }
    public string? MaxBufferSize { get; init; }
    public string? MaxBufferPoolSize { get; init; }
    public string? TransferMode { get; init; }
    public string? ReaderQuotaMaxDepth { get; init; }
    public string? ReaderQuotaMaxStringContentLength { get; init; }
    public string? ReaderQuotaMaxArrayLength { get; init; }
    public string? ReaderQuotaMaxBytesPerRead { get; init; }
    public string? ReaderQuotaMaxNameTableCharCount { get; init; }
}