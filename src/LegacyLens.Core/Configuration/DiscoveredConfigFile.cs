namespace LegacyLens.Core.Configuration;

public sealed class DiscoveredConfigFile
{
    public required string FilePath { get; init; }

    public int AppSettingsCount { get; init; }

    public int ConnectionStringsCount { get; init; }

    public int CustomSectionCount { get; init; }

    public List<DiscoveredAppSetting> AppSettings { get; init; } = new();

    public List<DiscoveredConnectionString> ConnectionStrings { get; init; } = new();

    public List<DiscoveredConfigSection> CustomSections { get; init; } = new();
}

public sealed class DiscoveredAppSetting
{
    public required string Key { get; init; }

    public string? MaskedValue { get; init; }
}

public sealed class DiscoveredConnectionString
{
    public required string Name { get; init; }

    public string? ProviderName { get; init; }

    public string? MaskedConnectionString { get; init; }
}

public sealed class DiscoveredConfigSection
{
    public required string Name { get; init; }

    public string? Type { get; init; }
}