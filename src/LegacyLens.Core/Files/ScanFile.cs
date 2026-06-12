namespace LegacyLens.Core.Files;

public sealed record ScanFile(
    string ProjectName,
    string ProjectFilePath,
    string ProjectDirectory,
    string FullPath,
    string RelativePath,
    string Extension,
    string Content);