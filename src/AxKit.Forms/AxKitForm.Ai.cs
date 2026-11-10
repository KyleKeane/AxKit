using System.ComponentModel;
using AxKit.Core;
using AxKit.Core.Ai;

namespace AxKit.Forms;

/// <summary>
/// AI help on this window (docs/axkit/ai-help.md AH-1, AH-3, AH-6; 2026-11-09): the axKit menu's AI help submenu
/// with the one switch, Ask about this window and the folders Claude may read; Shift+F1, a question about this
/// window with what the window knows about itself; the question run as Claude Code in print mode
/// (<see cref="AiRequest"/>) behind a waiting notice; and the answer as a notice text with a field under it for a
/// follow-up, which continues the same Claude session. axHelp and axDescribe call the same methods with their own
/// prompts; no window has AI code of its own beyond what it gathers.
/// </summary>
public abstract partial class AxKitForm
{
    /// <summary>The app the agreement is recorded under (AH-1.2): one for the service, however many windows use it.</summary>
    private const string AiApp = "AI help";

    private const string AiOff = "AI help is off: axKit menu, AI help, Let Claude help in the apps";

    private Func<string>? _userGuide;
    private CancellationTokenSource? _asking;

    /// <summary>axKit, AI help ▸ (AH-1.1): the switch, read as the submenu opens since another window may have changed it; Ask about this window; the folders.</summary>
    internal ToolStripMenuItem CreateAiHelpMenu()
    {
        var menu = new ToolStripMenuItem("AI &help");
        var on = new ToolStripMenuItem("&Let Claude help in the apps", null, (_, _) => ToggleAiHelp());
        menu.DropDownItems.Add(on);
        menu.DropDownItems.Add(Item("&Ask about this window...", AskAboutThisWindow, BundleKeys.AskClaude));
        menu.DropDownItems.Add(new ToolStripMenuItem("&Folders Claude may read...", null, (_, _) => ShowAiFolders()));
        CommandPalette.RefreshedOnOpening(menu, () => on.Checked = AiSettings.Load(AiSettings.DefaultPath, out _).Enabled);
        return menu;
    }

    /// <summary>On after the consent of AX-11.4 (AH-1.2), off at once; said either way.</summary>
    private void ToggleAiHelp()
    {
        var settings = AiSettings.Load(AiSettings.DefaultPath, out _);
        if (settings.Enabled)
        {
            settings.Enabled = false;
            if (SaveAi(settings))
            {
                Announce("AI help off", true);
            }

            return;
        }

        if (!HasConsent(ConsentTarget.AiHelp, "Before Claude helps in the apps", "&Turn on", ToggleAiHelp, () => Announce("AI help stays off", true), AiApp))
        {
            return;
        }

        settings.Enabled = true;
        if (SaveAi(settings))
        {
            Announce("AI help on: Shift+F1 asks Claude about this window; axHelp and axDescribe answer from anywhere", true);
        }
    }

