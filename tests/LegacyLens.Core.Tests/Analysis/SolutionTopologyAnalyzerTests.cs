using LegacyLens.Core.Analysis;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class SolutionTopologyAnalyzerTests : IDisposable
{
    private readonly string _tempDirectory;

    public SolutionTopologyAnalyzerTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"LegacyLensSolutionTopologyTests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Analyze_CreatesSummaryMembershipDependenciesHotspotsAndReadingOrder()
    {
        var web = CreateProject(
            "Sample.Web",
            "net48",
            ["..\\Sample.Services\\Sample.Services.csproj"]);

        var services = CreateProject(
            "Sample.Services",
            "net48",
            [
                "..\\Sample.Contracts\\Sample.Contracts.csproj",
                "..\\Sample.Data\\Sample.Data.csproj",
                "..\\Sample.Logging\\Sample.Logging.csproj"
            ]);

        var contracts = CreateProject("Sample.Contracts", "net48", []);
        var data = CreateProject("Sample.Data", "net48", []);
        var logging = CreateProject("Sample.Logging", "net48", []);

        var solution = new DiscoveredSolution
        {
            Name = "Sample",
            SolutionFilePath = Path.Combine(_tempDirectory, "Sample.sln"),
            ProjectFilePaths =
            [
                web.ProjectFilePath,
                services.ProjectFilePath,
                contracts.ProjectFilePath,
                data.ProjectFilePath,
                logging.ProjectFilePath
            ]
        };

        var webProjectDirectory = Path.GetDirectoryName(web.ProjectFilePath)!;

        var inventory = new ScanFileInventory(
            [
                new ScanFile(
                    web.Name,
                    web.ProjectFilePath,
                    webProjectDirectory,
                    Path.Combine(webProjectDirectory, "Program.cs"),
                    "Program.cs",
                    ".cs",
                    "var builder = WebApplication.CreateBuilder(args);")
            ],
            [],
            [],
            [],
            []);

        var report = new SolutionTopologyAnalyzer().Analyze(
            [solution],
            [web, services, contracts, data, logging],
            [],
            [],
            [],
            [],
            [],
            [],
            inventory);

        Assert.Equal(1, report.Summary.SolutionCount);
        Assert.Equal(5, report.Summary.ProjectCount);
        Assert.Equal(5, report.Summary.SolutionProjectMembershipCount);
        Assert.Equal(4, report.Summary.ProjectReferenceCount);

        Assert.Contains(
            report.Projects,
            project => project.Name == "Sample.Web" && project.IsPossibleEntryPoint);

        Assert.Contains(
            report.Dependencies,
            dependency => dependency.SourceProject == "Sample.Services" &&
                          dependency.TargetProject == "Sample.Data");

        Assert.Contains(
            report.Hotspots,
            hotspot => hotspot.ProjectName == "Sample.Services");

        Assert.Equal("Sample.Web", report.SuggestedReadingOrder.First().ProjectName);
    }

    [Fact]
    public void Analyze_DetectsSharedProjectsAcrossSolutions()
    {
        var shared = CreateProject("Sample.Shared", "net48", []);

        var firstSolution = new DiscoveredSolution
        {
            Name = "AppOne",
            SolutionFilePath = Path.Combine(_tempDirectory, "AppOne.sln"),
            ProjectFilePaths = [shared.ProjectFilePath]
        };

        var secondSolution = new DiscoveredSolution
        {
            Name = "AppTwo",
            SolutionFilePath = Path.Combine(_tempDirectory, "AppTwo.sln"),
            ProjectFilePaths = [shared.ProjectFilePath]
        };

        var report = new SolutionTopologyAnalyzer().Analyze(
            [firstSolution, secondSolution],
            [shared],
            [],
            [],
            [],
            [],
            [],
            [],
            ScanFileInventory.Empty);

        var sharedProject = Assert.Single(report.SharedProjects);
        Assert.Equal("Sample.Shared", sharedProject.ProjectName);
        Assert.Equal(2, sharedProject.SolutionCount);
    }

    [Fact]
    public void Analyze_DetectsPossibleCircularProjectReferences()
    {
        var projectA = CreateProject(
            "Project.A",
            "net48",
            ["..\\Project.B\\Project.B.csproj"]);

        var projectB = CreateProject(
            "Project.B",
            "net48",
            ["..\\Project.A\\Project.A.csproj"]);

        var report = new SolutionTopologyAnalyzer().Analyze(
            [],
            [projectA, projectB],
            [],
            [],
            [],
            [],
            [],
            [],
            ScanFileInventory.Empty);

        Assert.NotEmpty(report.PossibleCircularDependencies);
    }

    [Fact]
    public void Analyze_ClassifiesTestProjectFromPackageEvidence()
    {
        var tests = CreateProject("Sample.Tests", "net8.0", []);

        tests.PackageReferenceDetails.Add(new DiscoveredPackageReference
        {
            Name = "xunit",
            Version = "2.9.0",
            SourceFormat = "PackageReference",
            SourcePath = tests.ProjectFilePath
        });

        var report = new SolutionTopologyAnalyzer().Analyze(
            [],
            [tests],
            [],
            [],
            [],
            [],
            [],
            [],
            ScanFileInventory.Empty);

        var project = Assert.Single(report.Projects);

        Assert.True(project.IsPossibleTestProject);
        Assert.Equal(ProjectTopologyRole.Test, project.Role.Role);
    }

    private DiscoveredProject CreateProject(
        string name,
        string targetFramework,
        IEnumerable<string> references)
    {
        var directory = Path.Combine(_tempDirectory, name);
        Directory.CreateDirectory(directory);

        var projectFilePath = Path.Combine(directory, $"{name}.csproj");

        File.WriteAllText(projectFilePath, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{targetFramework}</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        return new DiscoveredProject
        {
            Name = name,
            ProjectFilePath = projectFilePath,
            TargetFramework = targetFramework,
            ProjectReferences = references.ToList()
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}