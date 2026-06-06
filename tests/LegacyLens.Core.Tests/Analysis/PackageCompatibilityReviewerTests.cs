using LegacyLens.Core.Analysis;
using LegacyLens.Core.Discovery;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class PackageCompatibilityReviewerTests
{
    [Fact]
    public void Review_ReturnsPackageCompatibilityEvidenceAndConcerns()
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

        var reviewer = new PackageCompatibilityReviewer();

        var items = reviewer.Review(new[] { project });

        var item = Assert.Single(items);
        Assert.Equal("SampleLegacyApp.Data", item.ProjectName);
        Assert.Equal("net48", item.ProjectTargetFramework);
        Assert.Equal("EntityFramework", item.PackageName);
        Assert.Equal("6.4.4", item.Version);
        Assert.Equal("net48", item.PackageTargetFramework);
        Assert.Equal("packages.config", item.SourceFormat);
        Assert.Equal(@"C:\Code\SampleLegacyApp.Data\packages.config", item.SourcePath);
        Assert.Contains("Classic Entity Framework", item.Concern);
        Assert.Contains("Project targets .NET Framework", item.Concern);
    }

    [Fact]
    public void Review_ReturnsMissingVersionConcern_WhenPackageReferenceHasNoVersion()
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
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj"
                }
            }
        };

        var reviewer = new PackageCompatibilityReviewer();

        var item = Assert.Single(reviewer.Review(new[] { project }));

        Assert.Equal("unknown", item.Version ?? "unknown");
        Assert.Contains("Package version was not found", item.Concern);
        Assert.Contains("WCF-related package", item.Concern);
    }

    [Fact]
    public void Review_ReturnsTargetFrameworkMismatchConcern_WhenPackageTargetFrameworkDiffers()
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

        var reviewer = new PackageCompatibilityReviewer();

        var item = Assert.Single(reviewer.Review(new[] { project }));

        Assert.Contains("Package target framework differs", item.Concern);
        Assert.Contains("serialization behaviour", item.Concern);
    }
}
