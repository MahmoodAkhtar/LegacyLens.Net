using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Analysis;

public sealed class ModernisationHintAnalyzer
{
    public IReadOnlyList<ModernisationHint> Analyze(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(wcfEndpoints);
        ArgumentNullException.ThrowIfNull(wcfServiceContracts);

        var hints = new List<ModernisationHint>();

        AddTargetFrameworkHints(projects, hints);
        AddProjectCouplingHints(projects, hints);
        AddPackageHints(projects, hints);
        AddWcfHints(wcfEndpoints, wcfServiceContracts, hints);

        return hints;
    }

    private static void AddTargetFrameworkHints(
        IReadOnlyList<DiscoveredProject> projects,
        List<ModernisationHint> hints)
    {
        foreach (var project in projects)
        {
            if (string.IsNullOrWhiteSpace(project.TargetFramework))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "Target Framework",
                    Finding = $"{project.Name} does not declare a target framework",
                    Reason = "Missing target framework information makes migration assessment harder."
                });

                continue;
            }

            if (project.TargetFramework.StartsWith("net4", StringComparison.OrdinalIgnoreCase))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Risk,
                    Area = "Target Framework",
                    Finding = $"{project.Name} targets {project.TargetFramework}",
                    Reason = ".NET Framework projects usually need extra assessment before migration to modern .NET."
                });
            }
        }
    }

    private static void AddProjectCouplingHints(
        IReadOnlyList<DiscoveredProject> projects,
        List<ModernisationHint> hints)
    {
        foreach (var project in projects.Where(x => x.ProjectReferences.Count >= 3))
        {
            hints.Add(new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Warning,
                Area = "Project Dependencies",
                Finding = $"{project.Name} references {project.ProjectReferences.Count} projects",
                Reason = "Projects with several direct dependencies may be harder to refactor or migrate independently."
            });
        }
    }

    private static void AddPackageHints(
        IReadOnlyList<DiscoveredProject> projects,
        List<ModernisationHint> hints)
    {
        foreach (var project in projects)
        {
            foreach (var package in project.PackageReferences)
            {
                if (package.StartsWith("System.ServiceModel", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Risk,
                        Area = "Packages",
                        Finding = $"{project.Name} references {package}",
                        Reason = "System.ServiceModel packages indicate WCF-related usage, which is important for modernisation planning."
                    });
                }

                if (package.Equals("EntityFramework", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Packages",
                        Finding = $"{project.Name} references EntityFramework",
                        Reason = "Classic Entity Framework may require assessment before migration to EF Core or modern .NET."
                    });
                }

                if (package.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Packages",
                        Finding = $"{project.Name} references Newtonsoft.Json",
                        Reason = "This is common in legacy and modern projects, but may be reviewed during modernisation."
                    });
                }
            }
        }
    }

    private static void AddWcfHints(
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        List<ModernisationHint> hints)
    {
        if (wcfEndpoints.Count > 0)
        {
            hints.Add(new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Risk,
                Area = "WCF",
                Finding = $"{wcfEndpoints.Count} WCF endpoint(s) discovered",
                Reason = "Configured WCF endpoints usually represent service boundaries or integration points that need migration assessment."
            });
        }

        if (wcfServiceContracts.Count > 0)
        {
            hints.Add(new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Risk,
                Area = "WCF",
                Finding = $"{wcfServiceContracts.Count} WCF service contract(s) discovered",
                Reason = "WCF service contracts identify service APIs that may need redesign, replacement, or compatibility planning."
            });
        }
    }
}