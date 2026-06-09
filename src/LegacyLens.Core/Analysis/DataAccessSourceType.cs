namespace LegacyLens.Core.Analysis;

public enum DataAccessSourceType
{
    Configuration,
    PackageReference,
    AssemblyReference,
    ProjectFile,
    SourceCode,
    EdmxFile,
    T4Template,
    DbmlFile,
    MigrationFolder,
    Unknown
}