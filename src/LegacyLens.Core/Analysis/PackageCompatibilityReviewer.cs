using LegacyLens.Core.Discovery;

namespace LegacyLens.Core.Analysis;

public sealed class PackageCompatibilityReviewer
{
    public IReadOnlyList<PackageCompatibilityReviewItem> Review(
        IReadOnlyList<DiscoveredProject> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        return projects
            .SelectMany(project => project.PackageReferenceDetails.Select(package =>
                new PackageCompatibilityReviewItem
                {
                    ProjectName = project.Name,
                    ProjectTargetFramework = project.TargetFramework,
                    PackageName = package.Name,
                    Version = package.Version,
                    PackageTargetFramework = package.PackageTargetFramework,
                    SourceFormat = package.SourceFormat,
                    SourcePath = package.SourcePath,
                    Concern = BuildConcern(project, package)
                }))
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.PackageName)
            .ThenBy(x => x.SourceFormat)
            .ToList();
    }

    private static string BuildConcern(
        DiscoveredProject project,
        DiscoveredPackageReference package)
    {
        var concerns = new List<string>();

        if (string.IsNullOrWhiteSpace(package.Version))
        {
            concerns.Add(
                "Package version was not found in the source file. Review package restore behaviour and version management before upgrade planning.");
        }

        if (package.SourceFormat.Equals("packages.config", StringComparison.OrdinalIgnoreCase))
        {
            concerns.Add(
                "Legacy packages.config reference. Review package restore format and possible migration to PackageReference during upgrade planning.");
        }

        if (PackageTargetFrameworkDiffersFromProject(project.TargetFramework, package.PackageTargetFramework))
        {
            concerns.Add(
                "Package target framework differs from the project target framework. Review whether the package reference metadata is stale or copied from another project.");
        }

        if (package.Name.StartsWith("System.ServiceModel", StringComparison.OrdinalIgnoreCase))
        {
            concerns.Add(
                "WCF-related package. Review WCF usage and replacement strategy before upgrading.");
        }

        if (package.Name.Equals("EntityFramework", StringComparison.OrdinalIgnoreCase))
        {
            concerns.Add(
                "Classic Entity Framework should be reviewed before migration to EF Core or modern .NET.");
        }

        if (package.Name.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
        {
            concerns.Add(
                "Common package, but serialization behaviour may need review during ASP.NET Core migration.");
        }

        if (ProjectTargetsOldDotNetFramework(project.TargetFramework))
        {
            concerns.Add(
                "Project targets .NET Framework. Review direct package dependencies as part of the framework upgrade plan.");
        }

        return concerns.Count == 0
            ? "No specific compatibility concern detected by the static MVP rules."
            : string.Join(" ", concerns);
    }

    private static bool ProjectTargetsOldDotNetFramework(string? targetFramework)
    {
        return SplitTargetFrameworks(targetFramework)
            .Any(x => x.StartsWith("net4", StringComparison.OrdinalIgnoreCase));
    }

    private static bool PackageTargetFrameworkDiffersFromProject(
        string? projectTargetFramework,
        string? packageTargetFramework)
    {
        if (string.IsNullOrWhiteSpace(projectTargetFramework) ||
            string.IsNullOrWhiteSpace(packageTargetFramework))
        {
            return false;
        }

        var projectTargetFrameworks = SplitTargetFrameworks(projectTargetFramework);

        return projectTargetFrameworks.Count > 0 &&
               !projectTargetFrameworks.Contains(
                   packageTargetFramework.Trim(),
                   StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> SplitTargetFrameworks(string? targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            return Array.Empty<string>();
        }

        return targetFramework
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
}
