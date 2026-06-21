using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;
using Xunit;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class CodeComplexityMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "LegacyLens.CodeComplexityMarkdownReportWriterTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var writer = new CodeComplexityMarkdownReportWriter();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            writer.Write(Path.Combine(_tempDirectory, "code-complexity.md"), null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_CreatesCodeComplexityReport()
    {
        var outputPath = Path.Combine(_tempDirectory, "output", "code-complexity.md");
        var report = CreateReport();

        new CodeComplexityMarkdownReportWriter().Write(outputPath, report);

        Assert.True(File.Exists(outputPath));

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# Code Complexity Report", markdown);
        Assert.Contains("## Summary", markdown);
        Assert.Contains("## Project Summary", markdown);
        Assert.Contains("## Member Hotspots", markdown);

        Assert.Contains(
            "This report estimates cyclomatic complexity from C# syntax without building the solution.",
            markdown);
    }

    [Fact]
    public void Write_FormatsEvidenceAndPathsAsMarkdownSafeTableCells()
    {
        var outputPath = Path.Combine(_tempDirectory, "code-complexity.md");
        var report = CreateReport();

        new CodeComplexityMarkdownReportWriter().Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("`if (value \\|\\| other)`", markdown);
        Assert.Contains(@"`C:\Code\Legacy.Web\Controllers\HomeController.cs`", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static CodeComplexityReport CreateReport()
    {
        var member = new CodeComplexityMember(
            "Legacy.Web",
            @"C:\Code\Legacy.Web\Controllers\HomeController.cs",
            42,
            "Legacy.Web.Controllers",
            "Legacy.Web.Controllers.HomeController",
            "Save",
            "Method",
            12,
            CodeComplexitySeverity.High,
            false,
            "if (value || other)");

        var type = new CodeComplexityTypeSummary(
            "Legacy.Web",
            @"C:\Code\Legacy.Web\Controllers\HomeController.cs",
            "Legacy.Web.Controllers",
            "Legacy.Web.Controllers.HomeController",
            1,
            12,
            12,
            12,
            CodeComplexitySeverity.High,
            false);

        var namespaceSummary = new CodeComplexityNamespaceSummary(
            "Legacy.Web",
            "Legacy.Web.Controllers",
            1,
            1,
            12,
            12,
            12,
            CodeComplexitySeverity.High);

        var project = new CodeComplexityProjectSummary(
            "Legacy.Web",
            1,
            1,
            1,
            12,
            12,
            12,
            CodeComplexitySeverity.High,
            0);

        return new CodeComplexityReport(
            new CodeComplexityScanSummary(1, 1, 0, 12, 12, 1, 0, 1, 0),
            [member],
            [type],
            [namespaceSummary],
            [project]);
    }
}