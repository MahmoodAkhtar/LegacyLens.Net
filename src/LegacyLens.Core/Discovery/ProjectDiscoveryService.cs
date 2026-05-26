using System.Xml.Linq;

namespace LegacyLens.Core.Discovery;

public sealed class ProjectDiscoveryService
{
    public IReadOnlyList<DiscoveredProject> DiscoverProjects(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path cannot be empty.", nameof(rootPath));
        }

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Root path does not exist: {rootPath}");
        }

        var projectFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);

        var projects = new List<DiscoveredProject>();

        foreach (var projectFile in projectFiles)
        {
            var document = XDocument.Load(projectFile);

            var projectName = Path.GetFileNameWithoutExtension(projectFile);

            var targetFramework = document
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "TargetFramework")
                ?.Value;

            var projectReferences = document
                .Descendants()
                .Where(x => x.Name.LocalName == "ProjectReference")
                .Select(x => x.Attribute("Include")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToList();

            var packageReferences = ReadPackageReferences(document, projectFile);

            projects.Add(new DiscoveredProject
            {
                Name = projectName,
                ProjectFilePath = projectFile,
                TargetFramework = targetFramework,
                ProjectReferences = projectReferences,
                PackageReferences = packageReferences
            });
        }

        return projects;
    }

    private static List<string> ReadPackageReferences(XDocument projectDocument, string projectFile)
    {
        var projectPackageReferences = projectDocument
            .Descendants()
            .Where(x => x.Name.LocalName == "PackageReference")
            .Select(x => x.Attribute("Include")?.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!);

        var packagesConfigReferences = ReadPackagesConfigReferences(projectFile);

        return projectPackageReferences
            .Concat(packagesConfigReferences)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static List<string> ReadPackagesConfigReferences(string projectFile)
    {
        var projectDirectory = Path.GetDirectoryName(projectFile);

        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return new List<string>();
        }

        var packagesConfigPath = Path.Combine(projectDirectory, "packages.config");

        if (!File.Exists(packagesConfigPath))
        {
            return new List<string>();
        }

        XDocument document;

        try
        {
            document = XDocument.Load(packagesConfigPath);
        }
        catch
        {
            return new List<string>();
        }

        return document
            .Descendants()
            .Where(x => x.Name.LocalName == "package")
            .Select(x => x.Attribute("id")?.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }
}