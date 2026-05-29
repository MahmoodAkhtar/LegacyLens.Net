using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Wcf;

public sealed class WcfServiceContractScannerTests : IDisposable
{
    private readonly string _rootPath;

    public WcfServiceContractScannerTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            "LegacyLensTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void Scan_ReturnsServiceContract_WhenInterfaceHasServiceContractAttribute()
    {
        WriteSourceFile(
            "CustomerContracts.cs",
            """
            using System.ServiceModel;

            namespace SampleLegacyApp.Contracts;

            [ServiceContract]
            public interface ICustomerService
            {
                [OperationContract]
                CustomerDto GetCustomer(int id);
            }
            """);

        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(_rootPath);

        var contract = Assert.Single(contracts);
        Assert.Equal("ICustomerService", contract.Name);
        Assert.EndsWith("CustomerContracts.cs", contract.SourceFilePath);
        Assert.Equal(["GetCustomer"], contract.Operations);
    }

    [Fact]
    public void Scan_ReturnsOnlyOperationsForEachContract_WhenMultipleContractsExistInSameFile()
    {
        WriteSourceFile(
            "Contracts.cs",
            """
            using System.ServiceModel;

            namespace SampleLegacyApp.Contracts;

            [ServiceContract]
            public interface ICustomerService
            {
                [OperationContract]
                CustomerDto GetCustomer(int id);
            }

            [ServiceContract]
            public interface IOrderService
            {
                [OperationContract]
                OrderDto GetOrder(int id);
            }
            """);

        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(_rootPath);

        Assert.Equal(2, contracts.Count);

        var customerContract = contracts.Single(x => x.Name == "ICustomerService");
        var orderContract = contracts.Single(x => x.Name == "IOrderService");

        Assert.Equal(["GetCustomer"], customerContract.Operations);
        Assert.Equal(["GetOrder"], orderContract.Operations);
    }

    [Fact]
    public void Scan_ReturnsOperation_WhenOperationContractHasConstructorArguments()
    {
        WriteSourceFile(
            "CustomerContracts.cs",
            """
            using System.ServiceModel;

            namespace SampleLegacyApp.Contracts;

            [ServiceContract(Name = "CustomerService")]
            public interface ICustomerService
            {
                [OperationContract(Action = "GetCustomer", ReplyAction = "GetCustomerResponse")]
                CustomerDto GetCustomer(int id);
            }
            """);

        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(_rootPath);

        var contract = Assert.Single(contracts);
        Assert.Equal("ICustomerService", contract.Name);
        Assert.Equal(["GetCustomer"], contract.Operations);
    }

    [Fact]
    public void Scan_ReturnsOperation_WhenAttributesUseAttributeSuffix()
    {
        WriteSourceFile(
            "CustomerContracts.cs",
            """
            using System.ServiceModel;

            namespace SampleLegacyApp.Contracts;

            [ServiceContractAttribute]
            public interface ICustomerService
            {
                [OperationContractAttribute]
                CustomerDto GetCustomer(int id);
            }
            """);

        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(_rootPath);

        var contract = Assert.Single(contracts);
        Assert.Equal("ICustomerService", contract.Name);
        Assert.Equal(["GetCustomer"], contract.Operations);
    }

    [Fact]
    public void Scan_ReturnsOperation_WhenReturnTypeIsGenericTask()
    {
        WriteSourceFile(
            "CustomerContracts.cs",
            """
            using System.ServiceModel;
            using System.Threading.Tasks;

            namespace SampleLegacyApp.Contracts;

            [ServiceContract]
            public interface ICustomerService
            {
                [OperationContract]
                Task<CustomerDto?> GetCustomerAsync(int id);
            }
            """);

        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(_rootPath);

        var contract = Assert.Single(contracts);
        Assert.Equal("ICustomerService", contract.Name);
        Assert.Equal(["GetCustomerAsync"], contract.Operations);
    }

    [Fact]
    public void Scan_DoesNotReturnOperationsOutsideServiceContractInterface()
    {
        WriteSourceFile(
            "CustomerContracts.cs",
            """
            using System.ServiceModel;

            namespace SampleLegacyApp.Contracts;

            public interface INotAServiceContract
            {
                [OperationContract]
                CustomerDto ShouldNotBeReturned(int id);
            }

            [ServiceContract]
            public interface ICustomerService
            {
                [OperationContract]
                CustomerDto GetCustomer(int id);
            }

            public sealed class Helper
            {
                [OperationContract]
                public CustomerDto AlsoShouldNotBeReturned(int id)
                {
                    return new CustomerDto();
                }
            }
            """);

        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(_rootPath);

        var contract = Assert.Single(contracts);
        Assert.Equal("ICustomerService", contract.Name);
        Assert.Equal(["GetCustomer"], contract.Operations);
    }

    [Fact]
    public void Scan_ReturnsEmptyOperations_WhenServiceContractHasNoOperationContracts()
    {
        WriteSourceFile(
            "CustomerContracts.cs",
            """
            using System.ServiceModel;

            namespace SampleLegacyApp.Contracts;

            [ServiceContract]
            public interface ICustomerService
            {
                CustomerDto GetCustomer(int id);
            }
            """);

        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(_rootPath);

        var contract = Assert.Single(contracts);
        Assert.Equal("ICustomerService", contract.Name);
        Assert.Empty(contract.Operations);
    }

    [Fact]
    public void Scan_ReturnsNoContracts_WhenNoServiceContractsExist()
    {
        WriteSourceFile(
            "CustomerContracts.cs",
            """
            namespace SampleLegacyApp.Contracts;

            public interface ICustomerService
            {
                CustomerDto GetCustomer(int id);
            }
            """);

        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(_rootPath);

        Assert.Empty(contracts);
    }

    [Fact]
    public void Scan_ThrowsArgumentException_WhenRootPathIsEmpty()
    {
        var scanner = new WcfServiceContractScanner();

        Assert.Throws<ArgumentException>(() => scanner.Scan(""));
    }

    [Fact]
    public void Scan_ThrowsDirectoryNotFoundException_WhenRootPathDoesNotExist()
    {
        var scanner = new WcfServiceContractScanner();

        var missingPath = Path.Combine(_rootPath, "missing");

        Assert.Throws<DirectoryNotFoundException>(() => scanner.Scan(missingPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private void WriteSourceFile(string fileName, string content)
    {
        var filePath = Path.Combine(_rootPath, fileName);

        File.WriteAllText(filePath, content);
    }
}