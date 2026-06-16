
using LegacyLens.Cli.Commands;
using LegacyLens.Cli.Parsing;
using LegacyLens.Cli.Progress;
using LegacyLens.Cli.Writers;

var parser = new CliParser();
var consoleWriter = new ScanConsoleWriter();

try
{
    var parseResult = parser.Parse(args);

    switch (parseResult.Kind)
    {
        case CliParseResultKind.Help:
            consoleWriter.WriteHelp();
            return 0;

        case CliParseResultKind.Version:
            consoleWriter.WriteVersion();
            return 0;

        case CliParseResultKind.Error:
            consoleWriter.WriteError(parseResult.Message ?? "Invalid command.");
            consoleWriter.WriteError("Use 'legacylens --help' for usage.");
            return 2;

        case CliParseResultKind.Scan:
            var options = parseResult.Options!;
            using (var progressReporter = ScanProgressReporterFactory.Create(options))
            {
                var command = new ScanCommand(progressReporter);
                var result = command.Execute(options);
                consoleWriter.Write(result, options);
            }

            return 0;

        default:
            consoleWriter.WriteError("Invalid command.");
            return 2;
    }
}
catch (DirectoryNotFoundException exception)
{
    consoleWriter.WriteError(exception.Message);
    return 2;
}
catch (ArgumentException exception)
{
    consoleWriter.WriteError(exception.Message);
    return 2;
}
catch (Exception exception)
{
    consoleWriter.WriteError("Unexpected error while running LegacyLens.NET.");
    consoleWriter.WriteError(exception.Message);
    return 1;
}



