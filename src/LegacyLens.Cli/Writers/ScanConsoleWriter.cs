using System.Reflection;
using LegacyLens.Cli.Commands;

namespace LegacyLens.Cli.Writers;

public sealed class ScanConsoleWriter
{
    public void Write(ScanResult result, ScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Quiet)
        {
            WriteQuiet(result);
            return;
        }

        if (options.Verbose)
        {
            WriteVerbose(result);
            return;
        }

        WriteNormal(result);
    }

    public void WriteHelp()
    {
        Console.WriteLine("LegacyLens.NET");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  legacylens scan <path> [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <path>                 Folder containing one or more .NET solutions or projects.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o, --output <file>    Markdown report file to create.");
        Console.WriteLine("  --output-dir <dir>     Directory where discovery-report.md should be written.");
        Console.WriteLine("  --format <format>      Report format. Currently only markdown is supported.");
        Console.WriteLine("  --quiet                Only print essential output.");
        Console.WriteLine("  --verbose              Print detailed discovery output.");
        Console.WriteLine("  -h, --help             Show help.");
        Console.WriteLine("  --version              Show version.");
        Console.WriteLine("  --artifacts <value>     Optional artifact selection. Currently supports upgrade-readiness.");
        Console.WriteLine("  --upgrade-target <tfm>  Optional requested target framework for upgrade-readiness wording.");
    }

    public void WriteVersion()
    {
        var version = typeof(ScanConsoleWriter)
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(version))
        {
            version = typeof(ScanConsoleWriter).Assembly.GetName().Version?.ToString() ?? "unknown";
        }

        Console.WriteLine(version);
    }

    public void WriteError(string message)
    {
        Console.Error.WriteLine(message);
    }

    private static void WriteQuiet(ScanResult result)
    {
        Console.WriteLine($"Markdown report generated: {result.OutputPath}");
    }

    private static void WriteNormal(ScanResult result)
    {
        Console.WriteLine("LegacyLens.NET");
        Console.WriteLine();
        Console.WriteLine($"Scan path: {result.ScanPath}");
        Console.WriteLine($"Report: {result.OutputPath}");
        Console.WriteLine();
        Console.WriteLine("Summary:");
        Console.WriteLine($"- Solutions discovered: {result.Solutions.Count}");
        Console.WriteLine($"- Projects discovered: {result.Projects.Count}");
        Console.WriteLine($"- Project references discovered: {result.ProjectReferenceCount}");
        Console.WriteLine($"- Package references discovered: {result.PackageReferenceCount}");
        Console.WriteLine($"- Assembly references discovered: {result.AssemblyReferenceCount}");
        Console.WriteLine($"- WCF endpoints discovered: {result.WcfEndpoints.Count}");
        Console.WriteLine($"- WCF service contracts discovered: {result.WcfServiceContracts.Count}");
        Console.WriteLine($"- WCF behaviours discovered: {result.WcfBehaviours.Count}");
        Console.WriteLine($"- Legacy ASP.NET artifacts discovered: {result.LegacyAspNetArtifacts.Count}");
        Console.WriteLine($"- Configuration files discovered: {result.ConfigFiles.Count}");
        Console.WriteLine($"- Modernisation hints discovered: {result.ModernisationHints.Count}");
        Console.WriteLine();
        Console.WriteLine("Top review areas:");

        if (result.ModernisationReviewAreas.Count == 0)
        {
            Console.WriteLine("- None");
        }
        else
        {
            var priority = 1;

            foreach (var reviewArea in result.ModernisationReviewAreas.Take(3))
            {
                Console.WriteLine($"{priority}. {reviewArea.Area}");
                priority++;
            }
        }

        Console.WriteLine();
        Console.WriteLine("Markdown report generated:");
        Console.WriteLine(result.OutputPath);
        
        if (!string.IsNullOrWhiteSpace(result.UpgradeReadinessOutputPath))
        {
            Console.WriteLine();
            Console.WriteLine("Upgrade readiness report generated:");
            Console.WriteLine(result.UpgradeReadinessOutputPath);
        }
    }

    private static void WriteVerbose(ScanResult result)
    {
        Console.WriteLine("LegacyLens.NET");
        Console.WriteLine();
        Console.WriteLine($"Scan path: {result.ScanPath}");
        Console.WriteLine($"Report: {result.OutputPath}");
        Console.WriteLine();

        WriteProjects(result);
        WriteWcfEndpoints(result);
        WriteWcfServiceContracts(result);
        WriteWcfBehaviours(result);
        WriteConfigurationFiles(result);
        WriteLegacyAspNetArtifacts(result);
        WriteModernisationHints(result);
        WriteModernisationReviewSummary(result);
        WriteSolutions(result);

        Console.WriteLine();
        Console.WriteLine($"Markdown report generated: {result.OutputPath}");
    }

    private static void WriteProjects(ScanResult result)
    {
        Console.WriteLine("Projects discovered:");

        if (result.Projects.Count == 0)
        {
            Console.WriteLine("- None");
            Console.WriteLine();
            return;
        }

        foreach (var project in result.Projects)
        {
            Console.WriteLine($"- {project.Name}");

            if (!string.IsNullOrWhiteSpace(project.TargetFramework))
            {
                Console.WriteLine($"  Target framework: {project.TargetFramework}");
            }

            foreach (var reference in project.ProjectReferences)
            {
                Console.WriteLine($"  Project reference: {reference}");
            }

            foreach (var assemblyReference in project.AssemblyReferences)
            {
                Console.WriteLine($"  Assembly reference: {assemblyReference}");
            }

            foreach (var package in project.PackageReferenceDetails)
            {
                var version = string.IsNullOrWhiteSpace(package.Version)
                    ? "unknown"
                    : package.Version;

                var packageTargetFramework = string.IsNullOrWhiteSpace(package.PackageTargetFramework)
                    ? string.Empty
                    : $", package target framework: {package.PackageTargetFramework}";

                Console.WriteLine(
                    $"  Package reference: {package.Name} {version} (source: {package.SourceFormat}{packageTargetFramework})");
            }

            if (project.PackageReferenceDetails.Count == 0)
            {
                foreach (var package in project.PackageReferences)
                {
                    Console.WriteLine($"  Package reference: {package}");
                }
            }
        }

        Console.WriteLine();
    }

    private static void WriteWcfEndpoints(ScanResult result)
    {
        Console.WriteLine("WCF endpoints discovered:");

        if (result.WcfEndpoints.Count == 0)
        {
            Console.WriteLine("- None");
            Console.WriteLine();
            return;
        }

        foreach (var endpoint in result.WcfEndpoints)
        {
            Console.WriteLine($"- {endpoint.ServiceName ?? "Unknown service"}");
            Console.WriteLine($"  Address: {endpoint.Address ?? string.Empty}");
            Console.WriteLine($"  Binding: {endpoint.Binding ?? string.Empty}");
            Console.WriteLine($"  Binding configuration: {endpoint.BindingConfiguration ?? string.Empty}");
            Console.WriteLine($"  Behaviour configuration: {endpoint.BehaviorConfiguration ?? string.Empty}");
            Console.WriteLine($"  Contract: {endpoint.Contract ?? string.Empty}");
            Console.WriteLine($"  Metadata exchange: {endpoint.IsMetadataExchangeEndpoint}");
            Console.WriteLine($"  Config file: {endpoint.ConfigFilePath}");
        }

        Console.WriteLine();
    }

    private static void WriteWcfServiceContracts(ScanResult result)
    {
        Console.WriteLine("WCF service contracts discovered:");

        if (result.WcfServiceContracts.Count == 0)
        {
            Console.WriteLine("- None");
            Console.WriteLine();
            return;
        }

        foreach (var contract in result.WcfServiceContracts)
        {
            Console.WriteLine($"- {contract.Name}");
            Console.WriteLine($"  Source file: {contract.SourceFilePath}");

            foreach (var operation in contract.Operations)
            {
                Console.WriteLine($"  Operation: {operation}");
            }
        }

        Console.WriteLine();
    }

    private static void WriteWcfBehaviours(ScanResult result)
    {
        Console.WriteLine("WCF behaviours discovered:");

        if (result.WcfBehaviours.Count == 0)
        {
            Console.WriteLine("- None");
            Console.WriteLine();
            return;
        }

        foreach (var behaviour in result.WcfBehaviours)
        {
            Console.WriteLine($"- {behaviour.Kind}: {behaviour.Name ?? "Unnamed"}");
            Console.WriteLine($"  Config file: {behaviour.ConfigFilePath}");

            if (behaviour.HasServiceMetadata)
            {
                Console.WriteLine("  Service metadata: True");
                Console.WriteLine($"  HTTP metadata enabled: {behaviour.ServiceMetadataHttpGetEnabled ?? string.Empty}");
                Console.WriteLine($"  HTTPS metadata enabled: {behaviour.ServiceMetadataHttpsGetEnabled ?? string.Empty}");
            }

            if (behaviour.HasServiceDebug)
            {
                Console.WriteLine("  Service debug: True");
                Console.WriteLine($"  Include exception detail in faults: {behaviour.IncludeExceptionDetailInFaults ?? string.Empty}");
            }

            if (behaviour.HasServiceThrottling)
            {
                Console.WriteLine("  Service throttling: True");
                Console.WriteLine($"  Max concurrent calls: {behaviour.MaxConcurrentCalls ?? string.Empty}");
                Console.WriteLine($"  Max concurrent sessions: {behaviour.MaxConcurrentSessions ?? string.Empty}");
                Console.WriteLine($"  Max concurrent instances: {behaviour.MaxConcurrentInstances ?? string.Empty}");
            }

            if (behaviour.HasWebHttp)
            {
                Console.WriteLine("  Web HTTP: True");
            }
        }

        Console.WriteLine();
    }

    private static void WriteConfigurationFiles(ScanResult result)
    {
        Console.WriteLine("Configuration files discovered:");

        if (result.ConfigFiles.Count == 0)
        {
            Console.WriteLine("- None");
            Console.WriteLine();
            return;
        }

        foreach (var configFile in result.ConfigFiles)
        {
            Console.WriteLine($"- {configFile.FilePath}");
            Console.WriteLine($"  App settings: {configFile.AppSettingsCount}");
            Console.WriteLine($"  Connection strings: {configFile.ConnectionStringsCount}");
            Console.WriteLine($"  Custom sections: {configFile.CustomSectionCount}");
        }

        Console.WriteLine();
    }

    private static void WriteLegacyAspNetArtifacts(ScanResult result)
    {
        Console.WriteLine("Legacy ASP.NET artifacts discovered:");

        if (result.LegacyAspNetArtifacts.Count == 0)
        {
            Console.WriteLine("- None");
            Console.WriteLine();
            return;
        }

        foreach (var artifact in result.LegacyAspNetArtifacts)
        {
            Console.WriteLine($"- {artifact.Kind}: {artifact.Name ?? Path.GetFileName(artifact.FilePath)}");
            Console.WriteLine($"  File: {artifact.FilePath}");
        }

        Console.WriteLine();
    }

    private static void WriteModernisationHints(ScanResult result)
    {
        Console.WriteLine("Modernisation hints discovered:");

        if (result.ModernisationHints.Count == 0)
        {
            Console.WriteLine("- None");
            Console.WriteLine();
            return;
        }

        foreach (var hint in result.ModernisationHints)
        {
            Console.WriteLine($"- [{hint.Severity}] {hint.Area}: {hint.Finding}");
        }

        Console.WriteLine();
    }

    private static void WriteModernisationReviewSummary(ScanResult result)
    {
        Console.WriteLine("Modernisation review summary:");

        if (result.ModernisationReviewAreas.Count == 0)
        {
            Console.WriteLine("- None");
            Console.WriteLine();
            return;
        }

        var priority = 1;

        foreach (var reviewArea in result.ModernisationReviewAreas)
        {
            Console.WriteLine($"- {priority}. {reviewArea.Area}");
            Console.WriteLine($"  Highest severity: {reviewArea.HighestSeverity}");
            Console.WriteLine($"  Risks: {reviewArea.RiskCount}");
            Console.WriteLine($"  Warnings: {reviewArea.WarningCount}");
            Console.WriteLine($"  Info: {reviewArea.InfoCount}");
            Console.WriteLine($"  Summary: {reviewArea.Summary}");

            priority++;
        }

        Console.WriteLine();
    }

    private static void WriteSolutions(ScanResult result)
    {
        Console.WriteLine("Solutions discovered:");

        if (result.Solutions.Count == 0)
        {
            Console.WriteLine("- None");
            return;
        }

        foreach (var solution in result.Solutions)
        {
            Console.WriteLine($"- {solution.Name}");
            Console.WriteLine($"  Solution file: {solution.SolutionFilePath}");
            Console.WriteLine($"  Projects: {solution.ProjectFilePaths.Count}");
        }
    }
}
