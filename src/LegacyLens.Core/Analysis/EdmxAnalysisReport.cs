namespace LegacyLens.Core.Analysis;

public sealed record EdmxAnalysisReport(IReadOnlyList<DiscoveredEdmxModel> Models)
{
    public int EdmxFileCount => Models.Count;
    public int ConceptualEntityCount => Models.Sum(model => model.ConceptualEntities.Count);
    public int ConceptualEntitySetCount => Models.Sum(model => model.ConceptualEntitySets.Count);
    public int StorageEntityCount => Models.Sum(model => model.StorageEntities.Count);
    public int AssociationCount => Models.Sum(model => model.Associations.Count);
    public int NavigationPropertyCount => Models.Sum(model => model.ConceptualEntities.Sum(entity => entity.NavigationPropertyCount));
    public int ComplexTypeCount => Models.Sum(model => model.ComplexTypes.Count);
    public int FunctionImportCount => Models.Sum(model => model.FunctionImports.Count);
    public int StoreFunctionCount => Models.Sum(model => model.StoreFunctions.Count);
    public int MappingFragmentCount => Models.Sum(model => model.MappingFragments.Count);
    public int ModificationFunctionMappingCount => Models.Sum(model => model.ModificationFunctionMappingCount);
    public int QueryViewCount => Models.Sum(model => model.QueryViewCount);
    public int DefiningQueryCount => Models.Sum(model => model.DefiningQueryCount);
    public int UpgradeConcernCount => Models.Sum(model => model.UpgradeConcerns.Count);
}

public sealed record DiscoveredEdmxModel(
    string? ProjectName,
    string FilePath,
    bool HasConceptualModel,
    bool HasStorageModel,
    bool HasMappingModel,
    bool HasDesignerMetadata,
    string? ParseError,
    IReadOnlyList<string> NamespaceUris,
    IReadOnlyList<EdmxConceptualEntity> ConceptualEntities,
    IReadOnlyList<string> ConceptualEntitySets,
    IReadOnlyList<string> ComplexTypes,
    IReadOnlyList<EdmxStorageEntity> StorageEntities,
    IReadOnlyList<EdmxAssociation> Associations,
    IReadOnlyList<EdmxFunctionImport> FunctionImports,
    IReadOnlyList<EdmxStoreFunction> StoreFunctions,
    IReadOnlyList<EdmxMappingFragment> MappingFragments,
    IReadOnlyList<EdmxCompanionFile> CompanionFiles,
    IReadOnlyList<EdmxUpgradeConcern> UpgradeConcerns,
    int ModificationFunctionMappingCount,
    int QueryViewCount,
    int DefiningQueryCount);

public sealed record EdmxConceptualEntity(
    string Name,
    string? EntitySet,
    IReadOnlyList<string> KeyProperties,
    int PropertyCount,
    int NavigationPropertyCount);

public sealed record EdmxStorageEntity(
    string Name,
    string? EntitySet,
    string? Schema,
    string? TableOrView,
    int ColumnCount,
    bool HasDefiningQuery);

public sealed record EdmxAssociation(
    string Name,
    string? FromRole,
    string? ToRole,
    string? Multiplicity);

public sealed record EdmxFunctionImport(
    string Name,
    string? ReturnType,
    string? StoreFunction);

public sealed record EdmxStoreFunction(
    string Name,
    string? Schema,
    bool? IsComposable,
    int ParameterCount);

public sealed record EdmxMappingFragment(
    string? EntitySet,
    string? EntityType,
    string? StoreEntitySet,
    int ScalarPropertyCount);

public sealed record EdmxCompanionFile(
    string Kind,
    string FilePath,
    string Evidence);

public sealed record EdmxUpgradeConcern(
    EdmxUpgradeConcernSeverity Severity,
    string Concern,
    string Evidence,
    string Recommendation);

public enum EdmxUpgradeConcernSeverity
{
    High,
    Medium,
    Low
}