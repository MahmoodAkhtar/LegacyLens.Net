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
