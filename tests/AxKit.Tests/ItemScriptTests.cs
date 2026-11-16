using System.Diagnostics;
using System.Text.Json;
using AxKit.Core;
using AxKit.TestShared;

namespace AxKit.Tests;

/// <summary>
/// The priorities list's two scripts (the root CLAUDE.md, rule 9), run by Windows PowerShell against a list in a
/// temporary folder: <c>tools/item.ps1</c> writes, notes and finishes an item, and <c>tools/item-of-commit.ps1</c>
/// marks the item a commit message names. Both name an item through <c>tools/items.ps1</c>, so words that name two
/// items are refused by both in the same way.
/// </summary>
public sealed class ItemScriptTests
{
    private const string Title = "axCalc: the history says its length.";

    private static ProcessResult Script(string script, string lists, params string[] arguments)
    {
        var start = new ProcessStartInfo(SystemPrograms.PowerShell);
        foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(LayoutTests.RepositoryRoot(), "tools", script) }.Concat(arguments).Concat(["-Lists", lists]))
        {
            start.ArgumentList.Add(argument);
        }

        var result = ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
        Assert.False(result.TimedOut, $"{script} did not end");
        return result;
    }

    private static ProcessResult NewItem(string lists, string title, string priority = "Low") =>
        Script("item.ps1", lists, "new", "-Title", title, "-App", "axCalc", "-Priority", priority, "-Where", "docs/axCalc/SPEC.md AC-5", "-Notes", "Small.");

    /// <summary>An item's field by its title, from the file as written.</summary>
    private static JsonElement Field(string file, string title)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        return document.RootElement.GetProperty("fields").EnumerateArray().First(field => field.GetProperty("title").GetString() == title).GetProperty("value").Clone();
    }

    [Fact]
    public void A_new_item_has_the_lists_fields_and_axTasks_file_name_and_a_second_of_that_name_is_refused()
    {
        using var temp = new TempFolder("axkit-item-script");
        var result = NewItem(temp.Path, Title);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("new item: " + Title, result.Output, StringComparison.Ordinal);

        // The file is named as axTask names it: the colon a space, no full stop at the end.
        var file = Path.Combine(temp.Path, ItemFiles.FileStem(Title) + ".json");
        Assert.True(File.Exists(file), "the item's file is not named by ItemFiles.FileStem");
        var bytes = File.ReadAllBytes(file);
        Assert.NotEqual(0xEF, bytes[0]);

        using (var document = JsonDocument.Parse(bytes))
        {
            Assert.Equal("axkit-item 1", document.RootElement.GetProperty("format").GetString());
            var titles = string.Join(", ", document.RootElement.GetProperty("fields").EnumerateArray().Select(field => field.GetProperty("title").GetString()));
            Assert.Equal("Title, Done, Started, Priority, App, Tags, Due, Where it is written, Notes", titles);
        }

        Assert.Equal(Title, Field(file, "Title").GetString());
        Assert.False(Field(file, "Done").GetBoolean());
        Assert.Equal("Low", Field(file, "Priority").GetString());
        Assert.Equal("axCalc", Field(file, "App").GetString());
        Assert.Equal("docs/axCalc/SPEC.md AC-5", Field(file, "Where it is written").GetString());
        Assert.Equal("Small.", Field(file, "Notes").GetString());

        // A second item of that name, and a priority the list does not have: refused, the file as it was.
        var again = NewItem(temp.Path, Title, "High");
        Assert.Equal(1, again.ExitCode);
        Assert.Contains("an item of that name exists", again.Output, StringComparison.Ordinal);
        Assert.Equal(bytes, File.ReadAllBytes(file));

        var urgent = NewItem(temp.Path, "axCalc: another thing", "Urgent");
        Assert.Equal(1, urgent.ExitCode);
        Assert.Contains("High, Normal, Low", urgent.Output, StringComparison.Ordinal);
        Assert.Single(Directory.GetFiles(temp.Path));
    }

    [Fact]
    public void A_note_goes_in_front_with_its_date_done_checks_and_words_that_name_two_items_are_refused()
    {
        using var temp = new TempFolder("axkit-item-script");
        Assert.Equal(0, NewItem(temp.Path, Title).ExitCode);
        Assert.Equal(0, NewItem(temp.Path, "axCalc: the history is kept").ExitCode);
        var file = Path.Combine(temp.Path, ItemFiles.FileStem(Title) + ".json");
        var other = Path.Combine(temp.Path, ItemFiles.FileStem("axCalc: the history is kept") + ".json");
        var otherBefore = File.ReadAllBytes(other);

        // "history" is in both titles: neither is written.
        var two = Script("item.ps1", temp.Path, "note", "-Name", "history", "-Text", "Never written");
        Assert.Equal(1, two.ExitCode);
        Assert.Contains("names 2 items", two.Output, StringComparison.Ordinal);
        Assert.Equal("Small.", Field(file, "Notes").GetString());
        Assert.Equal(1, Script("item.ps1", temp.Path, "done", "-Name", "history").ExitCode);
        Assert.Equal(1, Script("item.ps1", temp.Path, "done", "-Name", "no such words").ExitCode);

        var note = Script("item.ps1", temp.Path, "note", "-Name", "SAYS ITS LENGTH", "-Text", "The words are decided");
        Assert.Equal(0, note.ExitCode);
        var today = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal($"{today}: The words are decided. Small.", Field(file, "Notes").GetString());

        Assert.False(Field(file, "Done").GetBoolean());
        var done = Script("item.ps1", temp.Path, "done", "-Name", "says its length");
        Assert.Equal(0, done.ExitCode);
        Assert.Contains("marked done: " + Title, done.Output, StringComparison.Ordinal);
        Assert.True(Field(file, "Done").GetBoolean());
        Assert.Equal(otherBefore, File.ReadAllBytes(other));
    }

    [Fact]
    public void Without_the_list_the_item_script_says_so_and_ends_with_1()
    {
        using var temp = new TempFolder("axkit-item-script");
        var result = Script("item.ps1", Path.Combine(temp.Path, "not-there"), "done", "-Name", "anything");
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("the priorities list is not beside this repository", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_commit_message_marks_the_one_item_its_words_name_and_is_refused_for_two()
    {
        using var temp = new TempFolder("axkit-item-script");
        var lists = Path.Combine(temp.Path, "lists");
        Directory.CreateDirectory(lists);
        Assert.Equal(0, NewItem(lists, Title).ExitCode);
        Assert.Equal(0, NewItem(lists, "axCalc: the history is kept").ExitCode);
        var file = Path.Combine(lists, ItemFiles.FileStem(Title) + ".json");

        var two = Script("item-of-commit.ps1", lists, temp.File("two.txt", "Say the length\n\nItem: history\n"));
        Assert.Equal(1, two.ExitCode);
        Assert.Contains("names 2 items", two.Output, StringComparison.Ordinal);
        Assert.Equal(string.Empty, Field(file, "Started").GetString());

        var started = Script("item-of-commit.ps1", lists, temp.File("one.txt", "Say the length\n\nItem done: says its length\n"));
        Assert.Equal(0, started.ExitCode);
        Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), Field(file, "Started").GetString());
        Assert.True(Field(file, "Done").GetBoolean());

        // The scripts share one way of naming an item, and the rule names the tool where every session reads it.
        var root = LayoutTests.RepositoryRoot();
        Assert.Contains("items.ps1", File.ReadAllText(Path.Combine(root, "tools", "item-of-commit.ps1")), StringComparison.Ordinal);
        Assert.Contains("items.ps1", File.ReadAllText(Path.Combine(root, "tools", "item.ps1")), StringComparison.Ordinal);
        Assert.Contains("tools/item.ps1", File.ReadAllText(Path.Combine(root, "CLAUDE.md")), StringComparison.Ordinal);
    }
}
