namespace LegacyLens.Core.Wcf;

public sealed class WcfEndpoint
{
    public required string ConfigFilePath { get; init; }
    public string? ServiceName { get; init; }
    public string? Address { get; init; }
    public string? Binding { get; init; }
    public string? Contract { get; init; }
}