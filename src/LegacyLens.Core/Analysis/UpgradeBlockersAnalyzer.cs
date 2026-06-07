using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Analysis;

public sealed class UpgradeBlockersAnalyzer
{
    public UpgradeBlockersReport Analyze(
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

        var blockers = new List<UpgradeBlocker>();

        AddLegacyAspNetBlocker(projects, legacyAspNetArtifacts, blockers);
        AddWcfBlocker(projects, wcfEndpoints, wcfServiceContracts, wcfBehaviours, blockers);
        AddDataAccessBlocker(projects, modernisationHints, blockers);
        AddPackageManagementBlocker(projects, blockers);
        AddDirectAssemblyReferenceBlocker(projects, blockers);
        AddConfigurationRuntimeBlocker(projects, configFiles, blockers);

        if (blockers.Count == 0)
        {
            blockers.Add(new UpgradeBlocker
            {
                Priority = 1,
                Category = UpgradeBlockerCategory.UnknownRequiresManualReview,
                Impact = UpgradeBlockerImpact.Unknown,
                Title = "No visible MVP upgrade blocker matched the current static rules",
                WhyItMatters = "Static discovery did not find one of the known blocker categories, but this is not a compatibility guarantee.",
                DecisionsRequired = new[]
                {
                    "Review the codebase manually before making upgrade decisions.",
                    "Build and test the solution outside LegacyLens.NET before migration work begins."
                },
                Evidence = new[]
                {
                    new UpgradeBlockerEvidence
                    {
                        Source = "Static analysis summary",
                        Finding = "No configured MVP blocker rule matched the discovered evidence."
                    }
                }
            });
        }

        return new UpgradeBlockersReport
        {
            RequestedUpgradeTarget = requestedUpgradeTarget,
            Blockers = blockers
                .OrderBy(x => GetCategoryRank(x.Category))
                .ThenByDescending(x => GetImpactRank(x.Impact))
                .Select((blocker, index) => new UpgradeBlocker
                {
                    Priority = index + 1,
                    Category = blocker.Category,
                    Impact = blocker.Impact,
                    Title = blocker.Title,
                    WhyItMatters = blocker.WhyItMatters,
                    DecisionsRequired = blocker.DecisionsRequired,
                    Evidence = blocker.Evidence
                })
                .ToList()
        };
    }

    private static void AddLegacyAspNetBlocker(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        List<UpgradeBlocker> blockers)
    {
        var evidence = new List<UpgradeBlockerEvidence>();

        foreach (var project in projects)
        {
            foreach (var assembly in project.AssemblyReferences.Where(IsSystemWebReference))
            {
                evidence.Add(new UpgradeBlockerEvidence
                {
                    ProjectName = project.Name,
                    Source = project.ProjectFilePath,
                    Finding = $"Possible blocker: {assembly} assembly reference indicates classic ASP.NET / System.Web usage."
                });
            }
        }

        foreach (var artifact in legacyAspNetArtifacts)
        {
            evidence.Add(new UpgradeBlockerEvidence
            {
                ProjectName = FindProjectName(projects, artifact.FilePath),
                Source = artifact.FilePath,
                Finding = $"Possible blocker: {artifact.Kind} {ValueOrFileName(artifact.Name, artifact.FilePath)} requires migration review."
            });
        }

        if (evidence.Count == 0)
        {
            return;
        }

        blockers.Add(new UpgradeBlocker
        {
            Priority = 0,
            Category = UpgradeBlockerCategory.LegacyAspNetSystemWeb,
            Impact = UpgradeBlockerImpact.High,
            Title = "Migration decision required for classic ASP.NET / System.Web usage",
            WhyItMatters = "ASP.NET Core uses a different hosting model and request pipeline. System.Web, WebForms, ASMX, ASHX, Global.asax, HTTP modules, HTTP handlers, MVC 5, and Web API 2 evidence may require redesign, replacement, or staged migration.",
            DecisionsRequired = new[]
            {
                "Can the existing web host remain temporarily on .NET Framework?",
                "Which endpoints or pages should move to ASP.NET Core first?",
                "Do WebForms, ASMX, ASHX, HTTP module, or HTTP handler artifacts need replacement?"
            },
            Evidence = evidence
        });
    }

