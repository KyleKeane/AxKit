using AxKit.Core;
using AxKit.Core.Ai;

namespace AxKit.Forms;

/// <summary>
/// AI help on this window (docs/axKit/ai-help.md AH-1, AH-3, AH-6; 2026-11-09): the axKit menu's Claude
/// with the one switch, Ask about this window, the folders Claude may read, Claude's working folder and the AI tools;
/// Shift+F1, a question about this window with what the window knows about itself; and the members a window asks
/// Claude through. The work behind them (the switch, the question run as Claude Code in print mode, the answer with
/// its follow-up, the folders) is <see cref="AiHelpFlow"/>'s. axHelp uses only the working folder from here (its
/// conversation is axClaude); no window has AI code of its own beyond what it gathers.
/// </summary>
public abstract partial class AxKitForm
{
    private Func<string>? _userGuide;

    /// <summary>The question that waits for its answer; made in the constructor (AxKitForm.cs).</summary>
    private readonly AiHelpFlow _ai;

    /// <summary>
    /// axKit, Claude ▸ (AH-1.1; AX-9.1: a submenu of the axKit menu, beside the AI tools), in groups: the switch, read
    /// as the menu opens since another window may have changed it; the question, Ask about this window; what Claude may
    /// read, the folders and Claude's working folder.
    /// </summary>
    internal ToolStripMenuItem CreateClaudeMenu()
    {
        var menu = new ToolStripMenuItem("&Claude");
        // The switch and the folders Claude reads are never offered to an AI tool (docs/axKit/app-kit.md AK-2.6, exception 3).
        var on = CommandPalette.NeverForAiTools(new ToolStripMenuItem("&Use Claude in axKit", null, (_, _) => _ai.ToggleAiHelp()));
        menu.DropDownItems.Add(on);
        menu.DropDownItems.Add(new ToolStripSeparator());
        menu.DropDownItems.Add(Item("&Ask about this window...", AskAboutThisWindow, BundleKeys.AskClaude, CommandMark.Act));
        menu.DropDownItems.Add(new ToolStripSeparator());
        menu.DropDownItems.Add(CommandPalette.NeverForAiTools(new ToolStripMenuItem("&Folders Claude may read...", null, (_, _) => ShowAiFolders())));
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("Claude's &working folder", null, (_, _) => OpenAiWorkingFolder()), CommandMark.Open));
        CommandPalette.RefreshedOnOpening(menu, () => on.Checked = AiSettings.Load(AiSettings.DefaultPath, out _).Enabled);
        return menu;
    }

    /// <summary>
    /// Claude Code's working folder (AH-2.1, AH-D6), made when it is missing: Documents\axKit Claude unless the settings
    /// file names another; false, said, when it cannot be made.
    /// </summary>
    protected bool AiWorkingFolder(out string folder) => _ai.AiWorkingFolder(out folder);

    /// <summary>axKit, AI help, Claude's working folder: the folder opened in axFile, where a CLAUDE.md of the user's gives Claude its standing instructions.</summary>
    protected void OpenAiWorkingFolder() => _ai.OpenAiWorkingFolder();

    /// <summary>Shift+F1 and axKit, AI help, Ask about this window (AH-6): one field for the question, then the answer.</summary>
    protected void AskAboutThisWindow() => _ai.AskAboutThisWindow();

    /// <summary>What this window knows about itself (AH-6.2), redacted as a report is (RP-4.2): never the text of a document.</summary>
    internal WindowContext ContextOfThisWindow() =>
        new(AppTitle, Text, Redaction.Redact(FocusDescription()), Shortcuts, _userGuide?.Invoke(),
            Recent.Lines(DateTime.UtcNow).Select(line => Redaction.Redact(line)).ToList());

    /// <summary>
    /// One question to Claude (AH-2, AH-3.4): the waiting notice with Cancel, Claude Code run off the UI thread in the
    /// user's folders and <paramref name="extraFolders"/> (a window's own, for this call alone, AH-2.3), then
    /// <paramref name="then"/> with the answer, or nothing after a cancel. <paramref name="resume"/> continues a session.
    /// </summary>
    protected void AskClaude(string prompt, IReadOnlyList<string> extraFolders, string? resume, Action<AiAnswer> then) =>
        _ai.AskClaude(prompt, extraFolders, resume, then);

    /// <summary>
    /// The answer as a notice (AH-3.1, AH-6.3): "Claude says", the text read first, with a field under it whose Enter
    /// asks more in the same session, Copy, and Close, which runs <paramref name="closed"/>; an error as "Claude could
    /// not answer" with Claude Code's words.
    /// </summary>
    protected void ShowAnswer(AiAnswer answer, IReadOnlyList<string> extraFolders, Action? closed = null) =>
        _ai.ShowAnswer(answer, extraFolders, closed);

    /// <summary>axKit, AI help, Folders Claude may read: the list as a notice, with Add and Remove.</summary>
    protected void ShowAiFolders() => _ai.ShowAiFolders();
}
