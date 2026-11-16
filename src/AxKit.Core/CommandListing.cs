using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core.Mcp;

namespace AxKit.Core;

/// <summary>
/// One item of a window's menu bar as the window gives it to <see cref="CommandListing.Rows"/>: its plain label, its
/// menus from the bar down joined by ", ", its key as a chord of <see cref="KeyChord"/> (empty for none, or for a key
/// the item only shows as text), the key as the menu shows it, its mark (read, open, change, act), whether it is
/// checked (null for an item that is no check item) and whether the AI tools may see and run it.
/// </summary>
public sealed record MenuCommand(string Label, string Menu, string Key, string KeyText, string Mark, bool? Checked, bool Ai);

/// <summary>
/// One command of a window's list that has a key, in the order the window registered it: its plain label, its key as a
/// chord (empty for a key <see cref="KeyChord"/> has no name for), the key as a menu would show it, its mark, whether
/// its key acts at this moment, and whether it is a key with no menu item.
/// </summary>
public sealed record KeyCommand(string Label, string Key, string KeyText, string Mark, bool Applies, bool KeyOnly);

/// <summary>
/// One row of a window's commands as data (docs/axKit/user-workflows.md WF-2.3, section 5.2): <paramref name="Key"/>
/// the command's chord, <paramref name="Also"/> its further chords, <paramref name="KeyOnly"/> true for a key with no
/// menu item (its <paramref name="Menu"/> is empty then), and <paramref name="Now"/> the chord that would run the
/// command if pressed at this moment, empty when none would.
/// </summary>
public sealed record ListedCommand(string Label, string Menu, string Key, IReadOnlyList<string> Also, string KeyText,
    string Mark, bool? Checked, bool KeyOnly, bool Ai, string Now);

/// <summary>A window's commands as data: the app's name as <see cref="Dispatch.Apps"/> has it, the title of the notice the window shows (null for none) and the rows.</summary>
public sealed record WindowCommands(string App, string? Notice, IReadOnlyList<ListedCommand> Commands);

