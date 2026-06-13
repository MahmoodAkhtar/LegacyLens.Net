using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands.Runners;

public sealed class ExternalDependenciesArtifactRunner : IScanArtifactRunner
{
    private const string OutputFileName = "external-dependencies.md";

    public string ArtifactName => "external-dependencies";

    public bool ShouldRun(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Options.ShouldWriteArtifact(ArtifactName);
    }

    public ScanArtifactResult Run(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            context.Projects,
            context.WcfEndpoints,
            context.ConfigFiles);

        var outputPath = ArtifactOutputPathResolver.Resolve(
            context.ScanPath,
            context.Options,
            OutputFileName);

        var writer = new ExternalDependenciesMarkdownReportWriter();
        writer.Write(outputPath, report);

        return new ScanArtifactResult(
            ArtifactName,
            outputPath,
            report);
    }
}
