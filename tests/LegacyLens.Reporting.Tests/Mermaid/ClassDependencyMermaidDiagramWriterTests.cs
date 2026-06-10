using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Mermaid;
using Xunit;

namespace LegacyLens.Reporting.Tests.Mermaid;

public sealed class ClassDependencyMermaidDiagramWriterTests
{
    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var writer = new ClassDependencyMermaidDiagramWriter();

        var exception = Assert.Throws<ArgumentNullException>(() => writer.Write(null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_WhenReportHasNoDependencies_WritesEmptyDiagramNode()
    {
        var writer = new ClassDependencyMermaidDiagramWriter();

        var mermaid = writer.Write(CreateReport());

        Assert.Contains("```mermaid", mermaid);
        Assert.Contains("graph TD", mermaid);
        Assert.Contains("No_Class_Dependencies_Discovered[No class dependencies discovered]", mermaid);
        Assert.EndsWith("```" + Environment.NewLine, mermaid);
    }

    [Fact]
    public void Write_WhenReportHasDependency_WritesEdgeWithDependencyKindLabel()
    {
        var report = CreateReport(
            dependencies:
            [
                CreateDependency(
                    sourceType: "OrderService",
                    targetType: "OrderRepository",
                    kind: ClassDependencyKind.ObjectCreation,
                    evidence: "new OrderRepository()")
            ]);

        var writer = new ClassDependencyMermaidDiagramWriter();

        var mermaid = writer.Write(report);

        Assert.Contains("```mermaid", mermaid);
        Assert.Contains("graph TD", mermaid);
        Assert.Contains("OrderService -->|hardcoded new| OrderRepository", mermaid);
    }

    [Fact]
    public void Write_WhenMultipleKindsExistBetweenSameTypes_GroupsKindsIntoSingleEdge()
    {
        var report = CreateReport(
            dependencies:
            [
                CreateDependency(
                    sourceType: "OrderService",
                    targetType: "OrderRepository",
                    kind: ClassDependencyKind.ObjectCreation,
                    evidence: "new OrderRepository()"),

                CreateDependency(
                    sourceType: "OrderService",
                    targetType: "OrderRepository",
                    kind: ClassDependencyKind.Field,
                    evidence: "private readonly OrderRepository _repository;"),

                CreateDependency(
                    sourceType: "OrderService",
                    targetType: "OrderRepository",
                    kind: ClassDependencyKind.ConstructorParameter,
                    evidence: "public OrderService(OrderRepository repository)")
            ]);

        var writer = new ClassDependencyMermaidDiagramWriter();

        var mermaid = writer.Write(report);

        Assert.Contains("OrderService -->|constructor parameter, field, hardcoded new| OrderRepository", mermaid);
        Assert.Equal(1, CountOccurrences(mermaid, "OrderService -->|"));
    }

    [Fact]
    public void Write_WhenDuplicateKindsExistBetweenSameTypes_DeduplicatesLabels()
    {
        var report = CreateReport(
            dependencies:
            [
                CreateDependency(
                    sourceType: "OrderService",
                    targetType: "OrderRepository",
                    kind: ClassDependencyKind.ObjectCreation,
                    evidence: "new OrderRepository()"),

                CreateDependency(
                    sourceType: "OrderService",
                    targetType: "OrderRepository",
                    kind: ClassDependencyKind.ObjectCreation,
                    evidence: "new OrderRepository()")
            ]);

        var writer = new ClassDependencyMermaidDiagramWriter();

        var mermaid = writer.Write(report);

        Assert.Contains("OrderService -->|hardcoded new| OrderRepository", mermaid);
        Assert.DoesNotContain("hardcoded new, hardcoded new", mermaid);
        Assert.Equal(1, CountOccurrences(mermaid, "OrderService -->|"));
    }

    [Fact]
    public void Write_WhenTypeNamesContainMermaidUnsafeCharacters_SanitizesNodeNames()
    {
        var report = CreateReport(
            dependencies:
            [
                CreateDependency(
                    sourceType: "Order.Service",
                    targetType: "I-Order Repository",
                    kind: ClassDependencyKind.ConstructorParameter,
                    evidence: "public OrderService(IOrderRepository repository)")
            ]);

        var writer = new ClassDependencyMermaidDiagramWriter();

        var mermaid = writer.Write(report);

        Assert.Contains("Order_Service -->|constructor parameter| I_Order_Repository", mermaid);
        Assert.DoesNotContain("Order.Service -->", mermaid);
        Assert.DoesNotContain("I-Order Repository", mermaid);
    }

    [Fact]
    public void Write_WhenConcernsExist_OrdersConcernEdgesFirst()
    {
        var report = CreateReport(
            dependencies:
            [
                CreateDependency(
                    sourceType: "AlphaService",
                    targetType: "AlphaRepository",
                    kind: ClassDependencyKind.Field,
                    evidence: "private AlphaRepository _repository;"),

                CreateDependency(
                    sourceType: "CriticalService",
                    targetType: "ConfigurationManager",
                    kind: ClassDependencyKind.StaticMemberAccess,
                    evidence: "ConfigurationManager.AppSettings"),

                CreateDependency(
                    sourceType: "BetaService",
                    targetType: "BetaRepository",
                    kind: ClassDependencyKind.Field,
                    evidence: "private BetaRepository _repository;")
            ],
            concerns:
            [
                CreateConcern(
                    sourceType: "CriticalService",
                    targetType: "ConfigurationManager",
                    dependencyKind: ClassDependencyKind.StaticMemberAccess,
                    evidence: "ConfigurationManager.AppSettings")
            ]);

        var writer = new ClassDependencyMermaidDiagramWriter();

        var mermaid = writer.Write(report);

        var criticalIndex = mermaid.IndexOf("CriticalService -->|static access| ConfigurationManager", StringComparison.Ordinal);
        var alphaIndex = mermaid.IndexOf("AlphaService -->|field| AlphaRepository", StringComparison.Ordinal);
        var betaIndex = mermaid.IndexOf("BetaService -->|field| BetaRepository", StringComparison.Ordinal);

        Assert.True(criticalIndex >= 0);
        Assert.True(alphaIndex >= 0);
        Assert.True(betaIndex >= 0);
        Assert.True(criticalIndex < alphaIndex);
        Assert.True(criticalIndex < betaIndex);
    }

    [Fact]
    public void Write_WhenMaxEdgesIsSpecified_LimitsNumberOfEdges()
    {
        var report = CreateReport(
            dependencies:
            [
                CreateDependency(
                    sourceType: "AService",
                    targetType: "ARepository",
                    kind: ClassDependencyKind.Field,
                    evidence: "private ARepository _repository;"),

                CreateDependency(
                    sourceType: "BService",
                    targetType: "BRepository",
                    kind: ClassDependencyKind.Field,
                    evidence: "private BRepository _repository;"),

                CreateDependency(
                    sourceType: "CService",
                    targetType: "CRepository",
                    kind: ClassDependencyKind.Field,
                    evidence: "private CRepository _repository;")
            ]);

        var writer = new ClassDependencyMermaidDiagramWriter();

        var mermaid = writer.Write(report, maxEdges: 2);

        Assert.Contains("AService -->|field| ARepository", mermaid);
        Assert.Contains("BService -->|field| BRepository", mermaid);
        Assert.DoesNotContain("CService -->|field| CRepository", mermaid);
    }

    [Fact]
    public void Write_WritesFriendlyLabelsForEveryDependencyKind()
    {
        var dependencies = Enum
            .GetValues<ClassDependencyKind>()
            .Select((kind, index) => CreateDependency(
                sourceType: $"Source{index}",
                targetType: $"Target{index}",
                kind: kind,
                evidence: $"Evidence {index}"))
            .ToArray();

        var report = CreateReport(dependencies);

        var writer = new ClassDependencyMermaidDiagramWriter();

        var mermaid = writer.Write(report);

        Assert.Contains("Source0 -->|constructor parameter| Target0", mermaid);
        Assert.Contains("Source1 -->|field| Target1", mermaid);
        Assert.Contains("Source2 -->|property| Target2", mermaid);
        Assert.Contains("Source3 -->|method parameter| Target3", mermaid);
        Assert.Contains("Source4 -->|return type| Target4", mermaid);
        Assert.Contains("Source5 -->|local variable| Target5", mermaid);
        Assert.Contains("Source6 -->|hardcoded new| Target6", mermaid);
        Assert.Contains("Source7 -->|static access| Target7", mermaid);
        Assert.Contains("Source8 -->|inherits| Target8", mermaid);
        Assert.Contains("Source9 -->|implements| Target9", mermaid);
        Assert.Contains("Source10 -->|attribute| Target10", mermaid);
        Assert.Contains("Source11 -->|generic type| Target11", mermaid);
    }

    private static ClassDependencyReport CreateReport(
        IReadOnlyList<ClassDependency>? dependencies = null,
        IReadOnlyList<ClassDependencyConcern>? concerns = null)
    {
        return new ClassDependencyReport(
            Types: Array.Empty<DiscoveredType>(),
            Dependencies: dependencies ?? Array.Empty<ClassDependency>(),
            Concerns: concerns ?? Array.Empty<ClassDependencyConcern>(),
            Hotspots: Array.Empty<ClassCouplingHotspot>(),
            SourceFileCount: 0);
    }

    private static ClassDependency CreateDependency(
        string sourceType,
        string targetType,
        ClassDependencyKind kind,
        string evidence)
    {
        return new ClassDependency(
            ProjectName: "SampleLegacyApp.Services",
            SourcePath: @"C:\Repo\SampleLegacyApp.Services\OrderService.cs",
            LineNumber: 12,
            SourceType: sourceType,
            TargetType: targetType,
            Kind: kind,
            Evidence: evidence);
    }

    private static ClassDependencyConcern CreateConcern(
        string sourceType,
        string targetType,
        ClassDependencyKind dependencyKind,
        string evidence)
    {
        return new ClassDependencyConcern(
            Severity: ClassDependencyConcernSeverity.Medium,
            SourceType: sourceType,
            TargetType: targetType,
            DependencyKind: dependencyKind,
            ProjectName: "SampleLegacyApp.Services",
            SourcePath: @"C:\Repo\SampleLegacyApp.Services\OrderService.cs",
            LineNumber: 12,
            Evidence: evidence,
            WhyItMatters: "Static or concrete dependency evidence should be reviewed.",
            Recommendation: "Review whether this dependency needs an abstraction during migration.");
    }

    private static int CountOccurrences(string value, string searchText)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(searchText, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += searchText.Length;
        }

        return count;
    }
}