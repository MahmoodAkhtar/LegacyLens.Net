using System.Text;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Markdown;

public sealed class InterfaceInventoryMarkdownReportWriter
{
    public void Write(string outputPath, InterfaceInventoryReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(report);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var markdown = new StringBuilder();

        WriteHeader(markdown);
        WriteSummary(markdown, report);
        WriteAnalysisScope(markdown);
        WriteReviewFindings(markdown, report);
        WritePossibleExtensionPoints(markdown, report);
        WriteInterfaceOverview(markdown, report);
        WriteImplementations(markdown, report);
        WriteConsumers(markdown, report);
        WriteRegistrationEvidence(markdown, report);
        WriteInterfaceDetails(markdown, report);
        WriteNotesAndLimitations(markdown);

        File.WriteAllText(outputPath, markdown.ToString());
    }

    private static void WriteHeader(StringBuilder markdown)
    {
        markdown.AppendLine("# Interface Inventory Report");
        markdown.AppendLine();
    }

    private static void WriteSummary(StringBuilder markdown, InterfaceInventoryReport report)
    {
        markdown.AppendLine("## Summary");
        markdown.AppendLine();
        markdown.AppendLine("This report is based on static C# source and visible configuration/XML inspection. It highlights available abstractions, likely extension points, implementations, consumers, and DI/IoC wiring evidence that may need review. It does not prove runtime usage, active registration, or completeness.");
        markdown.AppendLine();
        markdown.AppendLine("| Item | Count |");
        markdown.AppendLine("|---|---:|");
        markdown.AppendLine($"| Projects analysed | {report.Interfaces.Select(item => item.ProjectName).Concat(report.Consumers.Select(item => item.ProjectName)).Distinct(StringComparer.OrdinalIgnoreCase).Count()} |");
        markdown.AppendLine($"| C# source files analysed | {report.SourceFileCount} |");
        markdown.AppendLine($"| Configuration/XML files inspected | {report.ConfigurationFileCount} |");
        markdown.AppendLine($"| Interfaces discovered | {report.Interfaces.Count} |");
        markdown.AppendLine($"| Static implementations discovered | {report.Implementations.Count} |");
        markdown.AppendLine($"| Static consumers discovered | {report.Consumers.Count} |");
        markdown.AppendLine($"| Registration evidence items discovered | {report.Registrations.Count} |");
        markdown.AppendLine($"| Interfaces with multiple implementations | {report.MultipleImplementationInterfaceCount} |");
        markdown.AppendLine($"| Interfaces with no static implementation found | {report.MissingStaticImplementationCount} |");
        markdown.AppendLine($"| Interfaces with no static consumer found | {report.MissingStaticConsumerCount} |");
        markdown.AppendLine($"| Dynamic/configuration-driven wiring items requiring review | {report.DynamicOrConfigurationDrivenWiringCount} |");
        markdown.AppendLine();
    }

    private static void WriteAnalysisScope(StringBuilder markdown)
    {
        markdown.AppendLine("## Analysis Scope");
        markdown.AppendLine();
        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine("| Analysis mode | Static / no-build |");
        markdown.AppendLine("| MSBuild compilation performed | No |");
        markdown.AppendLine("| NuGet restore performed | No |");
        markdown.AppendLine("| Runtime dependency injection resolved | No |");
        markdown.AppendLine("| Assemblies loaded | No |");
        markdown.AppendLine("| Configuration transforms applied | No |");
        markdown.AppendLine("| Completeness guarantee | No |");
        markdown.AppendLine();
    }

