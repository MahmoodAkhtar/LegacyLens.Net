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
                    var address = endpoint.Attribute("address")?.Value;
                    var binding = endpoint.Attribute("binding")?.Value;
                    var contract = endpoint.Attribute("contract")?.Value;
                    var bindingConfiguration = endpoint.Attribute("bindingConfiguration")?.Value;
                    var behaviorConfiguration = endpoint.Attribute("behaviorConfiguration")?.Value;
                    
                    var bindingElement = FindBindingElement(
                        serviceModelElement,
                        binding,
                        bindingConfiguration);

                    var securityElement = bindingElement?
                        .Elements()
                        .FirstOrDefault(x => x.Name.LocalName == "security");

                    var transportElement = securityElement?
                        .Elements()
                        .FirstOrDefault(x => x.Name.LocalName == "transport");

                    var messageElement = securityElement?
                        .Elements()
                        .FirstOrDefault(x => x.Name.LocalName == "message");
                    
                    endpoints.Add(new WcfEndpoint
                    {
                        ConfigFilePath = configFile,
                        ServiceName = serviceName,
                        Address = address,
                        Binding = binding,
                        Contract = contract,
                        BindingConfiguration = bindingConfiguration,
                        BehaviorConfiguration = behaviorConfiguration,
                        SecurityMode = securityElement?.Attribute("mode")?.Value,
                        TransportClientCredentialType = transportElement?.Attribute("clientCredentialType")?.Value,
                        MessageClientCredentialType = messageElement?.Attribute("clientCredentialType")?.Value,
                        IsMetadataExchangeEndpoint =
                            string.Equals(contract, "IMetadataExchange", StringComparison.OrdinalIgnoreCase) ||
                            binding?.StartsWith("mex", StringComparison.OrdinalIgnoreCase) == true
                    });
                }
            }
        }

        return endpoints;
    }
    
    private static XElement? FindBindingElement(
        XElement serviceModelElement,
        string? binding,
        string? bindingConfiguration)
    {
        if (string.IsNullOrWhiteSpace(binding) ||
            string.IsNullOrWhiteSpace(bindingConfiguration))
        {
            return null;
        }

        return serviceModelElement
            .Descendants()
            .FirstOrDefault(x =>
                x.Name.LocalName == binding &&
                x.Elements().Any(y =>
                    y.Name.LocalName == "binding" &&
                    string.Equals(
                        y.Attribute("name")?.Value,
                        bindingConfiguration,
                        StringComparison.OrdinalIgnoreCase)))
            ?.Elements()
            .FirstOrDefault(x =>
                x.Name.LocalName == "binding" &&
                string.Equals(
                    x.Attribute("name")?.Value,
                    bindingConfiguration,
                    StringComparison.OrdinalIgnoreCase));
    }
}