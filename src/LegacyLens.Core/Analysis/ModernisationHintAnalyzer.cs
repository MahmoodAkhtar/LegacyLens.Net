using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Analysis;

public sealed class ModernisationHintAnalyzer
{
    public IReadOnlyList<ModernisationHint> Analyze(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(wcfEndpoints);
        ArgumentNullException.ThrowIfNull(wcfServiceContracts);
        ArgumentNullException.ThrowIfNull(configFiles);

        var hints = new List<ModernisationHint>();

        AddTargetFrameworkHints(projects, hints);
        AddProjectCouplingHints(projects, hints);
        AddPackageHints(projects, hints);
        AddWcfHints(wcfEndpoints, wcfServiceContracts, hints);
        AddAssemblyReferenceHints(projects, hints);
        AddConfigHints(configFiles, hints);

        return hints;
    }
    
    private static void AddConfigHints(
        IReadOnlyList<DiscoveredConfigFile> configFiles,
        List<ModernisationHint> hints)
    {
        foreach (var configFile in configFiles)
        {
            if (configFile.AppSettingsCount >= 10)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "Configuration",
                    Finding = $"{Path.GetFileName(configFile.FilePath)} contains {configFile.AppSettingsCount} appSettings entries",
                    Reason = "A large number of appSettings entries may indicate environment-specific behaviour or operational settings hidden in configuration."
                });
            }

            if (configFile.ConnectionStringsCount > 0)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Info,
                    Area = "Configuration",
                    Finding = $"{Path.GetFileName(configFile.FilePath)} contains {configFile.ConnectionStringsCount} connection string(s)",
                    Reason = "Connection strings identify external data dependencies that should be reviewed during migration planning."
                });
            }

            if (configFile.CustomSectionCount > 0)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "Configuration",
                    Finding = $"{Path.GetFileName(configFile.FilePath)} contains {configFile.CustomSectionCount} custom configuration section(s)",
                    Reason = "Custom configuration sections may indicate framework-specific or application-specific behaviour that needs migration assessment."
                });
            }
        }
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
                        Reason =
                            "System.ServiceModel packages indicate WCF-related usage, which is important for modernisation planning."
                    });
                }

                if (package.Equals("EntityFramework", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Packages",
                        Finding = $"{project.Name} references EntityFramework",
                        Reason =
                            "Classic Entity Framework may require assessment before migration to EF Core or modern .NET."
                    });
                }

                if (package.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Packages",
                        Finding = $"{project.Name} references Newtonsoft.Json",
                        Reason =
                            "This is common in legacy and modern projects, but may be reviewed during modernisation."
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
                Reason =
                    "Configured WCF endpoints usually represent service boundaries or integration points that need migration assessment."
            });

            AddWcfBindingHints(wcfEndpoints, hints);
        }

        if (wcfServiceContracts.Count > 0)
        {
            hints.Add(new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Risk,
                Area = "WCF",
                Finding = $"{wcfServiceContracts.Count} WCF service contract(s) discovered",
                Reason =
                    "WCF service contracts identify service APIs that may need redesign, replacement, or compatibility planning."
            });
        }
    }

    private static void AddWcfBindingHints(
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        List<ModernisationHint> hints)
    {
        foreach (var endpoint in wcfEndpoints)
        {
            var serviceName = string.IsNullOrWhiteSpace(endpoint.ServiceName)
                ? "Unknown service"
                : endpoint.ServiceName;

            if (string.IsNullOrWhiteSpace(endpoint.Binding))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "WCF Binding",
                    Finding = $"{serviceName} has a WCF endpoint without a binding",
                    Reason = "Missing WCF binding information makes endpoint migration assessment harder."
                });

                continue;
            }

            if (endpoint.Binding.Equals("basicHttpBinding", StringComparison.OrdinalIgnoreCase))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "WCF Binding",
                    Finding = $"basicHttpBinding endpoint discovered for {serviceName}",
                    Reason =
                        "basicHttpBinding commonly indicates SOAP interoperability that may need replacement or compatibility planning."
                });

                continue;
            }

            if (endpoint.Binding.Equals("netTcpBinding", StringComparison.OrdinalIgnoreCase))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Risk,
                    Area = "WCF Binding",
                    Finding = $"netTcpBinding endpoint discovered for {serviceName}",
                    Reason =
                        "netTcpBinding is WCF-specific and usually needs careful migration or replacement planning."
                });

                continue;
            }

            if (endpoint.Binding.Equals("wsHttpBinding", StringComparison.OrdinalIgnoreCase))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "WCF Binding",
                    Finding = $"wsHttpBinding endpoint discovered for {serviceName}",
                    Reason = "wsHttpBinding may indicate SOAP and WS-* features that need modernisation assessment."
                });

                continue;
            }

            if (endpoint.Binding.Equals("netMsmqBinding", StringComparison.OrdinalIgnoreCase))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Risk,
                    Area = "WCF Binding",
                    Finding = $"netMsmqBinding endpoint discovered for {serviceName}",
                    Reason =
                        "netMsmqBinding indicates queue-based WCF integration that needs separate migration planning."
                });
            }
        }
    }
    
    private static void AddAssemblyReferenceHints(
        IReadOnlyList<DiscoveredProject> projects,
        List<ModernisationHint> hints)
    {
        foreach (var project in projects)
        {
            foreach (var reference in project.AssemblyReferences)
            {
                if (reference.Equals("System.Web", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Risk,
                        Area = "Legacy ASP.NET",
                        Finding = $"{project.Name} references System.Web",
                        Reason = "System.Web usually indicates classic ASP.NET, WebForms, MVC 5, ASMX, or ASP.NET-hosted legacy functionality that does not directly migrate to modern ASP.NET Core."
                    });
                }

                if (reference.StartsWith("System.Web.", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET",
                        Finding = $"{project.Name} references {reference}",
                        Reason = "System.Web-related assemblies indicate legacy ASP.NET functionality that may need separate migration assessment."
                    });
                }
            }
        }
    }
}