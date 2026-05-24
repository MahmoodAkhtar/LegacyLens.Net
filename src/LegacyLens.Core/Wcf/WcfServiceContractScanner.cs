using System.Text.RegularExpressions;

namespace LegacyLens.Core.Wcf;

public sealed class WcfServiceContractScanner
{
    private static readonly Regex ServiceContractInterfaceRegex = new(
        @"\[ServiceContract(?:\([^\)]*\))?\]\s*(?:public\s+|internal\s+)?interface\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex OperationContractRegex = new(
        @"\[OperationContract(?:\([^\)]*\))?\]\s*(?:[A-Za-z0-9_<>,\?\[\]\s]+\s+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\(",
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

        var sourceFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);

        var contracts = new List<WcfServiceContract>();

        foreach (var sourceFile in sourceFiles)
        {
            var content = File.ReadAllText(sourceFile);

            var serviceContractMatches = ServiceContractInterfaceRegex.Matches(content);

            foreach (Match contractMatch in serviceContractMatches)
            {
                var contractName = contractMatch.Groups["name"].Value;

                var operations = OperationContractRegex
                    .Matches(content)
                    .Select(x => x.Groups["name"].Value)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                contracts.Add(new WcfServiceContract
                {
                    Name = contractName,
                    SourceFilePath = sourceFile,
                    Operations = operations
                });
            }
        }

        return contracts;
    }
}