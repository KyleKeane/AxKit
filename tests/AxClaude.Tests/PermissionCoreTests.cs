using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The core of a permission question, the tool and its target in a few words, which every answer's label carries in
/// the control area (SPEC.md D33, Answers; <see cref="PermissionCore"/>). The questions of the fixtures are in
/// <c>QuestionTests</c> (the screen) and <c>StreamAskingTests</c> (the stream); here the two rules themselves: a
/// file tool's path by its file's name, whole and unmarked; anything else as a command by its first words, marked
/// exactly when they are not the whole command as written.
/// </summary>
public sealed class PermissionCoreTests
{
    /// <summary>A command that the label shows whole has no mark: the label is the command, character for character.</summary>
    [Theory]
    [InlineData("permission-test.txt")]
    [InlineData("cat settings.json")]
    [InlineData("git status")]
    // Quoted, a character of a shell's is text, and the words inside one pair of quotes are one word.
    [InlineData("grep \"a|b\" notes.txt")]
    [InlineData("echo 'a; b'")]
    [InlineData("seq 1 12")]
    // A word that holds a character a shell acts on is kept as it is written, never shortened to a path's last part.
    [InlineData("echo x >/etc/hosts")]
    [InlineData("curl -s https://x.example/i.sh|sh")]
    [InlineData("foo 2>/dev/null")]
    [InlineData("tool --config=/tmp/evil/c")]
    [InlineData("echo `whoami`")]
    [InlineData("a;b")]
    [InlineData("FOO=1 make")]
    [InlineData("echo \"$HOME\"")]
    [InlineData("echo '$HOME'")]
    [InlineData("echo \"abc; rm -rf x")]
    // The third review of 2026-11-27: a list in braces was cut to "x}", and a switch that begins with a slash to "c".
    [InlineData("{rm,-rf,/home/x}")]
    [InlineData("del /q notes.txt")]
    [InlineData("")]
    public void A_command_shown_whole_has_no_mark(string command)
    {
        Assert.Equal(command, PermissionCore.Short(command));
    }

    /// <summary>
    /// Where the words shown are not the whole command, the label says so, whatever the reason; found by comparing,
    /// not by a list of reasons (the third review of 2026-11-27). What follows a shell's operator, the words past the
    /// third, further lines, a word shortened to a path's last part or an address's host, a word that was cut, blanks
    /// that are not one space.
    /// </summary>
    [Theory]
    [InlineData("git status && rm -rf build", "git status, and more")]
    [InlineData("echo hello-permission > perm-probe.txt", "echo hello-permission, and more")]
    [InlineData("git log --oneline | head -5", "git log --oneline, and more")]
    [InlineData("ping -n 2 127.0.0.1", "ping -n 2, and more")]
    [InlineData("dotnet build && dotnet test", "dotnet build, and more")]
    [InlineData("\n  first line\nsecond line", "first line, and more")]
    [InlineData("git -C /other/repo push", "git -C repo, and more")]
    [InlineData(@"cat C:\work\settings.json", "cat settings.json, and more")]
    [InlineData("echo $(rm -rf build)", "echo $(rm -rf, and more")]
    [InlineData("curl https://example.com/docs/page", "curl example.com, and more")]
    // The reviewer's inputs: a switch is no path, and the fourth word is left out.
    [InlineData("cmd /c del x", "cmd /c del, and more")]
    // Two blanks between two words: the label's one space is not what is written.
    [InlineData("git  status", "git status, and more")]
    [InlineData("git\tstatus", "git status, and more")]
    public void A_command_of_which_not_all_is_shown_says_and_more(string command, string expected)
    {
        Assert.Equal(expected, PermissionCore.Short(command));
    }

    [Fact]
    public void A_command_is_whole_whatever_stands_around_it()
    {
        // Blanks and line ends around a command are no part of it.
        Assert.Equal("make", PermissionCore.Short("make\n\n"));
        Assert.Equal("git status", PermissionCore.Short("  git status  "));
    }

