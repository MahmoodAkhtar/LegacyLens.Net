using LegacyLens.Core.Files;
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

    [Fact]
    public void ScanInventory_ReturnsServiceContract_WhenScanFileContainsServiceContractAttribute()
    {
        var scanner = new WcfServiceContractScanner();
        var inventory = CreateInventory(
            CreateScanFile(
                "CustomerContracts.cs",
                """
                using System.ServiceModel;

                [ServiceContract]
                public interface ICustomerService
                {
                    [OperationContract]
                    CustomerDto GetCustomer(int id);
                }
                """));

        var contracts = scanner.Scan(inventory);

        var contract = Assert.Single(contracts);
        Assert.Equal("ICustomerService", contract.Name);
        Assert.EndsWith("CustomerContracts.cs", contract.SourceFilePath);
        Assert.Equal(["GetCustomer"], contract.Operations);
    }

    [Fact]
    public void ScanInventory_ReturnsServiceContract_WhenScanFileContainsServiceContractAttributeSuffix()
    {
        var scanner = new WcfServiceContractScanner();
        var inventory = CreateInventory(
            CreateScanFile(
                "CustomerContracts.cs",
                """
                using System.ServiceModel;

                [ServiceContractAttribute]
                public interface ICustomerService
                {
                }
                """));

        var contracts = scanner.Scan(inventory);

        var contract = Assert.Single(contracts);
        Assert.Equal("ICustomerService", contract.Name);
    }

    [Fact]
    public void ScanInventory_ReturnsOperation_WhenScanFileContainsOperationContractAttribute()
    {
        var scanner = new WcfServiceContractScanner();
        var inventory = CreateInventory(
            CreateScanFile(
                "CustomerContracts.cs",
                """
                using System.ServiceModel;

                [ServiceContract]
                public interface ICustomerService
                {
                    [OperationContract]
                    CustomerDto GetCustomer(int id);
                }
                """));

        var contracts = scanner.Scan(inventory);

        var contract = Assert.Single(contracts);
        Assert.Equal(["GetCustomer"], contract.Operations);
    }

    [Fact]
    public void ScanInventory_ReturnsOperation_WhenScanFileContainsOperationContractAttributeSuffix()
    {
        var scanner = new WcfServiceContractScanner();
        var inventory = CreateInventory(
            CreateScanFile(
                "CustomerContracts.cs",
                """
                using System.ServiceModel;

                [ServiceContract]
                public interface ICustomerService
                {
                    [OperationContractAttribute]
                    CustomerDto GetCustomer(int id);
                }
                """));

        var contracts = scanner.Scan(inventory);

        var contract = Assert.Single(contracts);
        Assert.Equal(["GetCustomer"], contract.Operations);
    }

    [Fact]
    public void ScanInventory_ReturnsNoContracts_WhenNoServiceContractTextExists()
    {
        var scanner = new WcfServiceContractScanner();
        var inventory = CreateInventory(
            CreateScanFile(
                "CustomerContracts.cs",
                """
                public interface ICustomerService
                {
                    CustomerDto GetCustomer(int id);
                }
                """));

        var contracts = scanner.Scan(inventory);

        Assert.Empty(contracts);
    }

    [Fact]
    public void ScanInventory_HandlesMultipleContractsInSameFile()
    {
        var scanner = new WcfServiceContractScanner();
        var inventory = CreateInventory(
            CreateScanFile(
                "Contracts.cs",
                """
                using System.ServiceModel;

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
                """));

        var contracts = scanner.Scan(inventory);

        Assert.Equal(2, contracts.Count);
        Assert.Equal(["GetCustomer"], contracts.Single(contract => contract.Name == "ICustomerService").Operations);
        Assert.Equal(["GetOrder"], contracts.Single(contract => contract.Name == "IOrderService").Operations);
    }

    [Fact]
    public void ScanInventory_DoesNotReturnOperationsOutsideServiceContractInterface()
    {
        var scanner = new WcfServiceContractScanner();
        var inventory = CreateInventory(
            CreateScanFile(
                "CustomerContracts.cs",
                """
                using System.ServiceModel;

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
                """));

        var contracts = scanner.Scan(inventory);

        var contract = Assert.Single(contracts);
        Assert.Equal(["GetCustomer"], contract.Operations);
    }

    [Fact]
    public void ScanInventory_ReturnsEmptyResults_WhenInventoryIsEmpty()
    {
        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(ScanFileInventory.Empty);

        Assert.Empty(contracts);
    }

    [Fact]
    public void ScanInventory_ThrowsArgumentNullException_WhenInventoryIsNull()
    {
        var scanner = new WcfServiceContractScanner();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            scanner.Scan((ScanFileInventory)null!));

        Assert.Equal("fileInventory", exception.ParamName);
    }

    [Fact]
    public void ScanCSharpFiles_ThrowsArgumentNullException_WhenInputIsNull()
    {
        var scanner = new WcfServiceContractScanner();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            scanner.Scan((IReadOnlyCollection<ScanFile>)null!));

        Assert.Equal("csharpFiles", exception.ParamName);
    }

    [Fact]
    public void ScanCSharpFiles_ReturnsEmptyResults_WhenManyIndexedFilesHaveNoServiceContractText()
    {
        var files = Enumerable.Range(1, 100)
            .Select(index => CreateScanFile(
                $"Source{index}.cs",
                $"public sealed class Source{index} {{ }}"))
            .ToArray();

        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(files);

        Assert.Empty(contracts);
    }

    [Fact]
    public void ScanCSharpFiles_UsesScanFileContentWithoutRequiringFileToExistOnDisk()
    {
        var missingSourcePath = Path.Combine(_rootPath, "DoesNotExist.cs");
        var scanner = new WcfServiceContractScanner();

        var contracts = scanner.Scan(new[]
        {
            CreateScanFile(
                missingSourcePath,
                """
                using System.ServiceModel;

                [ServiceContract]
                public interface IInventoryBackedService
                {
                    [OperationContract]
                    void Ping();
                }
                """)
        });

        var contract = Assert.Single(contracts);
        Assert.Equal("IInventoryBackedService", contract.Name);
        Assert.Equal(missingSourcePath, contract.SourceFilePath);
        Assert.Equal(["Ping"], contract.Operations);
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

    private static ScanFileInventory CreateInventory(params ScanFile[] csharpFiles)
    {
        return new ScanFileInventory(
            csharpFiles,
            Array.Empty<ScanFile>(),
            Array.Empty<ScanFile>(),
            Array.Empty<ScanFile>(),
            Array.Empty<string>());
    }

    private ScanFile CreateScanFile(string relativePathOrFullPath, string content)
    {
        var fullPath = Path.IsPathRooted(relativePathOrFullPath)
            ? relativePathOrFullPath
            : Path.Combine(_rootPath, relativePathOrFullPath);

        var projectDirectory = _rootPath;

        return new ScanFile(
            "SampleLegacyApp.Contracts",
            Path.Combine(projectDirectory, "SampleLegacyApp.Contracts.csproj"),
            projectDirectory,
            fullPath,
            Path.GetFileName(fullPath),
            ".cs",
            content);
    }
}
