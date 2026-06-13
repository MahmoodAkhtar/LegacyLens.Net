using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands.Runners;

public sealed class SolutionTopologyArtifactRunner : IScanArtifactRunner
{
    private const string OutputFileName = "solution-topology.md";

    public string ArtifactName => "solution-topology";

    public bool ShouldRun(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Options.ShouldWriteArtifact(ArtifactName);
    }

    public ScanArtifactResult Run(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var analyzer = new SolutionTopologyAnalyzer();

        var report = analyzer.Analyze(
            context.Solutions,
            context.Projects,
            context.WcfEndpoints,
            context.WcfServiceContracts,
            context.WcfBehaviours,
            context.LegacyAspNetArtifacts,
            context.ConfigFiles,
            context.ModernisationHints,
            context.FileInventory);

        var outputPath = ArtifactOutputPathResolver.Resolve(
            context.ScanPath,
            context.Options,
            OutputFileName);

        var writer = new SolutionTopologyMarkdownReportWriter();
        writer.Write(outputPath, report);

        return new ScanArtifactResult(
            ArtifactName,
            outputPath,
            report);
    }
}
