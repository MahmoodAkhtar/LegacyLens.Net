using System.Text.RegularExpressions;
using LegacyLens.Core.Files;

namespace LegacyLens.Core.Analysis;

public sealed class ClassDependencyAnalyzer
{
    private static readonly Regex NamespaceRegex = new(@"^\s*namespace\s+(?<name>[A-Za-z_][\w.]*)(\s*;|\s*\{)?",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex TypeRegex =
        new(
            @"\b(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly)\s+)*(?<kind>class|interface|record|struct|enum)\s+(?<name>[A-Za-z_][\w]*)(?<tail>[^\{;]*)",
            RegexOptions.Compiled);

    private static readonly Regex AttributeRegex =
        new(@"\[(?<name>[A-Za-z_][\w.]*)(Attribute)?(?:\([^\]]*\))?\]", RegexOptions.Compiled);

    private static readonly Regex NewRegex = new(@"\bnew\s+(?<type>[A-Za-z_][\w.<>]*)\s*\(", RegexOptions.Compiled);
    private static readonly Regex TargetTypedNewRegex = new(@"=\s*new\s*\(", RegexOptions.Compiled);

    private static readonly Regex StaticAccessRegex = new(
        @"\b(?<type>ConfigurationManager|DateTime|File|Directory|Environment|HttpContext|DependencyResolver|ControllerBuilder|GlobalConfiguration|RouteTable|GlobalFilters|ModelBinders|ValueProviderFactories|SmtpClient|HttpClient|SqlConnection)\s*\.",
        RegexOptions.Compiled);

    private static readonly Regex FieldRegex =
        new(
            @"^\s*(?:private|protected|internal|public)\s+(?:static\s+)?(?:readonly\s+)?(?<type>[A-Za-z_][\w.<>?\[\],]*)\s+[A-Za-z_][\w]*\s*(?:=|;)",
            RegexOptions.Compiled);

    private static readonly Regex PropertyRegex =
        new(
            @"^\s*(?:public|internal|protected|private)\s+(?:static\s+)?(?<type>[A-Za-z_][\w.<>?\[\],]*)\s+[A-Za-z_][\w]*\s*\{",
            RegexOptions.Compiled);

    private static readonly Regex MethodRegex =
        new(
            @"^\s*(?:public|internal|protected|private)\s+(?:static\s+)?(?<return>[A-Za-z_][\w.<>?\[\],]*)\s+(?<name>[A-Za-z_][\w]*)\s*\((?<params>[^)]*)\)",
            RegexOptions.Compiled);

    private static readonly Regex LocalRegex = new(@"^\s*(?:var|(?<type>[A-Za-z_][\w.<>?\[\],]*))\s+[A-Za-z_][\w]*\s*=",
        RegexOptions.Compiled);

    private static readonly Regex ConstructorRegex =
        new(@"^\s*(?:public|internal|protected|private)\s+(?<name>[A-Za-z_][\w]*)\s*\((?<params>[^)]*)\)",
            RegexOptions.Compiled);

    private static readonly HashSet<string> IgnoredTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "void", "string", "object", "bool", "byte", "short", "int", "long", "float", "double", "decimal", "char",
        "DateTime", "DateOnly", "TimeOnly", "Task", "ValueTask", "IEnumerable", "IReadOnlyList", "IReadOnlyCollection",
        "List", "Dictionary", "HashSet", "Array", "Nullable", "Action", "Func"
    };
    
