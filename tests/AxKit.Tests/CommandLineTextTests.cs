using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The start line's command line (security audit S-54): the message's words never reach the log.</summary>
public sealed class CommandLineTextTests
{
    [Fact]
    public void The_message_is_logged_as_its_length()
    {
        var line = CommandLineText.ForLog([@"C:\work", "--message", "Issue 12: words from a stranger", "--", "--resume"]);
        Assert.Equal(@"C:\work --message <message, 31 characters> -- --resume", line);
        Assert.DoesNotContain("stranger", line);
    }

    /// <summary>A command handed to axTerminal holds an item's values (axTerminal's SPEC AT-1.5a): its parts are counted, never written.</summary>
    [Fact]
    public void A_handed_command_is_logged_as_the_number_of_its_parts()
    {
        var line = CommandLineText.ForLog(["terminal", @"C:\work", "--command", "gh", "issue", "comment", "--body", "words of an item"]);
        Assert.Equal(@"terminal C:\work --command <command, 5 parts>", line);
        Assert.DoesNotContain("words", line);
    }

    [Fact]
    public void Other_arguments_pass_as_they_are()
    {
        Assert.Equal("--message", CommandLineText.ForLog(["--message"]));
        Assert.Equal(string.Empty, CommandLineText.ForLog([]));
        Assert.Equal("down notes.md", CommandLineText.ForLog(["down", "notes.md"]));
    }
}