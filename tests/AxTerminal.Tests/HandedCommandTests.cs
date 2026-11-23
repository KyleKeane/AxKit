using AxTerminal.Core;

namespace AxTerminal.Tests;

/// <summary>
/// A command handed to axTerminal (SPEC.md AT-1.5a; docs/axTask/forms-framework.md AF-4.5, AF-7.4): its parts as
/// the process's arguments, made into one line for the window's shell, each part still one part.
/// </summary>
public sealed class HandedCommandTests
{
    [Fact]
    public void A_folder_and_then_everything_after_the_option_is_the_command()
    {
        var (folder, command, problem) = HandedCommand.Parse([@"C:\work", "--command", "gh", "issue", "view", "12"]);
        Assert.Null(problem);
        Assert.Equal(@"C:\work", folder);
        Assert.Equal(["gh", "issue", "view", "12"], command);

        // A part that looks like an option, the option's own name included, is a part of the command.
        (folder, command, problem) = HandedCommand.Parse(["--command", "git", "--version", "--command", ""]);
        Assert.Null(problem);
        Assert.Null(folder);
        Assert.Equal(["git", "--version", "--command", ""], command);
    }

    [Fact]
    public void Without_the_option_the_arguments_are_as_they_were()
    {
        Assert.Equal((null, [], null), Shape(HandedCommand.Parse([])));
        Assert.Equal((@"C:\work", [], null), Shape(HandedCommand.Parse([@"C:\work"])));
        Assert.Equal("axTerminal opens one folder.", HandedCommand.Parse([@"C:\a", @"C:\b"]).Problem);
        Assert.Equal("Unknown option --send.", HandedCommand.Parse(["--send"]).Problem);
        Assert.Equal("Unknown option --send.", HandedCommand.Parse([@"C:\work", "--send", "--command", "dir"]).Problem);
        Assert.StartsWith("--command needs the command after it", HandedCommand.Parse([@"C:\work", "--command"]).Problem);

        static (string?, string[], string?) Shape((string? Folder, IReadOnlyList<string> Command, string? Problem) parsed) =>
            (parsed.Folder, parsed.Command.ToArray(), parsed.Problem);
    }

    [Fact]
    public void The_line_is_quoted_by_the_rule_of_the_windows_shell()
    {
        string[] command = ["gh", "issue", "comment", "12", "--body", "It's done; rm -rf $HOME & more"];

        // PowerShell: single quotes, a single quote doubled; nothing in them is expanded or acted on.
        const string powershell = "gh issue comment 12 --body 'It''s done; rm -rf $HOME & more'";
        Assert.Equal((powershell, null), HandedCommand.Line(ShellKind.PowerShell7, command));
        Assert.Equal((powershell, null), HandedCommand.Line(ShellKind.WindowsPowerShell, command));

        // cmd.exe has no quoting that makes every character plain: such a part is refused, and nothing is put anywhere.
        var (line, refused) = HandedCommand.Line(ShellKind.CommandPrompt, command);
        Assert.Null(line);
        Assert.NotNull(refused);
        Assert.Equal(("findstr /c:x \"C:\\lists\\Fix the tap.json\"", null), HandedCommand.Line(ShellKind.CommandPrompt, ["findstr", "/c:x", @"C:\lists\Fix the tap.json"]));
    }

    [Theory]
    [InlineData(ShellKind.GitBash)]
    [InlineData(ShellKind.Custom)]
    public void A_shell_without_a_rule_gets_no_line(ShellKind kind)
    {
        var (line, refused) = HandedCommand.Line(kind, ["dir"]);
        Assert.Null(line);
        Assert.StartsWith("axKit has no quoting rule for ", refused);
    }

    [Fact]
    public void A_line_break_in_a_part_is_refused_for_every_shell()
    {
        // A line break would send what stands before it.
        foreach (var kind in new[] { ShellKind.PowerShell7, ShellKind.WindowsPowerShell, ShellKind.CommandPrompt })
        {
            var (line, refused) = HandedCommand.Line(kind, ["echo", "one\ndel *"]);
            Assert.Null(line);
            Assert.Contains("line break", refused, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void What_is_said_names_who_put_it_there_and_that_it_was_not_sent()
    {
        Assert.Contains("not sent", HandedCommand.Said(null), StringComparison.Ordinal);
        Assert.Contains("axKit checked nothing of it", HandedCommand.Said(null), StringComparison.Ordinal);
        Assert.Equal("A command was handed to this window and is not in the command field: axKit has no quoting rule for Git Bash",
            HandedCommand.Said(HandedCommand.Line(ShellKind.GitBash, ["dir"]).Refused));
    }

    /// <summary>
    /// AT-1.5a, root rule 11: the field's name while the handed line stands in it says all the caution says, and
    /// begins with the field's own word, so that it is still the command field to the ear.
    /// </summary>
    [Fact]
    public void The_fields_name_carries_the_whole_caution()
    {
        Assert.StartsWith("Command, ", HandedCommand.FieldName, StringComparison.Ordinal);
        foreach (var part in new[] { "another program", "not sent", "axKit checked nothing of it", "read it", "press Enter only if you want to run it" })
        {
            Assert.Contains(part, HandedCommand.FieldName, StringComparison.Ordinal);
            Assert.Contains(part, HandedCommand.Said(null), StringComparison.OrdinalIgnoreCase);
        }
    }
}
