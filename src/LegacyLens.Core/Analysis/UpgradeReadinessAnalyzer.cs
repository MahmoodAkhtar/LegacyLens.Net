using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Analysis;

public sealed class UpgradeReadinessAnalyzer
{
    public UpgradeReadinessReport Analyze(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<DiscoveredConfigFile> configFiles,
        IReadOnlyList<ModernisationHint> modernisationHints,
        string? requestedUpgradeTarget)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(wcfEndpoints);
        ArgumentNullException.ThrowIfNull(wcfServiceContracts);
        ArgumentNullException.ThrowIfNull(wcfBehaviours);
        ArgumentNullException.ThrowIfNull(legacyAspNetArtifacts);
        ArgumentNullException.ThrowIfNull(configFiles);
        ArgumentNullException.ThrowIfNull(modernisationHints);

        var packageReviewer = new PackageCompatibilityReviewer();
        var packageReviews = packageReviewer.Review(projects);

        return new UpgradeReadinessReport
        {
            RequestedUpgradeTarget = requestedUpgradeTarget,
            Overview = BuildOverview(projects, wcfEndpoints, wcfServiceContracts, wcfBehaviours, legacyAspNetArtifacts, configFiles),
            ProjectReadiness = BuildProjectReadiness(projects, wcfEndpoints, wcfServiceContracts, wcfBehaviours, legacyAspNetArtifacts, configFiles),
            Concerns = BuildConcerns(projects, wcfEndpoints, wcfServiceContracts, legacyAspNetArtifacts, configFiles),
            PackageConsiderations = packageReviews.Select(x => new PackageUpgradeConsideration
            {
                ProjectName = x.ProjectName,
                PackageName = x.PackageName,
                Version = x.Version,
                ProjectTargetFramework = x.ProjectTargetFramework,
                PackageTargetFramework = x.PackageTargetFramework,
                SourceFormat = x.SourceFormat,
                SourcePath = x.SourcePath,
                PossibleConcern = x.Concern
            }).ToList(),
            AssemblyConsiderations = BuildAssemblyConsiderations(projects),
            ConfigurationRuntimeConsiderations = BuildConfigurationRuntimeConsiderations(
                wcfEndpoints,
                wcfServiceContracts,
                wcfBehaviours,
                legacyAspNetArtifacts,
                configFiles)
        };
    }

    private static IReadOnlyList<UpgradeReadinessOverviewItem> BuildOverview(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        var items = new List<UpgradeReadinessOverviewItem>();

        if (projects.Any(x => IsDotNetFramework(x.TargetFramework)))
        {
            items.Add(new UpgradeReadinessOverviewItem
            {
                Area = "Target frameworks",
                Status = "Requires review",
                Evidence = ".NET Framework projects detected"
            });
        }

        if (projects.SelectMany(x => x.PackageReferenceDetails).Any(x => x.SourceFormat.Equals("packages.config", StringComparison.OrdinalIgnoreCase)))
        {
            items.Add(new UpgradeReadinessOverviewItem
            {
                Area = "Package management",
                Status = "Requires review",
                Evidence = "packages.config detected"
            });
        }

        if (legacyAspNetArtifacts.Count > 0 || projects.Any(HasSystemWebReference))
        {
            items.Add(new UpgradeReadinessOverviewItem
            {
                Area = "Legacy ASP.NET",
                Status = "Possible blocker",
                Evidence = "System.Web or legacy ASP.NET artifacts detected"
            });
        }

        if (wcfEndpoints.Count > 0 || wcfServiceContracts.Count > 0 || wcfBehaviours.Count > 0 || projects.Any(HasWcfReference))
        {
            items.Add(new UpgradeReadinessOverviewItem
            {
                Area = "WCF",
                Status = "Requires review",
                Evidence = "System.ServiceModel or WCF configuration evidence detected"
            });
        }

        if (projects.SelectMany(x => x.PackageReferenceDetails).Any(x => x.Name.Equals("EntityFramework", StringComparison.OrdinalIgnoreCase)))
        {
            items.Add(new UpgradeReadinessOverviewItem
            {
                Area = "Data access",
                Status = "Requires review",
                Evidence = "EntityFramework package detected"
            });
        }

        if (projects.Any(x => x.AssemblyReferences.Count > 0))
        {
            items.Add(new UpgradeReadinessOverviewItem
            {
                Area = "Direct assemblies",
                Status = "Requires review",
                Evidence = "Direct assembly references detected"
            });
        }

        if (configFiles.Count > 0)
        {
            items.Add(new UpgradeReadinessOverviewItem
            {
                Area = "Configuration",
                Status = "Requires review",
                Evidence = "app.config or web.config detected"
            });
        }

        return items.Count == 0
            ? new[]
            {
                new UpgradeReadinessOverviewItem
                {
                    Area = "Static evidence",
                    Status = "No major concern detected",
                    Evidence = "No MVP upgrade-readiness concern matched the current static rules"
                }
            }
            : items;
    }

    private static IReadOnlyList<ProjectUpgradeReadiness> BuildProjectReadiness(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        return projects
            .Select(project =>
            {
                var hasLegacyWeb = HasSystemWebReference(project) || HasLegacyAspNetEvidence(project, legacyAspNetArtifacts, configFiles);
                var hasWcf = HasWcfReference(project) || wcfEndpoints.Count > 0 || wcfServiceContracts.Count > 0 || wcfBehaviours.Count > 0;
                var hasEf6 = project.PackageReferenceDetails.Any(x => x.Name.Equals("EntityFramework", StringComparison.OrdinalIgnoreCase));
                var usesPackagesConfig = project.PackageReferenceDetails.Any(x => x.SourceFormat.Equals("packages.config", StringComparison.OrdinalIgnoreCase));
                var targetsNetFramework = IsDotNetFramework(project.TargetFramework);

                if (string.IsNullOrWhiteSpace(project.TargetFramework))
                {
                    return CreateProject(project, UpgradeReadinessLevel.Unknown, "No target framework was discovered, so static upgrade readiness cannot be classified confidently.");
                }

                if (hasLegacyWeb)
                {
                    return CreateProject(project, UpgradeReadinessLevel.HigherRiskReviewFirst, "Legacy ASP.NET, System.Web, or web runtime evidence detected. Review before attempting a modern .NET upgrade.");
                }

                if (hasWcf || hasEf6 || usesPackagesConfig || project.AssemblyReferences.Count > 0)
                {
                    return CreateProject(project, UpgradeReadinessLevel.ModerateReviewRequired, "Static evidence found package, WCF, EF6, packages.config, or direct assembly considerations.");
                }

                if (targetsNetFramework)
                {
                    return CreateProject(project, UpgradeReadinessLevel.ModerateReviewRequired, "Project targets .NET Framework and requires review before moving to modern .NET.");
                }

                return CreateProject(project, UpgradeReadinessLevel.LowerRiskCandidate, "No major MVP upgrade-readiness concern matched this project from static evidence.");
            })
            .OrderByDescending(x => GetReadinessRank(x.Readiness))
            .ThenBy(x => x.ProjectName)
            .ToList();
    }

    private static ProjectUpgradeReadiness CreateProject(
        DiscoveredProject project,
        UpgradeReadinessLevel readiness,
        string reason)
    {
        return new ProjectUpgradeReadiness
        {
            ProjectName = project.Name,
            CurrentTargetFramework = project.TargetFramework,
            ProjectFilePath = project.ProjectFilePath,
            Readiness = readiness,
            Reason = reason
        };
    }

    private static IReadOnlyList<UpgradeConcern> BuildConcerns(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        var concerns = new List<UpgradeConcern>();

        if (projects.Any(x => IsDotNetFramework(x.TargetFramework)))
        {
            concerns.Add(new UpgradeConcern
            {
                Concern = ".NET Framework target framework",
                Evidence = string.Join(", ", projects.Where(x => IsDotNetFramework(x.TargetFramework)).Select(x => $"{x.Name} ({x.TargetFramework})")),
                WhyItMatters = "Requires review before moving to modern .NET."
            });
        }

        if (legacyAspNetArtifacts.Count > 0 || projects.Any(HasSystemWebReference))
        {
            concerns.Add(new UpgradeConcern
            {
                Concern = "Legacy ASP.NET runtime",
                Evidence = "System.Web or legacy ASP.NET artifact evidence found",
                WhyItMatters = "ASP.NET Core does not use the System.Web request pipeline."
            });
        }

        if (wcfEndpoints.Count > 0 || wcfServiceContracts.Count > 0 || projects.Any(HasWcfReference))
        {
            concerns.Add(new UpgradeConcern
            {
                Concern = "WCF usage",
                Evidence = $"{wcfEndpoints.Count} endpoint(s), {wcfServiceContracts.Count} service contract(s), and System.ServiceModel evidence where present",
                WhyItMatters = "WCF service boundaries, bindings, metadata, and clients need migration decisions."
            });
        }

        if (configFiles.Any(x => x.ConnectionStringsCount > 0))
        {
            concerns.Add(new UpgradeConcern
            {
                Concern = "Database/runtime dependencies",
                Evidence = "Connection strings detected",
                WhyItMatters = "External data dependencies should be reviewed during migration planning."
            });
        }

        if (configFiles.Any(x => x.CustomSectionCount > 0))
        {
            concerns.Add(new UpgradeConcern
            {
                Concern = "Custom configuration sections",
                Evidence = "Custom configSections detected",
                WhyItMatters = "Runtime configuration may need replacement or migration."
            });
        }

        return concerns;
    }

    private static IReadOnlyList<AssemblyUpgradeConsideration> BuildAssemblyConsiderations(
        IReadOnlyList<DiscoveredProject> projects)
    {
        return projects
            .SelectMany(project => project.AssemblyReferences.Select(assembly =>
                new AssemblyUpgradeConsideration
                {
                    ProjectName = project.Name,
                    AssemblyName = assembly,
                    ProjectFilePath = project.ProjectFilePath,
                    PossibleConcern = BuildAssemblyConcern(assembly)
                }))
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.AssemblyName)
            .ToList();
    }

    private static IReadOnlyList<ConfigurationRuntimeConsideration> BuildConfigurationRuntimeConsiderations(
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        var items = new List<ConfigurationRuntimeConsideration>();

        items.AddRange(configFiles.Select(x => new ConfigurationRuntimeConsideration
        {
            Source = x.FilePath,
            Finding = $"appSettings: {x.AppSettingsCount}, connection strings: {x.ConnectionStringsCount}, custom sections: {x.CustomSectionCount}",
            PossibleConcern = "Configuration values may represent runtime behaviour or external dependencies that need migration review."
        }));

        if (wcfEndpoints.Count > 0 || wcfServiceContracts.Count > 0 || wcfBehaviours.Count > 0)
        {
            items.Add(new ConfigurationRuntimeConsideration
            {
                Source = "WCF discovery",
                Finding = $"{wcfEndpoints.Count} endpoint(s), {wcfServiceContracts.Count} service contract(s), {wcfBehaviours.Count} behaviour(s)",
                PossibleConcern = "WCF runtime configuration and service boundaries may need migration or compatibility planning."
            });
        }

        if (legacyAspNetArtifacts.Count > 0)
        {
            items.Add(new ConfigurationRuntimeConsideration
            {
                Source = "Legacy ASP.NET discovery",
                Finding = $"{legacyAspNetArtifacts.Count} legacy ASP.NET artifact(s)",
                PossibleConcern = "Classic ASP.NET runtime, routing, startup, or request pipeline behaviour may need migration work."
            });
        }

        return items;
    }

    private static bool HasLegacyAspNetEvidence(
        DiscoveredProject project,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> artifacts,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);

        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return false;
        }

        return artifacts.Any(x => IsUnderDirectory(x.FilePath, projectDirectory)) ||
               configFiles.Any(x =>
                   IsUnderDirectory(x.FilePath, projectDirectory) &&
                   Path.GetFileName(x.FilePath).Equals("web.config", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsUnderDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory);

        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSystemWebReference(DiscoveredProject project)
    {
        return project.AssemblyReferences.Any(x =>
            x.Equals("System.Web", StringComparison.OrdinalIgnoreCase) ||
            x.StartsWith("System.Web.", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasWcfReference(DiscoveredProject project)
    {
        return project.AssemblyReferences.Any(x =>
                   x.Equals("System.ServiceModel", StringComparison.OrdinalIgnoreCase)) ||
               project.PackageReferenceDetails.Any(x =>
                   x.Name.StartsWith("System.ServiceModel", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDotNetFramework(string? targetFramework)
    {
        return SplitTargetFrameworks(targetFramework)
            .Any(x => x.StartsWith("net4", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> SplitTargetFrameworks(string? targetFramework)
    {
        return string.IsNullOrWhiteSpace(targetFramework)
            ? Array.Empty<string>()
            : targetFramework.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string BuildAssemblyConcern(string assembly)
    {
        if (assembly.Equals("System.Web", StringComparison.OrdinalIgnoreCase) ||
            assembly.StartsWith("System.Web.", StringComparison.OrdinalIgnoreCase))
        {
            return "Legacy ASP.NET assembly reference. ASP.NET Core does not use the System.Web request pipeline.";
        }

        if (assembly.Equals("System.ServiceModel", StringComparison.OrdinalIgnoreCase))
        {
            return "WCF assembly reference. WCF migration or compatibility strategy requires review.";
        }

        return "Direct assembly reference. Local framework or vendor DLL compatibility may need review.";
    }

    private static int GetReadinessRank(UpgradeReadinessLevel readiness)
    {
        return readiness switch
        {
            UpgradeReadinessLevel.HigherRiskReviewFirst => 4,
            UpgradeReadinessLevel.ModerateReviewRequired => 3,
            UpgradeReadinessLevel.Unknown => 2,
            UpgradeReadinessLevel.LowerRiskCandidate => 1,
            _ => 0
        };
    }
}