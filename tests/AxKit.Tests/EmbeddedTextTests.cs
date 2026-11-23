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

    /// <summary>
    /// The guides whose Menus section is still written by hand, to be converted: the list only shrinks. axFile's guide
    /// is changed with axFile's own work, which runs beside this conversion (2026-11-23).
    /// </summary>
    private static readonly string[] MenusByHand = ["axFile"];

    /// <summary>
    /// Every app's guide has its Menus section written from the command inventory (docs/axKit/documents.md; the
    /// maintainer, 2026-11-23: no documentation of keys kept by hand): the two marker lines of tools/guide-menus.ps1,
    /// so that a guide of a new app, or one rewritten, cannot list its menus by hand again unseen.
    /// </summary>
    [Fact]
    public void Every_apps_guide_has_its_menus_written_from_the_inventory()
    {
        var docs = Path.Combine(LayoutTests.RepositoryRoot(), "docs");
        var byHand = new List<string>();
        foreach (var app in Dispatch.Apps)
        {
            var guide = Path.Combine(docs, app.Name, "user-guide.md");
            if (File.Exists(guide) && !File.ReadAllText(guide).Contains("<!-- menus: written by tools/guide-menus.ps1", StringComparison.Ordinal))
            {
                byHand.Add(app.Name);
            }
        }

        Assert.Equal(MenusByHand, byHand.Order(StringComparer.Ordinal).ToArray());
    }
}
