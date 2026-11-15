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
    /// redacted, as standard form controls with the words focused (bundle AX-8.10). Send, or Ctrl+Enter in any control,
    /// saves the report into the Reports list and sends it (RP-5, RP-6); Escape cancels the form and keeps nothing (RP-D13).
    /// </summary>
    protected internal void ShowReport() => _reports.ShowReport();

    /// <summary>
    /// axKit → Your reports (RP-2.2, RP-6.7): the unsent reports sent first when there are any and the user has agreed,
    /// then the Reports list in axTask, made when it does not exist yet.
    /// </summary>
    protected internal void OpenReports() => _reports.OpenReports();

    /// <summary>
    /// The focused control's name and place, never an edit field's text (RP-3 item 5, RP-4.2); a question to Claude
    /// about this window carries it, and a report's fuller form. <paramref name="named"/> false is a report's plain
    /// form (RP-4.2a): the kind of control and the place alone, since a control's name can hold a list's or a folder's
    /// ("Details of Home") and a list's item is a file's or a task's.
    /// </summary>
    internal string FocusDescription(bool named = true)
    {
        var control = FocusedControl();
        if (control is null)
        {
            return "the window itself";
        }

        if (!named)
        {
            return control switch
            {
                TextBoxBase box => $"a text field, line {WindowReading.LogicalLine(box) + 1} of {Math.Max(1, box.Lines.Length)}",
                ListBox { Items.Count: 0 } => "a list, empty",
                ListBox list => $"a list, item {list.SelectedIndex + 1} of {list.Items.Count}",
                ButtonBase => "a button",
                _ => "a control",
            };
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
