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

                    var readerQuotasElement = bindingElement?
                        .Elements()
                        .FirstOrDefault(x => x.Name.LocalName == "readerQuotas");

                    endpoints.Add(new WcfEndpoint
                    {
                        ConfigFilePath = configFile,
                        ServiceName = serviceName,
                        Address = address,
                        Binding = binding,
                        Contract = contract,
                        BindingConfiguration = bindingConfiguration,
                        BehaviorConfiguration = behaviorConfiguration,
                        SecurityMode = GetAttributeValue(securityElement, "mode"),
                        TransportClientCredentialType = GetAttributeValue(transportElement, "clientCredentialType"),
                        MessageClientCredentialType = GetAttributeValue(messageElement, "clientCredentialType"),
                        IsMetadataExchangeEndpoint =
                            string.Equals(contract, "IMetadataExchange", StringComparison.OrdinalIgnoreCase) ||
                            binding?.StartsWith("mex", StringComparison.OrdinalIgnoreCase) == true,

                        OpenTimeout = GetAttributeValue(bindingElement, "openTimeout"),
                        CloseTimeout = GetAttributeValue(bindingElement, "closeTimeout"),
                        SendTimeout = GetAttributeValue(bindingElement, "sendTimeout"),
                        ReceiveTimeout = GetAttributeValue(bindingElement, "receiveTimeout"),
                        MaxReceivedMessageSize = GetAttributeValue(bindingElement, "maxReceivedMessageSize"),
                        MaxBufferSize = GetAttributeValue(bindingElement, "maxBufferSize"),
                        MaxBufferPoolSize = GetAttributeValue(bindingElement, "maxBufferPoolSize"),
                        TransferMode = GetAttributeValue(bindingElement, "transferMode"),

                        ReaderQuotaMaxDepth = GetAttributeValue(readerQuotasElement, "maxDepth"),
                        ReaderQuotaMaxStringContentLength =
                            GetAttributeValue(readerQuotasElement, "maxStringContentLength"),
                        ReaderQuotaMaxArrayLength = GetAttributeValue(readerQuotasElement, "maxArrayLength"),
                        ReaderQuotaMaxBytesPerRead = GetAttributeValue(readerQuotasElement, "maxBytesPerRead"),
                        ReaderQuotaMaxNameTableCharCount =
                            GetAttributeValue(readerQuotasElement, "maxNameTableCharCount")
                    });
                }
            }
        }

        return endpoints;
    }

    public IReadOnlyList<WcfBehaviour> ScanBehaviours(string rootPath)
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

        var behaviours = new List<WcfBehaviour>();

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

            AddServiceBehaviours(configFile, serviceModelElement, behaviours);
            AddEndpointBehaviours(configFile, serviceModelElement, behaviours);
        }

        return behaviours
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.ConfigFilePath)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static void AddServiceBehaviours(
        string configFile,
        XElement serviceModelElement,
        List<WcfBehaviour> behaviours)
    {
        var serviceBehaviourElements = serviceModelElement
            .Descendants()
            .Where(x => x.Name.LocalName == "serviceBehaviors")
            .Elements()
            .Where(x => x.Name.LocalName == "behavior");

        foreach (var behaviourElement in serviceBehaviourElements)
        {
            var serviceMetadataElement = behaviourElement
                .Elements()
                .FirstOrDefault(x => x.Name.LocalName == "serviceMetadata");

            var serviceDebugElement = behaviourElement
                .Elements()
                .FirstOrDefault(x => x.Name.LocalName == "serviceDebug");

            var serviceThrottlingElement = behaviourElement
                .Elements()
                .FirstOrDefault(x => x.Name.LocalName == "serviceThrottling");

            behaviours.Add(new WcfBehaviour
            {
                Kind = WcfBehaviourKind.ServiceBehaviour,
                ConfigFilePath = configFile,
                Name = GetAttributeValue(behaviourElement, "name"),

                HasServiceMetadata = serviceMetadataElement is not null,
                ServiceMetadataHttpGetEnabled = GetAttributeValue(serviceMetadataElement, "httpGetEnabled"),
                ServiceMetadataHttpsGetEnabled = GetAttributeValue(serviceMetadataElement, "httpsGetEnabled"),

                HasServiceDebug = serviceDebugElement is not null,
                IncludeExceptionDetailInFaults =
                    GetAttributeValue(serviceDebugElement, "includeExceptionDetailInFaults"),

                HasServiceThrottling = serviceThrottlingElement is not null,
                MaxConcurrentCalls = GetAttributeValue(serviceThrottlingElement, "maxConcurrentCalls"),
                MaxConcurrentSessions = GetAttributeValue(serviceThrottlingElement, "maxConcurrentSessions"),
                MaxConcurrentInstances = GetAttributeValue(serviceThrottlingElement, "maxConcurrentInstances")
            });
        }
    }

    private static void AddEndpointBehaviours(
        string configFile,
        XElement serviceModelElement,
        List<WcfBehaviour> behaviours)
    {
        var endpointBehaviourElements = serviceModelElement
            .Descendants()
            .Where(x => x.Name.LocalName == "endpointBehaviors")
            .Elements()
            .Where(x => x.Name.LocalName == "behavior");

        foreach (var behaviourElement in endpointBehaviourElements)
        {
            var webHttpElement = behaviourElement
                .Elements()
                .FirstOrDefault(x => x.Name.LocalName == "webHttp");

            behaviours.Add(new WcfBehaviour
            {
                Kind = WcfBehaviourKind.EndpointBehaviour,
                ConfigFilePath = configFile,
                Name = GetAttributeValue(behaviourElement, "name"),
                HasWebHttp = webHttpElement is not null
            });
        }
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

    private static string? GetAttributeValue(XElement? element, string attributeName)
    {
        if (element is null)
        {
            return null;
        }

        return element.Attribute(attributeName)?.Value;
    }
}