using LegacyLens.Cli.Commands;
using LegacyLens.Cli.Progress;

namespace LegacyLens.Cli.Tests.Progress;

public sealed class ScanProgressReporterFactoryTests
{
    [Fact]
    public void Create_WhenQuietIsTrue_ReturnsNullReporter()
    {
        var options = new ScanOptions
        {
            Path = ".",
            Quiet = true
        };

        var reporter = ScanProgressReporterFactory.Create(options);

        Assert.Same(NullScanProgressReporter.Instance, reporter);
    }

    [Fact]
    public void Create_WhenQuietIsFalse_ReturnsConsoleReporter()
    {
        var options = new ScanOptions
        {
            Path = "."
        };

        var reporter = ScanProgressReporterFactory.Create(options);

        Assert.IsType<ConsoleScanProgressReporter>(reporter);
    }

    [Fact]
    public void Create_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ScanProgressReporterFactory.Create(null!));
    }
}
