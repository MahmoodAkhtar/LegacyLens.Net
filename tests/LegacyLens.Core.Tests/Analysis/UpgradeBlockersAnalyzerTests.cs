using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class UpgradeBlockersAnalyzerTests
{
    [Fact]
    public void Analyze_WhenSystemWebReferenceExists_AddsLegacyAspNetBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Web",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "System.Web"
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal("net8.0", report.RequestedUpgradeTarget);
        Assert.Equal(1, blocker.Priority);
        Assert.Equal(UpgradeBlockerCategory.LegacyAspNetSystemWeb, blocker.Category);
        Assert.Equal(UpgradeBlockerImpact.High, blocker.Impact);
        Assert.Contains("classic ASP.NET", blocker.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("System.Web", blocker.WhyItMatters, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(blocker.DecisionsRequired);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal("SampleLegacyApp.Web", evidence.ProjectName);
        Assert.Equal(project.ProjectFilePath, evidence.Source);
        Assert.Contains("System.Web", evidence.Finding, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Possible blocker", evidence.Finding, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactExists_AddsLegacyAspNetBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Web",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
            TargetFramework = "net48"
        };

        var artifact = new DiscoveredLegacyAspNetArtifact
        {
            Kind = LegacyAspNetArtifactKind.WebFormsPage,
            Name = "Default.aspx",
            FilePath = @"C:\Code\SampleLegacyApp.Web\Default.aspx"
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            new[] { artifact },
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            null);

        var blocker = Assert.Single(report.Blockers);

        Assert.Null(report.RequestedUpgradeTarget);
        Assert.Equal(UpgradeBlockerCategory.LegacyAspNetSystemWeb, blocker.Category);
        Assert.Equal(UpgradeBlockerImpact.High, blocker.Impact);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal("SampleLegacyApp.Web", evidence.ProjectName);
        Assert.Equal(artifact.FilePath, evidence.Source);
        Assert.Contains("WebFormsPage", evidence.Finding);
        Assert.Contains("Default.aspx", evidence.Finding);
    }

    [Fact]
    public void Analyze_WhenSystemServiceModelReferenceExists_AddsWcfBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Services",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "System.ServiceModel"
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(UpgradeBlockerCategory.WcfServiceModel, blocker.Category);
        Assert.Equal(UpgradeBlockerImpact.High, blocker.Impact);
        Assert.Contains("WCF", blocker.Title);
        Assert.Contains("bindings", blocker.WhyItMatters, StringComparison.OrdinalIgnoreCase);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal("SampleLegacyApp.Services", evidence.ProjectName);
        Assert.Equal(project.ProjectFilePath, evidence.Source);
        Assert.Contains("System.ServiceModel", evidence.Finding);
    }

    [Fact]
    public void Analyze_WhenWcfEndpointExists_AddsWcfBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Web",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
            TargetFramework = "net48"
        };

        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
            ServiceName = "SampleLegacyApp.Services.CustomerService",
            Address = "",
            Binding = "basicHttpBinding",
            Contract = "SampleLegacyApp.Contracts.ICustomerContract"
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(UpgradeBlockerCategory.WcfServiceModel, blocker.Category);
        Assert.Equal(UpgradeBlockerImpact.High, blocker.Impact);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal("SampleLegacyApp.Web", evidence.ProjectName);
        Assert.Equal(endpoint.ConfigFilePath, evidence.Source);
        Assert.Contains("WCF endpoint", evidence.Finding);
        Assert.Contains("basicHttpBinding", evidence.Finding);
    }

    [Fact]
    public void Analyze_WhenWcfServiceContractExists_AddsWcfBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Contracts",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj",
            TargetFramework = "net48"
        };

        var serviceContract = new WcfServiceContract
        {
            Name = "ICustomerContract",
            SourceFilePath = @"C:\Code\SampleLegacyApp.Contracts\CustomerContracts.cs",
            Operations =
            {
                "GetCustomer"
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            new[] { serviceContract },
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(UpgradeBlockerCategory.WcfServiceModel, blocker.Category);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal("SampleLegacyApp.Contracts", evidence.ProjectName);
        Assert.Equal(serviceContract.SourceFilePath, evidence.Source);
        Assert.Contains("ICustomerContract", evidence.Finding);
        Assert.Contains("service contract", evidence.Finding, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_WhenWcfBehaviourExists_AddsWcfBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Web",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
            TargetFramework = "net48"
        };

        var behaviour = new WcfBehaviour
        {
            Kind = WcfBehaviourKind.ServiceBehaviour,
            Name = "CustomerServiceBehaviour",
            ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
            HasServiceMetadata = true,
            ServiceMetadataHttpGetEnabled = "true"
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[] { behaviour },
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(UpgradeBlockerCategory.WcfServiceModel, blocker.Category);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal("SampleLegacyApp.Web", evidence.ProjectName);
        Assert.Equal(behaviour.ConfigFilePath, evidence.Source);
        Assert.Contains("CustomerServiceBehaviour", evidence.Finding);
    }

    [Fact]
    public void Analyze_WhenSystemServiceModelPackageExists_AddsWcfBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Web",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
            TargetFramework = "net48",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "System.ServiceModel.Http",
                    Version = "4.10.3",
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj"
                }
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(UpgradeBlockerCategory.WcfServiceModel, blocker.Category);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal("SampleLegacyApp.Web", evidence.ProjectName);
        Assert.Contains("System.ServiceModel.Http", evidence.Finding);
        Assert.Contains("4.10.3", evidence.Finding);
    }

    [Fact]
    public void Analyze_WhenEntityFrameworkPackageExists_AddsDataAccessBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "EntityFramework",
                    Version = "6.4.4",
                    SourceFormat = "packages.config",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\packages.config",
                    PackageTargetFramework = "net48"
                }
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        Assert.Contains(
            report.Blockers,
            blocker =>
                blocker.Category == UpgradeBlockerCategory.Ef6EdmxDataAccess &&
                blocker.Impact == UpgradeBlockerImpact.Medium &&
                blocker.Evidence.Any(evidence =>
                    evidence.ProjectName == "SampleLegacyApp.Data" &&
                    evidence.Source == @"C:\Code\SampleLegacyApp.Data\packages.config" &&
                    evidence.Finding.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) &&
                    evidence.Finding.Contains("6.4.4", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Analyze_WhenModernisationHintContainsEdmxEvidence_AddsDataAccessBlocker()
    {
        var hint = new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Risk,
            Area = "Data Access",
            Finding = "Model.edmx file discovered",
            Reason = "EDMX models may need migration review.",
            EvidencePath = @"C:\Code\SampleLegacyApp.Data\Model.edmx"
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            new[] { hint },
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(UpgradeBlockerCategory.Ef6EdmxDataAccess, blocker.Category);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal(@"C:\Code\SampleLegacyApp.Data\Model.edmx", evidence.Source);
        Assert.Contains("Model.edmx", evidence.Finding);
    }

    [Fact]
    public void Analyze_WhenPackagesConfigExists_AddsPackageManagementBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "Dapper",
                    Version = "2.1.35",
                    SourceFormat = "packages.config",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\packages.config",
                    PackageTargetFramework = "net48"
                }
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        Assert.Contains(
            report.Blockers,
            blocker =>
                blocker.Category == UpgradeBlockerCategory.PackageManagement &&
                blocker.Impact == UpgradeBlockerImpact.Medium &&
                blocker.Evidence.Any(evidence =>
                    evidence.Finding.Contains("packages.config", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Analyze_WhenPackageVersionIsMissing_AddsPackageManagementBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Web",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
            TargetFramework = "net48",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "Newtonsoft.Json",
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj"
                }
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(UpgradeBlockerCategory.PackageManagement, blocker.Category);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Contains("Newtonsoft.Json", evidence.Finding);
        Assert.Contains("version", evidence.Finding, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_WhenPackageTargetFrameworkDiffersFromProject_AddsPackageManagementBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "Newtonsoft.Json",
                    Version = "13.0.3",
                    SourceFormat = "packages.config",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\packages.config",
                    PackageTargetFramework = "net472"
                }
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(UpgradeBlockerCategory.PackageManagement, blocker.Category);

        Assert.Contains(
            blocker.Evidence,
            evidence =>
                evidence.Finding.Contains("net472", StringComparison.OrdinalIgnoreCase) &&
                evidence.Finding.Contains("net48", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_WhenDirectAssemblyReferenceExists_AddsDirectAssemblyReferenceBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Services",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "Legacy.Vendor.Component"
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(UpgradeBlockerCategory.DirectAssemblyReferences, blocker.Category);
        Assert.Equal(UpgradeBlockerImpact.Medium, blocker.Impact);
        Assert.Contains("assembly", blocker.Title, StringComparison.OrdinalIgnoreCase);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal("SampleLegacyApp.Services", evidence.ProjectName);
        Assert.Equal(project.ProjectFilePath, evidence.Source);
        Assert.Contains("Legacy.Vendor.Component", evidence.Finding);
    }

    [Fact]
    public void Analyze_WhenOnlySystemWebAssemblyReferenceExists_DoesNotAlsoAddDirectAssemblyReferenceBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Web",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "System.Web"
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        Assert.Contains(
            report.Blockers,
            blocker => blocker.Category == UpgradeBlockerCategory.LegacyAspNetSystemWeb);

        Assert.DoesNotContain(
            report.Blockers,
            blocker => blocker.Category == UpgradeBlockerCategory.DirectAssemblyReferences);
    }

    [Fact]
    public void Analyze_WhenOnlySystemServiceModelAssemblyReferenceExists_DoesNotAlsoAddDirectAssemblyReferenceBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Services",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "System.ServiceModel"
            }
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        Assert.Contains(
            report.Blockers,
            blocker => blocker.Category == UpgradeBlockerCategory.WcfServiceModel);

        Assert.DoesNotContain(
            report.Blockers,
            blocker => blocker.Category == UpgradeBlockerCategory.DirectAssemblyReferences);
    }

    [Fact]
    public void Analyze_WhenConfigFileExists_AddsConfigurationRuntimeBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Web",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
            TargetFramework = "net48"
        };

        var configFile = new DiscoveredConfigFile
        {
            FilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
            AppSettingsCount = 2,
            ConnectionStringsCount = 1,
            CustomSectionCount = 1
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            new[] { configFile },
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(UpgradeBlockerCategory.ConfigurationRuntimeCoupling, blocker.Category);
        Assert.Equal(UpgradeBlockerImpact.Medium, blocker.Impact);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal("SampleLegacyApp.Web", evidence.ProjectName);
        Assert.Equal(configFile.FilePath, evidence.Source);
        Assert.Contains("appSettings", evidence.Finding);
        Assert.Contains("connection strings", evidence.Finding);
        Assert.Contains("custom sections", evidence.Finding);
    }

    [Fact]
    public void Analyze_WhenNoStaticBlockersMatch_AddsUnknownManualReviewBlocker()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleModernLibrary",
            ProjectFilePath = @"C:\Code\SampleModernLibrary\SampleModernLibrary.csproj",
            TargetFramework = "net8.0"
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            null);

        var blocker = Assert.Single(report.Blockers);

        Assert.Equal(1, blocker.Priority);
        Assert.Equal(UpgradeBlockerCategory.UnknownRequiresManualReview, blocker.Category);
        Assert.Equal(UpgradeBlockerImpact.Unknown, blocker.Impact);
        Assert.Contains("No visible MVP upgrade blocker", blocker.Title);
        Assert.Contains("not a compatibility guarantee", blocker.WhyItMatters, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(blocker.DecisionsRequired);

        var evidence = Assert.Single(blocker.Evidence);

        Assert.Equal("Static analysis summary", evidence.Source);
        Assert.Contains("No configured MVP blocker rule", evidence.Finding);
    }

    [Fact]
    public void Analyze_WhenMultipleBlockerTypesExist_OrdersBlockersByCategoryPriority()
    {
        var webProject = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Web",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "System.Web",
                "Legacy.Vendor.Component"
            },
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "System.ServiceModel.Http",
                    Version = "4.10.3",
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj"
                }
            }
        };

        var dataProject = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "EntityFramework",
                    Version = "6.4.4",
                    SourceFormat = "packages.config",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\packages.config",
                    PackageTargetFramework = "net48"
                }
            }
        };

        var configFile = new DiscoveredConfigFile
        {
            FilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
            AppSettingsCount = 2,
            ConnectionStringsCount = 1,
            CustomSectionCount = 1
        };

        var analyzer = new UpgradeBlockersAnalyzer();

        var report = analyzer.Analyze(
            new[] { webProject, dataProject },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            new[] { configFile },
            Array.Empty<ModernisationHint>(),
            "net8.0");

        Assert.Collection(
            report.Blockers,
            blocker =>
            {
                Assert.Equal(1, blocker.Priority);
                Assert.Equal(UpgradeBlockerCategory.LegacyAspNetSystemWeb, blocker.Category);
            },
            blocker =>
            {
                Assert.Equal(2, blocker.Priority);
                Assert.Equal(UpgradeBlockerCategory.WcfServiceModel, blocker.Category);
            },
            blocker =>
            {
                Assert.Equal(3, blocker.Priority);
                Assert.Equal(UpgradeBlockerCategory.Ef6EdmxDataAccess, blocker.Category);
            },
            blocker =>
            {
                Assert.Equal(4, blocker.Priority);
                Assert.Equal(UpgradeBlockerCategory.PackageManagement, blocker.Category);
            },
            blocker =>
            {
                Assert.Equal(5, blocker.Priority);
                Assert.Equal(UpgradeBlockerCategory.DirectAssemblyReferences, blocker.Category);
            },
            blocker =>
            {
                Assert.Equal(6, blocker.Priority);
                Assert.Equal(UpgradeBlockerCategory.ConfigurationRuntimeCoupling, blocker.Category);
            });
    }

    [Fact]
    public void Analyze_WhenInputsAreNull_ThrowsArgumentNullException()
    {
        var analyzer = new UpgradeBlockersAnalyzer();

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                null!,
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<WcfBehaviour>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<DiscoveredConfigFile>(),
                Array.Empty<ModernisationHint>(),
                "net8.0"));

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                null!,
                Array.Empty<WcfServiceContract>(),
                Array.Empty<WcfBehaviour>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<DiscoveredConfigFile>(),
                Array.Empty<ModernisationHint>(),
                "net8.0"));

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                null!,
                Array.Empty<WcfBehaviour>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<DiscoveredConfigFile>(),
                Array.Empty<ModernisationHint>(),
                "net8.0"));

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                null!,
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<DiscoveredConfigFile>(),
                Array.Empty<ModernisationHint>(),
                "net8.0"));

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<WcfBehaviour>(),
                null!,
                Array.Empty<DiscoveredConfigFile>(),
                Array.Empty<ModernisationHint>(),
                "net8.0"));

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<WcfBehaviour>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                null!,
                Array.Empty<ModernisationHint>(),
                "net8.0"));

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<WcfBehaviour>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<DiscoveredConfigFile>(),
                null!,
                "net8.0"));
    }
}