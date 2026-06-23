namespace LegacyLens.Core.Analysis;

public sealed record ClassRefactoringOpportunitiesReport(
    string RequestedTypeName,
    DateTimeOffset GeneratedLocal,
    DateTimeOffset GeneratedUtc,
    int SourceFileCount,
    int DiscoveredTypeCount,
    IReadOnlyList<ClassRefactoringTypeMatch> MatchingTypes,
    ClassRefactoringProfile? Profile,
    IReadOnlyList<RefactoringSignal> Signals,
    IReadOnlyList<ExistingSeam> ExistingSeams,
    IReadOnlyList<MissingOrWeakSeam> MissingOrWeakSeams,
    IReadOnlyList<TestabilityBarrier> TestabilityBarriers,
    IReadOnlyList<CharacterizationTestTarget> CharacterizationTestTargets,
    IReadOnlyList<TechniqueRecommendation> TechniqueRecommendations,
    IReadOnlyList<TechniqueRecommendation> NotRecommendedTechniques,
    IReadOnlyList<SuggestedRefactoringStep> SuggestedSteps)
{
    public bool IsFound => Profile is not null;

    public bool IsAmbiguous => MatchingTypes.Count > 1;

    public bool HasSingleMatch => MatchingTypes.Count == 1;

    public bool HasStrongRecommendations =>
        TechniqueRecommendations.Any(recommendation => recommendation.Strength is RecommendationStrength.Strong or RecommendationStrength.Moderate) ||
        SuggestedSteps.Count > 0;
}

public sealed record ClassRefactoringTypeMatch(
    string Name,
    string FullName,
    string ProjectName,
    string SourcePath,
    int LineNumber);

public sealed record ClassRefactoringProfile(
    string Name,
    string FullName,
    string ProjectName,
    string SourcePath,
    int LineNumber,
    string Accessibility,
    bool IsStatic,
    bool IsAbstract,
    bool IsSealed,
    IReadOnlyList<string> BaseTypes,
    IReadOnlyList<string> ImplementedInterfaces,
    IReadOnlyList<string> ConstructorParameterTypes,
    IReadOnlyList<string> FieldTypes,
    IReadOnlyList<string> PropertyTypes,
    IReadOnlyList<MethodRefactoringProfile> Methods,
    int MemberCount,
    int DependencyLikeSignalCount);

public sealed record MethodRefactoringProfile(
    string Name,
    string Signature,
    string Accessibility,
    string ReturnType,
    int LineNumber,
    int Complexity,
    bool IsVirtualOrAbstract,
    bool IsProtected,
    bool IsVoidLike,
    bool HasReturnValue,
    bool HasObjectCreation,
    bool HasStaticOrGlobalAccess,
    bool HasInvocation,
    bool HasFrameworkCoupling,
    bool HasConfigurationAccess,
    bool HasDataAccessSignal,
    bool HasExternalDependencySignal,
    MethodRole Role,
    TestingPathClassification TestingPath,
    string Evidence);

public sealed record RefactoringSignal(
    RefactoringSignalKind Kind,
    RefactoringSignalStrength Strength,
    RefactoringSignalConfidence Confidence,
    string SourcePath,
    int LineNumber,
    string MemberName,
    string Evidence,
    string WhyItMatters,
    string SuggestedReview);

public enum RefactoringSignalKind
{
    ExistingSeam,
    MissingOrWeakSeam,
    HardcodedObjectCreation,
    StaticGlobalDependency,
    ConstructorTestabilityConcern,
    MethodComplexityHotspot,
    SideEffectLikeMethod,
    FrameworkCoupling,
    ConfigurationCoupling,
    DataAccessCoupling,
    ExternalDependencyCoupling,
    InboundCoupling,
    LargeClassResponsibility,
    AdaptParameterOpportunity,
    DirectCharacterizationFeasible
}

public enum RefactoringSignalStrength
{
    Low,
    Medium,
    High
}

public enum RefactoringSignalConfidence
{
    Low,
    Medium,
    High
}

public sealed record TechniqueRecommendation(
    LegacyCodeTechnique Technique,
    RecommendationStrength Strength,
    string WhyItApplies,
    string HumanReviewRequired,
    IReadOnlyList<RecommendationBlocker> Blockers,
    string Evidence);

public enum LegacyCodeTechnique
{
    CharacterizationTests,
    Sensing,
    ExtractInterface,
    ParameterizeConstructor,
    ParameterizeMethod,
    ExtractAndOverrideFactoryMethod,
    ExtractAndOverrideCall,
    EncapsulateGlobalReferences,
    SproutMethod,
    SproutClass,
    ExtractMethod,
    BreakOutMethodObject,
    WrapClass,
    AdaptParameter,
    HigherLevelTestsFirst
}

public enum RecommendationStrength
{
    NotRecommended,
    NotEnoughEvidence,
    Low,
    Moderate,
    Strong
}

public sealed record RecommendationBlocker(
    string Reason,
    string Evidence);

public sealed record SuggestedRefactoringStep(
    int Order,
    string Step,
    SuggestedStepRisk Risk,
    SuggestedStepValue Value,
    string Why,
    string Evidence);

public enum SuggestedStepRisk
{
    Low,
    Medium,
    High
}

public enum SuggestedStepValue
{
    Low,
    Medium,
    High
}

public sealed record ExistingSeam(
    string Kind,
    string SourcePath,
    int LineNumber,
    string MemberName,
    string Evidence,
    string HowToUse);

public sealed record MissingOrWeakSeam(
    string Kind,
    string SourcePath,
    int LineNumber,
    string MemberName,
    string Evidence,
    string SuggestedTechnique);

public sealed record TestabilityBarrier(
    string Kind,
    string Strength,
    string SourcePath,
    int LineNumber,
    string MemberName,
    string Evidence,
    string WhyItMatters);

public sealed record CharacterizationTestTarget(
    string MemberName,
    string MethodRole,
    TestingPathClassification TestingPath,
    int Complexity,
    string SourcePath,
    int LineNumber,
    string SuggestedFirstTest,
    string Evidence);

public enum TestingPathClassification
{
    DirectCharacterizationPossible,
    CharacterizationViaExistingSeams,
    DependencyBreakingNeededFirst,
    HigherLevelTestRecommendedFirst,
    Unknown
}

public enum MethodRole
{
    PureOrPureishCalculation,
    SideEffectingWorkflow,
    FrameworkBoundary,
    DataAccessOperation,
    FactoryOrConstructionMethod,
    ConfigurationAccessMethod,
    Unknown
}
