using LegacyLens.Core.Files;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LegacyLens.Core.Analysis;

public sealed class CodeComplexityAnalyzer
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    public CodeComplexityReport Analyze(ScanFileInventory fileInventory)
    {
        ArgumentNullException.ThrowIfNull(fileInventory);

        var members = fileInventory.CSharpFiles
            .SelectMany(AnalyzeFile)
            .OrderBy(member => member.ProjectName, NameComparer)
            .ThenBy(member => member.SourcePath, NameComparer)
            .ThenBy(member => member.LineNumber)
            .ThenBy(member => member.MemberName, NameComparer)
            .ToArray();

        var typeSummaries = CreateTypeSummaries(members);
        var namespaceSummaries = CreateNamespaceSummaries(typeSummaries);
        var projectSummaries = CreateProjectSummaries(typeSummaries);
        var summary = CreateScanSummary(fileInventory.CSharpFiles.Count, members, typeSummaries);

        return new CodeComplexityReport(
            summary,
            members,
            typeSummaries,
            namespaceSummaries,
            projectSummaries);
    }

    private static IEnumerable<CodeComplexityMember> AnalyzeFile(ScanFile file)
    {
        if (string.IsNullOrWhiteSpace(file.Content))
        {
            yield break;
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(file.Content);
        var root = syntaxTree.GetCompilationUnitRoot();
        var isLikelyGenerated = IsLikelyGeneratedFile(file);

        foreach (var member in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
        {
            if (!BelongsToOwnMember(member))
            {
                continue;
            }

            yield return CreateMemberResult(file, syntaxTree, member, isLikelyGenerated);
        }

        foreach (var accessor in root.DescendantNodes().OfType<AccessorDeclarationSyntax>())
        {
            if (!BelongsToOwnMember(accessor))
            {
                continue;
            }

            yield return CreateAccessorResult(file, syntaxTree, accessor, isLikelyGenerated);
        }

        foreach (var localFunction in root.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
        {
            yield return CreateLocalFunctionResult(file, syntaxTree, localFunction, isLikelyGenerated);
        }

        var globalStatements = root.Members.OfType<GlobalStatementSyntax>().ToArray();
        if (globalStatements.Length > 0)
        {
            yield return CreateTopLevelStatementsResult(file, syntaxTree, globalStatements, isLikelyGenerated);
        }
    }

    private static CodeComplexityMember CreateMemberResult(
        ScanFile file,
        SyntaxTree syntaxTree,
        BaseMethodDeclarationSyntax member,
        bool isLikelyGenerated)
    {
        var typeDeclaration = member.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        var namespaceName = GetNamespaceName(member);
        var typeName = typeDeclaration is null ? "<global>" : CreateFullTypeName(typeDeclaration);
        var memberName = GetMemberName(member);
        var kind = GetMemberKind(member);
        var complexity = CalculateComplexity(member);

        return new CodeComplexityMember(
            file.ProjectName,
            file.FullPath,
            GetLineNumber(syntaxTree, member),
            namespaceName,
            typeName,
            memberName,
            kind,
            complexity,
            ClassifySeverity(complexity),
            isLikelyGenerated,
            GetEvidence(member));
    }

    private static CodeComplexityMember CreateAccessorResult(
        ScanFile file,
        SyntaxTree syntaxTree,
        AccessorDeclarationSyntax accessor,
        bool isLikelyGenerated)
    {
        var typeDeclaration = accessor.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        var property = accessor.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
        var indexer = accessor.Ancestors().OfType<IndexerDeclarationSyntax>().FirstOrDefault();
        var eventDeclaration = accessor.Ancestors().OfType<EventDeclarationSyntax>().FirstOrDefault();
        var containingMemberName = property?.Identifier.Text ?? indexer?.ThisKeyword.Text ?? eventDeclaration?.Identifier.Text ?? "<accessor>";
        var accessorName = accessor.Keyword.Text;
        var typeName = typeDeclaration is null ? "<global>" : CreateFullTypeName(typeDeclaration);
        var complexity = CalculateComplexity(accessor);

        return new CodeComplexityMember(
            file.ProjectName,
            file.FullPath,
            GetLineNumber(syntaxTree, accessor),
            GetNamespaceName(accessor),
            typeName,
            $"{containingMemberName}.{accessorName}",
            "Accessor",
            complexity,
            ClassifySeverity(complexity),
            isLikelyGenerated,
            GetEvidence(accessor));
    }

    private static CodeComplexityMember CreateLocalFunctionResult(
        ScanFile file,
        SyntaxTree syntaxTree,
        LocalFunctionStatementSyntax localFunction,
        bool isLikelyGenerated)
    {
        var typeDeclaration = localFunction.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        var typeName = typeDeclaration is null ? "<global>" : CreateFullTypeName(typeDeclaration);
        var complexity = CalculateComplexity(localFunction);

        return new CodeComplexityMember(
            file.ProjectName,
            file.FullPath,
            GetLineNumber(syntaxTree, localFunction),
            GetNamespaceName(localFunction),
            typeName,
            localFunction.Identifier.Text,
            "Local function",
            complexity,
            ClassifySeverity(complexity),
            isLikelyGenerated,
            GetEvidence(localFunction));
    }

    private static CodeComplexityMember CreateTopLevelStatementsResult(
        ScanFile file,
        SyntaxTree syntaxTree,
        IReadOnlyCollection<GlobalStatementSyntax> globalStatements,
        bool isLikelyGenerated)
    {
        var complexity = 1 + globalStatements.Sum(statement => CountDecisionPoints(statement));
        var firstStatement = globalStatements.First();

        return new CodeComplexityMember(
            file.ProjectName,
            file.FullPath,
            GetLineNumber(syntaxTree, firstStatement),
            "<global>",
            "<global>",
            "<top-level statements>",
            "Top-level statements",
            complexity,
            ClassifySeverity(complexity),
            isLikelyGenerated,
            "top-level statements");
    }

    private static int CalculateComplexity(SyntaxNode node)
    {
        return 1 + CountDecisionPoints(node);
    }

    private static int CountDecisionPoints(SyntaxNode node)
    {
        return node
            .DescendantNodes(descendIntoChildren: child => !IsNestedMemberBoundary(node, child))
            .Count(IsDecisionPoint);
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
            BinaryExpressionSyntax binary =>
                binary.IsKind(SyntaxKind.LogicalAndExpression) ||
                binary.IsKind(SyntaxKind.LogicalOrExpression),
            SwitchSectionSyntax section => section.Labels.Any(label => label is CaseSwitchLabelSyntax or CasePatternSwitchLabelSyntax),
            _ => false
        };
    }

    private static bool IsNestedMemberBoundary(SyntaxNode root, SyntaxNode child)
    {
        if (ReferenceEquals(root, child))
        {
            return false;
        }

        return child is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax;
    }

    private static bool BelongsToOwnMember(SyntaxNode member)
    {
        return member.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault() is null &&
               member.Ancestors().OfType<AccessorDeclarationSyntax>().FirstOrDefault() is null &&
               member.Ancestors().OfType<LocalFunctionStatementSyntax>().FirstOrDefault() is null;
    }

    private static CodeComplexityTypeSummary[] CreateTypeSummaries(IReadOnlyCollection<CodeComplexityMember> members)
    {
        return members
            .GroupBy(member => new { member.ProjectName, member.SourcePath, member.NamespaceName, member.TypeName })
            .Select(group =>
            {
                var groupMembers = group.ToArray();
                var totalComplexity = groupMembers.Sum(member => member.Complexity);
                var maxComplexity = groupMembers.Max(member => member.Complexity);
                return new CodeComplexityTypeSummary(
                    group.Key.ProjectName,
                    group.Key.SourcePath,
                    group.Key.NamespaceName,
                    group.Key.TypeName,
                    groupMembers.Length,
                    totalComplexity,
                    CalculateAverage(totalComplexity, groupMembers.Length),
                    maxComplexity,
                    groupMembers.Max(member => member.Severity),
                    groupMembers.Any(member => member.IsLikelyGeneratedCode));
            })
            .OrderByDescending(summary => summary.TotalComplexity)
            .ThenByDescending(summary => summary.MaximumMemberComplexity)
            .ThenBy(summary => summary.ProjectName, NameComparer)
            .ThenBy(summary => summary.TypeName, NameComparer)
            .ToArray();
    }

    private static CodeComplexityNamespaceSummary[] CreateNamespaceSummaries(IReadOnlyCollection<CodeComplexityTypeSummary> typeSummaries)
    {
        return typeSummaries
            .GroupBy(summary => new { summary.ProjectName, summary.NamespaceName })
            .Select(group =>
            {
                var summaries = group.ToArray();
                var memberCount = summaries.Sum(summary => summary.MemberCount);
                var totalComplexity = summaries.Sum(summary => summary.TotalComplexity);
                return new CodeComplexityNamespaceSummary(
                    group.Key.ProjectName,
                    group.Key.NamespaceName,
                    summaries.Select(summary => summary.TypeName).Distinct(NameComparer).Count(),
                    memberCount,
                    totalComplexity,
                    CalculateAverage(totalComplexity, memberCount),
                    summaries.Length == 0 ? 0 : summaries.Max(summary => summary.MaximumMemberComplexity),
                    summaries.Length == 0 ? CodeComplexitySeverity.Low : summaries.Max(summary => summary.HighestSeverity));
            })
            .OrderByDescending(summary => summary.TotalComplexity)
            .ThenBy(summary => summary.ProjectName, NameComparer)
            .ThenBy(summary => summary.NamespaceName, NameComparer)
            .ToArray();
    }

    private static CodeComplexityProjectSummary[] CreateProjectSummaries(IReadOnlyCollection<CodeComplexityTypeSummary> typeSummaries)
    {
        return typeSummaries
            .GroupBy(summary => summary.ProjectName)
            .Select(group =>
            {
                var summaries = group.ToArray();
                var memberCount = summaries.Sum(summary => summary.MemberCount);
                var totalComplexity = summaries.Sum(summary => summary.TotalComplexity);
                return new CodeComplexityProjectSummary(
                    group.Key,
                    summaries.Select(summary => summary.NamespaceName).Distinct(NameComparer).Count(),
                    summaries.Select(summary => summary.TypeName).Distinct(NameComparer).Count(),
                    memberCount,
                    totalComplexity,
                    CalculateAverage(totalComplexity, memberCount),
                    summaries.Length == 0 ? 0 : summaries.Max(summary => summary.MaximumMemberComplexity),
                    summaries.Length == 0 ? CodeComplexitySeverity.Low : summaries.Max(summary => summary.HighestSeverity),
                    summaries.Where(summary => summary.ContainsLikelyGeneratedCode).Sum(summary => summary.MemberCount));
            })
            .OrderByDescending(summary => summary.TotalComplexity)
            .ThenBy(summary => summary.ProjectName, NameComparer)
            .ToArray();
    }

    private static CodeComplexityScanSummary CreateScanSummary(
        int sourceFileCount,
        IReadOnlyCollection<CodeComplexityMember> members,
        IReadOnlyCollection<CodeComplexityTypeSummary> typeSummaries)
    {
        var totalComplexity = members.Sum(member => member.Complexity);

        return new CodeComplexityScanSummary(
            sourceFileCount,
            members.Count,
            members.Count(member => member.IsLikelyGeneratedCode),
            totalComplexity,
            CalculateAverage(totalComplexity, members.Count),
            members.Count(member => member.Severity == CodeComplexitySeverity.High),
            members.Count(member => member.Severity == CodeComplexitySeverity.VeryHigh),
            typeSummaries.Count(summary => summary.HighestSeverity == CodeComplexitySeverity.High),
            typeSummaries.Count(summary => summary.HighestSeverity == CodeComplexitySeverity.VeryHigh));
    }

    private static double CalculateAverage(int total, int count)
    {
        return count == 0 ? 0 : Math.Round((double)total / count, 2, MidpointRounding.AwayFromZero);
    }

    private static CodeComplexitySeverity ClassifySeverity(int complexity)
    {
        return complexity switch
        {
            >= 21 => CodeComplexitySeverity.VeryHigh,
            >= 11 => CodeComplexitySeverity.High,
            >= 6 => CodeComplexitySeverity.Moderate,
            _ => CodeComplexitySeverity.Low
        };
    }

    private static bool IsLikelyGeneratedFile(ScanFile file)
    {
        var fileName = Path.GetFileName(file.FullPath);

        return file.Content.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetNamespaceName(SyntaxNode node)
    {
        var namespaceName = string.Join(
            ".",
            node.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Reverse()
                .Select(ns => ns.Name.ToString())
                .Where(part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(namespaceName) ? "<global>" : namespaceName;
    }

    private static string CreateFullTypeName(BaseTypeDeclarationSyntax declaration)
    {
        var namespaceName = GetNamespaceName(declaration);
        var containingTypes = declaration
            .Ancestors()
            .OfType<BaseTypeDeclarationSyntax>()
            .Reverse()
            .Select(type => type.Identifier.Text)
            .Where(part => !string.IsNullOrWhiteSpace(part));

        var typeName = string.Join(".", containingTypes.Append(declaration.Identifier.Text));

        return namespaceName == "<global>" ? typeName : $"{namespaceName}.{typeName}";
    }

    private static string GetMemberName(BaseMethodDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax method => method.Identifier.Text,
            ConstructorDeclarationSyntax constructor => constructor.Identifier.Text,
            DestructorDeclarationSyntax destructor => $"~{destructor.Identifier.Text}",
            OperatorDeclarationSyntax operatorDeclaration => $"operator {operatorDeclaration.OperatorToken.Text}",
            ConversionOperatorDeclarationSyntax conversion => $"operator {conversion.Type}",
            _ => member.Kind().ToString()
        };
    }

    private static string GetMemberKind(BaseMethodDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax => "Method",
            ConstructorDeclarationSyntax => "Constructor",
            DestructorDeclarationSyntax => "Destructor",
            OperatorDeclarationSyntax => "Operator",
            ConversionOperatorDeclarationSyntax => "Conversion operator",
            _ => member.Kind().ToString()
        };
    }

    private static int GetLineNumber(SyntaxTree syntaxTree, SyntaxNode node)
    {
        return syntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }

    private static string GetEvidence(SyntaxNode node)
    {
        var text = node.ToString().Trim();
        var firstLine = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).FirstOrDefault() ?? string.Empty;

        return firstLine.Length <= 160 ? firstLine : string.Concat(firstLine.AsSpan(0, 157), "...");
    }
}
