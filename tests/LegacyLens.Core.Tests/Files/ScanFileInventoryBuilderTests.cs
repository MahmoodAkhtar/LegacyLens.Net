using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;

namespace LegacyLens.Core.Tests.Files;

public sealed class ScanFileInventoryBuilderTests : IDisposable
{
    private readonly string _root;

    public ScanFileInventoryBuilderTests()
    {
        _root = CreateTemporaryDirectory();
    }

    [Fact]
    public void Build_WhenProjectsIsNull_ThrowsArgumentNullException()
    {
        var builder = new ScanFileInventoryBuilder();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.Build(null!));

        Assert.Equal("projects", exception.ParamName);
    }

    [Fact]
    public void Build_WhenProjectDirectoryDoesNotExist_ReturnsEmptyInventory()
    {
        var project = new DiscoveredProject
        {
            Name = "Missing.Project",
            ProjectFilePath = Path.Combine(_root, "Missing", "Missing.Project.csproj")
        };

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        Assert.Empty(inventory.CSharpFiles);
        Assert.Empty(inventory.EdmxFiles);
        Assert.Empty(inventory.DbmlFiles);
        Assert.Empty(inventory.T4Files);
        Assert.Empty(inventory.MigrationDirectories);
    }

    [Fact]
    public void Build_DiscoversCSharpFiles()
    {
        var project = CreateProject("Sample.Data");
        var sourcePath = WriteFile(project, "CustomerRepository.cs", "public sealed class CustomerRepository { }");

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        var file = Assert.Single(inventory.CSharpFiles);

        Assert.Equal(sourcePath, file.FullPath);
        Assert.Equal(".cs", file.Extension);
    }

    [Fact]
    public void Build_DiscoversEdmxFiles()
    {
        var project = CreateProject("Sample.Data");
        var edmxPath = WriteFile(project, "LegacyModel.edmx", "<edmx />");

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        var file = Assert.Single(inventory.EdmxFiles);

        Assert.Equal(edmxPath, file.FullPath);
        Assert.Equal(".edmx", file.Extension);
    }

    [Fact]
    public void Build_DiscoversDbmlFiles()
    {
        var project = CreateProject("Sample.Data");
        var dbmlPath = WriteFile(project, "LegacyModel.dbml", "<dbml />");

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        var file = Assert.Single(inventory.DbmlFiles);

        Assert.Equal(dbmlPath, file.FullPath);
        Assert.Equal(".dbml", file.Extension);
    }

    [Fact]
    public void Build_DiscoversT4Files()
    {
        var project = CreateProject("Sample.Data");
        var t4Path = WriteFile(project, "LegacyModel.Context.tt", "EntityFramework ObjectContext");

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        var file = Assert.Single(inventory.T4Files);

        Assert.Equal(t4Path, file.FullPath);
        Assert.Equal(".tt", file.Extension);
    }

    [Fact]
    public void Build_DiscoversMigrationDirectories()
    {
        var project = CreateProject("Sample.Data");
        var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath)!;
        var migrationsDirectory = Path.Combine(projectDirectory, "Migrations");

        Directory.CreateDirectory(migrationsDirectory);

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        Assert.Contains(
            inventory.MigrationDirectories,
            path => path.Equals(migrationsDirectory, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_ExcludesBinAndObjFolders()
    {
        var project = CreateProject("Sample.Data");

        WriteFile(project, "CustomerRepository.cs", "public sealed class CustomerRepository { }");
        WriteFile(project, Path.Combine("bin", "Debug", "GeneratedFromBin.cs"), "public sealed class GeneratedFromBin { }");
        WriteFile(project, Path.Combine("obj", "Debug", "GeneratedFromObj.cs"), "public sealed class GeneratedFromObj { }");

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        Assert.Single(inventory.CSharpFiles);
        Assert.DoesNotContain(inventory.CSharpFiles, file => file.FullPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(inventory.CSharpFiles, file => file.FullPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_ExcludesGeneratedOutputFolders()
    {
        var project = CreateProject("Sample.Data");

        WriteFile(project, "CustomerRepository.cs", "public sealed class CustomerRepository { }");
        WriteFile(project, Path.Combine("output", "GeneratedOutput.cs"), "public sealed class GeneratedOutput { }");
        WriteFile(project, Path.Combine("reports", "GeneratedReport.cs"), "public sealed class GeneratedReport { }");
        WriteFile(project, Path.Combine("artifacts", "GeneratedArtifact.cs"), "public sealed class GeneratedArtifact { }");

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        var file = Assert.Single(inventory.CSharpFiles);

        Assert.Equal("CustomerRepository.cs", file.RelativePath);
    }

    [Fact]
    public void Build_SetsProjectNameAndProjectFilePath()
    {
        var project = CreateProject("Sample.Data");
        WriteFile(project, "CustomerRepository.cs", "public sealed class CustomerRepository { }");

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        var file = Assert.Single(inventory.CSharpFiles);

        Assert.Equal(project.Name, file.ProjectName);
        Assert.Equal(project.ProjectFilePath, file.ProjectFilePath);
    }

    [Fact]
    public void Build_SetsRelativePath()
    {
        var project = CreateProject("Sample.Data");
        WriteFile(project, Path.Combine("Models", "Customer.cs"), "public sealed class Customer { }");

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        var file = Assert.Single(inventory.CSharpFiles);

        Assert.Equal(Path.Combine("Models", "Customer.cs"), file.RelativePath);
    }

    [Fact]
    public void Build_ReadsContentForTextFiles()
    {
        var project = CreateProject("Sample.Data");

        WriteFile(
            project,
            "CustomerRepository.cs",
            "public sealed class CustomerRepository { }");

        var builder = new ScanFileInventoryBuilder();

        var inventory = builder.Build(new[] { project });

        var file = Assert.Single(inventory.CSharpFiles);

        Assert.Contains("CustomerRepository", file.Content);
    }

    public void Dispose()
    {
        DeleteDirectory(_root);
    }

    private DiscoveredProject CreateProject(string name)
    {
        var projectDirectory = Path.Combine(_root, name);
        Directory.CreateDirectory(projectDirectory);

        var projectFilePath = Path.Combine(projectDirectory, $"{name}.csproj");
        File.WriteAllText(projectFilePath, "<Project />");

        return new DiscoveredProject
        {
            Name = name,
            ProjectFilePath = projectFilePath,
            TargetFramework = "net48"
        };
    }

    private static string WriteFile(
        DiscoveredProject project,
        string relativePath,
        string content)
    {
        var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath)!;
        var fullPath = Path.Combine(projectDirectory, relativePath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);

        return fullPath;
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "LegacyLensTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(path);

        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best effort cleanup for tests.
        }
    }
}