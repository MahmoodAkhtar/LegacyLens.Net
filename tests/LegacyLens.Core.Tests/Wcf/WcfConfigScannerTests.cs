using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Wcf;

public sealed class WcfConfigScannerTests : IDisposable
{
    private readonly string _rootPath;

    public WcfConfigScannerTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void Scan_ReturnsBindingConfiguration_WhenEndpointUsesNamedBinding()
    {
        var configPath = Path.Combine(_rootPath, "web.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <system.serviceModel>
                <bindings>
                  <basicHttpBinding>
                    <binding name="CustomerBinding" />
                  </basicHttpBinding>
                </bindings>
                <services>
                  <service name="SampleLegacyApp.Services.CustomerService">
                    <endpoint
                      address=""
                      binding="basicHttpBinding"
                      bindingConfiguration="CustomerBinding"
                      contract="SampleLegacyApp.Contracts.ICustomerService" />
                  </service>
                </services>
              </system.serviceModel>
            </configuration>
            """);

        var scanner = new WcfConfigScanner();

        var endpoints = scanner.Scan(_rootPath);

        var endpoint = Assert.Single(endpoints);

        Assert.Equal("SampleLegacyApp.Services.CustomerService", endpoint.ServiceName);
        Assert.Equal("basicHttpBinding", endpoint.Binding);
        Assert.Equal("CustomerBinding", endpoint.BindingConfiguration);
        Assert.Equal("SampleLegacyApp.Contracts.ICustomerService", endpoint.Contract);
        Assert.Equal(configPath, endpoint.ConfigFilePath);
    }

    [Fact]
    public void Scan_ReturnsSecurityMode_WhenBindingConfigurationHasSecurity()
    {
        File.WriteAllText(
            Path.Combine(_rootPath, "app.config"),
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <system.serviceModel>
                <bindings>
                  <basicHttpBinding>
                    <binding name="SecureCustomerBinding">
                      <security mode="Transport" />
                    </binding>
                  </basicHttpBinding>
                </bindings>
                <services>
                  <service name="SampleLegacyApp.Services.CustomerService">
                    <endpoint
                      address=""
                      binding="basicHttpBinding"
                      bindingConfiguration="SecureCustomerBinding"
                      contract="SampleLegacyApp.Contracts.ICustomerService" />
                  </service>
                </services>
              </system.serviceModel>
            </configuration>
            """);

        var scanner = new WcfConfigScanner();

        var endpoints = scanner.Scan(_rootPath);

        var endpoint = Assert.Single(endpoints);

        Assert.Equal("basicHttpBinding", endpoint.Binding);
        Assert.Equal("SecureCustomerBinding", endpoint.BindingConfiguration);
        Assert.Equal("Transport", endpoint.SecurityMode);
    }

    [Fact]
    public void Scan_ReturnsTransportClientCredentialType_WhenConfigured()
    {
        File.WriteAllText(
            Path.Combine(_rootPath, "web.config"),
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <system.serviceModel>
                <bindings>
                  <basicHttpBinding>
                    <binding name="WindowsCustomerBinding">
                      <security mode="Transport">
                        <transport clientCredentialType="Windows" />
                      </security>
                    </binding>
                  </basicHttpBinding>
                </bindings>
                <services>
                  <service name="SampleLegacyApp.Services.CustomerService">
                    <endpoint
                      address=""
                      binding="basicHttpBinding"
                      bindingConfiguration="WindowsCustomerBinding"
                      contract="SampleLegacyApp.Contracts.ICustomerService" />
                  </service>
                </services>
              </system.serviceModel>
            </configuration>
            """);

        var scanner = new WcfConfigScanner();

        var endpoints = scanner.Scan(_rootPath);

        var endpoint = Assert.Single(endpoints);

        Assert.Equal("Transport", endpoint.SecurityMode);
        Assert.Equal("Windows", endpoint.TransportClientCredentialType);
    }

    [Fact]
    public void Scan_MarksEndpointAsMetadataExchangeEndpoint_WhenContractIsIMetadataExchange()
    {
        File.WriteAllText(
            Path.Combine(_rootPath, "web.config"),
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <system.serviceModel>
                <services>
                  <service name="SampleLegacyApp.Services.CustomerService">
                    <endpoint
                      address="mex"
                      binding="mexHttpBinding"
                      contract="IMetadataExchange" />
                  </service>
                </services>
              </system.serviceModel>
            </configuration>
            """);

        var scanner = new WcfConfigScanner();

        var endpoints = scanner.Scan(_rootPath);

        var endpoint = Assert.Single(endpoints);

        Assert.Equal("IMetadataExchange", endpoint.Contract);
        Assert.True(endpoint.IsMetadataExchangeEndpoint);
    }

