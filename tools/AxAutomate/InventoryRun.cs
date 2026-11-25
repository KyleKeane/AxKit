using System.Text;
using AxAutomate.Core;
using AxKit.Core;

namespace AxAutomate;

/// <summary>
/// The command inventory in a run of the command line (docs/axKit/user-workflows.md WF-2.7 to WF-2.10):
/// <c>docs/axKit/window-commands.json</c> read as the run begins (<see cref="Start"/>), each script's snapshots taken in
/// after it (<see cref="After"/>), and at the run's end (<see cref="End"/>) the lines said after the summary and, under
/// <c>--inventory write</c> or <c>rewrite</c>, the file written. The rows are kept, merged and compared by
/// <see cref="CommandInventory"/>; this class only holds the run's part. Under <c>--inventory off</c>, and without a
/// repository, it does nothing.
/// </summary>
internal static class InventoryRun
{
    /// <summary>The file's place in the repository, as the run's lines name it.</summary>
    public const string File = "docs/axKit/window-commands.json";

    /// <summary>WF-2.10's words, the first of the lines of a run that found the inventory stale.</summary>
    public const string Stale = "The command inventory is stale (docs/axKit/window-commands.json): a window lists a command that the file does not hold as it is. "
        + "Every script's own result stands. If you changed a menu, a key or a label, run the same scripts with --inventory write, then tools\\workflows.ps1, "
        + "and commit both with your change; if you did not, say so and change neither (docs/axKit/user-workflows.md WF-11.4).";

    private static string _mode = RunOptions.Off;
    private static string _path = string.Empty;
    private static string _temp = string.Empty;
    private static string _profile = string.Empty;
    private static IReadOnlyList<InventoryApp> _apps = [];

    /// <summary>Every snapshot of the run, in the order the scripts gave them.</summary>
    private static readonly List<WindowCommands> Seen = [];

    /// <summary>The lines of a check, each once, in the order they came.</summary>
    private static readonly List<string> StaleLines = [];

    /// <summary>The apps a check found no row for, each once.</summary>
    private static readonly List<string> Unheld = [];

    /// <summary>The apps a script that failed gave a snapshot of: never whole for a rewrite (WF-2.9).</summary>
    private static readonly HashSet<string> FailedApps = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The apps of which a snapshot was taken with no notice showing.</summary>
    private static readonly HashSet<string> ListedWhole = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The inventory read for the run. <paramref name="temp"/> is the run's folder, which holds every script's own
    /// <c>{temp}</c>, and with the profile folder decides what never enters the inventory
    /// (<see cref="CommandInventory.Kept"/>). Null when the run can go on; else why it cannot start: a file that is
    /// there and is no inventory of this format, which a check could not trust and a write would replace.
    /// </summary>
    public static string? Start(string mode, string? repository, string temp)
    {
        _mode = repository is null ? RunOptions.Off : mode;
        if (_mode == RunOptions.Off)
        {
            return null;
        }

        _repository = repository!;
        _path = Path.Combine(repository!, "docs", "axKit", "window-commands.json");
        _temp = temp;
        _profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!System.IO.File.Exists(_path))
        {
            return null;
        }

