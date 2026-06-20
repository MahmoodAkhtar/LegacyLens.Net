using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Mermaid;
using Xunit;

namespace LegacyLens.Reporting.Tests.Mermaid;

public sealed class ScopedClassDependencyMermaidDiagramWriterTests
{
    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var writer = new ScopedClassDependencyMermaidDiagramWriter();

        var exception = Assert.Throws<ArgumentNullException>(() => writer.Write(null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_WhenOutboundEdgesExceedCap_PreservesInboundEdge()
    {
        var rootPath = @"C:\Repo\Sample\RootType.cs";
        var root = new DiscoveredType(
            "RootType",
            "Sample.RootType",
            ClassDiscoveredTypeKind.Class,
            "Sample",
            rootPath,
            7);

        var outbound = Enumerable
            .Range(1, 10)
            .Select(index => new ClassDependency(
                "Sample",
                rootPath,
                index,
                "RootType",
                $"Dependency{index}",
                ClassDependencyKind.Field,
                $"private readonly Dependency{index} _dependency{index}",
                "Sample.RootType",
                $"Sample.Dependency{index}",
                $@"C:\Repo\Sample\Dependency{index}.cs"))
            .ToArray();

        var inbound = new[]
        {
            new ClassDependency(
                "Sample.Web",
                @"C:\Repo\Sample\Startup.cs",
                20,
                "Startup",
                "RootType",
                ClassDependencyKind.GenericTypeArgument,
                "AddTransient<IRootType, RootType>",
                "Sample.Web.Startup",
                "Sample.RootType",
                rootPath)
        };

        var report = new ScopedClassDependencyReport(
            "Sample.RootType",
            DateTimeOffset.Now,
            DateTimeOffset.UtcNow,
            SourceFileCount: 3,
            DiscoveredTypeCount: 12,
            MatchingTypes: new[] { root },
            RootType: root,
            OutboundDependencies: outbound,
            InboundDependants: inbound,
            Concerns: Array.Empty<ClassDependencyConcern>());

        var writer = new ScopedClassDependencyMermaidDiagramWriter();

        var mermaid = writer.Write(report, maxEdges: 3);

        Assert.Contains("Startup -->|generic type| RootType", mermaid);
        Assert.Contains("RootType -->|field| Dependency1", mermaid);
        Assert.Contains("MoreOutboundDependencies[More outbound dependencies omitted from compact diagram]", mermaid);
    }

    [Fact]
    public void Write_WhenNoDependencies_WritesNoDependenciesNode()
    {
        var root = new DiscoveredType(
            "RootType",
            "Sample.RootType",
            ClassDiscoveredTypeKind.Class,
            "Sample",
            @"C:\Repo\Sample\RootType.cs",
            7);

        var report = new ScopedClassDependencyReport(
            "Sample.RootType",
            DateTimeOffset.Now,
            DateTimeOffset.UtcNow,
            SourceFileCount: 1,
            DiscoveredTypeCount: 1,
            MatchingTypes: new[] { root },
            RootType: root,
            OutboundDependencies: Array.Empty<ClassDependency>(),
            InboundDependants: Array.Empty<ClassDependency>(),
            Concerns: Array.Empty<ClassDependencyConcern>());

        var writer = new ScopedClassDependencyMermaidDiagramWriter();

        var mermaid = writer.Write(report);

        Assert.Contains("RootType --> No_Direct_Dependencies[No direct source-level dependencies discovered]", mermaid);
    }
}
