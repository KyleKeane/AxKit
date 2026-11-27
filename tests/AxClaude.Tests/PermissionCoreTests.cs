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
    [InlineData(@"cat C:\work\settings.json", "cat settings.json")]
    [InlineData("git status", "git status")]
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
