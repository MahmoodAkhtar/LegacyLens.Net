namespace LegacyLens.Core.Analysis;

public sealed record ClassDependencyReport(
    IReadOnlyList<DiscoveredType> Types,
    IReadOnlyList<ClassDependency> Dependencies,
    IReadOnlyList<ClassDependencyConcern> Concerns,
    IReadOnlyList<ClassCouplingHotspot> Hotspots,
    int SourceFileCount)
{
    public int HardcodedConcreteDependencyCount => Dependencies.Count(dependency =>
        dependency.Kind == ClassDependencyKind.ObjectCreation);

    public int StaticDependencyCount => Dependencies.Count(dependency =>
        dependency.Kind == ClassDependencyKind.StaticMemberAccess);
}

public sealed record DiscoveredType(
    string Name,
    string FullName,
    ClassDiscoveredTypeKind Kind,
    string ProjectName,
    string SourcePath,
    int LineNumber);

public enum ClassDiscoveredTypeKind
{
    Class,
    Interface,
    Record,
    Struct,
    Enum
}

public sealed record ClassDependency(
    string ProjectName,
    string SourcePath,
    int LineNumber,
    string SourceType,
    string TargetType,
    ClassDependencyKind Kind,
    string Evidence);

public enum ClassDependencyKind
{
    ConstructorParameter,
    Field,
    Property,
    MethodParameter,
    ReturnType,
    LocalVariable,
    ObjectCreation,
    StaticMemberAccess,
    BaseClass,
    InterfaceImplementation,
    Attribute,
    GenericTypeArgument
}

public sealed record ClassDependencyConcern(
    ClassDependencyConcernSeverity Severity,
    string SourceType,
    string TargetType,
    ClassDependencyKind DependencyKind,
    string ProjectName,
    string SourcePath,
    int LineNumber,
    string Evidence,
    string WhyItMatters,
    string Recommendation);

public enum ClassDependencyConcernSeverity
{
    High,
    Medium,
    Low
}

public sealed record ClassCouplingHotspot(
    string Type,
    string ProjectName,
    int OutgoingDependencyCount,
    int IncomingDependencyCount,
    int ConcernCount,
    string Notes);