namespace LegacyLens.Core.Files;

public sealed record ScanFileInventory(
    IReadOnlyList<ScanFile> CSharpFiles,
    IReadOnlyList<ScanFile> EdmxFiles,
    IReadOnlyList<ScanFile> DbmlFiles,
    IReadOnlyList<ScanFile> T4Files,
    IReadOnlyList<string> MigrationDirectories)
{
    public static ScanFileInventory Empty { get; } = new(
        Array.Empty<ScanFile>(),
        Array.Empty<ScanFile>(),
        Array.Empty<ScanFile>(),
        Array.Empty<ScanFile>(),
        Array.Empty<string>());
}