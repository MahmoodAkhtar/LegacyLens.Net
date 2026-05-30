namespace LegacyLens.Core.LegacyAspNet;

public sealed class LegacyAspNetArtifactScanner
{
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