    private static void AddWcfBlocker(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
        List<UpgradeBlocker> blockers)
    {
        var evidence = new List<UpgradeBlockerEvidence>();

        foreach (var project in projects)
        {
            foreach (var assembly in project.AssemblyReferences.Where(IsServiceModelReference))
            {
                evidence.Add(new UpgradeBlockerEvidence
                {
                    ProjectName = project.Name,
                    Source = project.ProjectFilePath,
                    Finding = $"Possible blocker: {assembly} assembly reference indicates WCF / ServiceModel usage."
                });
            }

            foreach (var package in project.PackageReferenceDetails.Where(x => x.Name.StartsWith("System.ServiceModel", StringComparison.OrdinalIgnoreCase)))
            {
                evidence.Add(new UpgradeBlockerEvidence
                {
                    ProjectName = project.Name,
                    Source = package.SourcePath,
                    Finding = $"Possible blocker: WCF-related package {package.Name} {ValueOrUnknown(package.Version)} requires migration review."
                });
            }
        }

        foreach (var endpoint in wcfEndpoints)
        {
            evidence.Add(new UpgradeBlockerEvidence
            {
                ProjectName = FindProjectName(projects, endpoint.ConfigFilePath),
                Source = endpoint.ConfigFilePath,
                Finding = $"Possible blocker: WCF endpoint {ValueOrUnknown(endpoint.ServiceName)} using binding {ValueOrUnknown(endpoint.Binding)} requires service boundary and binding review."
            });
        }

        foreach (var contract in wcfServiceContracts)
        {
            evidence.Add(new UpgradeBlockerEvidence
            {
                ProjectName = FindProjectName(projects, contract.SourceFilePath),
                Source = contract.SourceFilePath,
                Finding = $"Possible blocker: WCF service contract {contract.Name} requires API migration or compatibility review."
            });
        }

        foreach (var behaviour in wcfBehaviours)
        {
            evidence.Add(new UpgradeBlockerEvidence
            {
                ProjectName = FindProjectName(projects, behaviour.ConfigFilePath),
                Source = behaviour.ConfigFilePath,
                Finding = $"Possible blocker: WCF {behaviour.Kind} {ValueOrUnknown(behaviour.Name)} requires runtime behaviour review."
            });
        }

        if (evidence.Count == 0)
        {
            return;
        }

        blockers.Add(new UpgradeBlocker
        {
            Priority = 0,
            Category = UpgradeBlockerCategory.WcfServiceModel,
            Impact = UpgradeBlockerImpact.High,
            Title = "Migration decision required for WCF / ServiceModel usage",
            WhyItMatters = "WCF hosting, bindings, metadata, security, behaviours, and generated clients may need replacement, compatibility planning, or isolation before moving to modern .NET.",
            DecisionsRequired = new[]
            {
                "Should WCF services remain on .NET Framework temporarily?",
                "Should service boundaries move to ASP.NET Core APIs, gRPC, queues, or another integration style?",
                "Are SOAP clients, metadata exchange endpoints, bindings, security, and timeouts externally depended on?"
            },
            Evidence = evidence
        });
    }

