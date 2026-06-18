using LegacyLens.Cli.Commands;
using LegacyLens.Cli.Commands.Runners;
using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;
using Xunit;

namespace LegacyLens.Cli.Tests.Commands.Runners;

public sealed class ScanArtifactRunnerTests
{
    public static TheoryData<IScanArtifactRunner, string> ArtifactRunners =>
        new()
        {
            { new UpgradeReadinessArtifactRunner(), ScanOptions.UpgradeReadinessArtifact },
            { new UpgradeBlockersArtifactRunner(), ScanOptions.UpgradeBlockersArtifact },
            { new ExternalDependenciesArtifactRunner(), ScanOptions.ExternalDependenciesArtifact },
            { new ConfigurationInventoryArtifactRunner(), ScanOptions.ConfigurationInventoryArtifact },
            { new DataAccessArtifactRunner(), ScanOptions.DataAccessArtifact },
            { new EdmxAnalysisArtifactRunner(), ScanOptions.EdmxAnalysisArtifact },
            { new ClassDependenciesArtifactRunner(), ScanOptions.ClassDependenciesArtifact },
            { new InterfaceInventoryArtifactRunner(), ScanOptions.InterfaceInventoryArtifact },
            { new SolutionTopologyArtifactRunner(), ScanOptions.SolutionTopologyArtifact }
        };

    [Theory]
    [MemberData(nameof(ArtifactRunners))]
    public void ArtifactName_ReturnsExpectedArtifactName(
        IScanArtifactRunner runner,
        string expectedArtifactName)
    {
        Assert.Equal(expectedArtifactName, runner.ArtifactName);
    }

    [Theory]
    [MemberData(nameof(ArtifactRunners))]
    public void ShouldRun_WhenMatchingArtifactIsSelected_ReturnsTrue(
        IScanArtifactRunner runner,
        string artifactName)
    {
        var context = CreateContext(artifactName);

        var shouldRun = runner.ShouldRun(context);

        Assert.True(shouldRun);
    }

    [Theory]
    [MemberData(nameof(ArtifactRunners))]
    public void ShouldRun_WhenAllArtifactsAreSelected_ReturnsTrue(
        IScanArtifactRunner runner,
        string _)
    {
        var context = CreateContextForAllArtifacts();

        var shouldRun = runner.ShouldRun(context);

        Assert.True(shouldRun);
    }

    [Theory]
    [MemberData(nameof(ArtifactRunners))]
    public void ShouldRun_WhenDifferentArtifactIsSelected_ReturnsFalse(
        IScanArtifactRunner runner,
        string artifactName)
    {
        var differentArtifact = artifactName.Equals(
            ScanOptions.UpgradeReadinessArtifact,
            StringComparison.OrdinalIgnoreCase)
            ? ScanOptions.UpgradeBlockersArtifact
            : ScanOptions.UpgradeReadinessArtifact;

        var context = CreateContext(differentArtifact);

        var shouldRun = runner.ShouldRun(context);

        Assert.False(shouldRun);
    }

    [Theory]
    [MemberData(nameof(ArtifactRunners))]
    public void ShouldRun_WhenContextIsNull_ThrowsArgumentNullException(
        IScanArtifactRunner runner,
        string _)
    {
        Assert.Throws<ArgumentNullException>(() => runner.ShouldRun(null!));
    }

    private static ScanContext CreateContext(string? artifact)
    {
        var scanPath = Directory.GetCurrentDirectory();

        return CreateContext(
            scanPath,
            new ScanOptions
            {
                Path = scanPath,
                Artifacts = artifact,
                SelectedArtifacts = artifact is null
                    ? []
                    : [artifact]
            });
    }

    private static ScanContext CreateContextForAllArtifacts()
    {
        var scanPath = Directory.GetCurrentDirectory();

        return CreateContext(
            scanPath,
            new ScanOptions
            {
                Path = scanPath,
                Artifacts = ScanOptions.AllArtifactsSelection,
                ShouldWriteAllArtifacts = true
            });
    }

    private static ScanContext CreateContext(
        string scanPath,
        ScanOptions options)
    {
        return new ScanContext(
            scanPath,
            Path.Combine(scanPath, "output", "discovery-report.md"),
            options,
            Array.Empty<DiscoveredSolution>(),
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>(),
            Array.Empty<ModernisationHint>(),
            Array.Empty<ModernisationReviewArea>(),
            ScanFileInventory.Empty);
    }
}
