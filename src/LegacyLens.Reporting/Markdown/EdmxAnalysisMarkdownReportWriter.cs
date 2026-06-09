using System.Text;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Markdown;

public sealed class EdmxAnalysisMarkdownReportWriter
{
    public void Write(string outputPath, EdmxAnalysisReport report)
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
        WriteEdmxFiles(markdown, report);
        WriteNamespaceUris(markdown, report);
        WriteUpgradeConcerns(markdown, report);
        WriteConceptualModelDetails(markdown, report);
        WriteStorageModelDetails(markdown, report);
        WriteAssociations(markdown, report);
        WriteFunctionImportsAndStoreFunctions(markdown, report);
        WriteMappingDetails(markdown, report);
        WriteCompanionGeneratedFiles(markdown, report);
        WriteSuggestedReviewOrder(markdown);
        WriteNotesAndLimitations(markdown);

        File.WriteAllText(outputPath, markdown.ToString());
    }

    private static void WriteHeader(StringBuilder markdown)
    {
        markdown.AppendLine("# EDMX Analysis");
        markdown.AppendLine();
    }

    private static void WriteSummary(StringBuilder markdown, EdmxAnalysisReport report)
    {
        markdown.AppendLine("## Summary");
        markdown.AppendLine();
        markdown.AppendLine("This report is based on static EDMX XML inspection. It highlights EF EDMX model contents and possible EF Core migration review points. It does not validate the model against a database.");
        markdown.AppendLine();

        if (report.Models.Count == 0)
        {
            markdown.AppendLine("No EDMX files were discovered.");
            markdown.AppendLine();
        }

        markdown.AppendLine("| Item | Count |");
        markdown.AppendLine("|---|---:|");
        markdown.AppendLine($"| EDMX files discovered | {report.Models.Count} |");
        markdown.AppendLine($"| Files with conceptual model | {report.Models.Count(model => model.HasConceptualModel)} |");
        markdown.AppendLine($"| Files with storage model | {report.Models.Count(model => model.HasStorageModel)} |");
        markdown.AppendLine($"| Files with mapping model | {report.Models.Count(model => model.HasMappingModel)} |");
        markdown.AppendLine($"| Files with designer metadata | {report.Models.Count(model => model.HasDesignerMetadata)} |");
        markdown.AppendLine($"| Files with parse errors | {report.Models.Count(model => !string.IsNullOrWhiteSpace(model.ParseError))} |");
        markdown.AppendLine($"| Upgrade concerns | {report.Models.Sum(model => model.UpgradeConcerns.Count)} |");
        markdown.AppendLine();
    }

    private static void WriteAnalysisScope(StringBuilder markdown)
    {
        markdown.AppendLine("## Analysis Scope");
        markdown.AppendLine();
        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine("| Analysis mode | Static / no-build |");
        markdown.AppendLine("| Database connection attempted | No |");
        markdown.AppendLine("| EDMX validated against live database | No |");
        markdown.AppendLine("| EF Core model generated | No |");
        markdown.AppendLine("| Automatic conversion performed | No |");
        markdown.AppendLine("| Compatibility guarantee | No |");
        markdown.AppendLine();
    }

    private static void WriteEdmxFiles(StringBuilder markdown, EdmxAnalysisReport report)
    {
        markdown.AppendLine("## EDMX Files");
        markdown.AppendLine();

        if (report.Models.Count == 0)
        {
            markdown.AppendLine("No EDMX files were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Project | EDMX File | Conceptual Model | Storage Model | Mapping Model | Designer Metadata | Parse Status |");
        markdown.AppendLine("|---|---|---|---|---|---|---|");

        foreach (var model in report.Models.OrderBy(model => model.ProjectName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(model => model.FilePath, StringComparer.OrdinalIgnoreCase))
        {
            markdown.AppendLine(
                $"| {Escape(model.ProjectName ?? "Unknown")} | `{Escape(model.FilePath)}` | {YesNo(model.HasConceptualModel)} | {YesNo(model.HasStorageModel)} | {YesNo(model.HasMappingModel)} | {YesNo(model.HasDesignerMetadata)} | {ParseStatus(model)} |");
        }

        markdown.AppendLine();

        foreach (var model in report.Models.Where(model => !string.IsNullOrWhiteSpace(model.ParseError)))
        {
            markdown.AppendLine($"- `{Escape(model.FilePath)}` parse error: {Escape(model.ParseError)}");
        }

        if (report.Models.Any(model => !string.IsNullOrWhiteSpace(model.ParseError)))
        {
            markdown.AppendLine();
        }
    }

    private static void WriteNamespaceUris(StringBuilder markdown, EdmxAnalysisReport report)
    {
        markdown.AppendLine("## Namespace URIs");
        markdown.AppendLine();

        var namespaceUris = report.Models
            .SelectMany(model => model.NamespaceUris)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (namespaceUris.Length == 0)
        {
            markdown.AppendLine("No XML namespace URIs were discovered.");
            markdown.AppendLine();
            return;
        }

        foreach (var namespaceUri in namespaceUris)
        {
            markdown.AppendLine($"- `{Escape(namespaceUri)}`");
        }

        markdown.AppendLine();
    }

    private static void WriteUpgradeConcerns(StringBuilder markdown, EdmxAnalysisReport report)
    {
        markdown.AppendLine("## Upgrade Concerns");
        markdown.AppendLine();

        var rows = report.Models
            .SelectMany(model => model.UpgradeConcerns.Select(concern => new
            {
                Model = model,
                Concern = concern
            }))
            .OrderBy(row => row.Concern.Severity)
            .ThenBy(row => row.Model.ProjectName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Model.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Concern.Concern, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rows.Length == 0)
        {
            markdown.AppendLine("No EDMX upgrade concerns were produced.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Severity | Project | EDMX File | Concern | Evidence | Recommendation |");
        markdown.AppendLine("|---|---|---|---|---|---|");

        foreach (var row in rows)
        {
            markdown.AppendLine(
                $"| {row.Concern.Severity} | {Escape(row.Model.ProjectName ?? "Unknown")} | `{Escape(row.Model.FilePath)}` | {Escape(row.Concern.Concern)} | {Escape(row.Concern.Evidence)} | {Escape(row.Concern.Recommendation)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteConceptualModelDetails(StringBuilder markdown, EdmxAnalysisReport report)
    {
        markdown.AppendLine("## Conceptual Model Details");
        markdown.AppendLine();

        var modelsWithConceptualDetails = report.Models
            .Where(model => model.ConceptualEntities.Count > 0 || model.ConceptualEntitySets.Count > 0 || model.ComplexTypes.Count > 0)
            .ToArray();

        if (modelsWithConceptualDetails.Length == 0)
        {
            markdown.AppendLine("No conceptual model details were discovered.");
            markdown.AppendLine();
            return;
        }

        foreach (var model in modelsWithConceptualDetails)
        {
            markdown.AppendLine($"### {Escape(Path.GetFileName(model.FilePath))}");
            markdown.AppendLine();

            if (model.ConceptualEntities.Count > 0)
            {
                markdown.AppendLine("| Entity | Entity Set | Key Properties | Property Count | Navigation Properties |");
                markdown.AppendLine("|---|---|---:|---:|---:|");

                foreach (var entity in model.ConceptualEntities.OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase))
                {
                    markdown.AppendLine(
                        $"| {Escape(entity.Name)} | {Escape(entity.EntitySet)} | {Escape(string.Join(", ", entity.KeyProperties))} | {entity.PropertyCount} | {entity.NavigationPropertyCount} |");
                }

                markdown.AppendLine();
            }

            if (model.ConceptualEntitySets.Count > 0)
            {
                markdown.AppendLine($"Entity sets: {Escape(string.Join(", ", model.ConceptualEntitySets))}");
                markdown.AppendLine();
            }

            if (model.ComplexTypes.Count > 0)
            {
                markdown.AppendLine($"Complex types: {Escape(string.Join(", ", model.ComplexTypes))}");
                markdown.AppendLine();
            }
        }
    }

    private static void WriteStorageModelDetails(StringBuilder markdown, EdmxAnalysisReport report)
    {
        markdown.AppendLine("## Storage Model Details");
        markdown.AppendLine();

        var modelsWithStorageDetails = report.Models
            .Where(model => model.StorageEntities.Count > 0 || model.StoreFunctions.Count > 0)
            .ToArray();

        if (modelsWithStorageDetails.Length == 0)
        {
            markdown.AppendLine("No storage model details were discovered.");
            markdown.AppendLine();
            return;
        }

        foreach (var model in modelsWithStorageDetails)
        {
            markdown.AppendLine($"### {Escape(Path.GetFileName(model.FilePath))}");
            markdown.AppendLine();

            if (model.StorageEntities.Count > 0)
            {
                markdown.AppendLine("| Entity | Entity Set | Schema | Table / View | Column Count | Defining Query |");
                markdown.AppendLine("|---|---|---|---|---:|---|");

                foreach (var entity in model.StorageEntities.OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase))
                {
                    markdown.AppendLine(
                        $"| {Escape(entity.Name)} | {Escape(entity.EntitySet)} | {Escape(entity.Schema)} | {Escape(entity.TableOrView)} | {entity.ColumnCount} | {YesNo(entity.HasDefiningQuery)} |");
                }

                markdown.AppendLine();
            }

            if (model.StoreFunctions.Count > 0)
            {
                WriteStoreFunctionsTable(markdown, model.StoreFunctions);
            }
        }
    }

    private static void WriteAssociations(StringBuilder markdown, EdmxAnalysisReport report)
    {
        markdown.AppendLine("## Associations");
        markdown.AppendLine();

        var associations = report.Models
            .SelectMany(model => model.Associations)
            .OrderBy(association => association.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (associations.Length == 0)
        {
            markdown.AppendLine("No associations were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Association | From Role | To Role | Multiplicity |");
        markdown.AppendLine("|---|---|---|---|");

        foreach (var association in associations)
        {
            markdown.AppendLine(
                $"| {Escape(association.Name)} | {Escape(association.FromRole)} | {Escape(association.ToRole)} | {Escape(association.Multiplicity)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteFunctionImportsAndStoreFunctions(StringBuilder markdown, EdmxAnalysisReport report)
    {
        markdown.AppendLine("## Function Imports and Store Functions");
        markdown.AppendLine();

        var functionImports = report.Models
            .SelectMany(model => model.FunctionImports)
            .OrderBy(functionImport => functionImport.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var storeFunctions = report.Models
            .SelectMany(model => model.StoreFunctions)
            .OrderBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (functionImports.Length == 0 && storeFunctions.Length == 0)
        {
            markdown.AppendLine("No function imports or store functions were discovered.");
            markdown.AppendLine();
            return;
        }

        if (functionImports.Length > 0)
        {
            markdown.AppendLine("| Function Import | Return Type | Store Function |");
            markdown.AppendLine("|---|---|---|");

            foreach (var functionImport in functionImports)
            {
                markdown.AppendLine(
                    $"| {Escape(functionImport.Name)} | {Escape(functionImport.ReturnType)} | {Escape(functionImport.StoreFunction)} |");
            }

            markdown.AppendLine();
        }

        if (storeFunctions.Length > 0)
        {
            WriteStoreFunctionsTable(markdown, storeFunctions);
        }
    }

    private static void WriteMappingDetails(StringBuilder markdown, EdmxAnalysisReport report)
    {
        markdown.AppendLine("## Mapping Details");
        markdown.AppendLine();

        var mappingFragments = report.Models
            .SelectMany(model => model.MappingFragments)
            .OrderBy(fragment => fragment.EntitySet ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(fragment => fragment.EntityType ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(fragment => fragment.StoreEntitySet ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (mappingFragments.Length == 0 && report.Models.Count == 0)
        {
            markdown.AppendLine("No mapping details were discovered.");
            markdown.AppendLine();
            return;
        }

        if (mappingFragments.Length > 0)
        {
            markdown.AppendLine("| Entity Set | Entity Type | Store Entity Set | Scalar Properties |");
            markdown.AppendLine("|---|---|---|---:|");

            foreach (var fragment in mappingFragments)
            {
                markdown.AppendLine(
                    $"| {Escape(fragment.EntitySet)} | {Escape(fragment.EntityType)} | {Escape(fragment.StoreEntitySet)} | {fragment.ScalarPropertyCount} |");
            }

            markdown.AppendLine();
        }
        else
        {
            markdown.AppendLine("No entity mapping fragments were discovered.");
            markdown.AppendLine();
        }

        markdown.AppendLine("| Mapping Signal | Count |");
        markdown.AppendLine("|---|---:|");
        markdown.AppendLine($"| Modification function mappings | {report.Models.Sum(model => model.ModificationFunctionMappingCount)} |");
        markdown.AppendLine($"| Query views | {report.Models.Sum(model => model.QueryViewCount)} |");
        markdown.AppendLine($"| Defining queries | {report.Models.Sum(model => model.DefiningQueryCount)} |");
        markdown.AppendLine();
    }

    private static void WriteCompanionGeneratedFiles(StringBuilder markdown, EdmxAnalysisReport report)
    {
        markdown.AppendLine("## Companion Generated Files");
        markdown.AppendLine();

        var rows = report.Models
            .SelectMany(model => model.CompanionFiles.Select(file => new
            {
                Model = model,
                File = file
            }))
            .OrderBy(row => row.Model.ProjectName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Model.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.File.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.File.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rows.Length == 0)
        {
            markdown.AppendLine("No companion generated or design-time files were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Project | EDMX File | Kind | File | Evidence |");
        markdown.AppendLine("|---|---|---|---|---|");

        foreach (var row in rows)
        {
            markdown.AppendLine(
                $"| {Escape(row.Model.ProjectName ?? "Unknown")} | `{Escape(Path.GetFileName(row.Model.FilePath))}` | {Escape(row.File.Kind)} | `{Escape(row.File.FilePath)}` | {Escape(row.File.Evidence)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteSuggestedReviewOrder(StringBuilder markdown)
    {
        markdown.AppendLine("## Suggested Review Order");
        markdown.AppendLine();
        markdown.AppendLine("1. Review EDMX files with parse errors first.");
        markdown.AppendLine("2. Review EDMX files with function imports, store functions, modification function mappings, query views, or defining queries.");
        markdown.AppendLine("3. Review conceptual entities, keys, associations, and navigation properties.");
        markdown.AppendLine("4. Review storage entity sets, tables, views, columns, and defining queries.");
        markdown.AppendLine("5. Review companion generated code and T4 templates before changing or deleting EDMX files.");
        markdown.AppendLine();
    }

    private static void WriteNotesAndLimitations(StringBuilder markdown)
    {
        markdown.AppendLine("## Notes and Limitations");
        markdown.AppendLine();
        markdown.AppendLine("- This report is based on static EDMX XML inspection only.");
        markdown.AppendLine("- LegacyLens.NET did not connect to a database.");
        markdown.AppendLine("- LegacyLens.NET did not validate the EDMX against a live database or schema.");
        markdown.AppendLine("- LegacyLens.NET did not generate EF Core models.");
        markdown.AppendLine("- LegacyLens.NET did not automatically convert EDMX models to EF Core.");
        markdown.AppendLine("- LegacyLens.NET did not run NuGet restore.");
        markdown.AppendLine("- LegacyLens.NET did not build the solution.");
        markdown.AppendLine("- LegacyLens.NET did not guarantee migration compatibility.");
        markdown.AppendLine("- No direct EF Core EDMX equivalent should be assumed.");
        markdown.AppendLine("- Findings should be verified by the development team before migration decisions are made.");
        markdown.AppendLine();
    }

    private static void WriteStoreFunctionsTable(
        StringBuilder markdown,
        IEnumerable<EdmxStoreFunction> storeFunctions)
    {
        markdown.AppendLine("| Store Function | Schema | Is Composable | Parameter Count |");
        markdown.AppendLine("|---|---|---|---:|");

        foreach (var function in storeFunctions.OrderBy(function => function.Name, StringComparer.OrdinalIgnoreCase))
        {
            markdown.AppendLine(
                $"| {Escape(function.Name)} | {Escape(function.Schema)} | {YesNo(function.IsComposable)} | {function.ParameterCount} |");
        }

        markdown.AppendLine();
    }

    private static string ParseStatus(DiscoveredEdmxModel model) =>
        string.IsNullOrWhiteSpace(model.ParseError)
            ? "Parsed"
            : "Parse error";

    private static string YesNo(bool value) => value ? "Yes" : "No";

    private static string YesNo(bool? value)
    {
        return value switch
        {
            true => "Yes",
            false => "No",
            null => "Unknown"
        };
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}