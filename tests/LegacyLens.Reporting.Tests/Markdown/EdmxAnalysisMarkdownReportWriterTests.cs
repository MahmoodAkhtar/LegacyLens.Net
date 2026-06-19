using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class EdmxAnalysisMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public EdmxAnalysisMarkdownReportWriterTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.EdmxAnalysisMarkdownReportWriterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_WhenOutputPathIsEmpty_ThrowsArgumentException()
    {
        var writer = new EdmxAnalysisMarkdownReportWriter();

        var exception = Assert.Throws<ArgumentException>(() =>
            writer.Write(
                string.Empty,
                new EdmxAnalysisReport(Array.Empty<DiscoveredEdmxModel>())));

        Assert.Equal("outputPath", exception.ParamName);
    }

    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");
        var writer = new EdmxAnalysisMarkdownReportWriter();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            writer.Write(outputPath, null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_CreatesOutputDirectoryWhenItDoesNotExist()
    {
        var outputPath = Path.Combine(
            _tempDirectory,
            "nested",
            "reports",
            "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(
            outputPath,
            new EdmxAnalysisReport(Array.Empty<DiscoveredEdmxModel>()));

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void Write_WhenNoEdmxFilesExist_WritesValidEmptyReport()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var report = new EdmxAnalysisReport(Array.Empty<DiscoveredEdmxModel>());

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# EDMX Analysis", markdown);
        Assert.Contains("## Summary", markdown);
        Assert.Contains("No EDMX files were discovered.", markdown);
        Assert.Contains("| EDMX files discovered | 0 |", markdown);
        Assert.Contains("| Files with parse errors | 0 |", markdown);
        Assert.Contains("| Analysis mode | Static / no-build |", markdown);
        Assert.Contains("| Database connection attempted | No |", markdown);
        Assert.Contains("| EDMX validated against live database | No |", markdown);
        Assert.Contains("| EF Core model generated | No |", markdown);
        Assert.Contains("| Compatibility guarantee | No |", markdown);
    }

    [Fact]
    public void Write_IncludesAnalysisScope()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Analysis Scope", markdown);
        Assert.Contains("| Analysis mode | Static / no-build |", markdown);
        Assert.Contains("| Database connection attempted | No |", markdown);
        Assert.Contains("| EDMX validated against live database | No |", markdown);
        Assert.Contains("| EF Core model generated | No |", markdown);
        Assert.Contains("| Automatic conversion performed | No |", markdown);
        Assert.Contains("| Compatibility guarantee | No |", markdown);
    }

    [Fact]
    public void Write_IncludesSummaryCounts()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Summary", markdown);
        Assert.Contains("| EDMX files discovered | 1 |", markdown);
        Assert.Contains("| Files with conceptual model | 1 |", markdown);
        Assert.Contains("| Files with storage model | 1 |", markdown);
        Assert.Contains("| Files with mapping model | 1 |", markdown);
        Assert.Contains("| Files with designer metadata | 1 |", markdown);
        Assert.Contains("| Files with parse errors | 0 |", markdown);
        Assert.Contains("| Upgrade concerns | 3 |", markdown);
    }

    [Fact]
    public void Write_IncludesEdmxFilesTable()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## EDMX Files", markdown);
        Assert.Contains("| Project | EDMX File | Conceptual Model | Storage Model | Mapping Model | Designer Metadata | Parse Status |", markdown);
        Assert.Contains("| SampleLegacyApp.Data | `C:\\Repo\\SampleLegacyApp.Data\\LegacyModel.edmx` | Yes | Yes | Yes | Yes | Parsed |", markdown);
    }

    [Fact]
    public void Write_IncludesNamespaceUris()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Namespace URIs", markdown);
        Assert.Contains("http://schemas.microsoft.com/ado/2009/11/edmx", markdown);
        Assert.Contains("http://schemas.microsoft.com/ado/2009/11/edm", markdown);
        Assert.Contains("http://schemas.microsoft.com/ado/2009/11/edm/ssdl", markdown);
        Assert.Contains("http://schemas.microsoft.com/ado/2009/11/mapping/cs", markdown);
    }

    [Fact]
    public void Write_IncludesUpgradeConcerns()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Upgrade Concerns", markdown);
        Assert.Contains("| Severity | Project | EDMX File | Concern | Evidence | Recommendation |", markdown);
        Assert.Contains("| High | SampleLegacyApp.Data | `C:\\Repo\\SampleLegacyApp.Data\\LegacyModel.edmx` | EDMX model requires migration decision | `LegacyModel.edmx is an Entity Framework EDMX model. No direct EF Core EDMX equivalent exists.` | Review whether to scaffold a new EF Core model from the database, keep EF6 isolated, or manually map equivalent entities and relationships. |", markdown);
        Assert.Contains("| Medium | SampleLegacyApp.Data | `C:\\Repo\\SampleLegacyApp.Data\\LegacyModel.edmx` | Stored procedure or function mapping requires review | `Function imports, store functions, or modification function mappings were found.` | Review stored procedure usage and decide whether EF Core stored procedure support, raw SQL, or explicit repository methods are needed. |", markdown);
        Assert.Contains("| Low | SampleLegacyApp.Data | `C:\\Repo\\SampleLegacyApp.Data\\LegacyModel.edmx` | Complex types require manual mapping review | `1 complex type(s) found.` | Review whether EF Core owned entity types or explicit value-object mappings are appropriate. |", markdown);
    }

    [Fact]
    public void Write_IncludesConceptualModelDetails()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Conceptual Model Details", markdown);
        Assert.Contains("### LegacyModel.edmx", markdown);
        Assert.Contains("| Entity | Entity Set | Key Properties | Property Count | Navigation Properties |", markdown);
        Assert.Contains("| Customer | Customers | Id | 2 | 1 |", markdown);
        Assert.Contains("Complex types: Address", markdown);
    }

    [Fact]
    public void Write_IncludesStorageModelDetails()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Storage Model Details", markdown);
        Assert.Contains("| Entity | Entity Set | Schema | Table / View | Column Count | Defining Query |", markdown);
        Assert.Contains("| CustomerTable | CustomerTables | dbo | Customers | 2 | Yes |", markdown);
        Assert.Contains("| Store Function | Schema | Is Composable | Parameter Count |", markdown);
        Assert.Contains("| GetCustomers | dbo | No | 1 |", markdown);
    }

    [Fact]
    public void Write_IncludesAssociations()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Associations", markdown);
        Assert.Contains("| Association | From Role | To Role | Multiplicity |", markdown);
        Assert.Contains("| FK_Customer_Order | Customer | Orders | 1 to * |", markdown);
    }

    [Fact]
    public void Write_IncludesFunctionImportsAndStoreFunctions()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Function Imports and Store Functions", markdown);
        Assert.Contains("| Function Import | Return Type | Store Function |", markdown);
        Assert.Contains("| GetCustomers | Collection(Model.Customer) | GetCustomers |", markdown);
        Assert.Contains("| Store Function | Schema | Is Composable | Parameter Count |", markdown);
        Assert.Contains("| GetCustomers | dbo | No | 1 |", markdown);
    }

    [Fact]
    public void Write_IncludesMappingDetails()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Mapping Details", markdown);
        Assert.Contains("| Entity Set | Entity Type | Store Entity Set | Scalar Properties |", markdown);
        Assert.Contains("| Customers | Customer | CustomerTables | 2 |", markdown);
        Assert.Contains("| Modification function mappings | 1 |", markdown);
        Assert.Contains("| Query views | 1 |", markdown);
        Assert.Contains("| Defining queries | 1 |", markdown);
    }

    [Fact]
    public void Write_IncludesCompanionGeneratedFiles()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Companion Generated Files", markdown);
        Assert.Contains("| Project | EDMX File | Kind | File | Evidence |", markdown);
        Assert.Contains("| SampleLegacyApp.Data | `LegacyModel.edmx` | T4 template | `C:\\Repo\\SampleLegacyApp.Data\\LegacyModel.Context.tt` | `T4 template found near EDMX file.` |", markdown);
        Assert.Contains("| SampleLegacyApp.Data | `LegacyModel.edmx` | Designer generated code | `C:\\Repo\\SampleLegacyApp.Data\\LegacyModel.Designer.cs` | `.Designer.cs file found near EDMX file.` |", markdown);
    }

    [Fact]
    public void Write_WhenModelHasParseError_IncludesParseErrorAndConcern()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var report = new EdmxAnalysisReport(
            new[]
            {
                new DiscoveredEdmxModel(
                    "SampleLegacyApp.Data",
                    @"C:\Repo\SampleLegacyApp.Data\BrokenModel.edmx",
                    HasConceptualModel: false,
                    HasStorageModel: false,
                    HasMappingModel: false,
                    HasDesignerMetadata: false,
                    ParseError: "Unexpected end of file.",
                    NamespaceUris: Array.Empty<string>(),
                    ConceptualEntities: Array.Empty<EdmxConceptualEntity>(),
                    ConceptualEntitySets: Array.Empty<string>(),
                    ComplexTypes: Array.Empty<string>(),
                    StorageEntities: Array.Empty<EdmxStorageEntity>(),
                    Associations: Array.Empty<EdmxAssociation>(),
                    FunctionImports: Array.Empty<EdmxFunctionImport>(),
                    StoreFunctions: Array.Empty<EdmxStoreFunction>(),
                    MappingFragments: Array.Empty<EdmxMappingFragment>(),
                    CompanionFiles: Array.Empty<EdmxCompanionFile>(),
                    UpgradeConcerns: new[]
                    {
                        new EdmxUpgradeConcern(
                            EdmxUpgradeConcernSeverity.High,
                            "EDMX file could not be parsed",
                            "BrokenModel.edmx could not be parsed as XML: Unexpected end of file.",
                            "Review the EDMX file manually. Static analysis could not inspect model details for this file.")
                    },
                    ModificationFunctionMappingCount: 0,
                    QueryViewCount: 0,
                    DefiningQueryCount: 0)
            });

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Files with parse errors | 1 |", markdown);
        Assert.Contains("| SampleLegacyApp.Data | `C:\\Repo\\SampleLegacyApp.Data\\BrokenModel.edmx` | No | No | No | No | Parse error |", markdown);
        Assert.Contains("Unexpected end of file.", markdown);
        Assert.Contains("EDMX file could not be parsed", markdown);
        Assert.Contains("Static analysis could not inspect model details for this file.", markdown);
    }

    [Fact]
    public void Write_IncludesSuggestedReviewOrder()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Suggested Review Order", markdown);
        Assert.Contains("1. Review EDMX files with parse errors first.", markdown);
        Assert.Contains("2. Review EDMX files with function imports, store functions, modification function mappings, query views, or defining queries.", markdown);
        Assert.Contains("3. Review conceptual entities, keys, associations, and navigation properties.", markdown);
        Assert.Contains("4. Review storage entity sets, tables, views, columns, and defining queries.", markdown);
        Assert.Contains("5. Review companion generated code and T4 templates before changing or deleting EDMX files.", markdown);
    }

    [Fact]
    public void Write_IncludesNotesAndLimitations()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, CreateSampleReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Notes and Limitations", markdown);
        Assert.Contains("This report is based on static EDMX XML inspection only.", markdown);
        Assert.Contains("LegacyLens.NET did not connect to a database.", markdown);
        Assert.Contains("LegacyLens.NET did not validate the EDMX against a live database or schema.", markdown);
        Assert.Contains("LegacyLens.NET did not generate EF Core models.", markdown);
        Assert.Contains("LegacyLens.NET did not automatically convert EDMX models to EF Core.", markdown);
        Assert.Contains("No direct EF Core EDMX equivalent should be assumed.", markdown);
    }

    [Fact]
    public void Write_EscapesMarkdownPipesInTableValues()
    {
        var outputPath = Path.Combine(_tempDirectory, "edmx-analysis.md");

        var report = new EdmxAnalysisReport(
            new[]
            {
                new DiscoveredEdmxModel(
                    "Legacy|Data",
                    @"C:\Repo\Legacy|Data\Model|WithPipe.edmx",
                    HasConceptualModel: true,
                    HasStorageModel: true,
                    HasMappingModel: true,
                    HasDesignerMetadata: false,
                    ParseError: null,
                    NamespaceUris: new[] { "urn:test|namespace" },
                    ConceptualEntities: new[]
                    {
                        new EdmxConceptualEntity(
                            "Customer|Entity",
                            "Customers|Set",
                            new[] { "Id|Key" },
                            PropertyCount: 2,
                            NavigationPropertyCount: 1)
                    },
                    ConceptualEntitySets: new[] { "Customers|Set" },
                    ComplexTypes: new[] { "Address|Type" },
                    StorageEntities: new[]
                    {
                        new EdmxStorageEntity(
                            "Customer|Table",
                            "Customer|Tables",
                            "dbo|schema",
                            "Customers|View",
                            ColumnCount: 2,
                            HasDefiningQuery: true)
                    },
                    Associations: new[]
                    {
                        new EdmxAssociation(
                            "FK|Customer",
                            "From|Role",
                            "To|Role",
                            "1|*")
                    },
                    FunctionImports: new[]
                    {
                        new EdmxFunctionImport(
                            "Get|Customers",
                            "Collection(Model.Customer|Entity)",
                            "Store|Function")
                    },
                    StoreFunctions: new[]
                    {
                        new EdmxStoreFunction(
                            "Store|Function",
                            "dbo|schema",
                            IsComposable: false,
                            ParameterCount: 1)
                    },
                    MappingFragments: new[]
                    {
                        new EdmxMappingFragment(
                            "Customers|Set",
                            "Customer|Entity",
                            "Customer|Tables",
                            ScalarPropertyCount: 2)
                    },
                    CompanionFiles: new[]
                    {
                        new EdmxCompanionFile(
                            "T4|template",
                            @"C:\Repo\Legacy|Data\Model.Context.tt",
                            "Evidence contains A|B.")
                    },
                    UpgradeConcerns: new[]
                    {
                        new EdmxUpgradeConcern(
                            EdmxUpgradeConcernSeverity.High,
                            "Concern|WithPipe",
                            "Evidence contains A|B.",
                            "Review X|Y.")
                    },
                    ModificationFunctionMappingCount: 1,
                    QueryViewCount: 1,
                    DefiningQueryCount: 1)
            });

        var writer = new EdmxAnalysisMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Legacy\\|Data", markdown);
        Assert.Contains("Model\\|WithPipe.edmx", markdown);
        Assert.Contains("Customer\\|Entity", markdown);
        Assert.Contains("Customers\\|Set", markdown);
        Assert.Contains("Id\\|Key", markdown);
        Assert.Contains("Address\\|Type", markdown);
        Assert.Contains("Customer\\|Table", markdown);
        Assert.Contains("dbo\\|schema", markdown);
        Assert.Contains("Customers\\|View", markdown);
        Assert.Contains("FK\\|Customer", markdown);
        Assert.Contains("From\\|Role", markdown);
        Assert.Contains("To\\|Role", markdown);
        Assert.Contains("Store\\|Function", markdown);
        Assert.Contains("T4\\|template", markdown);
        Assert.Contains("Evidence contains A\\|B.", markdown);
        Assert.Contains("Review X\\|Y.", markdown);
        Assert.Contains("`C:\\Repo\\Legacy\\|Data\\Model\\|WithPipe.edmx`", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static EdmxAnalysisReport CreateSampleReport()
    {
        return new EdmxAnalysisReport(
            new[]
            {
                new DiscoveredEdmxModel(
                    "SampleLegacyApp.Data",
                    @"C:\Repo\SampleLegacyApp.Data\LegacyModel.edmx",
                    HasConceptualModel: true,
                    HasStorageModel: true,
                    HasMappingModel: true,
                    HasDesignerMetadata: true,
                    ParseError: null,
                    NamespaceUris: new[]
                    {
                        "http://schemas.microsoft.com/ado/2009/11/edmx",
                        "http://schemas.microsoft.com/ado/2009/11/edm",
                        "http://schemas.microsoft.com/ado/2009/11/edm/ssdl",
                        "http://schemas.microsoft.com/ado/2009/11/mapping/cs"
                    },
                    ConceptualEntities: new[]
                    {
                        new EdmxConceptualEntity(
                            "Customer",
                            "Customers",
                            new[] { "Id" },
                            PropertyCount: 2,
                            NavigationPropertyCount: 1)
                    },
                    ConceptualEntitySets: new[] { "Customers" },
                    ComplexTypes: new[] { "Address" },
                    StorageEntities: new[]
                    {
                        new EdmxStorageEntity(
                            "CustomerTable",
                            "CustomerTables",
                            "dbo",
                            "Customers",
                            ColumnCount: 2,
                            HasDefiningQuery: true)
                    },
                    Associations: new[]
                    {
                        new EdmxAssociation(
                            "FK_Customer_Order",
                            "Customer",
                            "Orders",
                            "1 to *")
                    },
                    FunctionImports: new[]
                    {
                        new EdmxFunctionImport(
                            "GetCustomers",
                            "Collection(Model.Customer)",
                            "GetCustomers")
                    },
                    StoreFunctions: new[]
                    {
                        new EdmxStoreFunction(
                            "GetCustomers",
                            "dbo",
                            IsComposable: false,
                            ParameterCount: 1)
                    },
                    MappingFragments: new[]
                    {
                        new EdmxMappingFragment(
                            "Customers",
                            "Customer",
                            "CustomerTables",
                            ScalarPropertyCount: 2)
                    },
                    CompanionFiles: new[]
                    {
                        new EdmxCompanionFile(
                            "T4 template",
                            @"C:\Repo\SampleLegacyApp.Data\LegacyModel.Context.tt",
                            "T4 template found near EDMX file."),
                        new EdmxCompanionFile(
                            "Designer generated code",
                            @"C:\Repo\SampleLegacyApp.Data\LegacyModel.Designer.cs",
                            ".Designer.cs file found near EDMX file.")
                    },
                    UpgradeConcerns: new[]
                    {
                        new EdmxUpgradeConcern(
                            EdmxUpgradeConcernSeverity.High,
                            "EDMX model requires migration decision",
                            "LegacyModel.edmx is an Entity Framework EDMX model. No direct EF Core EDMX equivalent exists.",
                            "Review whether to scaffold a new EF Core model from the database, keep EF6 isolated, or manually map equivalent entities and relationships."),
                        new EdmxUpgradeConcern(
                            EdmxUpgradeConcernSeverity.Medium,
                            "Stored procedure or function mapping requires review",
                            "Function imports, store functions, or modification function mappings were found.",
                            "Review stored procedure usage and decide whether EF Core stored procedure support, raw SQL, or explicit repository methods are needed."),
                        new EdmxUpgradeConcern(
                            EdmxUpgradeConcernSeverity.Low,
                            "Complex types require manual mapping review",
                            "1 complex type(s) found.",
                            "Review whether EF Core owned entity types or explicit value-object mappings are appropriate.")
                    },
                    ModificationFunctionMappingCount: 1,
                    QueryViewCount: 1,
                    DefiningQueryCount: 1)
            });
    }
}
