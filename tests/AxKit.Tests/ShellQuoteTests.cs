using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// A command line for a shell made of elements that each stay one element (docs/axTask/forms-framework.md AF-4.5):
/// the rule of each shell, and no line at all where a shell has no rule or its rule cannot hold an element.
/// </summary>
public sealed class ShellQuoteTests
{
    private static string PowerShell(params string[] elements)
    {
        var (line, refused) = ShellQuote.CommandLine(SystemPrograms.PowerShell, elements);
        Assert.Null(refused);
        return line!;
    }

    [Fact]
    public void Plain_elements_stand_as_they_are_in_PowerShell()
    {
        Assert.Equal("gh issue view 15 --repo KyleKeane/AxKit-reports", PowerShell("gh", "issue", "view", "15", "--repo", "KyleKeane/AxKit-reports"));
        Assert.Equal(@"git -C C:\code\axkit status", PowerShell("git", "-C", @"C:\code\axkit", "status"));
    }

    [Theory]
    [InlineData("two words", "'two words'")]
    [InlineData("it's", "'it''s'")]
    [InlineData("$env:USERPROFILE", "'$env:USERPROFILE'")]
    [InlineData("a; Remove-Item x", "'a; Remove-Item x'")]
    [InlineData("a | b & c", "'a | b & c'")]
    [InlineData("$(whoami)", "'$(whoami)'")]
    [InlineData("`n", "'`n'")]
    [InlineData("--%", "'--%'")]
    [InlineData("-a.b", "'-a.b'")]
    [InlineData("-D:value", "'-D:value'")]
    [InlineData("@args", "'@args'")]
    [InlineData("a,b", "'a,b'")]
    [InlineData("caf\u00E9", "'caf\u00E9'")]
    public void An_element_PowerShell_would_read_as_more_goes_in_single_quotes(string element, string quoted) =>
        Assert.Equal("gh " + quoted, PowerShell("gh", element));

    [Fact]
    public void Every_character_PowerShell_reads_as_a_single_quote_is_doubled()
    {
        // The typographic quotes end a single-quoted text as the plain one does: a title pasted from a document
        // could otherwise close the quotes and be read as a command.
        Assert.Equal("gh 'a\u2019\u2019; calc; \u2018\u2018b'", PowerShell("gh", "a\u2019; calc; \u2018b"));
        Assert.Equal("gh '\u201A\u201A \u201B\u201B'", PowerShell("gh", "\u201A \u201B"));
    }

    [Fact]
    public void A_program_that_needs_quotes_is_called_with_the_call_operator()
    {
        Assert.Equal(@"& 'C:\Program Files\Git\cmd\git.exe' status", PowerShell(@"C:\Program Files\Git\cmd\git.exe", "status"));
        Assert.Equal("& '-odd' x", PowerShell("-odd", "x"));
    }

    [Fact]
    public void PowerShell_7_has_the_same_rule()
    {
        Assert.Equal("gh 'two words'", ShellQuote.CommandLine(Pwsh, ["gh", "two words"]).Line);
    }

    private const string Pwsh = @"C:\Program Files\PowerShell\7\pwsh.exe";