/// <summary>
/// A window's commands as data (docs/axKit/user-workflows.md WF-2.3 to WF-2.5): the rows made from the menu bar's
/// entries and the command list's keys, and the one line the automation channel answers <c>commands data</c> with
/// (section 5.2), written and read here so that the window and whoever asks it cannot disagree about its shape. The
/// window side, which reads the menus and asks each command whether its key acts, is
/// <c>AxKit.Forms/AxKitForm.Commands.cs</c>.
/// </summary>
public static class CommandListing
{
    /// <summary>The answer on one line, with "+" and the like as themselves: a chord reads "ctrl+f".</summary>
    private static readonly JsonSerializerOptions Compact = new() { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>A row while it is put together: a <see cref="ListedCommand"/> without its <c>now</c>, whose further chords are added to as the keys with no menu item are read.</summary>
    private sealed record Row(string Label, string Menu, string Key, List<string> Also, string KeyText, string Mark, bool? Checked, bool KeyOnly, bool Ai);

    /// <summary>
    /// The rows of a window (WF-2.3): one per entry of <paramref name="menus"/>, in menu order, then one per label of
    /// the keys with no menu item. <paramref name="keys"/> is every command of the window's list that has a key, in
    /// registration order, a command with a menu item as well as a key alone: all of them are needed to say what a
    /// chord would run.
    ///
    /// Keys with no menu item are folded by label, without regard to case, so that a second key never makes a name
    /// ambiguous (WF-2.4). Where a menu row has the label: a row of that label with the same chord takes the key in
    /// and nothing changes, else the first row of that label gains the chord among its further ones, once. Where no
    /// menu row has it: the first such key makes a row with its chord, and each later one of the label adds its chord
    /// to that row's further ones.
    ///
    /// Last, each row's <c>now</c> (WF-2.5): the first of its chords, its own and then the further ones in order, that
    /// acts at this moment (<see cref="Acts"/>); empty when none does or the row has no chord.
    /// </summary>
    public static IReadOnlyList<ListedCommand> Rows(IReadOnlyList<MenuCommand> menus, IReadOnlyList<KeyCommand> keys)
    {
        var rows = menus.Select(menu => new Row(menu.Label, menu.Menu, menu.Key, [], menu.KeyText, menu.Mark, menu.Checked, KeyOnly: false, menu.Ai)).ToList();
        var menuRows = rows.Count;
        foreach (var key in keys.Where(key => key.KeyOnly))
        {
            var named = rows.Take(menuRows).Where(row => Same(row.Label, key.Label)).ToList();
            if (named.Count > 0)
            {
                // The same command as a menu item: the item's own key, or one more key of the first item of that name.
                if (!named.Any(row => row.Key == key.Key))
                {
                    AddChord(named[0], key.Key);
                }

                continue;
            }

            if (rows.Skip(menuRows).FirstOrDefault(row => Same(row.Label, key.Label)) is { } alone)
            {
                AddChord(alone, key.Key);
            }
            else
            {
                // A key with no menu item is never for the AI tools: they list and run menu items alone.
                rows.Add(new Row(key.Label, string.Empty, key.Key, [], key.KeyText, key.Mark, Checked: null, KeyOnly: true, Ai: false));
            }
        }

        return rows.Select(row => new ListedCommand(row.Label, row.Menu, row.Key, row.Also, row.KeyText, row.Mark, row.Checked, row.KeyOnly, row.Ai,
            Chords(row).FirstOrDefault(chord => Acts(row, chord, keys)) ?? string.Empty)).ToList();
    }

    /// <summary>
    /// Whether an AI tool may see and run a menu item (WF-2.3): never in a window of an app the tools never read
    /// (<see cref="McpTools.NeverRead"/>, axPass and axClip, whose commands they never run either), whatever the item
    /// says; in every other app the item's own answer, <paramref name="forAiTools"/>.
    /// </summary>
    public static bool Ai(bool forAiTools, string appVerb) =>
        forAiTools && !McpTools.NeverRead.Contains(appVerb, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The listing as the one line of section 5.2. The automation channel sends this text as it is: it is the JSON
    /// object already, and serialized once more it would arrive as a JSON string, which <see cref="Read"/> takes for no
    /// listing.
    /// </summary>
    public static string Write(WindowCommands listing)
    {
        var rows = new JsonArray();
        foreach (var row in listing.Commands)
        {
            var also = new JsonArray();
            foreach (var chord in row.Also)
            {
                also.Add(JsonValue.Create(chord));
            }

            rows.Add(new JsonObject
            {
                ["label"] = row.Label,
                ["menu"] = row.Menu,
                ["key"] = row.Key,
                ["also"] = also,
                ["keyText"] = row.KeyText,
                ["mark"] = row.Mark,
                ["checked"] = row.Checked,
                ["keyOnly"] = row.KeyOnly,
                ["ai"] = row.Ai,
                ["now"] = row.Now,
            });
        }

        return new JsonObject { ["app"] = listing.App, ["notice"] = listing.Notice, ["commands"] = rows }.ToJsonString(Compact);
    }

    /// <summary>
    /// The listing of an answer, or null for an answer that is none: the array of palette lines a window from before
    /// this request answers with, an error object, a JSON string, a text that is no JSON. A listing is an object with
    /// the app's name and an array of rows that each have a label; what else a row lacks reads as empty or false.
    /// </summary>
    public static WindowCommands? Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (JsonElements.GetString(root, "app") is not { } app
                || !root.TryGetProperty("commands", out var commands) || commands.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var rows = new List<ListedCommand>();
            foreach (var row in commands.EnumerateArray())
            {
                if (JsonElements.GetString(row, "label") is not { } label)
                {
                    return null;
                }

                rows.Add(new ListedCommand(label, Text(row, "menu"), Text(row, "key"), Texts(row, "also"), Text(row, "keyText"), Text(row, "mark"),
                    Flag(row, "checked"), Flag(row, "keyOnly") == true, Flag(row, "ai") == true, Text(row, "now")));
            }

            return new WindowCommands(app, JsonElements.GetString(root, "notice"), rows);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool Same(string label, string other) => string.Equals(label, other, StringComparison.OrdinalIgnoreCase);

    /// <summary>One more chord of a row, once: not the row's own, not one it has, and never the empty chord of a key without a name.</summary>
    private static void AddChord(Row row, string chord)
    {
        if (chord.Length > 0 && chord != row.Key && !row.Also.Contains(chord))
        {
            row.Also.Add(chord);
        }
    }

    /// <summary>A row's chords in the order they are tried: its own, then the further ones.</summary>
    private static List<string> Chords(Row row)
    {
        var chords = new List<string>();
        if (row.Key.Length > 0)
        {
            chords.Add(row.Key);
        }

        chords.AddRange(row.Also);
        return chords;
    }

    /// <summary>
    /// Whether <paramref name="chord"/> would run the row's command if pressed now (WF-2.5). The commands that have the
    /// chord are taken in registration order, as the window's own key handling takes them, each with whether its key
    /// acts now. A menu row's chord acts when no command has it (the window base or the window handles that key
    /// itself: F1, Ctrl+S) or when one that has it acts. A row of keys alone has the chord only when the first command
    /// that acts on it carries the row's label: where another label's command takes the chord now, this one's does not
    /// run.
    /// </summary>
    private static bool Acts(Row row, string chord, IReadOnlyList<KeyCommand> keys)
    {
        var holders = keys.Where(key => key.Key == chord).ToList();
        if (!row.KeyOnly)
        {
            return holders.Count == 0 || holders.Any(key => key.Applies);
        }

        return holders.FirstOrDefault(key => key.Applies) is { } first && Same(first.Label, row.Label);
    }

    private static string Text(JsonElement row, string name) => JsonElements.GetString(row, name) ?? string.Empty;

    /// <summary>A true or false property of a row; null for null, for none and for anything else.</summary>
    private static bool? Flag(JsonElement row, string name) =>
        row.TryGetProperty(name, out var value) ? value.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => null } : null;

    private static List<string> Texts(JsonElement row, string name)
    {
        var texts = new List<string>();
        if (row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && item.GetString() is { } text)
                {
                    texts.Add(text);
                }
            }
        }

        return texts;
    }
}