    public ClassDependencyReport Analyze(ScanFileInventory fileInventory)
    {
        ArgumentNullException.ThrowIfNull(fileInventory);

        var sourceFiles = fileInventory.CSharpFiles
            .Select(file => new SourceFileInfo(
                file.ProjectName,
                file.FullPath,
                file.Content))
            .ToArray();

        var discoveredTypes = DiscoverTypes(sourceFiles).ToArray();

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
    
    private static IEnumerable<DiscoveredType> DiscoverTypes(IEnumerable<SourceFileInfo> sourceFiles)
    {
        foreach (var sourceFile in sourceFiles)
        {
            if (string.IsNullOrWhiteSpace(sourceFile.Source))
            {
                continue;
            }

            var namespaceName = NamespaceRegex.Match(sourceFile.Source) is { Success: true } matched
                ? matched.Groups["name"].Value
                : string.Empty;

            foreach (Match match in TypeRegex.Matches(sourceFile.Source))
            {
                var name = match.Groups["name"].Value;
                var kind = ParseTypeKind(match.Groups["kind"].Value);
                var fullName = string.IsNullOrWhiteSpace(namespaceName) ? name : $"{namespaceName}.{name}";

                yield return new DiscoveredType(
                    name,
                    fullName,
                    kind,
                    sourceFile.ProjectName,
                    sourceFile.SourcePath,
                    GetLineNumber(sourceFile.Source, match.Index));
            }
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

        var sourceTypesInFile = discoveredTypes
            .Where(type => string.Equals(type.SourcePath, sourceFile.SourcePath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(type => type.LineNumber)
            .ToArray();

        if (sourceTypesInFile.Length == 0)
        {
            yield break;
        }

        var lines = SplitLines(sourceFile.Source);
        var currentType = sourceTypesInFile.First();

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i];
            var trimmed = line.Trim();

            var matchingType = sourceTypesInFile.LastOrDefault(type => type.LineNumber <= lineNumber);
            if (matchingType is not null)
            {
                currentType = matchingType;
            }

            foreach (var dependency in DiscoverLineDependencies(sourceFile, currentType, line, lineNumber, knownTypes))
            {
                yield return dependency;
            }
        }
    }

    private static IEnumerable<ClassDependency> DiscoverLineDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        string line,
        int lineNumber,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        foreach (var dependency in
                 DiscoverDeclarationDependencies(sourceFile, sourceType, line, lineNumber, knownTypes))
        {
            yield return dependency;
        }

        foreach (Match match in AttributeRegex.Matches(line))
        {
            var targetType = NormaliseAttributeName(match.Groups["name"].Value);

            if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: true))
            {
                yield return CreateDependency(sourceFile, sourceType, targetType, ClassDependencyKind.Attribute,
                    lineNumber, match.Value);
            }
        }

