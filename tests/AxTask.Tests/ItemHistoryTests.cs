using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

public class ItemHistoryTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 18, 30, 5);
    private static readonly string Home = Path.Combine("C:\\", "lists", "Home");
    private static readonly string Work = Path.Combine("C:\\", "lists", "Work");

    private static string Item(string title, bool done, string notes = "") =>
        new JsonObject { ["schema"] = "list.json", ["Title"] = title, ["Done"] = done, ["Notes"] = notes }.ToJsonString();

    [Fact]
    public void A_list_keeps_no_history_until_its_configuration_says_so()
    {
        var config = ListConfig.Parse("""{"format":"axkit-list 2","item fields":[]}""");
        Assert.False(ItemHistory.On(config));
        Assert.True(ItemHistory.On(ItemHistory.With(config, true)));
        Assert.False(ItemHistory.On(ItemHistory.With(ItemHistory.With(config, true), false)));
        Assert.Null(ItemHistory.With(ItemHistory.With(config, true), false).Extra(ItemHistory.Key));
    }

    [Fact]
    public void A_write_gives_a_line_for_each_field_that_changed_and_none_for_a_list_without_history()
    {
        var path = Path.Combine(Home, "Fix the tap.json");
        var parts = new[] { FileChange.Write(path, Item("Fix the tap", false), Item("Fix the tap", true, "Bring the wrench")) };

        var lines = ItemHistory.Lines(parts, Now, _ => true);
        Assert.Equal(2, lines.Count);
        Assert.All(lines, line => Assert.Equal(Home, line.Folder));
        Assert.Equal("""{"time":"2026-11-24T18:30:05","item":"Fix the tap","field":"Done","from":false,"to":true}""", lines[0].Line);
        Assert.Equal("""{"time":"2026-11-24T18:30:05","item":"Fix the tap","field":"Notes","from":"","to":"Bring the wrench"}""", lines[1].Line);

        Assert.Empty(ItemHistory.Lines(parts, Now, _ => false));
    }

    [Fact]
    public void An_item_made_removed_or_moved_between_lists_is_a_line_and_list_json_is_none()
    {
        var path = Path.Combine(Home, "Washer.json");
        var moved = Path.Combine(Work, "Washer.json");
        var parts = new[]
        {
            FileChange.Write(path, null, Item("Washer", false)),
            FileChange.Move(path, moved),
            FileChange.Write(Path.Combine(Home, "list.json"), "{}", """{"order":["Washer"]}"""),
            new FileChange(moved, Item("Washer", false), null),
        };

        var lines = ItemHistory.Lines(parts, Now, _ => true).Select(line => line.Line).ToList();
        Assert.Equal(
        [
            """{"time":"2026-11-24T18:30:05","item":"Washer","happened":"made"}""",
            """{"time":"2026-11-24T18:30:05","item":"Washer","happened":"moved to","where":"Work"}""",
            """{"time":"2026-11-24T18:30:05","item":"Washer","happened":"moved from","where":"Home"}""",
            """{"time":"2026-11-24T18:30:05","item":"Washer","happened":"removed"}""",
        ], lines);
    }

    /// <summary>A move's lines name the item by its Title, as every other line does and as <see cref="ItemHistory.Of"/> finds them: never by the file's name, which differs for a Title with a character a file name cannot hold or one kept under a numbered name.</summary>
    [Fact]
    public void A_moved_item_is_named_by_its_title_not_by_its_file()
    {
        var parts = new[] { FileChange.Move(Path.Combine(Home, "a b c.json"), Path.Combine(Work, "a b c - 2.json")) };

        var lines = ItemHistory.Lines(parts, Now, _ => true, path => path.EndsWith("a b c - 2.json", StringComparison.Ordinal) ? "a/b: c?" : null);
        Assert.All(lines, line => Assert.Contains("\"item\":\"a/b: c?\"", line.Line.Replace("\\u003F", "?", StringComparison.Ordinal)));
        var text = string.Join("\n", lines.Select(line => line.Line));
        Assert.Equal(2, ItemHistory.Of(text, "a/b: c?").Count);

        // A write of the same step knows the Title too, and nothing read from the disk is needed then.
        var written = new[]
        {
            FileChange.Write(Path.Combine(Home, "a b c.json"), Item("a/b: c?", false), Item("a/b: c?", true)),
            FileChange.Move(Path.Combine(Home, "a b c.json"), Path.Combine(Work, "a b c.json")),
        };
        Assert.Equal(3, ItemHistory.Of(string.Join("\n", ItemHistory.Lines(written, Now, _ => true).Select(line => line.Line)), "a/b: c?").Count);

        // With no one to ask, the file's name, as before.
        Assert.Contains("\"item\":\"a b c - 2\"", ItemHistory.Lines(parts, Now, _ => true)[0].Line);
    }

    [Fact]
    public void An_undo_is_recorded_as_the_change_it_makes()
    {
        var path = Path.Combine(Home, "Fix the tap.json");
        var parts = new[] { FileChange.Write(path, Item("Fix the tap", false), Item("Fix the tap", true)) };
        var line = Assert.Single(ItemHistory.Lines(ItemHistory.Undone(parts), Now, _ => true));
        Assert.Contains("\"from\":true,\"to\":false", line.Line);
    }

    [Fact]
    public void An_items_history_is_followed_back_through_its_renames_newest_first()
    {
        var text = string.Join("\n",
            """{"time":"2026-11-20T09:00:00","item":"Tap","happened":"made"}""",
            """{"time":"2026-11-21T09:00:00","item":"Other","field":"Done","from":false,"to":true}""",
            """{"time":"2026-11-22T09:00:00","item":"Tap","field":"Title","from":"Tap","to":"Fix the tap"}""",
            "not a line of the history",
            """{"time":"2026-11-23T09:00:00","item":"Fix the tap","field":"Done","from":false,"to":true}""");

        var said = ItemHistory.Of(text, "Fix the tap").Select(ItemHistory.Said).ToList();
        Assert.Equal(
        [
            "2026-11-23 09:00, Done: from not checked to checked",
            "2026-11-22 09:00, Title: from Tap to Fix the tap",
            "2026-11-20 09:00, made",
        ], said);
    }
}
