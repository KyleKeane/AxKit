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
    [InlineData("\"quoted\"", "'\"quoted\"'")]
    [InlineData("", "''")]
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
        Assert.Equal("gh 'two words'", ShellQuote.CommandLine(@"C:\Program Files\PowerShell\7\pwsh.exe", ["gh", "two words"]).Line);
    }

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
