using System.Diagnostics;
using AxKit.Core;
using AxKit.Core.Reports;

namespace AxKit.Forms;

/// <summary>
/// Reports: a bug, a feature request or feedback from any window (docs/axKit/reports.md). A window holds one of these
/// (<see cref="AxKitForm.ShowReport"/> and <see cref="AxKitForm.OpenReports"/> call it). It keeps nothing between two
/// reports: what it knows is in the Reports list on the disk; it asks of the window its notices, its announcements, its
/// last actions and where the focus is (docs/axKit/repairs.json R-15: out of the window base, 2026-11-14).
/// </summary>
internal sealed class ReportFlow
{
    private const string ReportTitle = "Report to axKit";

    private static string ReportText =>
        "A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unless you write it. "
        + $"Type your words; Enter makes a new line. Tab goes on to the kind, the box for more detail and what is sent with your report. {BundleKeys.Text(BundleKeys.MainAction)} or the Send button sends. Escape cancels.";

    /// <summary>The agreement to the reports is recorded under this name, not the app's: every window sends the same way (RP-6.6).</summary>
    private const string ReportsConsentApp = "axKit";

    private const string ReportsConsentBox = "I understand that my report is sent encrypted to a private repository on GitHub, that only axKit's maintainer can read it, and that GitHub's terms apply to what GitHub stores";

    private const string CopyInstead = "Copy report puts the report on your clipboard, not encrypted, to send another way.";

    private readonly AxKitForm _form;

    public ReportFlow(AxKitForm form)
    {
        _form = form;
    }

    /// <summary>
    /// F4 and axKit → Send a report… (RP-2, RP-3): the notice "Report to axKit" with the window's context filled in,
    /// redacted, as standard form controls (bundle AX-8.10), the focus in the words, ready to type, and the notice's
    /// text, the explanation and disclaimer, said once as it opens. Send, or Ctrl+Enter in any control, saves the
    /// report into the Reports list and sends it (RP-5, RP-6); Enter in the words is a new line; Escape cancels the
    /// form whole and keeps nothing (RP-D13, which reversed RP-D6 on 2026-11-14).
    /// </summary>
    public void ShowReport() => Show(null, null, null, byUser: true);

    /// <summary>
    /// The report form with an AI tool's draft (docs/axKit/mcp.md MC-9.11): the words filled, the kind chosen, and the
    /// form's text starting with <see cref="AiCaution.Drafted"/>, said as the form opens; the rest as F4's. Only the
    /// user's Send sends. False, with nothing shown, while a notice is open. The form holds its keys for a moment as
    /// every notice does that the user did not open (bundle AX-6.4; the fourth review of 2026-11-27, M1: until then
    /// Ctrl+Enter, the form's Send from any field, sent an AI tool's draft before a word of it was heard).
    /// </summary>
    public bool ShowReport(string? draft, ReportKind? kind, string? drafter) => Show(draft, kind, drafter, byUser: false);

    /// <summary>The report form, F4's (<paramref name="byUser"/>) or an AI tool's draft.</summary>
    private bool Show(string? draft, ReportKind? kind, string? drafter, bool byUser)
    {
        if (_form.NoticeOpen)
        {
            return false;
        }

        // RP-4.2a: the plain form holds nothing that can name the user's files (no title, no names, no spoken lines,
        // of the log only today's errors, masked); the fuller form is the user's choice in the form's check box.
        var now = DateTime.UtcNow;
        var build = Environment.OSVersion.Version.Build;
        var windows = $"{(build >= 22000 ? "Windows 11" : "Windows 10")} build {build}";
        var nvda = NvdaVersion();
        var plain = new ReportContext(
            ReportPrivacy.AppLine(_form.AppName, BundleInfo.Version, windows, nvda),
            _form.FocusDescription(named: false),
            ReportPrivacy.Commands(_form.Recent.Entries(), now),
            ReportPrivacy.Errors(LogTail(ReportPrivacy.LogRead), DateTime.Now));
        var full = new ReportContext(
            Redact(ReportPrivacy.AppLine(_form.AppName, BundleInfo.Version, windows, nvda, _form.Text)),
            Redact(_form.FocusDescription()),
            _form.Recent.Lines(now).Select(Redact).ToList(),
            LogTail(ReportPrivacy.LogLines).Select(Redact).ToList());
        // The bundle's form (AX-8.10): the focus in the words, ready to type, the text, the explanation and
        // disclaimer, said once as it opens (rule 10), Send the default button, Ctrl+Enter from any control.
        var controls = new ReportControls(plain, full, draft, kind);
        var text = drafter is null ? ReportText : AiCaution.Drafted(drafter) + " " + ReportText;
        _form.ShowForm(ReportTitle, text, controls.Panel, "&Send", () => SaveReport(controls), controls.Missing, Cancelled, sayText: true, byUser: byUser);
        return true;
    }

