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
            var targetFramework = ReadTargetFramework(document);
            var projectReferences = ReadProjectReferences(document);
            var packageReferenceDetails = ReadPackageReferenceDetails(document, projectFile);
            var packageReferences = packageReferenceDetails
                .Select(x => x.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
            var assemblyReferences = ReadAssemblyReferences(document);

            projects.Add(new DiscoveredProject
            {
                Name = projectName,
                ProjectFilePath = projectFile,
                TargetFramework = targetFramework,
                ProjectReferences = projectReferences,
                PackageReferences = packageReferences,
                PackageReferenceDetails = packageReferenceDetails,
                AssemblyReferences = assemblyReferences
            });
        }

        return projects;
    }

    private static string? ReadTargetFramework(XDocument projectDocument)
    {
        var targetFramework = projectDocument
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "TargetFramework")
            ?.Value;

        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            return targetFramework.Trim();
        }

        var targetFrameworks = projectDocument
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "TargetFrameworks")
            ?.Value;

        return string.IsNullOrWhiteSpace(targetFrameworks)
            ? null
            : targetFrameworks.Trim();
    }

    private static List<string> ReadProjectReferences(XDocument projectDocument)
    {
        return projectDocument
            .Descendants()
            .Where(x => x.Name.LocalName == "ProjectReference")
            .Select(x => x.Attribute("Include")?.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();
    }

    private static List<DiscoveredPackageReference> ReadPackageReferenceDetails(
        XDocument projectDocument,
        string projectFile)
    {
        var packageReferences = projectDocument
            .Descendants()
            .Where(x => x.Name.LocalName == "PackageReference")
            .Select(x => CreatePackageReference(x, projectFile))
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        packageReferences.AddRange(ReadPackagesConfigReferences(projectFile));

        return packageReferences
            .OrderBy(x => x.Name)
            .ThenBy(x => x.SourceFormat)
            .ThenBy(x => x.Version)
            .ToList();
    }

    private static DiscoveredPackageReference? CreatePackageReference(
        XElement packageReferenceElement,
        string projectFile)
    {
        var name = GetFirstNonEmptyAttributeValue(
            packageReferenceElement,
            "Include",
            "Update");

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var version = packageReferenceElement.Attribute("Version")?.Value;

        if (string.IsNullOrWhiteSpace(version))
        {
            version = packageReferenceElement
                .Elements()
                .FirstOrDefault(x => x.Name.LocalName == "Version")
                ?.Value;
        }

        return new DiscoveredPackageReference
        {
            Name = name.Trim(),
            Version = NormalizeOptional(version),
            SourceFormat = "PackageReference",
            SourcePath = projectFile
        };
    }

    private static List<DiscoveredPackageReference> ReadPackagesConfigReferences(string projectFile)
    {
        var projectDirectory = Path.GetDirectoryName(projectFile);

        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return new List<DiscoveredPackageReference>();
        }

        var packagesConfigPath = Path.Combine(projectDirectory, "packages.config");

        if (!File.Exists(packagesConfigPath))
        {
            return new List<DiscoveredPackageReference>();
        }

        XDocument document;

        try
        {
            document = XDocument.Load(packagesConfigPath);
        }
        catch
        {
            return new List<DiscoveredPackageReference>();
        }

        return document
            .Descendants()
            .Where(x => x.Name.LocalName == "package")
            .Select(x =>
            {
                var name = x.Attribute("id")?.Value;

                if (string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }

                return new DiscoveredPackageReference
                {
                    Name = name.Trim(),
                    Version = NormalizeOptional(x.Attribute("version")?.Value),
                    SourceFormat = "packages.config",
                    SourcePath = packagesConfigPath,
                    PackageTargetFramework = NormalizeOptional(x.Attribute("targetFramework")?.Value)
                };
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();
    }
    
    private static List<string> ReadAssemblyReferences(XDocument projectDocument)
    {
        return projectDocument
            .Descendants()
            .Where(x => x.Name.LocalName == "Reference")
            .Select(x => x.Attribute("Include")?.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Select(x => x.Split(',')[0].Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static string? GetFirstNonEmptyAttributeValue(
        XElement element,
        params string[] attributeNames)
    {
        foreach (var attributeName in attributeNames)
        {
            var value = element.Attribute(attributeName)?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
