using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// The actions of a list (docs/axTask/forms-framework.md sections 2 to 7; SPEC.md TK-7a): what its configuration
/// declares under <c>"actions"</c>, run on the item selected. Five kinds: <c>set</c> writes fields, <c>move</c> moves
/// the item to a list, <c>open</c> opens a field's address or an app on the item's file, <c>run</c> starts a program
/// with its arguments as a list and no window, <c>terminal</c> hands a command line to axTerminal for the user to
/// send. Context, Actions of this list, names them all; eighteen keys run them (Ctrl+1 to Ctrl+9 and with Shift).
/// What an action is and what its values become are <c>AxTask.Core</c>'s (<c>ListActions</c>, <c>ActionValues</c>,
/// <c>ActionProgram</c>); this file asks, runs and says.
///
/// The rules that keep it safe are few. Nothing of an item ever becomes a command: a value is one argument, the
/// program is fixed in the file and found outside the list's folder. A program is asked about before its first run
/// on this computer and after any change to its action, with everything that would run written out. An action not
/// understood is never run. axTask's own code still asks the network nothing; what a program reaches is its own.
/// </summary>
public sealed partial class TaskForm
{
    private readonly ToolStripMenuItem _actionsMenu = new("Actions of this &list");

    /// <summary>The action whose program runs now, by its name; null when none does. One at a time in a window (AF-7.6).</summary>
    private string? _running;

    private CancellationTokenSource? _runStop;

    /// <summary>The job the actions' programs run in: they end with this window, however it ends.</summary>
    private ChildJob? _actionJob;

    /// <summary>What the last program run wrote, both streams, its last 64 KB, kept in memory only and never logged (AF-7.3).</summary>
    private (string Name, string Text)? _lastOutput;

    private const int OutputKept = 64 * 1024;

    /// <summary>The action the window last asked about by saying what it does (a move or an open, TK-7a.3), as its mark; null when none waits for its second press.</summary>
    private string? _askedAction;

    /// <summary>The status bar's second part (AF-7.6): the top folder, with "Running" and the action's name before it while a program of an action runs.</summary>
    private string PlaceText => _running is { } running ? $"Running {running}. {TopFolder}" : TopFolder;

    // ---- the list in hand ----

    private ListConfig? CurrentListDetails => _smart is null && !AtTop && Directory.Exists(_folder) ? ItemFolder.ReadList(_folder) : null;

    /// <summary>The menu and the keys of this file, placed by the constructor.</summary>
    private void PlaceActionMenu()
    {
        for (var key = 1; key <= ListActions.KeyCount; key++)
        {
            var k = key;
            var name = key <= 9 ? $"Action key {key}" : $"Action key Shift+{key - 9}";
            AddKey(new Command(name, AxTaskKeys.ActionKey(key), () => RunActionKey(k)) { When = NotRenaming, Mark = CommandMark.Act });
        }

        CommandPalette.RefreshedOnOpening(_actionsMenu, RefreshActionItems);
        RefreshActionItems();
    }

