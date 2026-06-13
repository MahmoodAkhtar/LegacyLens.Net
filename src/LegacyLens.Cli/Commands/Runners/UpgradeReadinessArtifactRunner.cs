using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands.Runners;

public sealed class UpgradeReadinessArtifactRunner : IScanArtifactRunner
{
    private const string OutputFileName = "upgrade-readiness-report.md";

    public string ArtifactName => "upgrade-readiness";

    public bool ShouldRun(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Options.ShouldWriteArtifact(ArtifactName);
    }

    public ScanArtifactResult Run(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var analyzer = new UpgradeReadinessAnalyzer();

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

        var writer = new UpgradeReadinessMarkdownReportWriter();
        writer.Write(outputPath, report);

        return new ScanArtifactResult(
            ArtifactName,
            outputPath,
            report);
    }
}
