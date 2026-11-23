using AxKit.Core.Events;

namespace AxKit.Forms;

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
            var fields = _wide.Concat(Controls.OfType<AxKitCheckBox>()).OrderBy(Controls.GetChildIndex).ToList();
            var at = fields.FindIndex(control => control.ContainsFocus);
            var next = at + (keyData == BundleKeys.ParagraphDown ? 1 : -1);
            if (at >= 0 && next >= 0 && next < fields.Count)
            {
                fields[next].Select();
            }
            else
            {
                EventDoor.Raise(EventKind.Edge);
            }

            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

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

    /// <summary>The keys in the form's order, the first key of each control: what a list of the form's fields shows.</summary>
    public IReadOnlyList<string> FieldKeys => [.. _keys.GroupBy(pair => pair.Value).Select(group => group.First().Key)];

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
