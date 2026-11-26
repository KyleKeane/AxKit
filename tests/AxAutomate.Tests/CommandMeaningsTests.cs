using AxAutomate.Core;
using AxKit.Core;

namespace AxAutomate.Tests;

/// <summary>
/// One command name means one thing across apps (CommandMeanings, docs/axKit/command-language.md CL-7.3): the rule
/// on small inventories, and the repository's inventory held to its list of exceptions,
/// <c>docs/axKit/command-exceptions.json</c>.
/// </summary>
public sealed class CommandMeaningsTests
{
    private static ListedCommand Row(string label, string menu, string key, string mark, bool keyOnly = false) =>
        new(label, menu, key, [], key, mark, null, keyOnly, true, string.Empty);

    [Fact]
    public void A_name_with_one_mark_and_one_chord_in_every_app_is_one_thing()
    {
        IReadOnlyList<InventoryApp> apps =
        [
            new("axDown", [Row("Find", "Navigation", "ctrl+f", "read")]),
            new("axJSON", [Row("Find", "Navigation", "ctrl+f", "read")]),
            // A command with no chord does not differ from one that has one.
            new("axPreview", [Row("Find", "Navigation", "", "read")]),
        ];
        Assert.Empty(CommandMeanings.Differences(apps));
    }

    [Fact]
    public void A_mark_or_a_chord_that_differs_is_found_with_the_apps_of_each()
    {
        IReadOnlyList<InventoryApp> apps =
        [
            new("axDown", [Row("Undo", "Actions", "ctrl+z", "change"), Row("New window", "Session", "ctrl+shift+n", "open")]),
            new("axFile", [Row("Undo", "Actions", "ctrl+z", "act"), Row("New window", "Session", "ctrl+n", "open")]),
        ];
        var found = CommandMeanings.Differences(apps);
        Assert.Equal(["New window", "Undo"], found.Select(difference => difference.Name));
        Assert.Equal("key", found[0].Differs);
        Assert.Equal(["ctrl+shift+n: axDown", "ctrl+n: axFile"], found[0].Details);
        Assert.Equal("mark", found[1].Differs);
        Assert.Equal(["change: axDown", "act: axFile"], found[1].Details);
    }

    [Fact]
    public void A_name_is_the_canonical_name_in_its_app()
    {
        // Heading in Next and in Previous are two commands of each app, "Heading, Navigation, Next" and "...,
        // Previous": each means one thing across apps though their keys differ from each other.
        IReadOnlyList<InventoryApp> apps =
        [
            new("axDown", [Row("Heading", "Navigation, Next", "ctrl+h", "read"), Row("Heading", "Navigation, Previous", "ctrl+shift+h", "read")]),
            new("axPreview", [Row("Heading", "Navigation, Next", "ctrl+h", "read"), Row("Heading", "Navigation, Previous", "ctrl+shift+h", "read")]),
        ];
        Assert.Empty(CommandMeanings.Differences(apps));
    }

    [Fact]
    public void A_key_alone_of_a_menu_row_s_label_is_that_row_s_command()
    {
        IReadOnlyList<InventoryApp> apps =
        [
            new("axFile", [Row("Ask about this window", "axKit, Claude", "shift+f1", "act")]),
            new("axPass", [Row("Ask about this window", "axKit, Claude", "shift+f1", "act"), Row("Ask about this window", "", "ctrl+slash", "act", keyOnly: true)]),
        ];
        Assert.Empty(CommandMeanings.Differences(apps));
    }

    [Fact]
    public void A_difference_not_listed_or_listed_otherwise_and_an_exception_no_longer_needed_are_problems()
    {
        NameDifference[] found = [new("Undo", "mark", ["change: axDown", "act: axFile"]), new("Paste", "key", ["enter: axClip", "ctrl+v: axFile"])];
        CommandException[] listed = [new("Paste", "mark", "why"), new("Rename", "mark", "why"), new("Undo", "mark", "")];
        var problems = CommandMeanings.Problems(found, listed);
        Assert.Contains(problems, line => line.StartsWith("Paste does not mean one thing across apps, its key differs", StringComparison.Ordinal));
        Assert.Contains("Rename is listed as an exception and now means one thing across apps: take it off docs/axKit/command-exceptions.json", problems);
        Assert.Contains("Undo is listed as an exception without its reason", problems);
        Assert.Equal(3, problems.Count);
    }

    [Fact]
    public void The_exceptions_file_is_read_and_its_format_held()
    {
        var read = CommandMeanings.ReadExceptions("""{"format": "axkit-command-exceptions 1", "exceptions": [{"name": "Undo", "differs": "mark", "reason": "a file operation"}]}""");
        Assert.Equal([new CommandException("Undo", "mark", "a file operation")], read);
        Assert.Throws<FormatException>(() => CommandMeanings.ReadExceptions("""{"format": "other", "exceptions": []}"""));
    }

    [Fact]
    public void Every_name_the_apps_share_means_one_thing_or_is_a_listed_exception()
    {
        // docs/axKit/command-language.md section 8 (c), decided 2026-11-25: today's differences reviewed and listed,
        // so that a new one is made one or added on purpose; the list keeps nothing that has been made one.
        var root = Root();
        var apps = CommandInventory.Read(File.ReadAllText(Path.Combine(root, "docs", "axKit", "window-commands.json")));
        var listed = CommandMeanings.ReadExceptions(File.ReadAllText(Path.Combine(root, "docs", "axKit", "command-exceptions.json")));
        Assert.Empty(CommandMeanings.Problems(CommandMeanings.Differences(apps), listed));
    }

    private static string Root()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("No repository above the tests.");
        }

        return root;
    }
}
