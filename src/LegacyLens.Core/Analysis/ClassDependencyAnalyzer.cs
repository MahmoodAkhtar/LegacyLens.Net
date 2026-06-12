using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LegacyLens.Core.Analysis;

public sealed class ClassDependencyAnalyzer
{
    private static readonly HashSet<string> IgnoredTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "void", "string", "object", "bool", "byte", "short", "int", "long", "float", "double", "decimal", "char",
        "DateTime", "DateOnly", "TimeOnly", "Task", "ValueTask", "IEnumerable", "IReadOnlyList", "IReadOnlyCollection",
        "List", "Dictionary", "HashSet", "Array", "Nullable", "Action", "Func"
    };

    private static readonly HashSet<string> StaticAccessTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ConfigurationManager",
        "DateTime",
        "File",
        "Directory",
        "Environment",
        "HttpContext",
        "DependencyResolver",
        "ControllerBuilder",
        "GlobalConfiguration",
        "RouteTable",
        "GlobalFilters",
        "ModelBinders",
        "ValueProviderFactories",
        "SmtpClient",
        "HttpClient",
        "SqlConnection"
    };

    public ClassDependencyReport Analyze(IReadOnlyCollection<DiscoveredProject> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var fileInventory = new ScanFileInventoryBuilder().Build(projects);

        return Analyze(fileInventory);
    }

    public ClassDependencyReport Analyze(ScanFileInventory fileInventory)
    {
        ArgumentNullException.ThrowIfNull(fileInventory);

        var sourceFiles = fileInventory.CSharpFiles
            .Select(ParseSourceFile)
            .ToArray();

        var discoveredTypes = sourceFiles
            .SelectMany(DiscoverTypes)
            .ToArray();

        var knownTypes = discoveredTypes
            .GroupBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var dependencies = sourceFiles
            .SelectMany(file => DiscoverDependencies(file, discoveredTypes, knownTypes))
            .GroupBy(CreateDependencyKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(dependency => dependency.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dependency => dependency.SourceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dependency => dependency.TargetType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dependency => dependency.Kind)
            .ThenBy(dependency => dependency.LineNumber)
            .ToArray();

        var concerns = dependencies
            .SelectMany(CreateConcerns)
            .GroupBy(CreateConcernKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(concern => concern.Severity)
            .ThenBy(concern => concern.SourceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(concern => concern.TargetType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var hotspots = CreateHotspots(discoveredTypes, dependencies, concerns);

        return new ClassDependencyReport(
            discoveredTypes,
            dependencies,
            concerns,
            hotspots,
            sourceFiles.Length);
    }

    private static SourceFileInfo ParseSourceFile(ScanFile file)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(file.Content);
        var root = syntaxTree.GetCompilationUnitRoot();

        return new SourceFileInfo(
            file.ProjectName,
            file.FullPath,
            file.Content,
            syntaxTree,
            root);
    }

    private static IEnumerable<DiscoveredType> DiscoverTypes(SourceFileInfo sourceFile)
    {
        if (string.IsNullOrWhiteSpace(sourceFile.Source))
        {
            yield break;
        }

        foreach (var declaration in sourceFile.Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            if (!IsSupportedTypeDeclaration(declaration))
            {
                continue;
            }

            var name = declaration.Identifier.Text;

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var fullName = CreateFullTypeName(declaration, name);
            var kind = ParseTypeKind(declaration);

            yield return new DiscoveredType(
                name,
                fullName,
                kind,
                sourceFile.ProjectName,
                sourceFile.SourcePath,
                GetLineNumber(sourceFile.SyntaxTree, declaration));
        }
    }

    private static IEnumerable<ClassDependency> DiscoverDependencies(
        SourceFileInfo sourceFile,
        IReadOnlyCollection<DiscoveredType> discoveredTypes,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        if (string.IsNullOrWhiteSpace(sourceFile.Source))
        {
            yield break;
        }

        foreach (var typeDeclaration in sourceFile.Root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var sourceType = FindDiscoveredType(sourceFile, discoveredTypes, typeDeclaration);

            if (sourceType is null)
            {
                continue;
            }

            foreach (var dependency in DiscoverTypeDependencies(sourceFile, sourceType, typeDeclaration, knownTypes))
            {
                yield return dependency;
            }
        }
    }

    private static IEnumerable<ClassDependency> DiscoverTypeDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        foreach (var dependency in DiscoverBaseTypeDependencies(sourceFile, sourceType, typeDeclaration, knownTypes))
        {
            yield return dependency;
        }

        foreach (var dependency in DiscoverAttributeDependencies(sourceFile, sourceType, typeDeclaration, knownTypes))
        {
            yield return dependency;
        }

        foreach (var dependency in DiscoverFieldDependencies(sourceFile, sourceType, typeDeclaration, knownTypes))
        {
            yield return dependency;
        }

        foreach (var dependency in DiscoverPropertyDependencies(sourceFile, sourceType, typeDeclaration, knownTypes))
        {
            yield return dependency;
        }

        foreach (var dependency in DiscoverConstructorDependencies(sourceFile, sourceType, typeDeclaration, knownTypes))
        {
            yield return dependency;
        }

        foreach (var dependency in DiscoverMethodDependencies(sourceFile, sourceType, typeDeclaration, knownTypes))
        {
            yield return dependency;
        }

        foreach (var dependency in DiscoverLocalVariableDependencies(sourceFile, sourceType, typeDeclaration, knownTypes))
        {
            yield return dependency;
        }

        foreach (var dependency in DiscoverObjectCreationDependencies(sourceFile, sourceType, typeDeclaration, knownTypes))
        {
            yield return dependency;
        }

        foreach (var dependency in DiscoverStaticAccessDependencies(sourceFile, sourceType, typeDeclaration))
        {
            yield return dependency;
        }

        foreach (var dependency in DiscoverGenericArgumentDependencies(sourceFile, sourceType, typeDeclaration, knownTypes))
        {
            yield return dependency;
        }
    }

    private static IEnumerable<ClassDependency> DiscoverBaseTypeDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        if (typeDeclaration.BaseList is null)
        {
            yield break;
        }

        foreach (var baseType in typeDeclaration.BaseList.Types)
        {
            var targetType = SimplifyTypeName(baseType.Type.ToString());

            if (!ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: true))
            {
                continue;
            }

            var kind = IsInterfaceType(targetType, knownTypes)
                ? ClassDependencyKind.InterfaceImplementation
                : ClassDependencyKind.BaseClass;

            yield return CreateDependency(
                sourceFile,
                sourceType,
                targetType,
                kind,
                GetLineNumber(sourceFile.SyntaxTree, baseType),
                baseType.ToString());
        }
    }

    private static IEnumerable<ClassDependency> DiscoverAttributeDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        foreach (var attribute in OwnNodes<AttributeSyntax>(typeDeclaration))
        {
            var targetType = NormaliseAttributeName(attribute.Name.ToString());

            if (!ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: true))
            {
                continue;
            }

            yield return CreateDependency(
                sourceFile,
                sourceType,
                targetType,
                ClassDependencyKind.Attribute,
                GetLineNumber(sourceFile.SyntaxTree, attribute),
                $"[{attribute}]");
        }
    }

    private static IEnumerable<ClassDependency> DiscoverFieldDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        foreach (var field in OwnNodes<FieldDeclarationSyntax>(typeDeclaration))
        {
            var targetType = SimplifyTypeName(field.Declaration.Type.ToString());

            if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: false))
            {
                yield return CreateDependency(
                    sourceFile,
                    sourceType,
                    targetType,
                    ClassDependencyKind.Field,
                    GetLineNumber(sourceFile.SyntaxTree, field),
                    GetDeclarationEvidence(field));
            }

            foreach (var variable in field.Declaration.Variables)
            {
                if (variable.Initializer?.Value is not ImplicitObjectCreationExpressionSyntax implicitNew)
                {
                    continue;
                }

                if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: true))
                {
                    yield return CreateDependency(
                        sourceFile,
                        sourceType,
                        targetType,
                        ClassDependencyKind.ObjectCreation,
                        GetLineNumber(sourceFile.SyntaxTree, implicitNew),
                        "new()");
                }
            }
        }
    }

    private static IEnumerable<ClassDependency> DiscoverPropertyDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        foreach (var property in OwnNodes<PropertyDeclarationSyntax>(typeDeclaration))
        {
            var targetType = SimplifyTypeName(property.Type.ToString());

            if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: false))
            {
                yield return CreateDependency(
                    sourceFile,
                    sourceType,
                    targetType,
                    ClassDependencyKind.Property,
                    GetLineNumber(sourceFile.SyntaxTree, property),
                    GetPropertyEvidence(property));
            }

            if (property.Initializer?.Value is ImplicitObjectCreationExpressionSyntax implicitNew &&
                ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: true))
            {
                yield return CreateDependency(
                    sourceFile,
                    sourceType,
                    targetType,
                    ClassDependencyKind.ObjectCreation,
                    GetLineNumber(sourceFile.SyntaxTree, implicitNew),
                    "new()");
            }
        }
    }

    private static IEnumerable<ClassDependency> DiscoverConstructorDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        foreach (var constructor in OwnNodes<ConstructorDeclarationSyntax>(typeDeclaration))
        {
            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                var parameterType = SimplifyTypeName(parameter.Type?.ToString() ?? string.Empty);

                if (ShouldIncludeTarget(parameterType, knownTypes, includeKnownFrameworkType: false))
                {
                    yield return CreateDependency(
                        sourceFile,
                        sourceType,
                        parameterType,
                        ClassDependencyKind.ConstructorParameter,
                        GetLineNumber(sourceFile.SyntaxTree, constructor),
                        GetConstructorEvidence(constructor));
                }
            }
        }
    }

    private static IEnumerable<ClassDependency> DiscoverMethodDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        foreach (var method in OwnNodes<MethodDeclarationSyntax>(typeDeclaration))
        {
            var returnType = SimplifyTypeName(method.ReturnType.ToString());

            if (ShouldIncludeTarget(returnType, knownTypes, includeKnownFrameworkType: false))
            {
                yield return CreateDependency(
                    sourceFile,
                    sourceType,
                    returnType,
                    ClassDependencyKind.ReturnType,
                    GetLineNumber(sourceFile.SyntaxTree, method),
                    GetMethodEvidence(method));
            }

            foreach (var parameter in method.ParameterList.Parameters)
            {
                var parameterType = SimplifyTypeName(parameter.Type?.ToString() ?? string.Empty);

                if (ShouldIncludeTarget(parameterType, knownTypes, includeKnownFrameworkType: false))
                {
                    yield return CreateDependency(
                        sourceFile,
                        sourceType,
                        parameterType,
                        ClassDependencyKind.MethodParameter,
                        GetLineNumber(sourceFile.SyntaxTree, method),
                        GetMethodEvidence(method));
                }
            }
        }
    }

    private static IEnumerable<ClassDependency> DiscoverLocalVariableDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        foreach (var local in OwnNodes<LocalDeclarationStatementSyntax>(typeDeclaration))
        {
            var targetType = SimplifyTypeName(local.Declaration.Type.ToString());

            if (!local.Declaration.Type.IsVar &&
                ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: false))
            {
                yield return CreateDependency(
                    sourceFile,
                    sourceType,
                    targetType,
                    ClassDependencyKind.LocalVariable,
                    GetLineNumber(sourceFile.SyntaxTree, local),
                    GetLocalDeclarationEvidence(local));
            }

            foreach (var variable in local.Declaration.Variables)
            {
                if (variable.Initializer?.Value is not ImplicitObjectCreationExpressionSyntax implicitNew)
                {
                    continue;
                }

                if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: true))
                {
                    yield return CreateDependency(
                        sourceFile,
                        sourceType,
                        targetType,
                        ClassDependencyKind.ObjectCreation,
                        GetLineNumber(sourceFile.SyntaxTree, implicitNew),
                        "new()");
                }
            }
        }
    }

    private static IEnumerable<ClassDependency> DiscoverObjectCreationDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        foreach (var objectCreation in OwnNodes<ObjectCreationExpressionSyntax>(typeDeclaration))
        {
            var targetType = SimplifyTypeName(objectCreation.Type.ToString());

            if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: true))
            {
                yield return CreateDependency(
                    sourceFile,
                    sourceType,
                    targetType,
                    ClassDependencyKind.ObjectCreation,
                    GetLineNumber(sourceFile.SyntaxTree, objectCreation),
                    GetObjectCreationEvidence(objectCreation));
            }
        }
    }

    private static IEnumerable<ClassDependency> DiscoverStaticAccessDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration)
    {
        foreach (var memberAccess in OwnNodes<MemberAccessExpressionSyntax>(typeDeclaration))
        {
            var targetType = SimplifyTypeName(memberAccess.Expression.ToString());

            if (!StaticAccessTypeNames.Contains(targetType))
            {
                continue;
            }

            yield return CreateDependency(
                sourceFile,
                sourceType,
                targetType,
                ClassDependencyKind.StaticMemberAccess,
                GetLineNumber(sourceFile.SyntaxTree, memberAccess),
                memberAccess.ToString());
        }
    }

    private static IEnumerable<ClassDependency> DiscoverGenericArgumentDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        TypeDeclarationSyntax typeDeclaration,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        foreach (var genericName in OwnNodes<GenericNameSyntax>(typeDeclaration))
        {
            foreach (var argument in genericName.TypeArgumentList.Arguments)
            {
                var targetType = SimplifyTypeName(argument.ToString());

                if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: false))
                {
                    yield return CreateDependency(
                        sourceFile,
                        sourceType,
                        targetType,
                        ClassDependencyKind.GenericTypeArgument,
                        GetLineNumber(sourceFile.SyntaxTree, argument),
                        genericName.ToString());
                }
            }
        }
    }

    private static IEnumerable<TNode> OwnNodes<TNode>(TypeDeclarationSyntax owner)
        where TNode : SyntaxNode
    {
        return owner
            .DescendantNodes()
            .OfType<TNode>()
            .Where(node => node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() == owner);
    }

    private static DiscoveredType? FindDiscoveredType(
        SourceFileInfo sourceFile,
        IEnumerable<DiscoveredType> discoveredTypes,
        TypeDeclarationSyntax typeDeclaration)
    {
        var lineNumber = GetLineNumber(sourceFile.SyntaxTree, typeDeclaration);

        return discoveredTypes.FirstOrDefault(type =>
            type.LineNumber == lineNumber &&
            type.SourcePath.Equals(sourceFile.SourcePath, StringComparison.OrdinalIgnoreCase) &&
            type.Name.Equals(typeDeclaration.Identifier.Text, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSupportedTypeDeclaration(BaseTypeDeclarationSyntax declaration)
    {
        return declaration is ClassDeclarationSyntax or InterfaceDeclarationSyntax or RecordDeclarationSyntax
            or StructDeclarationSyntax or EnumDeclarationSyntax;
    }

    private static string CreateFullTypeName(BaseTypeDeclarationSyntax declaration, string name)
    {
        var namespaceName = string.Join(
            ".",
            declaration
                .Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Reverse()
                .Select(ns => ns.Name.ToString())
                .Where(part => !string.IsNullOrWhiteSpace(part)));

        var containingTypes = declaration
            .Ancestors()
            .OfType<BaseTypeDeclarationSyntax>()
            .Reverse()
            .Select(type => type.Identifier.Text)
            .Where(part => !string.IsNullOrWhiteSpace(part));

        var typeName = string.Join(".", containingTypes.Append(name));

        return string.IsNullOrWhiteSpace(namespaceName)
            ? typeName
            : $"{namespaceName}.{typeName}";
    }

    private static ClassDiscoveredTypeKind ParseTypeKind(BaseTypeDeclarationSyntax declaration)
    {
        return declaration switch
        {
            InterfaceDeclarationSyntax => ClassDiscoveredTypeKind.Interface,
            RecordDeclarationSyntax => ClassDiscoveredTypeKind.Record,
            StructDeclarationSyntax => ClassDiscoveredTypeKind.Struct,
            EnumDeclarationSyntax => ClassDiscoveredTypeKind.Enum,
            _ => ClassDiscoveredTypeKind.Class
        };
    }

    private static ClassDependency CreateDependency(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        string targetType,
        ClassDependencyKind kind,
        int lineNumber,
        string evidence)
    {
        return new ClassDependency(
            sourceFile.ProjectName,
            sourceFile.SourcePath,
            lineNumber,
            sourceType.Name,
            SimplifyTypeName(targetType),
            kind,
            TrimEvidence(evidence));
    }

    private static IEnumerable<ClassDependencyConcern> CreateConcerns(ClassDependency dependency)
    {
        if (dependency.Kind == ClassDependencyKind.ObjectCreation)
        {
            yield return new ClassDependencyConcern(
                IsInfrastructureType(dependency.TargetType)
                    ? ClassDependencyConcernSeverity.High
                    : ClassDependencyConcernSeverity.High,
                dependency.SourceType,
                dependency.TargetType,
                dependency.Kind,
                dependency.ProjectName,
                dependency.SourcePath,
                dependency.LineNumber,
                dependency.Evidence,
                "Concrete construction hides the dependency and can make testing, replacement, and migration harder.",
                "Consider constructor injection behind an interface or an explicit factory if the dependency has behaviour or infrastructure impact.");
        }

        if (dependency.Kind == ClassDependencyKind.StaticMemberAccess)
        {
            yield return new ClassDependencyConcern(
                IsLegacyOrGlobalStatic(dependency.TargetType)
                    ? ClassDependencyConcernSeverity.Medium
                    : ClassDependencyConcernSeverity.Medium,
                dependency.SourceType,
                dependency.TargetType,
                dependency.Kind,
                dependency.ProjectName,
                dependency.SourcePath,
                dependency.LineNumber,
                dependency.Evidence,
                "Static/global access can hide runtime dependencies and may need review during migration or test isolation.",
                CreateStaticAccessRecommendation(dependency.TargetType));
        }

        if (dependency.Kind is ClassDependencyKind.Field or ClassDependencyKind.Property &&
            LooksConcrete(dependency.TargetType))
        {
            yield return new ClassDependencyConcern(
                ClassDependencyConcernSeverity.Medium,
                dependency.SourceType,
                dependency.TargetType,
                dependency.Kind,
                dependency.ProjectName,
                dependency.SourcePath,
                dependency.LineNumber,
                dependency.Evidence,
                "Concrete member dependencies increase coupling between types and may make substitution harder.",
                "Review whether this dependency should be represented by an interface, abstraction, or value object.");
        }

        if (dependency.Kind == ClassDependencyKind.ConstructorParameter && LooksConcrete(dependency.TargetType))
        {
            yield return new ClassDependencyConcern(
                ClassDependencyConcernSeverity.Medium,
                dependency.SourceType,
                dependency.TargetType,
                dependency.Kind,
                dependency.ProjectName,
                dependency.SourcePath,
                dependency.LineNumber,
                dependency.Evidence,
                "Constructor injection is visible, but injecting a concrete class still couples the source type to an implementation.",
                "Review whether an interface or stable abstraction is useful before refactoring or migration.");
        }

        if (dependency.Kind == ClassDependencyKind.BaseClass && LooksConcrete(dependency.TargetType))
        {
            yield return new ClassDependencyConcern(
                ClassDependencyConcernSeverity.Medium,
                dependency.SourceType,
                dependency.TargetType,
                dependency.Kind,
                dependency.ProjectName,
                dependency.SourcePath,
                dependency.LineNumber,
                dependency.Evidence,
                "Inheritance from a concrete base type can couple behaviour and lifecycle in ways that are harder to migrate than composition.",
                "Review whether the base class contains framework, infrastructure, or shared mutable behaviour before migration.");
        }

        if (dependency.Kind == ClassDependencyKind.Attribute && IsFrameworkAttribute(dependency.TargetType))
        {
            yield return new ClassDependencyConcern(
                ClassDependencyConcernSeverity.Medium,
                dependency.SourceType,
                dependency.TargetType,
                dependency.Kind,
                dependency.ProjectName,
                dependency.SourcePath,
                dependency.LineNumber,
                dependency.Evidence,
                "Framework-specific attributes can represent routing, filters, authorization, serialization, or service behaviour that may need migration mapping.",
                "Review the target framework equivalent and confirm behaviour is preserved during migration.");
        }
    }

    private static IReadOnlyList<ClassCouplingHotspot> CreateHotspots(
        IEnumerable<DiscoveredType> types,
        IReadOnlyCollection<ClassDependency> dependencies,
        IReadOnlyCollection<ClassDependencyConcern> concerns)
    {
        return types
            .Select(type =>
            {
                var outgoing = dependencies
                    .Where(dependency => dependency.SourceType.Equals(type.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(dependency => dependency.TargetType)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                var incoming = dependencies
                    .Where(dependency => dependency.TargetType.Equals(type.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(dependency => dependency.SourceType)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                var concernCount = concerns.Count(concern =>
                    concern.SourceType.Equals(type.Name, StringComparison.OrdinalIgnoreCase) ||
                    concern.TargetType.Equals(type.Name, StringComparison.OrdinalIgnoreCase));

                return new ClassCouplingHotspot(
                    type.Name,
                    type.ProjectName,
                    outgoing,
                    incoming,
                    concernCount,
                    CreateHotspotNotes(outgoing, incoming, concernCount));
            })
            .Where(hotspot => hotspot.OutgoingDependencyCount >= 3 || hotspot.IncomingDependencyCount >= 3 ||
                              hotspot.ConcernCount > 0)
            .OrderByDescending(hotspot => hotspot.ConcernCount)
            .ThenByDescending(hotspot => hotspot.OutgoingDependencyCount + hotspot.IncomingDependencyCount)
            .ThenBy(hotspot => hotspot.Type, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();
    }

    private static string CreateHotspotNotes(int outgoing, int incoming, int concerns)
    {
        if (concerns > 0)
        {
            return "Coupling concern evidence found.";
        }

        if (outgoing >= incoming)
        {
            return "Several outgoing source-level dependencies found.";
        }

        return "Several incoming source-level dependencies found.";
    }

    private static bool ShouldIncludeTarget(
        string targetType,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes,
        bool includeKnownFrameworkType)
    {
        if (string.IsNullOrWhiteSpace(targetType))
        {
            return false;
        }

        if (IgnoredTypeNames.Contains(targetType))
        {
            return false;
        }

        if (knownTypes.ContainsKey(targetType))
        {
            return true;
        }

        return includeKnownFrameworkType && IsKnownFrameworkOrInfrastructureType(targetType);
    }

    private static bool IsInterfaceType(string targetType, IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        if (knownTypes.TryGetValue(targetType, out var discoveredType))
        {
            return discoveredType.Kind == ClassDiscoveredTypeKind.Interface;
        }

        return targetType.StartsWith("I", StringComparison.Ordinal) && targetType.Length > 1 &&
               char.IsUpper(targetType[1]);
    }

    private static bool LooksConcrete(string targetType) =>
        !targetType.StartsWith("I", StringComparison.Ordinal) || targetType.Length <= 1 || !char.IsUpper(targetType[1]);

    private static bool IsKnownFrameworkOrInfrastructureType(string typeName) =>
        IsLegacyOrGlobalStatic(typeName) || IsInfrastructureType(typeName) || IsFrameworkAttribute(typeName);

    private static bool IsLegacyOrGlobalStatic(string typeName) =>
        MatchesAny(typeName, "ConfigurationManager", "DateTime", "File", "Directory", "Environment", "HttpContext",
            "DependencyResolver", "ControllerBuilder", "GlobalConfiguration", "RouteTable", "GlobalFilters",
            "ModelBinders", "ValueProviderFactories");

    private static bool IsInfrastructureType(string typeName) =>
        MatchesAny(typeName, "SmtpClient", "HttpClient", "SqlConnection", "DbConnection", "SqlCommand", "FileStream");

    private static bool IsFrameworkAttribute(string typeName) =>
        typeName.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase) ||
        MatchesAny(typeName, "Route", "RoutePrefix", "HttpGet", "HttpPost", "Authorize", "AllowAnonymous",
            "ValidateAntiForgeryToken", "ServiceContract", "OperationContract");

    private static string CreateStaticAccessRecommendation(string targetType)
    {
        if (targetType.Equals("ConfigurationManager", StringComparison.OrdinalIgnoreCase))
        {
            return "Consider migrating access behind IConfiguration, options binding, or a configuration abstraction.";
        }

        if (targetType.Equals("DateTime", StringComparison.OrdinalIgnoreCase))
        {
            return
                "Consider an injectable clock abstraction where deterministic testing or time-zone behaviour matters.";
        }

        return
            "Review whether this static dependency should remain global or move behind an abstraction during migration.";
    }

    private static string SimplifyTypeName(string value)
    {
        var typeName = value
            .Trim()
            .Replace("global::", string.Empty, StringComparison.Ordinal)
            .Replace("[]", string.Empty, StringComparison.Ordinal)
            .Trim()
            .TrimEnd('?');

        if (typeName.StartsWith("System.Nullable<", StringComparison.OrdinalIgnoreCase))
        {
            typeName = typeName["System.Nullable<".Length..].TrimEnd('>');
        }

        var genericIndex = typeName.IndexOf('<', StringComparison.Ordinal);
        if (genericIndex >= 0)
        {
            typeName = typeName[..genericIndex];
        }

        var lastDot = typeName.LastIndexOf(".", StringComparison.Ordinal);
        if (lastDot >= 0)
        {
            typeName = typeName[(lastDot + 1)..];
        }

        return typeName.Trim();
    }

    private static string NormaliseAttributeName(string value)
    {
        var name = SimplifyTypeName(value);
        return name.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase)
            ? name
            : $"{name}Attribute";
    }

    private static int GetLineNumber(SyntaxTree syntaxTree, SyntaxNode node) =>
        syntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;

    private static string GetDeclarationEvidence(FieldDeclarationSyntax field)
    {
        var firstVariable = field.Declaration.Variables.FirstOrDefault()?.Identifier.Text;

        if (string.IsNullOrWhiteSpace(firstVariable))
        {
            return field.ToString();
        }

        return $"{field.Modifiers} {field.Declaration.Type} {firstVariable}".Trim();
    }

    private static string GetPropertyEvidence(PropertyDeclarationSyntax property) =>
        $"{property.Modifiers} {property.Type} {property.Identifier} {{".Trim();

    private static string GetConstructorEvidence(ConstructorDeclarationSyntax constructor) =>
        $"{constructor.Modifiers} {constructor.Identifier}{constructor.ParameterList}".Trim();

    private static string GetMethodEvidence(MethodDeclarationSyntax method) =>
        $"{method.Modifiers} {method.ReturnType} {method.Identifier}{method.TypeParameterList}{method.ParameterList}".Trim();

    private static string GetLocalDeclarationEvidence(LocalDeclarationStatementSyntax local) =>
        local.Declaration.ToString();

    private static string GetObjectCreationEvidence(ObjectCreationExpressionSyntax objectCreation)
    {
        var argumentList = objectCreation.ArgumentList?.ToString() ?? "()";
        return $"new {objectCreation.Type}{argumentList}";
    }

    private static string TrimEvidence(string evidence)
    {
        var trimmed = evidence.Trim();
        return trimmed.Length <= 160 ? trimmed : trimmed[..157] + "...";
    }

    private static bool MatchesAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Equals(candidate, StringComparison.OrdinalIgnoreCase));

    private static string CreateDependencyKey(ClassDependency dependency) =>
        string.Join("|", dependency.ProjectName, dependency.SourcePath, dependency.LineNumber, dependency.SourceType,
            dependency.TargetType, dependency.Kind, dependency.Evidence);

    private static string CreateConcernKey(ClassDependencyConcern concern) =>
        string.Join("|", concern.SourcePath, concern.LineNumber, concern.SourceType, concern.TargetType,
            concern.DependencyKind, concern.Evidence);

    private sealed record SourceFileInfo(
        string ProjectName,
        string SourcePath,
        string Source,
        SyntaxTree SyntaxTree,
        CompilationUnitSyntax Root);
}
