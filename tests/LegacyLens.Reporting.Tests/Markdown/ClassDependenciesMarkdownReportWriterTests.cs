using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class ClassDependenciesMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public ClassDependenciesMarkdownReportWriterTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.ClassDependenciesMarkdownReportWriterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_WhenOutputPathIsEmpty_ThrowsArgumentException()
    {
        var writer = new ClassDependenciesMarkdownReportWriter();

        var report = CreateEmptyReport();

        var exception = Assert.Throws<ArgumentException>(() => writer.Write(string.Empty, report));

        Assert.Equal("outputPath", exception.ParamName);
    }

    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var outputPath = Path.Combine(_tempDirectory, "class-dependencies.md");
        var writer = new ClassDependenciesMarkdownReportWriter();

        var exception = Assert.Throws<ArgumentNullException>(() => writer.Write(outputPath, null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_WhenDirectoryDoesNotExist_CreatesDirectoryAndWritesReport()
    {
        var outputDirectory = Path.Combine(_tempDirectory, "nested", "reports");
        var outputPath = Path.Combine(outputDirectory, "class-dependencies.md");

        var writer = new ClassDependenciesMarkdownReportWriter();

        writer.Write(outputPath, CreateEmptyReport());

        Assert.True(Directory.Exists(outputDirectory));
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void Write_WhenReportIsEmpty_WritesExpectedHeadingsAndEmptyMessages()
    {
        var outputPath = Path.Combine(_tempDirectory, "class-dependencies.md");

        var writer = new ClassDependenciesMarkdownReportWriter();

        writer.Write(outputPath, CreateEmptyReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# Class Dependency Report", markdown);
        Assert.Contains("## Summary", markdown);
        Assert.Contains("## Analysis Scope", markdown);
        Assert.Contains("## Top Coupled Types", markdown);
        Assert.Contains("## Coupling Concerns", markdown);
        Assert.Contains("## Hardcoded Concrete Dependencies", markdown);
        Assert.Contains("## Static Dependency Hotspots", markdown);
        Assert.Contains("## Dependency Diagram", markdown);
        Assert.Contains("## Type Dependency Inventory", markdown);
        Assert.Contains("## Type Details", markdown);
        Assert.Contains("## Notes and Limitations", markdown);

        Assert.Contains("No high-coupling type hotspots were discovered by the MVP rules.", markdown);
        Assert.Contains("No coupling concerns were discovered by the MVP rules.", markdown);
        Assert.Contains("No hardcoded concrete dependencies were discovered by the MVP rules.", markdown);
        Assert.Contains("No static dependency hotspots were discovered by the MVP rules.", markdown);
        Assert.Contains("No source-level type dependencies were discovered.", markdown);
        Assert.Contains("No source-defined types were discovered.", markdown);
        Assert.Contains("No_Class_Dependencies_Discovered[No class dependencies discovered]", markdown);
    }

    [Fact]
    public void Write_WhenReportHasDependencies_WritesSummaryTablesDiagramInventoryAndTypeDetails()
    {
        var outputPath = Path.Combine(_tempDirectory, "class-dependencies.md");

        var report = CreateReportWithDependencies();

        var writer = new ClassDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Projects analysed | 1 |", markdown);
        Assert.Contains("| C# source files analysed | 2 |", markdown);
        Assert.Contains("| Types discovered | 3 |", markdown);
        Assert.Contains("| Dependency relationships discovered | 3 |", markdown);
        Assert.Contains("| Coupling concerns discovered | 2 |", markdown);
        Assert.Contains("| Hardcoded concrete dependencies discovered | 1 |", markdown);
        Assert.Contains("| Static dependencies discovered | 1 |", markdown);
        Assert.Contains("| High-coupling types discovered | 1 |", markdown);

        Assert.Contains("| `OrderService` | SampleLegacyApp.Services | 2 | 0 | 2 | Coupling concern evidence found. |", markdown);

        Assert.Contains("| High | `OrderService` | `OrderRepository` | hardcoded new | `new OrderRepository()` |", markdown);
        Assert.Contains("| Medium | `OrderService` | `ConfigurationManager` | static access | `ConfigurationManager.AppSettings` |", markdown);

        Assert.Contains("## Hardcoded Concrete Dependencies", markdown);
        Assert.Contains("| `OrderService` | `OrderRepository` | SampleLegacyApp.Services | `new OrderRepository()` | High |", markdown);

        Assert.Contains("## Static Dependency Hotspots", markdown);
        Assert.Contains("| `OrderService` | `ConfigurationManager` | SampleLegacyApp.Services | `ConfigurationManager.AppSettings` | Medium |", markdown);

        Assert.Contains("```mermaid", markdown);
        Assert.Contains("graph TD", markdown);
        Assert.Contains("OrderService -->|hardcoded new| OrderRepository", markdown);
        Assert.Contains("OrderService -->|static access| ConfigurationManager", markdown);

        Assert.Contains("| SampleLegacyApp.Services | `OrderService` | `OrderRepository` | hardcoded new | `C:\\Repo\\SampleLegacyApp.Services\\OrderService.cs` | 12 | `new OrderRepository()` |", markdown);
        Assert.Contains("| SampleLegacyApp.Services | `OrderService` | `ConfigurationManager` | static access | `C:\\Repo\\SampleLegacyApp.Services\\OrderService.cs` | 18 | `ConfigurationManager.AppSettings` |", markdown);

        Assert.Contains("### OrderService", markdown);
        Assert.Contains("- Project: SampleLegacyApp.Services", markdown);
        Assert.Contains("- Kind: Class", markdown);
        Assert.Contains("- Dependencies: 3", markdown);

        Assert.Contains("LegacyLens.NET did not build the solution or restore NuGet packages.", markdown);
        Assert.Contains("runtime call graph", markdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Write_EscapesPipesInMarkdownTables()
    {
        var outputPath = Path.Combine(_tempDirectory, "class-dependencies.md");

        var sourcePath = @"C:\Repo\Sample|App\Services\Order|Service.cs";

        var report = new ClassDependencyReport(
            new[]
            {
                new DiscoveredType(
                    "Order|Service",
                    "SampleLegacyApp.Services.Order|Service",
                    ClassDiscoveredTypeKind.Class,
                    "Sample|Project",
                    sourcePath,
                    3),

                new DiscoveredType(
                    "Order|Repository",
                    "SampleLegacyApp.Services.Order|Repository",
                    ClassDiscoveredTypeKind.Class,
                    "Sample|Project",
                    sourcePath,
                    8)
            },
            new[]
            {
                new ClassDependency(
                    "Sample|Project",
                    sourcePath,
                    12,
                    "Order|Service",
                    "Order|Repository",
                    ClassDependencyKind.ObjectCreation,
                    "new Order|Repository()")
            },
            new[]
            {
                new ClassDependencyConcern(
                    ClassDependencyConcernSeverity.High,
                    "Order|Service",
                    "Order|Repository",
                    ClassDependencyKind.ObjectCreation,
                    "Sample|Project",
                    sourcePath,
                    12,
                    "new Order|Repository()",
                    "Why|It|Matters",
                    "Review|Recommendation")
            },
            new[]
            {
                new ClassCouplingHotspot(
                    "Order|Service",
                    "Sample|Project",
                    1,
                    0,
                    1,
                    "Concern|Found")
            },
            SourceFileCount: 1);

        var writer = new ClassDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Order\\|Service", markdown);
        Assert.Contains("Order\\|Repository", markdown);
        Assert.Contains("Sample\\|Project", markdown);
        Assert.Contains("new Order\\|Repository()", markdown);
        Assert.Contains("Why\\|It\\|Matters", markdown);
        Assert.Contains("Review\\|Recommendation", markdown);
        Assert.Contains("Concern\\|Found", markdown);
        Assert.Contains("`C:\\Repo\\Sample\\|App\\Services\\Order\\|Service.cs`", markdown);
    }

    [Fact]
    public void Write_WritesFriendlyDependencyKindLabels()
    {
        var outputPath = Path.Combine(_tempDirectory, "class-dependencies.md");

        var dependencies = Enum
            .GetValues<ClassDependencyKind>()
            .Select((kind, index) => new ClassDependency(
                "SampleLegacyApp.Services",
                @"C:\Repo\SampleLegacyApp.Services\OrderService.cs",
                index + 1,
                "OrderService",
                $"Target{index}",
                kind,
                $"Evidence {index}"))
            .ToArray();

        var targetTypes = dependencies
            .Select(dependency => new DiscoveredType(
                dependency.TargetType,
                $"SampleLegacyApp.Services.{dependency.TargetType}",
                ClassDiscoveredTypeKind.Class,
                "SampleLegacyApp.Services",
                @"C:\Repo\SampleLegacyApp.Services\OrderService.cs",
                dependency.LineNumber))
            .ToArray();

        var report = new ClassDependencyReport(
            new[]
            {
                new DiscoveredType(
                    "OrderService",
                    "SampleLegacyApp.Services.OrderService",
                    ClassDiscoveredTypeKind.Class,
                    "SampleLegacyApp.Services",
                    @"C:\Repo\SampleLegacyApp.Services\OrderService.cs",
                    1)
            }.Concat(targetTypes).ToArray(),
            dependencies,
            Array.Empty<ClassDependencyConcern>(),
            Array.Empty<ClassCouplingHotspot>(),
            SourceFileCount: 1);

        var writer = new ClassDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("constructor parameter", markdown);
        Assert.Contains("field", markdown);
        Assert.Contains("property", markdown);
        Assert.Contains("method parameter", markdown);
        Assert.Contains("return type", markdown);
        Assert.Contains("local variable", markdown);
        Assert.Contains("hardcoded new", markdown);
        Assert.Contains("static access", markdown);
        Assert.Contains("inherits", markdown);
        Assert.Contains("implements", markdown);
        Assert.Contains("attribute", markdown);
        Assert.Contains("generic type", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static ClassDependencyReport CreateEmptyReport()
    {
        return new ClassDependencyReport(
            Array.Empty<DiscoveredType>(),
            Array.Empty<ClassDependency>(),
            Array.Empty<ClassDependencyConcern>(),
            Array.Empty<ClassCouplingHotspot>(),
            SourceFileCount: 0);
    }

    private static ClassDependencyReport CreateReportWithDependencies()
    {
        const string projectName = "SampleLegacyApp.Services";
        const string sourcePath = @"C:\Repo\SampleLegacyApp.Services\OrderService.cs";

        return new ClassDependencyReport(
            new[]
            {
                new DiscoveredType(
                    "OrderService",
                    "SampleLegacyApp.Services.OrderService",
                    ClassDiscoveredTypeKind.Class,
                    projectName,
                    sourcePath,
                    5),

                new DiscoveredType(
                    "OrderRepository",
                    "SampleLegacyApp.Services.OrderRepository",
                    ClassDiscoveredTypeKind.Class,
                    projectName,
                    sourcePath,
                    28),

                new DiscoveredType(
                    "IOrderService",
                    "SampleLegacyApp.Services.IOrderService",
                    ClassDiscoveredTypeKind.Interface,
                    projectName,
                    sourcePath,
                    1)
            },
            new[]
            {
                new ClassDependency(
                    projectName,
                    sourcePath,
                    7,
                    "OrderService",
                    "IOrderService",
                    ClassDependencyKind.InterfaceImplementation,
                    "public class OrderService : IOrderService"),

                new ClassDependency(
                    projectName,
                    sourcePath,
                    12,
                    "OrderService",
                    "OrderRepository",
                    ClassDependencyKind.ObjectCreation,
                    "new OrderRepository()"),

                new ClassDependency(
                    projectName,
                    sourcePath,
                    18,
                    "OrderService",
                    "ConfigurationManager",
                    ClassDependencyKind.StaticMemberAccess,
                    "ConfigurationManager.AppSettings")
            },
            new[]
            {
                new ClassDependencyConcern(
                    ClassDependencyConcernSeverity.High,
                    "OrderService",
                    "OrderRepository",
                    ClassDependencyKind.ObjectCreation,
                    projectName,
                    sourcePath,
                    12,
                    "new OrderRepository()",
                    "Concrete construction hides the dependency and can make testing, replacement, and migration harder.",
                    "Consider constructor injection behind an interface or an explicit factory if the dependency has behaviour or infrastructure impact."),

                new ClassDependencyConcern(
                    ClassDependencyConcernSeverity.Medium,
                    "OrderService",
                    "ConfigurationManager",
                    ClassDependencyKind.StaticMemberAccess,
                    projectName,
                    sourcePath,
                    18,
                    "ConfigurationManager.AppSettings",
                    "Static/global access can hide runtime dependencies and may need review during migration or test isolation.",
                    "Consider migrating access behind IConfiguration, options binding, or a configuration abstraction.")
            },
            new[]
            {
                new ClassCouplingHotspot(
                    "OrderService",
                    projectName,
                    OutgoingDependencyCount: 2,
                    IncomingDependencyCount: 0,
                    ConcernCount: 2,
                    "Coupling concern evidence found.")
            },
            SourceFileCount: 2);
    }
}