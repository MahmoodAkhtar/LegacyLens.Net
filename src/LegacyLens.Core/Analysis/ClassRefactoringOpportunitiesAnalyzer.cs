using LegacyLens.Core.Files;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LegacyLens.Core.Analysis;

public sealed class ClassRefactoringOpportunitiesAnalyzer
{
    private static readonly HashSet<string> PrimitiveOrLowValueTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "string", "object", "bool", "byte", "short", "int", "long", "float", "double", "decimal", "char",
        "DateTime", "DateTimeOffset", "Guid", "Task", "ValueTask", "IEnumerable", "IReadOnlyList", "List", "Dictionary"
    };

    private static readonly HashSet<string> StaticOrGlobalAccessTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ConfigurationManager", "DateTime", "DateTimeOffset", "File", "Directory", "Environment", "HttpContext",
        "DependencyResolver", "ControllerBuilder", "GlobalConfiguration", "RouteTable", "GlobalFilters", "Console",
        "Thread", "Task", "SmtpClient", "SqlConnection"
    };

    private static readonly string[] FrameworkBoundaryTerms =
    [
        "Controller", "ApiController", "HttpContext", "HttpRequest", "HttpResponse", "ActionResult", "IHttpActionResult",
        "HttpResponseMessage", "HttpClient", "IApplicationBuilder", "HttpConfiguration"
    ];

    private static readonly string[] DataAccessTerms =
    [
        "Repository", "DbContext", "ObjectContext", "SqlConnection", "DbConnection", "IDbConnection", "Dapper",
        "EntityFramework", "NHibernate", "UnitOfWork", "ExecuteReader", "ExecuteNonQuery", "ExecuteScalar", "FromSql", "SaveChanges"
    ];

    private static readonly string[] ExternalDependencyTerms =
    [
        "HttpClient", "SmtpClient", "MailMessage", "Queue", "Bus", "Rabbit", "Redis", "Cache", "File", "Directory", "Stream", "WebRequest"
    ];

    public ClassRefactoringOpportunitiesReport Analyze(
        IReadOnlyCollection<ScanFile> csharpFiles,
        string requestedTypeName,
        DateTimeOffset generatedLocal,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(csharpFiles);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedTypeName);

        var requested = requestedTypeName.Trim();
        var sourceFiles = csharpFiles
            .Select(ParseSourceFile)
            .ToArray();

        var discoveredTypes = sourceFiles
            .SelectMany(DiscoverClassMatches)
            .OrderBy(type => type.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(type => type.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(type => type.LineNumber)
            .ToArray();

        var matches = discoveredTypes
            .Where(type => type.FullName.Equals(requested, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length != 1)
        {
            return new ClassRefactoringOpportunitiesReport(
                requested,
                generatedLocal,
                generatedUtc,
                sourceFiles.Length,
                discoveredTypes.Length,
                matches,
                Profile: null,
                Signals: Array.Empty<RefactoringSignal>(),
                ExistingSeams: Array.Empty<ExistingSeam>(),
                MissingOrWeakSeams: Array.Empty<MissingOrWeakSeam>(),
                TestabilityBarriers: Array.Empty<TestabilityBarrier>(),
                CharacterizationTestTargets: Array.Empty<CharacterizationTestTarget>(),
                TechniqueRecommendations: Array.Empty<TechniqueRecommendation>(),
                NotRecommendedTechniques: CreateNotEnoughEvidenceRecommendations(),
                SuggestedSteps: Array.Empty<SuggestedRefactoringStep>());
        }

        var match = matches[0];
        var sourceFile = sourceFiles.Single(file => file.FullPath.Equals(match.SourcePath, StringComparison.OrdinalIgnoreCase));
        var classDeclaration = sourceFile.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(declaration => GetLineNumber(sourceFile.SyntaxTree, declaration) == match.LineNumber &&
                                  CreateFullTypeName(declaration, declaration.Identifier.Text).Equals(match.FullName, StringComparison.OrdinalIgnoreCase));

        var profile = CreateProfile(sourceFile, classDeclaration, match);
        var existingSeams = DiscoverExistingSeams(sourceFile, classDeclaration, profile);
        var missingOrWeakSeams = DiscoverMissingOrWeakSeams(sourceFile, classDeclaration, profile).ToArray();
        var barriers = DiscoverTestabilityBarriers(profile, missingOrWeakSeams).ToArray();
        var signals = DiscoverSignals(profile, existingSeams, missingOrWeakSeams, barriers).ToArray();
        var characterizationTargets = CreateCharacterizationTargets(profile).ToArray();
        var recommendations = CreateTechniqueRecommendations(profile, signals, existingSeams, missingOrWeakSeams, barriers).ToArray();
        var notRecommended = CreateNotRecommendedRecommendations(profile, existingSeams, missingOrWeakSeams, recommendations).ToArray();
        var steps = CreateSuggestedSteps(profile, existingSeams, missingOrWeakSeams, barriers, characterizationTargets).ToArray();

        return new ClassRefactoringOpportunitiesReport(
            requested,
            generatedLocal,
            generatedUtc,
            sourceFiles.Length,
            discoveredTypes.Length,
            matches,
            profile,
            signals,
            existingSeams,
            missingOrWeakSeams,
            barriers,
            characterizationTargets,
            recommendations,
            notRecommended,
            steps);
    }

    private static SourceFileInfo ParseSourceFile(ScanFile file)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(file.Content ?? string.Empty);
        return new SourceFileInfo(
            file.ProjectName,
            file.FullPath,
            syntaxTree,
            syntaxTree.GetCompilationUnitRoot());
    }

    private static IEnumerable<ClassRefactoringTypeMatch> DiscoverClassMatches(SourceFileInfo sourceFile)
    {
        foreach (var declaration in sourceFile.Root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var name = declaration.Identifier.Text;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            yield return new ClassRefactoringTypeMatch(
                name,
                CreateFullTypeName(declaration, name),
                sourceFile.ProjectName,
                sourceFile.FullPath,
                GetLineNumber(sourceFile.SyntaxTree, declaration));
        }
    }

    private static ClassRefactoringProfile CreateProfile(
        SourceFileInfo sourceFile,
        ClassDeclarationSyntax declaration,
        ClassRefactoringTypeMatch match)
    {
        var constructors = declaration.Members.OfType<ConstructorDeclarationSyntax>().ToArray();
        var fields = declaration.Members.OfType<FieldDeclarationSyntax>().ToArray();
        var properties = declaration.Members.OfType<PropertyDeclarationSyntax>().ToArray();
        var methods = declaration.Members.OfType<MethodDeclarationSyntax>()
            .Select(method => CreateMethodProfile(sourceFile, method, declaration))
            .OrderBy(method => method.LineNumber)
            .ToArray();

        var baseTypes = declaration.BaseList?.Types
            .Select(type => type.Type.ToString())
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        var implementedInterfaces = baseTypes
            .Where(IsLikelyInterfaceType)
            .ToArray();

        var constructorParameterTypes = constructors
            .SelectMany(constructor => constructor.ParameterList.Parameters)
            .Select(parameter => parameter.Type?.ToString() ?? string.Empty)
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var fieldTypes = fields
            .Select(field => field.Declaration.Type.ToString())
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var propertyTypes = properties
            .Select(property => property.Type.ToString())
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var dependencyLikeSignalCount = declaration.DescendantNodes()
            .Count(node => node is ObjectCreationExpressionSyntax or InvocationExpressionSyntax or MemberAccessExpressionSyntax);

        return new ClassRefactoringProfile(
            match.Name,
            match.FullName,
            match.ProjectName,
            match.SourcePath,
            match.LineNumber,
            GetAccessibility(declaration.Modifiers),
            declaration.Modifiers.Any(SyntaxKind.StaticKeyword),
            declaration.Modifiers.Any(SyntaxKind.AbstractKeyword),
            declaration.Modifiers.Any(SyntaxKind.SealedKeyword),
            baseTypes,
            implementedInterfaces,
            constructorParameterTypes,
            fieldTypes,
            propertyTypes,
            methods,
            declaration.Members.Count,
            dependencyLikeSignalCount);
    }

    private static MethodRefactoringProfile CreateMethodProfile(
        SourceFileInfo sourceFile,
        MethodDeclarationSyntax method,
        ClassDeclarationSyntax owningClass)
    {
        var returnType = method.ReturnType.ToString();
        var methodName = method.Identifier.Text;
        var objectCreation = method.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Any();
        var staticOrGlobalAccess = HasStaticOrGlobalAccess(method);
        var invocation = method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any();
        var frameworkCoupling = ContainsAny(method.ToString(), FrameworkBoundaryTerms) || ContainsAny(owningClass.BaseList?.ToString() ?? string.Empty, FrameworkBoundaryTerms);
        var configurationAccess = ContainsAny(method.ToString(), ["ConfigurationManager", "IConfiguration", "GetSection", "AppSettings", "ConnectionStrings"]);
        var dataAccess = ContainsAny(method.ToString(), DataAccessTerms);
        var externalDependency = ContainsAny(method.ToString(), ExternalDependencyTerms);
        var complexity = CalculateComplexity(method);
        var isVoidLike = returnType.Equals("void", StringComparison.OrdinalIgnoreCase) || returnType.Equals("Task", StringComparison.OrdinalIgnoreCase);
        var hasReturnValue = !isVoidLike;
        var role = ClassifyMethodRole(method, returnType, objectCreation, frameworkCoupling, configurationAccess, dataAccess, invocation);
        var testingPath = ClassifyTestingPath(role, isVoidLike, objectCreation, staticOrGlobalAccess, frameworkCoupling, invocation);
        var lineNumber = GetLineNumber(sourceFile.SyntaxTree, method);

        return new MethodRefactoringProfile(
            methodName,
            CreateMethodSignature(method),
            GetAccessibility(method.Modifiers),
            returnType,
            lineNumber,
            complexity,
            method.Modifiers.Any(SyntaxKind.VirtualKeyword) || method.Modifiers.Any(SyntaxKind.AbstractKeyword) || method.Modifiers.Any(SyntaxKind.OverrideKeyword),
            method.Modifiers.Any(SyntaxKind.ProtectedKeyword),
            isVoidLike,
            hasReturnValue,
            objectCreation,
            staticOrGlobalAccess,
            invocation,
            frameworkCoupling,
            configurationAccess,
            dataAccess,
            externalDependency,
            role,
            testingPath,
            GetEvidence(method));
    }

    private static IReadOnlyList<ExistingSeam> DiscoverExistingSeams(
        SourceFileInfo sourceFile,
        ClassDeclarationSyntax declaration,
        ClassRefactoringProfile profile)
    {
        var seams = new List<ExistingSeam>();

        foreach (var constructor in declaration.Members.OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var parameter in constructor.ParameterList.Parameters.Where(parameter => IsLikelyInterfaceType(parameter.Type?.ToString() ?? string.Empty)))
            {
                seams.Add(new ExistingSeam(
                    "Constructor-injected interface",
                    sourceFile.FullPath,
                    GetLineNumber(sourceFile.SyntaxTree, parameter),
                    constructor.Identifier.Text,
                    parameter.ToString(),
                    "Use a fake implementation when adding characterization tests."));
            }
        }

        foreach (var method in declaration.Members.OfType<MethodDeclarationSyntax>())
        {
            foreach (var parameter in method.ParameterList.Parameters.Where(parameter => IsLikelyInterfaceType(parameter.Type?.ToString() ?? string.Empty)))
            {
                seams.Add(new ExistingSeam(
                    "Method parameter interface",
                    sourceFile.FullPath,
                    GetLineNumber(sourceFile.SyntaxTree, parameter),
                    method.Identifier.Text,
                    parameter.ToString(),
                    "Pass a fake or stub through the method parameter to sense behaviour."));
            }

            if (method.Modifiers.Any(SyntaxKind.VirtualKeyword) || method.Modifiers.Any(SyntaxKind.ProtectedKeyword))
            {
                seams.Add(new ExistingSeam(
                    "Overridable method seam",
                    sourceFile.FullPath,
                    GetLineNumber(sourceFile.SyntaxTree, method),
                    method.Identifier.Text,
                    CreateMethodSignature(method),
                    "A test subclass may be able to override this behaviour while characterization tests are introduced."));
            }
        }

        foreach (var field in declaration.Members.OfType<FieldDeclarationSyntax>())
        {
            var fieldType = field.Declaration.Type.ToString();
            if (IsLikelyInterfaceType(fieldType))
            {
                seams.Add(new ExistingSeam(
                    "Interface field",
                    sourceFile.FullPath,
                    GetLineNumber(sourceFile.SyntaxTree, field),
                    string.Join(", ", field.Declaration.Variables.Select(variable => variable.Identifier.Text)),
                    field.ToString(),
                    "The field type suggests an object seam may already exist."));
            }
        }

        foreach (var property in declaration.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (IsLikelyInterfaceType(property.Type.ToString()))
            {
                seams.Add(new ExistingSeam(
                    "Interface property",
                    sourceFile.FullPath,
                    GetLineNumber(sourceFile.SyntaxTree, property),
                    property.Identifier.Text,
                    property.ToString(),
                    "The property type suggests an object seam may already exist."));
            }
        }

        foreach (var implementedInterface in profile.ImplementedInterfaces)
        {
            seams.Add(new ExistingSeam(
                "Implemented interface",
                profile.SourcePath,
                profile.LineNumber,
                profile.Name,
                implementedInterface,
                "The class can potentially be tested or wrapped through its interface boundary."));
        }

        return seams
            .GroupBy(seam => string.Join("|", seam.Kind, seam.MemberName, seam.Evidence), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static IEnumerable<MissingOrWeakSeam> DiscoverMissingOrWeakSeams(
        SourceFileInfo sourceFile,
        ClassDeclarationSyntax declaration,
        ClassRefactoringProfile profile)
    {
        foreach (var creation in declaration.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var typeName = SimplifyTypeName(creation.Type.ToString());
            if (PrimitiveOrLowValueTypes.Contains(typeName))
            {
                continue;
            }

            yield return new MissingOrWeakSeam(
                "Hardcoded concrete object creation",
                sourceFile.FullPath,
                GetLineNumber(sourceFile.SyntaxTree, creation),
                FindContainingMemberName(creation),
                creation.ToString(),
                "Parameterize Constructor, Parameterize Method, or Extract and Override Factory Method");
        }

        foreach (var access in declaration.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var expression = access.Expression.ToString();
            if (!StaticOrGlobalAccessTypes.Contains(SimplifyTypeName(expression)))
            {
                continue;
            }

            yield return new MissingOrWeakSeam(
                "Static or global dependency access",
                sourceFile.FullPath,
                GetLineNumber(sourceFile.SyntaxTree, access),
                FindContainingMemberName(access),
                access.ToString(),
                "Encapsulate Global References or Replace Global Reference with Getter");
        }

        foreach (var typeName in profile.FieldTypes.Concat(profile.PropertyTypes).Where(type => !IsLikelyInterfaceType(type)))
        {
            var simplified = SimplifyTypeName(typeName);
            if (PrimitiveOrLowValueTypes.Contains(simplified))
            {
                continue;
            }

            yield return new MissingOrWeakSeam(
                "Concrete collaborator member",
                profile.SourcePath,
                profile.LineNumber,
                profile.Name,
                typeName,
                "Extract Interface or Parameterize Constructor if the collaborator needs to be substituted in tests");
        }
    }

    private static IEnumerable<TestabilityBarrier> DiscoverTestabilityBarriers(
        ClassRefactoringProfile profile,
        IReadOnlyList<MissingOrWeakSeam> missingOrWeakSeams)
    {
        foreach (var seam in missingOrWeakSeams)
        {
            yield return new TestabilityBarrier(
                seam.Kind,
                seam.Kind.Contains("Hardcoded", StringComparison.OrdinalIgnoreCase) ? "High" : "Medium",
                seam.SourcePath,
                seam.LineNumber,
                seam.MemberName,
                seam.Evidence,
                "The dependency may be difficult to substitute while characterization tests are being added.");
        }

        foreach (var method in profile.Methods.Where(method => method.Complexity >= 11))
        {
            yield return new TestabilityBarrier(
                "High method complexity",
                method.Complexity >= 21 ? "High" : "Medium",
                profile.SourcePath,
                method.LineNumber,
                method.Name,
                method.Evidence,
                "High branching increases the number of behaviours that should be captured before refactoring.");
        }

        foreach (var method in profile.Methods.Where(method => method.IsVoidLike && method.HasInvocation))
        {
            yield return new TestabilityBarrier(
                "Void side-effect-like workflow",
                "Medium",
                profile.SourcePath,
                method.LineNumber,
                method.Name,
                method.Evidence,
                "The method may require sensing through fake dependencies or observable outputs.");
        }
    }

    private static IEnumerable<RefactoringSignal> DiscoverSignals(
        ClassRefactoringProfile profile,
        IReadOnlyList<ExistingSeam> existingSeams,
        IReadOnlyList<MissingOrWeakSeam> missingOrWeakSeams,
        IReadOnlyList<TestabilityBarrier> barriers)
    {
        foreach (var seam in existingSeams)
        {
            yield return new RefactoringSignal(
                RefactoringSignalKind.ExistingSeam,
                RefactoringSignalStrength.Medium,
                RefactoringSignalConfidence.High,
                seam.SourcePath,
                seam.LineNumber,
                seam.MemberName,
                seam.Evidence,
                "An existing object or override seam may allow safer characterization tests.",
                seam.HowToUse);
        }

        foreach (var seam in missingOrWeakSeams)
        {
            var kind = seam.Kind.Contains("Hardcoded", StringComparison.OrdinalIgnoreCase)
                ? RefactoringSignalKind.HardcodedObjectCreation
                : seam.Kind.Contains("Static", StringComparison.OrdinalIgnoreCase)
                    ? RefactoringSignalKind.StaticGlobalDependency
                    : RefactoringSignalKind.MissingOrWeakSeam;

            yield return new RefactoringSignal(
                kind,
                RefactoringSignalStrength.High,
                RefactoringSignalConfidence.High,
                seam.SourcePath,
                seam.LineNumber,
                seam.MemberName,
                seam.Evidence,
                "This may block direct unit-level characterization because the dependency is not easily replaceable.",
                seam.SuggestedTechnique);
        }

        foreach (var barrier in barriers)
        {
            var kind = barrier.Kind.Contains("complexity", StringComparison.OrdinalIgnoreCase)
                ? RefactoringSignalKind.MethodComplexityHotspot
                : barrier.Kind.Contains("Void", StringComparison.OrdinalIgnoreCase)
                    ? RefactoringSignalKind.SideEffectLikeMethod
                    : RefactoringSignalKind.ConstructorTestabilityConcern;

            yield return new RefactoringSignal(
                kind,
                barrier.Strength.Equals("High", StringComparison.OrdinalIgnoreCase) ? RefactoringSignalStrength.High : RefactoringSignalStrength.Medium,
                RefactoringSignalConfidence.Medium,
                barrier.SourcePath,
                barrier.LineNumber,
                barrier.MemberName,
                barrier.Evidence,
                barrier.WhyItMatters,
                "Review before making design changes.");
        }

        foreach (var method in profile.Methods.Where(method => method.TestingPath == TestingPathClassification.DirectCharacterizationPossible))
        {
            yield return new RefactoringSignal(
                RefactoringSignalKind.DirectCharacterizationFeasible,
                RefactoringSignalStrength.Low,
                RefactoringSignalConfidence.Medium,
                profile.SourcePath,
                method.LineNumber,
                method.Name,
                method.Signature,
                "This method appears to return a value without strong dependency-breaking signals.",
                "Start with direct characterization or ordinary unit tests.");
        }
    }

    private static IEnumerable<CharacterizationTestTarget> CreateCharacterizationTargets(ClassRefactoringProfile profile)
    {
        return profile.Methods
            .Where(method => method.Accessibility is "public" or "internal")
            .OrderByDescending(method => ScoreCharacterizationTarget(method))
            .ThenBy(method => method.LineNumber)
            .Take(10)
            .Select(method => new CharacterizationTestTarget(
                method.Name,
                method.Role.ToString(),
                method.TestingPath,
                method.Complexity,
                profile.SourcePath,
                method.LineNumber,
                CreateSuggestedFirstTest(method),
                method.Evidence));
    }

    private static IEnumerable<TechniqueRecommendation> CreateTechniqueRecommendations(
        ClassRefactoringProfile profile,
        IReadOnlyList<RefactoringSignal> signals,
        IReadOnlyList<ExistingSeam> existingSeams,
        IReadOnlyList<MissingOrWeakSeam> missingOrWeakSeams,
        IReadOnlyList<TestabilityBarrier> barriers)
    {
        if (profile.Methods.Count > 0)
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.CharacterizationTests,
                barriers.Count > 0 ? RecommendationStrength.Strong : RecommendationStrength.Moderate,
                barriers.Count > 0
                    ? "Evidence found for complexity, side effects, or dependency barriers; capture existing behaviour before refactoring."
                    : "Public or internal methods were found and can be used to document current behaviour before design changes.",
                "Confirm expected business behaviours and ugly-but-relied-on edge cases with the team.",
                Array.Empty<RecommendationBlocker>(),
                string.Join("; ", profile.Methods.Take(3).Select(method => method.Signature)));
        }

        if (missingOrWeakSeams.Any(seam => seam.Kind.Contains("Hardcoded", StringComparison.OrdinalIgnoreCase)))
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.ParameterizeConstructor,
                RecommendationStrength.Strong,
                "Hardcoded concrete object creation was found; constructor or method parameterization can create an object seam.",
                "Confirm object lifetime and runtime construction before changing constructors.",
                CreateRuntimeBlockers("Constructor changes can affect callers and DI registrations."),
                FirstEvidence(missingOrWeakSeams, "Hardcoded"));

            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.ExtractAndOverrideFactoryMethod,
                RecommendationStrength.Moderate,
                "Object creation inside the class may be isolated behind a factory method as a tactical dependency-breaking seam.",
                "Use as a stepping stone if constructor injection is too risky immediately.",
                CreateRuntimeBlockers("Factory override seams can be temporary and should be revisited after tests exist."),
                FirstEvidence(missingOrWeakSeams, "Hardcoded"));
        }

        if (missingOrWeakSeams.Any(seam => seam.Kind.Contains("Static", StringComparison.OrdinalIgnoreCase)))
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.EncapsulateGlobalReferences,
                RecommendationStrength.Strong,
                "Static or global access was found and may need to be wrapped before isolated tests are reliable.",
                "Confirm whether the static value is time, environment, configuration, filesystem, or framework state.",
                CreateRuntimeBlockers("Static state may be shared across tests and runtime requests."),
                FirstEvidence(missingOrWeakSeams, "Static"));
        }

        if (profile.Methods.Any(method => method.IsVoidLike && method.HasInvocation))
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.Sensing,
                RecommendationStrength.Moderate,
                "Void side-effect-like methods were found; fakes or wrappers may be needed to observe behaviour.",
                "Confirm which calls represent important observable behaviour.",
                CreateRuntimeBlockers("Static analysis cannot prove the runtime side effect."),
                string.Join("; ", profile.Methods.Where(method => method.IsVoidLike && method.HasInvocation).Take(3).Select(method => method.Signature)));
        }

        if (existingSeams.Count == 0 && missingOrWeakSeams.Any(seam => seam.Kind.Contains("Concrete collaborator", StringComparison.OrdinalIgnoreCase)))
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.ExtractInterface,
                RecommendationStrength.Moderate,
                "Concrete collaborator members were found and no strong interface seam was detected for the class.",
                "Confirm whether an interface already exists elsewhere but was not visible from syntax-only analysis.",
                CreateRuntimeBlockers("Do not introduce abstractions unless they support a real test seam or change need."),
                FirstEvidence(missingOrWeakSeams, "Concrete"));
        }

        if (profile.Methods.Any(method => method.Complexity >= 11))
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.ExtractMethod,
                RecommendationStrength.Low,
                "High-complexity methods may later benefit from extraction after characterization tests exist.",
                "Do not extract until behaviour is captured; verify local variables and side effects first.",
                CreateRuntimeBlockers("Extraction before tests can change behaviour accidentally."),
                string.Join("; ", profile.Methods.Where(method => method.Complexity >= 11).Take(3).Select(method => method.Signature)));
        }

        if (profile.Methods.Any(method => method.HasFrameworkCoupling))
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.HigherLevelTestsFirst,
                RecommendationStrength.Moderate,
                "Framework-boundary signals were found; direct unit characterization may be expensive before separating business logic.",
                "Confirm routing, filters, model binding, and framework lifecycle behaviour.",
                CreateRuntimeBlockers("Syntax-only analysis cannot simulate framework hosting."),
                string.Join("; ", profile.Methods.Where(method => method.HasFrameworkCoupling).Take(3).Select(method => method.Signature)));
        }
    }

    private static IEnumerable<TechniqueRecommendation> CreateNotRecommendedRecommendations(
        ClassRefactoringProfile profile,
        IReadOnlyList<ExistingSeam> existingSeams,
        IReadOnlyList<MissingOrWeakSeam> missingOrWeakSeams,
        IReadOnlyList<TechniqueRecommendation> recommendations)
    {
        if (existingSeams.Count > 0 && !recommendations.Any(recommendation => recommendation.Technique == LegacyCodeTechnique.ExtractInterface))
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.ExtractInterface,
                RecommendationStrength.NotRecommended,
                "Existing interface or override seam evidence was found; do not strongly recommend Extract Interface by default.",
                "Verify runtime DI/IoC registrations and whether the existing seam is usable in tests.",
                Array.Empty<RecommendationBlocker>(),
                existingSeams[0].Evidence);
        }

        if (!profile.Methods.Any(method => method.Complexity >= 21))
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.BreakOutMethodObject,
                RecommendationStrength.NotEnoughEvidence,
                "No method had very-high complexity evidence in this static profile.",
                "Review manually if a method has many locals or hidden responsibilities not captured by syntax heuristics.",
                Array.Empty<RecommendationBlocker>(),
                "No very-high complexity method detected.");
        }

        if (missingOrWeakSeams.Count == 0 && profile.Methods.All(method => method.TestingPath == TestingPathClassification.DirectCharacterizationPossible))
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.Sensing,
                RecommendationStrength.NotEnoughEvidence,
                "No strong side-effect or hidden dependency evidence was found.",
                "Review manually if production behaviour is more stateful than the visible source suggests.",
                Array.Empty<RecommendationBlocker>(),
                "No strong sensing signal detected.");
        }

        if (recommendations.All(recommendation => recommendation.Strength is RecommendationStrength.Low or RecommendationStrength.NotEnoughEvidence or RecommendationStrength.NotRecommended))
        {
            yield return new TechniqueRecommendation(
                LegacyCodeTechnique.WrapClass,
                RecommendationStrength.NotEnoughEvidence,
                "Wrap Class usually needs caller-boundary or intended-change context, which is not available from this static class-only profile.",
                "Consider only when changing callers is safer than changing the class directly.",
                Array.Empty<RecommendationBlocker>(),
                "No caller-boundary evidence available.");
        }
    }

    private static IReadOnlyList<TechniqueRecommendation> CreateNotEnoughEvidenceRecommendations() =>
    [
        new TechniqueRecommendation(
            LegacyCodeTechnique.CharacterizationTests,
            RecommendationStrength.NotEnoughEvidence,
            "The requested fully qualified type was not uniquely resolved, so no class-specific testing approach can be recommended.",
            "Confirm the fully qualified type name and source file.",
            Array.Empty<RecommendationBlocker>(),
            "No unique type match."),
        new TechniqueRecommendation(
            LegacyCodeTechnique.ExtractInterface,
            RecommendationStrength.NotEnoughEvidence,
            "The requested class source was not uniquely found.",
            "Confirm class identity before selecting a dependency-breaking technique.",
            Array.Empty<RecommendationBlocker>(),
            "No unique type match.")
    ];

    private static IEnumerable<SuggestedRefactoringStep> CreateSuggestedSteps(
        ClassRefactoringProfile profile,
        IReadOnlyList<ExistingSeam> existingSeams,
        IReadOnlyList<MissingOrWeakSeam> missingOrWeakSeams,
        IReadOnlyList<TestabilityBarrier> barriers,
        IReadOnlyList<CharacterizationTestTarget> targets)
    {
        var order = 1;

        if (targets.Count > 0)
        {
            yield return new SuggestedRefactoringStep(
                order++,
                "Capture current behaviour with characterization tests for the highest-value public methods.",
                SuggestedStepRisk.Low,
                SuggestedStepValue.High,
                "This creates feedback before design changes.",
                targets[0].Evidence);
        }

        if (existingSeams.Count > 0)
        {
            yield return new SuggestedRefactoringStep(
                order++,
                "Use existing seams with fakes or test subclasses before introducing new abstractions.",
                SuggestedStepRisk.Low,
                SuggestedStepValue.High,
                "Existing seams are usually lower risk than creating new public contracts.",
                existingSeams[0].Evidence);
        }

        if (missingOrWeakSeams.Any(seam => seam.Kind.Contains("Hardcoded", StringComparison.OrdinalIgnoreCase)))
        {
            yield return new SuggestedRefactoringStep(
                order++,
                "Break hardcoded construction only where it blocks the first characterization tests.",
                SuggestedStepRisk.Medium,
                SuggestedStepValue.High,
                "Dependency breaking should be narrow and motivated by a test need.",
                FirstEvidence(missingOrWeakSeams, "Hardcoded"));
        }

        if (missingOrWeakSeams.Any(seam => seam.Kind.Contains("Static", StringComparison.OrdinalIgnoreCase)))
        {
            yield return new SuggestedRefactoringStep(
                order++,
                "Encapsulate static/global access behind a small wrapper or getter when it blocks deterministic tests.",
                SuggestedStepRisk.Medium,
                SuggestedStepValue.Medium,
                "Global state often makes tests order-dependent or environment-dependent.",
                FirstEvidence(missingOrWeakSeams, "Static"));
        }

        if (profile.Methods.Any(method => method.Complexity >= 11))
        {
            yield return new SuggestedRefactoringStep(
                order++,
                "After behaviour is covered, extract smaller decision logic from high-complexity methods.",
                SuggestedStepRisk.Medium,
                SuggestedStepValue.Medium,
                "Extraction is safer after characterization tests are in place.",
                profile.Methods.First(method => method.Complexity >= 11).Evidence);
        }

        if (order == 1)
        {
            yield return new SuggestedRefactoringStep(
                1,
                "No strong recommendation: start with direct tests for returned-value behaviour and review manually before refactoring.",
                SuggestedStepRisk.Low,
                SuggestedStepValue.Medium,
                "The static profile did not show strong dependency-breaking or sensing barriers.",
                profile.Methods.FirstOrDefault()?.Evidence ?? profile.FullName);
        }
    }

    private static MethodRole ClassifyMethodRole(
        MethodDeclarationSyntax method,
        string returnType,
        bool hasObjectCreation,
        bool hasFrameworkCoupling,
        bool hasConfigurationAccess,
        bool hasDataAccess,
        bool hasInvocation)
    {
        var text = method.ToString();
        if (hasFrameworkCoupling)
        {
            return MethodRole.FrameworkBoundary;
        }

        if (hasConfigurationAccess)
        {
            return MethodRole.ConfigurationAccessMethod;
        }

        if (hasDataAccess)
        {
            return MethodRole.DataAccessOperation;
        }

        if (hasObjectCreation && !returnType.Equals("void", StringComparison.OrdinalIgnoreCase))
        {
            return MethodRole.FactoryOrConstructionMethod;
        }

        if (returnType.Equals("void", StringComparison.OrdinalIgnoreCase) || method.Identifier.Text.Contains("Save", StringComparison.OrdinalIgnoreCase) || method.Identifier.Text.Contains("Send", StringComparison.OrdinalIgnoreCase))
        {
            return MethodRole.SideEffectingWorkflow;
        }

        if (!hasInvocation && !hasObjectCreation && !ContainsAny(text, ExternalDependencyTerms))
        {
            return MethodRole.PureOrPureishCalculation;
        }

        return MethodRole.Unknown;
    }

    private static TestingPathClassification ClassifyTestingPath(
        MethodRole role,
        bool isVoidLike,
        bool objectCreation,
        bool staticOrGlobalAccess,
        bool frameworkCoupling,
        bool invocation)
    {
        if (frameworkCoupling || role == MethodRole.FrameworkBoundary)
        {
            return TestingPathClassification.HigherLevelTestRecommendedFirst;
        }

        if (objectCreation || staticOrGlobalAccess)
        {
            return TestingPathClassification.DependencyBreakingNeededFirst;
        }

        if (isVoidLike && invocation)
        {
            return TestingPathClassification.CharacterizationViaExistingSeams;
        }

        if (!isVoidLike)
        {
            return TestingPathClassification.DirectCharacterizationPossible;
        }

        return TestingPathClassification.Unknown;
    }

    private static string CreateSuggestedFirstTest(MethodRefactoringProfile method)
    {
        return method.TestingPath switch
        {
            TestingPathClassification.DirectCharacterizationPossible => "Call the method with representative inputs and assert the returned value that the current code actually produces.",
            TestingPathClassification.CharacterizationViaExistingSeams => "Use an existing seam or fake dependency to sense the calls or state changes produced by the method.",
            TestingPathClassification.DependencyBreakingNeededFirst => "Create the smallest dependency-breaking seam needed before asserting behaviour.",
            TestingPathClassification.HigherLevelTestRecommendedFirst => "Start with a higher-level test around the framework boundary, then separate business logic when safe.",
            _ => "Review manually and identify an observable behaviour before changing the method."
        };
    }

    private static int ScoreCharacterizationTarget(MethodRefactoringProfile method)
    {
        var score = method.Accessibility == "public" ? 10 : 0;
        score += method.Complexity;
        score += method.HasInvocation ? 4 : 0;
        score += method.HasObjectCreation ? 5 : 0;
        score += method.HasStaticOrGlobalAccess ? 5 : 0;
        score += method.HasReturnValue ? 2 : 0;
        return score;
    }

    private static int CalculateComplexity(SyntaxNode node)
    {
        return 1 + node.DescendantNodes(descendIntoChildren: child => !IsNestedMethodBoundary(node, child)).Count(IsDecisionPoint);
    }

    private static bool IsDecisionPoint(SyntaxNode node)
    {
        return node switch
        {
            IfStatementSyntax => true,
            ForStatementSyntax => true,
            ForEachStatementSyntax => true,
            WhileStatementSyntax => true,
            DoStatementSyntax => true,
            CatchClauseSyntax => true,
            ConditionalExpressionSyntax => true,
            SwitchExpressionArmSyntax => true,
            WhenClauseSyntax => true,
            BinaryExpressionSyntax binary => binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression),
            SwitchSectionSyntax section => section.Labels.Any(label => label is CaseSwitchLabelSyntax or CasePatternSwitchLabelSyntax),
            _ => false
        };
    }

    private static bool IsNestedMethodBoundary(SyntaxNode root, SyntaxNode child)
    {
        if (ReferenceEquals(root, child))
        {
            return false;
        }

        return child is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax;
    }

    private static bool HasStaticOrGlobalAccess(SyntaxNode node)
    {
        return node.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Any(access => StaticOrGlobalAccessTypes.Contains(SimplifyTypeName(access.Expression.ToString())));
    }

    private static string CreateFullTypeName(BaseTypeDeclarationSyntax declaration, string name)
    {
        var containingTypes = declaration.Ancestors()
            .OfType<BaseTypeDeclarationSyntax>()
            .Reverse()
            .Select(type => type.Identifier.Text)
            .Where(identifier => !string.IsNullOrWhiteSpace(identifier))
            .ToArray();

        var namespaceName = GetNamespaceName(declaration);
        var typeName = containingTypes.Length == 0
            ? name
            : $"{string.Join(".", containingTypes)}.{name}";

        return string.IsNullOrWhiteSpace(namespaceName)
            ? typeName
            : $"{namespaceName}.{typeName}";
    }

    private static string GetNamespaceName(SyntaxNode node)
    {
        var fileScoped = node.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScoped is not null)
        {
            return fileScoped.Name.ToString();
        }

        return node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? string.Empty;
    }

    private static int GetLineNumber(SyntaxTree syntaxTree, SyntaxNode node)
    {
        return syntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }

    private static int GetLineNumber(SyntaxTree syntaxTree, SyntaxToken token)
    {
        return syntaxTree.GetLineSpan(token.Span).StartLinePosition.Line + 1;
    }

    private static string GetAccessibility(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(SyntaxKind.PublicKeyword)) return "public";
        if (modifiers.Any(SyntaxKind.ProtectedKeyword)) return "protected";
        if (modifiers.Any(SyntaxKind.PrivateKeyword)) return "private";
        if (modifiers.Any(SyntaxKind.InternalKeyword)) return "internal";
        return "private";
    }

    private static bool IsLikelyInterfaceType(string typeName)
    {
        var simplified = SimplifyTypeName(typeName);
        return simplified.Length > 1 && simplified.StartsWith("I", StringComparison.Ordinal) && char.IsUpper(simplified[1]);
    }

    private static string SimplifyTypeName(string typeName)
    {
        var trimmed = typeName.Trim().TrimEnd('?');
        var genericTick = trimmed.IndexOf('<', StringComparison.Ordinal);
        if (genericTick >= 0)
        {
            trimmed = trimmed[..genericTick];
        }

        var lastDot = trimmed.LastIndexOf('.');
        return lastDot >= 0 ? trimmed[(lastDot + 1)..] : trimmed;
    }

    private static string FindContainingMemberName(SyntaxNode node)
    {
        return node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ??
               node.Ancestors().OfType<ConstructorDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ??
               node.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ??
               "<type>";
    }

    private static string CreateMethodSignature(MethodDeclarationSyntax method)
    {
        var parameters = string.Join(", ", method.ParameterList.Parameters.Select(parameter => parameter.ToString()));
        return $"{method.ReturnType} {method.Identifier.Text}({parameters})";
    }

    private static string GetEvidence(SyntaxNode node)
    {
        return node.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? node.Kind().ToString();
    }

    private static bool ContainsAny(string value, IEnumerable<string> terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<RecommendationBlocker> CreateRuntimeBlockers(string reason) =>
    [
        new RecommendationBlocker(reason, "Static analysis only; runtime wiring and callers were not resolved.")
    ];

    private static string FirstEvidence(IEnumerable<MissingOrWeakSeam> seams, string contains)
    {
        return seams.FirstOrDefault(seam => seam.Kind.Contains(contains, StringComparison.OrdinalIgnoreCase))?.Evidence ?? string.Empty;
    }

    private sealed record SourceFileInfo(
        string ProjectName,
        string FullPath,
        SyntaxTree SyntaxTree,
        CompilationUnitSyntax Root);
}
