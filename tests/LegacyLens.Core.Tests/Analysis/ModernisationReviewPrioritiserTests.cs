using FluentAssertions;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationReviewPrioritiserTests
{
    [Fact]
    public void Prioritise_ReturnsEmptyList_WhenHintsAreEmpty()
    {
        var prioritiser = new ModernisationReviewPrioritiser();

        var result = prioritiser.Prioritise(Array.Empty<ModernisationHint>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Prioritise_GroupsWcfHintsIntoWcfMigrationReviewArea()
    {
        var hints = new[]
        {
            CreateHint(
                ModernisationHintSeverity.Risk,
                "WCF",
                "3 WCF endpoint(s) discovered"),

            CreateHint(
                ModernisationHintSeverity.Warning,
                "WCF Binding",
                "basicHttpBinding endpoint discovered")
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var result = prioritiser.Prioritise(hints);

        result.Should().ContainSingle();

        result[0].Area.Should().Be("WCF migration");
        result[0].HighestSeverity.Should().Be(ModernisationHintSeverity.Risk);
        result[0].RiskCount.Should().Be(1);
        result[0].WarningCount.Should().Be(1);
        result[0].InfoCount.Should().Be(0);
    }

    [Fact]
    public void Prioritise_PrioritisesWcfAndLegacyAspNetAheadOfGenericTargetFramework_WhenSeverityIsEqual()
    {
        var hints = new[]
        {
            CreateHint(
                ModernisationHintSeverity.Risk,
                "Target Framework",
                "SampleLegacyApp.Contracts targets net48"),

            CreateHint(
                ModernisationHintSeverity.Risk,
                "Target Framework",
                "SampleLegacyApp.Data targets net48"),

            CreateHint(
                ModernisationHintSeverity.Risk,
                "Target Framework",
                "SampleLegacyApp.Services targets net48"),

            CreateHint(
                ModernisationHintSeverity.Risk,
                "Target Framework",
                "SampleLegacyApp.Web targets net48"),

            CreateHint(
                ModernisationHintSeverity.Risk,
                "WCF",
                "3 WCF endpoint(s) discovered"),

            CreateHint(
                ModernisationHintSeverity.Risk,
                "Legacy ASP.NET",
                "Default.aspx is a WebForms page")
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var result = prioritiser.Prioritise(hints);

        result.Select(x => x.Area).Should().Equal(
            "WCF migration",
            "Legacy ASP.NET migration",
            "Target framework review");
    }

    [Fact]
    public void Prioritise_KeepsWarningOnlyStartupAndPipelineBelowRiskAreas()
    {
        var hints = new[]
        {
            CreateHint(
                ModernisationHintSeverity.Warning,
                "Legacy ASP.NET Request Pipeline",
                "LegacyAuthModule registers an ASP.NET HTTP module"),

            CreateHint(
                ModernisationHintSeverity.Warning,
                "Legacy ASP.NET Web API Pipeline",
                "config.EnableCors enables ASP.NET Web API CORS configuration"),

            CreateHint(
                ModernisationHintSeverity.Risk,
                "Target Framework",
                "SampleLegacyApp.Web targets net48")
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var result = prioritiser.Prioritise(hints);

        result.Select(x => x.Area).Should().Equal(
            "Target framework review",
            "Startup and request pipeline review");
    }

    [Fact]
    public void Prioritise_UsesReviewAreaPriorityBeforeHintCounts_WhenSeverityIsEqual()
    {
        var hints = new[]
        {
            CreateHint(
                ModernisationHintSeverity.Warning,
                "Configuration",
                "Web.config contains 1 custom configuration section(s)"),

            CreateHint(
                ModernisationHintSeverity.Warning,
                "Packages",
                "SampleLegacyApp.Data references EntityFramework"),

            CreateHint(
                ModernisationHintSeverity.Warning,
                "Packages",
                "SampleLegacyApp.Web references SomeOtherPackage")
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var result = prioritiser.Prioritise(hints);

        result.Select(x => x.Area).Should().Equal(
            "Configuration review",
            "Dependency review");
    }

    [Fact]
    public void Prioritise_MapsSystemServiceModelPackageHintsToWcfMigration()
    {
        var hints = new[]
        {
            CreateHint(
                ModernisationHintSeverity.Risk,
                "Packages",
                "SampleLegacyApp.Web references System.ServiceModel.Http")
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var result = prioritiser.Prioritise(hints);

        result.Should().ContainSingle();

        result[0].Area.Should().Be("WCF migration");
        result[0].HighestSeverity.Should().Be(ModernisationHintSeverity.Risk);
        result[0].RiskCount.Should().Be(1);
    }

    [Fact]
    public void Prioritise_UsesHintCountsAsTieBreaker_WhenSeverityAndReviewAreaPriorityAreEqual()
    {
        var hints = new[]
        {
            CreateHint(
                ModernisationHintSeverity.Warning,
                "Other Area",
                "First unrelated warning"),

            CreateHint(
                ModernisationHintSeverity.Warning,
                "Another Other Area",
                "Second unrelated warning"),

            CreateHint(
                ModernisationHintSeverity.Warning,
                "Another Other Area",
                "Third unrelated warning")
        };

        var prioritiser = new ModernisationReviewPrioritiser();

        var result = prioritiser.Prioritise(hints);

        result.Should().ContainSingle();

        result[0].Area.Should().Be("Other review");
        result[0].WarningCount.Should().Be(3);
    }

    private static ModernisationHint CreateHint(
        ModernisationHintSeverity severity,
        string area,
        string finding)
    {
        return new ModernisationHint
        {
            Severity = severity,
            Area = area,
            Finding = finding,
            Reason = "Test reason"
        };
    }
}