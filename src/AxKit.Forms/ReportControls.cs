using AxKit.Core.Reports;

namespace AxKit.Forms;

/// <summary>
/// The fields of the notice "Report to axKit" (docs/axKit/reports.md RP-3), the window's own control under the notice's
/// text: the user's words first and focused, then the kind, the app and version, the last actions, where the focus
/// was, the log lines and the title, each named as an instruction, the window's context filled in and editable. The
/// notice's buttons, Send and Cancel, follow in the Tab order.
/// </summary>
public sealed class ReportControls : FlowLayoutPanel
{
    private readonly AxKitTextBox _words = new();
    private readonly AxKitList _kind = new();
    private readonly AxKitTextBox _app = new();
    private readonly AxKitTextBox _actions = new();
    private readonly AxKitTextBox _where = new();
    private readonly AxKitTextBox _log = new();
    private readonly AxKitTextBox _title = new();
    private readonly List<Control> _rows = [];
    private readonly Action<string> _say;
    private bool _titleTyped;

    /// <param name="app">The app and version line (RP-3 item 3), read-only.</param>
    /// <param name="where">Where the focus was (item 5), redacted.</param>
    /// <param name="actions">The last actions (item 4), redacted.</param>
    /// <param name="log">The log's last lines (item 6), redacted.</param>
    /// <param name="say">The window's announcer, interrupting.</param>
    /// <param name="accept">Presses the notice's default button (Enter in the words field).</param>
    public ReportControls(string app, string where, IReadOnlyList<string> actions, IReadOnlyList<string> log, Action<string> say, Action accept)
    {
        _say = say;
        Dock = DockStyle.Fill;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoScroll = true;
        Padding = new Padding(0, 6, 0, 0);
        TabIndex = 3;
        SizeChanged += (_, _) => LayoutRows();

        MultiLine(_words, "Type your report; Enter sends it, Shift+Enter starts a new line", rows: 4, acceptsReturn: false);
        _words.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.Handled = e.SuppressKeyPress = true;
            if (e.Modifiers == Keys.Shift)
            {
                _words.SelectedText = "\r\n";
            }
            else if (e.Modifiers == Keys.None)
            {
                accept();
            }
        };
        _words.Leave += (_, _) => FillTitle();

        _kind.AccessibleName = "Kind";
        foreach (var name in ReportKinds.Names)
        {
            _kind.Items.Add(name);
        }

        // NVDA reads the selected kind itself as the list moves; the app adds nothing (AX-7).
        _kind.SelectedIndex = 0;
        _kind.IntegralHeight = false;
        _kind.Height = _kind.ItemHeight * ReportKinds.Names.Count + 6;

        OneLine(_app, "App and version", app);
        _app.ReadOnly = true;
        MultiLine(_actions, "What you were doing: the last actions", rows: 5, acceptsReturn: true);
        _actions.Text = string.Join("\r\n", actions);
        OneLine(_where, "Where the focus was", where);
        MultiLine(_log, "Log lines, the last twenty", rows: 4, acceptsReturn: true);
        _log.Text = string.Join("\r\n", log);
        OneLine(_title, "Title", string.Empty);
        _title.TextChanged += (_, _) => _titleTyped = _title.Focused;

        Add(_words, "Your report");
        Add(_kind, "Kind");
        Add(_app, "App and version");
        Add(_actions, "What you were doing: the last actions");
        Add(_where, "Where the focus was");
        Add(_log, "Log lines, the last twenty");
        Add(_title, "Title");
        LayoutRows();
    }

    public string Words => _words.Text;

    public ReportKind Kind => ReportKinds.All[Math.Clamp(_kind.SelectedIndex, 0, ReportKinds.All.Count - 1)];

    public string App => _app.Text;

    public string Where => _where.Text;

    public IReadOnlyList<string> Actions => Lines(_actions.Text);

    public IReadOnlyList<string> Log => Lines(_log.Text);

    /// <summary>The title: typed, or the first line of the words (RP-3 item 7).</summary>
    public string Title
    {
        get
        {
            FillTitle();
            return _title.Text.Trim().Length > 0 ? _title.Text.Trim() : Report.TitleFrom(_words.Text);
        }
    }

    /// <summary>Whether the notice's default button may act (<see cref="Notice.Accepts"/>): not without words, which is said with the focus back in the field.</summary>
    public bool Ready()
    {
        if (_words.Text.Trim().Length > 0)
        {
            return true;
        }

        Sounds.Edge();
        _say("Type a few words first");
        _words.Select();
        return false;
    }

    public void FocusWords() => _words.Select();

    /// <summary>The report as the fields hold it, created now.</summary>
    public Report ToReport() => new(Title, Kind, Words.Trim(), App, Where.Trim(), Actions, Log, DateTime.UtcNow);

    private void FillTitle()
    {
        if (!_titleTyped && _words.Text.Trim().Length > 0)
        {
            _title.Text = Report.TitleFrom(_words.Text);
        }
    }

    private void Add(Control control, string label)
    {
        var caption = new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 0, 2) };
        Controls.Add(caption);
        Controls.Add(control);
        _rows.Add(control);
    }

    private static void OneLine(TextBox box, string name, string text)
    {
        box.AccessibleName = name;
        box.Text = text;
        box.Margin = new Padding(0);
    }

    private static void MultiLine(TextBox box, string name, int rows, bool acceptsReturn)
    {
        box.AccessibleName = name;
        box.Multiline = true;
        box.AcceptsReturn = acceptsReturn;
        box.WordWrap = true;
        box.ScrollBars = ScrollBars.Vertical;
        box.Margin = new Padding(0);
        box.Height = box.Font.Height * rows + 8;
    }

    private void LayoutRows()
    {
        var width = Math.Max(100, ClientSize.Width - Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
        foreach (var row in _rows)
        {
            row.Width = width;
        }
    }

    private static IReadOnlyList<string> Lines(string text) =>
        text.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
