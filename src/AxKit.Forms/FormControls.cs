using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Forms;

/// <summary>One field of a form as the AI tools read it (docs/axKit/mcp.md MC-7.10): its number from 1, its name, its keys, its kind, its value (null for a password), whether it has the focus.</summary>
public sealed record FormFieldShown(int Number, string Name, IReadOnlyList<string> Keys, string Kind, object? Value, bool Focused);

/// <summary>
/// The controls of a form (docs/axKit/SPEC.md AX-8.10): standard controls in Tab order, each with a caption over it
/// and its <c>AccessibleName</c>, an instruction, which NVDA reads itself. The one piece every form of the bundle is
/// made of: a window adds the controls it needs, in order, and shows them with <see cref="AxKitForm.ShowForm"/>,
/// which adds the notice with its explanation, the default button and Cancel. A text box of one line, where Enter
/// presses the form's default button; a text box of several lines, where Enter makes a new line; a list for a choice
/// of one; a check box that Space toggles. Never a list of fields (AX-8.10 rule 13), never radio buttons.
/// </summary>
public sealed class FormControls : FlowLayoutPanel
{
    private readonly List<Control> _wide = [];
    private readonly Dictionary<string, Control> _keys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Control, string> _captions = [];
    private Control? _first;
    private bool _selectFirst;

    public FormControls()
    {
        Dock = DockStyle.Fill;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoScroll = true;
        Padding = new Padding(0, 6, 0, 0);
        TabIndex = 3;
        SizeChanged += (_, _) => LayoutRows();
    }

