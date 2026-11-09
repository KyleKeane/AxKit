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

    /// <summary>The agreement to the reports is recorded under this name, not the app's: every window sends the same way (RP-6.6).</summary>
    private const string ReportsConsentApp = "axKit";

    private const string ReportsConsentBox = "I understand that my report becomes a public issue on GitHub and that GitHub's terms apply to what I write";

    /// <summary>
    /// F4 and axKit → Send a report… (RP-2, RP-3): the notice "Report to axKit" with the window's context filled in,
    /// redacted, and the words field focused. Enter saves the report into the Reports list and sends it (RP-5, RP-6);
    /// Escape keeps a draft when words were typed (RP-D6).
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

    /// <summary>
    /// axKit → Your reports (RP-2.2, RP-6.7): the unsent reports sent first when there are any and the user has agreed,
    /// then the Reports list in axTask, made when it does not exist yet.
    /// </summary>
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

        if (ReportFiles.Unsent(AppPaths.ReportsFolder).Count > 0 && ReporterToken.Token is not null && ReportsAgreed())
        {
            Do(async () =>
            {
                var outcome = await SendUnsentAsync();
                Announce(outcome.Sent switch
                {
                    0 => $"Not sent yet, {outcome.Reason}",
                    1 => "1 report sent",
                    _ => $"{outcome.Sent} reports sent",
                }, false);
                AxKitMenu.Start(Dispatch.AxTask, AppPaths.ReportsFolder);
            });
            return;
        }

        AxKitMenu.Start(Dispatch.AxTask, AppPaths.ReportsFolder);
    }

    /// <summary>Enter or Send: the report written into the Reports list first (RP-3.2), then sent with every unsent one (RP-6).</summary>
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

        SendReports(report);
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

    /// <summary>
    /// The sending (RP-6): a build without the token says so and offers the report for copying (RP-6.5); the first time,
    /// the consent notice (RP-6.6); then the sender runs off the UI thread and the outcome is said or shown.
    /// </summary>
    private void SendReports(Report saved)
    {
        if (ReporterToken.Token is null)
        {
            ShowCannotSend("This build cannot send reports", "This build of axKit was made without the sending token, so your report stays in Your reports. Copy it to send it another way, or open GitHub's own form, which needs a GitHub account of yours.", saved);
            return;
        }

        if (!ReportsAgreed())
        {
            ShowNotice(new Notice("Before the first report goes", ConsentTarget.GitHubReports.Work + "\n"
                + $"GitHub's terms and privacy statement apply to it, between you and GitHub: {ConsentTarget.GitHubReports.TermsUrl}\n"
                + "Your report is saved in Your reports whatever you choose here; Not now leaves it there, unsent, and asks again with the next one.",
            [
                new OverlayChoice("&Send", () =>
                {
                    Consent.Record(AppPaths.ConsentFile, ReportsConsentApp, ConsentTarget.GitHubReports.Program, BundleInfo.Version, DateTime.Now);
                    SendSaved(saved);
                }, IsDefault: true),
                new OverlayChoice("&Not now", () => Announce("Saved; not sent yet. It goes with the next report once you agree", false), IsCancel: true),
            ])
            {
                Consent = ReportsConsentBox,
            });
            return;
        }

        SendSaved(saved);
    }

    private bool ReportsAgreed() => Consent.Given(AppPaths.ConsentFile, ReportsConsentApp, ConsentTarget.GitHubReports.Program);

    private void SendSaved(Report saved) => Do(async () =>
    {
        var outcome = await SendUnsentAsync();
        if (outcome.Sent > 0 && outcome.Last is { } last)
        {
            Announce(outcome.Sent == 1 ? $"Sent as issue {last.Number}" : $"Sent as issue {last.Number}, with {outcome.Sent - 1} earlier", false);
            return;
        }

        switch (outcome.Reason)
        {
            case ReportSender.NoTokenReason:
                ShowCannotSend("This build cannot send reports", "This build of axKit was made without the sending token, so your report stays in Your reports. Copy it to send it another way, or open GitHub's own form, which needs a GitHub account of yours.", saved);
                break;
            case ReportSender.BlockedReason:
                ShowNotice(Notice.Plain("Report not sent", "Reports from this install or this version of axKit are blocked; the reports repository on GitHub says why. The report stays in Your reports."));
                break;
            case { } reason when reason.StartsWith("this version can no longer", StringComparison.Ordinal):
                ShowCannotSend("This version can no longer send reports", "GitHub no longer takes reports from this version of axKit. Update axKit from the Help menu and send it again from Your reports, or copy the report, or open GitHub's own form, which needs a GitHub account of yours.", saved);
                break;
            case null:
                Announce("Saved", false);
                break;
            default:
                Announce($"Saved; not sent yet, {outcome.Reason}. It goes with the next report", false);
                break;
        }
    });

    private static Task<SendOutcome> SendUnsentAsync() =>
        ReportSender.SendUnsentAsync(AppPaths.ReportsFolder, AppPaths.ReportsFile, BundleInfo.Version, ReporterToken.Token, AxKitHttp.Client, null, CancellationToken.None);

    /// <summary>The notice of RP-6.5: why the report cannot go, with Copy report and Open GitHub's form; the report stays saved.</summary>
    private void ShowCannotSend(string title, string text, Report report)
    {
        var body = ReportBody.Markdown(report, BundleInfo.Version, ReportSender.ReporterId(AppPaths.ReportsFile));
        ShowNotice(new Notice(title, text,
        [
            new OverlayChoice("&Copy report", () => CopyText(body.Replace("\n", "\r\n"), "Copied"), StaysOpen: true),
            new OverlayChoice("Open GitHub's &form", () => OpenUrl(GitHubIssues.NewIssueFormUrl(ReportSender.Repository, report.Title, body, ReportKinds.Label(report.Kind)))),
            OverlayChoice.Close,
        ]));
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

    /// <summary>
    /// "NVDA 2025.3" from its running process, or, since NVDA runs with rights that hide its module from the window,
    /// from the installed nvda.exe under Program Files; "NVDA running" when neither tells the version; "NVDA not running".
    /// </summary>
    private static string NvdaVersion()
    {
        bool running;
        string? version = null;
        try
        {
            using var nvda = Process.GetProcessesByName("nvda").FirstOrDefault();
            running = nvda is not null;
            version = nvda?.MainModule?.FileVersionInfo.ProductVersion;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            running = true;
        }

        if (!running)
        {
            return "NVDA not running";
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            foreach (var folder in new[] { Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.ProgramFiles })
            {
                var exe = Path.Combine(Environment.GetFolderPath(folder), "NVDA", "nvda.exe");
                if (File.Exists(exe))
                {
                    try
                    {
                        version = FileVersionInfo.GetVersionInfo(exe).ProductVersion;
                    }
                    catch (FileNotFoundException)
                    {
                        version = null;
                    }

                    break;
                }
            }
        }

        return string.IsNullOrWhiteSpace(version) ? "NVDA running" : "NVDA " + version.Trim();
    }

    private static string Redact(string text) => Redaction.Redact(text);
}
