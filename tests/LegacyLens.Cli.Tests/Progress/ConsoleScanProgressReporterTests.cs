using LegacyLens.Cli.Progress;

namespace LegacyLens.Cli.Tests.Progress;

public sealed class ConsoleScanProgressReporterTests
{
    [Fact]
    public void Reporter_WhenAnimationIsDisabled_WritesStableLineBasedPhaseProgress()
    {
        var output = CaptureConsoleOutput(() =>
        {
            using var reporter = new ConsoleScanProgressReporter(verbose: false);

            reporter.ScanStarted("C:\\Repos\\LegacyApp", "C:\\Repos\\LegacyApp\\output\\discovery-report.md");
            reporter.PhaseStarted("Discovering projects");
            reporter.PhaseCompleted("Projects discovered: 4");
            reporter.PhaseStarted("Building file inventory");
            reporter.PhaseCompleted("Source/config/model files indexed: 128");
            reporter.ScanCompleted();
        });

        Assert.Contains("LegacyLens.NET", output);
        Assert.Contains("Scan path: C:\\Repos\\LegacyApp", output);
        Assert.Contains("Report: C:\\Repos\\LegacyApp\\output\\discovery-report.md", output);
        Assert.Contains("Scanning...", output);
        Assert.Contains("| Discovering projects...", output);
        Assert.Contains("✓ Projects discovered: 4", output);
        Assert.Contains("/ Building file inventory...", output);
        Assert.Contains("✓ Source/config/model files indexed: 128", output);
        Assert.Contains("Completed in ", output);
    }

    [Fact]
    public void Reporter_WhenAnimationIsEnabled_WritesActivePhaseWithCarriageReturnAndCleanCompletion()
    {
        using var writer = new StringWriter();
        using var reporter = new ConsoleScanProgressReporter(
            verbose: false,
            writer: writer,
            enableAnimation: true,
            spinnerInterval: TimeSpan.FromSeconds(30));

        reporter.PhaseStarted("Discovering projects");
        reporter.PhaseCompleted("Projects discovered: 4");

        var output = writer.ToString();

        Assert.Contains("\r| Discovering projects...", output);
        Assert.Contains("\r                         \r", output);
        Assert.Contains("✓ Projects discovered: 4", output);
    }

    [Fact]
    public void VerboseDetail_WhenVerboseIsFalse_DoesNotWriteDetail()
    {
        var output = CaptureConsoleOutput(() =>
        {
            using var reporter = new ConsoleScanProgressReporter(verbose: false);

            reporter.VerboseDetail("Project: SampleLegacyApp.Web");
        });

        Assert.DoesNotContain("Project: SampleLegacyApp.Web", output);
    }

    [Fact]
    public void VerboseDetail_WhenVerboseIsTrue_WritesDetail()
    {
        var output = CaptureConsoleOutput(() =>
        {
            using var reporter = new ConsoleScanProgressReporter(verbose: true);

            reporter.VerboseDetail("Project: SampleLegacyApp.Web");
        });

        Assert.Contains("  Project: SampleLegacyApp.Web", output);
    }

    [Fact]
    public void VerboseDetail_WhenAnimationIsActive_WritesDetailOnCleanLineAndResumesSpinner()
    {
        using var writer = new StringWriter();
        using var reporter = new ConsoleScanProgressReporter(
            verbose: true,
            writer: writer,
            enableAnimation: true,
            spinnerInterval: TimeSpan.FromSeconds(30));

        reporter.PhaseStarted("Discovering projects");
        reporter.VerboseDetail("Project: SampleLegacyApp.Web");
        reporter.PhaseCompleted("Projects discovered: 4");

        var output = writer.ToString();

        Assert.Contains("\r| Discovering projects...", output);
        Assert.Contains("  Project: SampleLegacyApp.Web", output);
        Assert.Contains("\r/ Discovering projects...", output);
        Assert.Contains("✓ Projects discovered: 4", output);
    }

    [Fact]
    public void PhaseStarted_WhenPhaseNameIsNullOrWhiteSpace_ThrowsArgumentException()
    {
        using var reporter = new ConsoleScanProgressReporter(verbose: false);

        Assert.Throws<ArgumentException>(() => reporter.PhaseStarted(" "));
    }

    [Fact]
    public void PhaseCompleted_WhenMessageIsNullOrWhiteSpace_ThrowsArgumentException()
    {
        using var reporter = new ConsoleScanProgressReporter(verbose: false);

        Assert.Throws<ArgumentException>(() => reporter.PhaseCompleted(" "));
    }

    [Fact]
    public void VerboseDetail_WhenMessageIsNullOrWhiteSpace_ThrowsArgumentException()
    {
        using var reporter = new ConsoleScanProgressReporter(verbose: true);

        Assert.Throws<ArgumentException>(() => reporter.VerboseDetail(" "));
    }

    private static string CaptureConsoleOutput(Action action)
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();

        try
        {
            Console.SetOut(writer);
            action();
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
