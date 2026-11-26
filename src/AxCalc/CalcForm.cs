using AxCalc.Core;
using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Forms;

namespace AxCalc;

/// <summary>
/// axCalc's window (SPEC.md AC-2): the bundle's conversation window (<see cref="ConversationForm"/>;
/// docs/axKit/framework.md step 9, 2026-11-22; a window of its own with the history as a list until then): the
/// history as the text, one line an answer, the expression field under it, and a status bar with the last answer.
/// Enter in the field answers in one announcement; the history keeps every answer to read back, copy and take
/// into the field again. The text, the field, the keys between them, find, the bookmarks and the Context menu on a
/// line are the conversation window's; this class holds the calculator's commands. The evaluator is
/// <see cref="Calculator"/> and the history's lines are <see cref="CalcTranscript"/>, both in the Core.
/// </summary>
public sealed class CalcForm : ConversationForm
{
    private readonly CalcSettings _settings;
    private readonly Calculator _calculator = new();
    private readonly List<CalcEntry> _entries = [];
    private readonly CalcTranscript _lines;
    private readonly ChoiceItems<bool> _angles;

    public CalcForm(CalcSettings settings, string? settingsError)
        : this(settings, settingsError, new CalcTranscript())
    {
    }

    private CalcForm(CalcSettings settings, string? settingsError, CalcTranscript lines)
        : base(AxCalcApp.Name, settings, CalcSettings.DefaultPath, settingsError, lines, AxCalcKeys.Transcript, "History", "Expression", showMarkers: false)
    {
        _settings = settings;
        _lines = lines;
        SetTitle("Calculator");
        _calculator.Degrees = settings.Degrees;
        foreach (var (name, value) in settings.Variables)
        {
            _calculator.SetVariable(name, value);
        }

        _entries.AddRange(settings.History);
        _calculator.Answer = _entries.Count > 0 ? _entries[^1].Value : null;

        // ---- menus and keys (AC-7.1, AC-7.2): one command list (docs/axKit/app-kit.md AK-2), each key from the tables ----
        // The bundle's eight (AX-9.1): Session the history's file and Exit; Actions the calculator's commands;
        // Navigation find and the bookmarks; Context what acts on the history's line.
        var calculator = new ToolStripMenuItem();
        calculator.DropDownItems.Add(Item("&Save history...", SaveHistory, BundleKeys.Save, CommandMark.Change));
        calculator.DropDownItems.Add(new ToolStripSeparator());
        calculator.DropDownItems.Add(CreateExitItem());
        var actions = new ToolStripMenuItem();
        Add(actions, new Command("&Answer", AxCalcKeys.Act, Evaluate) { When = () => Input.Focused, Mark = CommandMark.Read });
        Add(actions, new Command("&Copy answer", AxCalcKeys.CopyAnswer, CopyAnswer) { Mark = CommandMark.Act });
        var copyIn = new ToolStripMenuItem("Copy answer &in");
        copyIn.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Hexadecimal", null, (_, _) => CopyCode(16)), CommandMark.Act));
        copyIn.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Binary", null, (_, _) => CopyCode(2)), CommandMark.Act));
        copyIn.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Octal", null, (_, _) => CopyCode(8)), CommandMark.Act));
        actions.DropDownItems.Add(copyIn);
        Add(actions, new Command("Answer in other &bases", AxCalcKeys.OtherBases, SayBases) { Mark = CommandMark.Read });
        Add(actions, new Command("Say the &last answer", AxCalcKeys.LastAnswer, SayLastAnswer) { Mark = CommandMark.Read });
        actions.DropDownItems.Add(new ToolStripSeparator());
        actions.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Variables", null, (_, _) => ShowVariables()), CommandMark.Open));
        actions.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Clear va&riables...", null, (_, _) => ClearVariables()), CommandMark.Change));
        Add(actions, new Command("Clear &history...", AxCalcKeys.ClearHistory, ClearHistory) { Mark = CommandMark.Act });

        var navigation = new ToolStripMenuItem();
        AddFindItems(navigation);
        navigation.DropDownItems.Add(new ToolStripSeparator());
        AddBookmarkItems(navigation);

        // Options, as the other windows' (AX-6): the window's own submenus and items first, then Text, General, Developer.
        var options = new ToolStripMenuItem();
        var angles = new ToolStripMenuItem("&Angles");
        _angles = new ChoiceItems<bool>(angles, [(true, "&Degrees"), (false, "&Radians")], _calculator.Degrees, SetDegrees);
        options.DropDownItems.Add(angles);
        options.DropDownItems.Add(Toggle("&Keep history between runs", settings.KeepHistory, v =>
        {
            // AC-9.1: off, the file holds no history from the save that follows.
            _settings.KeepHistory = v;
            _settings.Keep(_entries, _calculator.Variables);
        }));
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Calculator &resources",
            [("&Floating point explained", "https://floating-point-gui.de")]);

