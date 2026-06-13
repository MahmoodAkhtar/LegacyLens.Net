namespace LegacyLens.Core.Analysis;

public sealed record SolutionTopologyReport(
    SolutionTopologySummary Summary,
    IReadOnlyList<SolutionTopologySolution> Solutions,
    IReadOnlyList<SolutionTopologyProject> Projects,
    IReadOnlyList<SolutionProjectMembership> Memberships,
    IReadOnlyList<SolutionSharedProject> SharedProjects,
    IReadOnlyList<ProjectTopologyDependency> Dependencies,
    IReadOnlyList<ProjectTopologyHotspot> Hotspots,
    IReadOnlyList<SuggestedProjectReviewStep> SuggestedReadingOrder,
    IReadOnlyList<PossibleCircularProjectDependency> PossibleCircularDependencies);

public sealed record SolutionTopologySummary(
    int SolutionCount,
    int ProjectCount,
    int SolutionProjectMembershipCount,
    int SharedProjectCount,
    int ProjectReferenceCount,
    int PossibleEntryPointProjectCount,
    int PossibleTestProjectCount,
    int PossibleCircularProjectReferenceCount,
    int DependencyHotspotCount);

public sealed record SolutionTopologySolution(
    string Name,
    string SolutionFilePath,
    int ProjectCount,
    IReadOnlyList<string> PossibleEntryPointProjects,
    IReadOnlyList<string> PossibleTestProjects,
    string Notes);

public sealed record SolutionTopologyProject(
    string Name,
    string ProjectFilePath,
    string? TargetFramework,
    ProjectRoleClassification Role,
    int OutgoingProjectReferenceCount,
    int IncomingProjectReferenceCount,
    int PackageReferenceCount,
    int AssemblyReferenceCount,
    bool IsPossibleEntryPoint,
    string? PossibleEntryPointType,
    TopologyConfidence EntryPointConfidence,
    IReadOnlyList<ProjectRoleEvidence> EntryPointEvidence,
    bool IsPossibleTestProject,
    TopologyConfidence TestProjectConfidence,
    IReadOnlyList<ProjectRoleEvidence> TestProjectEvidence,
    string Layer);

public sealed record SolutionProjectMembership(
    string SolutionName,
    string SolutionFilePath,
    string ProjectName,
    string ProjectFilePath);

public sealed record SolutionSharedProject(
    string ProjectName,
    string ProjectFilePath,
    IReadOnlyList<string> SolutionNames,
    int SolutionCount,
    string WhyItMatters);

public sealed record ProjectTopologyDependency(
    string SourceProject,
    string TargetProject,
    IReadOnlyList<string> SourceSolutionNames,
    string SourceProjectFilePath,
    string TargetProjectFilePath,
    string Evidence);

public sealed record ProjectTopologyHotspot(
    string ProjectName,
    string ProjectFilePath,
    int IncomingReferences,
    int OutgoingReferences,
    int SolutionCount,
    string PossibleRole,
    string SuggestedReview);

public sealed record ProjectRoleClassification(
    ProjectTopologyRole Role,
    TopologyConfidence Confidence,
    IReadOnlyList<ProjectRoleEvidence> Evidence);

public sealed record ProjectRoleEvidence(
    string Evidence,
    string SourcePath);

public sealed record SuggestedProjectReviewStep(
    int Order,
    string ProjectName,
    string ProjectFilePath,
    string Reason,
    string Evidence);

public sealed record PossibleCircularProjectDependency(
    string Cycle,
    IReadOnlyList<string> ProjectsInvolved,
    string Evidence,
    string SuggestedReview);

public enum ProjectTopologyRole
{
    WebApplication,
    ApiApplication,
    ConsoleApplication,
    WindowsService,
    WcfServiceHost,
    WorkerServiceHost,
    ServiceLibrary,
    DomainCore,
    DataAccess,
    Infrastructure,
    SharedCommon,
    Contracts,
    Test,
    Unknown
}

public enum TopologyConfidence
{
    High,
    Medium,
    Low,
    Unknown
}