    private bool SaveAi(AiSettings settings)
    {
        try
        {
            settings.Save(AiSettings.DefaultPath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Failed("AI help", "The AI help settings could not be written: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Whether a question can be asked now (AH-1.3, AH-1.4, AH-1.2): off is said; Claude Code missing is a notice with
    /// Install it now; the consent is asked once for the service. False has said or shown why; <paramref name="retry"/>
    /// is the caller again, run after an install or an agreement.
    /// </summary>
    private bool AiReady(Action retry, out AiSettings settings, out string claude)
    {
        settings = AiSettings.Load(AiSettings.DefaultPath, out var problem);
        claude = string.Empty;
        if (problem is not null)
        {
            Log.Error("AI help: the settings file was not read: " + problem);
        }

        if (!settings.Enabled)
        {
            Announce(AiOff, true);
            return false;
        }

        if (settings.FindClaude() is not { } found)
        {
            ShowNotice(new Notice("Claude Code was not found",
                "AI help runs Claude Code, Anthropic's program, which was not found on this computer. Install Claude Code now runs Claude Code's own installer for your account; Claude Code then asks you to log in the first time it runs. Or install it by hand from its page, or name its program file in the settings (axKit menu, AI help).",
            [
                new OverlayChoice("&Install Claude Code now", () => InstallProgram(ProgramInstall.ClaudeCode, () => ClaudeCodeProgram.Find(null) is not null, retry, ConsentTarget.AiHelp)),
                new OverlayChoice("&Open install instructions", () => OpenUrl(ProgramInstall.ClaudeCode.Page), StaysOpen: true),
                OverlayChoice.Close,
            ]));
            return false;
        }

        claude = found;
        return HasConsent(ConsentTarget.AiHelp, "Before Claude helps in the apps", "&Connect", retry, () => Announce("Not now: nothing was asked", true), AiApp);
    }

    /// <summary>Shift+F1 and axKit, AI help, Ask about this window (AH-6): one field for the question, then the answer.</summary>
    protected void AskAboutThisWindow()
    {
        if (!AiReady(AskAboutThisWindow, out _, out _))
        {
            return;
        }

        AskText("Ask Claude about this window",
            "Claude gets this window's name and title, the name and place of the control you are in, your last actions here, the keyboard shortcuts and the user guide; never the text of what you are working on. Type your question and press Enter.",
            "Your question:", "Your question about this window", string.Empty, "&Ask",
            question => AskClaude(AiPrompts.AboutWindow(ContextOfThisWindow(), question), [], null, answer => ShowAnswer(answer, [])),
            emptyMessage: "Type a question first");
    }

    /// <summary>What this window knows about itself (AH-6.2), redacted as a report is (RP-4.2): never the text of a document.</summary>
    private WindowContext ContextOfThisWindow() =>
        new(AppTitle, Text, Redaction.Redact(FocusDescription()), Shortcuts, _userGuide?.Invoke(),
            Recent.Lines(DateTime.UtcNow).Select(line => Redaction.Redact(line)).ToList());

    /// <summary>
    /// One question to Claude (AH-2, AH-3.4): the waiting notice with Cancel, Claude Code run off the UI thread in the
    /// user's folders and <paramref name="extraFolders"/> (a window's own, for this call alone, AH-2.3), then
    /// <paramref name="then"/> with the answer, or nothing after a cancel. <paramref name="resume"/> continues a session.
    /// </summary>
    protected void AskClaude(string prompt, IReadOnlyList<string> extraFolders, string? resume, Action<AiAnswer> then)
    {
        if (!AiReady(() => AskClaude(prompt, extraFolders, resume, then), out var settings, out var claude))
        {
            return;
        }

        var folders = extraFolders.Concat(settings.Folders).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _asking?.Cancel();
        var asking = new CancellationTokenSource();
        _asking = asking;
        ShowNotice(new Notice("Asking Claude", "Claude is reading and answering; a few seconds, up to a minute for a long read. Escape stops the wait.",
        [
            new OverlayChoice("&Cancel", () =>
            {
                asking.Cancel();
                Announce("Cancelled", true);
            }, IsCancel: true),
        ]));
        // AH-2.5: the log holds the sizes and the outcome, never the prompt or the answer.
        Log.Info($"AI help: a question of {prompt.Length} characters in {folders.Count} folders{(resume is null ? string.Empty : ", the same session")}");
        Do(async () =>
        {
            AiAnswer answer;
            try
            {
                answer = await AiRequest.AskAsync(claude, prompt, folders, settings.Model, resume, asking.Token);
            }
            catch (OperationCanceledException)
            {
                Log.Info("AI help: cancelled");
                return;
            }
            catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException)
            {
                answer = new AiAnswer("Claude Code could not be run: " + ex.Message, true, null);
            }

            if (asking.IsCancellationRequested || IsDisposed)
            {
                return;
            }

            Log.Info(answer.IsError ? "AI help: Claude could not answer" : $"AI help: an answer of {answer.Text.Length} characters");
            then(answer);
        });
    }

    /// <summary>
    /// The answer as a notice (AH-3.1, AH-6.3): "Claude says", the text read first, with a field under it whose Enter
    /// asks more in the same session, Copy, and Close, which runs <paramref name="closed"/>; an error as "Claude could
    /// not answer" with Claude Code's words.
    /// </summary>
    protected void ShowAnswer(AiAnswer answer, IReadOnlyList<string> extraFolders, Action? closed = null)
    {
        if (answer.IsError)
        {
            ShowNotice(new Notice("Claude could not answer", answer.Text, [new OverlayChoice("&Close", closed, IsDefault: true, IsCancel: true)]));
            return;
        }

        ShowNotice(new Notice("Claude says", answer.Text,
        [
            new OverlayChoice("Ask &more", () => AskClaude(AiPrompts.FollowUp(Overlay.InputText), extraFolders, answer.SessionId, more => ShowAnswer(more, extraFolders, closed)), IsDefault: true),
            new OverlayChoice("Cop&y", () => CopyText(answer.Text, "Copied"), StaysOpen: true),
            new OverlayChoice("&Close", closed, IsCancel: true),
        ])
        {
            Input = new OverlayInput("Ask more:", "Ask more about this answer", string.Empty, "Type a question first"),
            FocusText = true,
        });
    }

    // ---- the folders Claude may read (AH-2.3) ----

    /// <summary>axKit, AI help, Folders Claude may read: the list as a notice, with Add and Remove.</summary>
    protected void ShowAiFolders()
    {
        var settings = AiSettings.Load(AiSettings.DefaultPath, out _);
        var list = settings.Folders.Count == 0
            ? "No folders yet: Claude reads nothing of yours until you add one. A question about a window or a screen needs none."
            : string.Join("\n", settings.Folders.Select((folder, index) => $"{index + 1}. {folder}"));
        var text = "The folders Claude may read for a question, with everything in them, under your account; Claude Code starts in the first.\n" + list;
        var choices = new List<OverlayChoice> { new("&Add a folder...", AddAiFolder, IsDefault: true) };
        if (settings.Folders.Count > 0)
        {
            choices.Add(new OverlayChoice("&Remove a folder...", () => RemoveAiFolder(settings)));
        }

        choices.Add(new OverlayChoice("&Close", IsCancel: true));
        ShowNotice(new Notice("Folders Claude may read", text, choices));
    }

    private void AddAiFolder()
    {
        var folder = ChooseFolder("Choose a folder Claude may read", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (folder is null)
        {
            ShowAiFolders();
            return;
        }

        var settings = AiSettings.Load(AiSettings.DefaultPath, out _);
        if (!settings.Folders.Contains(folder, StringComparer.OrdinalIgnoreCase))
        {
            settings.Folders.Add(folder);
        }

        if (SaveAi(settings))
        {
            ShowAiFolders();
            Announce($"Added {PathName.Of(folder)}", true);
        }
    }

    /// <summary>One button per folder, so that the one to remove is chosen by Tab and Enter.</summary>
    private void RemoveAiFolder(AiSettings settings)
    {
        var choices = settings.Folders.Select((folder, index) => new OverlayChoice($"&{index + 1}. {folder}", () =>
        {
            var current = AiSettings.Load(AiSettings.DefaultPath, out _);
            current.Folders.RemoveAll(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
            if (SaveAi(current))
            {
                ShowAiFolders();
                Announce($"Removed {PathName.Of(folder)}", true);
            }
        })).ToList();
        choices.Add(new OverlayChoice("&Cancel", ShowAiFolders, IsCancel: true));
        ShowNotice(new Notice("Remove which folder?", "Claude will no longer read it. Tab to the folder, Enter removes it.", choices));
    }
}
