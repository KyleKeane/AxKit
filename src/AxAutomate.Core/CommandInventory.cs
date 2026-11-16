using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>One app of the command inventory: its name as <see cref="Dispatch.Apps"/> has it, and its rows as they are stored (no <c>checked</c>, no <c>now</c>).</summary>
public sealed record InventoryApp(string App, IReadOnlyList<ListedCommand> Commands);

/// <summary>
/// The command inventory, <c>docs/axKit/window-commands.json</c> (docs/axKit/user-workflows.md WF-2.6 to WF-2.11,
/// format 5.3): by app, the rows the windows listed through the channel's <c>commands data</c>, without what is state.
/// Pure: the command line reads the file, merges each snapshot of a run into it (<see cref="Merge"/>), or drops what a
/// whole run did not see (<see cref="Rewritten"/>), names what a live window lists differently (<see cref="Stale"/>) and
/// writes it (<see cref="Write"/>); the checks and the reference find a command in it by its name, its chord or a menu
/// step's text. The inventory keeps one order: the apps by name, an app's menu rows by menus and label, then its keys
/// with no menu item by label (<see cref="Sorted"/>), so that what is merged is what is written and read back.
/// </summary>
public static class CommandInventory
{
    public const string Format = "axkit-window-commands 1";

    private static readonly JsonSerializerOptions Text = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// The inventory's apps in the file's order. An unreadable text, or one of another format, gives none; an app whose
    /// entry is not whole (no name, a row without a label) is left out, and a second entry of one app too.
    /// </summary>
    public static IReadOnlyList<InventoryApp> Read(string json)
    {
        var apps = new List<InventoryApp>();
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String || format.GetString() != Format
                || !root.TryGetProperty("apps", out var entries) || entries.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            foreach (var entry in entries.EnumerateArray())
            {
                // An app's entry is a listing of section 5.2 without its notice: the one reader of rows reads it.
                if (entry.ValueKind != JsonValueKind.Object || CommandListing.Read(entry.GetRawText()) is not { } listing
                    || apps.Any(app => Same(app.App, listing.App)))
                {
                    continue;
                }

                apps.Add(new InventoryApp(listing.App, [.. listing.Commands.Select(Stored)]));
            }
        }
        catch (JsonException)
        {
            return [];
        }