    [Fact]
    public void Scan_MarksEndpointAsMetadataExchangeEndpoint_WhenBindingStartsWithMex()
    {
        File.WriteAllText(
            Path.Combine(_rootPath, "app.config"),
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <system.serviceModel>
                <services>
                  <service name="SampleLegacyApp.Services.CustomerService">
                    <endpoint
                      address="mex"
                      binding="mexTcpBinding"
                      contract="SampleLegacyApp.Contracts.ICustomerService" />
                  </service>
                </services>
              </system.serviceModel>
            </configuration>
            """);

        var scanner = new WcfConfigScanner();

        var endpoints = scanner.Scan(_rootPath);

        var endpoint = Assert.Single(endpoints);

        Assert.Equal("mexTcpBinding", endpoint.Binding);
        Assert.Equal("SampleLegacyApp.Contracts.ICustomerService", endpoint.Contract);
        Assert.True(endpoint.IsMetadataExchangeEndpoint);
    }

    [Fact]
    public void Scan_ReturnsTimeouts_WhenBindingConfigurationDefinesTimeouts()
    {
        File.WriteAllText(
            Path.Combine(_rootPath, "web.config"),
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <system.serviceModel>
                <bindings>
                  <basicHttpBinding>
                    <binding
                      name="CustomerBinding"
                      openTimeout="00:01:00"
                      closeTimeout="00:02:00"
                      sendTimeout="00:03:00"
                      receiveTimeout="00:04:00" />
                  </basicHttpBinding>
                </bindings>
                <services>
                  <service name="SampleLegacyApp.Services.CustomerService">
                    <endpoint
                      address=""
                      binding="basicHttpBinding"
                      bindingConfiguration="CustomerBinding"
                      contract="SampleLegacyApp.Contracts.ICustomerService" />
                  </service>
                </services>
              </system.serviceModel>
            </configuration>
            """);

        var scanner = new WcfConfigScanner();

        var endpoints = scanner.Scan(_rootPath);

        var endpoint = Assert.Single(endpoints);

        Assert.Equal("00:01:00", endpoint.OpenTimeout);
        Assert.Equal("00:02:00", endpoint.CloseTimeout);
        Assert.Equal("00:03:00", endpoint.SendTimeout);
        Assert.Equal("00:04:00", endpoint.ReceiveTimeout);
    }

    [Fact]
    public void Scan_ReturnsMessageSizeLimits_WhenBindingConfigurationDefinesLimits()
    {
        File.WriteAllText(
            Path.Combine(_rootPath, "web.config"),
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <system.serviceModel>
                <bindings>
                  <basicHttpBinding>
                    <binding
                      name="CustomerBinding"
                      maxReceivedMessageSize="1048576"
                      maxBufferSize="65536"
                      maxBufferPoolSize="524288" />
                  </basicHttpBinding>
                </bindings>
                <services>
                  <service name="SampleLegacyApp.Services.CustomerService">
                    <endpoint
                      address=""
                      binding="basicHttpBinding"
                      bindingConfiguration="CustomerBinding"
                      contract="SampleLegacyApp.Contracts.ICustomerService" />
                  </service>
                </services>
              </system.serviceModel>
            </configuration>
            """);

        var scanner = new WcfConfigScanner();

        var endpoints = scanner.Scan(_rootPath);

        var endpoint = Assert.Single(endpoints);

        Assert.Equal("1048576", endpoint.MaxReceivedMessageSize);
        Assert.Equal("65536", endpoint.MaxBufferSize);
        Assert.Equal("524288", endpoint.MaxBufferPoolSize);
    }

    [Fact]
    public void Scan_ReturnsTransferMode_WhenBindingConfigurationDefinesTransferMode()
    {
        File.WriteAllText(
            Path.Combine(_rootPath, "web.config"),
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <system.serviceModel>
                <bindings>
                  <basicHttpBinding>
                    <binding
                      name="CustomerBinding"
                      transferMode="Streamed" />
                  </basicHttpBinding>
                </bindings>
                <services>
                  <service name="SampleLegacyApp.Services.CustomerService">
                    <endpoint
                      address=""
                      binding="basicHttpBinding"
                      bindingConfiguration="CustomerBinding"
                      contract="SampleLegacyApp.Contracts.ICustomerService" />
                  </service>
                </services>
              </system.serviceModel>
            </configuration>
            """);

        var scanner = new WcfConfigScanner();

        var endpoints = scanner.Scan(_rootPath);

        var endpoint = Assert.Single(endpoints);

        Assert.Equal("Streamed", endpoint.TransferMode);
    }

    [Fact]
    public void Scan_ReturnsReaderQuotas_WhenBindingConfigurationDefinesReaderQuotas()
    {
        File.WriteAllText(
            Path.Combine(_rootPath, "web.config"),
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <system.serviceModel>
                <bindings>
                  <basicHttpBinding>
                    <binding name="CustomerBinding">
                      <readerQuotas
                        maxDepth="32"
                        maxStringContentLength="8192"
                        maxArrayLength="16384"
                        maxBytesPerRead="4096"
                        maxNameTableCharCount="16384" />
                    </binding>
                  </basicHttpBinding>
                </bindings>
                <services>
                  <service name="SampleLegacyApp.Services.CustomerService">
                    <endpoint
                      address=""
                      binding="basicHttpBinding"
                      bindingConfiguration="CustomerBinding"
                      contract="SampleLegacyApp.Contracts.ICustomerService" />
                  </service>
                </services>
              </system.serviceModel>
            </configuration>
            """);

        var scanner = new WcfConfigScanner();

        var endpoints = scanner.Scan(_rootPath);

        var endpoint = Assert.Single(endpoints);

        Assert.Equal("32", endpoint.ReaderQuotaMaxDepth);
        Assert.Equal("8192", endpoint.ReaderQuotaMaxStringContentLength);
        Assert.Equal("16384", endpoint.ReaderQuotaMaxArrayLength);
        Assert.Equal("4096", endpoint.ReaderQuotaMaxBytesPerRead);
        Assert.Equal("16384", endpoint.ReaderQuotaMaxNameTableCharCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}