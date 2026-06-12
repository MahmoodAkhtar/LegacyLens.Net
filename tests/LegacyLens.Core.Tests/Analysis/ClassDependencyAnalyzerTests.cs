using LegacyLens.Core.Analysis;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ClassDependencyAnalyzerTests : IDisposable
{
    private readonly string _rootPath;
    private readonly string _projectDirectory;
    private readonly string _projectFilePath;

    public ClassDependencyAnalyzerTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.ClassDependencyAnalyzerTests",
            Guid.NewGuid().ToString("N"));

        _projectDirectory = Path.Combine(_rootPath, "SampleLegacyApp.Services");
        Directory.CreateDirectory(_projectDirectory);

        _projectFilePath = Path.Combine(_projectDirectory, "SampleLegacyApp.Services.csproj");
        File.WriteAllText(
            _projectFilePath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net48</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
    }

    [Fact]
    public void Analyze_WhenFileInventoryIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new ClassDependencyAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze((ScanFileInventory)null!));

        Assert.Equal("fileInventory", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenProjectDirectoryDoesNotExist_ReturnsEmptyReport()
    {
        var missingProject = new DiscoveredProject
        {
            Name = "Missing.Project",
            ProjectFilePath = Path.Combine(_rootPath, "Missing.Project", "Missing.Project.csproj"),
            TargetFramework = "net48"
        };

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { missingProject }));

        Assert.Empty(report.Types);
        Assert.Empty(report.Dependencies);
        Assert.Empty(report.Concerns);
        Assert.Empty(report.Hotspots);
        Assert.Equal(0, report.SourceFileCount);
        Assert.Equal(0, report.HardcodedConcreteDependencyCount);
        Assert.Equal(0, report.StaticDependencyCount);
    }

    [Fact]
    public void Analyze_DiscoversSourceDefinedTypes()
    {
        WriteSourceFile(
            "OrderTypes.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface IOrderRepository
            {
            }

            public class OrderRepository
            {
            }

            public record OrderDto(int Id);

            public struct OrderKey
            {
            }

            public enum OrderStatus
            {
                Pending
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Types, type =>
            type.Name == "IOrderRepository" &&
            type.FullName == "SampleLegacyApp.Services.IOrderRepository" &&
            type.Kind == ClassDiscoveredTypeKind.Interface &&
            type.ProjectName == "SampleLegacyApp.Services");

        Assert.Contains(report.Types, type =>
            type.Name == "OrderRepository" &&
            type.Kind == ClassDiscoveredTypeKind.Class);

        Assert.Contains(report.Types, type =>
            type.Name == "OrderDto" &&
            type.Kind == ClassDiscoveredTypeKind.Record);

        Assert.Contains(report.Types, type =>
            type.Name == "OrderKey" &&
            type.Kind == ClassDiscoveredTypeKind.Struct);

        Assert.Contains(report.Types, type =>
            type.Name == "OrderStatus" &&
            type.Kind == ClassDiscoveredTypeKind.Enum);

        Assert.Equal(1, report.SourceFileCount);
    }

    [Fact]
    public void Analyze_DiscoversDependencyKindsFromSourceEvidence()
    {
        WriteSourceFile(
            "OrderRepository.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface IOrderRepository
            {
            }

            public class OrderRepository : IOrderRepository
            {
            }

            public class OrderRequest
            {
            }

            public class OrderDto
            {
            }

            public class LegacyController
            {
            }
            """);

        WriteSourceFile(
            "OrderController.cs",
            """
            using System.Collections.Generic;

            namespace SampleLegacyApp.Services;

            [RoutePrefix("orders")]
            public class OrderController : LegacyController, IOrderRepository
            {
                private readonly OrderRepository _repository = new();

                public OrderDto Current { get; }

                public IReadOnlyList<OrderDto> GetAll()
                {
                    OrderDto dto = new OrderDto();
                    var now = DateTime.UtcNow;
                    return new[] { dto };
                }

                public OrderDto Get(OrderRequest request)
                {
                    var repository = new OrderRepository();
                    return new OrderDto();
                }
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderRepository" &&
            dependency.TargetType == "IOrderRepository" &&
            dependency.Kind == ClassDependencyKind.InterfaceImplementation);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "LegacyController" &&
            dependency.Kind == ClassDependencyKind.BaseClass);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "IOrderRepository" &&
            dependency.Kind == ClassDependencyKind.InterfaceImplementation);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "OrderRepository" &&
            dependency.Kind == ClassDependencyKind.Field);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "OrderRepository" &&
            dependency.Kind == ClassDependencyKind.ObjectCreation);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "OrderDto" &&
            dependency.Kind == ClassDependencyKind.Property);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "OrderDto" &&
            dependency.Kind == ClassDependencyKind.ReturnType);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "OrderRequest" &&
            dependency.Kind == ClassDependencyKind.MethodParameter);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "OrderDto" &&
            dependency.Kind == ClassDependencyKind.LocalVariable);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "DateTime" &&
            dependency.Kind == ClassDependencyKind.StaticMemberAccess);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "RoutePrefixAttribute" &&
            dependency.Kind == ClassDependencyKind.Attribute);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderController" &&
            dependency.TargetType == "OrderDto" &&
            dependency.Kind == ClassDependencyKind.GenericTypeArgument);
    }

    [Fact]
    public void Analyze_WhenHardcodedNewExists_AddsHighConcernAndHotspot()
    {
        WriteSourceFile(
            "OrderService.cs",
            """
            namespace SampleLegacyApp.Services;

            public class OrderRepository
            {
            }

            public class OrderService
            {
                private readonly OrderRepository _repository = new();

                public OrderRepository Create()
                {
                    return new OrderRepository();
                }
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.True(report.HardcodedConcreteDependencyCount >= 1);

        Assert.Contains(report.Concerns, concern =>
            concern.SourceType == "OrderService" &&
            concern.TargetType == "OrderRepository" &&
            concern.DependencyKind == ClassDependencyKind.ObjectCreation &&
            concern.Severity == ClassDependencyConcernSeverity.High &&
            concern.WhyItMatters.Contains("Concrete construction", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(report.Hotspots, hotspot =>
            hotspot.Type == "OrderService" &&
            hotspot.ProjectName == "SampleLegacyApp.Services" &&
            hotspot.ConcernCount > 0);
    }

    [Fact]
    public void Analyze_WhenStaticAccessExists_AddsStaticDependencyConcern()
    {
        WriteSourceFile(
            "LegacySettingsReader.cs",
            """
            namespace SampleLegacyApp.Services;

            public class LegacySettingsReader
            {
                public string? Read()
                {
                    return ConfigurationManager.AppSettings["ApiUrl"];
                }
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Equal(1, report.StaticDependencyCount);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal("LegacySettingsReader", dependency.SourceType);
        Assert.Equal("ConfigurationManager", dependency.TargetType);
        Assert.Equal(ClassDependencyKind.StaticMemberAccess, dependency.Kind);

        Assert.Contains(report.Concerns, concern =>
            concern.SourceType == "LegacySettingsReader" &&
            concern.TargetType == "ConfigurationManager" &&
            concern.DependencyKind == ClassDependencyKind.StaticMemberAccess &&
            concern.Severity == ClassDependencyConcernSeverity.Medium &&
            concern.Recommendation.Contains("IConfiguration", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_ExcludesBuildOutputSourceFiles()
    {
        WriteSourceFile(
            "OrderService.cs",
            """
            namespace SampleLegacyApp.Services;

            public class OrderService
            {
            }
            """);

        var binDirectory = Path.Combine(_projectDirectory, "bin", "Debug");
        Directory.CreateDirectory(binDirectory);

        File.WriteAllText(
            Path.Combine(binDirectory, "GeneratedBuildOutput.cs"),
            """
            namespace SampleLegacyApp.Services;

            public class GeneratedBuildOutput
            {
            }
            """);

        var objDirectory = Path.Combine(_projectDirectory, "obj", "Debug");
        Directory.CreateDirectory(objDirectory);

        File.WriteAllText(
            Path.Combine(objDirectory, "GeneratedObjOutput.cs"),
            """
            namespace SampleLegacyApp.Services;

            public class GeneratedObjOutput
            {
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Types, type => type.Name == "OrderService");
        Assert.DoesNotContain(report.Types, type => type.Name == "GeneratedBuildOutput");
        Assert.DoesNotContain(report.Types, type => type.Name == "GeneratedObjOutput");
        Assert.Equal(1, report.SourceFileCount);
    }

    [Fact]
    public void Analyze_DeduplicatesIdenticalDependenciesFromSameEvidence()
    {
        WriteSourceFile(
            "OrderService.cs",
            """
            namespace SampleLegacyApp.Services;

            public class OrderRepository
            {
            }

            public class OrderService
            {
                public OrderRepository Create()
                {
                    return new OrderRepository();
                }
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        var duplicateKeyCount = report.Dependencies
            .GroupBy(dependency => string.Join(
                "|",
                dependency.ProjectName,
                dependency.SourcePath,
                dependency.LineNumber,
                dependency.SourceType,
                dependency.TargetType,
                dependency.Kind,
                dependency.Evidence))
            .Count(group => group.Count() > 1);

        Assert.Equal(0, duplicateKeyCount);
    }
    
    [Fact]
    public void Analyze_WhenConstructorHasConcreteParameter_DiscoversConstructorParameterDependencyAndConcern()
    {
        WriteSourceFile(
            "OrderService.cs",
            """
            namespace SampleLegacyApp.Services;

            public class OrderRepository
            {
            }

            public class OrderService
            {
                public OrderService(OrderRepository repository)
                {
                }
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderService" &&
            dependency.TargetType == "OrderRepository" &&
            dependency.Kind == ClassDependencyKind.ConstructorParameter &&
            dependency.Evidence == "public OrderService(OrderRepository repository)");

        Assert.Contains(report.Concerns, concern =>
            concern.SourceType == "OrderService" &&
            concern.TargetType == "OrderRepository" &&
            concern.DependencyKind == ClassDependencyKind.ConstructorParameter &&
            concern.Severity == ClassDependencyConcernSeverity.Medium &&
            concern.WhyItMatters.Contains("Constructor injection is visible", StringComparison.OrdinalIgnoreCase));
    }
    
    [Fact]
    public void Analyze_WhenConstructorHasInterfaceParameter_DiscoversConstructorParameterWithoutConcreteConcern()
    {
        WriteSourceFile(
            "OrderService.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface IOrderRepository
            {
            }

            public class OrderService
            {
                public OrderService(IOrderRepository repository)
                {
                }
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderService" &&
            dependency.TargetType == "IOrderRepository" &&
            dependency.Kind == ClassDependencyKind.ConstructorParameter &&
            dependency.Evidence == "public OrderService(IOrderRepository repository)");

        Assert.DoesNotContain(report.Concerns, concern =>
            concern.SourceType == "OrderService" &&
            concern.TargetType == "IOrderRepository" &&
            concern.DependencyKind == ClassDependencyKind.ConstructorParameter);
    }

    [Fact]
    public void Analyze_ParsesMultilineConstructorParameters()
    {
        WriteSourceFile(
            "OrderService.cs",
            """
            namespace SampleLegacyApp.Services;

            public class OrderRepository
            {
            }

            public class OrderService
            {
                public OrderService(
                    OrderRepository repository)
                {
                }
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderService" &&
            dependency.TargetType == "OrderRepository" &&
            dependency.Kind == ClassDependencyKind.ConstructorParameter);
    }
    
    [Fact]
    public void Analyze_UsesSyntaxOwnershipForNestedTypes()
    {
        WriteSourceFile(
            "OuterService.cs",
            """
            namespace SampleLegacyApp.Services;

            public class OuterDependency
            {
            }

            public class InnerDependency
            {
            }

            public class OuterService
            {
                private readonly OuterDependency _outerDependency;

                public class InnerService
                {
                    private readonly InnerDependency _innerDependency;
                }
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OuterService" &&
            dependency.TargetType == "OuterDependency" &&
            dependency.Kind == ClassDependencyKind.Field);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "InnerService" &&
            dependency.TargetType == "InnerDependency" &&
            dependency.Kind == ClassDependencyKind.Field);

        Assert.DoesNotContain(report.Dependencies, dependency =>
            dependency.SourceType == "OuterService" &&
            dependency.TargetType == "InnerDependency");
    }
    
    [Fact]
    public void Analyze_IgnoresDependencyLookingTextInCommentsAndStrings()
    {
        WriteSourceFile(
            "OrderService.cs",
            """
            namespace SampleLegacyApp.Services;

            public class OrderRepository
            {
            }

            public class OrderService
            {
                public void Run()
                {
                    // new OrderRepository()
                    var text = "new OrderRepository()";
                }
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.DoesNotContain(report.Dependencies, dependency =>
            dependency.SourceType == "OrderService" &&
            dependency.TargetType == "OrderRepository" &&
            dependency.Kind == ClassDependencyKind.ObjectCreation);
    }
    
    [Fact]
    public void Analyze_DiscoversTargetTypedNewForLocalVariable()
    {
        WriteSourceFile(
            "OrderService.cs",
            """
            namespace SampleLegacyApp.Services;

            public class OrderRepository
            {
            }

            public class OrderService
            {
                public void Run()
                {
                    OrderRepository repository = new();
                }
            }
            """);

        var analyzer = new ClassDependencyAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderService" &&
            dependency.TargetType == "OrderRepository" &&
            dependency.Kind == ClassDependencyKind.LocalVariable);

        Assert.Contains(report.Dependencies, dependency =>
            dependency.SourceType == "OrderService" &&
            dependency.TargetType == "OrderRepository" &&
            dependency.Kind == ClassDependencyKind.ObjectCreation);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private static ScanFileInventory CreateInventory(IReadOnlyCollection<DiscoveredProject> projects)
    {
        return new ScanFileInventoryBuilder().Build(projects);
    }

    private DiscoveredProject CreateProject()
    {
        return new DiscoveredProject
        {
            Name = "SampleLegacyApp.Services",
            ProjectFilePath = _projectFilePath,
            TargetFramework = "net48"
        };
    }

    private void WriteSourceFile(string relativePath, string content)
    {
        var filePath = Path.Combine(_projectDirectory, relativePath);
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, content);
    }
}
