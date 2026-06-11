namespace LegacyLens.Cli.Commands;

public static class ArtifactOutputPathResolver
{
    public static string Resolve(
        string scanPath,
        ScanOptions options,
        string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scanPath);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            return Path.Combine(
                Path.GetFullPath(options.OutputDirectory),
                fileName);
        }

        if (!string.IsNullOrWhiteSpace(options.Output))
        {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.Output));

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                return Path.Combine(outputDirectory, fileName);
            }
        }

        return Path.Combine(scanPath, "output", fileName);
    }
}