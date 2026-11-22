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
    private Control? _first;

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

    /// <summary>The focus in the first control, where a form starts (AX-8.10 rule 1).</summary>
    public void FocusFirst() => _first?.Select();

    /// <summary>
    /// The control the form starts in, when it is not the first: a form opened on one of its fields (axTask's item
    /// form from a field of the detail pane, docs/axTask/forms-framework.md AF-14.2). A control that is not of this
    /// form changes nothing.
    /// </summary>
    public void StartIn(Control control)
    {
        if (Controls.Contains(control))
        {
            _first = control;
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
