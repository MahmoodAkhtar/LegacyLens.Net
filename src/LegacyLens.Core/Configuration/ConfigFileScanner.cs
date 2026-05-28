using System.Xml.Linq;

namespace LegacyLens.Core.Configuration;

public sealed class ConfigFileScanner
{
    public IReadOnlyList<DiscoveredConfigFile> Scan(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path cannot be empty.", nameof(rootPath));
        }

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Root path does not exist: {rootPath}");
        }

        var configFiles = Directory
            .GetFiles(rootPath, "*.config", SearchOption.AllDirectories)
            .Where(x =>
                string.Equals(Path.GetFileName(x), "app.config", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(x), "web.config", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var results = new List<DiscoveredConfigFile>();

        foreach (var configFile in configFiles)
        {
            XDocument document;

            try
            {
                document = XDocument.Load(configFile);
            }
            catch
            {
                continue;
            }

            var appSettingsCount = document
                .Descendants()
                .Where(x => x.Name.LocalName == "appSettings")
                .Elements()
                .Count(x => x.Name.LocalName == "add");

            var connectionStringsCount = document
                .Descendants()
                .Where(x => x.Name.LocalName == "connectionStrings")
                .Elements()
                .Count(x => x.Name.LocalName == "add");

            var customSectionCount = document
                .Descendants()
                .Where(x => x.Name.LocalName == "configSections")
                .Descendants()
                .Count(x => x.Name.LocalName == "section" || x.Name.LocalName == "sectionGroup");

            results.Add(new DiscoveredConfigFile
            {
                FilePath = configFile,
                AppSettingsCount = appSettingsCount,
                ConnectionStringsCount = connectionStringsCount,
                CustomSectionCount = customSectionCount
            });
        }

        return results;
    }
}