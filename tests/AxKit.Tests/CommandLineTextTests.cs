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

    [Fact]
    public void Other_arguments_pass_as_they_are()
    {
        Assert.Equal("--message", CommandLineText.ForLog(["--message"]));
        Assert.Equal(string.Empty, CommandLineText.ForLog([]));
        Assert.Equal("down notes.md", CommandLineText.ForLog(["down", "notes.md"]));
    }
}