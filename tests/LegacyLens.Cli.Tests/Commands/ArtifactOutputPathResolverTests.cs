using LegacyLens.Cli.Commands;

namespace LegacyLens.Cli.Tests.Commands;

public sealed class ArtifactOutputPathResolverTests
{
    [Fact]
    public void Resolve_WhenOutputDirectoryIsProvided_UsesOutputDirectory()
    {
        var scanPath = CreateScanPath();
        var outputDirectory = Path.Combine(Path.GetTempPath(), "LegacyLens.Tests", "Reports");
        var fileName = "upgrade-readiness-report.md";

        var options = new ScanOptions
        {
            Path = scanPath,
            OutputDirectory = outputDirectory
        };

        var result = ArtifactOutputPathResolver.Resolve(scanPath, options, fileName);

        Assert.Equal(
            Path.Combine(Path.GetFullPath(outputDirectory), fileName),
            result);
    }

    [Fact]
    public void Resolve_WhenOutputDirectoryAndOutputAreProvided_UsesOutputDirectory()
    {
        var scanPath = CreateScanPath();
        var outputDirectory = Path.Combine(Path.GetTempPath(), "LegacyLens.Tests", "Reports");
        var output = Path.Combine(Path.GetTempPath(), "LegacyLens.Tests", "MainReport", "custom-discovery.md");
        var fileName = "upgrade-blockers.md";

        var options = new ScanOptions
        {
            Path = scanPath,
            Output = output,
            OutputDirectory = outputDirectory
        };

        var result = ArtifactOutputPathResolver.Resolve(scanPath, options, fileName);

        Assert.Equal(
            Path.Combine(Path.GetFullPath(outputDirectory), fileName),
            result);
    }

    [Fact]
    public void Resolve_WhenOutputDirectoryIsNotProvidedAndOutputIsProvided_UsesOutputDirectoryFromOutput()
    {
        var scanPath = CreateScanPath();
        var outputDirectory = Path.Combine(Path.GetTempPath(), "LegacyLens.Tests", "MainReport");
        var output = Path.Combine(outputDirectory, "custom-discovery.md");
        var fileName = "external-dependencies.md";

        var options = new ScanOptions
        {
            Path = scanPath,
            Output = output
        };

        var result = ArtifactOutputPathResolver.Resolve(scanPath, options, fileName);

        Assert.Equal(
            Path.Combine(Path.GetFullPath(outputDirectory), fileName),
            result);
    }

    [Fact]
    public void Resolve_WhenNeitherOutputDirectoryNorOutputIsProvided_UsesScanPathOutputDirectory()
    {
        var scanPath = CreateScanPath();
        var fileName = "data-access-inventory.md";

        var options = new ScanOptions
        {
            Path = scanPath
        };

        var result = ArtifactOutputPathResolver.Resolve(scanPath, options, fileName);

        Assert.Equal(
            Path.Combine(scanPath, "output", fileName),
            result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Resolve_WhenScanPathIsNullOrWhiteSpace_ThrowsArgumentException(string? scanPath)
    {
        var options = new ScanOptions
        {
            Path = "scan-path"
        };

        Assert.ThrowsAny<ArgumentException>(() =>
            ArtifactOutputPathResolver.Resolve(
                scanPath!,
                options,
                "edmx-analysis.md"));
    }

    [Fact]
    public void Resolve_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        var scanPath = CreateScanPath();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            ArtifactOutputPathResolver.Resolve(
                scanPath,
                null!,
                "edmx-analysis.md"));

        Assert.Equal("options", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Resolve_WhenFileNameIsNullOrWhiteSpace_ThrowsArgumentException(string? fileName)
    {
        var scanPath = CreateScanPath();

        var options = new ScanOptions
        {
            Path = scanPath
        };

        Assert.ThrowsAny<ArgumentException>(() =>
            ArtifactOutputPathResolver.Resolve(
                scanPath,
                options,
                fileName!));
    }

    private static string CreateScanPath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.Tests",
            "ScanRoot");
    }
}