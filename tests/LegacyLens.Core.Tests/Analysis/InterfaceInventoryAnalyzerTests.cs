
using LegacyLens.Core.Analysis;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class InterfaceInventoryAnalyzerTests : IDisposable
{
    private readonly string _rootPath;
    private readonly string _projectDirectory;
    private readonly string _projectFilePath;

    public InterfaceInventoryAnalyzerTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.InterfaceInventoryAnalyzerTests",
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
        var analyzer = new InterfaceInventoryAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze((ScanFileInventory)null!));

        Assert.Equal("fileInventory", exception.ParamName);
    }

    [Fact]
    public void Analyze_DiscoversInterfacesImplementationsConsumersAndMicrosoftDiRegistrations()
    {
        WriteSourceFile(
            "CustomerService.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface ICustomerService
            {
                CustomerDto GetCustomer(int id);
            }

            public interface ICustomerRepository
            {
            }

            public sealed class CustomerService : ICustomerService
            {
                private readonly ICustomerRepository _repository;

                public CustomerService(ICustomerRepository repository)
                {
                    _repository = repository;
                }

                public CustomerDto GetCustomer(int id) => new();
            }

            public sealed class CachedCustomerService : ICustomerService
            {
                public CustomerDto GetCustomer(int id) => new();
            }

            public sealed class CustomerRepository : ICustomerRepository
            {
            }

            public sealed class CustomerController
            {
                public CustomerController(ICustomerService service)
                {
                }
            }

            public sealed class CustomerDto
            {
            }
            """);

        WriteSourceFile(
            "Program.cs",
            """
            using SampleLegacyApp.Services;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<ICustomerService, CustomerService>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            var app = builder.Build();
            app.MapGet("/customers/{id:int}", (int id, ICustomerService customerService) => customerService.GetCustomer(id));
            """);

        var analyzer = new InterfaceInventoryAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Interfaces, item =>
            item.Name == "ICustomerService" &&
            item.LikelyRole == "Service boundary" &&
            item.IsPossibleExtensionPoint);

        Assert.Contains(report.Interfaces, item =>
            item.Name == "ICustomerRepository" &&
            item.LikelyRole == "Repository abstraction");

        Assert.Contains(report.Implementations, item =>
            item.InterfaceName == "ICustomerService" &&
            item.ImplementationType == "CustomerService");

        Assert.Contains(report.Implementations, item =>
            item.InterfaceName == "ICustomerService" &&
            item.ImplementationType == "CachedCustomerService");

        Assert.Contains(report.Consumers, item =>
            item.InterfaceName == "ICustomerService" &&
            item.ConsumerType == "CustomerController" &&
            item.Kind == InterfaceConsumerKind.ConstructorParameter);

        Assert.Contains(report.Consumers, item =>
            item.InterfaceName == "ICustomerRepository" &&
            item.ConsumerType == "CustomerService" &&
            item.Kind == InterfaceConsumerKind.Field);

        Assert.Contains(report.Consumers, item =>
            item.InterfaceName == "ICustomerService" &&
            item.ConsumerType == "MinimalApiEndpoint" &&
            item.Kind == InterfaceConsumerKind.EndpointDelegateParameter);

        Assert.Contains(report.Registrations, item =>
            item.InterfaceName == "ICustomerService" &&
            item.ImplementationType == "CustomerService" &&
            item.Kind == InterfaceRegistrationKind.MicrosoftDependencyInjection &&
            !item.RequiresReview);

        Assert.Contains(report.Findings, item =>
            item.InterfaceName == "ICustomerService" &&
            item.Finding == "Multiple static implementations found");

        Assert.Contains(report.Findings, item =>
            item.InterfaceName == "ICustomerService" &&
            item.Finding == "Registration evidence found");
    }

    [Fact]
    public void Analyze_DiscoversMissingImplementationMissingConsumerAndDynamicServiceLocatorEvidence()
    {
        WriteSourceFile(
            "PluginTypes.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface IPluginExtension
            {
            }

            public interface IOrphanService
            {
            }

            public sealed class PluginHost
            {
                public void Load()
                {
                    var plugin = ServiceLocator.Current.GetInstance<IPluginExtension>();
                }
            }
            """);

        var analyzer = new InterfaceInventoryAnalyzer();

        var report = analyzer.Analyze(CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Consumers, item =>
            item.InterfaceName == "IPluginExtension" &&
            item.Kind == InterfaceConsumerKind.ServiceLocator);

        Assert.Contains(report.Registrations, item =>
            item.InterfaceName == "IPluginExtension" &&
            item.Kind == InterfaceRegistrationKind.CommonServiceLocator &&
            item.RequiresReview);

        Assert.Contains(report.Findings, item =>
            item.InterfaceName == "IPluginExtension" &&
            item.Finding == "No static implementation found");

        Assert.Contains(report.Findings, item =>
            item.InterfaceName == "IOrphanService" &&
            item.Finding == "No static implementation found");

        Assert.Contains(report.Findings, item =>
            item.InterfaceName == "IOrphanService" &&
            item.Finding == "No static consumer found");
    }

    [Fact]
    public void Analyze_DiscoversConfigurationDrivenSpringAndUnityRegistrationEvidence()
    {
        WriteSourceFile(
            "ServiceTypes.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface ICustomerService
            {
            }

            public sealed class CustomerService : ICustomerService
            {
            }
            """);

        File.WriteAllText(
            Path.Combine(_projectDirectory, "spring-objects.xml"),
            """
            <objects xmlns="http://www.springframework.net">
              <object id="customerService" type="SampleLegacyApp.Services.CustomerService, SampleLegacyApp.Services" singleton="true">
                <property name="serviceInterface" value="SampleLegacyApp.Services.ICustomerService" />
              </object>
            </objects>
            """);

        File.WriteAllText(
            Path.Combine(_projectDirectory, "unity.config"),
            """
            <configuration>
              <unity>
                <container>
                  <register type="SampleLegacyApp.Services.ICustomerService" mapTo="SampleLegacyApp.Services.CustomerService" />
                </container>
              </unity>
            </configuration>
            """);

        var analyzer = new InterfaceInventoryAnalyzer();

        var report = analyzer.Analyze(
            new[] { CreateProject() },
            CreateInventory(new[] { CreateProject() }));

        Assert.Contains(report.Registrations, item =>
            item.InterfaceName == "ICustomerService" &&
            item.Kind == InterfaceRegistrationKind.SpringNetXml &&
            item.RequiresReview);

        Assert.Contains(report.Registrations, item =>
            item.InterfaceName == "ICustomerService" &&
            item.Kind == InterfaceRegistrationKind.UnityXml &&
            item.RequiresReview);

        Assert.Contains(report.Findings, item =>
            item.InterfaceName == "ICustomerService" &&
            item.Finding == "Dynamic or configuration-driven wiring requires review");

        Assert.Equal(2, report.ConfigurationFileCount);
    }


    [Fact]
    public void Analyze_IgnoresSpringNetXmlCommentsWhenDiscoveringRegistrationEvidence()
    {
        WriteSourceFile(
            "ServiceTypes.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface ICustomerService
            {
            }

            public sealed class CustomerService : ICustomerService
            {
            }
            """);

        File.WriteAllText(
            Path.Combine(_projectDirectory, "spring-objects.xml"),
            """
            <objects xmlns="http://www.springframework.net">
              <!-- Expected interface-inventory.md signal for ICustomerService and CustomerService. -->
              <object id="customerService" type="SampleLegacyApp.Services.CustomerService, SampleLegacyApp.Services" />
            </objects>
            """);

        var analyzer = new InterfaceInventoryAnalyzer();

        var report = analyzer.Analyze(
            new[] { CreateProject() },
            CreateInventory(new[] { CreateProject() }));

        Assert.DoesNotContain(report.Registrations, item =>
            item.Kind == InterfaceRegistrationKind.SpringNetXml &&
            item.InterfaceName == "ICustomerService");

        Assert.DoesNotContain(report.Registrations, item =>
            item.Evidence.Contains("Expected interface-inventory.md signal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_IgnoresSpringNetDescriptionTextWhenDiscoveringRegistrationEvidence()
    {
        WriteSourceFile(
            "ServiceTypes.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface ICustomerService
            {
            }

            public sealed class CustomerService : ICustomerService
            {
            }
            """);

        File.WriteAllText(
            Path.Combine(_projectDirectory, "spring-objects.xml"),
            """
            <objects xmlns="http://www.springframework.net">
              <object id="customerService" type="SampleLegacyApp.Services.CustomerService, SampleLegacyApp.Services">
                <description>Manual-test Spring.NET object definition for ICustomerService.</description>
              </object>
            </objects>
            """);

        var analyzer = new InterfaceInventoryAnalyzer();

        var report = analyzer.Analyze(
            new[] { CreateProject() },
            CreateInventory(new[] { CreateProject() }));

        Assert.DoesNotContain(report.Registrations, item =>
            item.Kind == InterfaceRegistrationKind.SpringNetXml &&
            item.InterfaceName == "ICustomerService");

        Assert.DoesNotContain(report.Registrations, item =>
            item.Evidence.Contains("<description", StringComparison.OrdinalIgnoreCase) ||
            item.Evidence.Contains("Manual-test Spring.NET object definition", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_DoesNotReportRootSpringObjectsElementAsRegistrationEvidence()
    {
        WriteSourceFile(
            "ServiceTypes.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface ICustomerService
            {
            }

            public sealed class CustomerService : ICustomerService
            {
            }
            """);

        File.WriteAllText(
            Path.Combine(_projectDirectory, "spring-objects.xml"),
            """
            <objects xmlns="http://www.springframework.net">
              <!-- ICustomerService and CustomerService are mentioned only in root descendant comment text. -->
              Manual-test Spring.NET object definition for ICustomerService.
            </objects>
            """);

        var analyzer = new InterfaceInventoryAnalyzer();

        var report = analyzer.Analyze(
            new[] { CreateProject() },
            CreateInventory(new[] { CreateProject() }));

        Assert.DoesNotContain(report.Registrations, item =>
            item.Kind == InterfaceRegistrationKind.SpringNetXml &&
            item.Evidence.Contains("<objects", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(report.Registrations, item =>
            item.InterfaceName == "ICustomerService" &&
            item.Kind == InterfaceRegistrationKind.SpringNetXml);
    }

    [Fact]
    public void Analyze_UsesSpringNetObjectAttributesForRegistrationEvidence()
    {
        WriteSourceFile(
            "ServiceTypes.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface ICustomerService
            {
            }

            public sealed class CustomerService : ICustomerService
            {
            }
            """);

        File.WriteAllText(
            Path.Combine(_projectDirectory, "spring-objects.xml"),
            """
            <objects xmlns="http://www.springframework.net">
              <!-- Expected interface-inventory.md signal for ICustomerService. -->
              <object id="customerService" type="SampleLegacyApp.Services.CustomerService, SampleLegacyApp.Services">
                <description>Manual-test Spring.NET object definition for ICustomerService.</description>
                <property name="serviceInterface" value="SampleLegacyApp.Services.ICustomerService" />
              </object>
            </objects>
            """);

        var analyzer = new InterfaceInventoryAnalyzer();

        var report = analyzer.Analyze(
            new[] { CreateProject() },
            CreateInventory(new[] { CreateProject() }));

        var registration = Assert.Single(report.Registrations.Where(item =>
            item.InterfaceName == "ICustomerService" &&
            item.Kind == InterfaceRegistrationKind.SpringNetXml));

        Assert.Equal("CustomerService", registration.ImplementationType);
        Assert.True(registration.RequiresReview);
        Assert.Contains("<object", registration.Evidence);
        Assert.Contains("type=\"SampleLegacyApp.Services.CustomerService, SampleLegacyApp.Services\"", registration.Evidence);
        Assert.Contains("<property", registration.Evidence);
        Assert.Contains("value=\"SampleLegacyApp.Services.ICustomerService\"", registration.Evidence);
        Assert.DoesNotContain("Expected interface-inventory.md signal", registration.Evidence);
        Assert.DoesNotContain("<description", registration.Evidence);
        Assert.DoesNotContain("Manual-test Spring.NET object definition", registration.Evidence);
    }

    [Fact]
    public void Analyze_SimplifiesAssemblyQualifiedXmlTypeNamesCorrectly()
    {
        WriteSourceFile(
            "ServiceTypes.cs",
            """
            namespace SampleLegacyApp.Services;

            public interface ICustomerService
            {
            }

            public sealed class CustomerService : ICustomerService
            {
            }
            """);

        File.WriteAllText(
            Path.Combine(_projectDirectory, "spring-objects.xml"),
            """
            <objects xmlns="http://www.springframework.net">
              <object id="customerService" type="SampleLegacyApp.Services.CustomerService, SampleLegacyApp.Services">
                <property name="serviceInterface" value="SampleLegacyApp.Services.ICustomerService, SampleLegacyApp.Services" />
              </object>
            </objects>
            """);

        var analyzer = new InterfaceInventoryAnalyzer();

        var report = analyzer.Analyze(
            new[] { CreateProject() },
            CreateInventory(new[] { CreateProject() }));

        var registration = Assert.Single(report.Registrations.Where(item =>
            item.InterfaceName == "ICustomerService" &&
            item.Kind == InterfaceRegistrationKind.SpringNetXml));

        Assert.Equal("ICustomerService", registration.InterfaceName);
        Assert.Equal("CustomerService", registration.ImplementationType);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private void WriteSourceFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_projectDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
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

    private static ScanFileInventory CreateInventory(IReadOnlyCollection<DiscoveredProject> projects)
    {
        return new ScanFileInventoryBuilder().Build(projects);
    }
}