        // The bundle's menus (AX-9.1), Window, axKit and Help, end the bar in every window.
        PlaceMenuBar(MenuBar, calculator, actions, navigation, CreateTranscriptContext("Take this line's e&xpression into the field"), options, help);

        // Ctrl+Delete in the field clears it (AC-6.3; Escape until 2026-11-26, AX-8.18: Escape only goes back or
        // closes); in the history Escape goes back to the field and Enter takes the line's expression there, both the
        // conversation window's own (AC-5.2).
        AddKey(new Command("Clear the field", AxCalcKeys.ClearField, ClearField) { When = () => Input.Focused, Mark = CommandMark.Change });
        // Escape in the field: nothing to go back to, so the edge sound, never silence (AX-8.18: Escape goes back or
        // closes; the maintainer's "minimal messages, never silence"; silent from 2026-11-26 until the fix of that day).
        AddKey(new Command("Back: in the field, nothing to go back to", AxCalcKeys.Back, () => EventDoor.Raise(EventKind.Edge)) { When = () => Input.Focused, Mark = CommandMark.Read });
        Transcript.LineChosen += (_, _, text) => Recall(text);

        // ---- the history (AC-2.1): the answers kept from the last run, as lines ----
        _lines.Load(_entries.Select(entry => entry.Label));

