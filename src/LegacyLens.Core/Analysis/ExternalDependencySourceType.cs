namespace LegacyLens.Core.Analysis;

public enum ExternalDependencySourceType
{
    Configuration,
    PackageReference,
    AssemblyReference,
    WcfEndpoint,
    NuGetConfig,
    SourceCode,
    ProjectFile,
    Unknown
}