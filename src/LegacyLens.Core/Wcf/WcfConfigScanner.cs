using System.Xml.Linq;

namespace LegacyLens.Core.Wcf;

public sealed class WcfConfigScanner
{
    public IReadOnlyList<WcfEndpoint> Scan(string rootPath)
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

        var endpoints = new List<WcfEndpoint>();

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

            var serviceModelElement = document
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "system.serviceModel");

            if (serviceModelElement is null)
            {
                continue;
            }

            var services = serviceModelElement
                .Descendants()
                .Where(x => x.Name.LocalName == "service");

            foreach (var service in services)
            {
                var serviceName = service.Attribute("name")?.Value;

                var serviceEndpoints = service
                    .Descendants()
                    .Where(x => x.Name.LocalName == "endpoint");

                foreach (var endpoint in serviceEndpoints)
                {
                    endpoints.Add(new WcfEndpoint
                    {
                        ConfigFilePath = configFile,
                        ServiceName = serviceName,
                        Address = endpoint.Attribute("address")?.Value,
                        Binding = endpoint.Attribute("binding")?.Value,
                        Contract = endpoint.Attribute("contract")?.Value
                    });
                }
            }
        }

        return endpoints;
    }
}