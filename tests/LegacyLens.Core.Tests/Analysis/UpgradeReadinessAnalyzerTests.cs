using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class UpgradeReadinessAnalyzerTests
{
    [Fact]
    public void Analyze_WhenProjectTargetsNet48_ClassifiesAsModerateReviewRequired()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Contracts",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj",
            TargetFramework = "net48"
        };

        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var readiness = Assert.Single(report.ProjectReadiness);

        Assert.Equal("SampleLegacyApp.Contracts", readiness.ProjectName);
        Assert.Equal("net48", readiness.CurrentTargetFramework);
        Assert.Equal(UpgradeReadinessLevel.ModerateReviewRequired, readiness.Readiness);
        Assert.Contains(".NET Framework", readiness.Reason);
    }

    [Fact]
    public void Analyze_WhenProjectHasNoTargetFramework_ClassifiesAsUnknown()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Unknown",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Unknown\SampleLegacyApp.Unknown.csproj"
        };

        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            null);

        var readiness = Assert.Single(report.ProjectReadiness);

        Assert.Equal("SampleLegacyApp.Unknown", readiness.ProjectName);
        Assert.Null(readiness.CurrentTargetFramework);
        Assert.Equal(UpgradeReadinessLevel.Unknown, readiness.Readiness);
        Assert.Contains("No target framework", readiness.Reason);
    }

    [Fact]
    public void Analyze_WhenProjectHasNoMajorStaticConcerns_ClassifiesAsLowerRiskCandidate()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleModernLibrary",
            ProjectFilePath = @"C:\Code\SampleModernLibrary\SampleModernLibrary.csproj",
            TargetFramework = "net8.0"
        };

        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var readiness = Assert.Single(report.ProjectReadiness);

        Assert.Equal("SampleModernLibrary", readiness.ProjectName);
        Assert.Equal(UpgradeReadinessLevel.LowerRiskCandidate, readiness.Readiness);
        Assert.Contains("No major MVP upgrade-readiness concern", readiness.Reason);
    }

    [Fact]
    public void Analyze_WhenSystemWebReferenceExists_ClassifiesAsHigherRiskReviewFirst()
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

        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var readiness = Assert.Single(report.ProjectReadiness);

        Assert.Equal("SampleLegacyApp.Web", readiness.ProjectName);
        Assert.Equal(UpgradeReadinessLevel.HigherRiskReviewFirst, readiness.Readiness);
        Assert.Contains("Legacy ASP.NET", readiness.Reason);

        Assert.Contains(
            report.Overview,
            item =>
                item.Area == "Legacy ASP.NET" &&
                item.Status == "Possible blocker");

        Assert.Contains(
            report.Concerns,
            concern =>
                concern.Concern == "Legacy ASP.NET runtime" &&
                concern.WhyItMatters.Contains("System.Web"));
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactExists_ClassifiesMatchingProjectAsHigherRiskReviewFirst()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Web",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
            TargetFramework = "net48"
        };

        var artifact = new DiscoveredLegacyAspNetArtifact
        {
            Kind = LegacyAspNetArtifactKind.GlobalAsax,
            Name = "Global.asax",
            FilePath = @"C:\Code\SampleLegacyApp.Web\Global.asax"
        };

        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            new[] { artifact },
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var readiness = Assert.Single(report.ProjectReadiness);

        Assert.Equal(UpgradeReadinessLevel.HigherRiskReviewFirst, readiness.Readiness);
        Assert.Contains("Legacy ASP.NET", readiness.Reason);

        Assert.Contains(
            report.ConfigurationRuntimeConsiderations,
            item =>
                item.Source == "Legacy ASP.NET discovery" &&
                item.Finding.Contains("1 legacy ASP.NET artifact"));
    }

    [Fact]
    public void Analyze_WhenWcfEvidenceExists_AddsWcfConcern()
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

        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
            ServiceName = "SampleLegacyApp.Services.CustomerService",
            Address = "",
            Binding = "basicHttpBinding",
            Contract = "SampleLegacyApp.Contracts.ICustomerContract"
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

        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            new[] { endpoint },
            new[] { serviceContract },
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        Assert.Contains(
            report.Overview,
            item =>
                item.Area == "WCF" &&
                item.Status == "Requires review");

        Assert.Contains(
            report.Concerns,
            concern =>
                concern.Concern == "WCF usage" &&
                concern.WhyItMatters.Contains("WCF service boundaries"));

        Assert.Contains(
            report.AssemblyConsiderations,
            assembly =>
                assembly.ProjectName == "SampleLegacyApp.Services" &&
                assembly.AssemblyName == "System.ServiceModel" &&
                assembly.PossibleConcern.Contains("WCF"));

        Assert.Contains(
            report.ConfigurationRuntimeConsiderations,
            item =>
                item.Source == "WCF discovery" &&
                item.Finding.Contains("1 endpoint"));
    }

    [Fact]
    public void Analyze_WhenEntityFrameworkPackageExists_AddsPackageConsideration()
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

        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var package = Assert.Single(report.PackageConsiderations);

        Assert.Equal("SampleLegacyApp.Data", package.ProjectName);
        Assert.Equal("EntityFramework", package.PackageName);
        Assert.Equal("6.4.4", package.Version);
        Assert.Equal("net48", package.ProjectTargetFramework);
        Assert.Equal("net48", package.PackageTargetFramework);
        Assert.Equal("packages.config", package.SourceFormat);
        Assert.Equal(@"C:\Code\SampleLegacyApp.Data\packages.config", package.SourcePath);
        Assert.Contains("Classic Entity Framework", package.PossibleConcern);

        Assert.Contains(
            report.Overview,
            item =>
                item.Area == "Data access" &&
                item.Status == "Requires review");
    }

    [Fact]
    public void Analyze_WhenPackagesConfigExists_AddsPackageManagementOverviewItem()
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

        var analyzer = new UpgradeReadinessAnalyzer();

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
            report.Overview,
            item =>
                item.Area == "Package management" &&
                item.Evidence.Contains("packages.config"));
    }

    [Fact]
    public void Analyze_WhenConfigContainsConnectionStrings_AddsDatabaseRuntimeConcern()
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

        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            new[] { configFile },
            Array.Empty<ModernisationHint>(),
            "net8.0");

        Assert.Contains(
            report.Overview,
            item =>
                item.Area == "Configuration" &&
                item.Status == "Requires review");

        Assert.Contains(
            report.Concerns,
            concern =>
                concern.Concern == "Database/runtime dependencies" &&
                concern.Evidence.Contains("Connection strings"));

        Assert.Contains(
            report.Concerns,
            concern =>
                concern.Concern == "Custom configuration sections" &&
                concern.Evidence.Contains("Custom configSections"));

        Assert.Contains(
            report.ConfigurationRuntimeConsiderations,
            item =>
                item.Source == @"C:\Code\SampleLegacyApp.Web\Web.config" &&
                item.Finding.Contains("appSettings: 2") &&
                item.Finding.Contains("connection strings: 1") &&
                item.Finding.Contains("custom sections: 1"));
    }

    [Fact]
    public void Analyze_WhenDirectAssemblyReferenceExists_AddsAssemblyConsideration()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Infrastructure",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Infrastructure\SampleLegacyApp.Infrastructure.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "Legacy.Vendor.Library"
            }
        };

        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        var assembly = Assert.Single(report.AssemblyConsiderations);

        Assert.Equal("SampleLegacyApp.Infrastructure", assembly.ProjectName);
        Assert.Equal("Legacy.Vendor.Library", assembly.AssemblyName);
        Assert.Equal(@"C:\Code\SampleLegacyApp.Infrastructure\SampleLegacyApp.Infrastructure.csproj", assembly.ProjectFilePath);
        Assert.Contains("Direct assembly reference", assembly.PossibleConcern);

        Assert.Contains(
            report.Overview,
            item =>
                item.Area == "Direct assemblies" &&
                item.Status == "Requires review");
    }

    [Fact]
    public void Analyze_PreservesRequestedUpgradeTarget()
    {
        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            "net8.0");

        Assert.Equal("net8.0", report.RequestedUpgradeTarget);
    }

    [Fact]
    public void Analyze_WhenNoEvidenceExists_AddsNoMajorConcernOverviewItem()
    {
        var analyzer = new UpgradeReadinessAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            null);

        var overview = Assert.Single(report.Overview);

        Assert.Equal("Static evidence", overview.Area);
        Assert.Equal("No major concern detected", overview.Status);
    }
}