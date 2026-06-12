using System.Xml.Linq;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;

namespace LegacyLens.Core.Analysis;

public sealed class EdmxAnalyzer
{
    public EdmxAnalysisReport Analyze(
        string scanPath,
        IReadOnlyCollection<DiscoveredProject> projects)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scanPath);
        ArgumentNullException.ThrowIfNull(projects);

        var inventory = new ScanFileInventoryBuilder().Build(projects);

        return Analyze(
            scanPath,
            projects,
            inventory);
    }

    public EdmxAnalysisReport Analyze(
        string scanPath,
        IReadOnlyCollection<DiscoveredProject> projects,
        ScanFileInventory fileInventory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scanPath);
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(fileInventory);

        var edmxFiles = DiscoverEdmxFiles(scanPath, projects, fileInventory);

        var models = edmxFiles
            .Select(file => AnalyzeFile(file, FindProjectForEdmxFile(file, projects, fileInventory), fileInventory))
            .OrderBy(model => model.ProjectName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(model => model.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new EdmxAnalysisReport(models);
    }

    private static IReadOnlyList<string> DiscoverEdmxFiles(
        string scanPath,
        IEnumerable<DiscoveredProject> projects,
        ScanFileInventory fileInventory)
    {
        var files = fileInventory.EdmxFiles
            .Select(file => file.FullPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (files.Count == 0)
        {
            foreach (var file in DiscoverEdmxFiles(scanPath, projects))
            {
                files.Add(file);
            }
        }

        return files
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> DiscoverEdmxFiles(
        string scanPath,
        IEnumerable<DiscoveredProject> projects)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects)
        {
            var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);

            if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory))
            {
                continue;
            }

            foreach (var file in SafeEnumerateFiles(projectDirectory, "*.edmx"))
            {
                files.Add(file);
            }
        }

        if (files.Count == 0 && Directory.Exists(scanPath))
        {
            foreach (var file in SafeEnumerateFiles(scanPath, "*.edmx"))
            {
                files.Add(file);
            }
        }

        return files
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DiscoveredEdmxModel AnalyzeFile(
        string edmxFile,
        DiscoveredProject? project,
        ScanFileInventory? fileInventory = null)
    {
        try
        {
            var document = XDocument.Load(edmxFile, LoadOptions.None);
            var root = document.Root;

            if (root is null)
            {
                return CreateUnreadableModel(
                    edmxFile,
                    project,
                    "EDMX XML document has no root element.",
                    fileInventory);
            }

            var namespaces = root
                .DescendantsAndSelf()
                .Select(element => element.Name.NamespaceName)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var conceptualSchemas = GetSectionSchemas(document, "ConceptualModels");
            var storageSchemas = GetSectionSchemas(document, "StorageModels");
            var mappingRoots = GetSectionElements(document, "Mappings");
            var designerRoots = GetElements(document, "Designer").ToArray();

            var conceptualEntitySetLookup = ExtractConceptualEntitySets(conceptualSchemas);
            var conceptualEntities = ExtractConceptualEntities(conceptualSchemas, conceptualEntitySetLookup);

            var conceptualEntitySets = conceptualEntitySetLookup
                .Values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var complexTypes = conceptualSchemas
                .SelectMany(schema => DirectChildren(schema, "ComplexType"))
                .Select(type => GetAttribute(type, "Name"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray()
                .AsReadOnly();

            var storageEntitySets = ExtractStorageEntitySets(storageSchemas);
            var storageEntities = ExtractStorageEntities(storageSchemas, storageEntitySets);
            var associations = ExtractAssociations(conceptualSchemas, storageSchemas);
            var functionImports = ExtractFunctionImports(conceptualSchemas, mappingRoots);
            var storeFunctions = ExtractStoreFunctions(storageSchemas);
            var mappingFragments = ExtractMappingFragments(mappingRoots);
            var companionFiles = ExtractCompanionFiles(edmxFile, fileInventory);

            var modificationFunctionMappingCount = mappingRoots
                .SelectMany(rootElement => GetElements(rootElement, "ModificationFunctionMapping"))
                .Count();

            var queryViewCount = mappingRoots
                .SelectMany(rootElement => GetElements(rootElement, "QueryView"))
                .Count();

            var definingQueryCount = storageSchemas
                .SelectMany(schema => GetElements(schema, "DefiningQuery"))
                .Count();

            var hasConceptualModel = conceptualSchemas.Length > 0;
            var hasStorageModel = storageSchemas.Length > 0;
            var hasMappingModel = mappingRoots.Length > 0;
            var hasDesignerMetadata = designerRoots.Length > 0;

            var concerns = CreateUpgradeConcerns(
                Path.GetFileName(edmxFile),
                hasConceptualModel,
                hasStorageModel,
                hasMappingModel,
                hasDesignerMetadata,
                complexTypes.Count,
                functionImports.Count,
                storeFunctions.Count,
                modificationFunctionMappingCount,
                queryViewCount,
                definingQueryCount,
                companionFiles.Count,
                ParseError: null);

            return new DiscoveredEdmxModel(
                project?.Name,
                edmxFile,
                hasConceptualModel,
                hasStorageModel,
                hasMappingModel,
                hasDesignerMetadata,
                ParseError: null,
                namespaces,
                conceptualEntities,
                conceptualEntitySets,
                complexTypes,
                storageEntities,
                associations,
                functionImports,
                storeFunctions,
                mappingFragments,
                companionFiles,
                concerns,
                modificationFunctionMappingCount,
                queryViewCount,
                definingQueryCount);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return CreateUnreadableModel(
                edmxFile,
                project,
                exception.Message,
                fileInventory);
        }
    }

    private static DiscoveredEdmxModel CreateUnreadableModel(
        string edmxFile,
        DiscoveredProject? project,
        string parseError,
        ScanFileInventory? fileInventory = null)
    {
        var companionFiles = ExtractCompanionFiles(edmxFile, fileInventory);

        var concerns = CreateUpgradeConcerns(
            Path.GetFileName(edmxFile),
            HasConceptualModel: false,
            HasStorageModel: false,
            HasMappingModel: false,
            HasDesignerMetadata: false,
            ComplexTypeCount: 0,
            FunctionImportCount: 0,
            StoreFunctionCount: 0,
            ModificationFunctionMappingCount: 0,
            QueryViewCount: 0,
            DefiningQueryCount: 0,
            CompanionFileCount: companionFiles.Count,
            parseError);

        return new DiscoveredEdmxModel(
            project?.Name,
            edmxFile,
            HasConceptualModel: false,
            HasStorageModel: false,
            HasMappingModel: false,
            HasDesignerMetadata: false,
            parseError,
            Array.Empty<string>(),
            Array.Empty<EdmxConceptualEntity>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<EdmxStorageEntity>(),
            Array.Empty<EdmxAssociation>(),
            Array.Empty<EdmxFunctionImport>(),
            Array.Empty<EdmxStoreFunction>(),
            Array.Empty<EdmxMappingFragment>(),
            companionFiles,
            concerns,
            ModificationFunctionMappingCount: 0,
            QueryViewCount: 0,
            DefiningQueryCount: 0);
    }

    private static IReadOnlyList<EdmxUpgradeConcern> CreateUpgradeConcerns(
        string edmxFileName,
        bool HasConceptualModel,
        bool HasStorageModel,
        bool HasMappingModel,
        bool HasDesignerMetadata,
        int ComplexTypeCount,
        int FunctionImportCount,
        int StoreFunctionCount,
        int ModificationFunctionMappingCount,
        int QueryViewCount,
        int DefiningQueryCount,
        int CompanionFileCount,
        string? ParseError)
    {
        var concerns = new List<EdmxUpgradeConcern>();

        if (!string.IsNullOrWhiteSpace(ParseError))
        {
            concerns.Add(new EdmxUpgradeConcern(
                EdmxUpgradeConcernSeverity.High,
                "EDMX file could not be parsed",
                $"{edmxFileName} could not be parsed as XML: {ParseError}",
                "Review the EDMX file manually. Static analysis could not inspect model details for this file."));

            return concerns;
        }

        concerns.Add(new EdmxUpgradeConcern(
            EdmxUpgradeConcernSeverity.High,
            "EDMX model requires migration decision",
            $"{edmxFileName} is an Entity Framework EDMX model. No direct EF Core EDMX equivalent exists.",
            "Review whether to scaffold a new EF Core model from the database, keep EF6 isolated, or manually map equivalent entities and relationships."));

        if (HasConceptualModel && HasStorageModel && HasMappingModel)
        {
            concerns.Add(new EdmxUpgradeConcern(
                EdmxUpgradeConcernSeverity.Medium,
                "Conceptual, storage, and mapping sections require review",
                $"{edmxFileName} contains CSDL, SSDL, and MSL sections.",
                "Review entity-to-table mappings, keys, relationships, and generated code before choosing an EF Core migration approach."));
        }

        if (FunctionImportCount > 0 || StoreFunctionCount > 0 || ModificationFunctionMappingCount > 0)
        {
            concerns.Add(new EdmxUpgradeConcern(
                EdmxUpgradeConcernSeverity.Medium,
                "Stored procedure or function mapping requires review",
                "Function imports, store functions, or modification function mappings were found.",
                "Review stored procedure usage and decide whether EF Core stored procedure support, raw SQL, or explicit repository methods are needed."));
        }

        if (QueryViewCount > 0 || DefiningQueryCount > 0)
        {
            concerns.Add(new EdmxUpgradeConcern(
                EdmxUpgradeConcernSeverity.Medium,
                "Query-backed model requires review",
                "QueryView or DefiningQuery evidence was found.",
                "Review whether keyless entity types, database views, raw SQL, or rewritten queries are needed."));
        }

        if (ComplexTypeCount > 0)
        {
            concerns.Add(new EdmxUpgradeConcern(
                EdmxUpgradeConcernSeverity.Low,
                "Complex types require manual mapping review",
                $"{ComplexTypeCount} complex type(s) found.",
                "Review whether EF Core owned entity types or explicit value-object mappings are appropriate."));
        }

        if (HasDesignerMetadata)
        {
            concerns.Add(new EdmxUpgradeConcern(
                EdmxUpgradeConcernSeverity.Low,
                "Designer metadata found",
                "EDMX designer metadata was found.",
                "Designer layout information is not a runtime model but may confirm visual model maintenance history."));
        }

        if (CompanionFileCount > 0)
        {
            concerns.Add(new EdmxUpgradeConcern(
                EdmxUpgradeConcernSeverity.Low,
                "Companion generated or design-time files require review",
                $"{CompanionFileCount} companion file(s) were found near the EDMX file.",
                "Review generated context, entity, and T4 files to understand compile-time dependencies before migration."));
        }

        return concerns;
    }

    private static IReadOnlyList<EdmxConceptualEntity> ExtractConceptualEntities(
        IEnumerable<XElement> conceptualSchemas,
        IReadOnlyDictionary<string, string> entitySets)
    {
        return conceptualSchemas
            .SelectMany(schema => DirectChildren(schema, "EntityType"))
            .Select(entity =>
            {
                var name = GetAttribute(entity, "Name") ?? "Unknown";
                var keys = GetElements(entity, "Key")
                    .SelectMany(key => DirectChildren(key, "PropertyRef"))
                    .Select(propertyRef => GetAttribute(propertyRef, "Name"))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var propertyCount = DirectChildren(entity, "Property").Count();
                var navigationPropertyCount = DirectChildren(entity, "NavigationProperty").Count();

                return new EdmxConceptualEntity(
                    name,
                    entitySets.TryGetValue(name, out var entitySet) ? entitySet : null,
                    keys,
                    propertyCount,
                    navigationPropertyCount);
            })
            .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> ExtractConceptualEntitySets(IEnumerable<XElement> conceptualSchemas)
    {
        var entitySets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entitySet in conceptualSchemas
                     .SelectMany(schema => GetElements(schema, "EntityContainer"))
                     .SelectMany(container => DirectChildren(container, "EntitySet")))
        {
            var name = GetAttribute(entitySet, "Name");
            var entityType = GetLastNameSegment(GetAttribute(entitySet, "EntityType"));

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(entityType))
            {
                entitySets[entityType] = name;
            }
        }

        return entitySets;
    }

    private static IReadOnlyDictionary<string, StorageEntitySetInfo> ExtractStorageEntitySets(IEnumerable<XElement> storageSchemas)
    {
        var entitySets = new Dictionary<string, StorageEntitySetInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var entitySet in storageSchemas
                     .SelectMany(schema => GetElements(schema, "EntityContainer"))
                     .SelectMany(container => DirectChildren(container, "EntitySet")))
        {
            var name = GetAttribute(entitySet, "Name");

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            entitySets[name] = new StorageEntitySetInfo(
                name,
                GetLastNameSegment(GetAttribute(entitySet, "EntityType")),
                GetAttribute(entitySet, "Schema"),
                FirstNonWhiteSpace(GetAttribute(entitySet, "Table"), GetAttribute(entitySet, "Name")));
        }

        return entitySets;
    }

    private static IReadOnlyList<EdmxStorageEntity> ExtractStorageEntities(
        IEnumerable<XElement> storageSchemas,
        IReadOnlyDictionary<string, StorageEntitySetInfo> entitySets)
    {
        return storageSchemas
            .SelectMany(schema => DirectChildren(schema, "EntityType"))
            .Select(entity =>
            {
                var name = GetAttribute(entity, "Name") ?? "Unknown";
                var entitySet = entitySets.Values.FirstOrDefault(value =>
                    string.Equals(value.EntityType, name, StringComparison.OrdinalIgnoreCase));

                return new EdmxStorageEntity(
                    name,
                    entitySet?.Name,
                    entitySet?.Schema,
                    entitySet?.TableOrView,
                    DirectChildren(entity, "Property").Count(),
                    GetElements(entity, "DefiningQuery").Any());
            })
            .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<EdmxAssociation> ExtractAssociations(params IEnumerable<XElement>[] schemaSets)
    {
        return schemaSets
            .SelectMany(schemas => schemas)
            .SelectMany(schema => DirectChildren(schema, "Association"))
            .Select(association =>
            {
                var ends = DirectChildren(association, "End").ToArray();
                var from = ends.ElementAtOrDefault(0);
                var to = ends.ElementAtOrDefault(1);
                var fromRole = FirstNonWhiteSpace(GetAttribute(from, "Role"), GetLastNameSegment(GetAttribute(from, "Type")));
                var toRole = FirstNonWhiteSpace(GetAttribute(to, "Role"), GetLastNameSegment(GetAttribute(to, "Type")));
                var multiplicity = string.Join(" to ", new[]
                    {
                        GetAttribute(from, "Multiplicity"),
                        GetAttribute(to, "Multiplicity")
                    }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

                return new EdmxAssociation(
                    GetAttribute(association, "Name") ?? "Unknown",
                    fromRole,
                    toRole,
                    string.IsNullOrWhiteSpace(multiplicity) ? null : multiplicity);
            })
            .OrderBy(association => association.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<EdmxFunctionImport> ExtractFunctionImports(
        IEnumerable<XElement> conceptualSchemas,
        IEnumerable<XElement> mappingRoots)
    {
        var mappedFunctions = mappingRoots
            .SelectMany(root => GetElements(root, "FunctionImportMapping"))
            .Select(mapping => new
            {
                FunctionImportName = GetAttribute(mapping, "FunctionImportName"),
                FunctionName = GetLastNameSegment(GetAttribute(mapping, "FunctionName"))
            })
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.FunctionImportName))
            .GroupBy(mapping => mapping.FunctionImportName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().FunctionName, StringComparer.OrdinalIgnoreCase);

        return conceptualSchemas
            .SelectMany(schema => GetElements(schema, "EntityContainer"))
            .SelectMany(container => DirectChildren(container, "FunctionImport"))
            .Select(functionImport =>
            {
                var name = GetAttribute(functionImport, "Name") ?? "Unknown";

                return new EdmxFunctionImport(
                    name,
                    GetAttribute(functionImport, "ReturnType"),
                    mappedFunctions.TryGetValue(name, out var storeFunction) ? storeFunction : null);
            })
            .OrderBy(functionImport => functionImport.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<EdmxStoreFunction> ExtractStoreFunctions(IEnumerable<XElement> storageSchemas)
    {
        return storageSchemas
            .SelectMany(schema => DirectChildren(schema, "Function"))
            .Select(function => new EdmxStoreFunction(
                GetAttribute(function, "Name") ?? "Unknown",
                GetAttribute(function, "Schema"),
                TryParseBool(GetAttribute(function, "IsComposable")),
                DirectChildren(function, "Parameter").Count()))
            .OrderBy(function => function.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<EdmxMappingFragment> ExtractMappingFragments(IEnumerable<XElement> mappingRoots)
    {
        var fragments = new List<EdmxMappingFragment>();

        foreach (var entitySetMapping in mappingRoots.SelectMany(root => GetElements(root, "EntitySetMapping")))
        {
            var entitySet = GetAttribute(entitySetMapping, "Name");

            foreach (var entityTypeMapping in GetElements(entitySetMapping, "EntityTypeMapping"))
            {
                var entityType = GetLastNameSegment(FirstNonWhiteSpace(
                    GetAttribute(entityTypeMapping, "TypeName"),
                    GetAttribute(entityTypeMapping, "IsTypeOf")));

                foreach (var mappingFragment in DirectChildren(entityTypeMapping, "MappingFragment"))
                {
                    fragments.Add(new EdmxMappingFragment(
                        entitySet,
                        entityType,
                        GetAttribute(mappingFragment, "StoreEntitySet"),
                        DirectChildren(mappingFragment, "ScalarProperty").Count()));
                }
            }
        }

        return fragments
            .OrderBy(fragment => fragment.EntitySet ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(fragment => fragment.EntityType ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(fragment => fragment.StoreEntitySet ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<EdmxCompanionFile> ExtractCompanionFiles(
        string edmxFile,
        ScanFileInventory? fileInventory = null)
    {
        if (fileInventory is not null)
        {
            var companionFiles = ExtractCompanionFilesFromInventory(edmxFile, fileInventory);

            if (companionFiles.Count > 0)
            {
                return companionFiles;
            }
        }

        return ExtractCompanionFilesFromFileSystem(edmxFile);
    }

    private static IReadOnlyList<EdmxCompanionFile> ExtractCompanionFilesFromInventory(
        string edmxFile,
        ScanFileInventory fileInventory)
    {
        var directory = Path.GetDirectoryName(edmxFile);
        var baseName = Path.GetFileNameWithoutExtension(edmxFile);

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(baseName))
        {
            return Array.Empty<EdmxCompanionFile>();
        }

        var companions = new List<EdmxCompanionFile>();

        foreach (var file in fileInventory.T4Files.Where(file => IsCompanionCandidateDirectory(file.FullPath, directory)))
        {
            if (Path.GetFileNameWithoutExtension(file.FullPath).Contains(baseName, StringComparison.OrdinalIgnoreCase) ||
                FileLooksRelatedToEdmx(file))
            {
                companions.Add(new EdmxCompanionFile(
                    "T4 template",
                    file.FullPath,
                    "T4 template found near EDMX file."));
            }
        }

        foreach (var file in fileInventory.CSharpFiles.Where(file => IsCompanionCandidateDirectory(file.FullPath, directory)))
        {
            if (Path.GetFileName(file.FullPath).EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            {
                if (Path.GetFileNameWithoutExtension(file.FullPath).Contains(baseName, StringComparison.OrdinalIgnoreCase))
                {
                    companions.Add(new EdmxCompanionFile(
                        "Designer generated code",
                        file.FullPath,
                        ".Designer.cs file found near EDMX file."));
                }

                continue;
            }

            if (Path.GetFileNameWithoutExtension(file.FullPath).Contains(baseName, StringComparison.OrdinalIgnoreCase) ||
                FileLooksRelatedToEdmx(file))
            {
                companions.Add(new EdmxCompanionFile(
                    "Generated or related code",
                    file.FullPath,
                    "C# file near EDMX file appears related to generated model or context code."));
            }
        }

        return SortCompanionFiles(companions);
    }

    private static IReadOnlyList<EdmxCompanionFile> ExtractCompanionFilesFromFileSystem(string edmxFile)
    {
        var directory = Path.GetDirectoryName(edmxFile);
        var baseName = Path.GetFileNameWithoutExtension(edmxFile);

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(baseName) || !Directory.Exists(directory))
        {
            return Array.Empty<EdmxCompanionFile>();
        }

        var companions = new List<EdmxCompanionFile>();

        foreach (var file in SafeEnumerateFiles(directory, "*.tt", SearchOption.TopDirectoryOnly))
        {
            if (Path.GetFileNameWithoutExtension(file).Contains(baseName, StringComparison.OrdinalIgnoreCase) ||
                FileLooksRelatedToEdmx(file))
            {
                companions.Add(new EdmxCompanionFile(
                    "T4 template",
                    file,
                    "T4 template found near EDMX file."));
            }
        }

        foreach (var file in SafeEnumerateFiles(directory, "*.Designer.cs", SearchOption.TopDirectoryOnly))
        {
            if (Path.GetFileNameWithoutExtension(file).Contains(baseName, StringComparison.OrdinalIgnoreCase))
            {
                companions.Add(new EdmxCompanionFile(
                    "Designer generated code",
                    file,
                    ".Designer.cs file found near EDMX file."));
            }
        }

        foreach (var file in SafeEnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly))
        {
            if (Path.GetFileName(file).EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Path.GetFileNameWithoutExtension(file).Contains(baseName, StringComparison.OrdinalIgnoreCase) ||
                FileLooksRelatedToEdmx(file))
            {
                companions.Add(new EdmxCompanionFile(
                    "Generated or related code",
                    file,
                    "C# file near EDMX file appears related to generated model or context code."));
            }
        }

        return SortCompanionFiles(companions);
    }

    private static IReadOnlyList<EdmxCompanionFile> SortCompanionFiles(IEnumerable<EdmxCompanionFile> companionFiles)
    {
        return companionFiles
            .GroupBy(file => string.Join("|", file.Kind, file.FilePath), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(file => file.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(file => file.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsCompanionCandidateDirectory(string filePath, string edmxDirectory)
    {
        var fileDirectory = Path.GetDirectoryName(filePath);

        return !string.IsNullOrWhiteSpace(fileDirectory) &&
               fileDirectory.Equals(edmxDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static bool FileLooksRelatedToEdmx(ScanFile file)
    {
        var fileName = Path.GetFileName(file.FullPath);

        if (fileName.Contains("Context", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Model", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Entity", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return file.Content.Contains("ObjectContext", StringComparison.OrdinalIgnoreCase) ||
               file.Content.Contains("EntityObject", StringComparison.OrdinalIgnoreCase) ||
               file.Content.Contains("DbSet", StringComparison.OrdinalIgnoreCase) ||
               file.Content.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase);
    }

    private static bool FileLooksRelatedToEdmx(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        if (fileName.Contains("Context", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Model", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Entity", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var text = SafeReadAllText(filePath);

        return text.Contains("ObjectContext", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("EntityObject", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("DbSet", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase);
    }

    private static XElement[] GetSectionSchemas(XDocument document, string sectionName)
    {
        return GetSectionElements(document, sectionName)
            .SelectMany(section => DirectChildren(section, "Schema"))
            .ToArray();
    }

    private static XElement[] GetSectionElements(XDocument document, string sectionName)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName.Equals(sectionName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static IEnumerable<XElement> GetElements(XContainer container, string localName)
    {
        return container
            .Descendants()
            .Where(element => element.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<XElement> DirectChildren(XContainer container, string localName)
    {
        return container
            .Elements()
            .Where(element => element.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetAttribute(XElement? element, string localName)
    {
        return element?
            .Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private static DiscoveredProject? FindProjectForEdmxFile(
        string filePath,
        IEnumerable<DiscoveredProject> projects,
        ScanFileInventory fileInventory)
    {
        var nearestProject = FindNearestProject(filePath, projects);

        if (nearestProject is not null)
        {
            return nearestProject;
        }

        var inventoryFile = fileInventory.EdmxFiles.FirstOrDefault(file =>
            file.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

        if (inventoryFile is null)
        {
            return null;
        }

        return new DiscoveredProject
        {
            Name = inventoryFile.ProjectName,
            ProjectFilePath = inventoryFile.ProjectFilePath
        };
    }

    private static DiscoveredProject? FindNearestProject(string filePath, IEnumerable<DiscoveredProject> projects)
    {
        return projects
            .Select(project => new
            {
                Project = project,
                Directory = Path.GetDirectoryName(project.ProjectFilePath)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Directory))
            .Where(item => IsPathUnderDirectory(filePath, item.Directory!))
            .OrderByDescending(item => item.Directory!.Length)
            .Select(item => item.Project)
            .FirstOrDefault();
    }

    private static bool IsPathUnderDirectory(string filePath, string directory)
    {
        var fullFilePath = Path.GetFullPath(filePath);
        var fullDirectory = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return fullFilePath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SafeEnumerateFiles(
        string directory,
        string searchPattern,
        SearchOption searchOption = SearchOption.AllDirectories)
    {
        try
        {
            return Directory
                .EnumerateFiles(directory, searchPattern, searchOption)
                .Where(path => !IsBuildOutputPath(path))
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static bool IsBuildOutputPath(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return parts.Any(part =>
            part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static string SafeReadAllText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? FirstNonWhiteSpace(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetLastNameSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var index = value.LastIndexOf(".", StringComparison.Ordinal);

        return index < 0
            ? value
            : value[(index + 1)..];
    }

    private static bool? TryParseBool(string? value)
    {
        return bool.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    private sealed record StorageEntitySetInfo(
        string Name,
        string? EntityType,
        string? Schema,
        string? TableOrView);
}