        UpdateAngles();
        UpdateStatus();
        ApplyTextFont();
        RestoreWindow();
    }

    // ---- what the conversation window asks for ----

    /// <summary>Nothing of its own follows a new line: the answer is said by the command that gave it.</summary>
    protected override void OnTranscriptChanged()
    {
    }

    /// <summary>The status bar (AC-2.3): the last answer on the left; the angle unit and the variables on the right.</summary>
    protected override void UpdateStatus()
    {
        var variables = _calculator.Variables.Count;
        SetStatus(
            _calculator.Answer is { } answer ? "Answer " + NumberText.Format(answer) : "No answer yet",
            $"{(_calculator.Degrees ? "Degrees" : "Radians")}, {(variables == 0 ? "no variables" : variables == 1 ? "1 variable" : $"{variables} variables")}");
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override IEnumerable<string> DiagnosticsLines =>
        [$"History: {_entries.Count} entries, {_calculator.Variables.Count} variables, {(_calculator.Degrees ? "degrees" : "radians")}"];

    /// <summary>Ctrl+S, the bundle's save key: the history as a text file (AC-5.5).</summary>
    protected override void SaveCommand() => SaveHistory();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // AC-9.1: the history and the variables into the settings, which the base then writes.
        _settings.Keep(_entries, _calculator.Variables);
        base.OnFormClosing(e);
    }

    // ---- the answer (AC-3, AC-6) ----

    /// <summary>Enter in the field: the answer said and added to the history, the field emptied; an error said and the field kept to mend.</summary>
    private void Evaluate()
    {
        var text = Input.Text;
        if (text.Trim().Length == 0)
        {
            Announce("Type an expression", true);
            return;
        }

        var answer = _calculator.Evaluate(text);
        if (!answer.Ok)
        {
            Announce(answer.Spoken, true);
            return;
        }

        _entries.Add(CalcEntry.From(text, answer));
        _lines.AddAnswer(_entries[^1].Label);
        while (_entries.Count > CalcSettings.MaxHistory)
        {
            _entries.RemoveAt(0);
            _lines.RemoveOldestAnswer();
        }

        Input.Clear();
        UpdateStatus();
        Announce(answer.Spoken, true);
    }

    private void ClearField()
    {
        if (Input.TextLength == 0)
        {
            Announce("Empty", true);
            return;
        }

        Input.Clear();
        Announce("Cleared", true);
    }

    /// <summary>The entry a line of the history is, by its words; the newest of several that read the same, which hold the same expression and answer.</summary>
    private CalcEntry? EntryOf(string? line) => line is null ? null : _entries.LastOrDefault(entry => entry.Label == line);

    /// <summary>The value a command acts on: the answer of the line you are on while the history has the focus, the last answer otherwise.</summary>
    private double? CurrentValue() =>
        Transcript.Focused && EntryOf(Transcript.CaretLineText()) is { } entry ? entry.Value : _calculator.Answer;

    private void SayLastAnswer() =>
        Announce(_calculator.Answer is { } answer ? "Answer " + NumberText.Format(answer) : "No answer yet", true);

    /// <summary>Ctrl+I (AC-4.1): the answer in hexadecimal, binary and octal.</summary>
    private void SayBases()
    {
        if (CurrentValue() is not { } value)
        {
            Refuse("No answer yet");
            return;
        }

        Announce(NumberText.Bases(value), true);
    }

    private void CopyAnswer()
    {
        if (CurrentValue() is not { } value)
        {
            Refuse("No answer yet");
            return;
        }

        Copy(NumberText.Format(value));
    }

    /// <summary>Calculator → Copy answer in (AC-4.2): "0xFF", "0b11111111", "0o377"; why not, for an answer with no other bases.</summary>
    private void CopyCode(int radix)
    {
        if (CurrentValue() is not { } value)
        {
            Refuse("No answer yet");
            return;
        }

        if (NumberText.Code(value, radix) is { } code)
        {
            Copy(code);
        }
        else
        {
            Announce(NumberText.Bases(value), true);
        }
    }

    private void Copy(string text) => CopyText(text, "Copied " + text);

    // ---- the history (AC-5) ----

    private IReadOnlyList<string> Labels() => _entries.Select(entry => entry.Label).ToList();

    /// <summary>
    /// Enter on a line of the history, and the Context menu's item for it: the line's expression in the field, the
    /// caret at its end, the focus there. A line that is no answer (a line of the app's own, a bookmark) says so.
    /// </summary>
    private void Recall(string line)
    {
        if (EntryOf(line) is not { } entry)
        {
            Announce(_entries.Count == 0 ? "History, empty" : "No answer on this line", true);
            return;
        }

        Input.Text = entry.Expression;
        Input.Focus();
        Input.SelectionStart = Input.TextLength;
    }

    private void ClearHistory()
    {
        if (_entries.Count == 0)
        {
            Announce("History, empty", true);
            return;
        }

        var count = _entries.Count;
        ShowNotice(Notice.Ask("Clear the history?", count == 1 ? "The entry cannot be brought back." : $"The {count} entries cannot be brought back.", "C&lear", () =>
        {
            _entries.Clear();
            _lines.ClearAnswers();
            UpdateStatus();
            SaveState();
            Announce("History cleared", true);
        }));
    }

    /// <summary>Ctrl+S (AC-5.5): one entry per line through the Windows save dialog.</summary>
    private void SaveHistory()
    {
        if (_entries.Count == 0)
        {
            Announce("History, empty", true);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Save the history",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = "txt",
            FileName = "calculations.txt",
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        var path = dialog.FileName;
        var lines = Labels();
        Do(async () =>
        {
            try
            {
                await File.WriteAllLinesAsync(path, lines);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Fail($"{path} could not be written: {ex.Message}");
                return;
            }

            Announce("Saved " + Path.GetFileName(path), true);
        });
    }

    // ---- the variables (AC-3.6, AC-5.4) ----

    private void ShowVariables()
    {
        var variables = _calculator.Variables;
        var text = variables.Count == 0
            ? "No variables. Set one with name = value"
            : string.Join("\n", variables.Select(pair => $"{pair.Key} = {NumberText.Format(pair.Value)}"));
        ShowNotice(Notice.Plain("Variables", text));
    }

    private void ClearVariables()
    {
        var count = _calculator.Variables.Count;
        if (count == 0)
        {
            Announce("No variables", true);
            return;
        }

        ShowNotice(Notice.Ask("Clear the variables?", count == 1 ? "The variable cannot be brought back." : $"The {count} variables cannot be brought back.", "C&lear", () =>
        {
            _calculator.ClearVariables();
            UpdateStatus();
            SaveState();
            Announce("Variables cleared", true);
        }));
    }

    // ---- settings (AC-9) ----

    private void SetDegrees(bool degrees)
    {
        _calculator.Degrees = degrees;
        _settings.Degrees = degrees;
        UpdateAngles();
        UpdateStatus();
        SaveSettings();
        Announce(degrees ? "Degrees" : "Radians", true);
    }

    private void UpdateAngles() => _angles.Check(_calculator.Degrees);

    /// <summary>The history and the variables into the settings file now: after a clear, which should not come back after a crash.</summary>
    private void SaveState()
    {
        _settings.Keep(_entries, _calculator.Variables);
        SaveSettings();
    }
}
