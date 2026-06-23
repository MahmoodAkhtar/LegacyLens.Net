using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Markdown;

public sealed class ClassRefactoringOpportunitiesMarkdownReportWriter
{
    public void Write(string outputPath, ClassRefactoringOpportunitiesReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(report);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(outputPath, append: false);
        Write(writer, report);
    }

    public void Write(TextWriter writer, ClassRefactoringOpportunitiesReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);

        writer.WriteLine("# Class Refactoring Opportunities");
        writer.WriteLine();
        writer.WriteLine("This report is a static, evidence-backed refactoring planning aid for one requested class. It does not refactor code, generate patches, or prove that a refactoring is safe.");
        writer.WriteLine();

        WriteSummary(writer, report);

        if (!report.IsFound)
        {
            WriteNoMatchOrAmbiguity(writer, report);
            WriteLimitations(writer);
            return;
        }

        WriteClassProfile(writer, report.Profile!);
        WriteExistingSeams(writer, report.ExistingSeams);
        WriteMissingOrWeakSeams(writer, report.MissingOrWeakSeams);
        WriteTestabilityBarriers(writer, report.TestabilityBarriers);
        WriteMethodProfiles(writer, report.Profile!.Methods);
        WriteCharacterizationTargets(writer, report.CharacterizationTestTargets);
        WriteSignals(writer, report.Signals);
        WriteSuggestedSteps(writer, report.SuggestedSteps);
        WriteTechniqueRecommendations(writer, report.TechniqueRecommendations);
        WriteNotRecommended(writer, report.NotRecommendedTechniques);
        WriteEffectSketch(writer, report);
        WriteLimitations(writer);
    }

    private static void WriteSummary(TextWriter writer, ClassRefactoringOpportunitiesReport report)
    {
        writer.WriteLine("## Summary");
        writer.WriteLine();
        writer.WriteLine("| Field | Value |");
        writer.WriteLine("| --- | --- |");
        writer.WriteLine($"| Requested type | {MarkdownTableCell.Code(report.RequestedTypeName)} |");
        writer.WriteLine($"| Generated local | {MarkdownTableCell.Code(report.GeneratedLocal.ToString("O"))} |");
        writer.WriteLine($"| Generated UTC | {MarkdownTableCell.Code(report.GeneratedUtc.ToString("O"))} |");
        writer.WriteLine($"| Source files analysed | {report.SourceFileCount} |");
        writer.WriteLine($"| Discovered class count | {report.DiscoveredTypeCount} |");
        writer.WriteLine($"| Match status | {MarkdownTableCell.Escape(CreateMatchStatus(report))} |");

        if (report.Profile is not null)
        {
            writer.WriteLine($"| Project | {MarkdownTableCell.Escape(report.Profile.ProjectName)} |");
            writer.WriteLine($"| Source path | {MarkdownTableCell.Code(report.Profile.SourcePath)} |");
            writer.WriteLine($"| Existing seams | {report.ExistingSeams.Count} |");
            writer.WriteLine($"| Missing or weak seams | {report.MissingOrWeakSeams.Count} |");
            writer.WriteLine($"| Testability barriers | {report.TestabilityBarriers.Count} |");
            writer.WriteLine($"| Characterization targets | {report.CharacterizationTestTargets.Count} |");
        }

        writer.WriteLine();
    }

    private static void WriteNoMatchOrAmbiguity(TextWriter writer, ClassRefactoringOpportunitiesReport report)
    {
        if (report.IsAmbiguous)
        {
            writer.WriteLine("## Ambiguous Type Match");
            writer.WriteLine();
            writer.WriteLine("The requested fully qualified type matched more than one discovered class. LegacyLens.NET did not guess which class to analyse.");
            writer.WriteLine();
            writer.WriteLine("| Full Name | Project | Source Path | Line |");
            writer.WriteLine("| --- | --- | --- | ---: |");

            foreach (var match in report.MatchingTypes)
            {
                writer.WriteLine($"| {MarkdownTableCell.Code(match.FullName)} | {MarkdownTableCell.Escape(match.ProjectName)} | {MarkdownTableCell.Code(match.SourcePath)} | {match.LineNumber} |");
            }

            writer.WriteLine();
            return;
        }

        writer.WriteLine("## Type Not Found");
        writer.WriteLine();
        writer.WriteLine("The requested fully qualified type was not found in the shared project-aware C# file inventory. Short-name matching was not attempted, because that could produce unsafe or misleading refactoring guidance.");
        writer.WriteLine();
    }

    private static void WriteClassProfile(TextWriter writer, ClassRefactoringProfile profile)
    {
        writer.WriteLine("## Class Profile");
        writer.WriteLine();
        writer.WriteLine("| Field | Value |");
        writer.WriteLine("| --- | --- |");
        writer.WriteLine($"| Type | {MarkdownTableCell.Code(profile.FullName)} |");
        writer.WriteLine($"| Project | {MarkdownTableCell.Escape(profile.ProjectName)} |");
        writer.WriteLine($"| Source path | {MarkdownTableCell.Code(profile.SourcePath)} |");
        writer.WriteLine($"| Line | {profile.LineNumber} |");
        writer.WriteLine($"| Accessibility | {MarkdownTableCell.Escape(profile.Accessibility)} |");
        writer.WriteLine($"| Static | {FormatBoolean(profile.IsStatic)} |");
        writer.WriteLine($"| Abstract | {FormatBoolean(profile.IsAbstract)} |");
        writer.WriteLine($"| Sealed | {FormatBoolean(profile.IsSealed)} |");
        writer.WriteLine($"| Base types/interfaces | {MarkdownTableCell.Escape(JoinOrNone(profile.BaseTypes))} |");
        writer.WriteLine($"| Members | {profile.MemberCount} |");
        writer.WriteLine($"| Methods analysed | {profile.Methods.Count} |");
        writer.WriteLine($"| Dependency-like syntax signals | {profile.DependencyLikeSignalCount} |");
        writer.WriteLine();
    }

    private static void WriteExistingSeams(TextWriter writer, IReadOnlyList<ExistingSeam> seams)
    {
        writer.WriteLine("## Existing Seams");
        writer.WriteLine();

        if (seams.Count == 0)
        {
            writer.WriteLine("No strong existing seams were found from syntax-only analysis.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Kind | Member | Line | Evidence | How To Use |");
        writer.WriteLine("| --- | --- | ---: | --- | --- |");
        foreach (var seam in seams)
        {
            writer.WriteLine($"| {MarkdownTableCell.Escape(seam.Kind)} | {MarkdownTableCell.Code(seam.MemberName)} | {seam.LineNumber} | {MarkdownTableCell.Evidence(seam.Evidence)} | {MarkdownTableCell.Escape(seam.HowToUse)} |");
        }

        writer.WriteLine();
    }

    private static void WriteMissingOrWeakSeams(TextWriter writer, IReadOnlyList<MissingOrWeakSeam> seams)
    {
        writer.WriteLine("## Missing or Weak Seams");
        writer.WriteLine();

        if (seams.Count == 0)
        {
            writer.WriteLine("No hardcoded construction, static/global access, or concrete collaborator seam gaps were found by the current static heuristics.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Kind | Member | Line | Evidence | Suggested Technique |");
        writer.WriteLine("| --- | --- | ---: | --- | --- |");
        foreach (var seam in seams)
        {
            writer.WriteLine($"| {MarkdownTableCell.Escape(seam.Kind)} | {MarkdownTableCell.Code(seam.MemberName)} | {seam.LineNumber} | {MarkdownTableCell.Evidence(seam.Evidence)} | {MarkdownTableCell.Escape(seam.SuggestedTechnique)} |");
        }

        writer.WriteLine();
    }

    private static void WriteTestabilityBarriers(TextWriter writer, IReadOnlyList<TestabilityBarrier> barriers)
    {
        writer.WriteLine("## Testability Barriers");
        writer.WriteLine();

        if (barriers.Count == 0)
        {
            writer.WriteLine("No strong testability barriers were found by the current static heuristics. This does not prove the class is easy or safe to change.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Barrier | Strength | Member | Line | Evidence | Why It Matters |");
        writer.WriteLine("| --- | --- | --- | ---: | --- | --- |");
        foreach (var barrier in barriers)
        {
            writer.WriteLine($"| {MarkdownTableCell.Escape(barrier.Kind)} | {MarkdownTableCell.Escape(barrier.Strength)} | {MarkdownTableCell.Code(barrier.MemberName)} | {barrier.LineNumber} | {MarkdownTableCell.Evidence(barrier.Evidence)} | {MarkdownTableCell.Escape(barrier.WhyItMatters)} |");
        }

        writer.WriteLine();
    }

    private static void WriteMethodProfiles(TextWriter writer, IReadOnlyList<MethodRefactoringProfile> methods)
    {
        writer.WriteLine("## Method Refactoring Profile");
        writer.WriteLine();

        if (methods.Count == 0)
        {
            writer.WriteLine("No methods were found on the requested class.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Method | Role | Testing Path | Complexity | Line | Evidence |");
        writer.WriteLine("| --- | --- | --- | ---: | ---: | --- |");
        foreach (var method in methods)
        {
            writer.WriteLine($"| {MarkdownTableCell.Code(method.Signature)} | {MarkdownTableCell.Escape(method.Role.ToString())} | {MarkdownTableCell.Escape(method.TestingPath.ToString())} | {method.Complexity} | {method.LineNumber} | {MarkdownTableCell.Evidence(method.Evidence)} |");
        }

        writer.WriteLine();
    }

    private static void WriteCharacterizationTargets(TextWriter writer, IReadOnlyList<CharacterizationTestTarget> targets)
    {
        writer.WriteLine("## First Characterization Test Targets");
        writer.WriteLine();

        if (targets.Count == 0)
        {
            writer.WriteLine("No public or internal method targets were found. Review constructors, framework entry points, or callers manually before refactoring.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Member | Role | Testing Path | Complexity | Line | Suggested First Test | Evidence |");
        writer.WriteLine("| --- | --- | --- | ---: | ---: | --- | --- |");
        foreach (var target in targets)
        {
            writer.WriteLine($"| {MarkdownTableCell.Code(target.MemberName)} | {MarkdownTableCell.Escape(target.MethodRole)} | {MarkdownTableCell.Escape(target.TestingPath.ToString())} | {target.Complexity} | {target.LineNumber} | {MarkdownTableCell.Escape(target.SuggestedFirstTest)} | {MarkdownTableCell.Evidence(target.Evidence)} |");
        }

        writer.WriteLine();
    }

    private static void WriteSignals(TextWriter writer, IReadOnlyList<RefactoringSignal> signals)
    {
        writer.WriteLine("## Evidence-Backed Signals");
        writer.WriteLine();

        if (signals.Count == 0)
        {
            writer.WriteLine("No strong evidence-backed refactoring signals were found. No strong recommendation is made from the current static profile.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Signal | Strength | Confidence | Member | Line | Evidence | Suggested Review |");
        writer.WriteLine("| --- | --- | --- | --- | ---: | --- | --- |");
        foreach (var signal in signals)
        {
            writer.WriteLine($"| {MarkdownTableCell.Escape(signal.Kind.ToString())} | {signal.Strength} | {signal.Confidence} | {MarkdownTableCell.Code(signal.MemberName)} | {signal.LineNumber} | {MarkdownTableCell.Evidence(signal.Evidence)} | {MarkdownTableCell.Escape(signal.SuggestedReview)} |");
        }

        writer.WriteLine();
    }

    private static void WriteSuggestedSteps(TextWriter writer, IReadOnlyList<SuggestedRefactoringStep> steps)
    {
        writer.WriteLine("## Suggested Low-Risk / High-Value Order of Approach");
        writer.WriteLine();

        if (steps.Count == 0)
        {
            writer.WriteLine("No strong recommendation. Not enough evidence was found for a class-specific order of approach.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Order | Step | Risk | Value | Why | Evidence |");
        writer.WriteLine("| ---: | --- | --- | --- | --- | --- |");
        foreach (var step in steps)
        {
            writer.WriteLine($"| {step.Order} | {MarkdownTableCell.Escape(step.Step)} | {step.Risk} | {step.Value} | {MarkdownTableCell.Escape(step.Why)} | {MarkdownTableCell.Evidence(step.Evidence)} |");
        }

        writer.WriteLine();
    }

    private static void WriteTechniqueRecommendations(TextWriter writer, IReadOnlyList<TechniqueRecommendation> recommendations)
    {
        writer.WriteLine("## Technique Recommendations");
        writer.WriteLine();

        if (recommendations.Count == 0)
        {
            writer.WriteLine("No strong technique recommendation was made. Not enough evidence was found for class-specific technique guidance.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Technique | Strength | Why It Applies | What Needs Human Review | Evidence |");
        writer.WriteLine("| --- | --- | --- | --- | --- |");
        foreach (var recommendation in recommendations)
        {
            writer.WriteLine($"| {MarkdownTableCell.Escape(FormatTechnique(recommendation.Technique))} | {recommendation.Strength} | {MarkdownTableCell.Escape(recommendation.WhyItApplies)} | {MarkdownTableCell.Escape(recommendation.HumanReviewRequired)} | {MarkdownTableCell.Evidence(recommendation.Evidence)} |");
        }

        writer.WriteLine();
        WriteRecommendationBlockers(writer, recommendations);
    }

    private static void WriteRecommendationBlockers(TextWriter writer, IReadOnlyList<TechniqueRecommendation> recommendations)
    {
        var blockers = recommendations
            .SelectMany(recommendation => recommendation.Blockers.Select(blocker => (recommendation.Technique, blocker)))
            .ToArray();

        if (blockers.Length == 0)
        {
            return;
        }

        writer.WriteLine("### Recommendation Blockers / Review Gates");
        writer.WriteLine();
        writer.WriteLine("| Technique | Review Gate | Evidence |");
        writer.WriteLine("| --- | --- | --- |");
        foreach (var item in blockers)
        {
            writer.WriteLine($"| {MarkdownTableCell.Escape(FormatTechnique(item.Technique))} | {MarkdownTableCell.Escape(item.blocker.Reason)} | {MarkdownTableCell.Evidence(item.blocker.Evidence)} |");
        }

        writer.WriteLine();
    }

    private static void WriteNotRecommended(TextWriter writer, IReadOnlyList<TechniqueRecommendation> recommendations)
    {
        writer.WriteLine("## Techniques Not Recommended or Not Enough Evidence");
        writer.WriteLine();

        if (recommendations.Count == 0)
        {
            writer.WriteLine("No explicit technique suppressions were recorded.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Technique | Status | Reason | Evidence |");
        writer.WriteLine("| --- | --- | --- | --- |");
        foreach (var recommendation in recommendations)
        {
            writer.WriteLine($"| {MarkdownTableCell.Escape(FormatTechnique(recommendation.Technique))} | {MarkdownTableCell.Escape(recommendation.Strength.ToString())} | {MarkdownTableCell.Escape(recommendation.WhyItApplies)} | {MarkdownTableCell.Evidence(recommendation.Evidence)} |");
        }

        writer.WriteLine();
    }

    private static void WriteEffectSketch(TextWriter writer, ClassRefactoringOpportunitiesReport report)
    {
        writer.WriteLine("## Effect Sketch");
        writer.WriteLine();

        if (report.Profile is null)
        {
            writer.WriteLine("No effect sketch is available because the requested type was not uniquely resolved.");
            writer.WriteLine();
            return;
        }

        var targetNodes = report.MissingOrWeakSeams
            .Select(seam => ExtractTargetNode(seam.Evidence))
            .Where(node => !string.IsNullOrWhiteSpace(node))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

        if (targetNodes.Length == 0)
        {
            writer.WriteLine("No compact Mermaid effect sketch was generated because no obvious hardcoded or static dependency targets were found.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("```mermaid");
        writer.WriteLine("graph TD");
        foreach (var target in targetNodes)
        {
            writer.WriteLine($"    {SanitizeMermaidNode(report.Profile.Name)} --> {SanitizeMermaidNode(target)}");
        }
        writer.WriteLine("```");
        writer.WriteLine();
    }

    private static void WriteLimitations(TextWriter writer)
    {
        writer.WriteLine("## Notes and Limitations");
        writer.WriteLine();
        writer.WriteLine("- This report is based on static source analysis over the shared project-aware C# file inventory only.");
        writer.WriteLine("- LegacyLens.NET did not build the solution, restore NuGet packages, run tests, execute the application, or connect to external systems.");
        writer.WriteLine("- LegacyLens.NET did not resolve runtime dependency injection, reflection, dynamic loading, generated-code behaviour, or runtime call graphs.");
        writer.WriteLine("- Recommendations are review guidance, not automatic refactorings, code changes, or proof that a refactoring is safe.");
        writer.WriteLine("- Findings do not prove runtime behaviour, production usage, unused dependencies, correctness, defects, or completeness.");
        writer.WriteLine("- Suggested steps should be reviewed by the development team before code changes are made.");
        writer.WriteLine();
    }

    private static string CreateMatchStatus(ClassRefactoringOpportunitiesReport report)
    {
        if (report.IsAmbiguous)
        {
            return "Ambiguous duplicate full-name matches";
        }

        if (!report.IsFound)
        {
            return "No matching fully qualified class found";
        }

        return "Single fully qualified class match";
    }

    private static string JoinOrNone(IReadOnlyCollection<string> values)
    {
        return values.Count == 0 ? "None" : string.Join(", ", values);
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "Yes" : "No";
    }

    private static string FormatTechnique(LegacyCodeTechnique technique)
    {
        return technique switch
        {
            LegacyCodeTechnique.CharacterizationTests => "Characterization Tests",
            LegacyCodeTechnique.ExtractInterface => "Extract Interface",
            LegacyCodeTechnique.ParameterizeConstructor => "Parameterize Constructor",
            LegacyCodeTechnique.ParameterizeMethod => "Parameterize Method",
            LegacyCodeTechnique.ExtractAndOverrideFactoryMethod => "Extract and Override Factory Method",
            LegacyCodeTechnique.ExtractAndOverrideCall => "Extract and Override Call",
            LegacyCodeTechnique.EncapsulateGlobalReferences => "Encapsulate Global References",
            LegacyCodeTechnique.SproutMethod => "Sprout Method",
            LegacyCodeTechnique.SproutClass => "Sprout Class",
            LegacyCodeTechnique.ExtractMethod => "Extract Method",
            LegacyCodeTechnique.BreakOutMethodObject => "Break Out Method Object",
            LegacyCodeTechnique.WrapClass => "Wrap Class",
            LegacyCodeTechnique.AdaptParameter => "Adapt Parameter",
            LegacyCodeTechnique.HigherLevelTestsFirst => "Higher-Level Tests First",
            _ => technique.ToString()
        };
    }

    private static string ExtractTargetNode(string evidence)
    {
        var cleaned = evidence
            .Replace("new ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("()", string.Empty, StringComparison.Ordinal)
            .Replace(";", string.Empty, StringComparison.Ordinal)
            .Trim();

        var paren = cleaned.IndexOf('(', StringComparison.Ordinal);
        if (paren > 0)
        {
            cleaned = cleaned[..paren];
        }

        var dot = cleaned.IndexOf('.', StringComparison.Ordinal);
        if (dot > 0)
        {
            cleaned = cleaned[..dot];
        }

        var tokens = cleaned.Split([' ', '<', '>', '[', ']'], StringSplitOptions.RemoveEmptyEntries);
        return tokens.FirstOrDefault(token => token.Length > 0) ?? string.Empty;
    }

    private static string SanitizeMermaidNode(string value)
    {
        var chars = value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray();
        var sanitized = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
    }
}
