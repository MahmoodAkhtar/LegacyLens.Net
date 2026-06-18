namespace LegacyLens.Core.Analysis;

public sealed record InterfaceInventoryReport(
    IReadOnlyList<InterfaceDefinition> Interfaces,
    IReadOnlyList<InterfaceImplementation> Implementations,
    IReadOnlyList<InterfaceConsumer> Consumers,
    IReadOnlyList<InterfaceRegistrationEvidence> Registrations,
    IReadOnlyList<InterfaceInventoryFinding> Findings,
    int SourceFileCount,
    int ConfigurationFileCount)
{
    public int MultipleImplementationInterfaceCount => Interfaces.Count(interfaceDefinition =>
        Implementations.Count(implementation =>
            implementation.InterfaceName.Equals(interfaceDefinition.Name, StringComparison.OrdinalIgnoreCase)) > 1);

    public int MissingStaticImplementationCount => Interfaces.Count(interfaceDefinition =>
        !Implementations.Any(implementation =>
            implementation.InterfaceName.Equals(interfaceDefinition.Name, StringComparison.OrdinalIgnoreCase)));

    public int MissingStaticConsumerCount => Interfaces.Count(interfaceDefinition =>
        !Consumers.Any(consumer =>
            consumer.InterfaceName.Equals(interfaceDefinition.Name, StringComparison.OrdinalIgnoreCase)));

    public int DynamicOrConfigurationDrivenWiringCount => Registrations.Count(registration =>
        registration.RequiresReview);
}

public sealed record InterfaceDefinition(
    string ProjectName,
    string SourcePath,
    int LineNumber,
    string Name,
    string FullName,
    IReadOnlyList<string> InheritedInterfaces,
    string LikelyRole,
    bool IsPossibleExtensionPoint);

public sealed record InterfaceImplementation(
    string ProjectName,
    string SourcePath,
    int LineNumber,
    string InterfaceName,
    string ImplementationType,
    string Evidence);

public sealed record InterfaceConsumer(
    string ProjectName,
    string SourcePath,
    int LineNumber,
    string InterfaceName,
    string ConsumerType,
    InterfaceConsumerKind Kind,
    string Evidence);

public enum InterfaceConsumerKind
{
    ConstructorParameter,
    Field,
    Property,
    MethodParameter,
    ReturnType,
    LocalVariable,
    GenericOrCollectionUsage,
    EndpointDelegateParameter,
    ServiceLocator,
    FactoryOrResolver
}

public sealed record InterfaceRegistrationEvidence(
    string ProjectName,
    string SourcePath,
    int LineNumber,
    string InterfaceName,
    string? ImplementationType,
    InterfaceRegistrationKind Kind,
    string Evidence,
    bool RequiresReview,
    string Notes);

public enum InterfaceRegistrationKind
{
    MicrosoftDependencyInjection,
    Autofac,
    CastleWindsor,
    Ninject,
    Unity,
    StructureMap,
    SimpleInjector,
    LightInject,
    Lamar,
    CommonServiceLocator,
    AspNetDependencyResolver,
    SpringNetXml,
    CastleWindsorXml,
    UnityXml,
    EnterpriseLibraryObjectBuilder,
    CustomObjectFactory,
    UnknownDynamicWiring
}

public sealed record InterfaceInventoryFinding(
    InterfaceInventoryFindingSeverity Severity,
    string InterfaceName,
    string Finding,
    string Evidence,
    string Recommendation);

public enum InterfaceInventoryFindingSeverity
{
    Info,
    Warning,
    Review
}
