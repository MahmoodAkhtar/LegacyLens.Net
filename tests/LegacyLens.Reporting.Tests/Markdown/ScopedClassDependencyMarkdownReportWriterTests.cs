using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class ScopedClassDependencyMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public ScopedClassDependencyMarkdownReportWriterTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.ScopedClassDependencyMarkdownReportWriterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_WhenOutputPathIsEmpty_ThrowsArgumentException()
    {
        var writer = new ScopedClassDependencyMarkdownReportWriter();
        var report = CreateMatchedReport();

        var exception = Assert.Throws<ArgumentException>(() => writer.Write(string.Empty, report));

        Assert.Equal("outputPath", exception.ParamName);
    }

    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var writer = new ScopedClassDependencyMarkdownReportWriter();
        var outputPath = Path.Combine(_tempDirectory, "class-dependency-scope.md");

        var exception = Assert.Throws<ArgumentNullException>(() => writer.Write(outputPath, null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_WritesFocusedScopedReport()
    {
        var writer = new ScopedClassDependencyMarkdownReportWriter();
        var outputPath = Path.Combine(_tempDirectory, "class-dependency-scope.SampleLegacyApp.Services.CustomerService.20260620-153045.md");

        writer.Write(outputPath, CreateMatchedReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# Scoped Class Dependency Report", markdown);
        Assert.Contains("Requested type: `SampleLegacyApp.Services.CustomerService`", markdown);
        Assert.Contains("| Generated local | `2026-06-20 15:30:45 +01:00` |", markdown);
        Assert.Contains("| Generated UTC | `2026-06-20 14:30:45 UTC` |", markdown);
        Assert.Contains("| Direct outbound dependencies | 2 |", markdown);
        Assert.Contains("| Direct inbound dependants | 1 |", markdown);
        Assert.Contains("| Full name | `SampleLegacyApp.Services.CustomerService` |", markdown);
        Assert.Contains("## Direct Outbound Dependencies", markdown);
        Assert.Contains("| SampleLegacyApp.Services | `CustomerService` | `CustomerRepository` | field | `C:\\Repo\\SampleLegacyApp.Services\\CustomerService.cs` | 10 | `private readonly CustomerRepository _repository` |", markdown);
        Assert.Contains("## Direct Inbound Dependants", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `CustomerController` | `CustomerService` | constructor parameter | `C:\\Repo\\SampleLegacyApp.Web\\CustomerController.cs` | 7 | `CustomerController(CustomerService service)` |", markdown);
        Assert.Contains("## Related Review Concerns", markdown);
        Assert.Contains("Concrete member dependencies increase coupling", markdown);
        Assert.Contains("```mermaid", markdown);
        Assert.Contains("CustomerService -->|field| CustomerRepository", markdown);
        Assert.Contains("CustomerController -->|constructor parameter| CustomerService", markdown);
        Assert.Contains("transitive dependencies", markdown);
        Assert.Contains("runtime call graphs", markdown);
    }

    [Fact]
    public void Write_WhenNoMatch_WritesNoMatchMessage()
    {
        var writer = new ScopedClassDependencyMarkdownReportWriter();
        var outputPath = Path.Combine(_tempDirectory, "missing.md");

        writer.Write(outputPath, CreateNoMatchReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("No source-defined type matched the requested fully qualified type name", markdown);
        Assert.Contains("does not silently fall back to short-name matching", markdown);
        Assert.DoesNotContain("## Direct Outbound Dependencies", markdown);
    }

    [Fact]
    public void Write_WhenAmbiguous_WritesAmbiguityEvidence()
    {
        var writer = new ScopedClassDependencyMarkdownReportWriter();
        var outputPath = Path.Combine(_tempDirectory, "ambiguous.md");

        writer.Write(outputPath, CreateAmbiguousReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("matched multiple source-defined types", markdown);
        Assert.Contains("| `SampleLegacyApp.Services.CustomerService` | Class | SampleLegacyApp.Services | `C:\\Repo\\SampleLegacyApp.Services\\CustomerService.cs` | 8 |", markdown);
        Assert.Contains("| `SampleLegacyApp.Services.CustomerService` | Class | Duplicate.Project | `C:\\Repo\\Duplicate\\CustomerService.cs` | 5 |", markdown);
    }

    [Fact]
    public void Write_EscapesMarkdownSensitiveValuesInTables()
    {
        var writer = new ScopedClassDependencyMarkdownReportWriter();
        var outputPath = Path.Combine(_tempDirectory, "pipes.md");
        var sourcePath = @"C:\Repo\Sample|App\CustomerService.cs";

        var report = new ScopedClassDependencyReport(
            "Sample|App.Services.CustomerService",
            new DateTimeOffset(2026, 6, 20, 15, 30, 45, TimeSpan.FromHours(1)),
            new DateTimeOffset(2026, 6, 20, 14, 30, 45, TimeSpan.Zero),
            1,
            1,
            new[]
            {
                new DiscoveredType(
                    "Customer|Service",
                    "Sample|App.Services.CustomerService",
                    ClassDiscoveredTypeKind.Class,
                    "Sample|Project",
                    sourcePath,
                    4)
            },
            new DiscoveredType(
                "Customer|Service",
                "Sample|App.Services.CustomerService",
                ClassDiscoveredTypeKind.Class,
                "Sample|Project",
                sourcePath,
                4),
            new[]
            {
                new ClassDependency(
                    "Sample|Project",
                    sourcePath,
                    8,
                    "Customer|Service",
                    "Repository|Type",
                    ClassDependencyKind.ObjectCreation,
                    "new Repository|Type()")
            },
            Array.Empty<ClassDependency>(),
            new[]
            {
                new ClassDependencyConcern(
                    ClassDependencyConcernSeverity.High,
                    "Customer|Service",
                    "Repository|Type",
                    ClassDependencyKind.ObjectCreation,
                    "Sample|Project",
                    sourcePath,
                    8,
                    "new Repository|Type()",
                    "Why|Matters",
                    "Review|This")
            });

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Sample\\|App.Services.CustomerService", markdown);
        Assert.Contains("Customer\\|Service", markdown);
        Assert.Contains("Repository\\|Type", markdown);
        Assert.Contains("Sample\\|Project", markdown);
        Assert.Contains("new Repository\\|Type()", markdown);
        Assert.Contains("Why\\|Matters", markdown);
        Assert.Contains("Review\\|This", markdown);
        Assert.Contains("`C:\\Repo\\Sample\\|App\\CustomerService.cs`", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static ScopedClassDependencyReport CreateMatchedReport()
    {
        var servicePath = "C:\\Repo\\SampleLegacyApp.Services\\CustomerService.cs";
        var controllerPath = "C:\\Repo\\SampleLegacyApp.Web\\CustomerController.cs";
        var rootType = new DiscoveredType(
            "CustomerService",
            "SampleLegacyApp.Services.CustomerService",
            ClassDiscoveredTypeKind.Class,
            "SampleLegacyApp.Services",
            servicePath,
            8);

        return new ScopedClassDependencyReport(
            "SampleLegacyApp.Services.CustomerService",
            new DateTimeOffset(2026, 6, 20, 15, 30, 45, TimeSpan.FromHours(1)),
            new DateTimeOffset(2026, 6, 20, 14, 30, 45, TimeSpan.Zero),
            SourceFileCount: 2,
            DiscoveredTypeCount: 4,
            MatchingTypes: new[] { rootType },
            RootType: rootType,
            OutboundDependencies: new[]
            {
                new ClassDependency(
                    "SampleLegacyApp.Services",
                    servicePath,
                    8,
                    "CustomerService",
                    "ICustomerService",
                    ClassDependencyKind.InterfaceImplementation,
                    "ICustomerService"),
                new ClassDependency(
                    "SampleLegacyApp.Services",
                    servicePath,
                    10,
                    "CustomerService",
                    "CustomerRepository",
                    ClassDependencyKind.Field,
                    "private readonly CustomerRepository _repository")
            },
            InboundDependants: new[]
            {
                new ClassDependency(
                    "SampleLegacyApp.Web",
                    controllerPath,
                    7,
                    "CustomerController",
                    "CustomerService",
                    ClassDependencyKind.ConstructorParameter,
                    "CustomerController(CustomerService service)")
            },
            Concerns: new[]
            {
                new ClassDependencyConcern(
                    ClassDependencyConcernSeverity.Medium,
                    "CustomerService",
                    "CustomerRepository",
                    ClassDependencyKind.Field,
                    "SampleLegacyApp.Services",
                    servicePath,
                    10,
                    "private readonly CustomerRepository _repository",
                    "Concrete member dependencies increase coupling between types and may make substitution harder.",
                    "Review whether this dependency should be represented by an interface, abstraction, or value object.")
            });
    }

    private static ScopedClassDependencyReport CreateNoMatchReport()
    {
        return new ScopedClassDependencyReport(
            "SampleLegacyApp.Services.MissingType",
            DateTimeOffset.Now,
            DateTimeOffset.UtcNow,
            SourceFileCount: 2,
            DiscoveredTypeCount: 4,
            MatchingTypes: Array.Empty<DiscoveredType>(),
            RootType: null,
            OutboundDependencies: Array.Empty<ClassDependency>(),
            InboundDependants: Array.Empty<ClassDependency>(),
            Concerns: Array.Empty<ClassDependencyConcern>());
    }

    private static ScopedClassDependencyReport CreateAmbiguousReport()
    {
        var first = new DiscoveredType(
            "CustomerService",
            "SampleLegacyApp.Services.CustomerService",
            ClassDiscoveredTypeKind.Class,
            "SampleLegacyApp.Services",
            "C:\\Repo\\SampleLegacyApp.Services\\CustomerService.cs",
            8);
        var second = new DiscoveredType(
            "CustomerService",
            "SampleLegacyApp.Services.CustomerService",
            ClassDiscoveredTypeKind.Class,
            "Duplicate.Project",
            "C:\\Repo\\Duplicate\\CustomerService.cs",
            5);

        return new ScopedClassDependencyReport(
            "SampleLegacyApp.Services.CustomerService",
            DateTimeOffset.Now,
            DateTimeOffset.UtcNow,
            SourceFileCount: 2,
            DiscoveredTypeCount: 4,
            MatchingTypes: new[] { first, second },
            RootType: null,
            OutboundDependencies: Array.Empty<ClassDependency>(),
            InboundDependants: Array.Empty<ClassDependency>(),
            Concerns: Array.Empty<ClassDependencyConcern>());
    }
}
