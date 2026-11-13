namespace AxKit.Tests;

/// <summary>
/// The repository's git hooks (tools/hooks, installed with <c>git config core.hooksPath tools/hooks</c>) are there and
/// in the shape git needs: read off the sources, as <see cref="ReleaseWorkflowTests"/> reads the workflows. The
/// commit-msg hook is what keeps the priorities list's bookkeeping from depending on memory (the root CLAUDE.md, rule
/// 9): a commit names its item, and the script marks it started or done.
/// </summary>
public sealed class HookTests
{
    [Theory]
    [InlineData("pre-push", "tools/sync-repos.ps1")]
    [InlineData("commit-msg", "tools/item-of-commit.ps1")]
    public void A_hook_is_a_shell_script_with_unix_line_ends_that_runs_its_script(string hook, string script)
    {
        var root = LayoutTests.RepositoryRoot();
        var bytes = File.ReadAllBytes(Path.Combine(root, "tools", "hooks", hook));
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.StartsWith("#!/bin/sh\n", text, StringComparison.Ordinal);
        // sh does not read CRLF; .gitattributes keeps the folder's files with LF.
        Assert.DoesNotContain("\r", text, StringComparison.Ordinal);
        Assert.Contains(script, text, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(root, script.Replace('/', Path.DirectorySeparatorChar))), $"{script} is missing");
        Assert.Contains("tools/hooks/* text eol=lf", File.ReadAllText(Path.Combine(root, ".gitattributes")), StringComparison.Ordinal);
    }

    [Fact]
    public void The_commit_hook_refuses_a_message_without_its_item_and_marks_the_item()
    {
        var root = LayoutTests.RepositoryRoot();
        // The hook passes the script's exit code on: nothing after the script's line, and no "exit 0".
        var hook = File.ReadAllText(Path.Combine(root, "tools", "hooks", "commit-msg"));
        Assert.DoesNotContain("exit 0", hook, StringComparison.Ordinal);
        Assert.EndsWith("</dev/null\n", hook, StringComparison.Ordinal);

        var script = File.ReadAllText(Path.Combine(root, "tools", "item-of-commit.ps1"));
        // The three forms of the line, the two marks, and the pass for a clone without the lists.
        Assert.Contains("Item( done)?:", script, StringComparison.Ordinal);
        Assert.Contains("'Started'", script, StringComparison.Ordinal);
        Assert.Contains("$done.value = $true", script, StringComparison.Ordinal);
        Assert.Contains("needs its reason", script, StringComparison.Ordinal);
        Assert.Contains("if (-not (Test-Path -LiteralPath $Lists))", script, StringComparison.Ordinal);
        // Windows PowerShell 5.1 reads a script without a byte order mark as ANSI: the script stays ASCII.
        Assert.DoesNotContain(script, c => c > 127);

        // The rule that the hook enforces is written where every session reads it.
        Assert.Contains("item-of-commit", File.ReadAllText(Path.Combine(root, "CLAUDE.md")), StringComparison.Ordinal);
    }
}
