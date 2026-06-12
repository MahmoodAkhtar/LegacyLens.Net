namespace LegacyLens.Core.Files;

internal static class SafeFileSystem
{
    public static IReadOnlyList<string> EnumerateFiles(
        string directory,
        string searchPattern)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        try
        {
            return Directory
                .EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories)
                .Where(path => !IsExcludedPath(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public static IReadOnlyList<string> EnumerateDirectories(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        try
        {
            return Directory
                .EnumerateDirectories(directory, "*", SearchOption.AllDirectories)
                .Where(path => !IsExcludedPath(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public static string ReadAllTextOrEmpty(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return string.Empty;
        }

        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static bool IsExcludedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var parts = path.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Any(IsExcludedPart);
    }

    private static bool IsExcludedPart(string part)
    {
        return part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
               part.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
               part.Equals("output", StringComparison.OrdinalIgnoreCase) ||
               part.Equals("reports", StringComparison.OrdinalIgnoreCase) ||
               part.Equals("artifacts", StringComparison.OrdinalIgnoreCase) ||
               part.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
               part.Equals("Release", StringComparison.OrdinalIgnoreCase) ||
               part.Equals("Log", StringComparison.OrdinalIgnoreCase) ||
               part.Equals("Logs", StringComparison.OrdinalIgnoreCase) ||
               part.Equals("CodeCoverage", StringComparison.OrdinalIgnoreCase) ||
               part.StartsWith("TestResult", StringComparison.OrdinalIgnoreCase);
    }
}