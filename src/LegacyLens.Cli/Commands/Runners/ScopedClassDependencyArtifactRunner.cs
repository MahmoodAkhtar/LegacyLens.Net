using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands.Runners;

public sealed class ScopedClassDependencyArtifactRunner : IScanArtifactRunner
{
    public string ArtifactName => ScanOptions.ClassDependencyScopeArtifact;

    public bool ShouldRun(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Options.ShouldWriteScopedClassDependencyArtifact;
    }

    public ScanArtifactResult Run(ScanContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestedTypeName = context.Options.ClassDependencyType;
        if (string.IsNullOrWhiteSpace(requestedTypeName))
        {
            throw new InvalidOperationException("A scoped class dependency type is required to generate the class-dependency-scope artifact.");
        }

        var generatedLocal = DateTimeOffset.Now;
        var generatedUtc = generatedLocal.ToUniversalTime();

        var classDependencyReport = new ClassDependencyAnalyzer().Analyze(context.FileInventory);
        var scopedReport = new ScopedClassDependencyAnalyzer().Analyze(
            classDependencyReport,
            requestedTypeName,
            generatedLocal,
            generatedUtc);

        var outputPath = ArtifactOutputPathResolver.Resolve(
            context.ScanPath,
            context.Options,
            CreateOutputFileName(requestedTypeName, generatedLocal));

        var writer = new ScopedClassDependencyMarkdownReportWriter();
        writer.Write(outputPath, scopedReport);

        return new ScanArtifactResult(
            ArtifactName,
            outputPath,
            scopedReport);
    }

    private static string CreateOutputFileName(string requestedTypeName, DateTimeOffset generatedLocal)
    {
        var safeTypeName = CreateWindowsSafeTypeName(requestedTypeName);
        var timestamp = generatedLocal.ToString("yyyyMMdd-HHmmss");

        return $"class-dependency-scope.{safeTypeName}.{timestamp}.md";
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
