using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class ClassRefactoringOpportunitiesMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public ClassRefactoringOpportunitiesMarkdownReportWriterTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.ClassRefactoringOpportunitiesMarkdownReportWriterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_WhenOutputPathIsEmpty_ThrowsArgumentException()
    {
        var writer = new ClassRefactoringOpportunitiesMarkdownReportWriter();
        var report = CreateMatchedReport();

        var exception = Assert.Throws<ArgumentException>(() => writer.Write(string.Empty, report));

        Assert.Equal("outputPath", exception.ParamName);
    }

    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var writer = new ClassRefactoringOpportunitiesMarkdownReportWriter();
        var outputPath = Path.Combine(_tempDirectory, "class-refactoring-opportunities.md");

        var exception = Assert.Throws<ArgumentNullException>(() => writer.Write(outputPath, null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_WritesFocusedReportSections()
    {
        var writer = new ClassRefactoringOpportunitiesMarkdownReportWriter();
        var outputPath = Path.Combine(_tempDirectory, "class-refactoring-opportunities.Sample.CustomerService.20260620-153045.md");

        writer.Write(outputPath, CreateMatchedReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# Class Refactoring Opportunities", markdown);
        Assert.Contains("| Requested type | `Sample.CustomerService` |", markdown);
        Assert.Contains("| Existing seams | 1 |", markdown);
        Assert.Contains("| Missing or weak seams | 1 |", markdown);
        Assert.Contains("## Existing Seams", markdown);
        Assert.Contains("Constructor-injected interface", markdown);
        Assert.Contains("## Missing or Weak Seams", markdown);
        Assert.Contains("Hardcoded concrete object creation", markdown);
        Assert.Contains("## First Characterization Test Targets", markdown);
        Assert.Contains("DependencyBreakingNeededFirst", markdown);
        Assert.Contains("## Technique Recommendations", markdown);
        Assert.Contains("Parameterize Constructor", markdown);
        Assert.Contains("## Suggested Low-Risk / High-Value Order of Approach", markdown);
        Assert.Contains("## Effect Sketch", markdown);
        Assert.Contains("```mermaid", markdown);
        Assert.Contains("Recommendations are review guidance", markdown);
        Assert.Contains("did not build the solution", markdown);
    }

    [Fact]
    public void Write_WhenNoMatch_WritesNoMatchMessage()
    {
        var writer = new ClassRefactoringOpportunitiesMarkdownReportWriter();
        var outputPath = Path.Combine(_tempDirectory, "missing.md");

        writer.Write(outputPath, CreateNoMatchReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Type Not Found", markdown);
        Assert.Contains("Short-name matching was not attempted", markdown);
        Assert.Contains("| Source files analysed | 3 |", markdown);
        Assert.DoesNotContain("## Existing Seams", markdown);
    }

    [Fact]
    public void Write_WhenAmbiguous_WritesAmbiguityEvidence()
    {
        var writer = new ClassRefactoringOpportunitiesMarkdownReportWriter();
        var outputPath = Path.Combine(_tempDirectory, "ambiguous.md");

        writer.Write(outputPath, CreateAmbiguousReport());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Ambiguous Type Match", markdown);
        Assert.Contains("LegacyLens.NET did not guess", markdown);
        Assert.Contains("| `Sample.CustomerService` | One | `C:\\Repo\\One\\CustomerService.cs` | 4 |", markdown);
        Assert.Contains("| `Sample.CustomerService` | Two | `C:\\Repo\\Two\\CustomerService.cs` | 8 |", markdown);
    }

    [Fact]
    public void Write_EscapesMarkdownSensitiveTableValues()
    {
        var writer = new ClassRefactoringOpportunitiesMarkdownReportWriter();
        var outputPath = Path.Combine(_tempDirectory, "pipes.md");
        var report = CreateMatchedReportWithPipes();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Sample\\|Project", markdown);
        Assert.Contains("Customer\\|Service", markdown);
        Assert.Contains("new Repository\\|Type()", markdown);
        Assert.Contains("`C:\\Repo\\Sample\\|Project\\CustomerService.cs`", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static ClassRefactoringOpportunitiesReport CreateMatchedReport()
    {
        var generatedLocal = new DateTimeOffset(2026, 6, 20, 15, 30, 45, TimeSpan.FromHours(1));
        var generatedUtc = generatedLocal.ToUniversalTime();
        var sourcePath = @"C:\Repo\Sample\CustomerService.cs";
        var match = new ClassRefactoringTypeMatch("CustomerService", "Sample.CustomerService", "Sample.Project", sourcePath, 4);
        var method = new MethodRefactoringProfile(
            "Save",
            "void Save(Customer customer)",
            "public",
            "void",
            12,
            3,
            false,
            false,
            true,
            false,
            true,
            false,
            true,
            false,
            false,
            true,
            false,
            MethodRole.SideEffectingWorkflow,
            TestingPathClassification.DependencyBreakingNeededFirst,
            "public void Save(Customer customer)");

        var profile = new ClassRefactoringProfile(
            "CustomerService",
            "Sample.CustomerService",
            "Sample.Project",
            sourcePath,
            4,
            "public",
            false,
            false,
            true,
            ["ICustomerService"],
            ["ICustomerService"],
            ["ICustomerRepository"],
            ["CustomerRepository"],
            [],
            [method],
            3,
            2);

        var existingSeam = new ExistingSeam(
            "Constructor-injected interface",
            sourcePath,
            8,
            "CustomerService",
            "ICustomerRepository repository",
            "Use a fake implementation when adding characterization tests.");
        var missingSeam = new MissingOrWeakSeam(
            "Hardcoded concrete object creation",
            sourcePath,
            14,
            "Save",
            "new CustomerRepository()",
            "Parameterize Constructor");
        var barrier = new TestabilityBarrier(
            "Hardcoded concrete object creation",
            "High",
            sourcePath,
            14,
            "Save",
            "new CustomerRepository()",
            "The dependency may be difficult to substitute.");
        var target = new CharacterizationTestTarget(
            "Save",
            MethodRole.SideEffectingWorkflow.ToString(),
            TestingPathClassification.DependencyBreakingNeededFirst,
            3,
            sourcePath,
            12,
            "Create the smallest dependency-breaking seam needed before asserting behaviour.",
            "public void Save(Customer customer)");
        var signal = new RefactoringSignal(
            RefactoringSignalKind.HardcodedObjectCreation,
            RefactoringSignalStrength.High,
            RefactoringSignalConfidence.High,
            sourcePath,
            14,
            "Save",
            "new CustomerRepository()",
            "This may block direct unit-level characterization.",
            "Parameterize Constructor");
        var recommendation = new TechniqueRecommendation(
            LegacyCodeTechnique.ParameterizeConstructor,
            RecommendationStrength.Strong,
            "Hardcoded concrete object creation was found.",
            "Confirm object lifetime and runtime construction.",
            [new RecommendationBlocker("Runtime wiring not resolved.", "Static analysis only.")],
            "new CustomerRepository()");
        var notRecommended = new TechniqueRecommendation(
            LegacyCodeTechnique.BreakOutMethodObject,
            RecommendationStrength.NotEnoughEvidence,
            "No very-high complexity method was detected.",
            "Review manually.",
            [],
            "No very-high complexity method detected.");
        var step = new SuggestedRefactoringStep(
            1,
            "Break hardcoded construction only where it blocks the first characterization tests.",
            SuggestedStepRisk.Medium,
            SuggestedStepValue.High,
            "Dependency breaking should be narrow.",
            "new CustomerRepository()");

        return new ClassRefactoringOpportunitiesReport(
            "Sample.CustomerService",
            generatedLocal,
            generatedUtc,
            2,
            1,
            [match],
            profile,
            [signal],
            [existingSeam],
            [missingSeam],
            [barrier],
            [target],
            [recommendation],
            [notRecommended],
            [step]);
    }

    private static ClassRefactoringOpportunitiesReport CreateNoMatchReport()
    {
        var generatedLocal = new DateTimeOffset(2026, 6, 20, 15, 30, 45, TimeSpan.FromHours(1));
        return new ClassRefactoringOpportunitiesReport(
            "Sample.Missing",
            generatedLocal,
            generatedLocal.ToUniversalTime(),
            3,
            5,
            [],
            null,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }

    private static ClassRefactoringOpportunitiesReport CreateAmbiguousReport()
    {
        var generatedLocal = new DateTimeOffset(2026, 6, 20, 15, 30, 45, TimeSpan.FromHours(1));
        return new ClassRefactoringOpportunitiesReport(
            "Sample.CustomerService",
            generatedLocal,
            generatedLocal.ToUniversalTime(),
            2,
            2,
            [
                new ClassRefactoringTypeMatch("CustomerService", "Sample.CustomerService", "One", @"C:\Repo\One\CustomerService.cs", 4),
                new ClassRefactoringTypeMatch("CustomerService", "Sample.CustomerService", "Two", @"C:\Repo\Two\CustomerService.cs", 8)
            ],
            null,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }

    private static ClassRefactoringOpportunitiesReport CreateMatchedReportWithPipes()
    {
        var report = CreateMatchedReport();
        var sourcePath = @"C:\Repo\Sample|Project\CustomerService.cs";
        var profile = report.Profile! with
        {
            ProjectName = "Sample|Project",
            SourcePath = sourcePath,
            Name = "Customer|Service",
            FullName = "Sample.Customer|Service"
        };
        var missingSeam = report.MissingOrWeakSeams[0] with
        {
            SourcePath = sourcePath,
            Evidence = "new Repository|Type()"
        };

        return report with
        {
            RequestedTypeName = "Sample.Customer|Service",
            Profile = profile,
            MatchingTypes = [new ClassRefactoringTypeMatch("Customer|Service", "Sample.Customer|Service", "Sample|Project", sourcePath, 4)],
            MissingOrWeakSeams = [missingSeam]
        };
    }
}