    private static void WriteReviewFindings(StringBuilder markdown, InterfaceInventoryReport report)
    {
        markdown.AppendLine("## Review Findings");
        markdown.AppendLine();

        if (report.Findings.Count == 0)
        {
            markdown.AppendLine("No interface inventory findings were discovered by the MVP rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Severity | Interface | Finding | Evidence | Recommendation |");
        markdown.AppendLine("|---|---|---|---|---|");

        foreach (var finding in report.Findings)
        {
            markdown.AppendLine($"| {finding.Severity} | {MarkdownTableCell.Code(finding.InterfaceName)} | {Escape(finding.Finding)} | {MarkdownTableCell.Evidence(finding.Evidence)} | {Escape(finding.Recommendation)} |");
        }

        markdown.AppendLine();
    }

    private static void WritePossibleExtensionPoints(StringBuilder markdown, InterfaceInventoryReport report)
    {
        markdown.AppendLine("## Possible Extension Points");
        markdown.AppendLine();

        var rows = report.Interfaces
            .Where(item => item.IsPossibleExtensionPoint)
            .OrderByDescending(item => CountImplementations(report, item.Name))
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rows.Length == 0)
        {
            markdown.AppendLine("No likely extension-point interfaces were identified by the MVP naming and implementation-count rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Interface | Project | Likely Role | Implementations | Consumers | Registration Evidence | Why Review |");
        markdown.AppendLine("|---|---|---|---:|---:|---:|---|");

        foreach (var item in rows)
        {
            var implementationCount = CountImplementations(report, item.Name);
            var consumerCount = CountConsumers(report, item.Name);
            var registrationCount = CountRegistrations(report, item.Name);
            var whyReview = implementationCount > 1
                ? "Multiple implementations may indicate a strategy, plugin, or replaceable service boundary."
                : "Naming suggests this interface may represent a useful abstraction or extension seam.";

            markdown.AppendLine($"| {MarkdownTableCell.Code(item.Name)} | {Escape(item.ProjectName)} | {Escape(item.LikelyRole)} | {implementationCount} | {consumerCount} | {registrationCount} | {Escape(whyReview)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteInterfaceOverview(StringBuilder markdown, InterfaceInventoryReport report)
    {
        markdown.AppendLine("## Interface Overview");
        markdown.AppendLine();

        if (report.Interfaces.Count == 0)
        {
            markdown.AppendLine("No source-defined interfaces were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Interface | Project | Likely Role | Inherits | Implementations | Consumers | Registrations | Source Path | Line |");
        markdown.AppendLine("|---|---|---|---|---:|---:|---:|---|---:|");

        foreach (var item in report.Interfaces)
        {
            markdown.AppendLine($"| {MarkdownTableCell.Code(item.Name)} | {Escape(item.ProjectName)} | {Escape(item.LikelyRole)} | {Escape(FormatList(item.InheritedInterfaces))} | {CountImplementations(report, item.Name)} | {CountConsumers(report, item.Name)} | {CountRegistrations(report, item.Name)} | {MarkdownTableCell.Code(item.SourcePath)} | {item.LineNumber} |");
        }

        markdown.AppendLine();
    }

    private static void WriteImplementations(StringBuilder markdown, InterfaceInventoryReport report)
    {
        markdown.AppendLine("## Static Implementations");
        markdown.AppendLine();

        if (report.Implementations.Count == 0)
        {
            markdown.AppendLine("No static interface implementations were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Interface | Implementation Type | Project | Source Path | Line | Evidence |");
        markdown.AppendLine("|---|---|---|---|---:|---|");

        foreach (var implementation in report.Implementations)
        {
            markdown.AppendLine($"| {MarkdownTableCell.Code(implementation.InterfaceName)} | {MarkdownTableCell.Code(implementation.ImplementationType)} | {Escape(implementation.ProjectName)} | {MarkdownTableCell.Code(implementation.SourcePath)} | {implementation.LineNumber} | {MarkdownTableCell.Code(implementation.Evidence)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteConsumers(StringBuilder markdown, InterfaceInventoryReport report)
    {
        markdown.AppendLine("## Static Consumers");
        markdown.AppendLine();

        if (report.Consumers.Count == 0)
        {
            markdown.AppendLine("No static interface consumers were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Interface | Consumer Type | Consumer Kind | Project | Source Path | Line | Evidence |");
        markdown.AppendLine("|---|---|---|---|---|---:|---|");

        foreach (var consumer in report.Consumers)
        {
            markdown.AppendLine($"| {MarkdownTableCell.Code(consumer.InterfaceName)} | {MarkdownTableCell.Code(consumer.ConsumerType)} | {Escape(ToLabel(consumer.Kind))} | {Escape(consumer.ProjectName)} | {MarkdownTableCell.Code(consumer.SourcePath)} | {consumer.LineNumber} | {MarkdownTableCell.Code(consumer.Evidence)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteRegistrationEvidence(StringBuilder markdown, InterfaceInventoryReport report)
    {
        markdown.AppendLine("## Registration and Wiring Evidence");
        markdown.AppendLine();

        if (report.Registrations.Count == 0)
        {
            markdown.AppendLine("No DI/IoC registration evidence was discovered by the MVP rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Interface | Implementation | Kind | Requires Review | Project | Source Path | Line | Evidence | Notes |");
        markdown.AppendLine("|---|---|---|---|---|---|---:|---|---|");

        foreach (var registration in report.Registrations)
        {
            markdown.AppendLine($"| {MarkdownTableCell.Code(registration.InterfaceName)} | {MarkdownTableCell.Code(registration.ImplementationType ?? "Unknown")} | {Escape(ToLabel(registration.Kind))} | {FormatBoolean(registration.RequiresReview)} | {Escape(registration.ProjectName)} | {MarkdownTableCell.Code(registration.SourcePath)} | {registration.LineNumber} | {MarkdownTableCell.Code(registration.Evidence)} | {Escape(registration.Notes)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteInterfaceDetails(StringBuilder markdown, InterfaceInventoryReport report)
    {
        markdown.AppendLine("## Interface Details");
        markdown.AppendLine();

        if (report.Interfaces.Count == 0)
        {
            markdown.AppendLine("No interface details are available.");
            markdown.AppendLine();
            return;
        }

        foreach (var item in report.Interfaces)
        {
            markdown.AppendLine($"### {Escape(item.Name)}");
            markdown.AppendLine();
            markdown.AppendLine($"- Project: {Escape(item.ProjectName)}");
            markdown.AppendLine($"- Full name: {MarkdownTableCell.Code(item.FullName)}");
            markdown.AppendLine($"- Likely role: {Escape(item.LikelyRole)}");
            markdown.AppendLine($"- Possible extension point: {FormatBoolean(item.IsPossibleExtensionPoint)}");
            markdown.AppendLine($"- Source: {MarkdownTableCell.Code(item.SourcePath)} line {item.LineNumber}");
            markdown.AppendLine($"- Inherited interfaces: {Escape(FormatList(item.InheritedInterfaces))}");
            markdown.AppendLine($"- Static implementations: {CountImplementations(report, item.Name)}");
            markdown.AppendLine($"- Static consumers: {CountConsumers(report, item.Name)}");
            markdown.AppendLine($"- Registration evidence items: {CountRegistrations(report, item.Name)}");
            markdown.AppendLine();
        }
    }

    private static void WriteNotesAndLimitations(StringBuilder markdown)
    {
        markdown.AppendLine("## Notes and Limitations");
        markdown.AppendLine();
        markdown.AppendLine("- LegacyLens.NET did not build the solution, restore NuGet packages, execute code, or load assemblies.");
        markdown.AppendLine("- Static implementation evidence does not prove an implementation is registered or used at runtime.");
        markdown.AppendLine("- Missing static consumers or implementations do not prove an interface is unused; reflection, generated code, external assemblies, container modules, tests, or configuration-driven wiring may exist.");
        markdown.AppendLine("- DI/IoC registration evidence is based on visible source and configuration/XML patterns only. The report does not prove that a registration is active after transforms, environment-specific configuration, or runtime module loading.");
        markdown.AppendLine("- Dynamic service-locator, resolver, or custom factory evidence should be treated as a review prompt, not as a complete runtime dependency graph.");
    }

    private static int CountImplementations(InterfaceInventoryReport report, string interfaceName)
    {
        return report.Implementations.Count(implementation =>
            implementation.InterfaceName.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));
    }

    private static int CountConsumers(InterfaceInventoryReport report, string interfaceName)
    {
        return report.Consumers.Count(consumer =>
            consumer.InterfaceName.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));
    }

    private static int CountRegistrations(InterfaceInventoryReport report, string interfaceName)
    {
        return report.Registrations.Count(registration =>
            registration.InterfaceName.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatList(IReadOnlyCollection<string> values)
    {
        return values.Count == 0
            ? "None"
            : string.Join(", ", values.Select(MarkdownTableCell.Code));
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "Yes" : "No";
    }

    private static string ToLabel(InterfaceConsumerKind kind)
    {
        return kind switch
        {
            InterfaceConsumerKind.ConstructorParameter => "constructor parameter",
            InterfaceConsumerKind.MethodParameter => "method parameter",
            InterfaceConsumerKind.ReturnType => "return type",
            InterfaceConsumerKind.LocalVariable => "local variable",
            InterfaceConsumerKind.GenericOrCollectionUsage => "generic or collection usage",
            InterfaceConsumerKind.EndpointDelegateParameter => "endpoint delegate parameter",
            InterfaceConsumerKind.ServiceLocator => "service locator",
            InterfaceConsumerKind.FactoryOrResolver => "factory or resolver",
            _ => kind.ToString()
        };
    }

    private static string ToLabel(InterfaceRegistrationKind kind)
    {
        return kind switch
        {
            InterfaceRegistrationKind.MicrosoftDependencyInjection => "Microsoft DI",
            InterfaceRegistrationKind.CastleWindsor => "Castle Windsor",
            InterfaceRegistrationKind.CastleWindsorXml => "Castle Windsor XML",
            InterfaceRegistrationKind.SpringNetXml => "Spring.NET XML",
            InterfaceRegistrationKind.UnityXml => "Unity XML",
            InterfaceRegistrationKind.EnterpriseLibraryObjectBuilder => "Enterprise Library/ObjectBuilder",
            InterfaceRegistrationKind.CommonServiceLocator => "Common Service Locator",
            InterfaceRegistrationKind.AspNetDependencyResolver => "ASP.NET dependency resolver",
            InterfaceRegistrationKind.CustomObjectFactory => "custom object factory",
            InterfaceRegistrationKind.UnknownDynamicWiring => "unknown dynamic wiring",
            _ => kind.ToString()
        };
    }
    private static string Escape(string? value) => MarkdownTableCell.Escape(value);
}
