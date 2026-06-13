using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Mermaid;
using Xunit;

namespace LegacyLens.Reporting.Tests.Mermaid;

public sealed class SolutionTopologyMermaidDiagramWriterTests
{
    [Fact]
    public void WriteProjectDependencyGraph_WhenDependenciesExist_WritesMermaidEdges()
    {
        var report = CreateReport();

        var markdown = new SolutionTopologyMermaidDiagramWriter().WriteProjectDependencyGraph(report);

        Assert.Contains("```mermaid", markdown);
        Assert.Contains("flowchart TD", markdown);
        Assert.Contains("Sample_Web", markdown);
        Assert.Contains("Sample_Services", markdown);
    }

    [Fact]
    public void WriteSolutionProjectMap_WhenMembershipsExist_WritesSolutionEdges()
    {
        var report = CreateReport();

        var markdown = new SolutionTopologyMermaidDiagramWriter().WriteSolutionProjectMap(report);

        Assert.Contains("flowchart LR", markdown);
        Assert.Contains("Sample.sln", markdown);
        Assert.Contains("Sample.Web", markdown);
    }

    private static SolutionTopologyReport CreateReport()
    {
        var role = new ProjectRoleClassification(ProjectTopologyRole.WebApplication, TopologyConfidence.High, []);
        return new SolutionTopologyReport(
            new SolutionTopologySummary(1, 2, 2, 0, 1, 1, 0, 0, 0),
            [new SolutionTopologySolution("Sample", "Sample.sln", 2, ["Sample.Web"], [], "Static membership discovered.")],
            [
                new SolutionTopologyProject("Sample.Web", "Sample.Web.csproj", "net48", role, 1, 0, 0, 0, true, "Web application", TopologyConfidence.High, [], false, TopologyConfidence.Unknown, [], "Application / Entry Points"),
                new SolutionTopologyProject("Sample.Services", "Sample.Services.csproj", "net48", role, 0, 1, 0, 0, false, null, TopologyConfidence.Unknown, [], false, TopologyConfidence.Unknown, [], "Services")
            ],
            [
                new SolutionProjectMembership("Sample", "Sample.sln", "Sample.Web", "Sample.Web.csproj"),
                new SolutionProjectMembership("Sample", "Sample.sln", "Sample.Services", "Sample.Services.csproj")
            ],
            [],
            [new ProjectTopologyDependency("Sample.Web", "Sample.Services", ["Sample"], "Sample.Web.csproj", "Sample.Services.csproj", "..\\Sample.Services\\Sample.Services.csproj")],
            [], [], []);
    }
}
