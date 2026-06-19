using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class InterfaceInventoryMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public InterfaceInventoryMarkdownReportWriterTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.InterfaceInventoryMarkdownReportWriterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_WhenOutputPathIsEmpty_ThrowsArgumentException()
    {
        var writer = new InterfaceInventoryMarkdownReportWriter();

        var exception = Assert.Throws<ArgumentException>(() => writer.Write(string.Empty, CreateEmptyReport()));

        Assert.Equal("outputPath", exception.ParamName);
    }

    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var outputPath = Path.Combine(_tempDirectory, "interface-inventory.md");
        var writer = new InterfaceInventoryMarkdownReportWriter();

        var exception = Assert.Throws<ArgumentNullException>(() => writer.Write(outputPath, null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_WhenDirectoryDoesNotExist_CreatesDirectoryAndWritesReport()
    {
        var outputDirectory = Path.Combine(_tempDirectory, "nested", "reports");
        var outputPath = Path.Combine(outputDirectory, "interface-inventory.md");
        var writer = new InterfaceInventoryMarkdownReportWriter();

        writer.Write(outputPath, CreateEmptyReport());

        Assert.True(Directory.Exists(outputDirectory));
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void Write_WhenReportIsEmpty_WritesExpectedHeadingsAndEmptyMessages()
    {
        var outputPath = Path.Combine(_tempDirectory, "interface-inventory.md");
        var writer = new InterfaceInventoryMarkdownReportWriter();

        writer.Write(outputPath, CreateEmptyReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# Interface Inventory Report", markdown);
        Assert.Contains("## Summary", markdown);
        Assert.Contains("## Analysis Scope", markdown);
        Assert.Contains("## Review Findings", markdown);
        Assert.Contains("## Possible Extension Points", markdown);
        Assert.Contains("## Interface Overview", markdown);
        Assert.Contains("## Static Implementations", markdown);
        Assert.Contains("## Static Consumers", markdown);
        Assert.Contains("## Registration and Wiring Evidence", markdown);
        Assert.Contains("## Interface Details", markdown);
        Assert.Contains("## Notes and Limitations", markdown);

        Assert.Contains("No interface inventory findings were discovered by the MVP rules.", markdown);
        Assert.Contains("No likely extension-point interfaces were identified by the MVP naming and implementation-count rules.", markdown);
        Assert.Contains("No source-defined interfaces were discovered.", markdown);
        Assert.Contains("No static interface implementations were discovered.", markdown);
        Assert.Contains("No static interface consumers were discovered.", markdown);
        Assert.Contains("No DI/IoC registration evidence was discovered by the MVP rules.", markdown);
    }

    [Fact]
    public void Write_WhenReportHasFindings_WritesSummaryTablesAndDetails()
    {
        var outputPath = Path.Combine(_tempDirectory, "interface-inventory.md");
        var writer = new InterfaceInventoryMarkdownReportWriter();

        writer.Write(outputPath, CreateReportWithEvidence());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| C# source files analysed | 3 |", markdown);
        Assert.Contains("| Configuration/XML files inspected | 1 |", markdown);
        Assert.Contains("| Interfaces discovered | 2 |", markdown);
        Assert.Contains("| Static implementations discovered | 3 |", markdown);
        Assert.Contains("| Static consumers discovered | 2 |", markdown);
        Assert.Contains("| Registration evidence items discovered | 2 |", markdown);
        Assert.Contains("| Interfaces with multiple implementations | 1 |", markdown);
        Assert.Contains("| Interfaces with no static implementation found | 1 |", markdown);
        Assert.Contains("| Interfaces with no static consumer found | 1 |", markdown);
        Assert.Contains("| Dynamic/configuration-driven wiring items requiring review | 1 |", markdown);

        Assert.Contains("| Info | `ICustomerService` | Multiple static implementations found | `2 implementation type(s) were discovered.`", markdown);
        Assert.Contains("| Review | `IPluginExtension` | Dynamic or configuration-driven wiring requires review | `Spring.NET object definition`", markdown);

        Assert.Contains("| `ICustomerService` | SampleLegacyApp.Services | Service boundary | None | 2 | 2 | 1 |", markdown);
        Assert.Contains("| `IPluginExtension` | SampleLegacyApp.Services | General abstraction | None | 0 | 0 | 1 |", markdown);

        Assert.Contains("| `ICustomerService` | `CustomerService` | SampleLegacyApp.Services |", markdown);
        Assert.Contains("| `ICustomerService` | `CustomerController` | constructor parameter | SampleLegacyApp.Services |", markdown);
        Assert.Contains("| `ICustomerService` | `CustomerService` | Microsoft DI | No | SampleLegacyApp.Web |", markdown);
        Assert.Contains("| `IPluginExtension` | `Unknown` | Spring.NET XML | Yes | SampleLegacyApp.Web |", markdown);

        Assert.Contains("### ICustomerService", markdown);
        Assert.Contains("- Likely role: Service boundary", markdown);
        Assert.Contains("- Possible extension point: Yes", markdown);
        Assert.Contains("runtime dependency injection", markdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Write_EscapesPipesInMarkdownTables()
    {
        var outputPath = Path.Combine(_tempDirectory, "interface-inventory.md");
        var writer = new InterfaceInventoryMarkdownReportWriter();

        writer.Write(
            outputPath,
            new InterfaceInventoryReport(
                new[]
                {
                    new InterfaceDefinition(
                        "Sample|Project",
                        @"C:\Repo\Service|Types.cs",
                        4,
                        "IService|Boundary",
                        "SampleLegacyApp.Services.IService|Boundary",
                        Array.Empty<string>(),
                        "Service | boundary",
                        true)
                },
                Array.Empty<InterfaceImplementation>(),
                Array.Empty<InterfaceConsumer>(),
                Array.Empty<InterfaceRegistrationEvidence>(),
                Array.Empty<InterfaceInventoryFinding>(),
                1,
                0));

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("IService\\|Boundary", markdown);
        Assert.Contains("Sample\\|Project", markdown);
        Assert.Contains("Service \\| boundary", markdown);
    }


    [Fact]
    public void Write_WhenRegistrationEvidenceIsXmlLike_RendersEvidenceAsMarkdownSafeInlineCode()
    {
        var outputPath = Path.Combine(_tempDirectory, "interface-inventory.md");
        var writer = new InterfaceInventoryMarkdownReportWriter();
        var xmlEvidence = "<object id=\"customerServiceByInterface\" type=\"SampleLegacyApp.Services.ICustomerService, SampleLegacyApp.Services\" factory-object=\"customerService\" factory-method=\"ToString\" />";

        writer.Write(outputPath, CreateReportWithXmlEvidence(xmlEvidence, xmlEvidence));

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains($"| `ICustomerService` | `CustomerService` | Spring.NET XML | Yes | SampleLegacyApp.Web | `C:\\Repo\\SampleLegacyApp.Web\\spring-service.xml` | 12 | `{xmlEvidence}` |", markdown);
        Assert.DoesNotContain($"| {xmlEvidence} |", markdown);
    }

    [Fact]
    public void Write_WhenReviewFindingReusesXmlEvidence_RendersFindingEvidenceAsMarkdownSafeInlineCode()
    {
        var outputPath = Path.Combine(_tempDirectory, "interface-inventory.md");
        var writer = new InterfaceInventoryMarkdownReportWriter();
        var xmlEvidence = "<object id=\"customerServiceByInterface\" type=\"SampleLegacyApp.Services.ICustomerService, SampleLegacyApp.Services\" />";

        writer.Write(outputPath, CreateReportWithXmlEvidence(xmlEvidence, xmlEvidence));

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains($"| Review | `ICustomerService` | Configuration-driven wiring requires review | `{xmlEvidence}` |", markdown);
    }

    [Fact]
    public void Write_WhenEvidenceContainsPipeNewlineAndBackticks_KeepsTableRowStructurallySafe()
    {
        var outputPath = Path.Combine(_tempDirectory, "interface-inventory.md");
        var writer = new InterfaceInventoryMarkdownReportWriter();
        var registrationEvidence = "<object id=\"customer|service\">\r\n`factory`\r\n</object>";

        writer.Write(outputPath, CreateReportWithXmlEvidence(registrationEvidence, registrationEvidence));

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("`` <object id=\"customer\\|service\"> `factory` </object> ``", markdown);
        Assert.DoesNotContain("<object id=\"customer|service\">\r\n", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static InterfaceInventoryReport CreateReportWithXmlEvidence(string registrationEvidence, string findingEvidence)
    {
        return new InterfaceInventoryReport(
            new[]
            {
                new InterfaceDefinition(
                    "SampleLegacyApp.Services",
                    @"C:\Repo\SampleLegacyApp.Services\CustomerService.cs",
                    3,
                    "ICustomerService",
                    "SampleLegacyApp.Services.ICustomerService",
                    Array.Empty<string>(),
                    "Service boundary",
                    true)
            },
            Array.Empty<InterfaceImplementation>(),
            Array.Empty<InterfaceConsumer>(),
            new[]
            {
                new InterfaceRegistrationEvidence(
                    "SampleLegacyApp.Web",
                    @"C:\Repo\SampleLegacyApp.Web\spring-service.xml",
                    12,
                    "ICustomerService",
                    "CustomerService",
                    InterfaceRegistrationKind.SpringNetXml,
                    registrationEvidence,
                    true,
                    "Configuration-driven IoC evidence found.")
            },
            new[]
            {
                new InterfaceInventoryFinding(
                    InterfaceInventoryFindingSeverity.Review,
                    "ICustomerService",
                    "Configuration-driven wiring requires review",
                    findingEvidence,
                    "Confirm whether this registration source is active at runtime.")
            },
            1,
            1);
    }

    private static InterfaceInventoryReport CreateEmptyReport()
    {
        return new InterfaceInventoryReport(
            Array.Empty<InterfaceDefinition>(),
            Array.Empty<InterfaceImplementation>(),
            Array.Empty<InterfaceConsumer>(),
            Array.Empty<InterfaceRegistrationEvidence>(),
            Array.Empty<InterfaceInventoryFinding>(),
            0,
            0);
    }

    private static InterfaceInventoryReport CreateReportWithEvidence()
    {
        return new InterfaceInventoryReport(
            new[]
            {
                new InterfaceDefinition(
                    "SampleLegacyApp.Services",
                    @"C:\Repo\SampleLegacyApp.Services\CustomerService.cs",
                    3,
                    "ICustomerService",
                    "SampleLegacyApp.Services.ICustomerService",
                    Array.Empty<string>(),
                    "Service boundary",
                    true),
                new InterfaceDefinition(
                    "SampleLegacyApp.Services",
                    @"C:\Repo\SampleLegacyApp.Services\Plugins.cs",
                    5,
                    "IPluginExtension",
                    "SampleLegacyApp.Services.IPluginExtension",
                    Array.Empty<string>(),
                    "General abstraction",
                    false)
            },
            new[]
            {
                new InterfaceImplementation(
                    "SampleLegacyApp.Services",
                    @"C:\Repo\SampleLegacyApp.Services\CustomerService.cs",
                    8,
                    "ICustomerService",
                    "CustomerService",
                    "CustomerService : ICustomerService"),
                new InterfaceImplementation(
                    "SampleLegacyApp.Services",
                    @"C:\Repo\SampleLegacyApp.Services\CachedCustomerService.cs",
                    8,
                    "ICustomerService",
                    "CachedCustomerService",
                    "CachedCustomerService : ICustomerService"),
                new InterfaceImplementation(
                    "SampleLegacyApp.Services",
                    @"C:\Repo\SampleLegacyApp.Services\OtherCustomerService.cs",
                    8,
                    "IOtherService",
                    "OtherService",
                    "OtherService : IOtherService")
            },
            new[]
            {
                new InterfaceConsumer(
                    "SampleLegacyApp.Services",
                    @"C:\Repo\SampleLegacyApp.Services\CustomerController.cs",
                    10,
                    "ICustomerService",
                    "CustomerController",
                    InterfaceConsumerKind.ConstructorParameter,
                    "CustomerController(ICustomerService service)"),
                new InterfaceConsumer(
                    "SampleLegacyApp.Web",
                    @"C:\Repo\SampleLegacyApp.Web\Program.cs",
                    7,
                    "ICustomerService",
                    "MinimalApiEndpoint",
                    InterfaceConsumerKind.EndpointDelegateParameter,
                    "app.MapGet(...)")
            },
            new[]
            {
                new InterfaceRegistrationEvidence(
                    "SampleLegacyApp.Web",
                    @"C:\Repo\SampleLegacyApp.Web\Program.cs",
                    4,
                    "ICustomerService",
                    "CustomerService",
                    InterfaceRegistrationKind.MicrosoftDependencyInjection,
                    "builder.Services.AddSingleton<ICustomerService, CustomerService>()",
                    false,
                    "Microsoft DI registration evidence found."),
                new InterfaceRegistrationEvidence(
                    "SampleLegacyApp.Web",
                    @"C:\Repo\SampleLegacyApp.Web\spring-objects.xml",
                    2,
                    "IPluginExtension",
                    null,
                    InterfaceRegistrationKind.SpringNetXml,
                    "Spring.NET object definition",
                    true,
                    "Configuration-driven IoC evidence found.")
            },
            new[]
            {
                new InterfaceInventoryFinding(
                    InterfaceInventoryFindingSeverity.Info,
                    "ICustomerService",
                    "Multiple static implementations found",
                    "2 implementation type(s) were discovered.",
                    "Review this interface as a likely extension point."),
                new InterfaceInventoryFinding(
                    InterfaceInventoryFindingSeverity.Review,
                    "IPluginExtension",
                    "Dynamic or configuration-driven wiring requires review",
                    "Spring.NET object definition",
                    "Confirm whether this registration source is active at runtime.")
            },
            3,
            1);
    }
}
