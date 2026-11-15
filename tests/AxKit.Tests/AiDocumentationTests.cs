using AxKit.Core;
using AxKit.Core.Ai;

namespace AxKit.Tests;

/// <summary>
/// axKit's guides in Claude's working folder (docs/axKit/ai-help.md AH-7): which files there are, that every app's
/// guide is among them, and when they are written: where missing, again by another version, never when current, and
/// never over or instead of a file of the user's own.
/// </summary>
public sealed class AiDocumentationTests
{
    private static string Normal(string text) => text.ReplaceLineEndings("\n").TrimEnd();

    [Fact]
    public void Every_app_has_its_guide_among_the_files_as_the_repository_has_it()
    {
        var root = LayoutTests.RepositoryRoot();
        var files = AiDocumentation.Files("1.2.3").ToDictionary(file => file.Path, file => file.Text);
        foreach (var app in Dispatch.Apps)
        {
            // A new app whose guide AxKit.Core.csproj does not embed (a line Guide.<name>.md) fails here.
            Assert.True(files.TryGetValue(Path.Combine("documentation", app.Name + ".md"), out var guide),
                $"{app.Name}: its guide is not embedded in AxKit.Core. Add the line for docs/{app.Name}/user-guide.md with LogicalName=\"Guide.{app.Name}.md\" to src/AxKit.Core/AxKit.Core.csproj.");
            Assert.Equal(Normal(File.ReadAllText(Path.Combine(root, "docs", app.Name, "user-guide.md"))), Normal(guide!));
        }

        Assert.Equal(Normal(File.ReadAllText(Path.Combine(root, "docs", "axKit", "README.md"))), Normal(files[Path.Combine("documentation", "axKit.md")]));
        // The index, the bundle's guide, one guide per app, the two rules: nothing else.
        Assert.Equal(Dispatch.Apps.Count + 4, files.Count);
    }

