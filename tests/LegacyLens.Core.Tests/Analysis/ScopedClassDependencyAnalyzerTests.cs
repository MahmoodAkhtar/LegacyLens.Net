using LegacyLens.Core.Analysis;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ScopedClassDependencyAnalyzerTests
{
    [Fact]
    public void Analyze_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new ScopedClassDependencyAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(null!, "SampleLegacyApp.Services.CustomerService", DateTimeOffset.Now, DateTimeOffset.UtcNow));

        Assert.Equal("classDependencyReport", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenRequestedTypeNameIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new ScopedClassDependencyAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(CreateReport(), null!, DateTimeOffset.Now, DateTimeOffset.UtcNow));

        Assert.Equal("requestedTypeName", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Analyze_WhenRequestedTypeNameIsWhiteSpace_ThrowsArgumentException(string requestedTypeName)
    {
        var analyzer = new ScopedClassDependencyAnalyzer();

        var exception = Assert.Throws<ArgumentException>(() =>
            analyzer.Analyze(CreateReport(), requestedTypeName, DateTimeOffset.Now, DateTimeOffset.UtcNow));

        Assert.Equal("requestedTypeName", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenRequestedFullTypeNameMatches_ReturnsFocusedDependenciesAndConcerns()
    {
        var generatedLocal = new DateTimeOffset(2026, 6, 20, 15, 30, 45, TimeSpan.FromHours(1));
        var generatedUtc = generatedLocal.ToUniversalTime();
        var analyzer = new ScopedClassDependencyAnalyzer();

        var report = analyzer.Analyze(
            CreateReport(),
            "samplelegacyapp.services.customerservice",
            generatedLocal,
            generatedUtc);

        Assert.True(report.IsFound);
        Assert.False(report.IsAmbiguous);
        Assert.Equal("samplelegacyapp.services.customerservice", report.RequestedTypeName);
        Assert.Equal(generatedLocal, report.GeneratedLocal);
        Assert.Equal(generatedUtc, report.GeneratedUtc);
        Assert.Equal(2, report.SourceFileCount);
        Assert.Equal(4, report.DiscoveredTypeCount);
        Assert.Equal("CustomerService", report.RootType!.Name);

        Assert.Contains(report.OutboundDependencies, dependency =>
            dependency.SourceFullName == "SampleLegacyApp.Services.CustomerService" &&
            dependency.TargetFullName == "SampleLegacyApp.Data.CustomerRepository" &&
            dependency.Kind == ClassDependencyKind.Field);

        Assert.Contains(report.OutboundDependencies, dependency =>
            dependency.SourceFullName == "SampleLegacyApp.Services.CustomerService" &&
            dependency.TargetFullName == "SampleLegacyApp.Services.ICustomerService" &&
            dependency.Kind == ClassDependencyKind.InterfaceImplementation);

        Assert.Contains(report.InboundDependants, dependency =>
            dependency.SourceFullName == "SampleLegacyApp.Web.CustomerController" &&
            dependency.TargetFullName == "SampleLegacyApp.Services.CustomerService" &&
            dependency.Kind == ClassDependencyKind.ConstructorParameter);

        Assert.Single(report.Concerns);
        Assert.Equal("SampleLegacyApp.Services.CustomerService", report.Concerns[0].SourceFullName);
    }

    [Fact]
    public void Analyze_WhenNoFullNameMatch_DoesNotFallBackToShortName()
    {
        var analyzer = new ScopedClassDependencyAnalyzer();

        var report = analyzer.Analyze(
            CreateReport(),
            "CustomerService",
            DateTimeOffset.Now,
            DateTimeOffset.UtcNow);

        Assert.False(report.IsFound);
        Assert.False(report.IsAmbiguous);
        Assert.Empty(report.MatchingTypes);
        Assert.Empty(report.OutboundDependencies);
        Assert.Empty(report.InboundDependants);
        Assert.Empty(report.Concerns);
    }

    [Fact]
    public void Analyze_WhenDuplicateFullNameMatches_ReturnsAmbiguityWithoutGuessing()
    {
        var duplicate = new DiscoveredType(
            "CustomerService",
            "SampleLegacyApp.Services.CustomerService",
            ClassDiscoveredTypeKind.Class,
            "Duplicate.Project",
            "C:\\Repo\\Duplicate\\CustomerService.cs",
            5);

        var original = CreateReport();
        var duplicatedReport = original with
        {
            Types = original.Types.Append(duplicate).ToArray()
        };

        var analyzer = new ScopedClassDependencyAnalyzer();

        var report = analyzer.Analyze(
            duplicatedReport,
            "SampleLegacyApp.Services.CustomerService",
            DateTimeOffset.Now,
            DateTimeOffset.UtcNow);

        Assert.False(report.IsFound);
        Assert.True(report.IsAmbiguous);
        Assert.Equal(2, report.MatchingTypes.Count);
        Assert.Empty(report.OutboundDependencies);
        Assert.Empty(report.InboundDependants);
        Assert.Empty(report.Concerns);
    }

    [Fact]
    public void Analyze_WhenShortNamesCollide_DoesNotIncludeOutboundDependenciesFromOtherSourceType()
    {
        var analyzer = new ScopedClassDependencyAnalyzer();

        var report = analyzer.Analyze(
            CreateShortNameCollisionReport(),
            "Sample.Components.Controllers.ControlBarController",
            DateTimeOffset.Now,
            DateTimeOffset.UtcNow);

        Assert.True(report.IsFound);

        Assert.Contains(report.OutboundDependencies, dependency =>
            dependency.SourceFullName == "Sample.Components.Controllers.ControlBarController" &&
            dependency.TargetType == "ComponentDependency");

        Assert.DoesNotContain(report.OutboundDependencies, dependency =>
            dependency.SourceFullName == "Sample.InternalServices.ControlBarController" ||
            dependency.TargetType == "InternalDependency");
    }

    [Fact]
    public void Analyze_WhenInboundTargetShortNameIsAmbiguous_DoesNotIncludeUnresolvedInboundDependant()
    {
        var analyzer = new ScopedClassDependencyAnalyzer();

        var report = analyzer.Analyze(
            CreateShortNameCollisionReport(includeAmbiguousUnresolvedInbound: true),
            "Sample.Components.Controllers.ControlBarController",
            DateTimeOffset.Now,
            DateTimeOffset.UtcNow);

        Assert.True(report.IsFound);

        Assert.DoesNotContain(report.InboundDependants, dependency =>
            dependency.SourceFullName == "Sample.Web.Startup" &&
            dependency.TargetFullName is null &&
            dependency.TargetType == "ControlBarController");
    }

    [Fact]
    public void Analyze_WhenInboundTargetIsUniquelyResolved_IncludesInboundDependant()
    {
        var analyzer = new ScopedClassDependencyAnalyzer();

        var report = analyzer.Analyze(
            CreateShortNameCollisionReport(),
            "Sample.Components.Controllers.ControlBarController",
            DateTimeOffset.Now,
            DateTimeOffset.UtcNow);

        Assert.True(report.IsFound);

        Assert.Contains(report.InboundDependants, dependency =>
            dependency.SourceFullName == "Sample.Web.Startup" &&
            dependency.TargetFullName == "Sample.Components.Controllers.ControlBarController" &&
            dependency.TargetType == "ControlBarController");
    }

    [Fact]
    public void Analyze_WhenShortNamesCollide_DoesNotIncludeConcernsFromOtherSourceType()
    {
        var analyzer = new ScopedClassDependencyAnalyzer();

        var report = analyzer.Analyze(
            CreateShortNameCollisionReport(),
            "Sample.Components.Controllers.ControlBarController",
            DateTimeOffset.Now,
            DateTimeOffset.UtcNow);

        Assert.True(report.IsFound);

        Assert.Contains(report.Concerns, concern =>
            concern.SourceFullName == "Sample.Components.Controllers.ControlBarController" &&
            concern.TargetType == "ComponentDependency");

        Assert.DoesNotContain(report.Concerns, concern =>
            concern.SourceFullName == "Sample.InternalServices.ControlBarController" ||
            concern.TargetType == "InternalDependency");
    }

    private static ClassDependencyReport CreateReport()
    {
        var servicePath = "C:\\Repo\\SampleLegacyApp.Services\\CustomerService.cs";
        var controllerPath = "C:\\Repo\\SampleLegacyApp.Web\\CustomerController.cs";
        var repositoryPath = "C:\\Repo\\SampleLegacyApp.Data\\CustomerRepository.cs";

        return new ClassDependencyReport(
            new[]
            {
                new DiscoveredType(
                    "CustomerService",
                    "SampleLegacyApp.Services.CustomerService",
                    ClassDiscoveredTypeKind.Class,
                    "SampleLegacyApp.Services",
                    servicePath,
                    8),
                new DiscoveredType(
                    "ICustomerService",
                    "SampleLegacyApp.Services.ICustomerService",
                    ClassDiscoveredTypeKind.Interface,
                    "SampleLegacyApp.Services",
                    servicePath,
                    3),
                new DiscoveredType(
                    "CustomerRepository",
                    "SampleLegacyApp.Data.CustomerRepository",
                    ClassDiscoveredTypeKind.Class,
                    "SampleLegacyApp.Data",
                    repositoryPath,
                    3),
                new DiscoveredType(
                    "CustomerController",
                    "SampleLegacyApp.Web.CustomerController",
                    ClassDiscoveredTypeKind.Class,
                    "SampleLegacyApp.Web",
                    controllerPath,
                    6)
            },
            new[]
            {
                CreateDependency(
                    "SampleLegacyApp.Services",
                    servicePath,
                    8,
                    "CustomerService",
                    "ICustomerService",
                    ClassDependencyKind.InterfaceImplementation,
                    "ICustomerService",
                    "SampleLegacyApp.Services.CustomerService",
                    "SampleLegacyApp.Services.ICustomerService",
                    servicePath),
                CreateDependency(
                    "SampleLegacyApp.Services",
                    servicePath,
                    10,
                    "CustomerService",
                    "CustomerRepository",
                    ClassDependencyKind.Field,
                    "private readonly CustomerRepository _repository",
                    "SampleLegacyApp.Services.CustomerService",
                    "SampleLegacyApp.Data.CustomerRepository",
                    repositoryPath),
                CreateDependency(
                    "SampleLegacyApp.Web",
                    controllerPath,
                    7,
                    "CustomerController",
                    "CustomerService",
                    ClassDependencyKind.ConstructorParameter,
                    "CustomerController(CustomerService service)",
                    "SampleLegacyApp.Web.CustomerController",
                    "SampleLegacyApp.Services.CustomerService",
                    servicePath)
            },
            new[]
            {
                CreateConcern(
                    ClassDependencyConcernSeverity.Medium,
                    "CustomerService",
                    "CustomerRepository",
                    ClassDependencyKind.Field,
                    "SampleLegacyApp.Services",
                    servicePath,
                    10,
                    "private readonly CustomerRepository _repository",
                    "SampleLegacyApp.Services.CustomerService",
                    "SampleLegacyApp.Data.CustomerRepository")
            },
            Array.Empty<ClassCouplingHotspot>(),
            SourceFileCount: 2);
    }

    private static ClassDependencyReport CreateShortNameCollisionReport(bool includeAmbiguousUnresolvedInbound = false)
    {
        const string componentPath = "C:\\Repo\\Sample\\Components\\Controllers\\ControlBarController.cs";
        const string internalPath = "C:\\Repo\\Sample\\InternalServices\\ControlBarController.cs";
        const string startupPath = "C:\\Repo\\Sample\\Web\\Startup.cs";
        const string componentDependencyPath = "C:\\Repo\\Sample\\Components\\ComponentDependency.cs";
        const string internalDependencyPath = "C:\\Repo\\Sample\\InternalServices\\InternalDependency.cs";

        var dependencies = new List<ClassDependency>
        {
            CreateDependency(
                "Sample.Components",
                componentPath,
                10,
                "ControlBarController",
                "ComponentDependency",
                ClassDependencyKind.Field,
                "private readonly ComponentDependency _dependency",
                "Sample.Components.Controllers.ControlBarController",
                "Sample.Components.ComponentDependency",
                componentDependencyPath),
            CreateDependency(
                "Sample.InternalServices",
                internalPath,
                12,
                "ControlBarController",
                "InternalDependency",
                ClassDependencyKind.Field,
                "private readonly InternalDependency _dependency",
                "Sample.InternalServices.ControlBarController",
                "Sample.InternalServices.InternalDependency",
                internalDependencyPath),
            CreateDependency(
                "Sample.Web",
                startupPath,
                20,
                "Startup",
                "ControlBarController",
                ClassDependencyKind.GenericTypeArgument,
                "AddTransient<IControlBarController, ControlBarController>",
                "Sample.Web.Startup",
                "Sample.Components.Controllers.ControlBarController",
                componentPath)
        };

        if (includeAmbiguousUnresolvedInbound)
        {
            dependencies.Add(CreateDependency(
                "Sample.Web",
                startupPath,
                21,
                "Startup",
                "ControlBarController",
                ClassDependencyKind.ConstructorParameter,
                "new ControlBarController()",
                "Sample.Web.Startup",
                targetFullName: null,
                targetSourcePath: null));
        }

        return new ClassDependencyReport(
            new[]
            {
                new DiscoveredType(
                    "ControlBarController",
                    "Sample.Components.Controllers.ControlBarController",
                    ClassDiscoveredTypeKind.Class,
                    "Sample.Components",
                    componentPath,
                    7),
                new DiscoveredType(
                    "ControlBarController",
                    "Sample.InternalServices.ControlBarController",
                    ClassDiscoveredTypeKind.Class,
                    "Sample.InternalServices",
                    internalPath,
                    8),
                new DiscoveredType(
                    "Startup",
                    "Sample.Web.Startup",
                    ClassDiscoveredTypeKind.Class,
                    "Sample.Web",
                    startupPath,
                    5),
                new DiscoveredType(
                    "ComponentDependency",
                    "Sample.Components.ComponentDependency",
                    ClassDiscoveredTypeKind.Class,
                    "Sample.Components",
                    componentDependencyPath,
                    3),
                new DiscoveredType(
                    "InternalDependency",
                    "Sample.InternalServices.InternalDependency",
                    ClassDiscoveredTypeKind.Class,
                    "Sample.InternalServices",
                    internalDependencyPath,
                    3)
            },
            dependencies,
            new[]
            {
                CreateConcern(
                    ClassDependencyConcernSeverity.Medium,
                    "ControlBarController",
                    "ComponentDependency",
                    ClassDependencyKind.Field,
                    "Sample.Components",
                    componentPath,
                    10,
                    "private readonly ComponentDependency _dependency",
                    "Sample.Components.Controllers.ControlBarController",
                    "Sample.Components.ComponentDependency"),
                CreateConcern(
                    ClassDependencyConcernSeverity.Medium,
                    "ControlBarController",
                    "InternalDependency",
                    ClassDependencyKind.Field,
                    "Sample.InternalServices",
                    internalPath,
                    12,
                    "private readonly InternalDependency _dependency",
                    "Sample.InternalServices.ControlBarController",
                    "Sample.InternalServices.InternalDependency")
            },
            Array.Empty<ClassCouplingHotspot>(),
            SourceFileCount: 3);
    }

    private static ClassDependency CreateDependency(
        string projectName,
        string sourcePath,
        int lineNumber,
        string sourceType,
        string targetType,
        ClassDependencyKind kind,
        string evidence,
        string sourceFullName,
        string? targetFullName,
        string? targetSourcePath)
    {
        return new ClassDependency(
            projectName,
            sourcePath,
            lineNumber,
            sourceType,
            targetType,
            kind,
            evidence,
            sourceFullName,
            targetFullName,
            targetSourcePath);
    }

    private static ClassDependencyConcern CreateConcern(
        ClassDependencyConcernSeverity severity,
        string sourceType,
        string targetType,
        ClassDependencyKind dependencyKind,
        string projectName,
        string sourcePath,
        int lineNumber,
        string evidence,
        string sourceFullName,
        string? targetFullName)
    {
        return new ClassDependencyConcern(
            severity,
            sourceType,
            targetType,
            dependencyKind,
            projectName,
            sourcePath,
            lineNumber,
            evidence,
            "Concrete member dependencies increase coupling between types and may make substitution harder.",
            "Review whether this dependency should be represented by an interface, abstraction, or value object.",
            sourceFullName,
            targetFullName);
    }
}
