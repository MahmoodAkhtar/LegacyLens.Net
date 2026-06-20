using LegacyLens.Cli.Commands;
using LegacyLens.Cli.Parsing;

namespace LegacyLens.Cli.Tests.Parsing;

public sealed class CliParserTests
{
    [Fact]
    public void Parse_WhenSingleArtifactIsSelected_ParsesSelectedArtifact()
    {
        var result = Parse("scan", ".", "--artifacts", "solution-topology");

        Assert.Equal(CliParseResultKind.Scan, result.Kind);
        Assert.False(result.Options!.ShouldWriteAllArtifacts);
        Assert.Equal([ScanOptions.SolutionTopologyArtifact], result.Options.SelectedArtifacts);
        Assert.True(result.Options.ShouldWriteArtifact(ScanOptions.SolutionTopologyArtifact));
    }

    [Fact]
    public void Parse_WhenCommaSeparatedArtifactsAreSelected_ParsesSelectedArtifacts()
    {
        var result = Parse("scan", ".", "--artifacts", "solution-topology,class-dependencies,data-access");

        Assert.Equal(CliParseResultKind.Scan, result.Kind);
        Assert.Equal(
            [ScanOptions.SolutionTopologyArtifact, ScanOptions.ClassDependenciesArtifact, ScanOptions.DataAccessArtifact],
            result.Options!.SelectedArtifacts);
    }

    [Fact]
    public void Parse_WhenCommaSeparatedArtifactsContainSpaces_ParsesSelectedArtifacts()
    {
        var result = Parse("scan", ".", "--artifacts", "solution-topology,", "class-dependencies,", "data-access");

        Assert.Equal(CliParseResultKind.Scan, result.Kind);
        Assert.Equal(
            [ScanOptions.SolutionTopologyArtifact, ScanOptions.ClassDependenciesArtifact, ScanOptions.DataAccessArtifact],
            result.Options!.SelectedArtifacts);
    }

    [Fact]
    public void Parse_WhenDuplicateArtifactsAreSelected_DeduplicatesArtifacts()
    {
        var result = Parse("scan", ".", "--artifacts", "solution-topology,SOLUTION-TOPOLOGY,class-dependencies");

        Assert.Equal(CliParseResultKind.Scan, result.Kind);
        Assert.Equal(
            [ScanOptions.SolutionTopologyArtifact, ScanOptions.ClassDependenciesArtifact],
            result.Options!.SelectedArtifacts);
    }

    [Fact]
    public void Parse_WhenAllArtifactsAreSelected_SetsAllArtifactsFlag()
    {
        var result = Parse("scan", ".", "--artifacts", "all");

        Assert.Equal(CliParseResultKind.Scan, result.Kind);
        Assert.True(result.Options!.ShouldWriteAllArtifacts);
        Assert.Empty(result.Options.SelectedArtifacts);
        Assert.True(result.Options.ShouldWriteArtifact(ScanOptions.DataAccessArtifact));
        Assert.True(result.Options.ShouldWriteArtifact(ScanOptions.UpgradeReadinessArtifact));
        Assert.True(result.Options.ShouldWriteArtifact(ScanOptions.InterfaceInventoryArtifact));
    }

    [Fact]
    public void Parse_WhenAllIsCombinedWithOtherArtifacts_ReturnsError()
    {
        var result = Parse("scan", ".", "--artifacts", "all,data-access");

        Assert.Equal(CliParseResultKind.Error, result.Kind);
        Assert.Contains("Use --artifacts all by itself", result.Message);
    }

    [Fact]
    public void Parse_WhenArtifactNameIsUnknown_ReturnsSupportedValuesError()
    {
        var result = Parse("scan", ".", "--artifacts", "unknown-artifact");

        Assert.Equal(CliParseResultKind.Error, result.Kind);
        Assert.Contains("Unknown artifact name(s): unknown-artifact", result.Message);
        Assert.Contains(ScanOptions.UpgradeReadinessArtifact, result.Message);
        Assert.Contains(ScanOptions.InterfaceInventoryArtifact, result.Message);
        Assert.Contains(ScanOptions.AllArtifactsSelection, result.Message);
    }

    [Theory]
    [InlineData("upgrade-readiness")]
    [InlineData("upgrade-blockers")]
    [InlineData("solution-topology,upgrade-readiness")]
    [InlineData("all")]
    public void Parse_WhenUpgradeTargetIsUsedWithUpgradeRelatedArtifact_ReturnsScan(string artifacts)
    {
        var result = Parse("scan", ".", "--artifacts", artifacts, "--upgrade-target", "net8.0");

        Assert.Equal(CliParseResultKind.Scan, result.Kind);
        Assert.Equal("net8.0", result.Options!.UpgradeTarget);
    }

