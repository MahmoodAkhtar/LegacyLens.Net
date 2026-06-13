using LegacyLens.Cli.Commands;

namespace LegacyLens.Cli.Parsing;

public sealed class CliParser
{
    private const string MarkdownFormat = "markdown";

    public CliParseResult Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            return CliParseResult.Error("A command is required. Use 'legacylens scan <path>'.");
        }

        if (IsHelp(args[0]))
        {
            return CliParseResult.Help();
        }

        if (IsVersion(args[0]))
        {
            return CliParseResult.Version();
        }

        if (!args[0].Equals("scan", StringComparison.OrdinalIgnoreCase))
        {
            return CliParseResult.Error($"Unknown command: {args[0]}");
        }

        if (args.Length == 2 && IsHelp(args[1]))
        {
            return CliParseResult.Help();
        }

        return ParseScan(args.Skip(1).ToArray());
    }

    private static CliParseResult ParseScan(string[] args)
    {
        string? path = null;
        string? output = null;
        string? outputDirectory = null;
        var format = MarkdownFormat;
        var quiet = false;
        var verbose = false;
        string? artifacts = null;
        string[] selectedArtifacts = [];
        var shouldWriteAllArtifacts = false;
        string? upgradeTarget = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.Equals("--output", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-o", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadOptionValue(args, ref i, arg, out output, out var error))
                {
                    return CliParseResult.Error(error);
                }

                continue;
            }

            if (arg.Equals("--output-dir", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadOptionValue(args, ref i, arg, out outputDirectory, out var error))
                {
                    return CliParseResult.Error(error);
                }

                continue;
            }

            if (arg.Equals("--format", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadOptionValue(args, ref i, arg, out var parsedFormat, out var error))
                {
                    return CliParseResult.Error(error);
                }

                format = parsedFormat;
                continue;
            }

            if (arg.Equals("--quiet", StringComparison.OrdinalIgnoreCase))
            {
                quiet = true;
                continue;
            }

            if (arg.Equals("--verbose", StringComparison.OrdinalIgnoreCase))
            {
                verbose = true;
                continue;
            }

            if (arg.Equals("--artifacts", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadArtifactOptionValue(args, ref i, arg, path is not null, out artifacts, out var error))
                {
                    return CliParseResult.Error(error);
                }

                if (!TryParseArtifactSelection(
                        artifacts,
                        out selectedArtifacts,
                        out shouldWriteAllArtifacts,
                        out error))
                {
                    return CliParseResult.Error(error);
                }

                continue;
            }

            if (arg.Equals("--upgrade-target", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadOptionValue(args, ref i, arg, out upgradeTarget, out var error))
                {
                    return CliParseResult.Error(error);
                }

                continue;
            }

            if (arg.StartsWith("-", StringComparison.Ordinal))
            {
                return CliParseResult.Error($"Unknown option: {arg}");
            }

            if (path is not null)
            {
                return CliParseResult.Error($"Unexpected argument: {arg}");
            }

            path = arg;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return CliParseResult.Error("The scan path argument is required. Use 'legacylens scan <path>'.");
        }

        if (!format.Equals(MarkdownFormat, StringComparison.OrdinalIgnoreCase))
        {
            return CliParseResult.Error("Only markdown output is currently supported.");
        }

        if (!string.IsNullOrWhiteSpace(output) && !string.IsNullOrWhiteSpace(outputDirectory))
        {
            return CliParseResult.Error("Use either --output or --output-dir, not both.");
        }

        if (quiet && verbose)
        {
            return CliParseResult.Error("Use either --quiet or --verbose, not both.");
        }

        var options = new ScanOptions
        {
            Path = path,
            Output = output,
            OutputDirectory = outputDirectory,
            Format = MarkdownFormat,
            Quiet = quiet,
            Verbose = verbose,
            Artifacts = artifacts,
            SelectedArtifacts = selectedArtifacts,
            ShouldWriteAllArtifacts = shouldWriteAllArtifacts,
            UpgradeTarget = upgradeTarget
        };

        if (!string.IsNullOrWhiteSpace(upgradeTarget) &&
            !options.HasUpgradeRelatedArtifactSelection)
        {
            return CliParseResult.Error(
                "Use --upgrade-target only as upgrade report wording context when --artifacts includes upgrade-readiness, upgrade-blockers, or all.");
        }

        return CliParseResult.Scan(options);
    }

    private static bool TryParseArtifactSelection(
        string artifacts,
        out string[] selectedArtifacts,
        out bool shouldWriteAllArtifacts,
        out string error)
    {
        selectedArtifacts = [];
        shouldWriteAllArtifacts = false;
        error = string.Empty;

        var artifactNames = artifacts
            .Split(',', StringSplitOptions.TrimEntries)
            .ToArray();

        if (artifactNames.Length == 0 || artifactNames.Any(string.IsNullOrWhiteSpace))
        {
            error = $"Artifact selection must include at least one artifact name. Supported values are: {CreateSupportedArtifactList(includeAll: true)}.";
            return false;
        }

        if (artifactNames.Any(artifact => artifact.Equals(ScanOptions.AllArtifactsSelection, StringComparison.OrdinalIgnoreCase)))
        {
            if (artifactNames.Length > 1)
            {
                error = "Use --artifacts all by itself. It cannot be combined with other artifact names.";
                return false;
            }

            shouldWriteAllArtifacts = true;
            return true;
        }

        var unknownArtifacts = artifactNames
            .Where(artifact => !IsSupportedArtifact(artifact))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (unknownArtifacts.Length > 0)
        {
            error = $"Unknown artifact name(s): {string.Join(", ", unknownArtifacts)}. Supported values are: {CreateSupportedArtifactList(includeAll: true)}.";
            return false;
        }

        selectedArtifacts = artifactNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return true;
    }

    private static bool IsSupportedArtifact(string artifact)
    {
        return ScanOptions.SupportedArtifactNames.Contains(
            artifact,
            StringComparer.OrdinalIgnoreCase);
    }

    private static string CreateSupportedArtifactList(bool includeAll)
    {
        var supportedValues = includeAll
            ? ScanOptions.SupportedArtifactNames.Append(ScanOptions.AllArtifactsSelection)
            : ScanOptions.SupportedArtifactNames;

        return string.Join(", ", supportedValues);
    }

    private static bool TryReadArtifactOptionValue(
        IReadOnlyList<string> args,
        ref int index,
        string optionName,
        bool scanPathHasAlreadyBeenRead,
        out string value,
        out string error)
    {
        if (!TryReadOptionValue(args, ref index, optionName, out value, out error))
        {
            return false;
        }

        if (!scanPathHasAlreadyBeenRead)
        {
            return true;
        }

        while (index + 1 < args.Count &&
               !args[index + 1].StartsWith("-", StringComparison.Ordinal))
        {
            index++;
            value = $"{value} {args[index]}";
        }

        return true;
    }

    private static bool TryReadOptionValue(
        IReadOnlyList<string> args,
        ref int index,
        string optionName,
        out string value,
        out string error)
    {
        value = string.Empty;
        error = string.Empty;

        if (index + 1 >= args.Count || args[index + 1].StartsWith("-", StringComparison.Ordinal))
        {
            error = $"Option {optionName} requires a value.";
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private static bool IsHelp(string arg)
    {
        return arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("-h", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVersion(string arg)
    {
        return arg.Equals("--version", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class CliParseResult
{
    private CliParseResult(CliParseResultKind kind, ScanOptions? options = null, string? message = null)
    {
        Kind = kind;
        Options = options;
        Message = message;
    }

    public CliParseResultKind Kind { get; }
    public ScanOptions? Options { get; }
    public string? Message { get; }

    public static CliParseResult Scan(ScanOptions options) => new(CliParseResultKind.Scan, options);
    public static CliParseResult Help() => new(CliParseResultKind.Help);
    public static CliParseResult Version() => new(CliParseResultKind.Version);
    public static CliParseResult Error(string message) => new(CliParseResultKind.Error, message: message);
}

public enum CliParseResultKind
{
    Scan,
    Help,
    Version,
    Error
}
