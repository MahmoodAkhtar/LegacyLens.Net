using System.Text.RegularExpressions;
using System.Xml.Linq;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Analysis;

public sealed class SolutionTopologyAnalyzer
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private static readonly string[] TestPackageNames =
    [
        "xunit", "xunit.runner.visualstudio", "nunit", "nunit3testadapter", "mstest.testframework",
        "mstest.testadapter", "microsoft.net.test.sdk", "fluentassertions", "moq", "nsubstitute"
    ];

    public SolutionTopologyReport Analyze(
        IReadOnlyCollection<DiscoveredSolution> solutions,
        IReadOnlyCollection<DiscoveredProject> projects,
        IReadOnlyCollection<WcfEndpoint> wcfEndpoints,
        IReadOnlyCollection<WcfServiceContract> wcfServiceContracts,
        IReadOnlyCollection<WcfBehaviour> wcfBehaviours,
        IReadOnlyCollection<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyCollection<DiscoveredConfigFile> configFiles,
        IReadOnlyCollection<ModernisationHint> modernisationHints,
        ScanFileInventory fileInventory)
    {
        ArgumentNullException.ThrowIfNull(solutions);
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(wcfEndpoints);
        ArgumentNullException.ThrowIfNull(wcfServiceContracts);
        ArgumentNullException.ThrowIfNull(wcfBehaviours);
        ArgumentNullException.ThrowIfNull(legacyAspNetArtifacts);
        ArgumentNullException.ThrowIfNull(configFiles);
        ArgumentNullException.ThrowIfNull(modernisationHints);
        ArgumentNullException.ThrowIfNull(fileInventory);

        var projectLookup = projects
            .GroupBy(project => NormalisePath(project.ProjectFilePath), Comparer)
            .ToDictionary(group => group.Key, group => group.First(), Comparer);

        var memberships = CreateMemberships(solutions, projectLookup);
        var dependencies = CreateDependencies(projects, memberships, projectLookup);
        var incomingCounts = dependencies
            .GroupBy(dependency => NormalisePath(dependency.TargetProjectFilePath), Comparer)
            .ToDictionary(group => group.Key, group => group.Count(), Comparer);
        var outgoingCounts = dependencies
            .GroupBy(dependency => NormalisePath(dependency.SourceProjectFilePath), Comparer)
            .ToDictionary(group => group.Key, group => group.Count(), Comparer);
        var solutionCounts = memberships
            .GroupBy(membership => NormalisePath(membership.ProjectFilePath), Comparer)
            .ToDictionary(group => group.Key, group => group.Select(x => x.SolutionName).Distinct(Comparer).Count(), Comparer);

        var topologyProjects = projects
            .Select(project => CreateTopologyProject(
                project,
                incomingCounts.GetValueOrDefault(NormalisePath(project.ProjectFilePath)),
                outgoingCounts.GetValueOrDefault(NormalisePath(project.ProjectFilePath)),
                fileInventory,
                legacyAspNetArtifacts,
                configFiles,
                wcfEndpoints,
                wcfServiceContracts))
            .OrderBy(project => project.Name, Comparer)
            .ToArray();

        var sharedProjects = CreateSharedProjects(memberships);
        var topologySolutions = CreateSolutions(solutions, memberships, topologyProjects);
        var hotspots = CreateHotspots(topologyProjects, solutionCounts);
        var cycles = DetectCycles(projects, projectLookup);
        var readingOrder = CreateReadingOrder(topologyProjects, hotspots);

        var summary = new SolutionTopologySummary(
            solutions.Count,
            projects.Count,
            memberships.Count,
            sharedProjects.Count,
            dependencies.Count,
            topologyProjects.Count(project => project.IsPossibleEntryPoint),
            topologyProjects.Count(project => project.IsPossibleTestProject),
            cycles.Count,
            hotspots.Count);

        return new SolutionTopologyReport(
            summary,
            topologySolutions,
            topologyProjects,
            memberships,
            sharedProjects,
            dependencies,
            hotspots,
            readingOrder,
            cycles);
    }

    private static IReadOnlyList<SolutionProjectMembership> CreateMemberships(
        IEnumerable<DiscoveredSolution> solutions,
        IReadOnlyDictionary<string, DiscoveredProject> projectLookup)
    {
        return solutions
            .SelectMany(solution => solution.ProjectFilePaths.Select(projectPath => new { Solution = solution, ProjectPath = projectPath }))
            .Select(candidate =>
            {
                var normalisedPath = NormalisePath(candidate.ProjectPath);
                var projectName = projectLookup.TryGetValue(normalisedPath, out var project)
                    ? project.Name
                    : Path.GetFileNameWithoutExtension(candidate.ProjectPath);

                return new SolutionProjectMembership(
                    candidate.Solution.Name,
                    candidate.Solution.SolutionFilePath,
                    projectName,
                    normalisedPath);
            })
            .OrderBy(membership => membership.SolutionName, Comparer)
            .ThenBy(membership => membership.ProjectName, Comparer)
            .ToArray();
    }

    private static IReadOnlyList<ProjectTopologyDependency> CreateDependencies(
        IEnumerable<DiscoveredProject> projects,
        IReadOnlyCollection<SolutionProjectMembership> memberships,
        IReadOnlyDictionary<string, DiscoveredProject> projectLookup)
    {
        var sourceSolutionLookup = memberships
            .GroupBy(membership => NormalisePath(membership.ProjectFilePath), Comparer)
            .ToDictionary(
                group => group.Key,
                group => group.Select(x => x.SolutionName).Distinct(Comparer).OrderBy(x => x, Comparer).ToArray() as IReadOnlyList<string>,
                Comparer);

        var dependencies = new List<ProjectTopologyDependency>();

        foreach (var sourceProject in projects)
        {
            var sourceDirectory = Path.GetDirectoryName(sourceProject.ProjectFilePath);
            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                continue;
            }

            foreach (var reference in sourceProject.ProjectReferences)
            {
                var targetPath = NormalisePath(Path.Combine(sourceDirectory, reference));
                var targetProject = projectLookup.TryGetValue(targetPath, out var discoveredTarget)
                    ? discoveredTarget
                    : null;

                dependencies.Add(new ProjectTopologyDependency(
                    sourceProject.Name,
                    targetProject?.Name ?? Path.GetFileNameWithoutExtension(reference),
                    sourceSolutionLookup.GetValueOrDefault(NormalisePath(sourceProject.ProjectFilePath), Array.Empty<string>()),
                    NormalisePath(sourceProject.ProjectFilePath),
                    targetPath,
                    reference));
            }
        }

        return dependencies
            .OrderBy(dependency => dependency.SourceProject, Comparer)
            .ThenBy(dependency => dependency.TargetProject, Comparer)
            .ToArray();
    }

    private static SolutionTopologyProject CreateTopologyProject(
        DiscoveredProject project,
        int incomingCount,
        int outgoingCount,
        ScanFileInventory fileInventory,
        IReadOnlyCollection<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyCollection<DiscoveredConfigFile> configFiles,
        IReadOnlyCollection<WcfEndpoint> wcfEndpoints,
        IReadOnlyCollection<WcfServiceContract> wcfServiceContracts)
    {
        var role = ClassifyRole(project, fileInventory, legacyAspNetArtifacts, configFiles, wcfEndpoints, wcfServiceContracts);
        var entryPoint = InferEntryPoint(project, fileInventory, legacyAspNetArtifacts, configFiles, wcfEndpoints);
        var testProject = InferTestProject(project, fileInventory);

        return new SolutionTopologyProject(
            project.Name,
            NormalisePath(project.ProjectFilePath),
            project.TargetFramework,
            role,
            outgoingCount,
            incomingCount,
            project.PackageReferenceDetails.Count > 0 ? project.PackageReferenceDetails.Count : project.PackageReferences.Count,
            project.AssemblyReferences.Count,
            entryPoint.IsEntryPoint,
            entryPoint.EntryPointType,
            entryPoint.Confidence,
            entryPoint.Evidence,
            testProject.IsTestProject,
            testProject.Confidence,
            testProject.Evidence,
            ToLayer(role.Role));
    }

    private static ProjectRoleClassification ClassifyRole(
        DiscoveredProject project,
        ScanFileInventory fileInventory,
        IReadOnlyCollection<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyCollection<DiscoveredConfigFile> configFiles,
        IReadOnlyCollection<WcfEndpoint> wcfEndpoints,
        IReadOnlyCollection<WcfServiceContract> wcfServiceContracts)
    {
        var evidence = new List<ProjectRoleEvidence>();
        var projectFiles = ProjectFiles(fileInventory, project).ToArray();
        var projectDirectory = ProjectDirectory(project);

        if (InferTestProject(project, fileInventory).IsTestProject)
        {
            evidence.Add(new ProjectRoleEvidence("Test project naming or test framework package evidence found.", project.ProjectFilePath));
            return new ProjectRoleClassification(ProjectTopologyRole.Test, TopologyConfidence.High, evidence);
        }

        if (HasLegacyAspNetEvidence(project, legacyAspNetArtifacts, configFiles))
        {
            AddProjectFileEvidence(evidence, "Legacy ASP.NET/Web evidence found.", project, legacyAspNetArtifacts.Select(x => x.FilePath).Concat(configFiles.Select(x => x.FilePath)));
            var apiEvidence = legacyAspNetArtifacts.Any(artifact =>
                IsSameOrChildPath(artifact.FilePath, projectDirectory) &&
                artifact.Kind.ToString().Contains("WebApi", StringComparison.OrdinalIgnoreCase));

            return new ProjectRoleClassification(
                apiEvidence ? ProjectTopologyRole.ApiApplication : ProjectTopologyRole.WebApplication,
                TopologyConfidence.High,
                evidence);
        }

        if (HasWcfHostEvidence(project, wcfEndpoints, configFiles))
        {
            AddProjectFileEvidence(evidence, "WCF endpoint or system.serviceModel configuration evidence found.", project, wcfEndpoints.Select(x => x.ConfigFilePath).Concat(configFiles.Select(x => x.FilePath)));
            return new ProjectRoleClassification(ProjectTopologyRole.WcfServiceHost, TopologyConfidence.High, evidence);
        }

        if (HasWindowsServiceEvidence(projectFiles, out var serviceEvidence))
        {
            evidence.Add(serviceEvidence);
            return new ProjectRoleClassification(ProjectTopologyRole.WindowsService, TopologyConfidence.High, evidence);
        }

        if (HasConsoleEvidence(project, fileInventory, out var consoleEvidence))
        {
            evidence.Add(consoleEvidence);
            return new ProjectRoleClassification(ProjectTopologyRole.ConsoleApplication, TopologyConfidence.Medium, evidence);
        }

        if (HasDataAccessEvidence(project, fileInventory))
        {
            evidence.Add(new ProjectRoleEvidence("Data access package, assembly, model, or source naming evidence found.", project.ProjectFilePath));
            return new ProjectRoleClassification(ProjectTopologyRole.DataAccess, TopologyConfidence.Medium, evidence);
        }

        if (HasWcfContractEvidence(project, wcfServiceContracts))
        {
            AddProjectFileEvidence(evidence, "WCF service contract source evidence found.", project, wcfServiceContracts.Select(x => x.SourceFilePath));
            return new ProjectRoleClassification(ProjectTopologyRole.Contracts, TopologyConfidence.Medium, evidence);
        }

        if (NameContains(project.Name, "Contract", "Contracts", "Dto", "Messages"))
        {
            evidence.Add(new ProjectRoleEvidence("Project name suggests contracts or message types.", project.ProjectFilePath));
            return new ProjectRoleClassification(ProjectTopologyRole.Contracts, TopologyConfidence.Low, evidence);
        }

        if (NameContains(project.Name, "Domain", "Core", "Model", "Models"))
        {
            evidence.Add(new ProjectRoleEvidence("Project name suggests domain/core/model code.", project.ProjectFilePath));
            return new ProjectRoleClassification(ProjectTopologyRole.DomainCore, TopologyConfidence.Low, evidence);
        }

        if (NameContains(project.Name, "Service", "Services"))
        {
            evidence.Add(new ProjectRoleEvidence("Project name suggests service-layer code.", project.ProjectFilePath));
            return new ProjectRoleClassification(ProjectTopologyRole.ServiceLibrary, TopologyConfidence.Low, evidence);
        }

        if (NameContains(project.Name, "Infrastructure", "Infra"))
        {
            evidence.Add(new ProjectRoleEvidence("Project name suggests infrastructure code.", project.ProjectFilePath));
            return new ProjectRoleClassification(ProjectTopologyRole.Infrastructure, TopologyConfidence.Low, evidence);
        }

        if (NameContains(project.Name, "Shared", "Common", "Utility", "Utilities"))
        {
            evidence.Add(new ProjectRoleEvidence("Project name suggests shared/common utility code.", project.ProjectFilePath));
            return new ProjectRoleClassification(ProjectTopologyRole.SharedCommon, TopologyConfidence.Low, evidence);
        }

        return new ProjectRoleClassification(
            ProjectTopologyRole.Unknown,
            TopologyConfidence.Unknown,
            Array.Empty<ProjectRoleEvidence>());
    }

    private static EntryPointInference InferEntryPoint(
        DiscoveredProject project,
        ScanFileInventory fileInventory,
        IReadOnlyCollection<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyCollection<DiscoveredConfigFile> configFiles,
        IReadOnlyCollection<WcfEndpoint> wcfEndpoints)
    {
        var evidence = new List<ProjectRoleEvidence>();

        if (HasLegacyAspNetEvidence(project, legacyAspNetArtifacts, configFiles))
        {
            AddProjectFileEvidence(evidence, "Legacy ASP.NET/Web configuration or source evidence found.", project, legacyAspNetArtifacts.Select(x => x.FilePath).Concat(configFiles.Select(x => x.FilePath)));
            return new EntryPointInference(true, "Web application", TopologyConfidence.High, evidence);
        }

        if (HasWcfHostEvidence(project, wcfEndpoints, configFiles))
        {
            AddProjectFileEvidence(evidence, "WCF endpoint or system.serviceModel configuration evidence found.", project, wcfEndpoints.Select(x => x.ConfigFilePath).Concat(configFiles.Select(x => x.FilePath)));
            return new EntryPointInference(true, "WCF service host", TopologyConfidence.High, evidence);
        }

        if (HasWindowsServiceEvidence(ProjectFiles(fileInventory, project), out var serviceEvidence))
        {
            evidence.Add(serviceEvidence);
            return new EntryPointInference(true, "Windows service", TopologyConfidence.High, evidence);
        }

        if (HasConsoleEvidence(project, fileInventory, out var consoleEvidence))
        {
            evidence.Add(consoleEvidence);
            return new EntryPointInference(true, "Console application", TopologyConfidence.Medium, evidence);
        }

        if (NameContains(project.Name, "Host", "Worker"))
        {
            evidence.Add(new ProjectRoleEvidence("Project name suggests a host or worker entry point.", project.ProjectFilePath));
            return new EntryPointInference(true, "Worker/service host", TopologyConfidence.Low, evidence);
        }

        if (NameContains(project.Name, "Web", "Api", "Application", "App"))
        {
            evidence.Add(new ProjectRoleEvidence("Project name suggests an application entry point.", project.ProjectFilePath));
            return new EntryPointInference(true, "Unknown application entry point", TopologyConfidence.Low, evidence);
        }

        return new EntryPointInference(false, null, TopologyConfidence.Unknown, Array.Empty<ProjectRoleEvidence>());
    }

    private static TestProjectInference InferTestProject(DiscoveredProject project, ScanFileInventory fileInventory)
    {
        var evidence = new List<ProjectRoleEvidence>();

        if (NameContains(project.Name, "Test", "Tests", "UnitTests", "IntegrationTests", "Specs"))
        {
            evidence.Add(new ProjectRoleEvidence("Project name contains a test naming pattern.", project.ProjectFilePath));
        }

        var testPackage = project.PackageReferenceDetails
            .Select(package => package.Name)
            .Concat(project.PackageReferences)
            .FirstOrDefault(package => TestPackageNames.Any(testPackageName => package.Contains(testPackageName, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(testPackage))
        {
            evidence.Add(new ProjectRoleEvidence($"Test framework/package reference discovered: {testPackage}.", project.ProjectFilePath));
        }

        var testFile = ProjectFiles(fileInventory, project)
            .FirstOrDefault(file => file.RelativePath.Contains("test", StringComparison.OrdinalIgnoreCase) ||
                                    file.Content.Contains("[Fact]", StringComparison.OrdinalIgnoreCase) ||
                                    file.Content.Contains("[Test]", StringComparison.OrdinalIgnoreCase) ||
                                    file.Content.Contains("[TestMethod]", StringComparison.OrdinalIgnoreCase));

        if (testFile is not null)
        {
            evidence.Add(new ProjectRoleEvidence("Test source file or test attribute evidence found.", testFile.FullPath));
        }

        if (evidence.Count == 0)
        {
            return new TestProjectInference(false, TopologyConfidence.Unknown, Array.Empty<ProjectRoleEvidence>());
        }

        return new TestProjectInference(
            true,
            evidence.Count >= 2 ? TopologyConfidence.High : TopologyConfidence.Medium,
            evidence);
    }

    private static IReadOnlyList<SolutionSharedProject> CreateSharedProjects(IEnumerable<SolutionProjectMembership> memberships)
    {
        return memberships
            .GroupBy(membership => NormalisePath(membership.ProjectFilePath), Comparer)
            .Select(group => new
            {
                Project = group.First(),
                Solutions = group.Select(x => x.SolutionName).Distinct(Comparer).OrderBy(x => x, Comparer).ToArray()
            })
            .Where(group => group.Solutions.Length > 1)
            .Select(group => new SolutionSharedProject(
                group.Project.ProjectName,
                group.Project.ProjectFilePath,
                group.Solutions,
                group.Solutions.Length,
                "Shared project; changes may affect multiple solution entry points. Requires review before solution-level refactoring."))
            .OrderByDescending(project => project.SolutionCount)
            .ThenBy(project => project.ProjectName, Comparer)
            .ToArray();
    }

    private static IReadOnlyList<SolutionTopologySolution> CreateSolutions(
        IEnumerable<DiscoveredSolution> solutions,
        IReadOnlyCollection<SolutionProjectMembership> memberships,
        IReadOnlyCollection<SolutionTopologyProject> projects)
    {
        var projectLookup = projects.ToDictionary(project => NormalisePath(project.ProjectFilePath), project => project, Comparer);

        return solutions
            .Select(solution =>
            {
                var solutionProjects = memberships
                    .Where(membership => membership.SolutionName.Equals(solution.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(membership => projectLookup.GetValueOrDefault(NormalisePath(membership.ProjectFilePath)))
                    .Where(project => project is not null)
                    .Select(project => project!)
                    .ToArray();

                return new SolutionTopologySolution(
                    solution.Name,
                    solution.SolutionFilePath,
                    solution.ProjectFilePaths.Count,
                    solutionProjects.Where(project => project.IsPossibleEntryPoint).Select(project => project.Name).OrderBy(x => x, Comparer).ToArray(),
                    solutionProjects.Where(project => project.IsPossibleTestProject).Select(project => project.Name).OrderBy(x => x, Comparer).ToArray(),
                    solutionProjects.Length == 0
                        ? "No matching discovered C# projects were associated with this solution."
                        : "Solution membership discovered from static .sln project entries.");
            })
            .OrderBy(solution => solution.Name, Comparer)
            .ToArray();
    }

    private static IReadOnlyList<ProjectTopologyHotspot> CreateHotspots(
        IReadOnlyCollection<SolutionTopologyProject> projects,
        IReadOnlyDictionary<string, int> solutionCounts)
    {
        return projects
            .Select(project =>
            {
                var solutionCount = solutionCounts.GetValueOrDefault(NormalisePath(project.ProjectFilePath));
                return new ProjectTopologyHotspot(
                    project.Name,
                    project.ProjectFilePath,
                    project.IncomingProjectReferenceCount,
                    project.OutgoingProjectReferenceCount,
                    solutionCount,
                    ToRoleLabel(project.Role.Role),
                    CreateHotspotReview(project, solutionCount));
            })
            .Where(hotspot => hotspot.IncomingReferences >= 2 || hotspot.OutgoingReferences >= 3 || hotspot.SolutionCount > 1)
            .OrderByDescending(hotspot => hotspot.SolutionCount)
            .ThenByDescending(hotspot => hotspot.IncomingReferences)
            .ThenByDescending(hotspot => hotspot.OutgoingReferences)
            .ThenBy(hotspot => hotspot.ProjectName, Comparer)
            .Take(20)
            .ToArray();
    }

    private static string CreateHotspotReview(SolutionTopologyProject project, int solutionCount)
    {
        if (solutionCount > 1)
        {
            return "Review first because the project appears in multiple solution files.";
        }

        if (project.IncomingProjectReferenceCount >= 2)
        {
            return "Review first because several projects reference it; changes may have wider impact.";
        }

        if (project.OutgoingProjectReferenceCount >= 3)
        {
            return "Review to understand orchestration or broad dependency usage.";
        }

        return "Review if this project is in the planned change path.";
    }

    private static IReadOnlyList<SuggestedProjectReviewStep> CreateReadingOrder(
        IReadOnlyCollection<SolutionTopologyProject> projects,
        IReadOnlyCollection<ProjectTopologyHotspot> hotspots)
    {
        var hotspotNames = hotspots.Select(hotspot => hotspot.ProjectName).ToHashSet(Comparer);

        return projects
            .OrderBy(project => ReadingPriority(project))
            .ThenByDescending(project => project.IsPossibleEntryPoint)
            .ThenByDescending(project => hotspotNames.Contains(project.Name))
            .ThenBy(project => project.Name, Comparer)
            .Select((project, index) => new SuggestedProjectReviewStep(
                index + 1,
                project.Name,
                project.ProjectFilePath,
                CreateReadingReason(project),
                CreateReadingEvidence(project)))
            .ToArray();
    }

    private static int ReadingPriority(SolutionTopologyProject project)
    {
        if (project.IsPossibleEntryPoint)
        {
            return 0;
        }

        return project.Role.Role switch
        {
            ProjectTopologyRole.WcfServiceHost or ProjectTopologyRole.WorkerServiceHost or ProjectTopologyRole.ServiceLibrary => 1,
            ProjectTopologyRole.DomainCore or ProjectTopologyRole.Contracts => 2,
            ProjectTopologyRole.DataAccess => 3,
            ProjectTopologyRole.Infrastructure or ProjectTopologyRole.SharedCommon => 4,
            ProjectTopologyRole.Test => 5,
            _ => 6
        };
    }

    private static string CreateReadingReason(SolutionTopologyProject project)
    {
        if (project.IsPossibleEntryPoint)
        {
            return "Start here to understand application startup, hosting, routing, or orchestration.";
        }

        return project.Role.Role switch
        {
            ProjectTopologyRole.ServiceLibrary => "Review after entry points to understand service-layer orchestration.",
            ProjectTopologyRole.DomainCore => "Review core business/domain concepts after entry points and service boundaries.",
            ProjectTopologyRole.Contracts => "Review contracts to understand public boundaries and shared DTOs/messages.",
            ProjectTopologyRole.DataAccess => "Review data access after domain/service flow is understood.",
            ProjectTopologyRole.Infrastructure or ProjectTopologyRole.SharedCommon => "Review shared infrastructure/common code after higher-level flow is understood.",
            ProjectTopologyRole.Test => "Review tests after production structure is understood to confirm expected behaviour.",
            _ => "Review when following project references from known entry points or hotspots."
        };
    }

    private static string CreateReadingEvidence(SolutionTopologyProject project)
    {
        var evidence = project.EntryPointEvidence.Count > 0
            ? project.EntryPointEvidence
            : project.Role.Evidence;

        return evidence.Count == 0
            ? "No strong role evidence found; position based on project reference topology."
            : evidence[0].Evidence;
    }

    private static IReadOnlyList<PossibleCircularProjectDependency> DetectCycles(
        IReadOnlyCollection<DiscoveredProject> projects,
        IReadOnlyDictionary<string, DiscoveredProject> projectLookup)
    {
        var graph = projects.ToDictionary(
            project => NormalisePath(project.ProjectFilePath),
            project => ResolveReferencePaths(project, projectLookup).ToArray(),
            Comparer);

        var projectNames = projects.ToDictionary(project => NormalisePath(project.ProjectFilePath), project => project.Name, Comparer);
        var cycles = new List<PossibleCircularProjectDependency>();
        var seen = new HashSet<string>(Comparer);

        foreach (var projectPath in graph.Keys)
        {
            FindCycles(projectPath, projectPath, graph, projectNames, new Stack<string>(), cycles, seen);
        }

        return cycles
            .OrderBy(cycle => cycle.Cycle, Comparer)
            .ToArray();
    }

    private static void FindCycles(
        string start,
        string current,
        IReadOnlyDictionary<string, string[]> graph,
        IReadOnlyDictionary<string, string> projectNames,
        Stack<string> path,
        ICollection<PossibleCircularProjectDependency> cycles,
        ISet<string> seen)
    {
        path.Push(current);

        foreach (var next in graph.GetValueOrDefault(current, Array.Empty<string>()))
        {
            if (next.Equals(start, StringComparison.OrdinalIgnoreCase))
            {
                var cycleProjects = path.Reverse().Append(start).ToArray();
                var cycleNames = cycleProjects.Select(path => projectNames.GetValueOrDefault(path, Path.GetFileNameWithoutExtension(path))).ToArray();
                var key = string.Join("->", cycleNames.OrderBy(x => x, Comparer));

                if (seen.Add(key))
                {
                    cycles.Add(new PossibleCircularProjectDependency(
                        string.Join(" -> ", cycleNames),
                        cycleNames.Distinct(Comparer).ToArray(),
                        "Cycle discovered from static ProjectReference entries.",
                        "Review project boundaries and confirm whether the cycle is intentional or should be broken."));
                }
            }
            else if (!path.Contains(next, Comparer))
            {
                FindCycles(start, next, graph, projectNames, path, cycles, seen);
            }
        }

        path.Pop();
    }

    private static IEnumerable<string> ResolveReferencePaths(
        DiscoveredProject project,
        IReadOnlyDictionary<string, DiscoveredProject> projectLookup)
    {
        var sourceDirectory = Path.GetDirectoryName(project.ProjectFilePath);
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            yield break;
        }

        foreach (var reference in project.ProjectReferences)
        {
            var targetPath = NormalisePath(Path.Combine(sourceDirectory, reference));
            if (projectLookup.ContainsKey(targetPath))
            {
                yield return targetPath;
            }
        }
    }

    private static bool HasLegacyAspNetEvidence(
        DiscoveredProject project,
        IEnumerable<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IEnumerable<DiscoveredConfigFile> configFiles)
    {
        var projectDirectory = ProjectDirectory(project);
        return legacyAspNetArtifacts.Any(artifact => IsSameOrChildPath(artifact.FilePath, projectDirectory)) ||
               configFiles.Any(config => IsSameOrChildPath(config.FilePath, projectDirectory) &&
                                         Path.GetFileName(config.FilePath).Equals("Web.config", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasWcfHostEvidence(
        DiscoveredProject project,
        IEnumerable<WcfEndpoint> wcfEndpoints,
        IEnumerable<DiscoveredConfigFile> configFiles)
    {
        var projectDirectory = ProjectDirectory(project);
        return wcfEndpoints.Any(endpoint => IsSameOrChildPath(endpoint.ConfigFilePath, projectDirectory)) ||
               configFiles.Any(config => IsSameOrChildPath(config.FilePath, projectDirectory) &&
                                         config.CustomSections.Any(section => section.Name.Contains("system.serviceModel", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasWcfContractEvidence(DiscoveredProject project, IEnumerable<WcfServiceContract> contracts)
    {
        var projectDirectory = ProjectDirectory(project);
        return contracts.Any(contract => IsSameOrChildPath(contract.SourceFilePath, projectDirectory));
    }

    private static bool HasDataAccessEvidence(DiscoveredProject project, ScanFileInventory fileInventory)
    {
        var packageNames = project.PackageReferenceDetails.Select(x => x.Name).Concat(project.PackageReferences).ToArray();
        var assemblyNames = project.AssemblyReferences;

        return packageNames.Any(package => NameContains(package, "EntityFramework", "Microsoft.EntityFrameworkCore", "Dapper", "NHibernate", "System.Data.SqlClient", "Microsoft.Data.SqlClient")) ||
               assemblyNames.Any(assembly => NameContains(assembly, "System.Data", "EntityFramework")) ||
               fileInventory.EdmxFiles.Any(file => file.ProjectName.Equals(project.Name, StringComparison.OrdinalIgnoreCase)) ||
               fileInventory.DbmlFiles.Any(file => file.ProjectName.Equals(project.Name, StringComparison.OrdinalIgnoreCase)) ||
               NameContains(project.Name, "Data", "Repository", "Repositories", "Persistence");
    }

    private static bool HasWindowsServiceEvidence(IEnumerable<ScanFile> projectFiles, out ProjectRoleEvidence evidence)
    {
        var serviceFile = projectFiles.FirstOrDefault(file =>
            file.Content.Contains("ServiceBase", StringComparison.OrdinalIgnoreCase) ||
            file.Content.Contains("System.ServiceProcess", StringComparison.OrdinalIgnoreCase));

        if (serviceFile is not null)
        {
            evidence = new ProjectRoleEvidence("Windows service source evidence found: ServiceBase/System.ServiceProcess.", serviceFile.FullPath);
            return true;
        }

        evidence = new ProjectRoleEvidence(string.Empty, string.Empty);
        return false;
    }

    private static bool HasConsoleEvidence(
        DiscoveredProject project,
        ScanFileInventory fileInventory,
        out ProjectRoleEvidence evidence)
    {
        var outputType = TryReadOutputType(project.ProjectFilePath);
        if (outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase) ||
            outputType.Equals("WinExe", StringComparison.OrdinalIgnoreCase))
        {
            evidence = new ProjectRoleEvidence($"Project OutputType is {outputType}.", project.ProjectFilePath);
            return true;
        }

        var programFile = ProjectFiles(fileInventory, project)
            .FirstOrDefault(file => Path.GetFileName(file.FullPath).Equals("Program.cs", StringComparison.OrdinalIgnoreCase));

        if (programFile is not null)
        {
            evidence = new ProjectRoleEvidence("Program.cs discovered.", programFile.FullPath);
            return true;
        }

        var mainMethodFile = ProjectFiles(fileInventory, project)
            .FirstOrDefault(file => Regex.IsMatch(file.Content, @"\bstatic\s+(?:async\s+)?(?:Task|void|int)\s+Main\s*\(", RegexOptions.IgnoreCase));

        if (mainMethodFile is not null)
        {
            evidence = new ProjectRoleEvidence("Main method pattern discovered.", mainMethodFile.FullPath);
            return true;
        }

        evidence = new ProjectRoleEvidence(string.Empty, string.Empty);
        return false;
    }

    private static string TryReadOutputType(string projectFilePath)
    {
        try
        {
            if (!File.Exists(projectFilePath))
            {
                return string.Empty;
            }

            var document = XDocument.Load(projectFilePath);
            return document.Descendants().FirstOrDefault(element => element.Name.LocalName == "OutputType")?.Value.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static IEnumerable<ScanFile> ProjectFiles(ScanFileInventory fileInventory, DiscoveredProject project)
    {
        return fileInventory.CSharpFiles.Where(file => file.ProjectName.Equals(project.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static string ProjectDirectory(DiscoveredProject project)
    {
        return Path.GetDirectoryName(project.ProjectFilePath) ?? project.ProjectFilePath;
    }

    private static void AddProjectFileEvidence(
        ICollection<ProjectRoleEvidence> evidence,
        string message,
        DiscoveredProject project,
        IEnumerable<string> paths)
    {
        var projectDirectory = ProjectDirectory(project);
        var path = paths.FirstOrDefault(candidate => IsSameOrChildPath(candidate, projectDirectory));
        evidence.Add(new ProjectRoleEvidence(message, path ?? project.ProjectFilePath));
    }

    private static bool NameContains(string value, params string[] tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSameOrChildPath(string path, string candidateDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(candidateDirectory))
        {
            return false;
        }

        var fullPath = NormalisePath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var directory = NormalisePath(candidateDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return fullPath.Equals(directory, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               fullPath.StartsWith(directory + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalisePath(string path)
    {
        return Path.GetFullPath(path);
    }

    private static string ToLayer(ProjectTopologyRole role)
    {
        return role switch
        {
            ProjectTopologyRole.WebApplication or ProjectTopologyRole.ApiApplication or ProjectTopologyRole.ConsoleApplication or
                ProjectTopologyRole.WindowsService or ProjectTopologyRole.WcfServiceHost or ProjectTopologyRole.WorkerServiceHost => "Application / Entry Points",
            ProjectTopologyRole.ServiceLibrary => "Services",
            ProjectTopologyRole.DomainCore => "Domain / Core",
            ProjectTopologyRole.DataAccess => "Data Access",
            ProjectTopologyRole.Infrastructure or ProjectTopologyRole.SharedCommon => "Infrastructure / Shared",
            ProjectTopologyRole.Contracts => "Contracts",
            ProjectTopologyRole.Test => "Tests",
            _ => "Unknown"
        };
    }

    private static string ToRoleLabel(ProjectTopologyRole role)
    {
        return role switch
        {
            ProjectTopologyRole.WebApplication => "Web application",
            ProjectTopologyRole.ApiApplication => "API application",
            ProjectTopologyRole.ConsoleApplication => "Console application",
            ProjectTopologyRole.WindowsService => "Windows service",
            ProjectTopologyRole.WcfServiceHost => "WCF service host",
            ProjectTopologyRole.WorkerServiceHost => "Worker/service host",
            ProjectTopologyRole.ServiceLibrary => "Service library",
            ProjectTopologyRole.DomainCore => "Domain/Core",
            ProjectTopologyRole.DataAccess => "Data access",
            ProjectTopologyRole.Infrastructure => "Infrastructure",
            ProjectTopologyRole.SharedCommon => "Shared/Common",
            ProjectTopologyRole.Contracts => "Contracts",
            ProjectTopologyRole.Test => "Test",
            _ => "Unknown"
        };
    }

    private sealed record EntryPointInference(
        bool IsEntryPoint,
        string? EntryPointType,
        TopologyConfidence Confidence,
        IReadOnlyList<ProjectRoleEvidence> Evidence);

    private sealed record TestProjectInference(
        bool IsTestProject,
        TopologyConfidence Confidence,
        IReadOnlyList<ProjectRoleEvidence> Evidence);
}
