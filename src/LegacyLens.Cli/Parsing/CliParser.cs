using LegacyLens.Cli.Commands;

namespace LegacyLens.Cli.Parsing;

public sealed class CliParser
{
    private const string MarkdownFormat = "markdown";
    private const string UpgradeReadinessArtifact = "upgrade-readiness";
    private const string UpgradeBlockersArtifact = "upgrade-blockers";
    private const string ExternalDependenciesArtifact = "external-dependencies";
    private const string DataAccessArtifact = "data-access";
    private const string EdmxAnalysisArtifact = "edmx-analysis";
    private const string ClassDependenciesArtifact = "class-dependencies";
    
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
                if (!TryReadOptionValue(args, ref i, arg, out artifacts, out var error))
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

        if (!string.IsNullOrWhiteSpace(artifacts) && !IsSupportedArtifact(artifacts))
        {
            return CliParseResult.Error(
                "Only the upgrade-readiness, upgrade-blockers, external-dependencies, data-access, edmx-analysis, and class-dependencies artifacts are currently supported.");
        }

        if (!string.IsNullOrWhiteSpace(upgradeTarget) &&
            !IsUpgradeArtifact(artifacts))
        {
            return CliParseResult.Error("Use --upgrade-target with --artifacts upgrade-readiness or --artifacts upgrade-blockers.");
        }

        return CliParseResult.Scan(new ScanOptions
        {
            Path = path,
            Output = output,
            OutputDirectory = outputDirectory,
            Format = MarkdownFormat,
            Quiet = quiet,
            Verbose = verbose,
            Artifacts = artifacts,
            UpgradeTarget = upgradeTarget
        });
    }

    private static bool IsSupportedArtifact(string artifact)
    {
        return artifact.Equals(UpgradeReadinessArtifact, StringComparison.OrdinalIgnoreCase) ||
               artifact.Equals(UpgradeBlockersArtifact, StringComparison.OrdinalIgnoreCase) ||
               artifact.Equals(ExternalDependenciesArtifact, StringComparison.OrdinalIgnoreCase) ||
               artifact.Equals(DataAccessArtifact, StringComparison.OrdinalIgnoreCase) ||
               artifact.Equals(EdmxAnalysisArtifact, StringComparison.OrdinalIgnoreCase) ||
               artifact.Equals(ClassDependenciesArtifact, StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool IsUpgradeArtifact(string? artifact)
    {
        return artifact is not null &&
               (artifact.Equals(UpgradeReadinessArtifact, StringComparison.OrdinalIgnoreCase) ||
                artifact.Equals(UpgradeBlockersArtifact, StringComparison.OrdinalIgnoreCase));
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