using System.ComponentModel;
using AxKit.Core;
using AxKit.Core.Ai;

namespace AxKit.Forms;

/// <summary>
/// AI help on a window (docs/axKit/ai-help.md AH-1, AH-3, AH-6; 2026-11-09): the one switch, the question about this
/// window with what the window knows about itself, the question run as Claude Code in print mode
/// (<see cref="AiRequest"/>) behind a waiting notice, the answer as a notice text with a field under it for a
/// follow-up, which continues the same Claude session, the folders Claude may read and Claude's working folder. Off,
/// any ask turns Claude on through the reminder (AH-1.3). A window holds one of these (the members of
/// <c>AxKitForm.Ai.cs</c> call it, and make the menu); it holds the question that waits for its answer, and asks of
/// the window its notices, its announcements, the consent and what the window knows about itself
/// (docs/axKit/repairs.json R-15: out of the window base, 2026-11-14).
/// </summary>
internal sealed class AiHelpFlow
{
    /// <summary>The app the agreement is recorded under (AH-1.2): one for the service, however many windows use it.</summary>
    private const string AiApp = "AI help";

    private readonly AxKitForm _form;
    private CancellationTokenSource? _asking;

    public AiHelpFlow(AxKitForm form)
    {
        _form = form;
    }

    /// <summary>
    /// Claude Code's working folder (AH-2.1, AH-D6), made when it is missing: Documents\axKit Claude unless the settings
    /// file names another; false, said, when it cannot be made.
    /// </summary>
    public bool AiWorkingFolder(out string folder)
    {
        try
        {
            folder = AiSettings.Load(AiSettings.DefaultPath, out _).EnsureWorkingFolder();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _form.Failed("AI help", "Claude's working folder could not be made: " + ex.Message);
            folder = string.Empty;
            return false;
        }

        WriteDocumentation(folder);
        return true;
    }

    /// <summary>
    /// The folders whose guides this process has seen to once: a development build writes every file again at the
    /// first use of a folder and only then. The comparison of AH-7.7 runs at every use.
    /// </summary>
    private static readonly HashSet<string> GuidesSeenTo = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// axKit's guides in the working folder (AH-7.1), written where missing or changed and again by a new version of
    /// axKit, at every use of the folder (AH-7.7). Off the window's thread (bundle rule 3): the question does not wait
    /// for some thirty files. The cases that wait: a folder that never had them, and a rule file that is missing or no
    /// longer holds axKit's text, since Claude Code reads the rule files as it starts, moments from now; in every
    /// other case the comparison and any repair of a guide run beside the start. A failure is logged and nothing
    /// else: the question or the conversation goes on without them.
    /// </summary>
    private static void WriteDocumentation(string folder)
    {
        bool first;
        lock (GuidesSeenTo)
        {
            first = GuidesSeenTo.Add(folder);
        }

        // AH-7.7: at every use, not once: a file of axKit's that was deleted or changed since is put right. The two
        // rule files are compared here (two small reads), since Claude Code reads them as it starts; the guides are
        // compared off the window's thread.
        if (AiDocumentation.IsMissing(folder) || AiDocumentation.RulesNeedRepair(folder))
        {
            WriteGuides(folder, first);
        }
        else
        {
            _ = Task.Run(() => WriteGuides(folder, first));
        }
    }

    /// <summary>One writer at a time: two presses close together compare and write one after the other.</summary>
    private static readonly object GuidesWriting = new();

