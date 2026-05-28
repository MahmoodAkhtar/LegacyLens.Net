using LegacyLens.Core.Discovery;

namespace LegacyLens.Core.Tests.Discovery;

public sealed class SolutionDiscoveryServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public SolutionDiscoveryServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"LegacyLensTests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void DiscoverSolutions_WhenRootPathIsEmpty_ThrowsArgumentException()
    {
        var service = new SolutionDiscoveryService();

        var exception = Assert.Throws<ArgumentException>(() =>
            service.DiscoverSolutions(""));

        Assert.Equal("rootPath", exception.ParamName);
    }

    [Fact]
    public void DiscoverSolutions_WhenRootPathDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        var service = new SolutionDiscoveryService();

        var missingPath = Path.Combine(_tempDirectory, "Missing");

        Assert.Throws<DirectoryNotFoundException>(() =>
            service.DiscoverSolutions(missingPath));
    }

    [Fact]
    public void DiscoverSolutions_WhenNoSolutionFilesExist_ReturnsEmptyList()
    {
        var service = new SolutionDiscoveryService();

        var solutions = service.DiscoverSolutions(_tempDirectory);

        Assert.Empty(solutions);
    }

    [Fact]
    public void DiscoverSolutions_WhenSolutionFileExists_ReturnsDiscoveredSolution()
    {
        var solutionPath = Path.Combine(_tempDirectory, "SampleLegacyApp.sln");

        File.WriteAllText(solutionPath, """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SampleLegacyApp.Web", "SampleLegacyApp.Web\SampleLegacyApp.Web.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            """);

        var service = new SolutionDiscoveryService();

        var solutions = service.DiscoverSolutions(_tempDirectory);

        var solution = Assert.Single(solutions);

        Assert.Equal("SampleLegacyApp", solution.Name);
        Assert.Equal(solutionPath, solution.SolutionFilePath);
    }

    [Fact]
    public void DiscoverSolutions_WhenSolutionContainsCSharpProjects_ReturnsProjectFilePaths()
    {
        var solutionPath = Path.Combine(_tempDirectory, "SampleLegacyApp.sln");

        File.WriteAllText(solutionPath, """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SampleLegacyApp.Contracts", "SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SampleLegacyApp.Services", "SampleLegacyApp.Services\SampleLegacyApp.Services.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            """);

        var service = new SolutionDiscoveryService();

        var solutions = service.DiscoverSolutions(_tempDirectory);

        var solution = Assert.Single(solutions);

        Assert.Equal(2, solution.ProjectFilePaths.Count);

        Assert.Contains(
            Path.GetFullPath(Path.Combine(_tempDirectory, "SampleLegacyApp.Contracts", "SampleLegacyApp.Contracts.csproj")),
            solution.ProjectFilePaths);

        Assert.Contains(
            Path.GetFullPath(Path.Combine(_tempDirectory, "SampleLegacyApp.Services", "SampleLegacyApp.Services.csproj")),
            solution.ProjectFilePaths);
    }

    [Fact]
    public void DiscoverSolutions_WhenSolutionContainsSolutionFolders_IgnoresSolutionFolders()
    {
        var solutionPath = Path.Combine(_tempDirectory, "SampleLegacyApp.sln");

        File.WriteAllText(solutionPath, """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{66A26720-8FB5-11D2-AA7E-00C04F688DDE}") = "src", "src", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SampleLegacyApp.Web", "src\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            """);

        var service = new SolutionDiscoveryService();

        var solutions = service.DiscoverSolutions(_tempDirectory);

        var solution = Assert.Single(solutions);
        var projectPath = Assert.Single(solution.ProjectFilePaths);

        Assert.Equal(
            Path.GetFullPath(Path.Combine(_tempDirectory, "src", "SampleLegacyApp.Web", "SampleLegacyApp.Web.csproj")),
            projectPath);
    }

    [Fact]
    public void DiscoverSolutions_WhenSolutionContainsNonCSharpProjects_IgnoresNonCSharpProjects()
    {
        var solutionPath = Path.Combine(_tempDirectory, "SampleLegacyApp.sln");

        File.WriteAllText(solutionPath, """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SampleLegacyApp.Web", "SampleLegacyApp.Web\SampleLegacyApp.Web.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{9092AA53-FB77-4645-B42D-1CCCA6BD08BD}") = "SampleLegacyApp.Database", "SampleLegacyApp.Database\SampleLegacyApp.Database.sqlproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            """);

        var service = new SolutionDiscoveryService();

        var solutions = service.DiscoverSolutions(_tempDirectory);

        var solution = Assert.Single(solutions);
        var projectPath = Assert.Single(solution.ProjectFilePaths);

        Assert.Equal(
            Path.GetFullPath(Path.Combine(_tempDirectory, "SampleLegacyApp.Web", "SampleLegacyApp.Web.csproj")),
            projectPath);
    }

    [Fact]
    public void DiscoverSolutions_WhenSolutionContainsDuplicateProjectEntries_RemovesDuplicates()
    {
        var solutionPath = Path.Combine(_tempDirectory, "SampleLegacyApp.sln");

        File.WriteAllText(solutionPath, """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SampleLegacyApp.Web", "SampleLegacyApp.Web\SampleLegacyApp.Web.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "SampleLegacyApp.Web", "SampleLegacyApp.Web\SampleLegacyApp.Web.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            """);

        var service = new SolutionDiscoveryService();

        var solutions = service.DiscoverSolutions(_tempDirectory);

        var solution = Assert.Single(solutions);

        Assert.Single(solution.ProjectFilePaths);
    }

    [Fact]
    public void DiscoverSolutions_WhenMultipleSolutionFilesExist_ReturnsSolutionsOrderedByName()
    {
        var zetaSolutionPath = Path.Combine(_tempDirectory, "Zeta.sln");
        var alphaSolutionPath = Path.Combine(_tempDirectory, "Alpha.sln");

        File.WriteAllText(zetaSolutionPath, """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            """);

        File.WriteAllText(alphaSolutionPath, """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            """);

        var service = new SolutionDiscoveryService();

        var solutions = service.DiscoverSolutions(_tempDirectory);

        Assert.Equal(2, solutions.Count);
        Assert.Equal("Alpha", solutions[0].Name);
        Assert.Equal("Zeta", solutions[1].Name);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}