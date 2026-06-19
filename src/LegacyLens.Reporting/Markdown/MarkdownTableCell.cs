using System.Text.RegularExpressions;

namespace LegacyLens.Reporting.Markdown;

public static class MarkdownTableCell
{
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex BacktickRunPattern = new(@"`+", RegexOptions.Compiled);

    public static string Escape(string? value)
    {
        return EscapeTableSeparators(NormalizeWhitespace(value));
    }

    public static string Code(string? value)
    {
        var normalized = EscapeTableSeparators(NormalizeWhitespace(value));
        var fence = CreateBacktickFence(normalized);
        var content = normalized.Contains('`')
            ? $" {normalized} "
            : normalized;

        return $"{fence}{content}{fence}";
    }

    public static string Evidence(string? value)
    {
        return Code(value);
    }

    private static string NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return WhitespacePattern.Replace(value.Trim(), " ");
    }

    private static string EscapeTableSeparators(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string CreateBacktickFence(string value)
    {
        var longestRun = BacktickRunPattern
            .Matches(value)
            .Select(match => match.Length)
            .DefaultIfEmpty(0)
            .Max();

        return new string('`', longestRun + 1);
    }
}
