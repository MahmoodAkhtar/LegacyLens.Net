using LegacyLens.Core.Analysis;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationReviewPrioritiserTests
{
    [Fact]
    public void Prioritise_WhenHintsIsNull_Throws()
    {
        var prioritiser = new ModernisationReviewPrioritiser();

        Assert.Throws<ArgumentNullException>(() => prioritiser.Prioritise(null!));
    }

    [Fact]
    public void Prioritise_WhenNoHints_ReturnsEmptyList()
    {
        var prioritiser = new ModernisationReviewPrioritiser();

        var reviewAreas = prioritiser.Prioritise(Array.Empty<ModernisationHint>());

        Assert.Empty(reviewAreas);
    }

    [Fact]
    public void Prioritise_WhenWcfHintsExist_GroupsThemAsWcfMigration()
    {
        var hints = new[]
        {
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Risk,
                Area = "WCF",
                Finding = "1 WCF endpoint(s) discovered",
                Reason = "Configured WCF endpoints usually represent service boundaries or integration points."
            },
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Warning,
                Area = "WCF Binding",
                Finding = "basicHttpBinding endpoint discovered for CustomerService",
                Reason = "basicHttpBinding commonly indicates SOAP interoperability."
            },
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Info,
                Area = "WCF Timeout",
                Finding = "CustomerService has explicit WCF timeout settings",
                Reason = "Configured WCF timeout values should be reviewed."
            }
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var reviewAreas = prioritiser.Prioritise(hints);

        var reviewArea = Assert.Single(reviewAreas);

        Assert.Equal("WCF migration", reviewArea.Area);
        Assert.Equal(ModernisationHintSeverity.Risk, reviewArea.HighestSeverity);
        Assert.Equal(1, reviewArea.RiskCount);
        Assert.Equal(1, reviewArea.WarningCount);
        Assert.Equal(1, reviewArea.InfoCount);
        Assert.Contains("Review service boundaries", reviewArea.Summary);
    }

    [Fact]
    public void Prioritise_WhenRoutingHintsExist_GroupsThemAsRoutingReview()
    {
        var hints = new[]
        {
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Info,
                Area = "Legacy ASP.NET Routing",
                Finding = "RouteConfig.RegisterRoutes registers ASP.NET routes",
                Reason = "Route registration calls identify conventional route setup."
            },
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Info,
                Area = "Legacy ASP.NET Web API Routing",
                Finding = "MapHttpRoute registers ASP.NET Web API routes",
                Reason = "Web API route registration calls identify conventional HTTP API route setup."
            }
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var reviewAreas = prioritiser.Prioritise(hints);

        var reviewArea = Assert.Single(reviewAreas);

        Assert.Equal("Routing review", reviewArea.Area);
        Assert.Equal(ModernisationHintSeverity.Info, reviewArea.HighestSeverity);
        Assert.Equal(0, reviewArea.RiskCount);
        Assert.Equal(0, reviewArea.WarningCount);
        Assert.Equal(2, reviewArea.InfoCount);
    }

    [Fact]
    public void Prioritise_WhenStartupPipelineHintsExist_GroupsThemAsStartupAndRequestPipelineReview()
    {
        var hints = new[]
        {
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Warning,
                Area = "Legacy ASP.NET Filters",
                Finding = "FilterConfig.RegisterGlobalFilters registers ASP.NET MVC global filters",
                Reason = "Global filter registration should be reviewed."
            },
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Warning,
                Area = "Legacy ASP.NET Bundling",
                Finding = "BundleConfig.RegisterBundles registers ASP.NET MVC bundles",
                Reason = "Bundle registration calls may affect CSS and JavaScript delivery."
            },
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Info,
                Area = "Legacy ASP.NET Startup",
                Finding = "Global.asax.cs Application_Start contains ASP.NET application startup code",
                Reason = "Application_Start may contain route, filter, bundle, dependency injection, error handling, or lifecycle registration."
            }
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var reviewAreas = prioritiser.Prioritise(hints);

        var reviewArea = Assert.Single(reviewAreas);

        Assert.Equal("Startup and request pipeline review", reviewArea.Area);
        Assert.Equal(ModernisationHintSeverity.Warning, reviewArea.HighestSeverity);
        Assert.Equal(0, reviewArea.RiskCount);
        Assert.Equal(2, reviewArea.WarningCount);
        Assert.Equal(1, reviewArea.InfoCount);
    }

    [Fact]
    public void Prioritise_WhenLegacyAspNetHintsExist_GroupsThemAsLegacyAspNetMigration()
    {
        var hints = new[]
        {
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Risk,
                Area = "Legacy ASP.NET",
                Finding = "Default.aspx is a WebForms page",
                Reason = "WebForms pages indicate classic ASP.NET UI."
            },
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Warning,
                Area = "Legacy ASP.NET",
                Finding = "HomeController is an ASP.NET MVC controller",
                Reason = "ASP.NET MVC controllers may contain routing, action filters, model binding, authentication, or System.Web-specific behaviour."
            }
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var reviewAreas = prioritiser.Prioritise(hints);

        var reviewArea = Assert.Single(reviewAreas);

        Assert.Equal("Legacy ASP.NET migration", reviewArea.Area);
        Assert.Equal(ModernisationHintSeverity.Risk, reviewArea.HighestSeverity);
        Assert.Equal(1, reviewArea.RiskCount);
        Assert.Equal(1, reviewArea.WarningCount);
        Assert.Equal(0, reviewArea.InfoCount);
    }

    [Fact]
    public void Prioritise_WhenMultipleAreasExist_OrdersHighestRiskAreasFirst()
    {
        var hints = new[]
        {
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Info,
                Area = "Configuration",
                Finding = "Web.config contains 1 connection string(s)",
                Reason = "Connection strings identify external data dependencies."
            },
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Warning,
                Area = "Packages",
                Finding = "SampleLegacyApp.Data references EntityFramework",
                Reason = "Classic Entity Framework may require assessment."
            },
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Risk,
                Area = "Target Framework",
                Finding = "SampleLegacyApp.Web targets net48",
                Reason = ".NET Framework projects usually need extra assessment."
            }
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var reviewAreas = prioritiser.Prioritise(hints);

        Assert.Collection(
            reviewAreas,
            first => Assert.Equal("Target framework review", first.Area),
            second => Assert.Equal("Dependency review", second.Area),
            third => Assert.Equal("Configuration review", third.Area));
    }

    [Fact]
    public void Prioritise_WhenSystemServiceModelPackageHintExists_GroupsItAsWcfMigration()
    {
        var hints = new[]
        {
            new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Risk,
                Area = "Packages",
                Finding = "SampleLegacyApp.Web references System.ServiceModel.Http",
                Reason = "System.ServiceModel packages indicate WCF-related usage."
            }
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var reviewAreas = prioritiser.Prioritise(hints);

        var reviewArea = Assert.Single(reviewAreas);

        Assert.Equal("WCF migration", reviewArea.Area);
        Assert.Equal(ModernisationHintSeverity.Risk, reviewArea.HighestSeverity);
    }
}