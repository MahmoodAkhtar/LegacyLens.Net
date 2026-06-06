using System.Text;
using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Mermaid;

namespace LegacyLens.Reporting.Markdown;

public sealed class MarkdownReportWriter
{
    public void Write(
        string outputPath,
        IReadOnlyList<DiscoveredSolution> solutions,
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<ModernisationHint> modernisationHints,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        Write(
            outputPath,
            solutions,
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            Array.Empty<WcfBehaviour>(),
            legacyAspNetArtifacts,
            modernisationHints,
            configFiles);
    }

    public void Write(
        string outputPath,
        IReadOnlyList<DiscoveredSolution> solutions,
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<ModernisationHint> modernisationHints,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));
        }

        ArgumentNullException.ThrowIfNull(solutions);
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(wcfEndpoints);
        ArgumentNullException.ThrowIfNull(wcfServiceContracts);
        ArgumentNullException.ThrowIfNull(wcfBehaviours);
        ArgumentNullException.ThrowIfNull(legacyAspNetArtifacts);
        ArgumentNullException.ThrowIfNull(modernisationHints);
        ArgumentNullException.ThrowIfNull(configFiles);

        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var markdown = BuildMarkdown(
            solutions,
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            wcfBehaviours,
            legacyAspNetArtifacts,
            modernisationHints,
            configFiles);

        File.WriteAllText(outputPath, markdown);
    }

    private static string BuildMarkdown(
        IReadOnlyList<DiscoveredSolution> solutions,
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<WcfBehaviour> wcfBehaviours,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> legacyAspNetArtifacts,
        IReadOnlyList<ModernisationHint> modernisationHints,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# LegacyLens.NET Discovery Report");
        builder.AppendLine();

        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Solutions discovered: {solutions.Count}");
        builder.AppendLine($"- Projects discovered: {projects.Count}");
        builder.AppendLine($"- Project references discovered: {projects.Sum(x => x.ProjectReferences.Count)}");
        builder.AppendLine($"- Package references discovered: {projects.Sum(x => x.PackageReferences.Count)}");
        builder.AppendLine($"- WCF endpoints discovered: {wcfEndpoints.Count}");
        builder.AppendLine($"- WCF service contracts discovered: {wcfServiceContracts.Count}");
        builder.AppendLine($"- WCF behaviours discovered: {wcfBehaviours.Count}");
        builder.AppendLine($"- Legacy ASP.NET artifacts discovered: {legacyAspNetArtifacts.Count}");
        builder.AppendLine($"- Assembly references discovered: {projects.Sum(x => x.AssemblyReferences.Count)}");
        builder.AppendLine();

        AppendSolutions(builder, solutions);
        AppendProjects(builder, projects);
        AppendTargetFrameworkSummary(builder, projects);
        AppendPackageReferenceSummary(builder, projects);
        AppendProjectDependencyDiagram(builder, projects);
        AppendProjectReferences(builder, projects);
        AppendAssemblyReferences(builder, projects);
        AppendPackageReferences(builder, projects);
        AppendPackageCompatibilityReview(builder, projects);
        AppendWcfEndpoints(builder, wcfEndpoints);
        AppendWcfBindingDetails(builder, wcfEndpoints);
        AppendWcfReaderQuotas(builder, wcfEndpoints);
        AppendWcfBehaviours(builder, wcfBehaviours);
        AppendWcfServiceContracts(builder, wcfServiceContracts);
        AppendLegacyAspNetArtifacts(builder, legacyAspNetArtifacts);
        AppendConfigurationFiles(builder, configFiles);
        AppendModernisationReviewSummary(builder, modernisationHints);
        AppendModernisationHints(builder, modernisationHints);

        return builder.ToString();
    }

    private static void AppendWcfBehaviours(
        StringBuilder builder,
        IReadOnlyList<WcfBehaviour> behaviours)
    {
        builder.AppendLine("## WCF Behaviours");
        builder.AppendLine();
        builder.AppendLine(
            "| Kind | Name | Service Metadata | HTTP Metadata | HTTPS Metadata | Service Debug | Exception Detail In Faults | Service Throttling | Max Concurrent Calls | Max Concurrent Sessions | Max Concurrent Instances | Web HTTP | Config File |");
        builder.AppendLine("|---|---|---|---|---|---|---|---|---:|---:|---:|---|---|");

        if (behaviours.Count == 0)
        {
            builder.AppendLine(
                "| None | None | None | None | None | None | None | None | None | None | None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var behaviour in behaviours
                     .OrderBy(x => x.Kind)
                     .ThenBy(x => x.Name)
                     .ThenBy(x => x.ConfigFilePath))
        {
            builder.AppendLine(
                $"| {behaviour.Kind} | {Escape(behaviour.Name ?? "")} | {behaviour.HasServiceMetadata} | {Escape(behaviour.ServiceMetadataHttpGetEnabled ?? "")} | {Escape(behaviour.ServiceMetadataHttpsGetEnabled ?? "")} | {behaviour.HasServiceDebug} | {Escape(behaviour.IncludeExceptionDetailInFaults ?? "")} | {behaviour.HasServiceThrottling} | {Escape(behaviour.MaxConcurrentCalls ?? "")} | {Escape(behaviour.MaxConcurrentSessions ?? "")} | {Escape(behaviour.MaxConcurrentInstances ?? "")} | {behaviour.HasWebHttp} | `{behaviour.ConfigFilePath}` |");
        }

        builder.AppendLine();
    }

    private static void AppendSolutions(
        StringBuilder builder,
        IReadOnlyList<DiscoveredSolution> solutions)
    {
        builder.AppendLine("## Solutions");
        builder.AppendLine();
        builder.AppendLine("| Solution | Projects | Solution File |");
        builder.AppendLine("|---|---:|---|");

        if (solutions.Count == 0)
        {
            builder.AppendLine("| None | 0 | None |");
            builder.AppendLine();
            return;
        }

        foreach (var solution in solutions.OrderBy(x => x.Name))
        {
            builder.AppendLine(
                $"| {Escape(solution.Name)} | {solution.ProjectFilePaths.Count} | `{solution.SolutionFilePath}` |");
        }

        builder.AppendLine();
    }

    private static void AppendProjects(
        StringBuilder builder,
        IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Projects");
        builder.AppendLine();
        builder.AppendLine("| Project | Target Framework | Project File |");
        builder.AppendLine("|---|---|---|");

        if (projects.Count == 0)
        {
            builder.AppendLine("| None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var project in projects.OrderBy(x => x.Name))
        {
            builder.AppendLine(
                $"| {Escape(project.Name)} | {Escape(project.TargetFramework ?? "Unknown")} | `{project.ProjectFilePath}` |");
        }

        builder.AppendLine();
    }

    private static void AppendTargetFrameworkSummary(
        StringBuilder builder,
        IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Target Framework Summary");
        builder.AppendLine();
        builder.AppendLine("| Target Framework | Projects |");
        builder.AppendLine("|---|---:|");

        if (projects.Count == 0)
        {
            builder.AppendLine("| None | 0 |");
            builder.AppendLine();
            return;
        }

        var targetFrameworkGroups = projects
            .GroupBy(
                x => string.IsNullOrWhiteSpace(x.TargetFramework)
                    ? "Unknown"
                    : x.TargetFramework,
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key);

        foreach (var group in targetFrameworkGroups)
        {
            builder.AppendLine($"| {Escape(group.Key)} | {group.Count()} |");
        }

        builder.AppendLine();
    }

    private static void AppendPackageReferenceSummary(
        StringBuilder builder,
        IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Package Reference Summary");
        builder.AppendLine();
        builder.AppendLine("| Package | Projects |");
        builder.AppendLine("|---|---:|");

        var packageGroups = projects
            .SelectMany(project => project.PackageReferences
                .Where(package => !string.IsNullOrWhiteSpace(package))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(package => new
                {
                    Project = project,
                    Package = package
                }))
            .GroupBy(x => x.Package, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key)
            .ToList();

        if (packageGroups.Count == 0)
        {
            builder.AppendLine("| None | 0 |");
            builder.AppendLine();
            return;
        }

        foreach (var group in packageGroups)
        {
            var projectCount = group
                .Select(x => x.Project.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            builder.AppendLine($"| {Escape(group.Key)} | {projectCount} |");
        }

        builder.AppendLine();
    }

    private static void AppendProjectDependencyDiagram(
        StringBuilder builder,
        IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Project Dependency Diagram");
        builder.AppendLine();

        var mermaidDiagramWriter = new MermaidDiagramWriter();

        builder.AppendLine(mermaidDiagramWriter.BuildProjectDependencyDiagram(projects));
        builder.AppendLine();
    }

    private static void AppendProjectReferences(
        StringBuilder builder,
        IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Project References");
        builder.AppendLine();
        builder.AppendLine("| From | To |");
        builder.AppendLine("|---|---|");

        var hasReferences = false;

        foreach (var project in projects.OrderBy(x => x.Name))
        {
            foreach (var reference in project.ProjectReferences.OrderBy(x => x))
            {
                hasReferences = true;
                builder.AppendLine($"| {Escape(project.Name)} | `{reference}` |");
            }
        }

        if (!hasReferences)
        {
            builder.AppendLine("| None | None |");
        }

        builder.AppendLine();
    }

    private static void AppendAssemblyReferences(
        StringBuilder builder,
        IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Assembly References");
        builder.AppendLine();
        builder.AppendLine("| Project | Assembly |");
        builder.AppendLine("|---|---|");

        var hasAssemblyReferences = false;

        foreach (var project in projects.OrderBy(x => x.Name))
        {
            foreach (var assemblyReference in project.AssemblyReferences.OrderBy(x => x))
            {
                hasAssemblyReferences = true;
                builder.AppendLine($"| {Escape(project.Name)} | `{Escape(assemblyReference)}` |");
            }
        }

        if (!hasAssemblyReferences)
        {
            builder.AppendLine("| None | None |");
        }

        builder.AppendLine();
    }

    private static void AppendPackageReferences(
        StringBuilder builder,
        IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Package References");
        builder.AppendLine();
        builder.AppendLine("| Project | Package |");
        builder.AppendLine("|---|---|");

        var hasPackages = false;

        foreach (var project in projects.OrderBy(x => x.Name))
        {
            foreach (var package in project.PackageReferences.OrderBy(x => x))
            {
                hasPackages = true;
                builder.AppendLine($"| {Escape(project.Name)} | `{Escape(package)}` |");
            }
        }

        if (!hasPackages)
        {
            builder.AppendLine("| None | None |");
        }

        builder.AppendLine();
    }

    private static void AppendPackageCompatibilityReview(
        StringBuilder builder,
        IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Package Compatibility Review");
        builder.AppendLine();
        builder.AppendLine("| Project | Project Target Framework | Package | Version | Package Target Framework | Source | Source File | Concern |");
        builder.AppendLine("|---|---|---|---|---|---|---|---|");

        var reviewer = new PackageCompatibilityReviewer();
        var reviewItems = reviewer.Review(projects);

        if (reviewItems.Count == 0)
        {
            builder.AppendLine("| None | None | None | None | None | None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var item in reviewItems)
        {
            builder.AppendLine(
                $"| {Escape(item.ProjectName)} | {Escape(item.ProjectTargetFramework ?? "Unknown")} | {Escape(item.PackageName)} | {Escape(item.Version ?? "unknown")} | {Escape(item.PackageTargetFramework ?? "")} | {Escape(item.SourceFormat)} | `{Escape(item.SourcePath)}` | {Escape(item.Concern)} |");
        }

        builder.AppendLine();
    }

    private static void AppendWcfEndpoints(
        StringBuilder builder,
        IReadOnlyList<WcfEndpoint> endpoints)
    {
        builder.AppendLine("## WCF Endpoints");
        builder.AppendLine();
        builder.AppendLine(
            "| Service | Address | Binding | Binding Configuration | Security Mode | Transport Credential | Message Credential | Metadata Exchange | Contract | Config File |");
        builder.AppendLine("|---|---|---|---|---|---|---|---|---|---|");

        if (endpoints.Count == 0)
        {
            builder.AppendLine("| None | None | None | None | None | None | None | None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var endpoint in endpoints.OrderBy(x => x.ServiceName).ThenBy(x => x.Contract))
        {
            builder.AppendLine(
                $"| {Escape(endpoint.ServiceName ?? "Unknown")} | {Escape(endpoint.Address ?? "")} | {Escape(endpoint.Binding ?? "")} | {Escape(endpoint.BindingConfiguration ?? "")} | {Escape(endpoint.SecurityMode ?? "")} | {Escape(endpoint.TransportClientCredentialType ?? "")} | {Escape(endpoint.MessageClientCredentialType ?? "")} | {endpoint.IsMetadataExchangeEndpoint} | {Escape(endpoint.Contract ?? "")} | `{endpoint.ConfigFilePath}` |");
        }

        builder.AppendLine();
    }

    private static void AppendWcfBindingDetails(
        StringBuilder builder,
        IReadOnlyList<WcfEndpoint> endpoints)
    {
        builder.AppendLine("## WCF Binding Details");
        builder.AppendLine();
        builder.AppendLine(
            "| Service | Binding | Binding Configuration | Open Timeout | Close Timeout | Send Timeout | Receive Timeout | Max Received Message Size | Max Buffer Size | Max Buffer Pool Size | Transfer Mode |");
        builder.AppendLine("|---|---|---|---|---|---|---|---:|---:|---:|---|");

        var endpointsWithBindingDetails = endpoints
            .Where(HasBindingDetails)
            .OrderBy(x => x.ServiceName)
            .ThenBy(x => x.Contract)
            .ToList();

        if (endpointsWithBindingDetails.Count == 0)
        {
            builder.AppendLine("| None | None | None | None | None | None | None | None | None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var endpoint in endpointsWithBindingDetails)
        {
            builder.AppendLine(
                $"| {Escape(endpoint.ServiceName ?? "Unknown")} | {Escape(endpoint.Binding ?? "")} | {Escape(endpoint.BindingConfiguration ?? "")} | {Escape(endpoint.OpenTimeout ?? "")} | {Escape(endpoint.CloseTimeout ?? "")} | {Escape(endpoint.SendTimeout ?? "")} | {Escape(endpoint.ReceiveTimeout ?? "")} | {Escape(endpoint.MaxReceivedMessageSize ?? "")} | {Escape(endpoint.MaxBufferSize ?? "")} | {Escape(endpoint.MaxBufferPoolSize ?? "")} | {Escape(endpoint.TransferMode ?? "")} |");
        }

        builder.AppendLine();
    }

    private static void AppendWcfReaderQuotas(
        StringBuilder builder,
        IReadOnlyList<WcfEndpoint> endpoints)
    {
        builder.AppendLine("## WCF Reader Quotas");
        builder.AppendLine();
        builder.AppendLine(
            "| Service | Binding | Binding Configuration | Max Depth | Max String Content Length | Max Array Length | Max Bytes Per Read | Max Name Table Char Count |");
        builder.AppendLine("|---|---|---|---:|---:|---:|---:|---:|");

        var endpointsWithReaderQuotas = endpoints
            .Where(HasReaderQuotas)
            .OrderBy(x => x.ServiceName)
            .ThenBy(x => x.Contract)
            .ToList();

        if (endpointsWithReaderQuotas.Count == 0)
        {
            builder.AppendLine("| None | None | None | None | None | None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var endpoint in endpointsWithReaderQuotas)
        {
            builder.AppendLine(
                $"| {Escape(endpoint.ServiceName ?? "Unknown")} | {Escape(endpoint.Binding ?? "")} | {Escape(endpoint.BindingConfiguration ?? "")} | {Escape(endpoint.ReaderQuotaMaxDepth ?? "")} | {Escape(endpoint.ReaderQuotaMaxStringContentLength ?? "")} | {Escape(endpoint.ReaderQuotaMaxArrayLength ?? "")} | {Escape(endpoint.ReaderQuotaMaxBytesPerRead ?? "")} | {Escape(endpoint.ReaderQuotaMaxNameTableCharCount ?? "")} |");
        }

        builder.AppendLine();
    }

    private static void AppendWcfServiceContracts(
        StringBuilder builder,
        IReadOnlyList<WcfServiceContract> contracts)
    {
        builder.AppendLine("## WCF Service Contracts");
        builder.AppendLine();
        builder.AppendLine("| Contract | Operations | Source File |");
        builder.AppendLine("|---|---|---|");

        if (contracts.Count == 0)
        {
            builder.AppendLine("| None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var contract in contracts.OrderBy(x => x.Name))
        {
            var operations = contract.Operations.Count == 0
                ? ""
                : string.Join(", ", contract.Operations.Select(Escape));

            builder.AppendLine(
                $"| {Escape(contract.Name)} | {operations} | `{contract.SourceFilePath}` |");
        }

        builder.AppendLine();
    }

    private static void AppendLegacyAspNetArtifacts(
        StringBuilder builder,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        builder.AppendLine("## Legacy ASP.NET Artifacts");
        builder.AppendLine();
        builder.AppendLine("| Kind | Name | File |");
        builder.AppendLine("|---|---|---|");

        if (artifacts.Count == 0)
        {
            builder.AppendLine("| None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var artifact in artifacts
                     .OrderBy(x => x.Kind)
                     .ThenBy(x => x.Name)
                     .ThenBy(x => x.FilePath))
        {
            var name = string.IsNullOrWhiteSpace(artifact.Name)
                ? Path.GetFileName(artifact.FilePath)
                : artifact.Name;

            builder.AppendLine(
                $"| {Escape(artifact.Kind.ToString())} | {Escape(name)} | `{artifact.FilePath}` |");
        }

        builder.AppendLine();
    }

    private static void AppendConfigurationFiles(
        StringBuilder builder,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        builder.AppendLine("## Configuration Files");
        builder.AppendLine();
        builder.AppendLine("| Config File | App Settings | Connection Strings | Custom Sections |");
        builder.AppendLine("|---|---:|---:|---:|");

        if (configFiles.Count == 0)
        {
            builder.AppendLine("| None | 0 | 0 | 0 |");
            builder.AppendLine();
            return;
        }

        foreach (var configFile in configFiles.OrderBy(x => x.FilePath))
        {
            builder.AppendLine(
                $"| `{configFile.FilePath}` | {configFile.AppSettingsCount} | {configFile.ConnectionStringsCount} | {configFile.CustomSectionCount} |");
        }

        builder.AppendLine();
    }

    private static void AppendModernisationReviewSummary(
        StringBuilder builder,
        IReadOnlyList<ModernisationHint> hints)
    {
        builder.AppendLine("## Modernisation Review Summary");
        builder.AppendLine();
        builder.AppendLine("| Priority | Review Area | Highest Severity | Risks | Warnings | Info | Summary |");
        builder.AppendLine("|---:|---|---|---:|---:|---:|---|");

        var prioritiser = new ModernisationReviewPrioritiser();
        var reviewAreas = prioritiser.Prioritise(hints);

        if (reviewAreas.Count == 0)
        {
            builder.AppendLine("| 0 | None | None | 0 | 0 | 0 | None |");
            builder.AppendLine();
            return;
        }

        var priority = 1;

        foreach (var reviewArea in reviewAreas)
        {
            builder.AppendLine(
                $"| {priority} | {Escape(reviewArea.Area)} | {reviewArea.HighestSeverity} | {reviewArea.RiskCount} | {reviewArea.WarningCount} | {reviewArea.InfoCount} | {Escape(reviewArea.Summary)} |");

            priority++;
        }

        builder.AppendLine();
    }

    private static void AppendModernisationHints(
        StringBuilder builder,
        IReadOnlyList<ModernisationHint> hints)
    {
        builder.AppendLine("## Modernisation Hints");
        builder.AppendLine();
        builder.AppendLine("| Severity | Area | Finding | Evidence | Confidence | Source | Reason |");
        builder.AppendLine("|---|---|---|---|---|---|---|");

        if (hints.Count == 0)
        {
            builder.AppendLine("| None | None | None | None | None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var hint in hints
                     .OrderByDescending(x => x.Severity)
                     .ThenBy(x => x.Area)
                     .ThenBy(x => x.Finding))
        {
            var evidence = BuildEvidenceText(hint);
            var source = BuildSourceText(hint);

            builder.AppendLine(
                $"| {hint.Severity} | {Escape(hint.Area)} | {Escape(hint.Finding)} | {Escape(evidence)} | {hint.Confidence} | {source} | {Escape(hint.Reason)} |");
        }

        builder.AppendLine();
    }

    private static string BuildEvidenceText(ModernisationHint hint)
    {
        if (string.IsNullOrWhiteSpace(hint.EvidenceKind) &&
            string.IsNullOrWhiteSpace(hint.EvidenceName))
        {
            return "None";
        }

        if (string.IsNullOrWhiteSpace(hint.EvidenceKind))
        {
            return hint.EvidenceName ?? "None";
        }

        if (string.IsNullOrWhiteSpace(hint.EvidenceName))
        {
            return hint.EvidenceKind;
        }

        return $"{hint.EvidenceKind}: {hint.EvidenceName}";
    }

    private static string BuildSourceText(ModernisationHint hint)
    {
        if (string.IsNullOrWhiteSpace(hint.EvidencePath))
        {
            return "None";
        }

        return $"`{Escape(hint.EvidencePath)}`";
    }

    private static bool HasBindingDetails(WcfEndpoint endpoint)
    {
        return !string.IsNullOrWhiteSpace(endpoint.OpenTimeout) ||
               !string.IsNullOrWhiteSpace(endpoint.CloseTimeout) ||
               !string.IsNullOrWhiteSpace(endpoint.SendTimeout) ||
               !string.IsNullOrWhiteSpace(endpoint.ReceiveTimeout) ||
               !string.IsNullOrWhiteSpace(endpoint.MaxReceivedMessageSize) ||
               !string.IsNullOrWhiteSpace(endpoint.MaxBufferSize) ||
               !string.IsNullOrWhiteSpace(endpoint.MaxBufferPoolSize) ||
               !string.IsNullOrWhiteSpace(endpoint.TransferMode);
    }

    private static bool HasReaderQuotas(WcfEndpoint endpoint)
    {
        return !string.IsNullOrWhiteSpace(endpoint.ReaderQuotaMaxDepth) ||
               !string.IsNullOrWhiteSpace(endpoint.ReaderQuotaMaxStringContentLength) ||
               !string.IsNullOrWhiteSpace(endpoint.ReaderQuotaMaxArrayLength) ||
               !string.IsNullOrWhiteSpace(endpoint.ReaderQuotaMaxBytesPerRead) ||
               !string.IsNullOrWhiteSpace(endpoint.ReaderQuotaMaxNameTableCharCount);
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|");
    }
}