    [Fact]
    public void Parse_WhenUpgradeTargetIsUsedWithoutUpgradeRelatedArtifact_ReturnsError()
    {
        var result = Parse("scan", ".", "--artifacts", "data-access", "--upgrade-target", "net8.0");

        Assert.Equal(CliParseResultKind.Error, result.Kind);
        Assert.Contains("Use --upgrade-target only as upgrade report wording context when --artifacts includes upgrade-readiness, upgrade-blockers, or all", result.Message);
    }

    [Fact]
    public void Parse_WhenUpgradeTargetIsUsedWithoutArtifacts_ReturnsError()
    {
        var result = Parse("scan", ".", "--upgrade-target", "net8.0");

        Assert.Equal(CliParseResultKind.Error, result.Kind);
        Assert.Contains("Use --upgrade-target only as upgrade report wording context when --artifacts includes upgrade-readiness, upgrade-blockers, or all", result.Message);
    }


    [Fact]
    public void Parse_WhenScopedClassDependencyArtifactIsSelectedWithType_ReturnsScan()
    {
        var result = Parse(
            "scan",
            ".",
            "--artifacts",
            "class-dependency-scope",
            "--class-dependency-type",
            "SampleLegacyApp.Services.CustomerService");

        Assert.Equal(CliParseResultKind.Scan, result.Kind);
        Assert.Equal([ScanOptions.ClassDependencyScopeArtifact], result.Options!.SelectedArtifacts);
        Assert.Equal("SampleLegacyApp.Services.CustomerService", result.Options.ClassDependencyType);
        Assert.True(result.Options.ShouldWriteScopedClassDependencyArtifact);
    }

    [Fact]
    public void Parse_WhenScopedClassDependencyArtifactIsSelectedWithoutType_ReturnsError()
    {
        var result = Parse("scan", ".", "--artifacts", "class-dependency-scope");

        Assert.Equal(CliParseResultKind.Error, result.Kind);
        Assert.Equal(
            "The class-dependency-scope artifact requires --class-dependency-type <fully-qualified-type-name>.",
            result.Message);
    }

    [Fact]
    public void Parse_WhenClassDependencyTypeIsUsedWithUnrelatedArtifact_ReturnsError()
    {
        var result = Parse(
            "scan",
            ".",
            "--artifacts",
            "data-access",
            "--class-dependency-type",
            "SampleLegacyApp.Services.CustomerService");

        Assert.Equal(CliParseResultKind.Error, result.Kind);
        Assert.Equal(
            "Use --class-dependency-type only when --artifacts includes class-dependency-scope or all.",
            result.Message);
    }

    [Fact]
    public void Parse_WhenClassDependencyTypeIsUsedWithoutArtifacts_ReturnsError()
    {
        var result = Parse(
            "scan",
            ".",
            "--class-dependency-type",
            "SampleLegacyApp.Services.CustomerService");

        Assert.Equal(CliParseResultKind.Error, result.Kind);
        Assert.Equal(
            "Use --class-dependency-type only when --artifacts includes class-dependency-scope or all.",
            result.Message);
    }

    [Fact]
    public void Parse_WhenAllArtifactsAreSelectedWithClassDependencyType_ReturnsScan()
    {
        var result = Parse(
            "scan",
            ".",
            "--artifacts",
            "all",
            "--class-dependency-type",
            "SampleLegacyApp.Services.CustomerService");

        Assert.Equal(CliParseResultKind.Scan, result.Kind);
        Assert.True(result.Options!.ShouldWriteAllArtifacts);
        Assert.Equal("SampleLegacyApp.Services.CustomerService", result.Options.ClassDependencyType);
        Assert.True(result.Options.ShouldWriteScopedClassDependencyArtifact);
    }

    [Fact]
    public void Parse_WhenAllArtifactsAreSelectedWithoutClassDependencyType_DoesNotRequireScopedType()
    {
        var result = Parse("scan", ".", "--artifacts", "all");

        Assert.Equal(CliParseResultKind.Scan, result.Kind);
        Assert.True(result.Options!.ShouldWriteAllArtifacts);
        Assert.Null(result.Options.ClassDependencyType);
        Assert.False(result.Options.ShouldWriteScopedClassDependencyArtifact);
    }

    private static CliParseResult Parse(params string[] args)
    {
        var parser = new CliParser();
        return parser.Parse(args);
    }
}