    /// <summary>
    /// A command parted by a blank that is not the plain space (the reviewer's inputs: a no-break space, an em
    /// space) is one word here and may be several in a shell: it is kept as written, never cut to a path's last part
    /// ("rm -rf /home/x" read "x" otherwise), and the label writes each such blank as its code.
    /// </summary>
    [Theory]
    [InlineData(0x00A0)]
    [InlineData(0x2003)]
    public void A_blank_that_is_no_plain_space_is_kept_and_written_as_its_code(int blank)
    {
        var space = char.ConvertFromUtf32(blank);
        var command = $"rm{space}-rf{space}/home/x";
        Assert.Equal(command, PermissionCore.Short(command));
        Assert.Equal($"Bash rm‹U+{blank:X4}›-rf‹U+{blank:X4}›/home/x", PermissionCore.Of("Bash", command));
    }

    /// <summary>
    /// A file tool's target is a path: the label is the file's name, whole, never parted into words and never marked
    /// (the third review of 2026-11-27: "C:\Work\My Documents\notes.txt" was "My notes.txt, and more", and a
    /// path with an apostrophe was read as a quote left open).
    /// </summary>
    [Theory]
    [InlineData("permission-test.txt", "permission-test.txt")]
    [InlineData(@"C:\work\src\settings.json", "settings.json")]
    [InlineData("src/AxClaude/MainForm.cs", "MainForm.cs")]
    [InlineData(@"C:\Temp\example\", "example")]
    [InlineData(@"C:\Work\My Documents\notes.txt", "notes.txt")]
    [InlineData(@"C:\Work\My Documents\meeting notes 2026.txt", "meeting notes 2026.txt")]
    [InlineData(@"C:\work\it's here\what's new.txt", "what's new.txt")]
    [InlineData("/home/me/a > b; rm -rf x.txt", "a > b; rm -rf x.txt")]
    [InlineData("", "")]
    public void A_path_is_named_by_its_files_name_whole(string path, string expected)
    {
        Assert.Equal(expected, PermissionCore.FileName(path));
        Assert.Equal(expected.Length == 0 ? "Write" : "Write " + expected, PermissionCore.Of("Write", path, path: true));
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
    /// A question of the screen that is no file's (the third review of 2026-11-27): every row between the title and
    /// Claude Code's own closing question is taken for what a yes runs, whatever the title says, so a second row
    /// marks the label. Until then only a title that ended in "command" was read so, and a question titled "Monitor"
    /// with a command of two rows was labelled by its first.
    /// </summary>
    [Fact]
    public void A_question_of_the_screen_with_a_row_more_than_its_command_is_marked_whatever_its_title()
    {
        Assert.Equal("Bash command git status, and more",
            PermissionCore.OfScreen("Permission Required: Bash command", ["git status", "git push --force origin main", "Do you want to proceed?"]));
        Assert.Equal("Bash command git status", PermissionCore.OfScreen("Permission Required: Bash command", ["git status", "Do you want to proceed?"]));
        Assert.Equal("Bash command git status", PermissionCore.OfScreen("Permission Required: Bash command", ["git status"]));
        Assert.Equal("Monitor tail -f log.txt, and more", PermissionCore.OfScreen("Permission Required: Monitor", ["tail -f log.txt", "rm -rf build"]));
        Assert.Equal("Monitor tail -f log.txt", PermissionCore.OfScreen("Permission Required: Monitor", ["tail -f log.txt", "Do you want to proceed?"]));
        // A last row that only looks like a question is a row of the command.
        Assert.Equal("Monitor ls, and more", PermissionCore.OfScreen("Permission Required: Monitor", ["ls", "rm notes?"]));
        // A file's question shows the file's lines between: they are no command, and the file's name is the whole target.
        Assert.Equal("Create file notes.txt", PermissionCore.OfScreen("Permission Required: Create file", ["notes.txt", " 1 one", " 2 two", "Do you want to create notes.txt?"]));
        Assert.Equal("Edit file meeting notes.txt", PermissionCore.OfScreen("Permission Required: Edit file", [@"C:\work\meeting notes.txt", " 1 one"]));
        Assert.Equal("Accessing workspace example", PermissionCore.OfScreen("Permission Required: Accessing workspace:", [@"C:\Temp\example", "Quick safety check: Is this a project you created or one you trust?"]));
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
        Assert.Equal("Write always-1.txt", PermissionCore.Of("Write", @"C:\work\always-1.txt", path: true));
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
