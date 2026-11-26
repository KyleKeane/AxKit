using AxHelp.Core;

namespace AxHelp.Tests;

/// <summary>axHelp's pure code (docs/axHelp/SPEC.md HE-3.3, HE-5): the settings file with its two hotkeys and model, and the picture files it passes on.</summary>
public sealed class HelpTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axhelp-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void The_settings_round_trip_with_both_hotkeys_and_the_model()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "axHelp.json");
        var settings = HelpSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal("Ctrl+Shift+Slash", settings.Key.ToString());
        Assert.Equal("Ctrl+Shift+Period", settings.ScreenshotKey.ToString());
        Assert.Equal(HelpSettings.FastModel, settings.Model);

        settings.ScreenshotHotkeyText = "Ctrl+Alt+Shift+F11";
        settings.Model = "opus";
        settings.Save(path);
        var loaded = HelpSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Equal("Ctrl+Alt+Shift+F11", loaded.ScreenshotKey.ToString());
        Assert.Equal("opus", loaded.Model);

        // A file of 5.6.0, with its exchanges and an unusable key, still reads: the exchanges are left, the key falls back.
        File.WriteAllText(path, """{ "screenshotHotkey": "nonsense", "model": " ", "exchanges": [ { "question": "q", "answer": "a" } ] }""");
        var old = HelpSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Equal("Ctrl+Shift+Period", old.ScreenshotKey.ToString());
        Assert.Equal(HelpSettings.FastModel, old.Model);
    }

    /// <summary>HE-4.2: Windows+Alt+H and Windows+Alt+S, the keys until 2026-11-14, are taken over to the new ones, each on its own, and the file holds the new ones after a save.</summary>
    [Fact]
    public void A_file_with_the_retired_hotkeys_is_taken_over_to_the_new_ones()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "axHelp.json");
        File.WriteAllText(path, """{ "hotkey": "Windows+Alt+H", "screenshotHotkey": "Windows+Alt+S", "model": "opus", "startWithWindows": false }""");
        var settings = HelpSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal("Ctrl+Shift+Slash", settings.HotkeyText);
        Assert.Equal("Ctrl+Shift+Period", settings.ScreenshotHotkeyText);
        // Nothing else of the file changes.
        Assert.Equal("opus", settings.Model);
        Assert.False(settings.StartWithWindows);

        settings.Save(path);
        // The file itself holds the new keys now (a plus sign may be written escaped, so only the key's name is looked for).
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("Alt", text, StringComparison.Ordinal);
        Assert.Contains("Slash", text, StringComparison.Ordinal);
        Assert.Contains("Period", text, StringComparison.Ordinal);
        var again = HelpSettings.Load(path, out _);
        Assert.Equal("Ctrl+Shift+Slash", again.Key.ToString());
        Assert.Equal("Ctrl+Shift+Period", again.ScreenshotKey.ToString());
    }

    /// <summary>HE-4.2: a key the user chose stays, and one retired key in the file does not move the other.</summary>
    [Fact]
    public void A_hotkey_the_user_chose_is_kept()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "axHelp.json");
        File.WriteAllText(path, """{ "hotkey": "Ctrl+Alt+J", "screenshotHotkey": "Windows+Alt+S" }""");
        var mixed = HelpSettings.Load(path, out _);
        Assert.Equal("Ctrl+Alt+J", mixed.HotkeyText);
        Assert.Equal("Ctrl+Shift+Period", mixed.ScreenshotHotkeyText);

        File.WriteAllText(path, """{ "hotkey": "Windows+Alt+H", "screenshotHotkey": "Windows+Alt+F9" }""");
        var other = HelpSettings.Load(path, out _);
        Assert.Equal("Ctrl+Shift+Slash", other.HotkeyText);
        Assert.Equal("Windows+Alt+F9", other.ScreenshotHotkeyText);
    }

    [Fact]
    public void A_picture_file_is_one_of_the_kinds_claude_reads_and_anything_else_is_refused()
    {
        Assert.True(PictureFile.IsPicture(@"C:\pictures\chart.PNG"));
        Assert.True(PictureFile.IsPicture("photo.jpeg"));
        Assert.False(PictureFile.IsPicture("notes.txt"));
        Assert.Equal("notes.txt is not a picture axHelp can pass to Claude: a .png, .jpg, .jpeg, .gif or .webp file", PictureFile.Refusal(@"C:\notes.txt"));

        Directory.CreateDirectory(_folder);
        var file = Path.Combine(_folder, "blocks.png");
        Assert.Equal(file + " was not found", PictureFile.Refusal(file));
        File.WriteAllBytes(file, [1]);
        Assert.Null(PictureFile.Refusal(file));
        Assert.Equal("Pictures (*.png, *.jpg, *.jpeg, *.gif, *.webp)|*.png;*.jpg;*.jpeg;*.gif;*.webp|All files (*.*)|*.*", PictureFile.Filter);
    }

    [Fact]
    public void The_working_folders_two_files_are_written_once_and_the_users_own_are_kept()
    {
        Directory.CreateDirectory(_folder);
        Assert.Equal(["CLAUDE.md", Path.Combine(".claude", "agents", "agent.md")], WorkingFolderFiles.WriteMissing(_folder));
        Assert.Contains("model: opus", File.ReadAllText(Path.Combine(_folder, ".claude", "agents", "agent.md")));
        Assert.StartsWith("---\r\nname: agent\r\n", File.ReadAllText(Path.Combine(_folder, ".claude", "agents", "agent.md")));
        // HE-2.6: a new folder's instructions name axKit's guides too.
        Assert.Contains("read index.md in the folder documentation here", File.ReadAllText(Path.Combine(_folder, "CLAUDE.md")));
        // HE-2.8: and axKit's rules, the AI tools' among them.
        Assert.Contains(@"axKit's rules in the folder .claude\rules here", File.ReadAllText(Path.Combine(_folder, "CLAUDE.md")));

        File.WriteAllText(Path.Combine(_folder, "CLAUDE.md"), "my own");
        Assert.Empty(WorkingFolderFiles.WriteMissing(_folder));
        Assert.Equal("my own", File.ReadAllText(Path.Combine(_folder, "CLAUDE.md")));
    }

    /// <summary>HE-2.9: a folder as axHelp and axKit prepare it holds nothing to ask about; the agent changed, or a hook added, is asked about.</summary>
    [Fact]
    public void A_working_folder_as_axHelp_prepares_it_holds_no_file_to_ask_about_and_a_changed_agent_is_one()
    {
        Directory.CreateDirectory(_folder);
        var accepted = Path.Combine(_folder, "kept", "ai-folder-files.json");
        WorkingFolderFiles.WriteMissing(_folder);
        AxKit.Core.Ai.AiDocumentation.Write(_folder, "1.2.3");
        Assert.Empty(AxKit.Core.Ai.AiFolderCheck.NewOrChanged(_folder, accepted, WorkingFolderFiles.Obeyed));

        File.AppendAllText(Path.Combine(_folder, WorkingFolderFiles.AgentPath), "Run this first.\r\n");
        File.WriteAllText(Path.Combine(_folder, ".claude", "settings.json"), "{\"hooks\":{\"SessionStart\":[{\"hooks\":[{\"type\":\"command\",\"command\":\"x.cmd\"}]}]}}");
        var named = AxKit.Core.Ai.AiFolderCheck.NewOrChanged(_folder, accepted, WorkingFolderFiles.Obeyed);
        Assert.Equal([WorkingFolderFiles.AgentPath, Path.Combine(".claude", "settings.json")], named.Select(file => file.Path));

        // Start anyway and remember these files: silent from then on.
        Assert.True(AxKit.Core.Ai.AiFolderCheck.Accept(_folder, accepted, named));
        Assert.Empty(AxKit.Core.Ai.AiFolderCheck.NewOrChanged(_folder, accepted, WorkingFolderFiles.Obeyed));
    }

    [Fact]
    public void The_conversation_starts_on_the_working_folder_titled_axHelp_with_the_model()
    {
        // HE-1.3: always a window that hides on closing; HE-2.8: no option for the AI tools since 2026-11-26, which
        // every axClaude window gives while the user has them on; HE-2.7: hidden from the start when axHelp starts it ahead.
        Assert.Equal([@"C:\w", "--title", "axHelp", "--model", "haiku", "--hide-on-close", "--source", "screen"], HelpSettings.ConversationArguments(@"C:\w", "haiku", null));
        Assert.Equal([@"C:\w", "--title", "axHelp", "--model", "opus", "--hide-on-close", "--source", "screen", "--message", "Screenshot: a.png "], HelpSettings.ConversationArguments(@"C:\w", "opus", "Screenshot: a.png "));
        Assert.Equal([@"C:\w", "--title", "axHelp", "--model", "haiku", "--hide-on-close", "--source", "screen", "--hidden"], HelpSettings.ConversationArguments(@"C:\w", "haiku", null, hidden: true));
    }

    /// <summary>HE-2.3 (bundle repairs.md R-71): only a model's name is kept and reaches Claude Code's command line; anything else is the default.</summary>
    [Theory]
    [InlineData("haiku", "haiku")]
    [InlineData(" opus ", "opus")]
    [InlineData("claude-sonnet-4-5", "claude-sonnet-4-5")]
    [InlineData("opus[1m]", "opus[1m]")]
    [InlineData("claude_3.7", "claude_3.7")]
    [InlineData("--dangerously-skip-permissions", HelpSettings.FastModel)]
    [InlineData("-p", HelpSettings.FastModel)]
    [InlineData("opus --add-dir C:\\", HelpSettings.FastModel)]
    [InlineData("opus\" --x", HelpSettings.FastModel)]
    [InlineData("opus&calc", HelpSettings.FastModel)]
    [InlineData(".hidden", HelpSettings.FastModel)]
    [InlineData("op\nus", HelpSettings.FastModel)]
    [InlineData("opüs", HelpSettings.FastModel)]
    [InlineData("", HelpSettings.FastModel)]
    [InlineData(null, HelpSettings.FastModel)]
    public void Only_a_models_name_is_kept_and_reaches_the_command_line(string? written, string kept)
    {
        Assert.Equal(kept, HelpSettings.ModelOrDefault(written));
        Assert.Equal(kept, HelpSettings.ConversationArguments(@"C:\w", written!, null)[4]);

        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "axHelp.json");
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new { model = written }));
        Assert.Equal(kept, HelpSettings.Load(path, out _).Model);
        Assert.False(HelpSettings.IsModelName(new string('a', HelpSettings.MaxModelLength + 1)));
        Assert.All(HelpSettings.Models, choice => Assert.True(HelpSettings.IsModelName(choice.Model)));
    }

    /// <summary>HE-2.7: the conversation is started ahead only when axHelp started hidden, the setting is on, the agreement is given and Claude Code is installed.</summary>
    [Fact]
    public void The_conversation_is_started_hidden_only_with_all_four_conditions()
    {
        Assert.True(HelpSettings.StartsConversationHidden(startedHidden: true, keepReady: true, agreed: true, claudeInstalled: true));
        Assert.False(HelpSettings.StartsConversationHidden(startedHidden: false, keepReady: true, agreed: true, claudeInstalled: true));
        Assert.False(HelpSettings.StartsConversationHidden(startedHidden: true, keepReady: false, agreed: true, claudeInstalled: true));
        // Claude Code is never started before the user agreed.
        Assert.False(HelpSettings.StartsConversationHidden(startedHidden: true, keepReady: true, agreed: false, claudeInstalled: true));
        Assert.False(HelpSettings.StartsConversationHidden(startedHidden: true, keepReady: true, agreed: true, claudeInstalled: false));
    }

    [Fact]
    public void Keep_the_conversation_ready_is_off_at_first_and_round_trips()
    {
        // HE-D9: off at first, so that Claude Code never starts before the user acts unless they turned this on.
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "axHelp.json");
        var settings = HelpSettings.Load(path, out _);
        Assert.False(settings.KeepConversationReady);
        // A file from before the setting has it off too.
        File.WriteAllText(path, """{ "model": "opus" }""");
        Assert.False(HelpSettings.Load(path, out _).KeepConversationReady);

        settings.KeepConversationReady = true;
        settings.Save(path);
        Assert.Contains("\"keepConversationReady\": true", File.ReadAllText(path));
        Assert.True(HelpSettings.Load(path, out _).KeepConversationReady);
    }

    /// <summary>HE-2.1, HE-1.3: one conversation at a time in every order of start, press and quit.</summary>
    [Fact]
    public void A_press_hands_over_to_the_window_waits_for_a_started_process_and_starts_only_when_there_is_neither()
    {
        Assert.Equal(ConversationStep.HandOver, Conversation.Next(windowExists: true, startedProcessRuns: false));
        Assert.Equal(ConversationStep.HandOver, Conversation.Next(windowExists: true, startedProcessRuns: true));
        // Started and its window not there yet: wait, however long, never a second start.
        Assert.Equal(ConversationStep.Wait, Conversation.Next(windowExists: false, startedProcessRuns: true));
        Assert.Equal(ConversationStep.Start, Conversation.Next(windowExists: false, startedProcessRuns: false));

        Assert.Equal(ConversationEnd.AskToEnd, Conversation.AtQuit(windowExists: true, startedProcessRuns: true));
        Assert.Equal(ConversationEnd.AskToEnd, Conversation.AtQuit(windowExists: true, startedProcessRuns: false));
        // Quit before the started window exists: the process itself is ended, nothing is left unseen.
        Assert.Equal(ConversationEnd.EndProcess, Conversation.AtQuit(windowExists: false, startedProcessRuns: true));
        Assert.Equal(ConversationEnd.Nothing, Conversation.AtQuit(windowExists: false, startedProcessRuns: false));
    }
}
