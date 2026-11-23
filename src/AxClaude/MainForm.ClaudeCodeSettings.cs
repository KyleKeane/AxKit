using System.Text;
using AxClaude.Core;
using AxClaude.Core.Pty;
using AxClaude.Core.Transcript;
using AxKit.Core;
using AxKit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- Claude Code's own settings (FR-14): the table is Core's ClaudeCodeSettings (rule 12) ----

    /// <summary>The menu items of the settings file's keys, each with its setting and the index of its choice; their checks are read from the file as the menu opens.</summary>
    private readonly List<(ClaudeCodeSetting Setting, int Choice, ToolStripMenuItem Item)> _claudeCodeItems = [];

    /// <summary>One change of the file at a time, in the order chosen.</summary>
    private readonly SemaphoreSlim _claudeCodeWrites = new(1, 1);

    /// <summary>
    /// Claude Code's user settings file for the Claude this window starts (FR-14.2): the environment it is given
    /// decides, as for Claude Code itself; the user profile, or a test run's own folder, so that a run never reads or
    /// writes the user's real file.
    /// </summary>
    private static string ClaudeCodeSettingsFile =>
        ClaudeCodeSettings.UserSettingsFile(AutomationEnvironment.Home ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ClaudeLauncher.BuildEnvironment());

    /// <summary>Actions: Choose the model, Choose the effort (FR-14.3): the slash command sent bare, Claude Code's own picker comes up in the control area.</summary>
    private void AddClaudeCodeCommands(ToolStripMenuItem menu)
    {
        foreach (var setting in ClaudeCodeSettings.All.Where(setting => setting.Kind == ClaudeCodeSettingKind.SlashCommand))
        {
            menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem(setting.Name, null, (_, _) => SendClaudeCodeCommand(setting.Target)), CommandMark.Act)); // Types a command into Claude Code
        }
    }

    /// <summary>
    /// Options, Claude Code settings (FR-14.1): one check item per key of Claude Code's user settings file, the choices
    /// of a key with more than two values as items of their own, the groups parted by separators; checked from the
    /// file each time the menu opens.
    /// </summary>
    private ToolStripMenuItem CreateClaudeCodeSettingsMenu()
    {
        var menu = new ToolStripMenuItem("Claude &Code settings");
        string? group = null;
        foreach (var setting in ClaudeCodeSettings.All.Where(setting => setting.Kind == ClaudeCodeSettingKind.UserSettingsKey))
        {
            if (group is not null && group != setting.Group)
            {
                menu.DropDownItems.Add(new ToolStripSeparator());
            }

            group = setting.Group;
            if (setting.Choices.Count == 2)
            {
                // A check item: checked is the value that is not Claude Code's default. Act: it writes a file of
                // Claude Code's, outside axKit's own settings (docs/axKit/app-kit.md AK-2.5).
                var item = CommandPalette.Marked(new ToolStripMenuItem(setting.Name) { CheckOnClick = true }, CommandMark.Act); // Writes Claude Code's user settings file
                item.Click += (_, _) => ChooseClaudeCodeSetting(setting, item.Checked ? 1 : 0);
                _claudeCodeItems.Add((setting, 1, item));
                menu.DropDownItems.Add(item);
                continue;
            }

            for (var i = 0; i < setting.Choices.Count; i++)
            {
                var choice = i;
                var item = CommandPalette.Marked(new ToolStripMenuItem(setting.Choices[i].Text), CommandMark.Act); // Writes Claude Code's user settings file
                item.Click += (_, _) => ChooseClaudeCodeSetting(setting, choice);
                _claudeCodeItems.Add((setting, choice, item));
                menu.DropDownItems.Add(item);
            }
        }

        CommandPalette.RefreshedOnOpening(menu, CheckClaudeCodeItems);
        return menu;
    }

    /// <summary>
    /// The checks as the file holds them now: read each time the menu opens, since the user, Claude Code's /config or
    /// another window may have changed it. A small local file, read as the window base reads AI help's switch; a file
    /// that cannot be read or is no JSON object checks nothing.
    /// </summary>
    private void CheckClaudeCodeItems()
    {
        string? text;
        try
        {
            var path = ClaudeCodeSettingsFile;
            text = File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            text = "not readable";
        }

        foreach (var (setting, choice, item) in _claudeCodeItems)
        {
            item.Checked = ClaudeCodeSettings.ChoiceIn(setting, text) == choice;
        }
    }

    /// <summary>
    /// A choice of the menu written into Claude Code's user settings file off the UI thread (FR-14.2): that key alone
    /// changed, a copy of the file as it was beside it, then the change said with when it applies, or the reason the
    /// file was left as it was.
    /// </summary>
    private async void ChooseClaudeCodeSetting(ClaudeCodeSetting setting, int choice)
    {
        var path = ClaudeCodeSettingsFile;
        var json = setting.Choices[choice].Json;
        string? problem;
        await _claudeCodeWrites.WaitAsync();
        try
        {
            problem = await Task.Run(() => WriteClaudeCodeSetting(path, setting.Target, json));
        }
        finally
        {
            _claudeCodeWrites.Release();
        }

        if (IsDisposed)
        {
            return;
        }

        CheckClaudeCodeItems();
        if (problem is not null)
        {
            Fail(problem);
            return;
        }

        Log.Info($"Claude Code's settings: {setting.Target} {(json is null ? "removed" : "set to " + json)}");
        // The menu closes on a choice, so the choice is said (AX-6.4).
        Announce(MenuText.Plain(setting.Choices[choice].Text) + ", applies at once", true);
    }

    /// <summary>The file read, the one key changed and the file written whole, with the copy of what it was; null when done, else what went wrong.</summary>
    private static string? WriteClaudeCodeSetting(string path, string key, string? json)
    {
        try
        {
            var text = File.Exists(path) ? File.ReadAllText(path) : null;
            var change = ClaudeCodeSettings.WithKey(text, key, json);
            if (change.Error is { } error)
            {
                return $"Claude Code's settings file was not changed: it {error}. Correct it or move it away, then choose again. The file: {path}";
            }

            if (!change.Changed)
            {
                return null;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (text is not null)
            {
                File.Copy(path, path + ClaudeCodeSettings.BackupSuffix, overwrite: true);
            }

            // Written beside it and moved over it, so that Claude Code, which watches the file, never reads half of it.
            var written = path + ".axkit-new";
            File.WriteAllText(written, change.Text, new UTF8Encoding(false));
            File.Move(written, path, overwrite: true);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"Claude Code's settings file could not be written: {ex.Message}";
        }
    }

    /// <summary>A slash command sent as the user's own typed command would go (FR-14.3): only into an idle Claude, so that it is no answer to a question and waits behind no turn.</summary>
    private void SendClaudeCodeCommand(string command)
    {
        if (!ProcessRunning)
        {
            Announce(ClaudeStopped, true);
            return;
        }

        if (Streams)
        {
            // stream-mode.md SM-5.5: these commands open Claude Code's own pickers, which the stream does not have.
            SayNotWithStream();
            return;
        }

        if (!ClaudeIdle)
        {
            Announce("Not sent: Claude is busy. Choose it again when Claude is done", true);
            return;
        }

        _lastCommand = SlashCommands.Find(command);
        _model.Send(command, evenWhilePrompting: false);
        _messageSent = true;
        QueueWrites(command);
        ResendEnterWhileHeld(command, attempt: 1);
        if (_settings.AnnounceSent)
        {
            Announce("Sent", false);
        }
    }
}
