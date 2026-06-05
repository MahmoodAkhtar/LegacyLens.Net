using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Analysis;

public sealed class ModernisationHintAnalyzer
{
    public IReadOnlyList<ModernisationHint> Analyze(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        return Analyze(
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            Array.Empty<WcfBehaviour>(),
            legacyAspNetArtifacts,
            configFiles);
    }

    public IReadOnlyList<ModernisationHint> Analyze(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(wcfEndpoints);
        ArgumentNullException.ThrowIfNull(wcfServiceContracts);
        ArgumentNullException.ThrowIfNull(wcfBehaviours);
        ArgumentNullException.ThrowIfNull(legacyAspNetArtifacts);
        ArgumentNullException.ThrowIfNull(configFiles);

        var hints = new List<ModernisationHint>();

        AddTargetFrameworkHints(projects, hints);
        AddProjectCouplingHints(projects, hints);
        AddPackageHints(projects, hints);
        AddWcfHints(wcfEndpoints, wcfServiceContracts, wcfBehaviours, hints);
        AddAssemblyReferenceHints(projects, hints);
        AddLegacyAspNetArtifactHints(legacyAspNetArtifacts, hints);
        AddConfigHints(configFiles, hints);

        var hintsWithEvidence = AddEvidenceMetadata(
            hints,
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            wcfBehaviours,
            legacyAspNetArtifacts,
            configFiles);

        return DeduplicateHints(hintsWithEvidence);
    }

    private static IReadOnlyList<ModernisationHint> DeduplicateHints(
        IReadOnlyList<ModernisationHint> hints)
    {
        return hints
            .GroupBy(hint => new
            {
                hint.Severity,
                Area = NormaliseForComparison(hint.Area),
                Finding = NormaliseForComparison(hint.Finding),
                Reason = NormaliseForComparison(hint.Reason),
                EvidenceKind = NormaliseForComparison(hint.EvidenceKind),
                EvidenceName = NormaliseForComparison(hint.EvidenceName),
                EvidencePath = NormaliseForComparison(hint.EvidencePath),
                hint.Confidence
            })
            .Select(group => group.First())
            .ToList();
    }

