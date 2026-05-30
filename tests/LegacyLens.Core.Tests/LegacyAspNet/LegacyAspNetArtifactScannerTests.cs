using FluentAssertions;
using LegacyLens.Core.LegacyAspNet;

namespace LegacyLens.Core.Tests.LegacyAspNet;

public sealed class LegacyAspNetArtifactScannerTests
{
    [Fact]
    public void Scan_ReturnsWebFormsPage_WhenAspxFileExists()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "Default.aspx");
            File.WriteAllText(filePath, "<%@ Page Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle();
            artifacts[0].Kind.Should().Be(LegacyAspNetArtifactKind.WebFormsPage);
            artifacts[0].FilePath.Should().Be(filePath);
            artifacts[0].Name.Should().Be("Default.aspx");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsWebFormsUserControl_WhenAscxFileExists()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "CustomerSummary.ascx");
            File.WriteAllText(filePath, "<%@ Control Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle();
            artifacts[0].Kind.Should().Be(LegacyAspNetArtifactKind.WebFormsUserControl);
            artifacts[0].FilePath.Should().Be(filePath);
            artifacts[0].Name.Should().Be("CustomerSummary.ascx");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsMasterPage_WhenMasterFileExists()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "Site.master");
            File.WriteAllText(filePath, "<%@ Master Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle();
            artifacts[0].Kind.Should().Be(LegacyAspNetArtifactKind.MasterPage);
            artifacts[0].FilePath.Should().Be(filePath);
            artifacts[0].Name.Should().Be("Site.master");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsAsmxWebService_WhenAsmxFileExists()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "CustomerService.asmx");
            File.WriteAllText(filePath, "<%@ WebService Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle();
            artifacts[0].Kind.Should().Be(LegacyAspNetArtifactKind.AsmxWebService);
            artifacts[0].FilePath.Should().Be(filePath);
            artifacts[0].Name.Should().Be("CustomerService.asmx");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsHttpHandler_WhenAshxFileExists()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "Download.ashx");
            File.WriteAllText(filePath, "<%@ WebHandler Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle();
            artifacts[0].Kind.Should().Be(LegacyAspNetArtifactKind.HttpHandler);
            artifacts[0].FilePath.Should().Be(filePath);
            artifacts[0].Name.Should().Be("Download.ashx");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsGlobalAsax_WhenGlobalAsaxFileExists()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "Global.asax");
            File.WriteAllText(filePath, "<%@ Application Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle();
            artifacts[0].Kind.Should().Be(LegacyAspNetArtifactKind.GlobalAsax);
            artifacts[0].FilePath.Should().Be(filePath);
            artifacts[0].Name.Should().Be("Global.asax");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsMultipleArtifactTypes_WhenLegacyAspNetFilesExist()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var nestedPath = Path.Combine(rootPath, "Views");
            Directory.CreateDirectory(nestedPath);

            File.WriteAllText(Path.Combine(rootPath, "Default.aspx"), "");
            File.WriteAllText(Path.Combine(rootPath, "CustomerSummary.ascx"), "");
            File.WriteAllText(Path.Combine(rootPath, "Site.master"), "");
            File.WriteAllText(Path.Combine(rootPath, "CustomerService.asmx"), "");
            File.WriteAllText(Path.Combine(rootPath, "Download.ashx"), "");
            File.WriteAllText(Path.Combine(nestedPath, "Global.asax"), "");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().HaveCount(6);

            artifacts.Select(x => x.Kind).Should().BeEquivalentTo(new[]
            {
                LegacyAspNetArtifactKind.WebFormsPage,
                LegacyAspNetArtifactKind.WebFormsUserControl,
                LegacyAspNetArtifactKind.MasterPage,
                LegacyAspNetArtifactKind.AsmxWebService,
                LegacyAspNetArtifactKind.HttpHandler,
                LegacyAspNetArtifactKind.GlobalAsax
            });
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_IgnoresUnrelatedFiles()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(Path.Combine(rootPath, "Program.cs"), "public static class Program { }");
            File.WriteAllText(Path.Combine(rootPath, "Web.config"), "<configuration />");
            File.WriteAllText(Path.Combine(rootPath, "README.md"), "# Readme");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().BeEmpty();
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsArtifactsFromNestedDirectories()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var nestedPath = Path.Combine(rootPath, "Areas", "Admin");
            Directory.CreateDirectory(nestedPath);

            var filePath = Path.Combine(nestedPath, "Dashboard.aspx");
            File.WriteAllText(filePath, "<%@ Page Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle();
            artifacts[0].Kind.Should().Be(LegacyAspNetArtifactKind.WebFormsPage);
            artifacts[0].FilePath.Should().Be(filePath);
            artifacts[0].Name.Should().Be("Dashboard.aspx");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_DetectsFilesCaseInsensitively()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(Path.Combine(rootPath, "DEFAULT.ASPX"), "");
            File.WriteAllText(Path.Combine(rootPath, "CUSTOMER.ASCX"), "");
            File.WriteAllText(Path.Combine(rootPath, "SITE.MASTER"), "");
            File.WriteAllText(Path.Combine(rootPath, "SERVICE.ASMX"), "");
            File.WriteAllText(Path.Combine(rootPath, "HANDLER.ASHX"), "");
            File.WriteAllText(Path.Combine(rootPath, "GLOBAL.ASAX"), "");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Select(x => x.Kind).Should().BeEquivalentTo(new[]
            {
                LegacyAspNetArtifactKind.WebFormsPage,
                LegacyAspNetArtifactKind.WebFormsUserControl,
                LegacyAspNetArtifactKind.MasterPage,
                LegacyAspNetArtifactKind.AsmxWebService,
                LegacyAspNetArtifactKind.HttpHandler,
                LegacyAspNetArtifactKind.GlobalAsax
            });
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsArtifactsOrderedByKindThenFilePath()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var secondPagePath = Path.Combine(rootPath, "B.aspx");
            var firstPagePath = Path.Combine(rootPath, "A.aspx");
            var handlerPath = Path.Combine(rootPath, "Download.ashx");

            File.WriteAllText(secondPagePath, "");
            File.WriteAllText(handlerPath, "");
            File.WriteAllText(firstPagePath, "");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().HaveCount(3);

            artifacts.Select(x => x.Kind).Should().Equal(
                LegacyAspNetArtifactKind.WebFormsPage,
                LegacyAspNetArtifactKind.WebFormsPage,
                LegacyAspNetArtifactKind.HttpHandler);

            artifacts[0].FilePath.Should().Be(firstPagePath);
            artifacts[1].FilePath.Should().Be(secondPagePath);
            artifacts[2].FilePath.Should().Be(handlerPath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ThrowsArgumentException_WhenRootPathIsEmpty()
    {
        var scanner = new LegacyAspNetArtifactScanner();

        var act = () => scanner.Scan("");

        act.Should().Throw<ArgumentException>()
            .WithMessage("Root path cannot be empty.*");
    }

    [Fact]
    public void Scan_ThrowsDirectoryNotFoundException_WhenRootPathDoesNotExist()
    {
        var scanner = new LegacyAspNetArtifactScanner();
        var rootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var act = () => scanner.Scan(rootPath);

        act.Should().Throw<DirectoryNotFoundException>()
            .WithMessage($"Root path does not exist: {rootPath}");
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "LegacyLensTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(path);

        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}