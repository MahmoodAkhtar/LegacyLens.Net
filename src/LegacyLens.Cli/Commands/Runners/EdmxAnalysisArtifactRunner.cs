using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands.Runners;

public sealed class EdmxAnalysisArtifactRunner : IScanArtifactRunner
{
    private const string OutputFileName = "edmx-analysis.md";

    public string ArtifactName => "edmx-analysis";

    public bool ShouldRun(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Options.ShouldWriteEdmxAnalysis;
    }

    public ScanArtifactResult Run(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var analyzer = new EdmxAnalyzer();

        var report = analyzer.Analyze(
            context.ScanPath,
            context.Projects);

        var outputPath = ArtifactOutputPathResolver.Resolve(
            context.ScanPath,
            context.Options,
            OutputFileName);

        var writer = new EdmxAnalysisMarkdownReportWriter();
        writer.Write(outputPath, report);

        return new ScanArtifactResult(
            ArtifactName,
            outputPath,
            report);
    }
}