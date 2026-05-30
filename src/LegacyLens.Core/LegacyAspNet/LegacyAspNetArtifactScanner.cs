using System.Text.RegularExpressions;

namespace LegacyLens.Core.LegacyAspNet;

public sealed class LegacyAspNetArtifactScanner
{
    private static readonly Regex MvcControllerClassRegex = new(
        @"\bclass\s+(?<name>[A-Za-z_][A-Za-z0-9_]*Controller)\s*:\s*(?<baseTypes>[^{]+)\{",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public IReadOnlyList<DiscoveredLegacyAspNetArtifact> Scan(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path cannot be empty.", nameof(rootPath));
        }

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Root path does not exist: {rootPath}");
        }

        var artifacts = new List<DiscoveredLegacyAspNetArtifact>();

        AddFileBasedArtifacts(rootPath, artifacts);
        AddSourceLevelArtifacts(rootPath, artifacts);

        return artifacts
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.FilePath)
            .ToList();
    }

    private static void AddFileBasedArtifacts(
        string rootPath,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        foreach (var filePath in Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath);

            if (extension.Equals(".aspx", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.WebFormsPage,
                    filePath));

                continue;
            }

            if (extension.Equals(".ascx", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.WebFormsUserControl,
                    filePath));

                continue;
            }

            if (extension.Equals(".master", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.MasterPage,
                    filePath));

                continue;
            }

            if (extension.Equals(".asmx", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.AsmxWebService,
                    filePath));

                continue;
            }

            if (extension.Equals(".ashx", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.HttpHandler,
                    filePath));

                continue;
            }

            if (fileName.Equals("Global.asax", StringComparison.OrdinalIgnoreCase))
            {
                artifacts.Add(CreateArtifact(
                    LegacyAspNetArtifactKind.GlobalAsax,
                    filePath));
            }
        }
    }

    private static void AddSourceLevelArtifacts(
        string rootPath,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        foreach (var sourceFilePath in Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories))
        {
            string source;

            try
            {
                source = File.ReadAllText(sourceFilePath);
            }
            catch
            {
                continue;
            }

            AddMvcControllerArtifacts(sourceFilePath, source, artifacts);
            AddRouteConfigArtifact(sourceFilePath, source, artifacts);
        }
    }

    private static void AddMvcControllerArtifacts(
        string sourceFilePath,
        string source,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        foreach (Match match in MvcControllerClassRegex.Matches(source))
        {
            var controllerName = match.Groups["name"].Value;
            var baseTypes = match.Groups["baseTypes"].Value;

            if (!InheritsFromMvcController(baseTypes))
            {
                continue;
            }

            artifacts.Add(new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.MvcController,
                FilePath = sourceFilePath,
                Name = controllerName
            });
        }
    }

    private static void AddRouteConfigArtifact(
        string sourceFilePath,
        string source,
        List<DiscoveredLegacyAspNetArtifact> artifacts)
    {
        if (!Path.GetFileName(sourceFilePath).Equals("RouteConfig.cs", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!LooksLikeAspNetRouteConfig(source))
        {
            return;
        }

        artifacts.Add(CreateArtifact(
            LegacyAspNetArtifactKind.RouteConfig,
            sourceFilePath));
    }

    private static bool InheritsFromMvcController(string baseTypes)
    {
        return baseTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(IsMvcControllerBaseType);
    }

    private static bool IsMvcControllerBaseType(string baseType)
    {
        return baseType.Equals("Controller", StringComparison.OrdinalIgnoreCase) ||
               baseType.Equals("System.Web.Mvc.Controller", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeAspNetRouteConfig(string source)
    {
        return source.Contains("RouteCollection", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("routes.MapRoute", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("System.Web.Routing", StringComparison.OrdinalIgnoreCase);
    }

    private static DiscoveredLegacyAspNetArtifact CreateArtifact(
        LegacyAspNetArtifactKind kind,
        string filePath)
    {
        return new DiscoveredLegacyAspNetArtifact
        {
            Kind = kind,
            FilePath = filePath,
            Name = Path.GetFileName(filePath)
        };
    }
}