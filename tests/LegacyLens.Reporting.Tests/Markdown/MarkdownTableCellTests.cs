using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class MarkdownTableCellTests
{
    [Fact]
    public void Escape_WhenValueContainsPipe_EscapesTableSeparator()
    {
        var formatted = MarkdownTableCell.Escape("alpha|beta");

        Assert.Equal("alpha\\|beta", formatted);
    }

    [Fact]
    public void Evidence_WhenValueIsXmlLike_RendersAsInlineCode()
    {
        var formatted = MarkdownTableCell.Evidence("<object id=\"customerService\" />");

        Assert.Equal("`<object id=\"customerService\" />`", formatted);
    }

    [Fact]
    public void Evidence_WhenValueContainsNewline_NormalizesToSingleLine()
    {
        var formatted = MarkdownTableCell.Evidence("first line\r\nsecond line");

        Assert.Equal("`first line second line`", formatted);
    }

    [Fact]
    public void Evidence_WhenValueContainsBackticks_UsesLongerInlineCodeFence()
    {
        var formatted = MarkdownTableCell.Evidence("builder.Services.Add(`value`)");

        Assert.Equal("`` builder.Services.Add(`value`) ``", formatted);
    }

    [Fact]
    public void Evidence_WhenValueContainsPipe_EscapesPipeInsideInlineCode()
    {
        var formatted = MarkdownTableCell.Evidence("left|right");

        Assert.Equal("`left\\|right`", formatted);
    }
}
