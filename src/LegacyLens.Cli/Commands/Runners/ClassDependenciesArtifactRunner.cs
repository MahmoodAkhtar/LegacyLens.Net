using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands.Runners;

public sealed class ClassDependenciesArtifactRunner : IScanArtifactRunner
{
    private const string OutputFileName = "class-dependencies.md";

    public string ArtifactName => "class-dependencies";

    public bool ShouldRun(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Options.ShouldWriteArtifact(ArtifactName);
    }

    public ScanArtifactResult Run(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(context.FileInventory);

        var outputPath = ArtifactOutputPathResolver.Resolve(
            context.ScanPath,
            context.Options,
            OutputFileName);

        var writer = new ClassDependenciesMarkdownReportWriter();
        writer.Write(outputPath, report);

        return new ScanArtifactResult(
            ArtifactName,
            outputPath,
            report);
    }
}
