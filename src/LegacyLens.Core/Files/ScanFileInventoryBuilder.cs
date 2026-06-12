using LegacyLens.Core.Discovery;

namespace LegacyLens.Core.Files;

public sealed class ScanFileInventoryBuilder
{
    public ScanFileInventory Build(IReadOnlyCollection<DiscoveredProject> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var csharpFiles = new List<ScanFile>();
        var edmxFiles = new List<ScanFile>();
        var dbmlFiles = new List<ScanFile>();
        var t4Files = new List<ScanFile>();
        var migrationDirectories = new List<string>();

        foreach (var project in projects)
        {
            var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);

            if (string.IsNullOrWhiteSpace(projectDirectory) ||
                !Directory.Exists(projectDirectory) ||
                SafeFileSystem.IsExcludedPath(projectDirectory))
            {
                continue;
            }

            AddFiles(project, projectDirectory, "*.cs", csharpFiles);
            AddFiles(project, projectDirectory, "*.edmx", edmxFiles);
            AddFiles(project, projectDirectory, "*.dbml", dbmlFiles);
            AddFiles(project, projectDirectory, "*.tt", t4Files);

            foreach (var migrationDirectory in DiscoverMigrationDirectories(projectDirectory))
            {
                migrationDirectories.Add(migrationDirectory);
            }
        }

        return new ScanFileInventory(
            csharpFiles,
            edmxFiles,
            dbmlFiles,
            t4Files,
            migrationDirectories
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static void AddFiles(
        DiscoveredProject project,
        string projectDirectory,
        string searchPattern,
        ICollection<ScanFile> files)
    {
        foreach (var path in SafeFileSystem.EnumerateFiles(projectDirectory, searchPattern))
        {
            files.Add(CreateScanFile(project, projectDirectory, path));
        }
    }

    private static ScanFile CreateScanFile(
        DiscoveredProject project,
        string projectDirectory,
        string path)
    {
        return new ScanFile(
            project.Name,
            project.ProjectFilePath,
            projectDirectory,
            path,
            CreateRelativePath(projectDirectory, path),
            Path.GetExtension(path),
            SafeFileSystem.ReadAllTextOrEmpty(path));
    }

    private static IReadOnlyList<string> DiscoverMigrationDirectories(string projectDirectory)
    {
        return SafeFileSystem
            .EnumerateDirectories(projectDirectory)
            .Where(IsMigrationDirectory)
            .ToArray();
    }

    private static bool IsMigrationDirectory(string directory)
    {
        var name = Path.GetFileName(directory);

        return name.Equals("Migrations", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".Migrations", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateRelativePath(string projectDirectory, string path)
    {
        try
        {
            return Path.GetRelativePath(projectDirectory, path);
        }
        catch
        {
            return path;
        }
    }
}