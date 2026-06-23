using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands.Runners;

public sealed class ClassRefactoringOpportunitiesArtifactRunner : IScanArtifactRunner
{
    public string ArtifactName => ScanOptions.ClassRefactoringOpportunitiesArtifact;

    public bool ShouldRun(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Options.ShouldWriteClassRefactoringOpportunitiesArtifact;
    }

    public ScanArtifactResult Run(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestedTypeName = context.Options.ClassRefactoringType;
        if (string.IsNullOrWhiteSpace(requestedTypeName))
        {
            throw new InvalidOperationException("A class refactoring type is required to generate the class-refactoring-opportunities artifact.");
        }

        var generatedLocal = DateTimeOffset.Now;
        var generatedUtc = generatedLocal.ToUniversalTime();

        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();
        var report = analyzer.Analyze(
            context.FileInventory.CSharpFiles,
            requestedTypeName,
            generatedLocal,
            generatedUtc);

        var outputPath = ArtifactOutputPathResolver.Resolve(
            context.ScanPath,
            context.Options,
            CreateOutputFileName(requestedTypeName, generatedLocal));

        var writer = new ClassRefactoringOpportunitiesMarkdownReportWriter();
        writer.Write(outputPath, report);

        return new ScanArtifactResult(
            ArtifactName,
            outputPath,
            report);
    }

    private static string CreateOutputFileName(string requestedTypeName, DateTimeOffset generatedLocal)
    {
        var safeTypeName = CreateWindowsSafeTypeName(requestedTypeName);
        var timestamp = generatedLocal.ToString("yyyyMMdd-HHmmss");

        return $"class-refactoring-opportunities.{safeTypeName}.{timestamp}.md";
    }

    private static string CreateWindowsSafeTypeName(string requestedTypeName)
    {
        var invalidFileNameChars = Path.GetInvalidFileNameChars().ToHashSet();

        var safe = new string(requestedTypeName
            .Trim()
            .Select(character => invalidFileNameChars.Contains(character) || char.IsControl(character) ? '_' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(safe) ? "unknown-type" : safe;
    }
}