    private static string NormaliseForComparison(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private static IReadOnlyList<ModernisationHint> AddEvidenceMetadata(
        IReadOnlyList<ModernisationHint> hints,
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        return hints
            .Select(hint =>
                TryAddPackageEvidence(hint, projects) ??
                TryAddAssemblyReferenceEvidence(hint, projects) ??
                TryAddWcfBehaviourEvidence(hint, wcfBehaviours) ??
                TryAddWcfEndpointEvidence(hint, wcfEndpoints) ??
                TryAddWcfServiceContractEvidence(hint, wcfServiceContracts) ??
                TryAddLegacyAspNetArtifactEvidence(hint, legacyAspNetArtifacts) ??
                TryAddConfigFileEvidence(hint, configFiles) ??
                TryAddProjectEvidence(hint, projects) ??
                AddSummaryEvidence(hint))
            .ToList();
    }

    private static ModernisationHint? TryAddPackageEvidence(
        ModernisationHint hint,
        IReadOnlyList<DiscoveredProject> projects)
    {
        foreach (var project in projects)
        {
            foreach (var package in project.PackageReferences)
            {
                if (hint.Finding.Contains(project.Name, StringComparison.OrdinalIgnoreCase) &&
                    hint.Finding.Contains(package, StringComparison.OrdinalIgnoreCase))
                {
                    return WithEvidence(
                        hint,
                        "PackageReference",
                        package,
                        project.ProjectFilePath,
                        ModernisationHintConfidence.High);
                }
            }
        }

        return null;
    }

    private static ModernisationHint? TryAddAssemblyReferenceEvidence(
        ModernisationHint hint,
        IReadOnlyList<DiscoveredProject> projects)
    {
        foreach (var project in projects)
        {
            foreach (var reference in project.AssemblyReferences)
            {
                if (hint.Finding.Contains(project.Name, StringComparison.OrdinalIgnoreCase) &&
                    hint.Finding.Contains(reference, StringComparison.OrdinalIgnoreCase))
                {
                    return WithEvidence(
                        hint,
                        "AssemblyReference",
                        reference,
                        project.ProjectFilePath,
                        ModernisationHintConfidence.High);
                }
            }
        }

        return null;
    }

    private static ModernisationHint? TryAddProjectEvidence(
        ModernisationHint hint,
        IReadOnlyList<DiscoveredProject> projects)
    {
        foreach (var project in projects)
        {
            if (hint.Finding.StartsWith(project.Name, StringComparison.OrdinalIgnoreCase))
            {
                return WithEvidence(
                    hint,
                    "Project",
                    project.Name,
                    project.ProjectFilePath,
                    ModernisationHintConfidence.High);
            }
        }

        return null;
    }

    private static ModernisationHint? TryAddWcfEndpointEvidence(
        ModernisationHint hint,
        IReadOnlyList<WcfEndpoint> wcfEndpoints)
    {
        if (!IsWcfEndpointHint(hint))
        {
            return null;
        }

        foreach (var endpoint in wcfEndpoints)
        {
            var serviceName = GetServiceName(endpoint);

            if (hint.Finding.Contains(serviceName, StringComparison.OrdinalIgnoreCase))
            {
                return WithEvidence(
                    hint,
                    "WcfEndpoint",
                    serviceName,
                    endpoint.ConfigFilePath,
                    ModernisationHintConfidence.High);
            }
        }

        if (hint.Area.Equals("WCF", StringComparison.OrdinalIgnoreCase) &&
            hint.Finding.Contains("endpoint", StringComparison.OrdinalIgnoreCase))
        {
            if (wcfEndpoints.Count == 1)
            {
                var endpoint = wcfEndpoints[0];

                return WithEvidence(
                    hint,
                    "WcfEndpoint",
                    GetServiceName(endpoint),
                    endpoint.ConfigFilePath,
                    ModernisationHintConfidence.Medium);
            }

            if (wcfEndpoints.Count > 1)
            {
                return WithEvidence(
                    hint,
                    "WcfEndpointSummary",
                    $"{wcfEndpoints.Count} WCF endpoint(s)",
                    null,
                    ModernisationHintConfidence.Medium);
            }
        }

        return null;
    }

    private static bool IsWcfEndpointHint(ModernisationHint hint)
    {
        return hint.Area.Equals("WCF", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Equals("WCF Binding", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Equals("WCF Configuration", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Equals("WCF Security", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Equals("WCF Timeout", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Equals("WCF Binding Limits", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Equals("WCF Transfer Mode", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Equals("WCF Reader Quotas", StringComparison.OrdinalIgnoreCase) ||
               hint.Area.Equals("WCF Metadata", StringComparison.OrdinalIgnoreCase);
    }

    private static ModernisationHint? TryAddWcfServiceContractEvidence(
        ModernisationHint hint,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts)
    {
        if (!IsWcfServiceContractHint(hint))
        {
            return null;
        }

        foreach (var contract in wcfServiceContracts)
        {
            if (hint.Finding.Contains(contract.Name, StringComparison.OrdinalIgnoreCase))
            {
                return WithEvidence(
                    hint,
                    "WcfServiceContract",
                    contract.Name,
                    contract.SourceFilePath,
                    ModernisationHintConfidence.High);
            }
        }

        if (hint.Area.Equals("WCF", StringComparison.OrdinalIgnoreCase) &&
            hint.Finding.Contains("service contract", StringComparison.OrdinalIgnoreCase))
        {
            if (wcfServiceContracts.Count == 1)
            {
                var contract = wcfServiceContracts[0];

                return WithEvidence(
                    hint,
                    "WcfServiceContract",
                    contract.Name,
                    contract.SourceFilePath,
                    ModernisationHintConfidence.Medium);
            }

            if (wcfServiceContracts.Count > 1)
            {
                return WithEvidence(
                    hint,
                    "WcfServiceContractSummary",
                    $"{wcfServiceContracts.Count} WCF service contract(s)",
                    null,
                    ModernisationHintConfidence.Medium);
            }
        }

        return null;
    }

    private static bool IsWcfServiceContractHint(ModernisationHint hint)
    {
        return hint.Area.Equals("WCF", StringComparison.OrdinalIgnoreCase) ||
               hint.Finding.Contains("service contract", StringComparison.OrdinalIgnoreCase);
    }

    private static ModernisationHint? TryAddWcfBehaviourEvidence(
        ModernisationHint hint,
        IReadOnlyList<WcfBehaviour> wcfBehaviours)
    {
        if (!hint.Area.StartsWith("WCF", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        foreach (var behaviour in wcfBehaviours)
        {
            var behaviourName = GetWcfBehaviourName(behaviour);

            if (hint.Finding.Contains(behaviourName, StringComparison.OrdinalIgnoreCase))
            {
                return WithEvidence(
                    hint,
                    "WcfBehaviour",
                    behaviourName,
                    behaviour.ConfigFilePath,
                    ModernisationHintConfidence.High);
            }
        }

        return null;
    }

    private static ModernisationHint? TryAddLegacyAspNetArtifactEvidence(
        ModernisationHint hint,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts)
    {
        if (!hint.Area.StartsWith("Legacy ASP.NET", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var matchingArtifact = legacyAspNetArtifacts
            .Select(artifact => new
            {
                Artifact = artifact,
                Name = GetLegacyAspNetArtifactName(artifact)
            })
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Name) &&
                hint.Finding.Contains(x.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Name.Length)
            .FirstOrDefault();

        if (matchingArtifact is null)
        {
            return null;
        }

        return WithEvidence(
            hint,
            "LegacyAspNetArtifact",
            matchingArtifact.Name,
            matchingArtifact.Artifact.FilePath,
            ModernisationHintConfidence.High);
    }

    private static ModernisationHint? TryAddConfigFileEvidence(
        ModernisationHint hint,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        if (!hint.Area.Equals("Configuration", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        foreach (var configFile in configFiles)
        {
            var fileName = Path.GetFileName(configFile.FilePath);

            if (hint.Finding.Contains(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return WithEvidence(
                    hint,
                    "ConfigurationFile",
                    fileName,
                    configFile.FilePath,
                    ModernisationHintConfidence.High);
            }
        }

        return null;
    }

    private static ModernisationHint AddSummaryEvidence(ModernisationHint hint)
    {
        return WithEvidence(
            hint,
            "AnalysisSummary",
            hint.Area,
            null,
            ModernisationHintConfidence.Medium);
    }

    private static ModernisationHint WithEvidence(
        ModernisationHint hint,
        string evidenceKind,
        string evidenceName,
        string? evidencePath,
        ModernisationHintConfidence confidence)
    {
        return new ModernisationHint
        {
            Severity = hint.Severity,
            Area = hint.Area,
            Finding = hint.Finding,
            Reason = hint.Reason,
            EvidenceKind = evidenceKind,
            EvidenceName = evidenceName,
            EvidencePath = evidencePath,
            Confidence = confidence
        };
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
                    Finding =
                        $"{Path.GetFileName(configFile.FilePath)} contains {configFile.AppSettingsCount} appSettings entries",
                    Reason =
                        "A large number of appSettings entries may indicate environment-specific behaviour or operational settings hidden in configuration."
                });
            }

            if (configFile.ConnectionStringsCount > 0)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Info,
                    Area = "Configuration",
                    Finding =
                        $"{Path.GetFileName(configFile.FilePath)} contains {configFile.ConnectionStringsCount} connection string(s)",
                    Reason =
                        "Connection strings identify external data dependencies that should be reviewed during migration planning."
                });
            }

            if (configFile.CustomSectionCount > 0)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "Configuration",
                    Finding =
                        $"{Path.GetFileName(configFile.FilePath)} contains {configFile.CustomSectionCount} custom configuration section(s)",
                    Reason =
                        "Custom configuration sections may indicate framework-specific or application-specific behaviour that needs migration assessment."
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
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
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
            AddWcfEndpointDetailHints(wcfEndpoints, hints);
            AddWcfOperationalDetailHints(wcfEndpoints, hints);
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

        AddWcfBehaviourHints(wcfBehaviours, hints);
    }

    private static void AddWcfBehaviourHints(
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
        List<ModernisationHint> hints)
    {
        foreach (var behaviour in wcfBehaviours)
        {
            var behaviourName = GetWcfBehaviourName(behaviour);

            if (behaviour.Kind == WcfBehaviourKind.ServiceBehaviour)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Info,
                    Area = "WCF Behaviour",
                    Finding = $"{behaviourName} is a WCF service behaviour",
                    Reason =
                        "WCF service behaviours can contain metadata, debug, throttling, credential, authorization, and runtime settings that need migration review."
                });
            }

            if (behaviour.Kind == WcfBehaviourKind.EndpointBehaviour)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Info,
                    Area = "WCF Behaviour",
                    Finding = $"{behaviourName} is a WCF endpoint behaviour",
                    Reason =
                        "WCF endpoint behaviours can affect request handling, serialization, dispatch, message inspection, and REST-style endpoint behaviour."
                });
            }

            if (behaviour.HasServiceMetadata)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Info,
                    Area = "WCF Metadata",
                    Finding = $"{behaviourName} configures WCF service metadata publishing",
                    Reason =
                        "Service metadata settings are useful discovery signals when identifying SOAP contracts, generated clients, and compatibility requirements."
                });
            }

            if (IsTrue(behaviour.ServiceMetadataHttpGetEnabled) ||
                IsTrue(behaviour.ServiceMetadataHttpsGetEnabled))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Info,
                    Area = "WCF Metadata",
                    Finding = $"{behaviourName} enables WCF metadata publishing over HTTP or HTTPS",
                    Reason =
                        "HTTP or HTTPS metadata publishing may indicate externally discoverable SOAP metadata that clients depend on."
                });
            }

            if (IsTrue(behaviour.IncludeExceptionDetailInFaults))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "WCF Debug",
                    Finding = $"{behaviourName} includes exception detail in WCF faults",
                    Reason =
                        "Including exception detail in faults can expose implementation details and should be reviewed before moving to modern hosting or public endpoints."
                });
            }

            if (behaviour.HasServiceThrottling)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "WCF Throttling",
                    Finding = $"{behaviourName} configures WCF service throttling",
                    Reason =
                        "WCF throttling settings may need explicit equivalents in modern hosting, gateway, queue, or service runtime configuration."
                });
            }

            if (behaviour.HasWebHttp)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "WCF REST",
                    Finding = $"{behaviourName} uses WCF webHttp endpoint behaviour",
                    Reason =
                        "webHttp endpoint behaviour usually indicates REST-style WCF endpoints that need separate review when migrating to ASP.NET Core APIs."
                });
            }
        }
    }

    private static string GetWcfBehaviourName(WcfBehaviour behaviour)
    {
        if (!string.IsNullOrWhiteSpace(behaviour.Name))
        {
            return behaviour.Name;
        }

        return behaviour.Kind == WcfBehaviourKind.ServiceBehaviour
            ? "Unnamed service behaviour"
            : "Unnamed endpoint behaviour";
    }

    private static bool IsTrue(string? value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddWcfEndpointDetailHints(
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        List<ModernisationHint> hints)
    {
        foreach (var endpoint in wcfEndpoints)
        {
            var serviceName = GetServiceName(endpoint);

            if (!string.IsNullOrWhiteSpace(endpoint.BindingConfiguration))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Info,
                    Area = "WCF Configuration",
                    Finding = $"{serviceName} uses binding configuration {endpoint.BindingConfiguration}",
                    Reason =
                        "Named WCF binding configurations may contain security, timeout, size, protocol, or credential settings that need migration review."
                });
            }

            if (!string.IsNullOrWhiteSpace(endpoint.SecurityMode) &&
                !endpoint.SecurityMode.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "WCF Security",
                    Finding = $"{serviceName} uses WCF security mode {endpoint.SecurityMode}",
                    Reason =
                        "WCF security settings need explicit review when replacing WCF endpoints with modern HTTP, JSON, gRPC, or other service endpoints."
                });
            }

            if (!string.IsNullOrWhiteSpace(endpoint.TransportClientCredentialType) &&
                !endpoint.TransportClientCredentialType.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "WCF Security",
                    Finding = $"{serviceName} uses transport credential type {endpoint.TransportClientCredentialType}",
                    Reason =
                        "Transport credential settings may affect authentication and hosting choices during service migration."
                });
            }

            if (endpoint.IsMetadataExchangeEndpoint)
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Info,
                    Area = "WCF Metadata",
                    Finding = $"{serviceName} exposes a metadata exchange endpoint",
                    Reason =
                        "Metadata exchange endpoints are useful discovery signals when identifying SOAP contracts and generated client dependencies."
                });
            }
        }
    }

    private static void AddWcfOperationalDetailHints(
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        List<ModernisationHint> hints)
    {
        foreach (var endpoint in wcfEndpoints)
        {
            var serviceName = GetServiceName(endpoint);

            if (HasTimeoutSettings(endpoint))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Info,
                    Area = "WCF Timeout",
                    Finding = $"{serviceName} has explicit WCF timeout settings",
                    Reason =
                        "Configured WCF timeout values should be reviewed when replacing endpoints because modern HTTP, JSON, gRPC, hosting, gateway, and client timeout behaviour may differ."
                });
            }

            if (HasMessageSizeOrBufferLimits(endpoint))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Info,
                    Area = "WCF Binding Limits",
                    Finding = $"{serviceName} has explicit WCF message size or buffer limits",
                    Reason =
                        "Configured WCF message size and buffer limits should be reviewed when migrating endpoints because equivalent request, response, and hosting limits may need to be set explicitly."
                });
            }

            if (!string.IsNullOrWhiteSpace(endpoint.TransferMode))
            {
                var isStreamingTransferMode = IsStreamingTransferMode(endpoint.TransferMode);

                hints.Add(new ModernisationHint
                {
                    Severity = isStreamingTransferMode
                        ? ModernisationHintSeverity.Warning
                        : ModernisationHintSeverity.Info,
                    Area = "WCF Transfer Mode",
                    Finding = $"{serviceName} uses WCF transfer mode {endpoint.TransferMode}",
                    Reason = isStreamingTransferMode
                        ? "Streaming WCF transfer modes may affect endpoint redesign, request buffering, file upload/download behaviour, hosting limits, and client compatibility."
                        : "Explicit WCF transfer mode settings should be reviewed when replacing endpoints because modern hosting and client behaviour may differ."
                });
            }

            if (HasReaderQuotas(endpoint))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "WCF Reader Quotas",
                    Finding = $"{serviceName} has explicit WCF reader quota settings",
                    Reason =
                        "Configured WCF reader quotas may affect XML payload compatibility, maximum object graph depth, string sizes, array sizes, and generated SOAP client behaviour during migration."
                });
            }
        }
    }

    private static void AddWcfBindingHints(
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        List<ModernisationHint> hints)
    {
        foreach (var endpoint in wcfEndpoints)
        {
            var endpointDescription = GetWcfEndpointDescription(endpoint);

            if (string.IsNullOrWhiteSpace(endpoint.Binding))
            {
                hints.Add(new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "WCF Binding",
                    Finding = $"{endpointDescription} has a WCF endpoint without a binding",
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
                    Finding = $"basicHttpBinding endpoint discovered for {endpointDescription}",
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
                    Finding = $"netTcpBinding endpoint discovered for {endpointDescription}",
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
                    Finding = $"wsHttpBinding endpoint discovered for {endpointDescription}",
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
                    Finding = $"netMsmqBinding endpoint discovered for {endpointDescription}",
                    Reason =
                        "netMsmqBinding indicates queue-based WCF integration that needs separate migration planning."
                });
            }
        }
    }
    
    private static string GetWcfEndpointDescription(WcfEndpoint endpoint)
    {
        var description = GetServiceName(endpoint);

        if (!string.IsNullOrWhiteSpace(endpoint.Contract))
        {
            description += $" contract {endpoint.Contract}";
        }

        if (!string.IsNullOrWhiteSpace(endpoint.BindingConfiguration))
        {
            description += $" using binding configuration {endpoint.BindingConfiguration}";
        }

        return description;
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
                        Reason =
                            "System.Web usually indicates classic ASP.NET, WebForms, MVC 5, ASMX, or ASP.NET-hosted legacy functionality that does not directly migrate to modern ASP.NET Core."
                    });
                }

                if (reference.StartsWith("System.Web.", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET",
                        Finding = $"{project.Name} references {reference}",
                        Reason =
                            "System.Web-related assemblies indicate legacy ASP.NET functionality that may need separate migration assessment."
                    });
                }
            }
        }
    }

    private static void AddLegacyAspNetArtifactHints(
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        List<ModernisationHint> hints)
    {
        foreach (var artifact in legacyAspNetArtifacts)
        {
            var name = GetLegacyAspNetArtifactName(artifact);

            switch (artifact.Kind)
            {
                case LegacyAspNetArtifactKind.WebFormsPage:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Risk,
                        Area = "Legacy ASP.NET",
                        Finding = $"{name} is a WebForms page",
                        Reason =
                            "WebForms pages indicate classic ASP.NET UI that does not directly migrate to ASP.NET Core and usually needs redesign or replacement planning."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebFormsUserControl:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET",
                        Finding = $"{name} is a WebForms user control",
                        Reason =
                            "WebForms user controls may contain reusable UI and page lifecycle behaviour that needs review during ASP.NET Core migration planning."
                    });
                    break;

                case LegacyAspNetArtifactKind.MasterPage:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET",
                        Finding = $"{name} is a WebForms master page",
                        Reason =
                            "Master pages usually indicate shared WebForms layout structure that may need redesign when moving to modern ASP.NET."
                    });
                    break;

                case LegacyAspNetArtifactKind.AsmxWebService:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Risk,
                        Area = "Legacy ASP.NET",
                        Finding = $"{name} is an ASMX web service",
                        Reason =
                            "ASMX web services are legacy SOAP-style ASP.NET endpoints that usually need replacement or compatibility planning during modernisation."
                    });
                    break;

                case LegacyAspNetArtifactKind.HttpHandler:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET",
                        Finding = $"{name} is an ASP.NET HTTP handler",
                        Reason =
                            "HTTP handlers may contain custom request processing behaviour that needs mapping to modern ASP.NET middleware, endpoints, or controllers."
                    });
                    break;

                case LegacyAspNetArtifactKind.GlobalAsax:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET",
                        Finding = $"{name} is a Global.asax application file",
                        Reason =
                            "Global.asax may contain application startup, routing, error handling, or lifecycle code that should be reviewed when migrating to modern ASP.NET hosting."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcController:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET",
                        Finding = $"{name} is an ASP.NET MVC controller",
                        Reason =
                            "ASP.NET MVC controllers may contain routing, action filters, model binding, authentication, or System.Web-specific behaviour that needs review when moving to ASP.NET Core."
                    });
                    break;

                case LegacyAspNetArtifactKind.RouteConfig:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET",
                        Finding = $"{name} is an ASP.NET route configuration file",
                        Reason =
                            "Route configuration may define URL patterns, defaults, constraints, or ignored routes that should be reviewed when migrating to endpoint routing in ASP.NET Core."
                    });
                    break;

                case LegacyAspNetArtifactKind.AreaRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET",
                        Finding = $"{name} is an ASP.NET MVC area registration",
                        Reason =
                            "ASP.NET MVC area registrations may define area-specific routes and feature boundaries that should be reviewed when migrating to ASP.NET Core endpoint routing."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcAction:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET",
                        Finding = $"{name} is an ASP.NET MVC action",
                        Reason =
                            "MVC actions identify request-handling behaviour that should be reviewed for routing, model binding, result shape, filters, and ASP.NET Core controller migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcRouteAttribute:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET Routing",
                        Finding = $"{name} uses ASP.NET MVC attribute routing",
                        Reason =
                            "Attribute routes should be mapped carefully to ASP.NET Core endpoint routing to preserve URL patterns, defaults, constraints, and client compatibility."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcActionAttribute:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET MVC Attributes",
                        Finding = $"{name} uses an ASP.NET MVC action attribute",
                        Reason =
                            "MVC action attributes such as HTTP verb, authorization, anonymous access, anti-forgery, and output caching attributes may affect behaviour during ASP.NET Core migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcApplicationStartup:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET Startup",
                        Finding = $"{name} contains ASP.NET application startup code",
                        Reason =
                            "Application_Start may contain route, filter, bundle, dependency injection, error handling, or application lifecycle registration that needs mapping to ASP.NET Core hosting."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcAreaRegistrationCall:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET Startup",
                        Finding = $"{name} registers ASP.NET MVC areas",
                        Reason =
                            "Area registration calls identify MVC area routing setup that should be reviewed during ASP.NET Core endpoint routing migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcRouteRegistrationCall:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET Routing",
                        Finding = $"{name} registers ASP.NET routes",
                        Reason =
                            "Route registration calls identify conventional route setup that should be mapped carefully to ASP.NET Core endpoint routing."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcBundleConfig:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Bundling",
                        Finding = $"{name} is an ASP.NET MVC bundle configuration file",
                        Reason =
                            "ASP.NET MVC bundling and minification usually need replacement with a modern static asset, build, or bundling strategy."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcBundleRegistrationCall:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Bundling",
                        Finding = $"{name} registers ASP.NET MVC bundles",
                        Reason =
                            "Bundle registration calls may affect CSS and JavaScript delivery and should be reviewed when moving to modern ASP.NET hosting."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcFilterConfig:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Filters",
                        Finding = $"{name} is an ASP.NET MVC filter configuration file",
                        Reason =
                            "Global filters can affect authorization, error handling, caching, model binding, or other cross-cutting request behaviour during migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcFilterRegistrationCall:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Filters",
                        Finding = $"{name} registers ASP.NET MVC global filters",
                        Reason =
                            "Global filter registration should be reviewed because equivalent ASP.NET Core filters, middleware, or endpoint conventions may need to be configured explicitly."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcDependencyResolverRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Dependency Resolution",
                        Finding = $"{name} configures ASP.NET MVC dependency resolution",
                        Reason =
                            "MVC dependency resolver registration can affect controller activation, service lifetimes, filters, model binders, and other application services that need explicit mapping during ASP.NET Core migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcControllerFactoryRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Request Pipeline",
                        Finding = $"{name} configures an ASP.NET MVC controller factory",
                        Reason =
                            "Custom MVC controller factories can affect controller creation, dependency injection, routing behaviour, and request handling during migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcGlobalFilterRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Filters",
                        Finding = $"{name} registers an ASP.NET MVC global filter",
                        Reason =
                            "Global filter registrations can affect authorization, error handling, caching, model binding, and other cross-cutting request behaviour."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcModelBinderRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Model Binding",
                        Finding = $"{name} configures ASP.NET MVC model binders",
                        Reason =
                            "Custom MVC model binder registrations can materially affect request input handling and should be mapped carefully when moving to ASP.NET Core model binding."
                    });
                    break;

                case LegacyAspNetArtifactKind.MvcValueProviderFactoryRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Model Binding",
                        Finding = $"{name} configures ASP.NET MVC value provider factories",
                        Reason =
                            "Custom MVC value provider factories can affect how route, query string, form, header, cookie, or other request values are supplied to model binding."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiController:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Web API",
                        Finding = $"{name} is an ASP.NET Web API controller",
                        Reason =
                            "ASP.NET Web API controllers may contain HTTP API behaviour, routing, model binding, filters, authentication, or System.Web hosting assumptions that need review when moving to ASP.NET Core."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiAction:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET Web API",
                        Finding = $"{name} is an ASP.NET Web API action",
                        Reason =
                            "Web API actions identify HTTP endpoint behaviour that should be reviewed for routing, request and response shape, model binding, filters, and ASP.NET Core API migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiRouteAttribute:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET Web API Routing",
                        Finding = $"{name} uses ASP.NET Web API attribute routing",
                        Reason =
                            "Web API attribute routes should be mapped carefully to ASP.NET Core endpoint routing to preserve URL patterns, defaults, constraints, and client compatibility."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiActionAttribute:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Web API Attributes",
                        Finding = $"{name} uses an ASP.NET Web API action attribute",
                        Reason =
                            "Web API action attributes such as HTTP verb, authorization, anonymous access, and accept verbs attributes may affect endpoint behaviour during ASP.NET Core migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiConfig:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET Web API",
                        Finding = $"{name} is an ASP.NET Web API configuration file",
                        Reason =
                            "WebApiConfig may define HTTP API routing, formatters, filters, services, or other Web API configuration that should be mapped during ASP.NET Core migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiRouteRegistrationCall:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET Web API Routing",
                        Finding = $"{name} registers ASP.NET Web API routes",
                        Reason =
                            "Web API route registration calls identify conventional HTTP API route setup that should be mapped carefully to ASP.NET Core endpoint routing."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiGlobalConfigurationCall:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Info,
                        Area = "Legacy ASP.NET Web API Startup",
                        Finding = $"{name} registers ASP.NET Web API startup configuration",
                        Reason =
                            "Web API startup registration should be reviewed because routing, formatters, filters, dependency resolution, or other API configuration may need explicit ASP.NET Core equivalents."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiDependencyResolverRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Dependency Resolution",
                        Finding = $"{name} configures ASP.NET Web API dependency resolution",
                        Reason =
                            "Web API dependency resolver configuration can affect controller activation, service lifetimes, filters, message handlers, and other API services that need explicit ASP.NET Core equivalents."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiFormatterConfiguration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Web API Pipeline",
                        Finding = $"{name} configures ASP.NET Web API formatters",
                        Reason =
                            "Web API formatter configuration can affect JSON, XML, media type negotiation, serialization settings, and client compatibility during migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiMessageHandlerRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Web API Pipeline",
                        Finding = $"{name} registers an ASP.NET Web API message handler",
                        Reason =
                            "Web API message handlers can implement cross-cutting request and response behaviour that may need mapping to ASP.NET Core middleware or delegating handlers."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiFilterRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Web API Pipeline",
                        Finding = $"{name} registers an ASP.NET Web API filter",
                        Reason =
                            "Web API filter registrations can affect authorization, exception handling, action execution, caching, or other endpoint behaviour during migration."
                    });
                    break;

                case LegacyAspNetArtifactKind.WebApiCorsRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Web API Pipeline",
                        Finding = $"{name} enables ASP.NET Web API CORS configuration",
                        Reason =
                            "CORS configuration affects browser clients and cross-origin API access and should be mapped explicitly when migrating to ASP.NET Core."
                    });
                    break;
                
                case LegacyAspNetArtifactKind.HttpModuleRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Request Pipeline",
                        Finding = $"{name} registers an ASP.NET HTTP module",
                        Reason =
                            "HTTP modules can affect authentication, authorization, logging, headers, errors, or request lifecycle behaviour and may need mapping to ASP.NET Core middleware."
                    });
                    break;

                case LegacyAspNetArtifactKind.HttpHandlerRegistration:
                    hints.Add(new ModernisationHint
                    {
                        Severity = ModernisationHintSeverity.Warning,
                        Area = "Legacy ASP.NET Request Pipeline",
                        Finding = $"{name} registers an ASP.NET HTTP handler",
                        Reason =
                            "HTTP handler registrations can route requests to custom processing code and may need mapping to ASP.NET Core middleware, endpoints, or controllers."
                    });
                    break;
            }
        }
    }

    private static string GetLegacyAspNetArtifactName(DiscoveredLegacyAspNetArtifact artifact)
    {
        if (!string.IsNullOrWhiteSpace(artifact.Name))
        {
            return artifact.Name;
        }

        var fileName = Path.GetFileName(artifact.FilePath);

        return string.IsNullOrWhiteSpace(fileName)
            ? artifact.FilePath
            : fileName;
    }

    private static string GetServiceName(WcfEndpoint endpoint)
    {
        return string.IsNullOrWhiteSpace(endpoint.ServiceName)
            ? "Unknown service"
            : endpoint.ServiceName;
    }

    private static bool HasTimeoutSettings(WcfEndpoint endpoint)
    {
        return !string.IsNullOrWhiteSpace(endpoint.OpenTimeout) ||
               !string.IsNullOrWhiteSpace(endpoint.CloseTimeout) ||
               !string.IsNullOrWhiteSpace(endpoint.SendTimeout) ||
               !string.IsNullOrWhiteSpace(endpoint.ReceiveTimeout);
    }

    private static bool HasMessageSizeOrBufferLimits(WcfEndpoint endpoint)
    {
        return !string.IsNullOrWhiteSpace(endpoint.MaxReceivedMessageSize) ||
               !string.IsNullOrWhiteSpace(endpoint.MaxBufferSize) ||
               !string.IsNullOrWhiteSpace(endpoint.MaxBufferPoolSize);
    }

    private static bool HasReaderQuotas(WcfEndpoint endpoint)
    {
        return !string.IsNullOrWhiteSpace(endpoint.ReaderQuotaMaxDepth) ||
               !string.IsNullOrWhiteSpace(endpoint.ReaderQuotaMaxStringContentLength) ||
               !string.IsNullOrWhiteSpace(endpoint.ReaderQuotaMaxArrayLength) ||
               !string.IsNullOrWhiteSpace(endpoint.ReaderQuotaMaxBytesPerRead) ||
               !string.IsNullOrWhiteSpace(endpoint.ReaderQuotaMaxNameTableCharCount);
    }

    private static bool IsStreamingTransferMode(string transferMode)
    {
        return transferMode.Equals("Streamed", StringComparison.OrdinalIgnoreCase) ||
               transferMode.Equals("StreamedRequest", StringComparison.OrdinalIgnoreCase) ||
               transferMode.Equals("StreamedResponse", StringComparison.OrdinalIgnoreCase);
    }
}