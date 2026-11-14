using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// release.ps1 and tools/sign-release.ps1, read as text, as <see cref="HookTests"/> reads the hooks. The parts of them
/// that talk to git and GitHub ran only during a real release, and 5.8.0 met two faults there (2026-11-14): a filter
/// with a quoted space, which Windows PowerShell 5.1 hands to gh as two arguments, and a check of the hooks setting.
/// The rehearsal (<c>release.ps1 -Rehearse</c>) now asks the same questions through the same functions without
/// releasing; these tests hold the scripts to the shape that makes that safe.
/// What a text test cannot know: that PowerShell really ends the script at the rehearsal's exit (it checks where the
/// block and its exit stand, not how the script runs), what a script called from above the guard does (test.ps1,
/// tools/release-notes.ps1), a git or gh command built in a way these patterns do not see, and whether GitHub answers.
/// Running <c>.\release.ps1 -Rehearse</c> is the check of those.
/// </summary>
public sealed class ReleaseScriptTests
{
    private const string EndOfRehearsal = "# ---- End of the rehearsal.";

    /// <summary>The script's lines of code: without the help block at its top and without comment lines.</summary>
    private static List<string> CodeLines(string script)
    {
        var path = Path.Combine(LayoutTests.RepositoryRoot(), script.Replace('/', Path.DirectorySeparatorChar));
        var lines = new List<string>();
        var inHelp = false;
        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("<#", StringComparison.Ordinal))
            {
                inHelp = true;
            }
            // The marker is a comment, and the tests need to find it.
            if (!inHelp && (!trimmed.StartsWith('#') || trimmed.StartsWith(EndOfRehearsal, StringComparison.Ordinal)))
            {
                lines.Add(line);
            }
            if (trimmed.EndsWith("#>", StringComparison.Ordinal))
            {
                inHelp = false;
            }
        }
        return lines;
    }

    [Theory]
    [InlineData("release.ps1")]
    [InlineData("tools/sign-release.ps1")]
    public void The_script_is_ascii(string script)
    {
        // Windows PowerShell 5.1 reads a script without a byte order mark as ANSI.
        var bytes = File.ReadAllBytes(Path.Combine(LayoutTests.RepositoryRoot(), script.Replace('/', Path.DirectorySeparatorChar)));
        Assert.DoesNotContain(bytes, b => b > 127);
    }

    [Theory]
    [InlineData("release.ps1", 3)]
    [InlineData("tools/sign-release.ps1", 3)]
    public void A_jq_filter_is_one_plain_word(string script, int atLeast)
    {
        var filters = 0;
        foreach (var line in CodeLines(script))
        {
            foreach (Match match in Regex.Matches(line, @"--jq'?\s*,?\s*(.*)$"))
            {
                filters++;
                var rest = match.Groups[1].Value;
                // The filter is a word in single quotes, or a bare word: no space and no double quote in it, and never
                // a double-quoted string, which PowerShell would also expand.
                var filter = Regex.Match(rest, @"^(?:'([^'""\s]+)'|([^'""\s)]+)(?=[\s)]|$))");
                Assert.True(filter.Success, $"{script}: the --jq filter of this line is not one plain word: {line.Trim()}");
            }
        }
        Assert.True(filters >= atLeast, $"{script}: {filters} --jq filters were found, {atLeast} expected; the test no longer sees them");
    }

    [Theory]
    [InlineData("release.ps1")]
    [InlineData("tools/sign-release.ps1")]
    public void The_rehearsal_is_one_block_that_ends_in_an_exit_above_everything_that_changes_something(string script)
    {
        var lines = CodeLines(script);
        Assert.Contains(lines, l => l.Trim() == "[switch]$Rehearse" || l.Trim() == "[switch]$Rehearse,");
        if (script == "release.ps1")
        {
            Assert.Contains(lines, l => l.Trim() == "[switch]$WithTests");
        }

        // The block: "if ($Rehearse) {" at the left edge, its "}" at the left edge right above the marker, no other
        // "}" at the left edge between them, and "exit 0" as the block's last statement after the failing exit.
        var start = lines.IndexOf("if ($Rehearse) {");
        var end = lines.FindIndex(l => l.StartsWith(EndOfRehearsal, StringComparison.Ordinal));
        Assert.True(start >= 0 && end > start, $"{script}: the rehearsal block or its end marker is gone");
        Assert.Equal(start, lines.LastIndexOf("if ($Rehearse) {"));
        Assert.Equal("}", lines[end - 1]);
        Assert.Equal("exit 0", lines[end - 2].Trim());
        Assert.Equal("if ($failed -gt 0) { exit 1 }", lines[end - 3].Trim());
        Assert.DoesNotContain(lines.Skip(start + 1).Take(end - 2 - start), l => l.StartsWith('}'));

        var above = lines.Take(end).ToList();
        var below = lines.Skip(end + 1).ToList();

        // Above the marker (the shared functions and the rehearsal), git is only asked: these verbs, and "tag" and
        // "config" only in their reading forms.
        string[] reading = ["rev-parse", "fetch", "merge-base", "status", "tag --list", "config --get"];
        foreach (var line in above)
        {
            foreach (Match match in Regex.Matches(line, @"git -C \$root (\S+)(?: (\S+))?"))
            {
                var verb = match.Groups[1].Value;
                if (verb is "tag" or "config")
                {
                    verb += " " + match.Groups[2].Value;
                }
                Assert.True(reading.Contains(verb), $"{script}: 'git {verb}' stands above the end of the rehearsal: {line.Trim()}");
            }
        }

        // And gh is only asked: every question goes through Get-GitHubAnswer with words that read, or is "gh run watch".
        string[] questions = ["run list", "run view", "release view", "release list"];
        foreach (var line in above)
        {
            if (line.Contains("Get-GitHubAnswer", StringComparison.Ordinal) && !line.StartsWith("function Get-GitHubAnswer", StringComparison.Ordinal))
            {
                var words = Regex.Match(line, @"Get-GitHubAnswer @\('(\w+)', '(\w+)'");
                Assert.True(words.Success && questions.Contains(words.Groups[1].Value + " " + words.Groups[2].Value),
                    $"{script}: this question to GitHub is not one of the reading ones: {line.Trim()}");
            }
            var direct = Regex.Match(line, @"^\s*gh\s+(\S+)\s+(\S+)");
            if (direct.Success)
            {
                Assert.Equal("run watch", direct.Groups[1].Value + " " + direct.Groups[2].Value);
            }
        }

        // The statements that change something: none above the marker, and each still found below it, so that a
        // pattern that no longer matches anything fails here and not silently.
        string[] changing = script == "release.ps1"
            ? [@"git -C \$root add ", @"git -C \$root commit ", @"git -C \$root tag -a ", @"git -C \$root push .* origin main", @"git -C \$root push .* origin \$tag",
               @"WriteAllText\(", @"tools\\outside-calls\.ps1'\)", @"tools\\sign-release\.ps1'\) \$Version"]
            : [@"gh release download ", @"gh release upload ", @"gh release edit ", @"gh attestation verify ", @"release-key\.ps1'\) sign ", @"New-Item ", @"Remove-Item "];
        foreach (var pattern in changing)
        {
            Assert.DoesNotContain(above, l => Regex.IsMatch(l, pattern));
            Assert.True(below.Any(l => Regex.IsMatch(l, pattern)), $"{script}: nothing below the rehearsal matches \"{pattern}\" any more; bring the test up to date");
        }
        Assert.DoesNotContain(above, l => Regex.IsMatch(l, @"Set-Content|Out-File|Add-Content|Invoke-Expression|\biex\b"));

        // The one start of the signing script above the marker is its rehearsal.
        if (script == "release.ps1")
        {
            var starts = above.Where(l => l.Contains(@"tools\sign-release.ps1')", StringComparison.Ordinal)).ToList();
            Assert.Single(starts);
            Assert.EndsWith(@"tools\sign-release.ps1') -Rehearse", starts[0], StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_real_run_asks_through_the_functions_the_rehearsal_calls()
    {
        // Each question's function is called on both sides of the marker: the command line the rehearsal tries is the
        // one the release runs.
        (string Script, string[] Functions)[] shared =
        [
            ("release.ps1", ["Get-VersionProblem", "Get-BranchProblem", "Get-BehindProblem", "Get-HooksProblem", "Get-UncommittedFiles", "Get-TagProblem",
                "Get-ChangelogProblem", "Get-ProjectWithVersion", "Find-BuildRun", "Get-RunStatus", "Get-RunConclusion"]),
            ("tools/sign-release.ps1", ["Find-ReleaseRun", "Watch-Run", "Get-ReleaseIsDraft", "Get-ReleaseFiles"]),
        ];
        foreach (var (script, functions) in shared)
        {
            var lines = CodeLines(script);
            var start = lines.IndexOf("if ($Rehearse) {");
            var end = lines.FindIndex(l => l.StartsWith(EndOfRehearsal, StringComparison.Ordinal));
            foreach (var function in functions)
            {
                bool Calls(string l) => l.Contains(function, StringComparison.Ordinal) && !l.StartsWith("function ", StringComparison.Ordinal);
                Assert.Contains(lines.Take(start), l => l.StartsWith("function " + function, StringComparison.Ordinal));
                Assert.True(lines.Skip(start).Take(end - start).Any(Calls), $"{script}: the rehearsal no longer calls {function}");
                Assert.True(lines.Skip(end).Any(Calls), $"{script}: the real run no longer calls {function}");
            }
            // And the real run has no question to GitHub of its own beside them.
            Assert.DoesNotContain(lines.Skip(end), l => l.Contains("--jq", StringComparison.Ordinal) || l.Contains("Get-GitHubAnswer", StringComparison.Ordinal));
        }
    }
}
