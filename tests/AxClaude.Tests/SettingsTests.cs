using AxClaude.Core;
using AxClaude.Core.Transcript;
using AxKit.Core;

namespace AxClaude.Tests;

public class SettingsTests
{
    [Fact]
    public void Settings_round_trip_through_the_file()
    {
        var path = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"), "settings.json");
        try
        {
            var settings = new AppSettings { FontFamily = "Consolas", FontSize = 14, ReplySpeech = ReplySpeechMode.FirstLines, SpeakToolCalls = true, PtyColumns = 200, MarkerTimeStamps = true, JoinWrappedLines = false, WorkingMusic = true, BackgroundAlert = true };
            settings.RememberFolder(@"C:\one");
            settings.RememberFolder(@"C:\two");
            settings.RememberFolder(@"c:\ONE");
            settings.Window = new WindowPlacement { X = 10, Y = 20, Width = 800, Height = 600, Maximized = true };
            settings.Save(path);

            var loaded = AppSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.Equal("Consolas", loaded.FontFamily);
            Assert.Equal(14, loaded.FontSize);
            Assert.Equal(ReplySpeechMode.FirstLines, loaded.ReplySpeech);
            Assert.Contains("\"replySpeech\": \"firstLines\"", File.ReadAllText(path));
            Assert.DoesNotContain("speakReplies", File.ReadAllText(path));
            Assert.True(loaded.SpeakToolCalls);
            Assert.Equal(200, loaded.PtyColumns);
            Assert.True(loaded.MarkerTimeStamps);
            Assert.False(loaded.JoinWrappedLines);
            Assert.True(loaded.WorkingMusic);
            Assert.True(loaded.BackgroundAlert);
            Assert.Equal(@"c:\ONE", loaded.LastProjectFolder);
            Assert.Equal([@"c:\ONE", @"C:\two"], loaded.RecentFolders);
            Assert.True(loaded.Window!.Maximized);
            Assert.Equal(800, loaded.Window.Width);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void Missing_or_broken_file_falls_back_to_defaults()
    {
        var path = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"), "settings.json");
        var missing = AppSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal(240, missing.PtyColumns);
        Assert.True(missing.AnnounceBell);
        Assert.True(missing.JoinWrappedLines);
        Assert.False(missing.MarkerTimeStamps);
        Assert.False(missing.WorkingMusic);
        Assert.False(missing.BackgroundAlert);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            File.WriteAllText(path, "{ not json");
            var broken = AppSettings.Load(path, out error);
            Assert.NotNull(error);
            Assert.Equal(50, broken.PtyRows);

            File.WriteAllText(path, """{ "ptyColumns": 5, "fontSize": -3, "recentFolders": [" ", "C:\\x"], "window": { "width": 10, "height": 10 } }""");
            var sanitized = AppSettings.Load(path, out error);
            Assert.Null(error);
            Assert.Equal(240, sanitized.PtyColumns);
            Assert.Equal(0, sanitized.FontSize);
            Assert.Equal([@"C:\x"], sanitized.RecentFolders);
            Assert.Null(sanitized.Window);
            Assert.Equal(ReplySpeechMode.None, sanitized.ReplySpeech);

            // The 1.1 key: speaking replies meant every line.
            File.WriteAllText(path, """{ "speakReplies": true }""");
            var upgraded = AppSettings.Load(path, out error);
            Assert.Null(error);
            Assert.Equal(ReplySpeechMode.All, upgraded.ReplySpeech);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    [Fact]
    public void The_event_settings_are_on_at_first_and_keep_their_names_in_the_file()
    {
        // docs/axClaude/events-and-grounding.md EG-9.1, EG-9.2, EG-9.6: the two Developer switches, both on at first,
        // and the file-only word that puts the core back in shadow.
        var path = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"), "settings.json");
        try
        {
            var missing = AppSettings.Load(path, out var error);
            Assert.Null(error);
            Assert.True(missing.ClaudeHooks);
            Assert.True(missing.GroundInSessionFile);
            Assert.Equal("on", missing.EventCore);

            missing.Save(path);
            var text = File.ReadAllText(path);
            Assert.Contains("\"claudeHooks\": true", text);
            Assert.Contains("\"groundInSessionFile\": true", text);
            Assert.Contains("\"eventCore\": \"on\"", text);

            File.WriteAllText(path, """{ "claudeHooks": false, "groundInSessionFile": false, "eventCore": "shadow" }""");
            var chosen = AppSettings.Load(path, out error);
            Assert.Null(error);
            Assert.False(chosen.ClaudeHooks);
            Assert.False(chosen.GroundInSessionFile);
            Assert.Equal(AppSettings.EventCoreShadow, chosen.EventCore);

            // The word is read whatever its case; any other word, or none, is the first one.
            File.WriteAllText(path, """{ "eventCore": " Shadow " }""");
            Assert.Equal("shadow", AppSettings.Load(path, out error).EventCore);
            File.WriteAllText(path, """{ "eventCore": "off" }""");
            Assert.Equal(AppSettings.EventCoreOn, AppSettings.Load(path, out error).EventCore);
            File.WriteAllText(path, """{ "eventCore": null }""");
            Assert.Equal("on", AppSettings.Load(path, out error).EventCore);
            Assert.Null(error);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }
}
