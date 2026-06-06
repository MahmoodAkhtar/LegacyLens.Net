namespace LegacyLens.Cli.Commands;

public sealed class ScanOptions
{
    public required string Path { get; init; }
    public string? Output { get; init; }
    public string? OutputDirectory { get; init; }
    public string Format { get; init; } = "markdown";
    public bool Quiet { get; init; }
    public bool Verbose { get; init; }
}
