using LegacyLens.Core.Analysis;
using LegacyLens.Core.Files;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ClassRefactoringOpportunitiesAnalyzerTests
{
    private static readonly DateTimeOffset GeneratedLocal = new(2026, 6, 20, 15, 30, 45, TimeSpan.FromHours(1));
    private static readonly DateTimeOffset GeneratedUtc = GeneratedLocal.ToUniversalTime();

    [Fact]
    public void Analyze_WhenFilesAreNull_ThrowsArgumentNullException()
    {
        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(null!, "Sample.CustomerService", GeneratedLocal, GeneratedUtc));

        Assert.Equal("csharpFiles", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenRequestedTypeNameIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(Array.Empty<ScanFile>(), null!, GeneratedLocal, GeneratedUtc));

        Assert.Equal("requestedTypeName", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Analyze_WhenRequestedTypeNameIsWhiteSpace_ThrowsArgumentException(string requestedTypeName)
    {
        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();

        var exception = Assert.Throws<ArgumentException>(() =>
            analyzer.Analyze(Array.Empty<ScanFile>(), requestedTypeName, GeneratedLocal, GeneratedUtc));

        Assert.Equal("requestedTypeName", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenNoFullNameMatch_DoesNotFallBackToShortName()
    {
        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();

        var report = analyzer.Analyze(
            [CreateScanFile("Sample.CustomerService.cs", "namespace Sample; public sealed class CustomerService { }")],
            "CustomerService",
            GeneratedLocal,
            GeneratedUtc);

        Assert.False(report.IsFound);
        Assert.False(report.IsAmbiguous);
        Assert.Empty(report.MatchingTypes);
        Assert.Equal(1, report.SourceFileCount);
        Assert.Equal(1, report.DiscoveredTypeCount);
        Assert.Contains(report.NotRecommendedTechniques, recommendation =>
            recommendation.Strength == RecommendationStrength.NotEnoughEvidence);
    }

    [Fact]
    public void Analyze_WhenDuplicateFullNameMatches_ReturnsAmbiguityWithoutGuessing()
    {
        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();

        var report = analyzer.Analyze(
            [
                CreateScanFile("One.cs", "namespace Sample; public sealed class CustomerService { public int Get() => 1; }", projectName: "One"),
                CreateScanFile("Two.cs", "namespace Sample; public sealed class CustomerService { public int Get() => 2; }", projectName: "Two")
            ],
            "Sample.CustomerService",
            GeneratedLocal,
            GeneratedUtc);

        Assert.False(report.IsFound);
        Assert.True(report.IsAmbiguous);
        Assert.Equal(2, report.MatchingTypes.Count);
        Assert.Null(report.Profile);
        Assert.Empty(report.ExistingSeams);
        Assert.Empty(report.MissingOrWeakSeams);
    }

    [Fact]
    public void Analyze_ForAlreadyTestableClass_ReturnsDirectCharacterizationAndNoStrongDependencyBreaking()
    {
        var source = """
            namespace Sample;

            public sealed class PriceCalculator
            {
                public decimal Calculate(Order order)
                {
                    return order.Total * 0.2m;
                }
            }

            public sealed class Order
            {
                public decimal Total { get; set; }
            }
            """;
        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();

        var report = analyzer.Analyze(
            [CreateScanFile("PriceCalculator.cs", source)],
            "Sample.PriceCalculator",
            GeneratedLocal,
            GeneratedUtc);

        Assert.True(report.IsFound);
        Assert.Empty(report.MissingOrWeakSeams);
        Assert.Empty(report.TestabilityBarriers);
        Assert.Contains(report.Profile!.Methods, method =>
            method.Name == "Calculate" &&
            method.Role == MethodRole.PureOrPureishCalculation &&
            method.TestingPath == TestingPathClassification.DirectCharacterizationPossible);
        Assert.Contains(report.CharacterizationTestTargets, target =>
            target.MemberName == "Calculate" &&
            target.TestingPath == TestingPathClassification.DirectCharacterizationPossible);
        Assert.DoesNotContain(report.TechniqueRecommendations, recommendation =>
            recommendation.Technique == LegacyCodeTechnique.ParameterizeConstructor &&
            recommendation.Strength == RecommendationStrength.Strong);
        Assert.Contains(report.SuggestedSteps, step => step.Step.Contains("Capture current behaviour", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_ForHardcodedDependencyClass_ReportsMissingSeamAndDependencyBreaking()
    {
        var source = """
            namespace Sample;

            public sealed class CustomerService
            {
                public void Save(Customer customer)
                {
                    var repository = new CustomerRepository();
                    repository.Save(customer);
                }
            }

            public sealed class Customer { }
            public sealed class CustomerRepository
            {
                public void Save(Customer customer) { }
            }
            """;
        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();

        var report = analyzer.Analyze(
            [CreateScanFile("CustomerService.cs", source)],
            "Sample.CustomerService",
            GeneratedLocal,
            GeneratedUtc);

        Assert.True(report.IsFound);
        Assert.Contains(report.MissingOrWeakSeams, seam => seam.Kind == "Hardcoded concrete object creation");
        Assert.Contains(report.TestabilityBarriers, barrier => barrier.Kind == "Hardcoded concrete object creation");
        Assert.Contains(report.Profile!.Methods, method =>
            method.Name == "Save" &&
            method.TestingPath == TestingPathClassification.DependencyBreakingNeededFirst);
        Assert.Contains(report.TechniqueRecommendations, recommendation =>
            recommendation.Technique == LegacyCodeTechnique.ParameterizeConstructor &&
            recommendation.Strength == RecommendationStrength.Strong);
        Assert.Contains(report.SuggestedSteps, step => step.Step.Contains("Break hardcoded construction", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_WhenExistingInterfaceSeamExists_DowngradesExtractInterfaceRecommendation()
    {
        var source = """
            namespace Sample;

            public interface ICustomerRepository
            {
                void Save(Customer customer);
            }

            public sealed class CustomerService
            {
                private readonly ICustomerRepository _repository;

                public CustomerService(ICustomerRepository repository)
                {
                    _repository = repository;
                }

                public void Save(Customer customer)
                {
                    _repository.Save(customer);
                }
            }

            public sealed class Customer { }
            """;
        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();

        var report = analyzer.Analyze(
            [CreateScanFile("CustomerService.cs", source)],
            "Sample.CustomerService",
            GeneratedLocal,
            GeneratedUtc);

        Assert.True(report.IsFound);
        Assert.Contains(report.ExistingSeams, seam => seam.Kind == "Constructor-injected interface");
        Assert.DoesNotContain(report.MissingOrWeakSeams, seam => seam.Kind == "Hardcoded concrete object creation");
        Assert.Contains(report.NotRecommendedTechniques, recommendation =>
            recommendation.Technique == LegacyCodeTechnique.ExtractInterface &&
            recommendation.Strength == RecommendationStrength.NotRecommended);
        Assert.Contains(report.Profile!.Methods, method =>
            method.Name == "Save" &&
            method.TestingPath == TestingPathClassification.CharacterizationViaExistingSeams);
    }

    [Fact]
    public void Analyze_WhenStaticGlobalAccessExists_RecommendsEncapsulation()
    {
        var source = """
            using System.Configuration;
            namespace Sample;

            public sealed class CustomerService
            {
                public string GetQueueName()
                {
                    return ConfigurationManager.AppSettings["QueueName"];
                }
            }
            """;
        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();

        var report = analyzer.Analyze(
            [CreateScanFile("CustomerService.cs", source)],
            "Sample.CustomerService",
            GeneratedLocal,
            GeneratedUtc);

        Assert.True(report.IsFound);
        Assert.Contains(report.MissingOrWeakSeams, seam => seam.Kind == "Static or global dependency access");
        Assert.Contains(report.TechniqueRecommendations, recommendation =>
            recommendation.Technique == LegacyCodeTechnique.EncapsulateGlobalReferences &&
            recommendation.Strength == RecommendationStrength.Strong);
        Assert.Contains(report.Profile!.Methods, method =>
            method.Name == "GetQueueName" &&
            method.Role == MethodRole.ConfigurationAccessMethod);
    }

    [Fact]
    public void Analyze_OnlyUsesProvidedSharedInventoryFiles()
    {
        var analyzer = new ClassRefactoringOpportunitiesAnalyzer();

        var report = analyzer.Analyze(
            [CreateScanFile("Included.cs", "namespace Included; public sealed class Visible { public int Get() => 1; }")],
            "Excluded.Hidden",
            GeneratedLocal,
            GeneratedUtc);

        Assert.False(report.IsFound);
        Assert.Equal(1, report.SourceFileCount);
        Assert.Equal(1, report.DiscoveredTypeCount);
    }

    private static ScanFile CreateScanFile(
        string fileName,
        string content,
        string projectName = "Sample.Project")
    {
        var fullPath = Path.Combine("C:\\Repo", projectName, fileName);

        return new ScanFile(
            projectName,
            Path.Combine("C:\\Repo", projectName, projectName + ".csproj"),
            Path.Combine("C:\\Repo", projectName),
            fullPath,
            fileName,
            ".cs",
            content);
    }
}
