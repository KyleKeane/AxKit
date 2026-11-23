using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The guides as a window shows them (Help → User guide): the scripts' marker lines are left out.</summary>
public sealed class EmbeddedTextTests
{
    [Fact]
    public void A_line_that_is_only_an_HTML_comment_is_left_out()
    {
        var text = "## Menus\r\n\r\n<!-- menus: written by a script -->\r\n- **Session**: Exit.\r\n  <!-- end of the menus -->\r\nA <!-- comment --> inside a line stays.\r\n";
        Assert.Equal("## Menus\r\n\r\n- **Session**: Exit.\r\nA <!-- comment --> inside a line stays.\r\n", EmbeddedText.Shown(text));
    }

    /// <summary>
    /// Every guide that holds the Menus markers has its section written (tools/guide-menus.ps1): a guide with the two
    /// markers next to each other would show an empty Menus section.
    /// </summary>
    [Fact]
    public void A_guide_with_the_menus_markers_has_its_menus_written()
    {
        foreach (var guide in Directory.GetFiles(Path.Combine(LayoutTests.RepositoryRoot(), "docs"), "user-guide.md", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(guide);
            var start = Array.FindIndex(lines, line => line.StartsWith("<!-- menus: written by tools/guide-menus.ps1", StringComparison.Ordinal));
            if (start >= 0)
            {
                Assert.True(lines[start + 1].StartsWith("- **", StringComparison.Ordinal), $"{guide}: run tools/guide-menus.ps1");
            }
        }
    }
}