    /// <summary>
    /// The review's second pass of 2026-11-27, by a real run: the single quotes are how PowerShell reads the
    /// line, and how it then hands an element to a program is another step, which Windows PowerShell 5.1 takes
    /// without care. With a double quote the element arrived in pieces, with a space and a backslash at its end
    /// it took the next element into itself, and an empty one did not arrive. PowerShell 7.3 hands these on
    /// whole, so no one line is right for both, and they are refused for both, with the reason.
    /// </summary>
    [Theory]
    [InlineData("say \"hello there\" now", "a double quote")]
    [InlineData("a\"b", "a double quote")]
    [InlineData("", "is empty")]
    [InlineData(@"C:\My Folder\", "a backslash at its end")]
    public void An_element_PowerShell_does_not_hand_to_a_program_whole_is_refused(string element, string reason)
    {
        foreach (var shell in new[] { SystemPrograms.PowerShell, Pwsh })
        {
            var (line, refused) = ShellQuote.CommandLine(shell, ["gh", element, "next"]);
            Assert.Null(line);
            Assert.Contains(reason, refused);
        }

        // A backslash at the end of an element without a space is handed on as it is.
        Assert.Equal(@"gh C:\Folder\ next", PowerShell("gh", @"C:\Folder\", "next"));
    }

    /// <summary>Elements with everything a PowerShell line could act on, which the rule lets through: each must reach a program as it is.</summary>
    private static readonly string[] Hard =
    [
        "It's done; rm -rf $HOME & more", @"C:\My Folder\file.txt", @"C:\Folder\", "`n and `$x", "$(whoami)", "semi;colon", "-a.b", "--body",
        "50% !x^", "(paren)", "@splat", "{brace}", "#hash", "'lead", "a|b", "a>b", "caf\u00E9", "a\u2019b", "a,b", "two  spaces", @"back\slash in the middle",
    ];

    /// <summary>
    /// The line the rule makes, run by the shell itself: a script of the test's holds the line, the shell runs
    /// it, and the program the line calls writes each argument it was handed on a line of its own.
    /// </summary>
    private static void Each_hard_element_reaches_the_program_whole(string shell)
    {
        using var temp = new TempFolder("axkit-quote");
        var echo = temp.File("echo.ps1", "[Console]::OutputEncoding = [Text.Encoding]::UTF8\r\nforeach ($a in $args) { [Console]::Out.WriteLine('<' + $a + '>') }\r\n");
        var (line, refused) = ShellQuote.CommandLine(shell, [SystemPrograms.PowerShell, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", echo, .. Hard]);
        Assert.Null(refused);
        // With a byte order mark, which is how Windows PowerShell knows a script is UTF-8.
        var script = Path.Combine(temp.Path, "line.ps1");
        File.WriteAllText(script, line + "\r\n", new System.Text.UTF8Encoding(true));

        var start = new System.Diagnostics.ProcessStartInfo(shell) { StandardOutputEncoding = System.Text.Encoding.UTF8 };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script })
        {
            start.ArgumentList.Add(argument);
        }

        var result = ProcessRun.Run(start, [], TimeSpan.FromSeconds(120));
        Assert.False(result.Stopped, "The shell did not end");
        Assert.Equal(Hard.Select(element => $"<{element}>"), result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(written => written.TrimEnd('\r')));
    }

    [Fact]
    public void Windows_PowerShell_hands_each_element_of_the_line_to_the_program_whole() =>
        Each_hard_element_reaches_the_program_whole(SystemPrograms.PowerShell);

    /// <summary>A test of PowerShell 7 itself, which is said as skipped where PowerShell 7 is not installed, never passed in silence.</summary>
    private sealed class PowerShell7FactAttribute : FactAttribute
    {
        public PowerShell7FactAttribute()
        {
            if (!File.Exists(Pwsh))
            {
                Skip = "PowerShell 7 (pwsh.exe) is not installed in Program Files: the quoting rule was not run against it on this computer.";
            }
        }
    }

    [PowerShell7Fact]
    public void PowerShell_7_hands_each_element_of_the_line_to_the_program_whole() =>
        Each_hard_element_reaches_the_program_whole(Pwsh);

    [Fact]
    public void The_Command_Prompt_quotes_spaces_and_refuses_what_it_would_act_on()
    {
        Assert.Equal("gh issue view 15", ShellQuote.CommandLine(SystemPrograms.Cmd, ["gh", "issue", "view", "15"]).Line);
        Assert.Equal("gh \"two words\" \"a;b\" \"\"", ShellQuote.CommandLine(SystemPrograms.Cmd, ["gh", "two words", "a;b", ""]).Line);
        // A backslash before the closing quote is doubled, so that the program reads the folder and not a quote.
        Assert.Equal("dir \"C:\\My files\\\\\"", ShellQuote.CommandLine(SystemPrograms.Cmd, ["dir", @"C:\My files\"]).Line);

        foreach (var acted in new[] { "50%", "now!", "say \"hi\"", "a^b", "a&b", "a|b", "a<b", "a>b" })
        {
            var (line, refused) = ShellQuote.CommandLine(SystemPrograms.Cmd, ["gh", acted]);
            Assert.Null(line);
            Assert.Contains("which the Command Prompt acts on", refused, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_line_break_a_shell_without_a_rule_and_no_program_give_no_line()
    {
        Assert.Equal("a part of the command holds a line break or another control character", ShellQuote.CommandLine(SystemPrograms.PowerShell, ["gh", "one\ntwo"]).Refused);
        Assert.Equal("a part of the command holds a line break or another control character", ShellQuote.CommandLine(SystemPrograms.Cmd, ["gh", "a\tb"]).Refused);
        Assert.Equal("axKit has no quoting rule for the shell bash", ShellQuote.CommandLine(@"C:\Program Files\Git\bin\bash.exe", ["gh"]).Refused);
        Assert.Equal("there is no program to name", ShellQuote.CommandLine(SystemPrograms.PowerShell, []).Refused);
        Assert.Equal("there is no program to name", ShellQuote.CommandLine(SystemPrograms.PowerShell, [" ", "x"]).Refused);
    }
}
