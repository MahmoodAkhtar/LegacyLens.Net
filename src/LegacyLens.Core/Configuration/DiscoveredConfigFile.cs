namespace LegacyLens.Core.Configuration;

public sealed class DiscoveredConfigFile
{
    public required string FilePath { get; init; }
    public int AppSettingsCount { get; init; }
    public int ConnectionStringsCount { get; init; }
    public int CustomSectionCount { get; init; }
}