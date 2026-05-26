using LegacyLens.Core.Discovery;

namespace LegacyLens.Core.Tests;

public sealed class ProjectDiscoveryServiceTests
{
    [Fact]
    public void DiscoverProjects_ShouldDiscoverPackagesFromPackagesConfig()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var projectDirectory = Path.Combine(rootPath, "SampleLegacyApp.Data");
            Directory.CreateDirectory(projectDirectory);

            var projectFilePath = Path.Combine(projectDirectory, "SampleLegacyApp.Data.csproj");

            File.WriteAllText(projectFilePath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net48</TargetFramework>
                  </PropertyGroup>
                </Project>
                """);

            var packagesConfigPath = Path.Combine(projectDirectory, "packages.config");

            File.WriteAllText(packagesConfigPath, """
                <?xml version="1.0" encoding="utf-8"?>
                <packages>
                  <package id="EntityFramework" version="6.4.4" targetFramework="net48" />
                  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net48" />
                </packages>
                """);

            var service = new ProjectDiscoveryService();

            var projects = service.DiscoverProjects(rootPath);

            var project = Assert.Single(projects);

            Assert.Equal("SampleLegacyApp.Data", project.Name);
            Assert.Contains("EntityFramework", project.PackageReferences);
            Assert.Contains("Newtonsoft.Json", project.PackageReferences);
        }
        finally
        {
            DeleteTemporaryDirectory(rootPath);
        }
    }

    [Fact]
    public void DiscoverProjects_ShouldMergePackageReferenceAndPackagesConfigPackages()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var projectDirectory = Path.Combine(rootPath, "SampleLegacyApp.Data");
            Directory.CreateDirectory(projectDirectory);

            var projectFilePath = Path.Combine(projectDirectory, "SampleLegacyApp.Data.csproj");

            File.WriteAllText(projectFilePath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net48</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Dapper" Version="2.1.66" />
                  </ItemGroup>
                </Project>
                """);

            var packagesConfigPath = Path.Combine(projectDirectory, "packages.config");

            File.WriteAllText(packagesConfigPath, """
                <?xml version="1.0" encoding="utf-8"?>
                <packages>
                  <package id="EntityFramework" version="6.4.4" targetFramework="net48" />
                </packages>
                """);

            var service = new ProjectDiscoveryService();

            var projects = service.DiscoverProjects(rootPath);

            var project = Assert.Single(projects);

            Assert.Contains("Dapper", project.PackageReferences);
            Assert.Contains("EntityFramework", project.PackageReferences);
        }
        finally
        {
            DeleteTemporaryDirectory(rootPath);
        }
    }

    [Fact]
    public void DiscoverProjects_ShouldNotDuplicatePackagesFoundInProjectFileAndPackagesConfig()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var projectDirectory = Path.Combine(rootPath, "SampleLegacyApp.Web");
            Directory.CreateDirectory(projectDirectory);

            var projectFilePath = Path.Combine(projectDirectory, "SampleLegacyApp.Web.csproj");

            File.WriteAllText(projectFilePath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net48</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                  </ItemGroup>
                </Project>
                """);

            var packagesConfigPath = Path.Combine(projectDirectory, "packages.config");

            File.WriteAllText(packagesConfigPath, """
                <?xml version="1.0" encoding="utf-8"?>
                <packages>
                  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net48" />
                </packages>
                """);

            var service = new ProjectDiscoveryService();

            var projects = service.DiscoverProjects(rootPath);

            var project = Assert.Single(projects);

            Assert.Single(project.PackageReferences, x => x == "Newtonsoft.Json");
        }
        finally
        {
            DeleteTemporaryDirectory(rootPath);
        }
    }

    [Fact]
    public void DiscoverProjects_ShouldIgnoreInvalidPackagesConfig()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var projectDirectory = Path.Combine(rootPath, "SampleLegacyApp.Data");
            Directory.CreateDirectory(projectDirectory);

            var projectFilePath = Path.Combine(projectDirectory, "SampleLegacyApp.Data.csproj");

            File.WriteAllText(projectFilePath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net48</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Dapper" Version="2.1.66" />
                  </ItemGroup>
                </Project>
                """);

            var packagesConfigPath = Path.Combine(projectDirectory, "packages.config");

            File.WriteAllText(packagesConfigPath, """
                <packages>
                  <package id="EntityFramework"
                """);

            var service = new ProjectDiscoveryService();

            var projects = service.DiscoverProjects(rootPath);

            var project = Assert.Single(projects);

            Assert.Contains("Dapper", project.PackageReferences);
            Assert.DoesNotContain("EntityFramework", project.PackageReferences);
        }
        finally
        {
            DeleteTemporaryDirectory(rootPath);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTemporaryDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}