    private static void WriteGuides(string folder, bool first)
    {
        try
        {
            lock (GuidesWriting)
            {
                // A development build's guides change without its version: written again, once in the process.
                var written = AiDocumentation.Write(folder, BundleInfo.Version, rewrite: first && BundleInfo.IsDevelopmentBuild);
                if (written.Count > 0)
                {
                    Log.Info($"Wrote axKit's guides into Claude's working folder: {written.Count} files");
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("axKit's guides could not be written into Claude's working folder: " + ex.Message);
        }
    }

    /// <summary>axKit, AI help, Claude's working folder: the folder opened in axFile, where a CLAUDE.md of the user's gives Claude its standing instructions.</summary>
    public void OpenAiWorkingFolder()
    {
        if (AiWorkingFolder(out var folder))
        {
            WindowMenu.Start(Dispatch.AxFile, folder);
        }
    }

    /// <summary>The switch (AH-1.1): off at once, said; on through <see cref="TurnOnAi"/>.</summary>
    public void ToggleAiHelp()
    {
        var settings = AiSettings.Load(AiSettings.DefaultPath, out _);
        if (settings.Enabled)
        {
            settings.Enabled = false;
            if (SaveAi(settings))
            {
                _form.Announce("Claude off", true);
            }

            return;
        }

        TurnOnAi(() => { });
    }

    /// <summary>
    /// Claude turned on (AH-1.2, AH-1.3; 5.6.0: whatever asked for Claude while it was off comes here, so that the
    /// first ask is the reminder and not a refusal): the consent notice of AX-11.4 when the agreement is not recorded
    /// yet (its Turn on records it and comes back here; Not now leaves Claude off and says so), then the switch on,
    /// "Claude on", and <paramref name="then"/>, the ask that was in hand.
    /// </summary>
    private void TurnOnAi(Action then)
    {
        if (!_form.HasConsent(ConsentTarget.AiHelp, "Before Claude helps in axKit", "&Turn on", () => TurnOnAi(then), () => _form.Announce("Claude stays off", true), AiApp))
        {
            return;
        }

        var settings = AiSettings.Load(AiSettings.DefaultPath, out _);
        settings.Enabled = true;
        if (SaveAi(settings))
        {
            _form.Announce("Claude on", true);
            then();
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
            _form.Failed("AI help", "The AI help settings could not be written: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Whether a question can be asked now (AH-1.3, AH-1.4, AH-1.2): off turns Claude on, through the reminder the
    /// first time, and asks again; Claude Code missing is a notice with Install it now; the consent is asked once for
    /// the service. False has said or shown why, or has the ask in hand; <paramref name="retry"/> is the caller again,
    /// run after the switch, an install or an agreement.
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
            TurnOnAi(retry);
            return false;
        }

        if (settings.FindClaude() is not { } found)
        {
            _form.ShowNotice(new Notice("Claude Code was not found",
                "AI help runs Claude Code, Anthropic's program, which was not found on this computer. Install Claude Code now runs Claude Code's own installer for your account; Claude Code then asks you to log in the first time it runs. Or install it by hand from its page, or name its program file as claudePath in the settings file ai-help.json.",
            [
                new OverlayChoice("&Install Claude Code now", () => _form.InstallProgram(ProgramInstall.ClaudeCode, () => ClaudeCodeProgram.Find(null) is not null, retry, ConsentTarget.AiHelp)),
                new OverlayChoice("&Open install instructions", () => _form.OpenUrl(ProgramInstall.ClaudeCode.Page), StaysOpen: true),
                OverlayChoice.Close,
            ]));
            return false;
        }

        claude = found;
        return _form.HasConsent(ConsentTarget.AiHelp, "Before Claude helps in axKit", "&Connect", retry, () => _form.Announce("Not now: nothing was asked", true), AiApp);
    }

    /// <summary>Shift+F1 and axKit, AI help, Ask about this window (AH-6): one field for the question, then the answer.</summary>
    public void AskAboutThisWindow()
    {
        if (!AiReady(AskAboutThisWindow, out _, out _))
        {
            return;
        }

        _form.AskText("Ask Claude about this window",
            "Claude gets this window's name and title, the name and place of the control you are in, your last actions here, the keyboard shortcuts and the user guide; never the text of what you are working on. Type your question and press Enter.",
            "Your question:", "Your question about this window", string.Empty, "&Ask",
            question => AskClaude(AiPrompts.AboutWindow(_form.ContextOfThisWindow(), question), [], null, answer => ShowAnswer(answer, [])),
            emptyMessage: "Type a question first");
    }

    /// <summary>
    /// One question to Claude (AH-2, AH-3.4): the waiting notice with Cancel, Claude Code run off the UI thread in the
    /// user's folders and <paramref name="extraFolders"/> (a window's own, for this call alone, AH-2.3), then
    /// <paramref name="then"/> with the answer, or nothing after a cancel. <paramref name="resume"/> continues a session.
    /// </summary>
    public void AskClaude(string prompt, IReadOnlyList<string> extraFolders, string? resume, Action<AiAnswer> then)
    {
        if (!AiReady(() => AskClaude(prompt, extraFolders, resume, then), out var settings, out var claude) || !AiWorkingFolder(out var workingFolder))
        {
            return;
        }

        var folders = extraFolders.Concat(settings.Folders).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _asking?.Cancel();
        var asking = new CancellationTokenSource();
        _asking = asking;
        _form.ShowNotice(new Notice("Asking Claude", "Claude is reading and answering; a few seconds, up to a minute for a long read. Escape stops the wait.",
        [
            new OverlayChoice("&Cancel", () =>
            {
                asking.Cancel();
                _form.Announce("Cancelled", true);
            }, IsCancel: true),
        ]));
        // AH-2.5: the log holds the sizes and the outcome, never the prompt or the answer.
        Log.Info($"AI help: a question of {prompt.Length} characters in {folders.Count} folders{(resume is null ? string.Empty : ", the same session")}");
        _form.Do(async () =>
        {
            AiAnswer answer;
            try
            {
                answer = await AiRequest.AskAsync(claude, prompt, workingFolder, folders, settings.Model, resume, asking.Token);
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

            if (asking.IsCancellationRequested || _form.IsDisposed)
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
    public void ShowAnswer(AiAnswer answer, IReadOnlyList<string> extraFolders, Action? closed = null)
    {
        if (answer.IsError)
        {
            _form.ShowNotice(new Notice("Claude could not answer", answer.Text, [new OverlayChoice("&Close", closed, IsDefault: true, IsCancel: true)]));
            return;
        }

        // Under every answer, who wrote it and that it can be wrong (AX-11.12); Copy takes the answer alone.
        _form.ShowNotice(new Notice("Claude says", answer.Text.TrimEnd() + "\n\n" + AiCaution.Written,
        [
            new OverlayChoice("Ask &more", () => AskClaude(AiPrompts.FollowUp(_form.Overlay.InputText), extraFolders, answer.SessionId, more => ShowAnswer(more, extraFolders, closed)), IsDefault: true),
            new OverlayChoice("Cop&y", () => _form.CopyText(answer.Text, "Copied"), StaysOpen: true),
            new OverlayChoice("&Close", closed, IsCancel: true),
        ])
        {
            Input = new OverlayInput("Ask more:", "Ask more about this answer", string.Empty, "Type a question first"),
            FocusText = true,
        });
    }

    // ---- the folders Claude may read (AH-2.3) ----

    /// <summary>axKit, AI help, Folders Claude may read: the list as a notice, with Add and Remove.</summary>
    public void ShowAiFolders()
    {
        var settings = AiSettings.Load(AiSettings.DefaultPath, out _);
        var list = settings.Folders.Count == 0
            ? "No folders yet: beyond its working folder, Claude reads nothing of yours until you add one. A question about a window or a screen needs none."
            : string.Join("\n", settings.Folders.Select((folder, index) => $"{index + 1}. {folder}"));
        var text = $"The folders Claude may read for a question, with everything in them, under your account. Claude Code itself starts in its working folder, {settings.ResolvedWorkingFolder()}, where a CLAUDE.md of yours gives it standing instructions.\n" + list;
        var choices = new List<OverlayChoice> { new("&Add a folder...", AddAiFolder, IsDefault: true) };
        if (settings.Folders.Count > 0)
        {
            choices.Add(new OverlayChoice("&Remove a folder...", () => RemoveAiFolder(settings)));
        }

        choices.Add(new OverlayChoice("&Close", IsCancel: true));
        _form.ShowNotice(new Notice("Folders Claude may read", text, choices));
    }

    private void AddAiFolder()
    {
        var folder = _form.ChooseFolder("Choose a folder Claude may read", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
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
            _form.Announce($"Added {PathName.Of(folder)}", true);
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
                _form.Announce($"Removed {PathName.Of(folder)}", true);
            }
        })).ToList();
        choices.Add(new OverlayChoice("&Cancel", ShowAiFolders, IsCancel: true));
        _form.ShowNotice(new Notice("Remove which folder?", "Claude will no longer read it. Tab to the folder, Enter removes it.", choices));
    }
}
