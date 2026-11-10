using AxHelp.Core;

namespace AxHelp.Tests;

/// <summary>axHelp's pure code (docs/axHelp/SPEC.md HE-3, HE-5): the exchanges kept, the matches and the settings file.</summary>
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
    public void The_last_ten_exchanges_are_kept_newest_first_and_a_question_asked_again_moves_up()
    {
        var settings = new HelpSettings();
        for (var i = 0; i < 12; i++)
        {
            settings.Remember($"question {i}", $"answer {i}", $"s{i}", new DateTime(2026, 11, 9, 10, i, 0));
        }

        Assert.Equal(HelpSettings.Kept, settings.Exchanges.Count);
        Assert.Equal("question 11", settings.Exchanges[0].Question);
        Assert.Equal("question 2", settings.Exchanges[^1].Question);

        settings.Remember("Question 5", "a new answer", "s5b", new DateTime(2026, 11, 9, 11, 0, 0));
        Assert.Equal(HelpSettings.Kept, settings.Exchanges.Count);
        Assert.Equal("Question 5", settings.Exchanges[0].Question);
        Assert.Equal("a new answer", settings.Exchanges[0].Answer);
        Assert.Equal("s5b", settings.Exchanges[0].SessionId);
        Assert.Single(settings.Exchanges, exchange => exchange.Question.Equals("question 5", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void The_matches_hold_every_typed_word_and_nothing_typed_lists_them_all()
    {
        IReadOnlyList<HelpExchange> exchanges =
        [
            new("What does Ctrl+Shift+W do", "Word wrap.", "s1", DateTime.Now),
            new("Where are my notes", "In C:\\notes.", null, DateTime.Now),
        ];
        Assert.Equal(2, HelpLines.Matches(exchanges, "  ").Count);
        Assert.Equal(["Where are my notes"], HelpLines.Matches(exchanges, "notes").Select(e => e.Question));
        Assert.Equal(["What does Ctrl+Shift+W do"], HelpLines.Matches(exchanges, "ctrl shift").Select(e => e.Question));
        Assert.Empty(HelpLines.Matches(exchanges, "zzqq"));
        Assert.Equal("What does Ctrl+Shift+W do, 1 of 2", HelpLines.Spoken(exchanges[0], 0, 2));
        Assert.Equal("No earlier question matches zzqq", HelpLines.NoMatch(" zzqq "));
    }

    [Fact]
    public void The_settings_round_trip_with_the_hotkey_default_and_the_exchanges()
    {
        var path = Path.Combine(_folder, "axHelp.json");
        var settings = HelpSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal("Windows+Alt+H", settings.Key.ToString());
        Assert.Empty(settings.Exchanges);

        settings.Remember("q", "a", "s", new DateTime(2026, 11, 9, 12, 0, 0));
        settings.Save(path);
        var loaded = HelpSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Single(loaded.Exchanges);
        Assert.Equal("q", loaded.Exchanges[0].Question);
        Assert.Equal("s", loaded.Exchanges[0].SessionId);
        Assert.Equal(new DateTime(2026, 11, 9, 12, 0, 0), loaded.Exchanges[0].When);
        // The file writes "+" as + (System.Text.Json's default escaping), so the hotkey is checked as read back.
        Assert.Equal("Windows+Alt+H", loaded.Key.ToString());
        Assert.Contains("\"hotkey\":", File.ReadAllText(path));
    }
}
