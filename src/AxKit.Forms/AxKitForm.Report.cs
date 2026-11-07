using System.Diagnostics;
using AxKit.Core;
using AxKit.Core.Mcp;
using AxKit.Core.Reports;

namespace AxKit.Forms;

public abstract partial class AxKitForm
{
    // ---- reports: a bug, a feature request or feedback from any window (docs/axkit/reports.md) ----

    private const string ReportTitle = "Report to axKit";

    private const string ReportText =
        "A report goes to axKit's maintainer as a public issue on GitHub, posted by axKit itself with nothing that names you. "
        + "Type your words and press Enter. Tab reviews the kind and what the window adds: the app, the last actions, where the focus was, the log lines, the title.";

    /// <summary>
    /// F4 and axKit → Report a bug or an idea… (RP-2, RP-3): the notice "Report to axKit" with the window's context
    /// filled in, redacted, and the words field focused. Enter saves the report into the Reports list and sends it
    /// (RP-5, RP-6); Escape keeps a draft when words were typed (RP-D6).
    /// </summary>
    protected internal void ShowReport()
    {
        if (NoticeOpen)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var controls = new ReportControls(
            Redact(AppLine()),
            Redact(FocusDescription()),
            Recent.Lines(now).Select(Redact).ToList(),
            LogTail().Select(Redact).ToList(),
            said => Announce(said, true),
            Overlay.PressDefault);
        ShowNotice(new Notice(ReportTitle, ReportText,
        [
            new OverlayChoice("&Send", () => SaveReport(controls), IsDefault: true),
            new OverlayChoice("Cancel", () => KeepDraft(controls), IsCancel: true),
        ])
        {
            Custom = controls,
            Accepts = controls.Ready,
        });
        BeginInvoke(controls.FocusWords);
    }

    /// <summary>axKit → Your reports (RP-2.2): the Reports list in axTask, made when it does not exist yet.</summary>
    protected internal void OpenReports()
    {
        try
        {
            ReportFiles.EnsureList(AppPaths.ReportsFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Your reports", "The reports folder could not be made: " + ex.Message);
            return;
        }

        AxKitMenu.Start(Dispatch.AxTask, AppPaths.ReportsFolder);
    }

    /// <summary>Enter or Send: the report written into the Reports list first (RP-3.2); the sending follows (RP-6).</summary>
    private void SaveReport(ReportControls controls)
    {
        var report = controls.ToReport();
        try
        {
            ReportFiles.Save(report, ReportStatus.NotSent(null), AppPaths.ReportsFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Report not saved", "The report could not be written: " + ex.Message);
            return;
        }

        Announce("Saved; not sent yet. It goes with the next report", false);
    }

    /// <summary>Escape or Cancel: words typed are kept as a draft in the Reports list (RP-D6); none typed cancels.</summary>
    private void KeepDraft(ReportControls controls)
    {
        if (controls.Words.Trim().Length == 0)
        {
            Announce("Cancelled", false);
            return;
        }

        try
        {
            ReportFiles.Save(controls.ToReport(), ReportStatus.Draft, AppPaths.ReportsFolder);
            Announce("Kept as a draft in Your reports", false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Draft not kept", "The draft could not be written: " + ex.Message);
        }
    }

    /// <summary>"axFile 5.1.0, Windows 11 build 26200, NVDA 2025.3; window: notes - axFile" (RP-3 item 3).</summary>
    private string AppLine()
    {
        var build = Environment.OSVersion.Version.Build;
        var windows = build >= 22000 ? "Windows 11" : "Windows 10";
        return $"{AppName} {BundleInfo.Version}, {windows} build {build}, {NvdaVersion()}; window: {Text}";
    }

    /// <summary>The focused control's name and place, never its text (RP-3 item 5, RP-4.2).</summary>
    private string FocusDescription()
    {
        var control = FocusedControl();
        if (control is null)
        {
            return "the window itself";
        }

        var name = control.AccessibleName is { Length: > 0 } accessible ? accessible : control.Name is { Length: > 0 } own ? own : control.GetType().Name;
        var secret = AppVerb is { } verb && McpTools.NeverRead.Contains(verb) || WindowReading.IsSecret(control);
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

    private static IReadOnlyList<string> LogTail()
    {
        try
        {
            return Log.Tail(20).ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>"NVDA 2025.3" from its running process, "NVDA running" when the version cannot be read, "NVDA not running".</summary>
    private static string NvdaVersion()
    {
        try
        {
            using var nvda = Process.GetProcessesByName("nvda").FirstOrDefault();
            if (nvda is null)
            {
                return "NVDA not running";
            }

            var version = nvda.MainModule?.FileVersionInfo.ProductVersion;
            return string.IsNullOrWhiteSpace(version) ? "NVDA running" : "NVDA " + version.Trim();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            return "NVDA running";
        }
    }

    private static string Redact(string text) => Redaction.Redact(text);
}
