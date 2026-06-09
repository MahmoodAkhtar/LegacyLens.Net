using LegacyLens.Core.Analysis;
using LegacyLens.Core.Discovery;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class EdmxAnalyzerTests
{
    [Fact]
    public void Analyze_WhenScanPathIsEmpty_ThrowsArgumentException()
    {
        var analyzer = new EdmxAnalyzer();

        var exception = Assert.Throws<ArgumentException>(() =>
            analyzer.Analyze(
                string.Empty,
                Array.Empty<DiscoveredProject>()));

        Assert.Equal("scanPath", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenProjectsIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new EdmxAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                @"C:\Code\SampleLegacyApp",
                null!));

        Assert.Equal("projects", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenNoEdmxFilesExist_ReturnsEmptyReport()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var project = CreateProject(root);

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { project });

            Assert.Empty(report.Models);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenEdmxFileExists_ParsesModelSections()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var project = CreateProject(root);
            var edmxFile = Path.Combine(root, "LegacyModel.edmx");

            File.WriteAllText(edmxFile, CreateSampleEdmx());

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { project });

            var model = Assert.Single(report.Models);

            Assert.Equal("SampleLegacyApp.Data", model.ProjectName);
            Assert.Equal(edmxFile, model.FilePath);
            Assert.True(model.HasConceptualModel);
            Assert.True(model.HasStorageModel);
            Assert.True(model.HasMappingModel);
            Assert.True(model.HasDesignerMetadata);
            Assert.Null(model.ParseError);

            Assert.Contains(
                "http://schemas.microsoft.com/ado/2009/11/edmx",
                model.NamespaceUris);

            Assert.Contains(
                "http://schemas.microsoft.com/ado/2009/11/edm",
                model.NamespaceUris);

            Assert.Contains(
                "http://schemas.microsoft.com/ado/2009/11/edm/ssdl",
                model.NamespaceUris);

            Assert.Contains(
                "http://schemas.microsoft.com/ado/2009/11/mapping/cs",
                model.NamespaceUris);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenEdmxFileExists_ExtractsConceptualModelDetails()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var project = CreateProject(root);
            var edmxFile = Path.Combine(root, "LegacyModel.edmx");

            File.WriteAllText(edmxFile, CreateSampleEdmx());

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { project });

            var model = Assert.Single(report.Models);

            var entity = Assert.Single(model.ConceptualEntities);

            Assert.Equal("Customer", entity.Name);
            Assert.Equal("Customers", entity.EntitySet);
            Assert.Contains("Id", entity.KeyProperties);
            Assert.Equal(2, entity.PropertyCount);
            Assert.Equal(1, entity.NavigationPropertyCount);

            Assert.Contains("Customers", model.ConceptualEntitySets);
            Assert.Contains("Address", model.ComplexTypes);

            var association = Assert.Single(model.Associations);

            Assert.Equal("FK_Customer_Order", association.Name);
            Assert.Equal("Customer", association.FromRole);
            Assert.Equal("Orders", association.ToRole);
            Assert.Equal("1 to *", association.Multiplicity);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenEdmxFileExists_ExtractsStorageModelDetails()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var project = CreateProject(root);
            var edmxFile = Path.Combine(root, "LegacyModel.edmx");

            File.WriteAllText(edmxFile, CreateSampleEdmx());

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { project });

            var model = Assert.Single(report.Models);

            var storageEntity = Assert.Single(model.StorageEntities);

            Assert.Equal("CustomerTable", storageEntity.Name);
            Assert.Equal("CustomerTables", storageEntity.EntitySet);
            Assert.Equal("dbo", storageEntity.Schema);
            Assert.Equal("Customers", storageEntity.TableOrView);
            Assert.Equal(2, storageEntity.ColumnCount);
            Assert.True(storageEntity.HasDefiningQuery);

            var storeFunction = Assert.Single(model.StoreFunctions);

            Assert.Equal("GetCustomers", storeFunction.Name);
            Assert.Equal("dbo", storeFunction.Schema);
            Assert.False(storeFunction.IsComposable);
            Assert.Equal(1, storeFunction.ParameterCount);

            Assert.Equal(1, model.DefiningQueryCount);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenEdmxFileExists_ExtractsFunctionImportsAndMappings()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var project = CreateProject(root);
            var edmxFile = Path.Combine(root, "LegacyModel.edmx");

            File.WriteAllText(edmxFile, CreateSampleEdmx());

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { project });

            var model = Assert.Single(report.Models);

            var functionImport = Assert.Single(model.FunctionImports);

            Assert.Equal("GetCustomers", functionImport.Name);
            Assert.Equal("Collection(Model.Customer)", functionImport.ReturnType);
            Assert.Equal("GetCustomers", functionImport.StoreFunction);

            var mappingFragment = Assert.Single(model.MappingFragments);

            Assert.Equal("Customers", mappingFragment.EntitySet);
            Assert.Equal("Customer", mappingFragment.EntityType);
            Assert.Equal("CustomerTables", mappingFragment.StoreEntitySet);
            Assert.Equal(2, mappingFragment.ScalarPropertyCount);

            Assert.Equal(1, model.ModificationFunctionMappingCount);
            Assert.Equal(1, model.QueryViewCount);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenEdmxFileExists_AddsUpgradeConcerns()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var project = CreateProject(root);
            var edmxFile = Path.Combine(root, "LegacyModel.edmx");

            File.WriteAllText(edmxFile, CreateSampleEdmx());

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { project });

            var model = Assert.Single(report.Models);

            Assert.Contains(
                model.UpgradeConcerns,
                concern =>
                    concern.Severity == EdmxUpgradeConcernSeverity.High &&
                    concern.Concern == "EDMX model requires migration decision");

            Assert.Contains(
                model.UpgradeConcerns,
                concern =>
                    concern.Severity == EdmxUpgradeConcernSeverity.Medium &&
                    concern.Concern == "Stored procedure or function mapping requires review");

            Assert.Contains(
                model.UpgradeConcerns,
                concern =>
                    concern.Severity == EdmxUpgradeConcernSeverity.Medium &&
                    concern.Concern == "Query-backed model requires review");

            Assert.Contains(
                model.UpgradeConcerns,
                concern =>
                    concern.Severity == EdmxUpgradeConcernSeverity.Low &&
                    concern.Concern == "Complex types require manual mapping review");

            Assert.Contains(
                model.UpgradeConcerns,
                concern =>
                    concern.Severity == EdmxUpgradeConcernSeverity.Low &&
                    concern.Concern == "Designer metadata found");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenEdmxXmlIsMalformed_ReturnsParseConcernInsteadOfThrowing()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var project = CreateProject(root);
            var edmxFile = Path.Combine(root, "BrokenModel.edmx");

            File.WriteAllText(edmxFile, "<Edmx><Runtime>");

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { project });

            var model = Assert.Single(report.Models);

            Assert.Equal("SampleLegacyApp.Data", model.ProjectName);
            Assert.Equal(edmxFile, model.FilePath);
            Assert.False(model.HasConceptualModel);
            Assert.False(model.HasStorageModel);
            Assert.False(model.HasMappingModel);
            Assert.False(model.HasDesignerMetadata);
            Assert.False(string.IsNullOrWhiteSpace(model.ParseError));

            var concern = Assert.Single(model.UpgradeConcerns);

            Assert.Equal(EdmxUpgradeConcernSeverity.High, concern.Severity);
            Assert.Equal("EDMX file could not be parsed", concern.Concern);
            Assert.Contains("BrokenModel.edmx", concern.Evidence, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_AssociatesEdmxFileWithNearestProject()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var outerProjectDirectory = Path.Combine(root, "SampleLegacyApp.Data");
            var nestedProjectDirectory = Path.Combine(outerProjectDirectory, "Nested.Data");

            Directory.CreateDirectory(outerProjectDirectory);
            Directory.CreateDirectory(nestedProjectDirectory);

            var outerProject = new DiscoveredProject
            {
                Name = "SampleLegacyApp.Data",
                ProjectFilePath = Path.Combine(outerProjectDirectory, "SampleLegacyApp.Data.csproj"),
                TargetFramework = "net48"
            };

            var nestedProject = new DiscoveredProject
            {
                Name = "Nested.Data",
                ProjectFilePath = Path.Combine(nestedProjectDirectory, "Nested.Data.csproj"),
                TargetFramework = "net48"
            };

            var edmxFile = Path.Combine(nestedProjectDirectory, "NestedModel.edmx");

            File.WriteAllText(edmxFile, CreateMinimalEdmx());

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { outerProject, nestedProject });

            var model = Assert.Single(report.Models);

            Assert.Equal("Nested.Data", model.ProjectName);
            Assert.Equal(edmxFile, model.FilePath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenNoProjectEdmxFilesExist_FallsBackToScanPathDiscovery()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var projectDirectory = Path.Combine(root, "SampleLegacyApp.Data");
            var orphanDirectory = Path.Combine(root, "OrphanModels");

            Directory.CreateDirectory(projectDirectory);
            Directory.CreateDirectory(orphanDirectory);

            var project = new DiscoveredProject
            {
                Name = "SampleLegacyApp.Data",
                ProjectFilePath = Path.Combine(projectDirectory, "SampleLegacyApp.Data.csproj"),
                TargetFramework = "net48"
            };

            var edmxFile = Path.Combine(orphanDirectory, "OrphanModel.edmx");

            File.WriteAllText(edmxFile, CreateMinimalEdmx());

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { project });

            var model = Assert.Single(report.Models);

            Assert.Null(model.ProjectName);
            Assert.Equal(edmxFile, model.FilePath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenCompanionFilesExist_AddsCompanionFilesAndConcern()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var project = CreateProject(root);
            var edmxFile = Path.Combine(root, "LegacyModel.edmx");
            var t4File = Path.Combine(root, "LegacyModel.Context.tt");
            var designerFile = Path.Combine(root, "LegacyModel.Designer.cs");
            var contextFile = Path.Combine(root, "LegacyModel.Context.cs");

            File.WriteAllText(edmxFile, CreateMinimalEdmx());
            File.WriteAllText(t4File, "EntityFramework ObjectContext");
            File.WriteAllText(designerFile, "// generated designer code");
            File.WriteAllText(contextFile, "public sealed class LegacyModelContext { }");

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { project });

            var model = Assert.Single(report.Models);

            Assert.Equal(3, model.CompanionFiles.Count);

            Assert.Contains(
                model.CompanionFiles,
                file =>
                    file.Kind == "T4 template" &&
                    file.FilePath == t4File);

            Assert.Contains(
                model.CompanionFiles,
                file =>
                    file.Kind == "Designer generated code" &&
                    file.FilePath == designerFile);

            Assert.Contains(
                model.CompanionFiles,
                file =>
                    file.Kind == "Generated or related code" &&
                    file.FilePath == contextFile);

            Assert.Contains(
                model.UpgradeConcerns,
                concern =>
                    concern.Severity == EdmxUpgradeConcernSeverity.Low &&
                    concern.Concern == "Companion generated or design-time files require review");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_IgnoresEdmxFilesUnderBuildOutputFolders()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var binDirectory = Path.Combine(root, "bin", "Debug");
            Directory.CreateDirectory(binDirectory);

            File.WriteAllText(
                Path.Combine(binDirectory, "GeneratedModel.edmx"),
                CreateMinimalEdmx());

            var project = CreateProject(root);

            var analyzer = new EdmxAnalyzer();

            var report = analyzer.Analyze(root, new[] { project });

            Assert.Empty(report.Models);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static DiscoveredProject CreateProject(string root)
    {
        return new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = Path.Combine(root, "SampleLegacyApp.Data.csproj"),
            TargetFramework = "net48"
        };
    }

    private static string CreateMinimalEdmx()
    {
        return """
               <?xml version="1.0" encoding="utf-8"?>
               <edmx:Edmx Version="3.0" xmlns:edmx="http://schemas.microsoft.com/ado/2009/11/edmx">
                 <edmx:Runtime>
                   <edmx:ConceptualModels>
                     <Schema Namespace="Model" xmlns="http://schemas.microsoft.com/ado/2009/11/edm">
                       <EntityContainer Name="ModelContainer">
                         <EntitySet Name="Customers" EntityType="Model.Customer" />
                       </EntityContainer>
                       <EntityType Name="Customer">
                         <Key>
                           <PropertyRef Name="Id" />
                         </Key>
                         <Property Name="Id" Type="Int32" Nullable="false" />
                       </EntityType>
                     </Schema>
                   </edmx:ConceptualModels>
                 </edmx:Runtime>
               </edmx:Edmx>
               """;
    }

    private static string CreateSampleEdmx()
    {
        return """
               <?xml version="1.0" encoding="utf-8"?>
               <edmx:Edmx Version="3.0" xmlns:edmx="http://schemas.microsoft.com/ado/2009/11/edmx">
                 <edmx:Runtime>
                   <edmx:ConceptualModels>
                     <Schema Namespace="Model" Alias="Self" xmlns="http://schemas.microsoft.com/ado/2009/11/edm">
                       <EntityContainer Name="ModelContainer">
                         <EntitySet Name="Customers" EntityType="Model.Customer" />
                         <FunctionImport Name="GetCustomers" ReturnType="Collection(Model.Customer)" />
                       </EntityContainer>

                       <EntityType Name="Customer">
                         <Key>
                           <PropertyRef Name="Id" />
                         </Key>
                         <Property Name="Id" Type="Int32" Nullable="false" />
                         <Property Name="Name" Type="String" MaxLength="100" />
                         <NavigationProperty Name="Orders" Relationship="Model.FK_Customer_Order" FromRole="Customer" ToRole="Orders" />
                       </EntityType>

                       <ComplexType Name="Address">
                         <Property Name="Line1" Type="String" />
                       </ComplexType>

                       <Association Name="FK_Customer_Order">
                         <End Role="Customer" Type="Model.Customer" Multiplicity="1" />
                         <End Role="Orders" Type="Model.Order" Multiplicity="*" />
                       </Association>
                     </Schema>
                   </edmx:ConceptualModels>

                   <edmx:StorageModels>
                     <Schema Namespace="Store" Provider="System.Data.SqlClient" ProviderManifestToken="2008" xmlns="http://schemas.microsoft.com/ado/2009/11/edm/ssdl">
                       <EntityContainer Name="StoreContainer">
                         <EntitySet Name="CustomerTables" EntityType="Store.CustomerTable" Schema="dbo" Table="Customers" />
                       </EntityContainer>

                       <EntityType Name="CustomerTable">
                         <Key>
                           <PropertyRef Name="Id" />
                         </Key>
                         <Property Name="Id" Type="int" Nullable="false" />
                         <Property Name="Name" Type="nvarchar" MaxLength="100" />
                         <DefiningQuery>
                           SELECT Id, Name FROM dbo.Customers
                         </DefiningQuery>
                       </EntityType>

                       <Function Name="GetCustomers" Schema="dbo" IsComposable="false">
                         <Parameter Name="id" Type="int" Mode="In" />
                       </Function>
                     </Schema>
                   </edmx:StorageModels>

                   <edmx:Mappings>
                     <Mapping Space="C-S" xmlns="http://schemas.microsoft.com/ado/2009/11/mapping/cs">
                       <EntityContainerMapping StorageEntityContainer="StoreContainer" CdmEntityContainer="ModelContainer">
                         <EntitySetMapping Name="Customers">
                           <EntityTypeMapping TypeName="Model.Customer">
                             <MappingFragment StoreEntitySet="CustomerTables">
                               <ScalarProperty Name="Id" ColumnName="Id" />
                               <ScalarProperty Name="Name" ColumnName="Name" />
                             </MappingFragment>
                           </EntityTypeMapping>
                         </EntitySetMapping>

                         <FunctionImportMapping FunctionImportName="GetCustomers" FunctionName="Store.GetCustomers" />

                         <ModificationFunctionMapping>
                           <InsertFunction FunctionName="Store.InsertCustomer" />
                         </ModificationFunctionMapping>

                         <QueryView>
                           SELECT VALUE Model.Customer FROM ModelContainer.Customers AS Customer
                         </QueryView>
                       </EntityContainerMapping>
                     </Mapping>
                   </edmx:Mappings>
                 </edmx:Runtime>

                 <edmx:Designer>
                   <edmx:Connection>
                     <DesignerInfoPropertySet />
                   </edmx:Connection>
                   <edmx:Options>
                     <DesignerInfoPropertySet />
                   </edmx:Options>
                   <edmx:Diagrams>
                     <Diagram Name="ModelDiagram" />
                   </edmx:Diagrams>
                 </edmx:Designer>
               </edmx:Edmx>
               """;
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Best effort cleanup only. The test result should not depend on temp directory deletion.
        }
    }
}