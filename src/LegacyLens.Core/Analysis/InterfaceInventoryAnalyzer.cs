using System.Xml;
using System.Xml.Linq;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LegacyLens.Core.Analysis;

public sealed class InterfaceInventoryAnalyzer
{
    private static readonly HashSet<string> IgnoredInterfaceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "IDisposable",
        "IAsyncDisposable",
        "IEnumerable",
        "IEnumerable<T>",
        "IQueryable",
        "IQueryable<T>",
        "ICollection",
        "ICollection<T>",
        "IList",
        "IList<T>",
        "IReadOnlyList",
        "IReadOnlyList<T>",
        "IReadOnlyCollection",
        "IReadOnlyCollection<T>",
        "IDictionary",
        "IDictionary<TKey,TValue>",
        "IComparable",
        "IEquatable",
        "ICloneable",
        "IFormattable"
    };

    private static readonly string[] RegistrationMethodNames =
    [
        "AddSingleton",
        "AddScoped",
        "AddTransient",
        "TryAddSingleton",
        "TryAddScoped",
        "TryAddTransient"
    ];

    public InterfaceInventoryReport Analyze(IReadOnlyCollection<DiscoveredProject> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var fileInventory = new ScanFileInventoryBuilder().Build(projects);

        return Analyze(projects, fileInventory);
    }

    public InterfaceInventoryReport Analyze(ScanFileInventory fileInventory)
    {
        ArgumentNullException.ThrowIfNull(fileInventory);

        return Analyze(Array.Empty<DiscoveredProject>(), fileInventory);
    }

    public InterfaceInventoryReport Analyze(
        IReadOnlyCollection<DiscoveredProject> projects,
        ScanFileInventory fileInventory)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(fileInventory);

        var sourceFiles = fileInventory.CSharpFiles
            .Select(ParseSourceFile)
            .ToArray();

        var interfaces = sourceFiles
            .SelectMany(DiscoverInterfaces)
            .GroupBy(interfaceDefinition => CreateInterfaceKey(interfaceDefinition), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(interfaceDefinition => interfaceDefinition.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(interfaceDefinition => interfaceDefinition.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var knownInterfaces = interfaces
            .GroupBy(interfaceDefinition => interfaceDefinition.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var implementations = sourceFiles
            .SelectMany(sourceFile => DiscoverImplementations(sourceFile, knownInterfaces))
            .GroupBy(CreateImplementationKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(implementation => implementation.InterfaceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(implementation => implementation.ImplementationType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var consumers = sourceFiles
            .SelectMany(sourceFile => DiscoverConsumers(sourceFile, knownInterfaces))
            .GroupBy(CreateConsumerKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(consumer => consumer.InterfaceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(consumer => consumer.ConsumerType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var configurationFiles = DiscoverConfigurationFiles(projects, fileInventory).ToArray();

        var registrations = sourceFiles
            .SelectMany(sourceFile => DiscoverSourceRegistrations(sourceFile, knownInterfaces))
            .Concat(configurationFiles.SelectMany(file => DiscoverConfigurationRegistrations(file, knownInterfaces)))
            .GroupBy(CreateRegistrationKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(registration => registration.InterfaceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(registration => registration.Kind)
            .ThenBy(registration => registration.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var findings = CreateFindings(
                interfaces,
                implementations,
                consumers,
                registrations)
            .OrderByDescending(finding => finding.Severity)
            .ThenBy(finding => finding.InterfaceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.Finding, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new InterfaceInventoryReport(
            interfaces,
            implementations,
            consumers,
            registrations,
            findings,
            sourceFiles.Length,
            configurationFiles.Length);
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

    private static IEnumerable<InterfaceDefinition> DiscoverInterfaces(SourceFileInfo sourceFile)
    {
        foreach (var declaration in sourceFile.Root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            var name = declaration.Identifier.Text;

            if (string.IsNullOrWhiteSpace(name) || IsIgnoredInterface(name))
            {
                continue;
            }

            var inheritedInterfaces = declaration.BaseList?.Types
                    .Select(type => SimplifyTypeName(type.Type.ToString()))
                    .Where(type => !string.IsNullOrWhiteSpace(type))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(type => type, StringComparer.OrdinalIgnoreCase)
                    .ToArray() ??
                Array.Empty<string>();

            yield return new InterfaceDefinition(
                sourceFile.ProjectName,
                sourceFile.SourcePath,
                GetLineNumber(sourceFile.SyntaxTree, declaration),
                name,
                CreateFullTypeName(declaration, name),
                inheritedInterfaces,
                ClassifyLikelyRole(name),
                IsPossibleExtensionPoint(name));
        }
    }

    private static IEnumerable<InterfaceImplementation> DiscoverImplementations(
        SourceFileInfo sourceFile,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        foreach (var declaration in sourceFile.Root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (declaration is InterfaceDeclarationSyntax)
            {
                continue;
            }

            if (declaration.BaseList is null)
            {
                continue;
            }

            var implementationType = declaration.Identifier.Text;
            if (string.IsNullOrWhiteSpace(implementationType))
            {
                continue;
            }

            foreach (var baseType in declaration.BaseList.Types)
            {
                var interfaceName = SimplifyTypeName(baseType.Type.ToString());

                if (!knownInterfaces.ContainsKey(interfaceName))
                {
                    continue;
                }

                yield return new InterfaceImplementation(
                    sourceFile.ProjectName,
                    sourceFile.SourcePath,
                    GetLineNumber(sourceFile.SyntaxTree, baseType),
                    interfaceName,
                    implementationType,
                    baseType.ToString());
            }
        }
    }

    private static IEnumerable<InterfaceConsumer> DiscoverConsumers(
        SourceFileInfo sourceFile,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        foreach (var typeDeclaration in sourceFile.Root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (typeDeclaration is InterfaceDeclarationSyntax)
            {
                continue;
            }

            var consumerType = typeDeclaration.Identifier.Text;
            if (string.IsNullOrWhiteSpace(consumerType))
            {
                continue;
            }

            foreach (var consumer in DiscoverTypeConsumers(sourceFile, typeDeclaration, consumerType, knownInterfaces))
            {
                yield return consumer;
            }
        }

        foreach (var topLevelStatement in sourceFile.Root.DescendantNodes().OfType<GlobalStatementSyntax>())
        {
            foreach (var consumer in DiscoverEndpointDelegateConsumers(sourceFile, topLevelStatement, knownInterfaces))
            {
                yield return consumer;
            }
        }
    }

    private static IEnumerable<InterfaceConsumer> DiscoverTypeConsumers(
        SourceFileInfo sourceFile,
        TypeDeclarationSyntax typeDeclaration,
        string consumerType,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        foreach (var constructor in OwnNodes<ConstructorDeclarationSyntax>(typeDeclaration))
        {
            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                foreach (var interfaceName in ExtractKnownInterfaces(parameter.Type, knownInterfaces))
                {
                    yield return CreateConsumer(
                        sourceFile,
                        interfaceName,
                        consumerType,
                        InterfaceConsumerKind.ConstructorParameter,
                        GetLineNumber(sourceFile.SyntaxTree, constructor),
                        TrimEvidence(constructor.ToString()));
                }
            }
        }

        foreach (var field in OwnNodes<FieldDeclarationSyntax>(typeDeclaration))
        {
            foreach (var interfaceName in ExtractKnownInterfaces(field.Declaration.Type, knownInterfaces))
            {
                yield return CreateConsumer(
                    sourceFile,
                    interfaceName,
                    consumerType,
                    ContainsGenericOrCollectionUsage(field.Declaration.Type)
                        ? InterfaceConsumerKind.GenericOrCollectionUsage
                        : InterfaceConsumerKind.Field,
                    GetLineNumber(sourceFile.SyntaxTree, field),
                    GetDeclarationEvidence(field));
            }
        }

        foreach (var property in OwnNodes<PropertyDeclarationSyntax>(typeDeclaration))
        {
            foreach (var interfaceName in ExtractKnownInterfaces(property.Type, knownInterfaces))
            {
                yield return CreateConsumer(
                    sourceFile,
                    interfaceName,
                    consumerType,
                    ContainsGenericOrCollectionUsage(property.Type)
                        ? InterfaceConsumerKind.GenericOrCollectionUsage
                        : InterfaceConsumerKind.Property,
                    GetLineNumber(sourceFile.SyntaxTree, property),
                    TrimEvidence(property.ToString()));
            }
        }

        foreach (var method in OwnNodes<MethodDeclarationSyntax>(typeDeclaration))
        {
            foreach (var interfaceName in ExtractKnownInterfaces(method.ReturnType, knownInterfaces))
            {
                yield return CreateConsumer(
                    sourceFile,
                    interfaceName,
                    consumerType,
                    InterfaceConsumerKind.ReturnType,
                    GetLineNumber(sourceFile.SyntaxTree, method),
                    GetMethodEvidence(method));
            }

            foreach (var parameter in method.ParameterList.Parameters)
            {
                foreach (var interfaceName in ExtractKnownInterfaces(parameter.Type, knownInterfaces))
                {
                    yield return CreateConsumer(
                        sourceFile,
                        interfaceName,
                        consumerType,
                        InterfaceConsumerKind.MethodParameter,
                        GetLineNumber(sourceFile.SyntaxTree, method),
                        GetMethodEvidence(method));
                }
            }
        }

        foreach (var local in OwnNodes<LocalDeclarationStatementSyntax>(typeDeclaration))
        {
            foreach (var interfaceName in ExtractKnownInterfaces(local.Declaration.Type, knownInterfaces))
            {
                yield return CreateConsumer(
                    sourceFile,
                    interfaceName,
                    consumerType,
                    ContainsGenericOrCollectionUsage(local.Declaration.Type)
                        ? InterfaceConsumerKind.GenericOrCollectionUsage
                        : InterfaceConsumerKind.LocalVariable,
                    GetLineNumber(sourceFile.SyntaxTree, local),
                    TrimEvidence(local.ToString()));
            }
        }

        foreach (var invocation in OwnNodes<InvocationExpressionSyntax>(typeDeclaration))
        {
            foreach (var consumer in DiscoverServiceLocatorConsumers(sourceFile, invocation, consumerType, knownInterfaces))
            {
                yield return consumer;
            }
        }
    }

    private static IEnumerable<InterfaceConsumer> DiscoverEndpointDelegateConsumers(
        SourceFileInfo sourceFile,
        GlobalStatementSyntax topLevelStatement,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        foreach (var invocation in topLevelStatement.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var invocationText = invocation.Expression.ToString();
            if (!invocationText.EndsWith("MapGet", StringComparison.OrdinalIgnoreCase) &&
                !invocationText.EndsWith("MapPost", StringComparison.OrdinalIgnoreCase) &&
                !invocationText.EndsWith("MapPut", StringComparison.OrdinalIgnoreCase) &&
                !invocationText.EndsWith("MapDelete", StringComparison.OrdinalIgnoreCase) &&
                !invocationText.EndsWith("MapPatch", StringComparison.OrdinalIgnoreCase) &&
                !invocationText.EndsWith("MapMethods", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var parameter in invocation.DescendantNodes().OfType<ParameterSyntax>())
            {
                foreach (var interfaceName in ExtractKnownInterfaces(parameter.Type, knownInterfaces))
                {
                    yield return CreateConsumer(
                        sourceFile,
                        interfaceName,
                        "MinimalApiEndpoint",
                        InterfaceConsumerKind.EndpointDelegateParameter,
                        GetLineNumber(sourceFile.SyntaxTree, parameter),
                        TrimEvidence(invocation.ToString()));
                }
            }
        }
    }

    private static IEnumerable<InterfaceConsumer> DiscoverServiceLocatorConsumers(
        SourceFileInfo sourceFile,
        InvocationExpressionSyntax invocation,
        string consumerType,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        var text = invocation.ToString();

        if (!LooksLikeServiceLocatorUsage(text))
        {
            yield break;
        }

        foreach (var typeArgument in invocation.DescendantNodesAndSelf().OfType<GenericNameSyntax>().SelectMany(name => name.TypeArgumentList.Arguments))
        {
            var interfaceName = SimplifyTypeName(typeArgument.ToString());
            if (!knownInterfaces.ContainsKey(interfaceName))
            {
                continue;
            }

            yield return CreateConsumer(
                sourceFile,
                interfaceName,
                consumerType,
                InterfaceConsumerKind.ServiceLocator,
                GetLineNumber(sourceFile.SyntaxTree, invocation),
                TrimEvidence(text));
        }
    }

    private static IEnumerable<InterfaceRegistrationEvidence> DiscoverSourceRegistrations(
        SourceFileInfo sourceFile,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        foreach (var invocation in sourceFile.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var text = invocation.ToString();

            foreach (var registration in DiscoverMicrosoftDiRegistrations(sourceFile, invocation, knownInterfaces))
            {
                yield return registration;
            }

            foreach (var registration in DiscoverLegacyIoCRegistrations(sourceFile, invocation, text, knownInterfaces))
            {
                yield return registration;
            }
        }
    }

    private static IEnumerable<InterfaceRegistrationEvidence> DiscoverMicrosoftDiRegistrations(
        SourceFileInfo sourceFile,
        InvocationExpressionSyntax invocation,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            yield break;
        }

        var methodName = memberAccess.Name switch
        {
            GenericNameSyntax genericName => genericName.Identifier.Text,
            IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
            _ => string.Empty
        };

        if (!RegistrationMethodNames.Contains(methodName, StringComparer.OrdinalIgnoreCase))
        {
            yield break;
        }

        if (memberAccess.Name is GenericNameSyntax generic && generic.TypeArgumentList.Arguments.Count > 0)
        {
            var interfaceName = SimplifyTypeName(generic.TypeArgumentList.Arguments[0].ToString());
            if (!knownInterfaces.ContainsKey(interfaceName))
            {
                yield break;
            }

            var implementationType = generic.TypeArgumentList.Arguments.Count > 1
                ? SimplifyTypeName(generic.TypeArgumentList.Arguments[1].ToString())
                : null;

            yield return new InterfaceRegistrationEvidence(
                sourceFile.ProjectName,
                sourceFile.SourcePath,
                GetLineNumber(sourceFile.SyntaxTree, invocation),
                interfaceName,
                implementationType,
                InterfaceRegistrationKind.MicrosoftDependencyInjection,
                TrimEvidence(invocation.ToString()),
                RequiresReview: implementationType is null,
                implementationType is null
                    ? "Factory or instance registration may need manual review to identify the runtime implementation."
                    : "Microsoft.Extensions.DependencyInjection registration evidence found.");

            yield break;
        }

        if (invocation.ArgumentList.Arguments.Count >= 1)
        {
            var firstArgument = invocation.ArgumentList.Arguments[0].ToString();
            var interfaceName = ExtractTypeOfArgument(firstArgument);

            if (string.IsNullOrWhiteSpace(interfaceName) || !knownInterfaces.ContainsKey(interfaceName))
            {
                yield break;
            }

            var implementationType = invocation.ArgumentList.Arguments.Count > 1
                ? ExtractTypeOfArgument(invocation.ArgumentList.Arguments[1].ToString())
                : null;

            yield return new InterfaceRegistrationEvidence(
                sourceFile.ProjectName,
                sourceFile.SourcePath,
                GetLineNumber(sourceFile.SyntaxTree, invocation),
                interfaceName,
                implementationType,
                InterfaceRegistrationKind.MicrosoftDependencyInjection,
                TrimEvidence(invocation.ToString()),
                RequiresReview: string.IsNullOrWhiteSpace(implementationType),
                string.IsNullOrWhiteSpace(implementationType)
                    ? "Factory or instance registration may need manual review to identify the runtime implementation."
                    : "Microsoft.Extensions.DependencyInjection type-based registration evidence found.");
        }
    }

    private static IEnumerable<InterfaceRegistrationEvidence> DiscoverLegacyIoCRegistrations(
        SourceFileInfo sourceFile,
        InvocationExpressionSyntax invocation,
        string text,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        foreach (var genericName in invocation.DescendantNodesAndSelf().OfType<GenericNameSyntax>())
        {
            var typeArguments = genericName.TypeArgumentList.Arguments
                .Select(argument => SimplifyTypeName(argument.ToString()))
                .Where(type => !string.IsNullOrWhiteSpace(type))
                .ToArray();

            if (typeArguments.Length == 0)
            {
                continue;
            }

            var interfaceName = typeArguments.FirstOrDefault(knownInterfaces.ContainsKey);
            if (string.IsNullOrWhiteSpace(interfaceName))
            {
                continue;
            }

            var kind = ClassifySourceRegistrationKind(text);
            if (kind == InterfaceRegistrationKind.UnknownDynamicWiring && !LooksLikeServiceLocatorUsage(text))
            {
                continue;
            }

            var implementationType = typeArguments.FirstOrDefault(type =>
                !type.Equals(interfaceName, StringComparison.OrdinalIgnoreCase));

            yield return new InterfaceRegistrationEvidence(
                sourceFile.ProjectName,
                sourceFile.SourcePath,
                GetLineNumber(sourceFile.SyntaxTree, invocation),
                interfaceName,
                implementationType,
                kind,
                TrimEvidence(text),
                RequiresReview: implementationType is null || kind == InterfaceRegistrationKind.UnknownDynamicWiring,
                kind == InterfaceRegistrationKind.UnknownDynamicWiring
                    ? "Dynamic resolver or service-locator evidence found; runtime wiring needs manual review."
                    : "Legacy IoC registration evidence found.");
        }

        if (LooksLikeResolverSetup(text))
        {
            yield return new InterfaceRegistrationEvidence(
                sourceFile.ProjectName,
                sourceFile.SourcePath,
                GetLineNumber(sourceFile.SyntaxTree, invocation),
                "Unknown interface",
                null,
                InterfaceRegistrationKind.AspNetDependencyResolver,
                TrimEvidence(text),
                RequiresReview: true,
                "ASP.NET dependency resolver setup may hide runtime interface wiring.");
        }
    }

    private static IEnumerable<InterfaceRegistrationEvidence> DiscoverConfigurationRegistrations(
        ConfigurationFileInfo file,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        if (string.IsNullOrWhiteSpace(file.Content))
        {
            yield break;
        }

        XDocument? document;
        try
        {
            document = XDocument.Parse(file.Content, LoadOptions.SetLineInfo);
        }
        catch
        {
            yield break;
        }

        foreach (var element in document.Descendants())
        {
            var elementName = element.Name.LocalName;
            var attributes = element.Attributes().ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value, StringComparer.OrdinalIgnoreCase);
            var text = element.ToString(SaveOptions.DisableFormatting);
            var interfaceName = FindKnownInterfaceName(attributes.Values.Append(text), knownInterfaces);

            if (string.IsNullOrWhiteSpace(interfaceName))
            {
                continue;
            }

            var kind = ClassifyXmlRegistrationKind(file.FullPath, elementName, text);
            if (kind == InterfaceRegistrationKind.UnknownDynamicWiring)
            {
                continue;
            }

            var implementationType = FindImplementationCandidate(attributes.Values, interfaceName);
            var lineNumber = element is IXmlLineInfo lineInfo && lineInfo.HasLineInfo()
                ? lineInfo.LineNumber
                : 1;

            yield return new InterfaceRegistrationEvidence(
                file.ProjectName,
                file.FullPath,
                lineNumber,
                interfaceName,
                implementationType,
                kind,
                TrimEvidence(text),
                RequiresReview: true,
                "Configuration-driven IoC evidence found. Static analysis cannot prove the section is loaded or active at runtime.");
        }
    }

    private static IEnumerable<ConfigurationFileInfo> DiscoverConfigurationFiles(
        IReadOnlyCollection<DiscoveredProject> projects,
        ScanFileInventory fileInventory)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects)
        {
            var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);
            if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory))
            {
                continue;
            }

            foreach (var path in SafeFileSystem.EnumerateFiles(projectDirectory, "*.config").Concat(SafeFileSystem.EnumerateFiles(projectDirectory, "*.xml")))
            {
                if (!seen.Add(path))
                {
                    continue;
                }

                yield return new ConfigurationFileInfo(project.Name, path, SafeFileSystem.ReadAllTextOrEmpty(path));
            }
        }

        foreach (var projectGroup in fileInventory.CSharpFiles.GroupBy(file => file.ProjectDirectory, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(projectGroup.Key) || !Directory.Exists(projectGroup.Key))
            {
                continue;
            }

            var projectName = projectGroup.First().ProjectName;
            foreach (var path in SafeFileSystem.EnumerateFiles(projectGroup.Key, "*.config").Concat(SafeFileSystem.EnumerateFiles(projectGroup.Key, "*.xml")))
            {
                if (!seen.Add(path))
                {
                    continue;
                }

                yield return new ConfigurationFileInfo(projectName, path, SafeFileSystem.ReadAllTextOrEmpty(path));
            }
        }
    }

    private static IEnumerable<InterfaceInventoryFinding> CreateFindings(
        IReadOnlyList<InterfaceDefinition> interfaces,
        IReadOnlyList<InterfaceImplementation> implementations,
        IReadOnlyList<InterfaceConsumer> consumers,
        IReadOnlyList<InterfaceRegistrationEvidence> registrations)
    {
        foreach (var interfaceDefinition in interfaces)
        {
            var implementationCount = implementations.Count(implementation => implementation.InterfaceName.Equals(interfaceDefinition.Name, StringComparison.OrdinalIgnoreCase));
            var consumerCount = consumers.Count(consumer => consumer.InterfaceName.Equals(interfaceDefinition.Name, StringComparison.OrdinalIgnoreCase));
            var registrationCount = registrations.Count(registration => registration.InterfaceName.Equals(interfaceDefinition.Name, StringComparison.OrdinalIgnoreCase));

            if (implementationCount > 1)
            {
                yield return new InterfaceInventoryFinding(
                    InterfaceInventoryFindingSeverity.Info,
                    interfaceDefinition.Name,
                    "Multiple static implementations found",
                    $"{implementationCount} implementation type(s) were discovered.",
                    "Review this interface as a likely extension point or strategy boundary before changing implementations.");
            }

            if (implementationCount == 0)
            {
                yield return new InterfaceInventoryFinding(
                    InterfaceInventoryFindingSeverity.Warning,
                    interfaceDefinition.Name,
                    "No static implementation found",
                    $"No class implementing {interfaceDefinition.Name} was found in scanned source files.",
                    "Check generated code, external assemblies, reflection, or configuration-driven wiring before treating this as unused.");
            }

            if (consumerCount == 0)
            {
                yield return new InterfaceInventoryFinding(
                    InterfaceInventoryFindingSeverity.Warning,
                    interfaceDefinition.Name,
                    "No static consumer found",
                    $"No constructor, member, method, endpoint, or service-locator consumer was found for {interfaceDefinition.Name}.",
                    "Review runtime wiring and tests before assuming the interface is unused.");
            }

            if (registrationCount > 0)
            {
                yield return new InterfaceInventoryFinding(
                    InterfaceInventoryFindingSeverity.Info,
                    interfaceDefinition.Name,
                    "Registration evidence found",
                    $"{registrationCount} registration or wiring evidence item(s) were discovered.",
                    "Use the registration evidence with implementation and consumer evidence to understand runtime composition.");
            }
        }

        foreach (var registration in registrations.Where(registration => registration.RequiresReview))
        {
            yield return new InterfaceInventoryFinding(
                InterfaceInventoryFindingSeverity.Review,
                registration.InterfaceName,
                "Dynamic or configuration-driven wiring requires review",
                registration.Evidence,
                "Confirm whether this registration source is active at runtime and whether transforms or container modules add more wiring.");
        }
    }

    private static InterfaceConsumer CreateConsumer(
        SourceFileInfo sourceFile,
        string interfaceName,
        string consumerType,
        InterfaceConsumerKind kind,
        int lineNumber,
        string evidence)
    {
        return new InterfaceConsumer(
            sourceFile.ProjectName,
            sourceFile.SourcePath,
            lineNumber,
            interfaceName,
            consumerType,
            kind,
            TrimEvidence(evidence));
    }

    private static IEnumerable<string> ExtractKnownInterfaces(
        TypeSyntax? typeSyntax,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        if (typeSyntax is null)
        {
            yield break;
        }

        var typeNames = typeSyntax.DescendantNodesAndSelf()
            .OfType<TypeSyntax>()
            .Select(type => SimplifyTypeName(type.ToString()))
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var typeName in typeNames)
        {
            if (knownInterfaces.ContainsKey(typeName))
            {
                yield return typeName;
            }
        }
    }

    private static bool ContainsGenericOrCollectionUsage(TypeSyntax typeSyntax)
    {
        return typeSyntax.DescendantNodesAndSelf().OfType<GenericNameSyntax>().Any();
    }

    private static InterfaceRegistrationKind ClassifySourceRegistrationKind(string text)
    {
        if (text.Contains("Autofac", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("RegisterType", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("RegisterInstance", StringComparison.OrdinalIgnoreCase) ||
            text.Contains(".As<", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.Autofac;
        }

        if (text.Contains("Component.For", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ImplementedBy", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Windsor", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.CastleWindsor;
        }

        if (text.Contains("Bind<", StringComparison.OrdinalIgnoreCase) ||
            text.Contains(".To<", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.Ninject;
        }

        if (text.Contains("RegisterType", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.Unity;
        }

        if (text.Contains("For<", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Use<", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.StructureMap;
        }

        if (text.Contains("Register<", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.SimpleInjector;
        }

        if (text.Contains("RegisterAssembly", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.UnknownDynamicWiring;
        }

        if (LooksLikeServiceLocatorUsage(text))
        {
            return InterfaceRegistrationKind.CommonServiceLocator;
        }

        return InterfaceRegistrationKind.UnknownDynamicWiring;
    }

    private static InterfaceRegistrationKind ClassifyXmlRegistrationKind(string path, string elementName, string text)
    {
        var combined = $"{path} {elementName} {text}";

        if (combined.Contains("spring", StringComparison.OrdinalIgnoreCase) ||
            elementName.Equals("object", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.SpringNetXml;
        }

        if (combined.Contains("castle", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("windsor", StringComparison.OrdinalIgnoreCase) ||
            elementName.Equals("component", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.CastleWindsorXml;
        }

        if (combined.Contains("unity", StringComparison.OrdinalIgnoreCase) ||
            elementName.Equals("register", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.UnityXml;
        }

        if (combined.Contains("enterpriseLibrary", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("objectBuilder", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.EnterpriseLibraryObjectBuilder;
        }

        if (combined.Contains("factory", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("container", StringComparison.OrdinalIgnoreCase))
        {
            return InterfaceRegistrationKind.CustomObjectFactory;
        }

        return InterfaceRegistrationKind.UnknownDynamicWiring;
    }

    private static string? FindKnownInterfaceName(
        IEnumerable<string> values,
        IReadOnlyDictionary<string, InterfaceDefinition> knownInterfaces)
    {
        foreach (var value in values)
        {
            foreach (var knownInterface in knownInterfaces.Keys)
            {
                if (value.Contains(knownInterface, StringComparison.OrdinalIgnoreCase))
                {
                    return knownInterface;
                }
            }
        }

        return null;
    }

    private static string? FindImplementationCandidate(IEnumerable<string> values, string interfaceName)
    {
        foreach (var value in values)
        {
            var candidate = value
                .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(SimplifyTypeName)
                .FirstOrDefault(part =>
                    !part.Equals(interfaceName, StringComparison.OrdinalIgnoreCase) &&
                    !part.StartsWith("I", StringComparison.Ordinal) &&
                    part.Length > 1 &&
                    char.IsUpper(part[0]));

            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ExtractTypeOfArgument(string text)
    {
        const string prefix = "typeof(";
        var index = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var start = index + prefix.Length;
        var end = text.IndexOf(')', start);
        if (end <= start)
        {
            return null;
        }

        return SimplifyTypeName(text[start..end]);
    }

    private static bool LooksLikeServiceLocatorUsage(string text)
    {
        return text.Contains("ServiceLocator", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("GetInstance<", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("GetService<", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Resolve<", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeResolverSetup(string text)
    {
        return text.Contains("DependencyResolver.SetResolver", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("config.DependencyResolver", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("SetControllerFactory", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIgnoredInterface(string name)
    {
        return IgnoredInterfaceNames.Contains(name) ||
               IgnoredInterfaceNames.Contains($"{name}<T>");
    }

    private static string ClassifyLikelyRole(string interfaceName)
    {
        if (interfaceName.Contains("Repository", StringComparison.OrdinalIgnoreCase))
        {
            return "Repository abstraction";
        }

        if (interfaceName.Contains("Service", StringComparison.OrdinalIgnoreCase))
        {
            return "Service boundary";
        }

        if (interfaceName.Contains("Factory", StringComparison.OrdinalIgnoreCase))
        {
            return "Factory abstraction";
        }

        if (interfaceName.Contains("Provider", StringComparison.OrdinalIgnoreCase))
        {
            return "Provider abstraction";
        }

        if (interfaceName.Contains("Handler", StringComparison.OrdinalIgnoreCase))
        {
            return "Handler abstraction";
        }

        if (interfaceName.Contains("Strategy", StringComparison.OrdinalIgnoreCase) ||
            interfaceName.Contains("Policy", StringComparison.OrdinalIgnoreCase))
        {
            return "Strategy or policy abstraction";
        }

        if (interfaceName.Contains("Contract", StringComparison.OrdinalIgnoreCase))
        {
            return "Service contract";
        }

        return "General abstraction";
    }

    private static bool IsPossibleExtensionPoint(string interfaceName)
    {
        return interfaceName.Contains("Service", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.Contains("Repository", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.Contains("Provider", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.Contains("Factory", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.Contains("Strategy", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.Contains("Handler", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.Contains("Policy", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.Contains("Contract", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<TNode> OwnNodes<TNode>(TypeDeclarationSyntax owner)
        where TNode : SyntaxNode
    {
        return owner
            .DescendantNodes()
            .OfType<TNode>()
            .Where(node => node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() == owner);
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

    private static string SimplifyTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return string.Empty;
        }

        var cleaned = typeName
            .Replace("global::", string.Empty, StringComparison.Ordinal)
            .Trim()
            .Trim('?')
            .Trim();

        var nullableIndex = cleaned.IndexOf('?');
        if (nullableIndex > 0)
        {
            cleaned = cleaned[..nullableIndex];
        }

        var genericTickIndex = cleaned.IndexOf('`');
        if (genericTickIndex > 0)
        {
            cleaned = cleaned[..genericTickIndex];
        }

        var genericIndex = cleaned.IndexOf('<');
        if (genericIndex > 0)
        {
            cleaned = cleaned[..genericIndex];
        }

        var arrayIndex = cleaned.IndexOf('[');
        if (arrayIndex > 0)
        {
            cleaned = cleaned[..arrayIndex];
        }

        var lastDotIndex = cleaned.LastIndexOf('.');
        if (lastDotIndex >= 0 && lastDotIndex + 1 < cleaned.Length)
        {
            cleaned = cleaned[(lastDotIndex + 1)..];
        }

        return cleaned.Trim();
    }

    private static int GetLineNumber(SyntaxTree syntaxTree, SyntaxNode node)
    {
        return syntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }

    private static string GetDeclarationEvidence(FieldDeclarationSyntax field)
    {
        return TrimEvidence(field.Declaration.ToString());
    }

    private static string GetMethodEvidence(MethodDeclarationSyntax method)
    {
        return TrimEvidence($"{method.ReturnType} {method.Identifier}{method.ParameterList}");
    }

    private static string TrimEvidence(string evidence)
    {
        var trimmed = string.Join(
                " ",
                evidence
                    .Replace("\r", " ", StringComparison.Ordinal)
                    .Replace("\n", " ", StringComparison.Ordinal)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Trim();

        return trimmed.Length <= 160
            ? trimmed
            : trimmed[..157] + "...";
    }

    private static string CreateInterfaceKey(InterfaceDefinition interfaceDefinition)
    {
        return $"{interfaceDefinition.ProjectName}|{interfaceDefinition.SourcePath}|{interfaceDefinition.Name}|{interfaceDefinition.LineNumber}";
    }

    private static string CreateImplementationKey(InterfaceImplementation implementation)
    {
        return $"{implementation.ProjectName}|{implementation.InterfaceName}|{implementation.ImplementationType}|{implementation.SourcePath}|{implementation.LineNumber}";
    }

    private static string CreateConsumerKey(InterfaceConsumer consumer)
    {
        return $"{consumer.ProjectName}|{consumer.InterfaceName}|{consumer.ConsumerType}|{consumer.Kind}|{consumer.SourcePath}|{consumer.LineNumber}";
    }

    private static string CreateRegistrationKey(InterfaceRegistrationEvidence registration)
    {
        return $"{registration.ProjectName}|{registration.InterfaceName}|{registration.ImplementationType}|{registration.Kind}|{registration.SourcePath}|{registration.LineNumber}|{registration.Evidence}";
    }

    private sealed record SourceFileInfo(
        string ProjectName,
        string SourcePath,
        string Source,
        SyntaxTree SyntaxTree,
        CompilationUnitSyntax Root);

    private sealed record ConfigurationFileInfo(
        string ProjectName,
        string FullPath,
        string Content);
}
