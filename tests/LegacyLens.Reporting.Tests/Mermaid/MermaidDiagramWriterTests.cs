using FluentAssertions;
using LegacyLens.Core.Discovery;
using LegacyLens.Reporting.Mermaid;

namespace LegacyLens.Reporting.Tests.Mermaid;

public sealed class MermaidDiagramWriterTests
{
    [Fact]
    public void BuildProjectDependencyDiagram_ThrowsArgumentNullException_WhenProjectsIsNull()
    {
        var writer = new MermaidDiagramWriter();

        var act = () => writer.BuildProjectDependencyDiagram(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildProjectDependencyDiagram_WhenProjectsHaveReferences_WritesMermaidDependencyDiagram()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var contractsProjectPath = Path.Combine(
                rootPath,
                "SampleLegacyApp.Contracts",
                "SampleLegacyApp.Contracts.csproj");

            var dataProjectPath = Path.Combine(
                rootPath,
                "SampleLegacyApp.Data",
                "SampleLegacyApp.Data.csproj");

            var servicesProjectPath = Path.Combine(
                rootPath,
                "SampleLegacyApp.Services",
                "SampleLegacyApp.Services.csproj");

            var webProjectPath = Path.Combine(
                rootPath,
                "SampleLegacyApp.Web",
                "SampleLegacyApp.Web.csproj");

            var projects = new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = webProjectPath,
                    ProjectReferences =
                    {
                        @"..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj",
                        @"..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj"
                    }
                },
                new()
                {
                    Name = "SampleLegacyApp.Services",
                    ProjectFilePath = servicesProjectPath,
                    ProjectReferences =
                    {
                        @"..\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
                        @"..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj"
                    }
                },
                new()
                {
                    Name = "SampleLegacyApp.Contracts",
                    ProjectFilePath = contractsProjectPath
                },
                new()
                {
                    Name = "SampleLegacyApp.Data",
                    ProjectFilePath = dataProjectPath
                }
            };

            var writer = new MermaidDiagramWriter();

            var diagram = writer.BuildProjectDependencyDiagram(projects);

            diagram.Should().Be(
                "```mermaid" + Environment.NewLine +
                "graph TD" + Environment.NewLine +
                "    SampleLegacyApp_Services --> SampleLegacyApp_Contracts" + Environment.NewLine +
                "    SampleLegacyApp_Services --> SampleLegacyApp_Data" + Environment.NewLine +
                "    SampleLegacyApp_Web --> SampleLegacyApp_Contracts" + Environment.NewLine +
                "    SampleLegacyApp_Web --> SampleLegacyApp_Services" + Environment.NewLine +
                "```" + Environment.NewLine);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void BuildProjectDependencyDiagram_SanitizesProjectNamesForMermaidNodeIds()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var webProjectPath = Path.Combine(
                rootPath,
                "Legacy Web-App",
                "Legacy Web-App.csproj");

            var dataProjectPath = Path.Combine(
                rootPath,
                "Legacy.Data Layer",
                "Legacy.Data Layer.csproj");

            var projects = new List<DiscoveredProject>
            {
                new()
                {
                    Name = "Legacy Web-App",
                    ProjectFilePath = webProjectPath,
                    ProjectReferences =
                    {
                        @"..\Legacy.Data Layer\Legacy.Data Layer.csproj"
                    }
                },
                new()
                {
                    Name = "Legacy.Data Layer",
                    ProjectFilePath = dataProjectPath
                }
            };

            var writer = new MermaidDiagramWriter();

            var diagram = writer.BuildProjectDependencyDiagram(projects);

            diagram.Should().Contain("    Legacy_Web_App --> Legacy_Data_Layer");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void BuildProjectDependencyDiagram_WhenReferencedProjectIsNotInProjectList_UsesReferenceFileName()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var webProjectPath = Path.Combine(
                rootPath,
                "Legacy.Web",
                "Legacy.Web.csproj");

            var projects = new List<DiscoveredProject>
            {
                new()
                {
                    Name = "Legacy.Web",
                    ProjectFilePath = webProjectPath,
                    ProjectReferences =
                    {
                        @"..\Missing.Project\Missing.Project.csproj"
                    }
                }
            };

            var writer = new MermaidDiagramWriter();

            var diagram = writer.BuildProjectDependencyDiagram(projects);

            diagram.Should().Contain("    Legacy_Web --> Missing_Project");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void BuildProjectDependencyDiagram_WhenNoProjectReferencesExist_WritesNoReferencesNode()
    {
        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "Legacy.Web",
                ProjectFilePath = Path.Combine(
                    Path.GetTempPath(),
                    "Legacy.Web",
                    "Legacy.Web.csproj")
            }
        };

        var writer = new MermaidDiagramWriter();

        var diagram = writer.BuildProjectDependencyDiagram(projects);

        diagram.Should().Be(
            "```mermaid" + Environment.NewLine +
            "graph TD" + Environment.NewLine +
            "    NoProjectReferencesFound[No project references found]" + Environment.NewLine +
            "```" + Environment.NewLine);
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "LegacyLensMermaidDiagramWriterTests",
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