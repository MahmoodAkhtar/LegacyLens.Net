using System.Text.RegularExpressions;
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

            var appSettings = document
                .Descendants()
                .Where(x => x.Name.LocalName == "appSettings")
                .Elements()
                .Where(x => x.Name.LocalName == "add")
                .Select(x => new DiscoveredAppSetting
                {
                    Key = GetAttributeValue(x, "key") ?? string.Empty,
                    MaskedValue = MaskSensitiveValue(
                        GetAttributeValue(x, "value"),
                        GetAttributeValue(x, "key"))
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToList();

            var connectionStrings = document
                .Descendants()
                .Where(x => x.Name.LocalName == "connectionStrings")
                .Elements()
                .Where(x => x.Name.LocalName == "add")
                .Select(x => new DiscoveredConnectionString
                {
                    Name = GetAttributeValue(x, "name") ?? string.Empty,
                    ProviderName = GetAttributeValue(x, "providerName"),
                    MaskedConnectionString = MaskSensitiveValue(
                        GetAttributeValue(x, "connectionString"),
                        GetAttributeValue(x, "name"))
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .ToList();

            var customSections = document
                .Descendants()
                .Where(x => x.Name.LocalName == "configSections")
                .Descendants()
                .Where(x => x.Name.LocalName == "section" || x.Name.LocalName == "sectionGroup")
                .Select(x => new DiscoveredConfigSection
                {
                    Name = GetAttributeValue(x, "name") ?? string.Empty,
                    Type = GetAttributeValue(x, "type")
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .ToList();

            results.Add(new DiscoveredConfigFile
            {
                FilePath = configFile,
                AppSettingsCount = appSettings.Count,
                ConnectionStringsCount = connectionStrings.Count,
                CustomSectionCount = customSections.Count,
                AppSettings = appSettings,
                ConnectionStrings = connectionStrings,
                CustomSections = customSections
            });
        }

        return results;
    }

    private static string? GetAttributeValue(XElement element, string attributeName)
    {
        return element
            .Attributes()
            .FirstOrDefault(x => string.Equals(x.Name.LocalName, attributeName, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private static string? MaskSensitiveValue(string? value, string? key = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (LooksSensitive(key))
        {
            return "***";
        }

        var masked = value;

        masked = Regex.Replace(
            masked,
            @"(?i)(password|pwd|user\s*id|uid|accountkey|accesskey|sharedaccesskey|clientsecret|client_secret|secret|token|apikey|api_key|sig)\s*=\s*([^;&#""'\s]+)",
            match => $"{match.Groups[1].Value}=***");

        masked = Regex.Replace(
            masked,
            @"(?i)([?&](?:password|pwd|accountkey|accesskey|sharedaccesskey|clientsecret|client_secret|secret|token|apikey|api_key|sig|code)=)([^&#]*)",
            match => $"{match.Groups[1].Value}***");

        masked = Regex.Replace(
            masked,
            @"(?i)(https?://)([^:/?#\s]+):([^@/?#\s]+)@",
            "$1***:***@");

        return masked;
    }

    private static bool LooksSensitive(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return key.Contains("password", StringComparison.OrdinalIgnoreCase)
            || key.Contains("pwd", StringComparison.OrdinalIgnoreCase)
            || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || key.Contains("token", StringComparison.OrdinalIgnoreCase)
            || key.Contains("apikey", StringComparison.OrdinalIgnoreCase)
            || key.Contains("api_key", StringComparison.OrdinalIgnoreCase)
            || key.Contains("accesskey", StringComparison.OrdinalIgnoreCase)
            || key.Contains("clientsecret", StringComparison.OrdinalIgnoreCase)
            || key.Contains("client_secret", StringComparison.OrdinalIgnoreCase);
    }
}