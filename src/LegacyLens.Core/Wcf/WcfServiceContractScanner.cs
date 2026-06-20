using System.Text.RegularExpressions;
using LegacyLens.Core.Files;

namespace LegacyLens.Core.Wcf;

public sealed class WcfServiceContractScanner
{
    private static readonly Regex InterfaceRegex = new(
        @"(?s)(?<attributes>(?:\s*\[[^\]]+\]\s*)+)\s*(?:public\s+|internal\s+|private\s+|protected\s+|new\s+|partial\s+)*interface\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\s*:\s*[^{]+)?\s*\{",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex OperationMemberRegex = new(
        @"(?s)(?<attributes>(?:\s*\[[^\]]+\]\s*)+)\s*(?:public\s+|internal\s+|private\s+|protected\s+|new\s+|static\s+|virtual\s+|abstract\s+|partial\s+|async\s+)*[A-Za-z_][A-Za-z0-9_<>,\.\?\[\]\s]*\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:<[^>]+>\s*)?\(",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public IReadOnlyList<WcfServiceContract> Scan(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path cannot be empty.", nameof(rootPath));
        }

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Root path does not exist: {rootPath}");
        }

        var sourceFiles = Directory
            .GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new SourceFileRecord(
                path,
                ProjectName: null,
                File.ReadAllText(path)))
            .ToArray();

        return ScanSourceFiles(sourceFiles);
    }

    public IReadOnlyList<WcfServiceContract> Scan(ScanFileInventory fileInventory)
    {
        ArgumentNullException.ThrowIfNull(fileInventory);

        return Scan(fileInventory.CSharpFiles);
    }

    public IReadOnlyList<WcfServiceContract> Scan(IReadOnlyCollection<ScanFile> csharpFiles)
    {
        ArgumentNullException.ThrowIfNull(csharpFiles);

        var sourceFiles = csharpFiles
            .OrderBy(file => file.FullPath, StringComparer.OrdinalIgnoreCase)
            .Select(file => new SourceFileRecord(
                file.FullPath,
                file.ProjectName,
                file.Content ?? string.Empty))
            .ToArray();

        return ScanSourceFiles(sourceFiles);
    }

    private static IReadOnlyList<WcfServiceContract> ScanSourceFiles(IEnumerable<SourceFileRecord> sourceFiles)
    {
        var contracts = new List<WcfServiceContract>();

        foreach (var sourceFile in sourceFiles)
        {
            if (!ShouldInspectSource(sourceFile.Content))
            {
                continue;
            }

            var interfaceMatches = InterfaceRegex.Matches(sourceFile.Content);

            foreach (Match interfaceMatch in interfaceMatches)
            {
                var attributes = interfaceMatch.Groups["attributes"].Value;

                if (!ContainsAttribute(attributes, "ServiceContract"))
                {
                    continue;
                }

                var contractName = interfaceMatch.Groups["name"].Value;
                var openingBraceIndex = sourceFile.Content.IndexOf('{', interfaceMatch.Index + interfaceMatch.Length - 1);

                if (openingBraceIndex < 0)
                {
                    continue;
                }

                var closingBraceIndex = FindMatchingClosingBrace(sourceFile.Content, openingBraceIndex);

                if (closingBraceIndex < 0)
                {
                    continue;
                }

                var interfaceBody = sourceFile.Content.Substring(
                    openingBraceIndex + 1,
                    closingBraceIndex - openingBraceIndex - 1);

                var operations = FindOperations(interfaceBody);

                contracts.Add(new WcfServiceContract
                {
                    Name = contractName,
                    SourceFilePath = sourceFile.FullPath,
                    Operations = operations
                });
            }
        }

        return contracts
            .OrderBy(contract => contract.SourceFilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(contract => contract.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ShouldInspectSource(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        return content.Contains("ServiceContract", StringComparison.Ordinal) ||
               content.Contains("ServiceContractAttribute", StringComparison.Ordinal);
    }

    private static List<string> FindOperations(string interfaceBody)
    {
        return OperationMemberRegex
            .Matches(interfaceBody)
            .Where(x => ContainsAttribute(x.Groups["attributes"].Value, "OperationContract"))
            .Select(x => x.Groups["name"].Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ContainsAttribute(string attributes, string attributeName)
    {
        var pattern =
            $@"\[\s*(?:[A-Za-z_][A-Za-z0-9_]*\.)*{Regex.Escape(attributeName)}(?:Attribute)?(?:\s*\(|\s*,|\s*\])";

        return Regex.IsMatch(
            attributes,
            pattern,
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
    }

    private static int FindMatchingClosingBrace(string content, int openingBraceIndex)
    {
        var depth = 0;
        var inString = false;
        var inChar = false;
        var inSingleLineComment = false;
        var inMultiLineComment = false;
        var isEscaped = false;

        for (var i = openingBraceIndex; i < content.Length; i++)
        {
            var current = content[i];
            var next = i + 1 < content.Length ? content[i + 1] : '\0';

            if (inSingleLineComment)
            {
                if (current is '\r' or '\n')
                {
                    inSingleLineComment = false;
                }

                continue;
            }

            if (inMultiLineComment)
            {
                if (current == '*' && next == '/')
                {
                    inMultiLineComment = false;
                    i++;
                }

                continue;
            }

            if (inString)
            {
                if (current == '"' && !isEscaped)
                {
                    inString = false;
                }

                isEscaped = current == '\\' && !isEscaped;
                continue;
            }

            if (inChar)
            {
                if (current == '\'' && !isEscaped)
                {
                    inChar = false;
                }

                isEscaped = current == '\\' && !isEscaped;
                continue;
            }

            if (current == '/' && next == '/')
            {
                inSingleLineComment = true;
                i++;
                continue;
            }

            if (current == '/' && next == '*')
            {
                inMultiLineComment = true;
                i++;
                continue;
            }

            if (current == '"')
            {
                inString = true;
                isEscaped = false;
                continue;
            }

            if (current == '\'')
            {
                inChar = true;
                isEscaped = false;
                continue;
            }

            if (current == '{')
            {
                depth++;
                continue;
            }

            if (current == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private sealed record SourceFileRecord(
        string FullPath,
        string? ProjectName,
        string Content);
}