    /// <summary>
    /// The list's actions by their names, each with its key, as the menu opens (AF-6.2), so that the palette and an
    /// AI tool's listing hold them too; then Read the last output. An action whose form has a hidden field is never
    /// offered to the AI tools (AF-7.5).
    /// </summary>
    private void RefreshActionItems()
    {
        _actionsMenu.DropDownItems.Clear();
        var actions = ListActions.Read(CurrentListDetails);
        foreach (var action in actions)
        {
            var named = action.Name;
            // An action may open the browser, start an app or run a program: act, whatever its kind.
            var item = CommandPalette.Marked(new ToolStripMenuItem(action.Label.Replace("&", "&&", StringComparison.Ordinal), null, (_, _) => RunActionNamed(named)), CommandMark.Act);
            if (action.Key is { } key)
            {
                item.ShortcutKeyDisplayString = BundleKeys.Text(AxTaskKeys.ActionKey(key));
            }

            _actionsMenu.DropDownItems.Add(action.HasHiddenField ? CommandPalette.NeverForAiTools(item) : item);
        }

        if (actions.Count == 0)
        {
            _actionsMenu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("No actions in this list", null,
                (_, _) => Announce("This list has no actions. They are written in the list's configuration: Ctrl+J on the list opens it in axJSON", true)), CommandMark.Read));
        }

        _actionsMenu.DropDownItems.Add(new ToolStripSeparator());
        _actionsMenu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Read the last &output", null, (_, _) => ReadLastOutput()), CommandMark.Read));
    }

    // ---- running an action ----

    private void RunActionKey(int key)
    {
        if (_smart is not null || AtTop)
        {
            EventDoor.Raise(EventKind.NoCommand, EventSound.Edge);
            Announce("Action keys act in a list: open one first", true);
            return;
        }

        if (ListActions.Read(CurrentListDetails).FirstOrDefault(known => known.Key == key) is not { } action)
        {
            EventDoor.Raise(EventKind.NoCommand, EventSound.Edge);
            Announce($"No action on {ListActions.KeyText(key)} in this list; its actions are written in its configuration, Ctrl+J on the list", true);
            return;
        }

        RunAction(action, ListActions.KeyText(key), pressed: true);
    }

    /// <summary>An action chosen in the menu: read again from the file, by its name, so that what runs is what the file holds now.</summary>
    private void RunActionNamed(string name)
    {
        if (ListActions.Read(CurrentListDetails).FirstOrDefault(known => known.Name == name) is not { } action)
        {
            Refuse($"The list has no action called {name} any more");
            return;
        }

        RunAction(action, action.Name, pressed: false);
    }

    private void RunAction(ConfiguredAction action, string how, bool pressed)
    {
        // An AI tool's run of an action that starts a program is asked about whatever was accepted before (AF-7.5).
        var forAiTool = ClickedForAiTool;
        if (action.NotUnderstood is not null)
        {
            Refuse(action.Label);
            return;
        }

        if (SelectedRow() is not { Tag: ItemFile item })
        {
            Refuse("Select an item first");
            return;
        }

        if (!Changeable(item))
        {
            return;
        }

        var folder = Path.GetDirectoryName(item.Path)!;
        if (action.Fields.Count == 0)
        {
            PerformAction(action, item, folder, new Dictionary<string, string>(), how, pressed, forAiTool);
            return;
        }

        AskActionForm(action, item, folder, values => PerformAction(action, item, folder, values, how, pressed, forAiTool));
    }

    /// <summary>
    /// An action's form (AF-5): a control for each of its fields in order, named by the field's title, which the
    /// file writes as an instruction; the form's text says what the action will do; Ctrl+Enter or the button runs
    /// it, Escape leaves and nothing happened. A hidden field is typed unseen and its box emptied as it is read.
    /// </summary>
    private void AskActionForm(ConfiguredAction action, ItemFile item, string folder, Action<IReadOnlyDictionary<string, string>> then)
    {
        var context = new ActionContext(item.Flat, item.Config, item.Path, folder, new Dictionary<string, string>(), Today);
        var form = new FormControls();
        var made = new List<(ActionField Field, Control Control)>();
        foreach (var field in action.Fields)
        {
            var (first, refused) = ActionValues.Fill(field.Value, context, action.Name, argument: false);
            if (refused is not null)
            {
                Refuse(refused);
                return;
            }

            switch (field.Type)
            {
                case FieldTypes.Choice:
                    made.Add((field, form.AddList(field.Title, field.Options, Math.Max(0, field.Options.ToList().FindIndex(option => option.Equals(first, StringComparison.OrdinalIgnoreCase))))));
                    break;
                case FieldTypes.LongText:
                    made.Add((field, form.AddLines(field.Title, rows: 4, first ?? string.Empty)));
                    break;
                default:
                    var box = form.AddText(field.Title, first ?? string.Empty);
                    box.UseSystemPasswordChar = field.Hidden;
                    made.Add((field, box));
                    break;
            }
        }

        // What each control holds, as the action takes it; or the control that still needs something, and what to say.
        (Dictionary<string, string>? Values, Control? Lacking, string? Said) Typed()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (field, control) in made)
            {
                var text = control is AxKitList list ? list.SelectedItem as string ?? string.Empty : control.Text.ReplaceLineEndings("\n");
                if (field.Required && text.Trim().Length == 0)
                {
                    return (null, control, $"{field.Title}: this is needed");
                }

                if (field.Type == FieldTypes.Date && text.Trim().Length > 0)
                {
                    var parsed = FieldTypes.Parse(FieldTypes.Date, text, [], Today);
                    if (parsed.Refused is not null)
                    {
                        return (null, control, $"{field.Title}: {parsed.Refused}");
                    }

                    text = FieldTypes.TextOf(parsed.Value);
                }

                values[field.Title] = text;
            }

            return (values, null, null);
        }

        ShowForm(action.Name,
            $"{action.Name} {action.Described}. It acts on {ItemFolder.NameOf(item)}. Ctrl+Enter or the button runs it; Escape leaves, and nothing happened."
                + (form.EnterMakesNewLine ? " In a box of several lines Enter makes a new line." : string.Empty),
            form, action.Name.Replace("&", "&&", StringComparison.Ordinal),
            () =>
            {
                var values = Typed().Values;
                // What was typed unseen is not kept in the window beyond this.
                foreach (var (field, control) in made)
                {
                    if (field.Hidden)
                    {
                        control.Text = string.Empty;
                    }
                }

                if (values is not null)
                {
                    then(values);
                }
            },
            () => Typed() is { Lacking: { } lacking, Said: { } said } ? (lacking, said) : null);
    }

    private void PerformAction(ConfiguredAction action, ItemFile item, string folder, IReadOnlyDictionary<string, string> form, string how, bool pressed, bool forAiTool)
    {
        var context = new ActionContext(item.Flat, item.Config, item.Path, folder, form, Today);
        switch (action.Does)
        {
            case ListActions.SetKind:
                var (set, refused) = ActionValues.Set(action.Set, context, action.Name);
                if (refused is not null)
                {
                    Refuse(refused);
                    return;
                }

                SetFields(item, set!, action.Name);
                return;
            case ListActions.MoveKind:
                if (!action.AsksFirst || AcceptedOrAsked(action, folder, how, pressed))
                {
                    MoveItem(item, folder, action.To, action.Name);
                }

                return;
            case ListActions.OpenKind:
                if (!action.AsksFirst || AcceptedOrAsked(action, folder, how, pressed))
                {
                    OpenTarget(item, action.Open);
                }

                return;
            case ListActions.RunKind:
                StartRun(action, item, folder, context, forAiTool);
                return;
            default:
                StartTerminal(action, item, folder, context, forAiTool);
                return;
        }
    }

    /// <summary>A refusal: its sound, and its words said once.</summary>
    private void Refuse(string said)
    {
        EventDoor.Raise(EventKind.Refused);
        Announce(said, true);
    }

    /// <summary>
    /// Whether an action that moves the item or opens something may act now (SPEC.md TK-7a.3; bundle repairs R-72):
    /// yes when the user accepted it before. Otherwise the first press says what it does in this list and does
    /// nothing; the same action run again, still the same in the file, accepts it, for good where the settings can
    /// be saved, and acts. A configuration that came from elsewhere or was rewritten so never moves an item or opens
    /// a program or an address at the first press. An acceptance of the action keys there were before 2026-11-21
    /// stands (the key's digit and the action as it was then, <see cref="ActionKeys.MarkText"/>).
    /// </summary>
    private bool AcceptedOrAsked(ConfiguredAction action, string folder, string how, bool pressed)
    {
        var mark = ListActions.MarkText(folder, action);
        var before = action.Does == ListActions.MoveKind ? new ListAction(action.Name, Move: action.To) : new ListAction(action.Name, Open: action.Open);
        if (_settings.AcceptsAction(SettingsPath, mark) || (action.Key is >= 1 and <= 9 && _settings.AcceptsActionKey(SettingsPath, folder, action.Key.Value, before)))
        {
            return true;
        }

        if (_askedAction == mark)
        {
            _askedAction = null;
            if (_settings.AcceptAction(SettingsPath, mark))
            {
                SaveSettings();
            }

            return true;
        }

        _askedAction = mark;
        Announce($"{how} in this list now {action.Described}. {(pressed ? "Press it again" : "Choose it again")} to do it.", true);
        return false;
    }

    /// <summary>A set action (RP-8.1): the fields written as one step.</summary>
    private void SetFields(ItemFile item, IReadOnlyDictionary<string, JsonNode?> set, string name)
    {
        if (!CheckFresh(item.Path, item.Text))
        {
            return;
        }

        var changed = ActionKeys.ApplySet(item.Document, set);
        Commit(new Step([FileChange.Write(item.Path, item.Text, item.TextWith(changed))], $"{name}: {ActionKeys.SpokenSet(set)}"), item.Path);
    }

    /// <summary>A move action (RP-8.1): to the list at the path from the item's own list, the links following (IA-D3).</summary>
    private void MoveItem(ItemFile item, string folder, string move, string name)
    {
        var target = Path.GetFullPath(Path.Combine(folder, move));
        var top = TopFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Directory.Exists(target) || !target.StartsWith(top, StringComparison.OrdinalIgnoreCase) || ItemFolder.IsArchive(TopFolder, target) || target.Equals(folder, StringComparison.OrdinalIgnoreCase))
        {
            Announce($"No list to move to at {move}", true);
            return;
        }

        if (!CheckFresh(item.Path, item.Text))
        {
            return;
        }

        var to = ItemFiles.FreePath(target, Path.GetFileNameWithoutExtension(item.Path), ".json");
        var parts = new List<FileChange> { FileChange.Move(item.Path, to) };
        parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopFolder), item.Path, to));
        Commit(new Step(parts, $"{name}: {ItemFolder.NameOf(item)} moved to {Path.GetFileName(target)}"), NeighbourKey());
    }

    /// <summary>An open action (RP-8.1): an app on the item's file, or a field's web address in the browser.</summary>
    private void OpenTarget(ItemFile item, string open)
    {
        if (Dispatch.Apps.FirstOrDefault(known => known.Verb.Equals(open, StringComparison.OrdinalIgnoreCase)) is { } app)
        {
            // A shell or Claude Code never on a folder off this computer (RP-8.1, S-5's rule; 2026-11-09).
            if (ActionKeys.OpenRefusal(app.Verb, item.Path) is { } refused)
            {
                Announce(refused, true);
                return;
            }

            WindowMenu.Start(app.Verb, item.Path);
            return;
        }

        // The address is the item's own value, whether its list names the field or keeps it.
        var held = item.Flat.Has(open);
        var value = item.Flat.Text(open).Trim();
        if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            OpenUrl(value);
            return;
        }

        Announce(!held && item.Document.IndexOf(open) < 0 ? $"No field called {open}, and no app called that" : value.Length == 0 ? $"{open} is empty" : $"{open} holds no web address", true);
    }

    // ---- a command for axTerminal ----

    /// <summary>
    /// A terminal action (AF-4.5, AF-7.4): its command filled for the item and handed to a new axTerminal window in
    /// the action's folder, each part as an argument of its own; axTerminal makes the line for its shell and puts it
    /// into its command field. Nothing is sent: the user reads it there and presses Enter himself. Asked about
    /// before the first time and after any change of the action, as a program is (AF-7.2), and every time for an AI
    /// tool; only in a folder on a drive of this computer (S-5).
    /// </summary>
    private void StartTerminal(ConfiguredAction action, ItemFile item, string folder, ActionContext context, bool forAiTool)
    {
        var (terminal, refused) = ActionValues.Terminal(action, context);
        if (terminal is null)
        {
            Refuse(refused ?? $"{action.Name} cannot be filled for this item");
            return;
        }

        if (!Directory.Exists(terminal.Folder) || !LocalPaths.IsLocal(terminal.Folder))
        {
            Refuse($"{action.Name} would open axTerminal in {terminal.Folder}, which is not a folder on a drive of this computer");
            return;
        }

        void HandOver()
        {
            // A new axTerminal window with the command's parts as its arguments, which puts them into its command
            // field as one line for its shell and sends nothing (axTerminal's SPEC AT-1.5a). The start itself, of
            // axKit's own program file, is WindowMenu's, where its outside-call comment is (AF-7.7).
            WindowMenu.StartWith(Dispatch.AxTerminal, [terminal.Folder, TerminalCommandOption, .. terminal.Command]);
            Announce($"{action.Name} is in axTerminal's command field", true);
        }

        var mark = ListActions.MarkText(folder, action);
        if (!forAiTool && _settings.AcceptsAction(SettingsPath, mark))
        {
            HandOver();
            return;
        }

        // The AI caution first, and in the question's name, heard every time (bundle rule 11, AX-11.12).
        var text = new StringBuilder()
            .AppendLine("# Read this first").AppendLine()
            .AppendLine(AiCaution.Action).AppendLine()
            .AppendLine("# The action").AppendLine()
            .AppendLine($"It is in the file {Path.Combine(folder, ItemDocument.ListFile)}.")
            .AppendLine()
            .AppendLine($"{action.Name} {action.Described}.")
            .AppendLine()
            .AppendLine($"The folder axTerminal opens in: {terminal.Folder}")
            .AppendLine($"The command as it is for {ItemFolder.NameOf(item)}, one part on a line:");
        foreach (var part in terminal.Command)
        {
            text.AppendLine(DisplayText.Visible(part));
        }

        text.AppendLine().Append("axTerminal puts the command into its command field and does not send it: you read it there and press Enter yourself. axKit does not know what the command does or reaches. Sending it is your own choice.");
        ShowNotice(new Notice($"Put {action.Name} into axTerminal? {AiCaution.ActionShort}", text.ToString(),
        [
            new OverlayChoice("&Cancel", IsDefault: true, IsCancel: true),
            new OverlayChoice("Put it there &once", HandOver),
            new OverlayChoice("Put it there, and do not &ask again for this action", () =>
            {
                if (_settings.AcceptAction(SettingsPath, mark))
                {
                    SaveSettings();
                }

                HandOver();
            }),
        ]));
    }

    /// <summary>The option of axTerminal's command line after which every argument is a part of the command (axTerminal's SPEC AT-1.5a).</summary>
    private const string TerminalCommandOption = "--command";

    // ---- a program ----

    /// <summary>
    /// A run action (AF-3.2, AF-7): filled for the item, its program found, and then run at once when the user
    /// accepted this very action before, or asked about first. Every refusal comes before anything is started.
    /// </summary>
    private void StartRun(ConfiguredAction action, ItemFile item, string folder, ActionContext context, bool forAiTool)
    {
        var (run, refused) = ActionValues.Run(action, context);
        if (run is null)
        {
            Refuse(refused ?? $"{action.Name} cannot be filled for this item");
            return;
        }

        var (program, why) = ActionProgram.Find(run.Program, folder);
        if (program is null)
        {
            Refuse($"{action.Name} {why}");
            return;
        }

        if (!Directory.Exists(run.Folder) || !LocalPaths.IsLocal(run.Folder))
        {
            Refuse($"{action.Name} would run in {run.Folder}, which is not a folder on a drive of this computer");
            return;
        }

        var line = SystemPrograms.LineFor(program, run.Arguments);
        if (line.Refusal is { } unsafeFor)
        {
            Refuse($"{action.Name}: {unsafeFor}");
            return;
        }

        if (_running is { } other)
        {
            Refuse($"{other} is still running: Actions, Stop the running action ends it");
            return;
        }

        var mark = ListActions.MarkText(folder, action);
        if (!forAiTool && _settings.AcceptsAction(SettingsPath, mark))
        {
            RunNow(action, item, program, line, run);
            return;
        }

        // AF-7.2: asked before the first run on this computer, and again after any change to the action. Cancel is
        // where Enter is; running is never the default answer (bundle rule 11).
        var typedUnseen = action.Input is { } input && action.Fields.Any(field => field.Hidden && (ActionValues.Names(input) ?? []).Any(name => field.Title.Equals(ActionValues.FormFieldOf(name), StringComparison.OrdinalIgnoreCase)));
        // The AI caution first, and in the question's name, heard every time (bundle rule 11, AX-11.12).
        var text = new StringBuilder()
            .AppendLine("# Read this first").AppendLine()
            .AppendLine(AiCaution.Action).AppendLine()
            .AppendLine("# The action").AppendLine()
            .AppendLine($"It is in the file {Path.Combine(folder, ItemDocument.ListFile)}.")
            .AppendLine()
            .AppendLine($"{action.Name} {action.Described}.")
            .AppendLine()
            .AppendLine($"The program: {program}")
            .AppendLine($"The folder it runs in: {run.Folder}")
            .AppendLine(run.Arguments.Count == 0 ? "It gets no arguments." : $"Its arguments as they are for {ItemFolder.NameOf(item)}, one on a line:");
        foreach (var argument in run.Arguments)
        {
            text.AppendLine(DisplayText.Visible(argument));
        }

        if (run.Input is not null)
        {
            text.AppendLine().AppendLine(typedUnseen ? "Its input holds what you typed unseen, and is not shown." : "Its input:");
            if (!typedUnseen)
            {
                text.AppendLine(DisplayText.Visible(run.Input));
            }
        }

        if (run.Then.Count > 0)
        {
            text.AppendLine().AppendLine($"When the program ends without an error, the item gets: {ActionKeys.SpokenSet(run.Then)}.");
        }

        text.AppendLine().Append("axKit does not know what this program does or reaches. Running it is your own choice.");
        ShowNotice(new Notice($"Run {action.Name}? {AiCaution.ActionShort}", text.ToString(),
        [
            new OverlayChoice("&Cancel", IsDefault: true, IsCancel: true),
            new OverlayChoice("Run it &once", () => RunNow(action, item, program, line, run)),
            new OverlayChoice("Run it, and do not &ask again for this action", () =>
            {
                if (_settings.AcceptAction(SettingsPath, mark))
                {
                    SaveSettings();
                }

                RunNow(action, item, program, line, run);
            }),
        ]));
    }

    /// <summary>
    /// The program run to its end off the window's thread (AF-7.3, AF-7.6): "Running Answer" as it starts, then
    /// "Answer: done" with what its <c>then</c> set, or "Answer failed, exit code 1" with the sound of a failure and
    /// its output opened. One at a time; in a job that ends with the window; nothing of its output is logged.
    /// </summary>
    private async void RunNow(ConfiguredAction action, ItemFile item, string program, ProgramLine line, FilledRun run)
    {
        if (_running is { } other)
        {
            Refuse($"{other} is still running: Actions, Stop the running action ends it");
            return;
        }

        var name = action.Name;
        _running = name;
        SetPlace(PlaceText);
        var stop = _runStop = new CancellationTokenSource();
        var job = _actionJob ??= ChildJob.TryCreate();
        Announce($"Running {name}", true);
        Log.Info("An action of a list started a program");

        var utf8 = new UTF8Encoding(false);
        var start = new ProcessStartInfo(line.FileName, line.Arguments)
        {
            WorkingDirectory = run.Folder,
            StandardOutputEncoding = utf8,
            StandardErrorEncoding = utf8,
            StandardInputEncoding = utf8,
        };
        IReadOnlyList<string> input = run.Input is null ? [] : run.Input.ReplaceLineEndings("\n").Split('\n');
        ProcessResult? result = null;
        string? notStarted = null;
        var stopped = false;
        try
        {
            // Outside call, program, List actions: the program an action of the list's configuration names, found on
            // the search path or by its full path and never in the list's folder, with its arguments as a list;
            // asked about before its first run and after every change of the action (forms-framework.md AF-7.2).
            result = await Task.Run(() => ProcessRun.RunAsync(start, input, TimeSpan.FromSeconds(run.Seconds), stop.Token, process => job?.Add(process)));
        }
        catch (OperationCanceledException)
        {
            stopped = true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            notStarted = ex.Message;
        }

        _running = null;
        _runStop = null;
        stop.Dispose();
        if (IsDisposed)
        {
            return;
        }

        SetPlace(PlaceText);

        if (stopped)
        {
            Announce($"{name} stopped", true);
            return;
        }

        if (result is null)
        {
            EventDoor.Raise(EventKind.Failed);
            Announce($"{name} could not be started: {notStarted}", true);
            return;
        }

        var written = result.Output + (result.Error.Length == 0 ? string.Empty : (result.Output.Length == 0 ? string.Empty : "\r\n\r\n") + "What it wrote as errors:\r\n" + result.Error);
        _lastOutput = (name, written.Length <= OutputKept ? written : "(The start is cut: the last 64 KB.)\r\n" + written[^OutputKept..]);
        if (result.TimedOut || result.ExitCode != 0)
        {
            EventDoor.Raise(EventKind.Failed);
            Announce(result.TimedOut ? $"{name} was stopped after {run.Seconds} seconds, its time limit" : $"{name} failed, exit code {result.ExitCode}", true);
            ReadLastOutput();
            return;
        }

        // A program that ended well sounds as a finished command does everywhere, the done chime (the maintainer's
        // choice of 2026-11-21): it may have taken a while, and the user may be elsewhere in the window by then.
        EventDoor.Raise(EventKind.TurnDone);

        // AF-3.2: what the action sets once its program ended well, as one undo step, on the item as it is now.
        if (run.Then.Count > 0)
        {
            if (ItemFolder.ReadItem(item.Path) is not { ConfigProblem: null } now)
            {
                Announce($"{name}: done. Its item is no longer there to be changed, so nothing was set", true);
                return;
            }

            var changed = ActionKeys.ApplySet(now.Document, run.Then);
            Commit(new Step([FileChange.Write(now.Path, now.Text, now.TextWith(changed))], $"{name}: done. {ActionKeys.SpokenSet(run.Then)}"), SelectedKey());
            return;
        }

        Announce($"{name}: done", true);
    }

    /// <summary>Actions, Stop the running action (AF-7.6): the program ended with everything it started.</summary>
    private void StopAction()
    {
        if (_runStop is not { } stop || _running is null)
        {
            Announce("No action is running", true);
            return;
        }

        stop.Cancel();
    }

    /// <summary>Context, Actions of this list, Read the last output (AF-7.3): what the last program wrote, read-only.</summary>
    private void ReadLastOutput()
    {
        if (_lastOutput is not { } last)
        {
            Announce("No action has run a program in this window yet", true);
            return;
        }

        ShowNotice(Notice.Plain($"Output of {last.Name}", last.Text.Trim().Length == 0 ? "It wrote nothing." : DisplayText.Visible(last.Text)));
    }

    /// <summary>The programs the actions started end with the window.</summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _runStop?.Cancel();
        _actionJob?.Dispose();
        base.OnFormClosed(e);
    }
}