        return apps;
    }

    /// <summary>
    /// The file of 5.3: the apps and their rows in the one order (<see cref="Sorted"/>), two spaces of indentation, one
    /// row a line, line feeds, ending with one. An app without rows is not written.
    /// </summary>
    public static string Write(IReadOnlyList<InventoryApp> apps)
    {
        var sorted = Sorted(apps);
        var text = new StringBuilder();
        text.Append("{\n  \"format\": ").Append(Quoted(Format)).Append(",\n");
        if (sorted.Count == 0)
        {
            text.Append("  \"apps\": []\n}\n");
            return text.ToString();
        }

        text.Append("  \"apps\": [\n");
        for (var i = 0; i < sorted.Count; i++)
        {
            var app = sorted[i];
            text.Append("    {\n      \"app\": ").Append(Quoted(app.App)).Append(",\n      \"commands\": [\n");
            for (var j = 0; j < app.Commands.Count; j++)
            {
                text.Append("        ").Append(Line(app.Commands[j])).Append(j < app.Commands.Count - 1 ? ",\n" : "\n");
            }

            text.Append("      ]\n    }").Append(i < sorted.Count - 1 ? ",\n" : "\n");
        }

        text.Append("  ]\n}\n");
        return text.ToString();
    }

    /// <summary>
    /// Whether a row of a snapshot enters the inventory and is checked against it (WF-2.8). Left out: a numbered entry
    /// of a list (recent files, open windows, recently closed ones: <c>AxKitWindows.Label</c>'s form), and a row whose
    /// label, menus or key text holds <paramref name="temp"/> or <paramref name="profile"/>, with either separator,
    /// without regard to case. <paramref name="temp"/> is the run's folder; the folder that holds every script's own
    /// folder of one run of the command line leaves out what names any of them. An empty folder leaves out nothing.
    /// A row whose label is the name of a program, a device, a folder or a shell of the computer the run was made on
    /// (the user's, and stale on the next computer) gets a rule of its own here, named for what it leaves out.
    /// </summary>
    public static bool Kept(ListedCommand row, string temp, string profile) =>
        !IsNumberedEntry(row.Label) && !NamesFolder(row, temp) && !NamesFolder(row, profile);

    /// <summary>A row as the inventory stores it: what is state, <c>checked</c> and <c>now</c>, gone (WF-D-11).</summary>
    public static ListedCommand Stored(ListedCommand row) => row with { Also = [.. row.Also], Checked = null, Now = string.Empty };

    /// <summary>
    /// The inventory with a snapshot merged in (WF-2.9): the snapshot's rows that are kept (<see cref="Kept"/>), stored
    /// without state and grouped by label and menus; each group replaces the app's rows of that label and menus, so a
    /// changed key replaces its row and several commands of one name (WF-1.5) stay side by side; the app's other rows
    /// stay. An app not yet in the inventory is added. Of a snapshot taken while a notice shows, only the rows of keys
    /// alone are merged (WF-2.7). The result is in the one order.
    /// </summary>
    public static IReadOnlyList<InventoryApp> Merge(IReadOnlyList<InventoryApp> apps, WindowCommands snapshot, string temp, string profile)
    {
        var rows = Checked(snapshot, temp, profile);
        if (rows.Count == 0)
        {
            return Sorted(apps);
        }

        var groups = new List<List<ListedCommand>>();
        foreach (var row in rows)
        {
            if (groups.FirstOrDefault(group => SamePlace(group[0], row)) is { } group)
            {
                group.Add(row);
            }
            else
            {
                groups.Add([row]);
            }
        }

        var result = apps.ToList();
        var index = result.FindIndex(app => Same(app.App, snapshot.App));
        var merged = index >= 0 ? result[index].Commands.ToList() : [];
        foreach (var group in groups)
        {
            merged.RemoveAll(row => SamePlace(row, group[0]));
            merged.AddRange(group);
        }

        if (index >= 0)
        {
            result[index] = new InventoryApp(snapshot.App, merged);
        }
        else
        {
            result.Add(new InventoryApp(snapshot.App, merged));
        }

        return Sorted(result);
    }

    /// <summary>
    /// The inventory after a rewrite (WF-2.9): for each app of <paramref name="wholeApps"/>, its rows become the kept rows
    /// of the snapshots <paramref name="seen"/> alone, merged one after another; every other app as <see cref="Merge"/>
    /// leaves it. A command that left a whole app's window leaves the inventory this way.
    /// </summary>
    public static IReadOnlyList<InventoryApp> Rewritten(IReadOnlyList<InventoryApp> apps, IReadOnlyList<WindowCommands> seen, IReadOnlySet<string> wholeApps,
        string temp, string profile)
    {
        IReadOnlyList<InventoryApp> result = Sorted(apps.Where(app => !wholeApps.Any(whole => Same(whole, app.App))));
        foreach (var snapshot in seen)
        {
            result = Merge(result, snapshot, temp, profile);
        }

        return result;
    }

    /// <summary>Whether the inventory has a row of the app; an app without one is not checked (WF-2.10).</summary>
    public static bool Holds(IReadOnlyList<InventoryApp> apps, string app) => Of(apps, app).Count > 0;

    /// <summary>
    /// What a live window lists that the inventory does not hold as it is (WF-2.10), one line a row, each once: none for
    /// an app the inventory holds no row for (<see cref="Holds"/>); else, for each kept row of the snapshot (of one taken
    /// under a notice, its rows of keys alone) that no row of the app equals in every stored field, "&lt;app&gt;:
    /// &lt;the row's line&gt; is not in the inventory" when the app has no row of that label and menus, and else the
    /// first field that differs, named: "&lt;app&gt;: Go to line, Navigation has key ctrl+shift+g; the inventory has ctrl+g".
    /// </summary>
    public static IReadOnlyList<string> Stale(IReadOnlyList<InventoryApp> apps, WindowCommands snapshot, string temp, string profile)
    {
        var held = Of(apps, snapshot.App);
        var lines = new List<string>();
        if (held.Count == 0)
        {
            return lines;
        }

        foreach (var row in Checked(snapshot, temp, profile))
        {
            if (held.Any(known => Equal(known, row)))
            {
                continue;
            }

            var named = CommandNames.Line(new CommandName(row.Label, row.Menu));
            var line = held.FirstOrDefault(known => SamePlace(known, row)) is { } placed
                ? $"{snapshot.App}: {named} {Difference(row, placed)}"
                : $"{snapshot.App}: {named} is not in the inventory";
            if (!lines.Contains(line))
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    /// <summary>An app's rows in the inventory's order; none for an app it does not hold.</summary>
    public static IReadOnlyList<ListedCommand> Of(IReadOnlyList<InventoryApp> apps, string app) =>
        apps.FirstOrDefault(known => Same(known.App, app))?.Commands ?? [];

    /// <summary>
    /// A command's name resolved in the inventory (WF-1.3, WF-2.11), by <see cref="CommandNames.Resolve"/>. With an app,
    /// among that app's rows; with null, app by app in the inventory's order, found when some app finds exactly one
    /// row: never among the union of all apps' rows, where Find would be ambiguous. Among an app's rows a row of keys
    /// alone whose label a menu row also has is passed over: it is that menu row's command, listed while its item was not
    /// offered. The answer's index and candidates count in the rows of <see cref="Of"/> of the app given, or of the app
    /// that found the row; for null and a name no app finds, there are no candidates. <paramref name="row"/> is the row
    /// found, else null.
    /// </summary>
    public static CommandResolution Resolve(IReadOnlyList<InventoryApp> apps, string? app, string name, out ListedCommand? row)
    {
        if (app is not null)
        {
            return ResolveAmong(Of(apps, app), name, out row);
        }

        foreach (var known in apps)
        {
            var resolution = ResolveAmong(known.Commands, name, out row);
            if (resolution.Match == CommandMatch.Found)
            {
                return resolution;
            }
        }

        row = null;
        return CommandNames.Resolve([], name);
    }

    /// <summary>
    /// The one row of the app that holds a chord, as its key or among its further ones (WF-7.3): the chord is put in
    /// <see cref="KeyChord"/>'s order and case first, and a row of keys alone whose label a menu row has is passed over.
    /// Null for an app not known (null), an empty chord, and a chord no row or several rows hold.
    /// </summary>
    public static ListedCommand? OfChord(IReadOnlyList<InventoryApp> apps, string? app, string chord)
    {
        if (app is null || string.IsNullOrWhiteSpace(chord))
        {
            return null;
        }

        var wanted = KeyChord.Normal(chord.Trim());
        var rows = Of(apps, app);
        var holding = rows.Where(row => !PassedOver(rows, row) && (SameChord(row.Key, wanted) || row.Also.Any(also => SameChord(also, wanted)))).ToList();
        return holding.Count == 1 ? holding[0] : null;
    }

    /// <summary>
    /// The one row of the app that a <c>menu</c> step's text answers (WF-7.3): a menu row whose label, or whose label, a
    /// comma, a space and its menus ("Larger text, Options, Text"), starts with the text, without regard to case, as the
    /// step chooses a menu item. A key with no menu item is no menu item. Null for an app not known (null), an empty
    /// text, and a text no row or several rows answer.
    /// </summary>
    public static ListedCommand? OfMenuText(IReadOnlyList<InventoryApp> apps, string? app, string text)
    {
        if (app is null || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var wanted = text.Trim();
        var answering = Of(apps, app).Where(row => !row.KeyOnly
            && (row.Label.StartsWith(wanted, StringComparison.OrdinalIgnoreCase)
                || CommandNames.Line(new CommandName(row.Label, row.Menu)).StartsWith(wanted, StringComparison.OrdinalIgnoreCase))).ToList();
        return answering.Count == 1 ? answering[0] : null;
    }

    /// <summary>
    /// The inventory in its one order (5.3): the apps by name, ordinal without regard to case, an app without rows left
    /// out; an app's menu rows first, by their menus and then their label, ordinal without regard to case, rows equal in
    /// both in the order they came; then its keys with no menu item, by label.
    /// </summary>
    private static List<InventoryApp> Sorted(IEnumerable<InventoryApp> apps) =>
        [.. apps.Where(app => app.Commands.Count > 0)
            .OrderBy(app => app.App, StringComparer.OrdinalIgnoreCase)
            .Select(app => new InventoryApp(app.App, [.. app.Commands
                .OrderBy(row => row.KeyOnly)
                .ThenBy(row => row.Menu, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.Label, StringComparer.OrdinalIgnoreCase)]))];

    /// <summary>The rows of a snapshot that are merged and checked: the kept ones, stored; of a snapshot under a notice, its rows of keys alone (WF-2.7).</summary>
    private static List<ListedCommand> Checked(WindowCommands snapshot, string temp, string profile) =>
        [.. snapshot.Commands.Where(row => Kept(row, temp, profile) && (snapshot.Notice is null || row.KeyOnly)).Select(Stored)];

    /// <summary>
    /// The form <c>AxKitWindows.Label</c> gives an entry of a list, "1. notes.md - axDown" once plain: one character of 1
    /// to 9 or a to z, a full stop, a space, then text (WF-D-12).
    /// </summary>
    private static bool IsNumberedEntry(string label)
    {
        var plain = CommandNames.Plain(label);
        return plain.Length > 3 && (plain[0] is (>= '1' and <= '9') or (>= 'a' and <= 'z')) && plain[1] == '.' && plain[2] == ' ';
    }

    /// <summary>Whether the row's label, menus or key text holds the folder, with either separator, without regard to case.</summary>
    private static bool NamesFolder(ListedCommand row, string folder)
    {
        var wanted = Backslashed(folder).Trim().TrimEnd('\\');
        return wanted.Length > 0 && new[] { row.Label, row.Menu, row.KeyText }
            .Any(text => Backslashed(text).Contains(wanted, StringComparison.OrdinalIgnoreCase));
    }

    private static string Backslashed(string text) => text.Replace('/', '\\');

    /// <summary>A row of keys alone whose label a menu row of the same app also has: the menu row's command (WF-2.11).</summary>
    private static bool PassedOver(IReadOnlyList<ListedCommand> rows, ListedCommand row) =>
        row.KeyOnly && rows.Any(other => !other.KeyOnly && SameText(other.Label, row.Label));

    /// <summary>The resolver over an app's rows with the rows of keys alone that are passed over left out; the answer's indexes count in <paramref name="rows"/>.</summary>
    private static CommandResolution ResolveAmong(IReadOnlyList<ListedCommand> rows, string name, out ListedCommand? row)
    {
        var named = Enumerable.Range(0, rows.Count).Where(i => !PassedOver(rows, rows[i])).ToList();
        var resolution = CommandNames.Resolve([.. named.Select(i => new CommandName(rows[i].Label, rows[i].Menu))], name);
        row = resolution.Match == CommandMatch.Found ? rows[named[resolution.Index]] : null;
        return resolution with
        {
            Index = resolution.Index >= 0 ? named[resolution.Index] : -1,
            Candidates = [.. resolution.Candidates.Select(i => named[i])],
        };
    }

    /// <summary>Whether two rows are of one label and one menus, as the resolver compares them: plain, without regard to case.</summary>
    private static bool SamePlace(ListedCommand one, ListedCommand other) => SameText(one.Label, other.Label) && SameText(one.Menu, other.Menu);

    private static bool SameText(string one, string other) =>
        string.Equals(CommandNames.Plain(one), CommandNames.Plain(other), StringComparison.OrdinalIgnoreCase);

    private static bool Same(string one, string other) => string.Equals(one, other, StringComparison.OrdinalIgnoreCase);

    private static bool SameChord(string key, string wanted) => key.Length > 0 && string.Equals(key, wanted, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether two rows are equal in every stored field (WF-2.10): label, menus, key, also, key text, mark, keyOnly and ai.</summary>
    private static bool Equal(ListedCommand one, ListedCommand other) =>
        one.Label == other.Label && one.Menu == other.Menu && one.Key == other.Key && one.Also.SequenceEqual(other.Also)
        && one.KeyText == other.KeyText && one.Mark == other.Mark && one.KeyOnly == other.KeyOnly && one.Ai == other.Ai;

    /// <summary>The first stored field in which a live row differs from the inventory's row of its label and menus, named: "has key ctrl+shift+g; the inventory has ctrl+g".</summary>
    private static string Difference(ListedCommand live, ListedCommand held)
    {
        static string Has(string field, string listed, string kept) => $"has {field} {listed}; the inventory has {kept}";
        static string Chord(string chord) => chord.Length > 0 ? chord : "none";
        static string Chords(IReadOnlyList<string> chords) => chords.Count > 0 ? string.Join(", ", chords) : "none";
        static string Flag(bool value) => value ? "true" : "false";

        if (live.Label != held.Label)
        {
            return Has("label", Quoted(live.Label), Quoted(held.Label));
        }

        if (live.Menu != held.Menu)
        {
            return Has("menu", Quoted(live.Menu), Quoted(held.Menu));
        }

        if (live.Key != held.Key)
        {
            return Has("key", Chord(live.Key), Chord(held.Key));
        }

        if (!live.Also.SequenceEqual(held.Also))
        {
            return Has("also", Chords(live.Also), Chords(held.Also));
        }

        if (live.KeyText != held.KeyText)
        {
            return Has("keyText", Quoted(live.KeyText), Quoted(held.KeyText));
        }

        if (live.Mark != held.Mark)
        {
            return Has("mark", Quoted(live.Mark), Quoted(held.Mark));
        }

        return live.KeyOnly != held.KeyOnly
            ? Has("keyOnly", Flag(live.KeyOnly), Flag(held.KeyOnly))
            : Has("ai", Flag(live.Ai), Flag(held.Ai));
    }

    /// <summary>A row on its one line of 5.3, without <c>checked</c> and <c>now</c>.</summary>
    private static string Line(ListedCommand row) =>
        "{\"label\": " + Quoted(row.Label) + ", \"menu\": " + Quoted(row.Menu) + ", \"key\": " + Quoted(row.Key)
        + ", \"also\": [" + string.Join(", ", row.Also.Select(Quoted)) + "], \"keyText\": " + Quoted(row.KeyText)
        + ", \"mark\": " + Quoted(row.Mark) + ", \"keyOnly\": " + (row.KeyOnly ? "true" : "false") + ", \"ai\": " + (row.Ai ? "true" : "false") + "}";

    private static string Quoted(string value) => JsonSerializer.Serialize(value, Text);
}
