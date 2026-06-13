using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands.Runners;

public sealed class ConfigurationInventoryArtifactRunner : IScanArtifactRunner
{
    private const string OutputFileName = "configuration-inventory.md";

    public string ArtifactName => "configuration-inventory";

    public bool ShouldRun(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Options.ShouldWriteArtifact(ArtifactName);
    }

    public ScanArtifactResult Run(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            context.Projects,
            context.ConfigFiles,
            context.FileInventory);

        var outputPath = ArtifactOutputPathResolver.Resolve(
            context.ScanPath,
            context.Options,
            OutputFileName);

        var writer = new ConfigurationInventoryMarkdownReportWriter();
        writer.Write(outputPath, report);

        return new ScanArtifactResult(
            ArtifactName,
            outputPath,
            report);
    }
}
