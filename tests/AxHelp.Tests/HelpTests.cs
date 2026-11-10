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
        Assert.Equal("Windows+Alt+H", settings.Key.ToString());
        Assert.Equal("Windows+Alt+S", settings.ScreenshotKey.ToString());
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
        Assert.Equal("Windows+Alt+S", old.ScreenshotKey.ToString());
        Assert.Equal(HelpSettings.FastModel, old.Model);
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

        File.WriteAllText(Path.Combine(_folder, "CLAUDE.md"), "my own");
        Assert.Empty(WorkingFolderFiles.WriteMissing(_folder));
        Assert.Equal("my own", File.ReadAllText(Path.Combine(_folder, "CLAUDE.md")));
    }

    [Fact]
    public void The_conversation_starts_on_the_working_folder_titled_axHelp_with_the_model()
    {
        Assert.Equal([@"C:\w", "--title", "axHelp", "--model", "haiku"], HelpSettings.ConversationArguments(@"C:\w", "haiku", null));
        Assert.Equal([@"C:\w", "--title", "axHelp", "--model", "opus", "--message", "Screenshot: a.png "], HelpSettings.ConversationArguments(@"C:\w", "opus", "Screenshot: a.png "));
    }
}