    /// <summary>
    /// axKit → Your reports (RP-2.2, RP-6.7): the unsent reports sent first when there are any and the user has agreed,
    /// then the Reports list in axTask, made when it does not exist yet.
    /// </summary>
    public void OpenReports()
    {
        try
        {
            ReportFiles.EnsureList(AppPaths.ReportsFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _form.ShowError("Your reports", "The reports folder could not be made: " + ex.Message);
            return;
        }

        if (ReportFiles.UnsentOfTheForm(AppPaths.ReportsFolder, AppPaths.ReportsWrittenFile).Ours.Count > 0 && ReporterToken.Token is not null && ReportsAgreed())
        {
            _form.Do(async () =>
            {
                var outcome = await SendUnsentAsync();
                _form.Announce(outcome.Sent switch
                {
                    0 => $"Not sent yet, {outcome.Reason}",
                    1 => "1 report sent",
                    _ => $"{outcome.Sent} reports sent",
                } + NotOfTheForm(outcome), false);
                WindowMenu.Start(Dispatch.AxTask, AppPaths.ReportsFolder);
            });
            return;
        }

        WindowMenu.Start(Dispatch.AxTask, AppPaths.ReportsFolder);
    }

    /// <summary>Send: the report written into the Reports list first (RP-3.2), then sent with every unsent one (RP-6).</summary>
    private void SaveReport(ReportControls controls)
    {
        var report = controls.ToReport();
        try
        {
            ReportFiles.Save(report, ReportStatus.NotSent(null), AppPaths.ReportsFolder, AppPaths.ReportsWrittenFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _form.ShowError("Report not saved", "The report could not be written: " + ex.Message);
            return;
        }

        SendReports(report);
    }

    /// <summary>
    /// Escape or Cancel: the form is cancelled whole, as Windows cancels a dialog, and nothing is kept (bundle AX-8.10
    /// rule 7; RP-D13). Until 2026-11-14 words typed were kept as a draft (RP-D6); drafts saved then stay in Your reports.
    /// </summary>
    private void Cancelled() => _form.Announce("Cancelled", false);

    /// <summary>
    /// The sending (RP-6): a build without the token says so and offers the report for copying (RP-6.5); the first time,
    /// the consent notice (RP-6.6); then the sender runs off the UI thread and the outcome is said or shown.
    /// </summary>
    private void SendReports(Report saved)
    {
        if (ReporterToken.Token is null)
        {
            ShowCannotSend("This build cannot send reports", "This build of axKit was made without the sending token, so your report stays in Your reports. " + CopyInstead, saved);
            return;
        }

        if (!ReportsAgreed())
        {
            _form.ShowNotice(new Notice("Before the first report goes", ConsentTarget.GitHubReports.Work + "\n"
                + $"GitHub's terms and privacy statement apply to what GitHub stores, between you and GitHub: {ConsentTarget.GitHubReports.TermsUrl}\n"
                + "Your report is saved in Your reports whatever you choose here; Not now leaves it there, unsent, and asks again with the next one.",
            [
                new OverlayChoice("&Send", () =>
                {
                    // The agreement of 5.1.0 to 5.8.0 was to a public issue: it never counts here, and it goes from
                    // the file with the new one's record, so that About lists what was agreed to last.
                    Consent.Withdraw(AppPaths.ConsentFile, ReportsConsentApp, ConsentTarget.GitHubReportsBefore);
                    Consent.Record(AppPaths.ConsentFile, ReportsConsentApp, ConsentTarget.GitHubReports.Program, BundleInfo.Version, DateTime.Now);
                    SendSaved(saved);
                }, IsDefault: true),
                new OverlayChoice("&Not now", () => _form.Announce("Saved; not sent yet. It goes with the next report once you agree", false), IsCancel: true),
            ])
            {
                Consent = ReportsConsentBox,
            });
            return;
        }

        SendSaved(saved);
    }

    private static bool ReportsAgreed() => Consent.Given(AppPaths.ConsentFile, ReportsConsentApp, ConsentTarget.GitHubReports.Program);

    private void SendSaved(Report saved) => _form.Do(async () =>
    {
        var outcome = await SendUnsentAsync();
        if (outcome.Sent > 0 && outcome.Last is { } last)
        {
            // The number is the report's in the maintainer's private list: said so that it can be quoted to him, and
            // never as an issue, which would be a place the user could open.
            _form.Announce((outcome.Sent == 1 ? $"Sent, report {last.Number}" : $"Sent, report {last.Number}, with {outcome.Sent - 1} earlier") + NotOfTheForm(outcome), false);
            return;
        }

        switch (outcome.Reason)
        {
            case ReportSender.NoTokenReason:
                ShowCannotSend("This build cannot send reports", "This build of axKit was made without the sending token, so your report stays in Your reports. " + CopyInstead, saved);
                break;
            case ReportSender.BlockedInstallReason:
                ShowBlockedInstall();
                break;
            case ReportSender.BlockedVersionReason:
                _form.ShowNotice(Notice.Plain("Report not sent",
                    "Reports from this version of axKit are not being accepted. Nothing was sent. Your report stays saved in Your reports on this computer.\n"
                    + "Update axKit from the Help menu, or from the Microsoft Store if you installed it there. After the update, open Your reports from the Help menu, and the report is sent."));
                break;
            case { } reason when reason.StartsWith("this version can no longer", StringComparison.Ordinal):
                ShowCannotSend("This version can no longer send reports", "GitHub no longer takes reports from this version of axKit. Update axKit from the Help menu and send it again from Your reports. " + CopyInstead, saved);
                break;
            case null:
                _form.Announce("Saved" + NotOfTheForm(outcome), false);
                break;
            default:
                _form.Announce($"Saved; not sent yet, {outcome.Reason}. It goes with the next report" + NotOfTheForm(outcome), false);
                break;
        }
    });

    /// <summary>RP-6.9: after what was sent, the one sentence about waiting files the report form did not write; nothing when there are none.</summary>
    private static string NotOfTheForm(SendOutcome outcome) =>
        outcome.NotOfTheForm == 0 ? string.Empty : ". " + ReportWritten.NotSentSentence(outcome.NotOfTheForm);

    private static Task<SendOutcome> SendUnsentAsync() =>
        ReportSender.SendUnsentAsync(AppPaths.ReportsFolder, AppPaths.ReportsFile, AppPaths.ReportsWrittenFile, BundleInfo.Version, ReporterToken.Token, ReportSeal.PublicKey, AxKitHttp.Client, null, CancellationToken.None);

    /// <summary>
    /// The notice of RP-6.5: why the report cannot go, with Copy report, the plain report to the user's own clipboard;
    /// the report stays saved. GitHub's own form, offered until 2026-11-14, would post into a repository that is
    /// private now, which no user can reach.
    /// </summary>
    private void ShowCannotSend(string title, string text, Report report)
    {
        var body = ReportBody.Markdown(report, BundleInfo.Version, ReportSender.ReporterId(AppPaths.ReportsFile));
        _form.ShowNotice(new Notice(title, text,
        [
            new OverlayChoice("&Copy report", () => _form.CopyText(body.Replace("\n", "\r\n"), "Copied"), StaysOpen: true),
            OverlayChoice.Close,
        ]));
    }

    /// <summary>
    /// The notice for an install the block list names (RP-6.4, RP-D12): said plainly, with the install id, a random
    /// id that names nobody, and the one place to ask when it is a mistake, the block list's own page, never the
    /// reports repository, which no user can open. Close is Enter's and Escape's choice.
    /// </summary>
    private void ShowBlockedInstall()
    {
        var id = ReportSender.ReporterId(AppPaths.ReportsFile).ToString("D");
        _form.ShowNotice(new Notice("Report not sent",
            "Reports from this install of axKit are not being accepted. Nothing was sent. Your report stays saved in Your reports on this computer.\n"
            + "If this is a mistake, ask on the block list's page: open an issue there and give your install id. The id is random and names nobody.\n"
            + $"Your install id: {id}",
        [
            new OverlayChoice("Copy &install id", () => _form.CopyText(id, "Copied"), StaysOpen: true),
            new OverlayChoice("&Open the block list page", () => _form.OpenUrl(ReportSender.BlocklistIssuesUrl)),
            OverlayChoice.Close,
        ]));
    }

    private static IReadOnlyList<string> LogTail(int lines)
    {
        try
        {
            return Log.Tail(lines).ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
