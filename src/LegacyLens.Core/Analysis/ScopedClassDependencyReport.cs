namespace LegacyLens.Core.Analysis;

public sealed record ScopedClassDependencyReport(
    string RequestedTypeName,
    DateTimeOffset GeneratedLocal,
    DateTimeOffset GeneratedUtc,
    int SourceFileCount,
    int DiscoveredTypeCount,
    IReadOnlyList<DiscoveredType> MatchingTypes,
    DiscoveredType? RootType,
    IReadOnlyList<ClassDependency> OutboundDependencies,
    IReadOnlyList<ClassDependency> InboundDependants,
    IReadOnlyList<ClassDependencyConcern> Concerns)
{
    public bool IsFound => RootType is not null;

    public bool IsAmbiguous => MatchingTypes.Count > 1;

    public bool HasSingleMatch => MatchingTypes.Count == 1;
}
