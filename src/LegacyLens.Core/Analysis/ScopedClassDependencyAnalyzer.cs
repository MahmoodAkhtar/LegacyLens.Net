namespace LegacyLens.Core.Analysis;

public sealed class ScopedClassDependencyAnalyzer
{
    public ScopedClassDependencyReport Analyze(
        ClassDependencyReport classDependencyReport,
        string requestedTypeName,
        DateTimeOffset generatedLocal,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(classDependencyReport);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedTypeName);

        var requested = requestedTypeName.Trim();

        var matches = classDependencyReport.Types
            .Where(type => type.FullName.Equals(requested, StringComparison.OrdinalIgnoreCase))
            .OrderBy(type => type.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(type => type.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(type => type.LineNumber)
            .ToArray();

        if (matches.Length != 1)
        {
            return new ScopedClassDependencyReport(
                requested,
                generatedLocal,
                generatedUtc,
                classDependencyReport.SourceFileCount,
                classDependencyReport.Types.Count,
                matches,
                RootType: null,
                OutboundDependencies: Array.Empty<ClassDependency>(),
                InboundDependants: Array.Empty<ClassDependency>(),
                Concerns: Array.Empty<ClassDependencyConcern>());
        }

        var rootType = matches[0];

        var outboundDependencies = classDependencyReport.Dependencies
            .Where(dependency => IsExactSourceMatch(dependency, rootType))
            .OrderBy(dependency => dependency.TargetType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dependency => dependency.Kind)
            .ThenBy(dependency => dependency.LineNumber)
            .ToArray();

        var inboundDependants = classDependencyReport.Dependencies
            .Where(dependency => IsExactTargetMatch(dependency, rootType))
            .OrderBy(dependency => dependency.SourceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dependency => dependency.Kind)
            .ThenBy(dependency => dependency.LineNumber)
            .ToArray();

        var scopedDependencyKeys = outboundDependencies
            .Concat(inboundDependants)
            .Select(CreateDependencyEvidenceKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var concerns = classDependencyReport.Concerns
            .Where(concern =>
                IsExactConcernSourceMatch(concern, rootType) ||
                IsExactConcernTargetMatch(concern, rootType) ||
                scopedDependencyKeys.Contains(CreateConcernEvidenceKey(concern)))
            .OrderBy(concern => concern.Severity)
            .ThenBy(concern => concern.SourceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(concern => concern.TargetType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(concern => concern.DependencyKind)
            .ThenBy(concern => concern.LineNumber)
            .ToArray();

        return new ScopedClassDependencyReport(
            requested,
            generatedLocal,
            generatedUtc,
            classDependencyReport.SourceFileCount,
            classDependencyReport.Types.Count,
            matches,
            rootType,
            outboundDependencies,
            inboundDependants,
            concerns);
    }

    private static bool IsExactSourceMatch(ClassDependency dependency, DiscoveredType rootType)
    {
        if (!string.IsNullOrWhiteSpace(dependency.SourceFullName))
        {
            return dependency.SourceFullName.Equals(rootType.FullName, StringComparison.OrdinalIgnoreCase);
        }

        return dependency.SourcePath.Equals(rootType.SourcePath, StringComparison.OrdinalIgnoreCase) &&
               dependency.SourceType.Equals(rootType.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExactTargetMatch(ClassDependency dependency, DiscoveredType rootType)
    {
        return !string.IsNullOrWhiteSpace(dependency.TargetFullName) &&
               dependency.TargetFullName.Equals(rootType.FullName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExactConcernSourceMatch(ClassDependencyConcern concern, DiscoveredType rootType)
    {
        if (!string.IsNullOrWhiteSpace(concern.SourceFullName))
        {
            return concern.SourceFullName.Equals(rootType.FullName, StringComparison.OrdinalIgnoreCase);
        }

        return concern.SourcePath.Equals(rootType.SourcePath, StringComparison.OrdinalIgnoreCase) &&
               concern.SourceType.Equals(rootType.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExactConcernTargetMatch(ClassDependencyConcern concern, DiscoveredType rootType)
    {
        return !string.IsNullOrWhiteSpace(concern.TargetFullName) &&
               concern.TargetFullName.Equals(rootType.FullName, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateDependencyEvidenceKey(ClassDependency dependency)
    {
        return string.Join(
            "|",
            dependency.ProjectName,
            dependency.SourcePath,
            dependency.LineNumber,
            dependency.SourceType,
            dependency.TargetType,
            dependency.Kind,
            dependency.Evidence);
    }

    private static string CreateConcernEvidenceKey(ClassDependencyConcern concern)
    {
        return string.Join(
            "|",
            concern.ProjectName,
            concern.SourcePath,
            concern.LineNumber,
            concern.SourceType,
            concern.TargetType,
            concern.DependencyKind,
            concern.Evidence);
    }
}
