namespace LegacyLens.Core.Wcf;

public sealed class WcfServiceContract
{
    public required string Name { get; init; }
    public required string SourceFilePath { get; init; }
    public List<string> Operations { get; init; } = new();
}