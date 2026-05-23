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

            var packageReferences = document
                .Descendants()
                .Where(x => x.Name.LocalName == "PackageReference")
                .Select(x => x.Attribute("Include")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToList();

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
}