using AxKit.Core.Mcp;

namespace AxKit.Forms;

public abstract partial class AxKitForm
{
    // ---- reports: a bug, a feature request or feedback from any window (docs/axKit/reports.md): the window's two
    // members and what it says of its own focus; the notice, the saving and the sending are ReportFlow's ----

    /// <summary>Made in the constructor (AxKitForm.cs).</summary>
    private readonly ReportFlow _reports;

    /// <summary>
    /// F4 and axKit → Send a report… (RP-2, RP-3): the notice "Report to axKit" with the window's context filled in,
    /// redacted, and the words field focused. Enter saves the report into the Reports list and sends it (RP-5, RP-6);
    /// Escape keeps a draft when words were typed (RP-D6).
    /// </summary>
    protected internal void ShowReport() => _reports.ShowReport();

    /// <summary>
    /// axKit → Your reports (RP-2.2, RP-6.7): the unsent reports sent first when there are any and the user has agreed,
    /// then the Reports list in axTask, made when it does not exist yet.
    /// </summary>
    protected internal void OpenReports() => _reports.OpenReports();

    /// <summary>The focused control's name and place, never its text (RP-3 item 5, RP-4.2); a report and a question to Claude about this window both carry it.</summary>
    internal string FocusDescription()
    {
        var control = FocusedControl();
        if (control is null)
        {
            return "the window itself";
        }

        var name = control.AccessibleName is { Length: > 0 } accessible ? accessible : control.Name is { Length: > 0 } own ? own : control.GetType().Name;
        var secret = McpTools.NeverRead.Contains(AppVerb) || WindowReading.IsSecret(control);
        switch (control)
        {
            case TextBoxBase box:
                {
                    var lines = Math.Max(1, box.Lines.Length);
                    var line = WindowReading.LogicalLine(box) + 1;
                    var kept = box.ReadOnly ? string.Empty : " (the text is not included)";
                    return $"{name}, line {line} of {lines}{kept}";
                }

            case ListBox list when list.Items.Count == 0:
                return $"{name}, empty";
            case ListBox list:
                {
                    var place = $"{name}, item {list.SelectedIndex + 1} of {list.Items.Count}";
                    return secret || list.SelectedIndex < 0 ? place : $"{place}: {list.SelectedItem}";
                }

            default:
                return name;
        }
    }
}