    /// <summary>
    /// The focus arrived in the form from outside it (AX-7.11, framework FW-3.8): the form says so itself, once for
    /// all its controls, which stay silent as the focus moves between them (<see cref="AxKitForm.SoundOnEntry"/>).
    /// </summary>
    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        (FindForm() as AxKitForm)?.SoundOnEntry(this, AxKit.Core.Events.EventKind.FormEntered);
    }

    /// <summary>
    /// Ctrl+Down and Ctrl+Up: the next and the previous field, from any control of the form (the maintainer,
    /// 2026-11-23: "ctrl+up and down should go to next field in a form"); a box of several lines gives up its
    /// paragraph keys here, which are the same keys (<see cref="BundleKeys.ParagraphDown"/>). Past the last or the
    /// first field, the edge sound and the focus stays.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is BundleKeys.ParagraphDown or BundleKeys.ParagraphUp)
        {
            MoveField(keyData == BundleKeys.ParagraphDown ? 1 : -1);
            return true;
        }

        // AX-8.10 rule 18: in a form the mode key is the form's, from any of its fields.
        if (keyData == BundleKeys.ToggleMode)
        {
            ToggleNavigation();
            return true;
        }

        return (Navigation && NavigationKey(keyData)) || base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>
    /// The form's navigation mode (AX-8.10 rule 18; the maintainer, 2026-11-23: "having user search in a form for
    /// field names would be useful. can we use ctrl+space to toggle nav mode in a form in the same way we have it in
    /// axdown"): nothing is typed; Down and Up go to the next and the previous field; a letter or a digit goes to the
    /// next field whose name starts with what was typed, as type-ahead does in a list (<see cref="TypeAhead"/>); Space
    /// still checks a check box; Enter goes back to editing in the field. Off as the focus leaves the form.
    /// </summary>
    public bool Navigation { get; private set; }

    /// <summary>The keys every form has (rules 15 and 18), in its explanation, named from the key table.</summary>
    public static string KeysLine =>
        $"{BundleKeys.Text(BundleKeys.ParagraphDown)} and {BundleKeys.Text(BundleKeys.ParagraphUp)} move to the next and the previous field. "
        + $"{BundleKeys.Text(BundleKeys.ToggleMode)} turns navigation mode on, where nothing is typed: the start of a field's name, or its number, goes to it, Down and Up move between the fields, and Enter goes back to typing.";

    private string _typed = string.Empty;
    private DateTime _typedAt;

    private void ToggleNavigation()
    {
        Navigation = !Navigation;
        _typed = string.Empty;
        (FindForm() as AxKitForm)?.Announce(TextModes.Toggled(Navigation), true);
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        Navigation = false;
    }

    /// <summary>A key down in navigation mode: true when the form took it, false for a key that keeps its work (Tab, Escape, the arrows across, a key with Ctrl or Alt).</summary>
    private bool NavigationKey(Keys keyData)
    {
        if ((keyData & (Keys.Control | Keys.Alt)) != 0)
        {
            return false;
        }

        var code = keyData & Keys.KeyCode;
        switch (code)
        {
            case Keys.Down or Keys.Up when (keyData & Keys.Shift) == 0:
                MoveField(code == Keys.Down ? 1 : -1);
                return true;
            case Keys.Return:
                ToggleNavigation();
                return true;
            case Keys.Space when Fields().FirstOrDefault(control => control.ContainsFocus) is AxKitCheckBox or FormCheckList or FormRadioList || ActiveButton():
                return false;
            case >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9:
                FindField(code is >= Keys.A and <= Keys.Z ? (char)('a' + (code - Keys.A)) : (char)('0' + (code - Keys.D0)));
                return true;
            case Keys.Tab or Keys.Escape or Keys.Left or Keys.Right or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown or >= Keys.F1 and <= Keys.F24:
                return false;
            default:
                // A key that would type or edit: nothing here, as in an editor's navigation mode.
                EventDoor.Raise(EventKind.NoCommand, EventSound.Unassigned);
                return true;
        }
    }

    /// <summary>Whether a button of the form has the focus (Check all, Uncheck all): Space presses it in navigation mode too.</summary>
    private bool ActiveButton() => Controls.OfType<Button>().Any(button => button.Focused);

    /// <summary>The fields in the form's order: the controls a value is typed or chosen in.</summary>
    private List<Control> Fields() => [.. _wide.Concat(Controls.OfType<AxKitCheckBox>()).OrderBy(Controls.GetChildIndex)];

    /// <summary>The field <paramref name="by"/> places on; the edge sound past the first or the last, the focus staying.</summary>
    private void MoveField(int by)
    {
        var fields = Fields();
        var at = fields.FindIndex(control => control.ContainsFocus);
        var next = at + by;
        if (at >= 0 && next >= 0 && next < fields.Count)
        {
            fields[next].Select();
        }
        else
        {
            EventDoor.Raise(EventKind.Edge);
        }
    }

    /// <summary>The next field whose name starts with what was typed in the last second, the lists' type-ahead over the fields' names (their first key, or their caption).</summary>
    private void FindField(char letter)
    {
        var now = DateTime.UtcNow;
        // Within a second the letters join, as in a list; a digit after letters, or a letter after digits, starts anew.
        var joins = (now - _typedAt).TotalMilliseconds < 1000 && _typed.Length > 0 && char.IsAsciiDigit(_typed[^1]) == char.IsAsciiDigit(letter);
        _typed = joins ? _typed + letter : letter.ToString();
        _typedAt = now;
        // Digits name a field by its number, where the form keys its fields so (the item form).
        if (_typed.All(char.IsAsciiDigit) && Field(_typed) is { } numbered)
        {
            numbered.Select();
            return;
        }

        // A check list's lines are searched as fields of their own, so that a name typed lands on its line.
        var stops = Fields().SelectMany(control => control is FormCheckList checks
            ? checks.ItemNames.Select((name, line) => (Control: control, Name: name, Line: line))
            : [(Control: control, Name: NameOf(control), Line: -1)]).ToList();
        var here = stops.FindIndex(stop => stop.Control.ContainsFocus && (stop.Line < 0 || ((FormCheckList)stop.Control).SelectedIndex == stop.Line));
        var at = TypeAhead.Find([.. stops.Select(stop => stop.Name)], _typed, Math.Max(0, here));
        if (at >= 0)
        {
            stops[at].Control.Select();
            if (stops[at].Control is FormCheckList list)
            {
                list.SelectedIndex = stops[at].Line;
            }
        }
        else
        {
            EventDoor.Raise(EventKind.Edge);
        }
    }

    /// <summary>A field's name: its first key, or its caption; a check list's caption always, since its lines are keyed by their own names.</summary>
    private string NameOf(Control control) =>
        (control is FormCheckList ? null : _keys.FirstOrDefault(pair => pair.Value == control && !pair.Key.All(char.IsAsciiDigit)).Key)
        ?? (_captions.TryGetValue(control, out var caption) ? caption : control.AccessibleName ?? string.Empty);

    /// <summary>Whether a text box here takes Enter as a new line: the form's text then says its keys itself (<see cref="Notice.KeyLine"/>).</summary>
    public bool EnterMakesNewLine { get; private set; }

    /// <summary>A text box of one line, named with an instruction ("Type the name of the server"); Enter in it presses the form's default button.</summary>
    public AxKitTextBox AddText(string name, string text = "")
    {
        var box = new AxKitTextBox { AccessibleName = name, Text = text, Margin = new Padding(0) };
        Add(box, name);
        return box;
    }

    /// <summary>A text box of several lines; Enter in it makes a new line unless it is only read, where Enter presses the default button.</summary>
    public AxKitTextBox AddLines(string name, int rows, string text = "", bool readOnly = false)
    {
        var box = new AxKitTextBox
        {
            AccessibleName = name,
            Multiline = true,
            AcceptsReturn = !readOnly,
            ReadOnly = readOnly,
            WordWrap = true,
            ScrollBars = ScrollBars.Vertical,
            Margin = new Padding(0),
            Text = text.ReplaceLineEndings("\r\n"),
        };
        box.Height = box.Font.Height * rows + 8;
        EnterMakesNewLine |= !readOnly;
        Add(box, name);
        return box;
    }

    /// <summary>A choice of one as a list, the bundle's, with <paramref name="selected"/> selected: Up and Down move, NVDA reads the selection itself.</summary>
    public AxKitList AddList(string name, IReadOnlyList<string> items, int selected)
    {
        var list = new AxKitList { AccessibleName = name, IntegralHeight = false, Margin = new Padding(0) };
        list.Items.AddRange([.. items]);
        if (items.Count > 0)
        {
            list.SelectedIndex = Math.Clamp(selected, 0, items.Count - 1);
        }

        list.Height = list.ItemHeight * Math.Clamp(items.Count, 1, 8) + 6;
        Add(list, name);
        return list;
    }

    /// <summary>A group of checks of one kind as one list (rule 19): one Tab stop, Space checks a line, Ctrl+A all.</summary>
    public FormCheckList AddCheckList(string name, IReadOnlyList<(string Name, bool Checked)> items)
    {
        var list = new FormCheckList(items) { AccessibleName = name, Margin = new Padding(0) };
        list.Height = list.ItemHeight * Math.Clamp(items.Count, 1, 8) + 6;
        Add(list, name);
        return list;
    }

    /// <summary>A choice of one as a list where Space chooses (rule 20): one Tab stop, the arrows move without choosing.</summary>
    public FormRadioList AddRadioList(string name, IReadOnlyList<string> items, int chosen)
    {
        var list = new FormRadioList(items, chosen) { AccessibleName = name, Margin = new Padding(0) };
        list.Height = list.ItemHeight * Math.Clamp(items.Count, 1, 8) + 6;
        Add(list, name);
        return list;
    }

    /// <summary>A button inside the form (Check all before a check list): Enter or Space presses it, as any button.</summary>
    public Button AddButton(string text, Action pressed)
    {
        var button = new Button { Text = text, AccessibleName = text.Replace("&", string.Empty), AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        button.Click += (_, _) => pressed();
        Controls.Add(button);
        _first ??= button;
        return button;
    }

    /// <summary>A check box, the bundle's, not checked; its text is its name.</summary>
    public AxKitCheckBox AddCheck(string text)
    {
        var box = new AxKitCheckBox { Text = text, AccessibleName = text, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        Controls.Add(box);
        _first ??= box;
        return box;
    }

    /// <summary>The focus in the first control, where a form starts (AX-8.10 rule 1), its text selected when the form was started so.</summary>
    public void FocusFirst()
    {
        _first?.Select();
        if (_selectFirst && _first is TextBoxBase box)
        {
            box.SelectAll();
        }
    }

    /// <summary>
    /// The control the form starts in, when it is not the first: a form opened on one of its fields (the item form
    /// from a field of the detail pane, docs/axTask/forms-framework.md AF-14.2). A control that is not of this form
    /// changes nothing.
    /// </summary>
    public void StartIn(Control control, bool selectContents = false)
    {
        if (Controls.Contains(control))
        {
            _first = control;
            _selectFirst = selectContents;
        }
    }

    /// <summary>
    /// A name the control is reached by, besides its place (the maintainer, 2026-11-23: "it should be easy to mark
    /// each field in a form with an uuid or number or label that can be used to select it and select its contents or
    /// place focus there"): the item form keys each field by its name and its number from 1. Keys are compared
    /// without case; a key given twice names the first control.
    /// </summary>
    public void Key(string key, Control control)
    {
        if (Controls.Contains(control))
        {
            _keys.TryAdd(key, control);
        }
    }

    /// <summary>The control a key names, or null.</summary>
    public Control? Field(string key) => _keys.GetValueOrDefault(key);

    /// <summary>The focus put in the control a key names, its text selected when asked: false for a key that names none.</summary>
    public bool Focus(string key, bool selectContents = false)
    {
        if (Field(key) is not { } control)
        {
            return false;
        }

        control.Select();
        if (selectContents && control is TextBoxBase box)
        {
            box.SelectAll();
        }

        return true;
    }

    /// <summary>
    /// The form's fields as the AI tools read them (docs/axKit/mcp.md MC-7.10): in the form's order, each with its
    /// number from 1, its name, its keys, its kind, its value and whether it has the focus. A field that hides what is
    /// typed gives no value; a check list gives its lines as NVDA reads them.
    /// </summary>
    public IReadOnlyList<FormFieldShown> Shown() =>
    [
        .. Fields().Select((control, at) => new FormFieldShown(NumberOf(control, at), NameOf(control),
            [.. _keys.Where(pair => pair.Value == control).Select(pair => pair.Key)], KindOf(control), ValueOf(control), control.ContainsFocus)),
    ];

    /// <summary>A field's number: the number it is keyed by (the item form numbers its fields itself), else its place from 1.</summary>
    private int NumberOf(Control control, int at) =>
        _keys.Where(pair => pair.Value == control && pair.Key.All(char.IsAsciiDigit)).Select(pair => int.Parse(pair.Key, System.Globalization.CultureInfo.InvariantCulture)).DefaultIfEmpty(at + 1).Min();

    private static string KindOf(Control control) => control switch
    {
        FormCheckList => "check list",
        FormRadioList => "choice of one",
        AxKitCheckBox => "check box",
        ListBox => "list",
        TextBoxBase { Multiline: true } => "text of several lines",
        _ => "text",
    };

    private static object? ValueOf(Control control) => control switch
    {
        TextBox { UseSystemPasswordChar: true } or TextBox { PasswordChar: not '\0' } => null,
        TextBoxBase box => box.Text,
        AxKitCheckBox box => box.Checked ? "checked" : "not checked",
        FormCheckList or FormRadioList => ((ListBox)control).Items.Cast<object>().Select(item => item.ToString()).ToArray(),
        ListBox list => list.SelectedItem?.ToString(),
        _ => null,
    };

    /// <summary>
    /// The focus put in the field <paramref name="wanted"/> names (MC-7.10): its key, its number from 1, its name, or a
    /// line of a check list by its name, without regard to case; its text selected when <paramref name="selectContents"/>.
    /// The field's name, or null when nothing here is so named.
    /// </summary>
    public string? Reach(string wanted, bool selectContents)
    {
        if (Locate(wanted) is not { } found)
        {
            return null;
        }

        found.Control.Select();
        if (found.Line >= 0 && found.Control is FormCheckList list)
        {
            list.SelectedIndex = found.Line;
        }
        else if (selectContents && found.Control is TextBoxBase box)
        {
            box.SelectAll();
        }

        return found.Name;
    }

    /// <summary>The name of the field <paramref name="wanted"/> names, as <see cref="Reach"/> finds it, or null; nothing moves.</summary>
    public string? NameOfField(string wanted) => Locate(wanted)?.Name;

    private (Control Control, int Line, string Name)? Locate(string wanted)
    {
        var fields = Fields();
        var text = wanted.Trim();
        // A line of a check list by its name first: the item form keys each line by its name, and the key gives the list.
        foreach (var checks in fields.OfType<FormCheckList>())
        {
            var line = checks.ItemNames.ToList().FindIndex(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase));
            if (line >= 0)
            {
                return (checks, line, checks.ItemNames[line]);
            }
        }

        // Then a key, then a place from 1 where the form keys no numbers itself, then a name.
        var control = Field(text)
            ?? (int.TryParse(text, out var number) && number >= 1 && number <= fields.Count && !_keys.Keys.Any(key => key.All(char.IsAsciiDigit)) ? fields[number - 1] : null)
            ?? fields.FirstOrDefault(field => string.Equals(NameOf(field), text, StringComparison.OrdinalIgnoreCase));
        return control is null ? null : (control, -1, NameOf(control));
    }

    /// <summary>The form started in the control a key names: <see cref="StartIn"/> by key.</summary>
    public void StartIn(string key, bool selectContents = false)
    {
        if (Field(key) is { } control)
        {
            StartIn(control, selectContents);
        }
    }

    private void Add(Control control, string caption)
    {
        Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(0, 6, 0, 2) });
        _captions[control] = caption;
        Controls.Add(control);
        _wide.Add(control);
        _first ??= control;
        LayoutRows();
    }

    private void LayoutRows()
    {
        var width = Math.Max(100, ClientSize.Width - Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
        foreach (var row in _wide)
        {
            row.Width = width;
        }
    }
}