        var text = System.IO.File.ReadAllText(_path);
        _apps = CommandInventory.Read(text);
        return _apps.Count == 0 && Lf(text) != CommandInventory.Write([])
            ? $"{File} is not a command inventory of the format \"{CommandInventory.Format}\": put it right, or run with --inventory off."
            : null;
    }

    /// <summary>
    /// The scripts the run plays. Under <c>--inventory write</c> and <c>rewrite</c> only those that list a window's
    /// commands, by a <c>check commands</c> or a <c>do</c> step (WF-2.9), and one line saying how many were passed over;
    /// otherwise all of them.
    /// </summary>
    public static List<string> Playing(List<string> scripts, Action<string> say)
    {
        if (_mode is not (RunOptions.Write or RunOptions.Rewrite))
        {
            return scripts;
        }

        var playing = scripts.Where(Lists).ToList();
        if (playing.Count < scripts.Count)
        {
            say($"--inventory {_mode}: {scripts.Count - playing.Count} of {scripts.Count} script(s) passed over, since they hold neither a check commands nor a do step.");
        }

        // WF-2.9a: a write that plays every script of the repository that lists a window's commands knows every row there
        // is, so it removes the rows no window listed, as a rewrite does (a menu item renamed or gone left its old row
        // behind under write, which needed a remembered rewrite until 2026-11-24).
        var suite = Path.Combine(_repository, "tests", "scripts");
        var played = playing.Select(Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _wholeSuite = _mode == RunOptions.Write && Directory.Exists(suite)
            // The live scripts are left out of a run without --live (they start the real Claude Code), so the suite is
            // the others; the menus they list are the same windows' menus the others list.
            && Directory.GetFiles(suite, "*.txt", SearchOption.AllDirectories)
                .Where(file => !ScriptReader.IsLive(ScriptReader.NeedsOfFile(file)) && Lists(file))
                .All(file => played.Contains(Path.GetFullPath(file)));
        if (_wholeSuite)
        {
            say("--inventory write: every script that lists a window's commands plays, so the rows no window lists are removed, as --inventory rewrite does.");
        }

        return playing;
    }

    /// <summary>The repository the run writes into; empty under <c>off</c>.</summary>
    private static string _repository = string.Empty;

    /// <summary>Whether a write plays the whole suite of scripts that list commands, and so writes as a rewrite (WF-2.9a).</summary>
    private static bool _wholeSuite;

    /// <summary>
    /// A script's snapshots taken in: checked against the inventory under <c>check</c> (WF-2.10), kept for the end
    /// under <c>write</c> and <c>rewrite</c>, with whether the script passed, which decides a rewrite's whole apps.
    /// </summary>
    public static void After(ScriptResult result)
    {
        if (_mode == RunOptions.Off)
        {
            return;
        }

        foreach (var snapshot in result.Commands)
        {
            Seen.Add(snapshot);
            if (!result.Ok)
            {
                FailedApps.Add(snapshot.App);
            }

            if (snapshot.Notice is null)
            {
                ListedWhole.Add(snapshot.App);
            }

            if (_mode != RunOptions.Check)
            {
                continue;
            }

            if (!CommandInventory.Holds(_apps, snapshot.App))
            {
                if (!Unheld.Contains(snapshot.App, StringComparer.OrdinalIgnoreCase))
                {
                    Unheld.Add(snapshot.App);
                }

                continue;
            }

            foreach (var line in CommandInventory.Stale(_apps, snapshot, _temp, _profile).Where(line => !StaleLines.Contains(line)))
            {
                StaleLines.Add(line);
            }
        }
    }

    /// <summary>Whether <see cref="Converted"/> merged rows in: the file is then written at the end under <c>check</c> too.</summary>
    private static bool _converted;

    /// <summary>
    /// The rows the converter's <c>do</c> lines ran, merged into the inventory (WF-10.2, <see cref="ConvertRun"/>), so
    /// that the cheap check passes for the lines it wrote; the file is written at the run's end when it changed,
    /// whatever the mode but <c>off</c>. Only for an app the inventory holds rows for: an app joins it whole, by
    /// <c>--inventory write</c>, never by the few rows of one script.
    /// </summary>
    public static void Converted(IEnumerable<WindowCommands> rows)
    {
        if (_mode == RunOptions.Off)
        {
            return;
        }

        foreach (var snapshot in rows.Where(snapshot => CommandInventory.Holds(_apps, snapshot.App)))
        {
            _apps = CommandInventory.Merge(_apps, snapshot, _temp, _profile);
            _converted = true;
        }
    }

    /// <summary>
    /// The run's end: the lines said after the summary, and whether the inventory was found stale (exit code 3 when
    /// every script passed, WF-11.5). <c>check</c>: WF-2.10's words and one line per row that differs, then one line
    /// per app without rows. <c>write</c>: every snapshot merged in; <c>rewrite</c>: as write, and each whole app's
    /// rows only those this run saw (an app is whole when every script that gave a snapshot of it passed and one of
    /// those snapshots had no notice). The file is written when it changed, and one line says how many rows were
    /// added, replaced and removed. A run that was stopped before its end checks and writes nothing.
    /// </summary>
    public static (IReadOnlyList<string> Lines, bool Stale) End(bool stopped)
    {
        var lines = new List<string>();
        if (_mode == RunOptions.Off || stopped)
        {
            return (lines, false);
        }

        if (_mode == RunOptions.Check)
        {
            if (StaleLines.Count > 0)
            {
                lines.Add(Stale);
                lines.AddRange(StaleLines.Select(line => "  " + line));
            }

            lines.AddRange(Unheld.Select(app => $"The command inventory has no rows for {app} yet: its commands were not checked"));
            var merged = CommandInventory.Write(_apps);
            if (_converted && System.IO.File.Exists(_path) && Lf(System.IO.File.ReadAllText(_path)) != merged)
            {
                // WF-10.2: the rows of the converted do lines, as the windows listed them; CRLF, as below.
                System.IO.File.WriteAllText(_path, merged.Replace("\n", "\r\n", StringComparison.Ordinal), new UTF8Encoding(false));
                lines.Add($"The command inventory ({File}) written: the rows of the converted do lines merged in. Run tools\\workflows.ps1, and commit both with the scripts.");
            }

            return (lines, StaleLines.Count > 0);
        }

        IReadOnlyList<InventoryApp> written;
        if (_mode == RunOptions.Rewrite || _wholeSuite)
        {
            var whole = Seen.Select(snapshot => snapshot.App)
                .Where(app => !FailedApps.Contains(app) && ListedWhole.Contains(app))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            written = CommandInventory.Rewritten(_apps, Seen, whole, _temp, _profile);
            if (whole.Count > 0)
            {
                lines.Add($"Whole in this run, their rows only those it saw: {string.Join(", ", whole.Order(StringComparer.OrdinalIgnoreCase))}.");
            }
        }
        else
        {
            written = _apps;
            foreach (var snapshot in Seen)
            {
                written = CommandInventory.Merge(written, snapshot, _temp, _profile);
            }

            // A write of part of the suite adds and replaces, and cannot know that a row it did not see is gone: another
            // folder's scripts list the same window in states this run never reached. Said, so that a renamed or moved
            // command's old row is not left in silence (docs/axKit/repairs.json R-85, the update switch of 2026-11-24).
            lines.Add("Rows this run did not see are kept: a command renamed or gone keeps its old row until a write of the whole suite, or a --inventory rewrite of the app's scripts, removes it.");
        }

        var (added, replaced, removed) = Counted(_apps, written);
        var text = CommandInventory.Write(written);
        var before = System.IO.File.Exists(_path) ? Lf(System.IO.File.ReadAllText(_path)) : null;
        if (before == text)
        {
            lines.Add($"The command inventory ({File}) holds every row this run saw as it is: nothing written.");
        }
        else
        {
            // The repository's files are checked out with CRLF (.gitattributes, text=auto): written so, a file that did
            // not change in substance is not changed on disk either. UTF-8 without a byte order mark.
            System.IO.File.WriteAllText(_path, text.Replace("\n", "\r\n", StringComparison.Ordinal), new UTF8Encoding(false));
            lines.Add($"The command inventory ({File}) written: {added} row(s) added, {replaced} replaced, {removed} removed. Run tools\\workflows.ps1, and commit both with the change that made them differ (docs/axKit/user-workflows.md WF-11.4).");
        }

        return (lines, false);
    }

    /// <summary>Whether a script's lines hold a step that takes a snapshot: <c>check commands</c>, or <c>do</c>, after any <c>iffound</c>.</summary>
    private static bool Lists(string script)
    {
        IEnumerable<string> lines = script.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase)
            ? ScriptLines.Prepare(WorkflowFile.Read(script), Run.Nvda).Lines.Select(line => line.Text)
            : System.IO.File.ReadLines(script);
        foreach (var line in lines)
        {
            var text = line.Trim();
            while (text.StartsWith("iffound ", StringComparison.Ordinal))
            {
                text = text[8..].TrimStart();
            }

            if (text == "check commands" || text.StartsWith("do ", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// What a write changed, row by row in each app: added (no row of its label and menus was there), replaced (one
    /// was, not equal in every stored field) and removed (a row of a label and menus the new rows no longer have, or
    /// one more of a label and menus than the new rows hold).
    /// </summary>
    private static (int Added, int Replaced, int Removed) Counted(IReadOnlyList<InventoryApp> before, IReadOnlyList<InventoryApp> after)
    {
        int added = 0, replaced = 0, removed = 0;
        foreach (var app in before.Select(app => app.App).Concat(after.Select(app => app.App)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var old = CommandInventory.Of(before, app);
            var now = CommandInventory.Of(after, app);
            foreach (var place in old.Concat(now).Select(Place).Distinct(StringComparer.Ordinal))
            {
                var oldRows = old.Where(row => Place(row) == place).Select(Fields).ToList();
                var newRows = now.Where(row => Place(row) == place).Select(Fields).ToList();
                var unchanged = newRows.Count(fields => oldRows.Remove(fields));
                var changed = newRows.Count - unchanged;
                if (oldRows.Count == 0)
                {
                    added += changed;
                    continue;
                }

                var paired = Math.Min(changed, oldRows.Count);
                replaced += paired;
                added += changed - paired;
                removed += oldRows.Count - paired;
            }
        }

        return (added, replaced, removed);
    }

    /// <summary>A row's label and menus as the inventory compares them: plain, without regard to case.</summary>
    private static string Place(ListedCommand row) =>
        CommandNames.Plain(row.Label).ToUpperInvariant() + "\n" + CommandNames.Plain(row.Menu).ToUpperInvariant();

    /// <summary>A row's stored fields as one text, for telling an unchanged row from a changed one.</summary>
    private static string Fields(ListedCommand row) =>
        string.Join("\n", row.Label, row.Menu, row.Key, string.Join(" ", row.Also), row.KeyText, row.Mark, row.KeyOnly, row.Ai);

    private static string Lf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
