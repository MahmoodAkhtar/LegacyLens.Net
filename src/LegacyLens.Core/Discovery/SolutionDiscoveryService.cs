using System.Text.RegularExpressions;

namespace LegacyLens.Core.Discovery;

public sealed class SolutionDiscoveryService
{
    private static readonly Regex ProjectLineRegex = new(
        "^Project\\(\"(?<projectTypeGuid>[^\"]+)\"\\)\\s*=\\s*\"(?<name>[^\"]+)\",\\s*\"(?<path>[^\"]+)\",\\s*\"(?<projectGuid>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IReadOnlyList<DiscoveredSolution> DiscoverSolutions(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path cannot be empty.", nameof(rootPath));
        }

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Root path does not exist: {rootPath}");
        }

        var solutionFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);

        var solutions = new List<DiscoveredSolution>();

        foreach (var solutionFile in solutionFiles)
        {
            var solutionDirectory = Path.GetDirectoryName(solutionFile);

            if (string.IsNullOrWhiteSpace(solutionDirectory))
            {
                continue;
            }

            var projectFilePaths = new List<string>();

            foreach (var line in File.ReadLines(solutionFile))
            {
                var match = ProjectLineRegex.Match(line);

                if (!match.Success)
                {
                    continue;
                }

                var projectPath = match.Groups["path"].Value;

                if (!projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fullProjectPath = Path.GetFullPath(
                    Path.Combine(solutionDirectory, projectPath));

                projectFilePaths.Add(fullProjectPath);
            }

            solutions.Add(new DiscoveredSolution
            {
                Name = Path.GetFileNameWithoutExtension(solutionFile),
                SolutionFilePath = solutionFile,
                ProjectFilePaths = projectFilePaths
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList()
            });
        }

        return solutions
            .OrderBy(x => x.Name)
            .ToList();
    }
}