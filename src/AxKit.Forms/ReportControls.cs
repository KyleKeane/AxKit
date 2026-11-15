using AxKit.Core.Reports;

namespace AxKit.Forms;

/// <summary>
/// The controls of the form "Report to axKit" (docs/axKit/reports.md RP-3) on the bundle's <see cref="FormControls"/>
/// (docs/axKit/SPEC.md AX-8.10): the user's words first, then the kind, the check box for more detail, and one text
/// that is only read, what is sent with the report: the app and version, the last actions, where the focus was and
/// the log lines, in the plain form or, once the box is checked, the fuller one (RP-4.2a). Enter in the words makes a
/// new line and never sends; Ctrl+Enter, the bundle's main action, presses Send from any control.
/// </summary>
internal sealed class ReportControls
{
    private readonly AxKitTextBox _words;
    private readonly AxKitList _kind;
    private readonly AxKitCheckBox _detail;
    private readonly AxKitTextBox _sent;

    /// <summary>What the report carries of the window: the plain form until the user checks the box, the fuller one while it is checked.</summary>
    private ReportContext _context;

    /// <param name="plain">What the window adds unless the user asks for more (RP-4.2a): nothing that can name their files.</param>
    /// <param name="full">The fuller form, redacted, shown and sent when the user checks the detail box.</param>
    public ReportControls(ReportContext plain, ReportContext full)
    {
        _context = plain;
        // Enter makes a new line, as in every text box of several lines (the user's decision, 2026-11-14: a stray
        // Enter had sent an unfinished report that morning).
        _words = Panel.AddLines("Your report", rows: 5);
        // NVDA reads the selected kind itself as the list moves; the app adds nothing (AX-7).
        _kind = Panel.AddList("Kind", ReportKinds.Names, 0);
        _detail = Panel.AddCheck(ReportPrivacy.DetailBox);
        // What is sent is the window's own words in one form or the other; a change of the box fills it afresh.
        _detail.CheckedChanged += (_, _) => Fill(_detail.Checked ? full : plain);
        // Only read: the report carries the context as the window gathered it, plain or fuller, never an edited copy.
        _sent = Panel.AddLines("What is sent with your report", rows: 8, readOnly: true);
        _sent.ReadOnly = true;
        Fill(plain);
    }

    /// <summary>The form's controls, for <see cref="AxKitForm.ShowForm"/>.</summary>
    public FormControls Panel { get; } = new();

    private void Fill(ReportContext context)
    {
        _context = context;
        _sent.Text = string.Join("\r\n", ReportPrivacy.Shown(context));
    }

    public string Words => _words.Text;

    public ReportKind Kind => ReportKinds.All[Math.Clamp(_kind.SelectedIndex, 0, ReportKinds.All.Count - 1)];

    /// <summary>What Send still needs: words, asked for with the focus back in their field; null when the report may go.</summary>
    public (Control Field, string Said)? Missing() => _words.Text.Trim().Length > 0 ? null : (_words, "Type a few words first");

    /// <summary>The report as the form holds it, created now: the user's words and kind, the title from the first line of the words (RP-3), and the context the box shows.</summary>
    public Report ToReport() => new(Report.TitleFrom(Words), Kind, Words.Trim(), _context.App, _context.Where.Trim(), _context.Actions, _context.Log, DateTime.UtcNow);
}