    [Fact]
    public void The_index_names_every_file_with_a_line_saying_what_it_is_and_the_version()
    {
        var files = AiDocumentation.Files("1.2.3");
        Assert.Equal(Path.Combine("documentation", "index.md"), files[0].Path);
        var index = files[0].Text;
        Assert.Contains("written here by axKit 1.2.3", index, StringComparison.Ordinal);
        Assert.Contains("- axKit.md: axKit as a whole", index, StringComparison.Ordinal);
        foreach (var app in Dispatch.Apps)
        {
            Assert.Contains($"\n- {app.Name}.md: {app.Description}.\n", index, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_rule_tells_claude_to_read_the_index_and_the_guide_first_and_to_say_when_they_do_not_cover_it()
    {
        Assert.Equal(Path.Combine(".claude", "rules", "axkit-documentation.md"), AiDocumentation.RulePath);
        Assert.Contains("read documentation/index.md and the guide that fits with your Read tool before you", AiDocumentation.Rule, StringComparison.Ordinal);
        Assert.Contains("When the guides do not cover the question, say so", AiDocumentation.Rule, StringComparison.Ordinal);
        // No frontmatter: a rule with a paths field is loaded only for matching files, this one at every start.
        Assert.StartsWith("# axKit's guides", AiDocumentation.Rule, StringComparison.Ordinal);
    }

    [Fact]
    public void The_files_are_written_where_missing_and_left_alone_while_the_version_is_current()
    {
        using var temp = new TempFolder("axkit-ai-documentation");
        var written = AiDocumentation.Write(temp.Path, "1.2.3");
        Assert.Equal(AiDocumentation.Files("1.2.3").Select(file => file.Path), written);
        var folder = Path.Combine(temp.Path, "documentation");
        Assert.Equal("1.2.3", File.ReadLines(Path.Combine(folder, "axkit-version.txt")).First());
        Assert.StartsWith("# axFile", File.ReadAllText(Path.Combine(folder, "axFile.md")), StringComparison.Ordinal);
        Assert.DoesNotContain("\n", File.ReadAllText(Path.Combine(folder, "axFile.md")).Replace("\r\n", string.Empty), StringComparison.Ordinal);
        Assert.StartsWith("# axKit's guides\r\n", File.ReadAllText(Path.Combine(temp.Path, ".claude", "rules", "axkit-documentation.md")), StringComparison.Ordinal);

        // Current: nothing is written, a changed file included (the version is all that is looked at).
        File.WriteAllText(Path.Combine(folder, "axFile.md"), "changed");
        Assert.Empty(AiDocumentation.Write(temp.Path, "1.2.3"));
        Assert.Equal("changed", File.ReadAllText(Path.Combine(folder, "axFile.md")));

        // A file that went missing is written again, alone.
        File.Delete(Path.Combine(folder, "axDown.md"));
        File.Delete(Path.Combine(temp.Path, ".claude", "rules", "axkit-documentation.md"));
        Assert.Equal([Path.Combine("documentation", "axDown.md"), AiDocumentation.RulePath], AiDocumentation.Write(temp.Path, "1.2.3"));
        Assert.Equal("changed", File.ReadAllText(Path.Combine(folder, "axFile.md")));
    }

    [Fact]
    public void Another_version_writes_every_file_again_and_keeps_the_users_own_files()
    {
        using var temp = new TempFolder("axkit-ai-documentation");
        var folder = Path.Combine(temp.Path, "documentation");
        AiDocumentation.Write(temp.Path, "1.2.3");
        File.WriteAllText(Path.Combine(folder, "axFile.md"), "an old guide");
        var own = temp.File(Path.Combine("documentation", "my notes.md"), "my own");
        var claude = temp.File("CLAUDE.md", "my instructions");
        var rule = temp.File(Path.Combine(".claude", "rules", "mine.md"), "my rule");

        var written = AiDocumentation.Write(temp.Path, "1.3.0");
        Assert.Equal(Dispatch.Apps.Count + 4, written.Count);
        Assert.StartsWith("# axFile", File.ReadAllText(Path.Combine(folder, "axFile.md")), StringComparison.Ordinal);
        Assert.Contains("axKit 1.3.0", File.ReadAllText(Path.Combine(folder, "index.md")), StringComparison.Ordinal);
        Assert.Equal("1.3.0", File.ReadLines(Path.Combine(folder, "axkit-version.txt")).First());
        Assert.Equal("my own", File.ReadAllText(own));
        Assert.Equal("my instructions", File.ReadAllText(claude));
        Assert.Equal("my rule", File.ReadAllText(rule));
        Assert.Empty(AiDocumentation.Write(temp.Path, "1.3.0"));

        // A development build asks for every file again whatever the marker says: its guides change without its version.
        File.WriteAllText(Path.Combine(folder, "axFile.md"), "an old guide");
        Assert.Equal(Dispatch.Apps.Count + 4, AiDocumentation.Write(temp.Path, "1.3.0", rewrite: true).Count);
        Assert.StartsWith("# axFile", File.ReadAllText(Path.Combine(folder, "axFile.md")), StringComparison.Ordinal);
        Assert.Equal("my own", File.ReadAllText(own));
    }

    [Fact]
    public void A_file_an_earlier_version_wrote_and_this_one_no_longer_has_is_deleted_by_the_markers_list_alone()
    {
        using var temp = new TempFolder("axkit-ai-documentation");
        var folder = Path.Combine(temp.Path, "documentation");
        Assert.True(AiDocumentation.IsMissing(temp.Path));
        AiDocumentation.Write(temp.Path, "1.2.3");
        Assert.False(AiDocumentation.IsMissing(temp.Path));
        var marker = Path.Combine(folder, "axkit-version.txt");
        // The marker names every file written, after the version.
        Assert.Equal(["1.2.3", .. AiDocumentation.Files("1.2.3").Select(file => file.Path)], File.ReadAllLines(marker));

        // An earlier axKit wrote axOld.md for an app that has since left; the user added a file of their own; and a
        // marker that names files outside the two places axKit writes to is not believed.
        var gone = temp.File(Path.Combine("documentation", "axOld.md"), "an app that left");
        var own = temp.File(Path.Combine("documentation", "mine.md"), "my own");
        var outside = temp.File("CLAUDE.md", "my instructions");
        var deeper = temp.File(Path.Combine("documentation", "sub", "note.md"), "mine too");
        File.AppendAllLines(marker, [Path.Combine("documentation", "axOld.md"), "CLAUDE.md", Path.Combine("documentation", "..", "CLAUDE.md"), Path.Combine("documentation", "sub", "note.md")]);

        // Current: nothing is deleted. Another version: the file the marker named and this version lacks goes, alone.
        AiDocumentation.Write(temp.Path, "1.2.3");
        Assert.True(File.Exists(gone));
        AiDocumentation.Write(temp.Path, "1.3.0");
        Assert.False(File.Exists(gone));
        Assert.Equal("my own", File.ReadAllText(own));
        Assert.Equal("my instructions", File.ReadAllText(outside));
        Assert.Equal("mine too", File.ReadAllText(deeper));
        Assert.DoesNotContain("axOld", File.ReadAllText(marker), StringComparison.Ordinal);

        Assert.True(AiDocumentation.IsOurs(Path.Combine("documentation", "axFile.md")));
        Assert.True(AiDocumentation.IsOurs(AiDocumentation.RulePath));
        Assert.True(AiDocumentation.IsOurs(AiToolsRule.RulePath));
        Assert.False(AiDocumentation.IsOurs("CLAUDE.md"));
        Assert.False(AiDocumentation.IsOurs(Path.Combine("documentation", "..", "CLAUDE.md")));
        Assert.False(AiDocumentation.IsOurs(Path.Combine("documentation", "notes.txt")));
        Assert.False(AiDocumentation.IsOurs(Path.Combine(".claude", "agents", "agent.md")));
    }
}