        foreach (Match match in NewRegex.Matches(line))
        {
            var targetType = SimplifyTypeName(match.Groups["type"].Value);

            if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: true))
            {
                yield return CreateDependency(sourceFile, sourceType, targetType, ClassDependencyKind.ObjectCreation,
                    lineNumber, match.Value);
            }
        }

        var fieldMatch = FieldRegex.Match(line);
        if (fieldMatch.Success && TargetTypedNewRegex.IsMatch(line))
        {
            var targetType = SimplifyTypeName(fieldMatch.Groups["type"].Value);

            if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: true))
            {
                yield return CreateDependency(sourceFile, sourceType, targetType, ClassDependencyKind.ObjectCreation,
                    lineNumber, "new()");
            }
        }

        foreach (Match match in StaticAccessRegex.Matches(line))
        {
            var targetType = SimplifyTypeName(match.Groups["type"].Value);
            yield return CreateDependency(sourceFile, sourceType, targetType, ClassDependencyKind.StaticMemberAccess,
                lineNumber, match.Value);
        }

        foreach (var genericArgument in ExtractGenericArguments(line))
        {
            if (ShouldIncludeTarget(genericArgument, knownTypes, includeKnownFrameworkType: false))
            {
                yield return CreateDependency(sourceFile, sourceType, genericArgument,
                    ClassDependencyKind.GenericTypeArgument, lineNumber, line.Trim());
            }
        }
    }

    private static IEnumerable<ClassDependency> DiscoverDeclarationDependencies(
        SourceFileInfo sourceFile,
        DiscoveredType sourceType,
        string line,
        int lineNumber,
        IReadOnlyDictionary<string, DiscoveredType> knownTypes)
    {
        var typeMatch = TypeRegex.Match(line);
        if (typeMatch.Success)
        {
            foreach (var inheritedType in ExtractInheritedTypes(typeMatch.Groups["tail"].Value))
            {
                if (!ShouldIncludeTarget(inheritedType, knownTypes, includeKnownFrameworkType: true))
                {
                    continue;
                }

                var kind = IsInterfaceType(inheritedType, knownTypes)
                    ? ClassDependencyKind.InterfaceImplementation
                    : ClassDependencyKind.BaseClass;

                yield return CreateDependency(sourceFile, sourceType, inheritedType, kind, lineNumber, line.Trim());
            }

            yield break;
        }

        var fieldMatch = FieldRegex.Match(line);
        if (fieldMatch.Success)
        {
            var targetType = SimplifyTypeName(fieldMatch.Groups["type"].Value);

            if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: false))
            {
                yield return CreateDependency(sourceFile, sourceType, targetType, ClassDependencyKind.Field, lineNumber,
                    line.Trim());
            }
        }

        var propertyMatch = PropertyRegex.Match(line);
        if (propertyMatch.Success)
        {
            var targetType = SimplifyTypeName(propertyMatch.Groups["type"].Value);

            if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: false))
            {
                yield return CreateDependency(sourceFile, sourceType, targetType, ClassDependencyKind.Property,
                    lineNumber, line.Trim());
            }
        }

        var constructorMatch = ConstructorRegex.Match(line);
        if (constructorMatch.Success &&
            string.Equals(constructorMatch.Groups["name"].Value, sourceType.Name, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var parameterType in ExtractParameterTypes(constructorMatch.Groups["params"].Value))
            {
                if (ShouldIncludeTarget(parameterType, knownTypes, includeKnownFrameworkType: false))
                {
                    yield return CreateDependency(
                        sourceFile,
                        sourceType,
                        parameterType,
                        ClassDependencyKind.ConstructorParameter,
                        lineNumber,
                        line.Trim());
                }
            }

            yield break;
        }

        var methodMatch = MethodRegex.Match(line);
        if (methodMatch.Success)
        {
            var returnType = SimplifyTypeName(methodMatch.Groups["return"].Value);

            if (ShouldIncludeTarget(returnType, knownTypes, includeKnownFrameworkType: false))
            {
                yield return CreateDependency(sourceFile, sourceType, returnType, ClassDependencyKind.ReturnType,
                    lineNumber, line.Trim());
            }

            foreach (var parameterType in ExtractParameterTypes(methodMatch.Groups["params"].Value))
            {
                if (ShouldIncludeTarget(parameterType, knownTypes, includeKnownFrameworkType: false))
                {
                    yield return CreateDependency(sourceFile, sourceType, parameterType,
                        ClassDependencyKind.MethodParameter, lineNumber, line.Trim());
                }
            }
        }

        var localMatch = LocalRegex.Match(line);
        if (localMatch.Success && localMatch.Groups["type"].Success)
        {
            var targetType = SimplifyTypeName(localMatch.Groups["type"].Value);

            if (ShouldIncludeTarget(targetType, knownTypes, includeKnownFrameworkType: false))
            {
                yield return CreateDependency(sourceFile, sourceType, targetType, ClassDependencyKind.LocalVariable,
                    lineNumber, line.Trim());
            }
        }
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

    private static IEnumerable<string> ExtractInheritedTypes(string tail)
    {
        var colonIndex = tail.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex < 0)
        {
            yield break;
        }

        var inheritanceText = tail[(colonIndex + 1)..];
        var whereIndex = inheritanceText.IndexOf(" where ", StringComparison.Ordinal);
        if (whereIndex >= 0)
        {
            inheritanceText = inheritanceText[..whereIndex];
        }

        foreach (var candidate in inheritanceText.Split(',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var typeName = SimplifyTypeName(candidate);
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                yield return typeName;
            }
        }
    }

    private static IEnumerable<string> ExtractParameterTypes(string parameterText)
    {
        if (string.IsNullOrWhiteSpace(parameterText))
        {
            yield break;
        }

        foreach (var parameter in parameterText.Split(',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = parameter
                .Replace("ref ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("out ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("in ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length < 2)
            {
                continue;
            }

            var typeName = SimplifyTypeName(parts[0]);
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                yield return typeName;
            }
        }
    }

    private static IEnumerable<string> ExtractGenericArguments(string line)
    {
        var matches = Regex.Matches(line, @"<(?<args>[A-Za-z_][\w.<>?,\s]*)>");
        foreach (Match match in matches)
        {
            foreach (var argument in match.Groups["args"].Value
                         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var typeName = SimplifyTypeName(argument);
                if (!string.IsNullOrWhiteSpace(typeName))
                {
                    yield return typeName;
                }
            }
        }
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
        var typeName = value.Trim().Trim('?').Replace("[]", string.Empty, StringComparison.Ordinal);
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

    private static ClassDiscoveredTypeKind ParseTypeKind(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "interface" => ClassDiscoveredTypeKind.Interface,
            "record" => ClassDiscoveredTypeKind.Record,
            "struct" => ClassDiscoveredTypeKind.Struct,
            "enum" => ClassDiscoveredTypeKind.Enum,
            _ => ClassDiscoveredTypeKind.Class
        };
    }

    private static string[] SplitLines(string source) =>
        source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static int GetLineNumber(string source, int index) =>
        source[..Math.Min(index, source.Length)].Count(character => character == '\n') + 1;

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

    private sealed record SourceFileInfo(string ProjectName, string SourcePath, string Source);
}
