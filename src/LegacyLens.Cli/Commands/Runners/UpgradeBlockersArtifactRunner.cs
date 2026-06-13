using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands.Runners;

public sealed class UpgradeBlockersArtifactRunner : IScanArtifactRunner
{
    private const string OutputFileName = "upgrade-blockers.md";

    public string ArtifactName => "upgrade-blockers";

    public bool ShouldRun(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Options.ShouldWriteArtifact(ArtifactName);
    }

    public ScanArtifactResult Run(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            context.Projects,
            context.WcfEndpoints,
            context.WcfServiceContracts,
            context.WcfBehaviours,
            context.LegacyAspNetArtifacts,
            context.ConfigFiles,
            context.ModernisationHints,
            context.Options.UpgradeTarget);

        var outputPath = ArtifactOutputPathResolver.Resolve(
            context.ScanPath,
            context.Options,
            OutputFileName);

        var writer = new UpgradeBlockersMarkdownReportWriter();
        writer.Write(outputPath, report);

        return new ScanArtifactResult(
            ArtifactName,
            outputPath,
            report);
    }
}
