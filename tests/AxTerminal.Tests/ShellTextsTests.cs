using AxTerminal.Core;

namespace AxTerminal.Tests;

/// <summary>
/// What a shell is told and what is read from it stays in <see cref="ShellLauncher"/> and <see cref="ShellTexts"/>
/// (root CLAUDE.md rule 12; docs/axKit/access-layers.md): the window writes no key sequence, no mark and no option of
/// a shell's in a string, and the marks' words stand in those two files alone. Read off the sources with
/// <see cref="SourceTexts"/>. The keys and the facts about the shells are pinned too, so that a move changes nothing
/// that is sent.
/// </summary>
public sealed class ShellTextsTests
{
    /// <summary>What a window file must not hold inside a string, as it is written in the source.</summary>
    private static readonly string[] NeverInTheWindow =
    [
        @"\x", @"\u00", @"\e", "133;", "axkit=", "-NoExit", "-NoLogo", "-EncodedCommand", "--login", "PROMPT_COMMAND", "PSReadLine", ">> ", "More? ",
    ];

    /// <summary>The characteristic words, each with the files of src/AxTerminal.Core that may hold it.</summary>
    private static readonly (string Text, string[] Files)[] OnePlace =
    [
        ("133;", ["ShellLauncher.cs"]),
        ("axkit=", ["ShellLauncher.cs", "ShellTexts.cs"]),
        ("-EncodedCommand", ["ShellLauncher.cs"]),
        ("--login", ["ShellLauncher.cs"]),
        ("PROMPT_COMMAND", ["ShellLauncher.cs"]),
        ("More? ", ["ShellLauncher.cs"]),
        (@"\u001b", ["ShellTexts.cs"]),
    ];

    [Fact]
    public void The_window_writes_no_key_sequence_and_no_mark_of_a_shells()
    {
        var offenders = SourceTexts.LiteralsUnder("src/AxTerminal")
            // Enter alone is a key sent; inside a longer text ("\r\n") it is a line break of the edit control.
            .Where(literal => literal.Text == @"\r" || NeverInTheWindow.Any(text => literal.Text.Contains(text, StringComparison.Ordinal)))
            .Select(literal => literal.ToString())
            .ToList();
        Assert.True(offenders.Count == 0, "A key or a word of a shell's in a window file; it belongs in ShellTexts or ShellLauncher (rule 12): " + string.Join("; ", offenders));
    }

    [Fact]
    public void Each_characteristic_word_is_written_in_its_own_file_of_the_core()
    {
        var literals = SourceTexts.LiteralsUnder("src/AxTerminal.Core");
        foreach (var (text, files) in OnePlace)
        {
            var holders = literals.Where(literal => literal.Text.Contains(text, StringComparison.Ordinal)).ToList();
            Assert.True(holders.Count > 0, $"\"{text}\" is watched and written nowhere: the list is stale.");
            var strays = holders.Where(literal => !files.Contains(Path.GetFileName(literal.File))).Select(literal => literal.ToString()).ToList();
            Assert.True(strays.Count == 0, $"\"{text}\" belongs in {string.Join(" or ", files)} alone (rule 12): " + string.Join("; ", strays));
        }
    }

    /// <summary>The model asks for the field the prompt texts print: every shell's marks carry it in front of the secret's place.</summary>
    [Fact]
    public void Every_prompt_text_prints_the_secrets_field_the_model_reads()
    {
        var field = ShellTexts.NonceField + ShellLauncher.NonceToken;
        Assert.Contains("133;A;" + field, ShellLauncher.PromptSnippet);
        Assert.Contains("133;B;" + field, ShellLauncher.PromptSnippet);
        Assert.Contains("133;D;$code;" + field, ShellLauncher.PromptSnippet);
        Assert.Contains("133;A;" + field, ShellLauncher.BashPromptCommand);
        Assert.Contains("133;B;" + field, ShellLauncher.BashPromptCommand);
        Assert.Contains("133;D;%s;" + field, ShellLauncher.BashPromptCommand);
        var cmd = ShellLauncher.CommandPromptWithMarks(null);
        Assert.Contains("133;A;" + field, cmd);
        Assert.Contains("133;B;" + field, cmd);
        Assert.Contains("133;D;" + field, cmd);
        Assert.Equal("ABD", new string([ShellTexts.PromptStartMark, ShellTexts.PromptEndMark, ShellTexts.CommandDoneMark]));
    }

    [Fact]
    public void The_keys_are_what_a_console_sends()
    {
        Assert.Equal("\r", ShellTexts.Enter);
        Assert.Equal("\x03", ShellTexts.Interrupt);
        Assert.Equal("\x04", ShellTexts.EndOfInput);
        Assert.Equal("\x1b", ShellTexts.Escape);
        Assert.Equal("\x1b[A", ShellTexts.Up);
        Assert.Equal("\x1b[B", ShellTexts.Down);
    }

    [Theory]
    [InlineData(ShellKind.PowerShell7, true)]
    [InlineData(ShellKind.WindowsPowerShell, true)]
    [InlineData(ShellKind.CommandPrompt, false)]
    [InlineData(ShellKind.GitBash, false)]
    [InlineData(ShellKind.Custom, false)]
    public void Only_PowerShell_needs_an_empty_line_after_a_block(ShellKind kind, bool expected)
    {
        Assert.Equal(expected, ShellTexts.EndsBlockOnEmptyLine(kind));
    }

    [Theory]
    [InlineData("Password:", true)]
    [InlineData("Continue?", true)]
    [InlineData("Overwrite [y/N]", true)]
    [InlineData("Are you sure (yes/no)", true)]
    [InlineData("Copying files", false)]
    [InlineData("100%", false)]
    [InlineData("", false)]
    public void A_row_that_ends_like_a_question_is_known(string row, bool expected)
    {
        Assert.Equal(expected, ShellTexts.EndsLikeQuestion(row));
    }

    [Fact]
    public void An_error_of_PowerShells_own_has_the_code_its_prompt_text_gives()
    {
        Assert.Equal(-1, ShellTexts.ErrorWithoutCode);
        Assert.Contains("$code = -1", ShellLauncher.PromptSnippet);
    }
}