    private static void AddDataAccessBlocker(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<ModernisationHint> modernisationHints,
        List<UpgradeBlocker> blockers)
    {
        var evidence = new List<UpgradeBlockerEvidence>();

        foreach (var project in projects)
        {
            foreach (var package in project.PackageReferenceDetails.Where(x => x.Name.Equals("EntityFramework", StringComparison.OrdinalIgnoreCase)))
            {
                evidence.Add(new UpgradeBlockerEvidence
                {
                    ProjectName = project.Name,
                    Source = package.SourcePath,
                    Finding = $"Possible blocker: EntityFramework {ValueOrUnknown(package.Version)} requires EF6, EF Core, or isolation decision."
                });
            }
        }

        foreach (var hint in modernisationHints.Where(x =>
                     x.Finding.Contains(".edmx", StringComparison.OrdinalIgnoreCase) ||
                     x.Finding.Contains("ObjectContext", StringComparison.OrdinalIgnoreCase) ||
                     x.Finding.Contains("DbContext", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add(new UpgradeBlockerEvidence
            {
                Source = hint.EvidencePath ?? "Modernisation hint",
                Finding = $"Possible blocker: {hint.Finding}"
            });
        }

        if (evidence.Count == 0)
        {
            return;
        }

        blockers.Add(new UpgradeBlocker
        {
            Priority = 0,
            Category = UpgradeBlockerCategory.Ef6EdmxDataAccess,
            Impact = UpgradeBlockerImpact.Medium,
            Title = "Data access migration or isolation decision required",
            WhyItMatters = "Classic Entity Framework, EDMX, ObjectContext, or legacy DbContext usage may affect whether data access can move directly to modern .NET or needs a staged approach.",
            DecisionsRequired = new[]
            {
                "Can EF6 remain temporarily while other projects move?",
                "Is EF Core migration required for the target architecture?",
                "Are EDMX or generated model files present and still maintained?"
            },
            Evidence = evidence
        });
    }

    private static void AddPackageManagementBlocker(
        IReadOnlyList<DiscoveredProject> projects,
        List<UpgradeBlocker> blockers)
    {
        var evidence = new List<UpgradeBlockerEvidence>();

        foreach (var project in projects)
        {
            foreach (var package in project.PackageReferenceDetails)
            {
                if (package.SourceFormat.Equals("packages.config", StringComparison.OrdinalIgnoreCase))
                {
                    evidence.Add(new UpgradeBlockerEvidence
                    {
                        ProjectName = project.Name,
                        Source = package.SourcePath,
                        Finding = $"Possible blocker: {package.Name} uses legacy packages.config package management."
                    });
                }

                if (string.IsNullOrWhiteSpace(package.Version))
                {
                    evidence.Add(new UpgradeBlockerEvidence
                    {
                        ProjectName = project.Name,
                        Source = package.SourcePath,
                        Finding = $"Requires review: {package.Name} package version was not discovered."
                    });
                }

                if (PackageTargetFrameworkDiffersFromProject(project.TargetFramework, package.PackageTargetFramework))
                {
                    evidence.Add(new UpgradeBlockerEvidence
                    {
                        ProjectName = project.Name,
                        Source = package.SourcePath,
                        Finding = $"Requires review: {package.Name} package target framework {package.PackageTargetFramework} differs from project target framework {project.TargetFramework}."
                    });
                }
            }
        }

        if (evidence.Count == 0)
        {
            return;
        }

        blockers.Add(new UpgradeBlocker
        {
            Priority = 0,
            Category = UpgradeBlockerCategory.PackageManagement,
            Impact = UpgradeBlockerImpact.Medium,
            Title = "Package management migration decision required",
            WhyItMatters = "Legacy packages.config usage, missing versions, or mismatched package target framework metadata can complicate package restore, upgrade sequencing, and dependency review.",
            DecisionsRequired = new[]
            {
                "Should packages.config projects be migrated to PackageReference?",
                "Are package versions centrally managed elsewhere?",
                "Do package target framework values reflect the current project targets?"
            },
            Evidence = evidence
        });
    }

    private static void AddDirectAssemblyReferenceBlocker(
        IReadOnlyList<DiscoveredProject> projects,
        List<UpgradeBlocker> blockers)
    {
        var evidence = projects
            .SelectMany(project => project.AssemblyReferences
                .Where(x => !IsSystemWebReference(x) && !IsServiceModelReference(x))
                .Select(assembly => new UpgradeBlockerEvidence
                {
                    ProjectName = project.Name,
                    Source = project.ProjectFilePath,
                    Finding = $"Requires review: direct assembly reference {assembly} may need compatibility or package replacement review."
                }))
            .ToList();

        if (evidence.Count == 0)
        {
            return;
        }

        blockers.Add(new UpgradeBlocker
        {
            Priority = 0,
            Category = UpgradeBlockerCategory.DirectAssemblyReferences,
            Impact = UpgradeBlockerImpact.Medium,
            Title = "Direct assembly references require compatibility review",
            WhyItMatters = "Direct assembly references may indicate framework assemblies, local DLLs, or vendor dependencies that need replacement, package migration, or compatibility validation.",
            DecisionsRequired = new[]
            {
                "Can each assembly reference be replaced by a NuGet package?",
                "Are any local or vendor DLLs required at runtime?",
                "Does each referenced assembly have a modern .NET-compatible equivalent?"
            },
            Evidence = evidence
        });
    }

    private static void AddConfigurationRuntimeBlocker(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<DiscoveredConfigFile> configFiles,
        List<UpgradeBlocker> blockers)
    {
        var evidence = new List<UpgradeBlockerEvidence>();

        foreach (var configFile in configFiles)
        {
            evidence.Add(new UpgradeBlockerEvidence
            {
                ProjectName = FindProjectName(projects, configFile.FilePath),
                Source = configFile.FilePath,
                Finding = $"Requires review: configuration file contains {configFile.AppSettingsCount} appSettings, {configFile.ConnectionStringsCount} connection strings, and {configFile.CustomSectionCount} custom sections."
            });
        }

        if (evidence.Count == 0)
        {
            return;
        }

        blockers.Add(new UpgradeBlocker
        {
            Priority = 0,
            Category = UpgradeBlockerCategory.ConfigurationRuntimeCoupling,
            Impact = UpgradeBlockerImpact.Medium,
            Title = "Configuration and runtime coupling requires migration review",
            WhyItMatters = "app.config and web.config can contain runtime behaviour, environment-specific settings, connection strings, custom sections, WCF configuration, and ASP.NET pipeline configuration that may need explicit replacement.",
            DecisionsRequired = new[]
            {
                "Which settings move to appsettings.json, environment variables, or secret stores?",
                "Which connection strings and external dependencies are required for migration testing?",
                "Do custom configuration sections need replacement code?"
            },
            Evidence = evidence
        });
    }

    private static bool IsSystemWebReference(string value) =>
        value.Equals("System.Web", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("System.Web.", StringComparison.OrdinalIgnoreCase);

    private static bool IsServiceModelReference(string value) =>
        value.Equals("System.ServiceModel", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("System.ServiceModel.", StringComparison.OrdinalIgnoreCase);

    private static string? FindProjectName(
        IReadOnlyList<DiscoveredProject> projects,
        string sourcePath)
    {
        var fullSourcePath = Path.GetFullPath(sourcePath);

        return projects
            .Select(project => new
            {
                Project = project,
                Directory = Path.GetDirectoryName(Path.GetFullPath(project.ProjectFilePath))
            })
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Directory) &&
                fullSourcePath.StartsWith(x.Directory, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Directory!.Length)
            .Select(x => x.Project.Name)
            .FirstOrDefault();
    }

    private static string ValueOrUnknown(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();

    private static string ValueOrFileName(string? value, string filePath) =>
        string.IsNullOrWhiteSpace(value) ? Path.GetFileName(filePath) : value.Trim();

    private static bool PackageTargetFrameworkDiffersFromProject(
        string? projectTargetFramework,
        string? packageTargetFramework)
    {
        if (string.IsNullOrWhiteSpace(projectTargetFramework) ||
            string.IsNullOrWhiteSpace(packageTargetFramework))
        {
            return false;
        }

        return projectTargetFramework
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(x => !x.Equals(packageTargetFramework.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static int GetCategoryRank(UpgradeBlockerCategory category)
    {
        return category switch
        {
            UpgradeBlockerCategory.LegacyAspNetSystemWeb => 10,
            UpgradeBlockerCategory.WcfServiceModel => 20,
            UpgradeBlockerCategory.Ef6EdmxDataAccess => 30,
            UpgradeBlockerCategory.PackageManagement => 40,
            UpgradeBlockerCategory.DirectAssemblyReferences => 50,
            UpgradeBlockerCategory.ConfigurationRuntimeCoupling => 60,
            UpgradeBlockerCategory.WindowsOnlyPlatformSpecificApis => 70,
            UpgradeBlockerCategory.CustomBuildMsBuildBehaviour => 80,
            UpgradeBlockerCategory.UnknownRequiresManualReview => 90,
            _ => 100
        };
    }

    private static int GetImpactRank(UpgradeBlockerImpact impact)
    {
        return impact switch
        {
            UpgradeBlockerImpact.High => 4,
            UpgradeBlockerImpact.Medium => 3,
            UpgradeBlockerImpact.Low => 2,
            UpgradeBlockerImpact.Unknown => 1,
            _ => 0
        };
    }
}