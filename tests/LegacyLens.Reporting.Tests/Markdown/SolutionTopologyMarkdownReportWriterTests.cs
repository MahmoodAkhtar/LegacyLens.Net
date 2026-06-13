using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;
using Xunit;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class SolutionTopologyMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"LegacyLensSolutionTopologyWriterTests_{Guid.NewGuid():N}");

    [Fact]
    public void Write_WritesExpectedSectionsAndNoCyclesMessage()
    {
        Directory.CreateDirectory(_tempDirectory);
        var outputPath = Path.Combine(_tempDirectory, "solution-topology.md");
        var role = new ProjectRoleClassification(ProjectTopologyRole.Unknown, TopologyConfidence.Unknown, []);
        var report = new SolutionTopologyReport(
            new SolutionTopologySummary(0, 1, 0, 0, 0, 0, 0, 0, 0),
            [],
            [new SolutionTopologyProject("Sample.Core", "Sample.Core.csproj", "net8.0", role, 0, 0, 0, 0, false, null, TopologyConfidence.Unknown, [], false, TopologyConfidence.Unknown, [], "Unknown")],
            [], [], [], [],
            [new SuggestedProjectReviewStep(1, "Sample.Core", "Sample.Core.csproj", "Review when following project references.", "No strong role evidence found.")],
            []);

        new SolutionTopologyMarkdownReportWriter().Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);
        Assert.Contains("# Solution Topology", markdown);
        Assert.Contains("## Analysis Scope", markdown);
        Assert.Contains("## Suggested Reading Order", markdown);
        Assert.Contains("No possible circular project references were discovered", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
