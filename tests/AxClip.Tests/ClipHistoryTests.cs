using AxClip.Core;

namespace AxClip.Tests;

/// <summary>The history (docs/axClip/SPEC.md CP-2.3, CP-2.5, CP-3), what a copy is kept for (CP-3.1) and the settings (CP-6.1).</summary>
public sealed class ClipHistoryTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 14, 0, 0, DateTimeKind.Local);

    [Fact]
    public void A_copy_is_the_newest_and_a_copy_again_moves_it_to_the_top()
    {
        var history = new ClipHistory();
        history.Add("one", Now);
        history.Add("two", Now.AddMinutes(1));
        history.Add("one", Now.AddMinutes(2));
        Assert.Equal(["one", "two"], history.Items.Select(item => item.Text));
        Assert.Equal(Now.AddMinutes(2), history.Items[0].Copied);
    }

    [Fact]
    public void The_history_keeps_the_newest_two_hundred()
    {
        var history = new ClipHistory();
        for (var i = 0; i < ClipHistory.MaxItems + 5; i++)
        {
            history.Add($"item {i}", Now.AddSeconds(i));
        }

        Assert.Equal(ClipHistory.MaxItems, history.Items.Count);
        Assert.Equal($"item {ClipHistory.MaxItems + 4}", history.Items[0].Text);
        Assert.Equal("item 5", history.Items[^1].Text);
    }

    [Fact]
    public void A_label_is_the_first_line_with_text_squeezed_and_cut()
    {
        Assert.Equal("git status --porcelain", new ClipEntry("git status --porcelain", Now).Label);
        Assert.Equal("First line, 3 lines", new ClipEntry("\r\n  First   line  \r\nsecond\r\nthird\r\n", Now).Label);
        var long_ = new ClipEntry(new string('a', 200), Now).Label;
        Assert.Equal(ClipEntry.LabelLength + 1, long_.Length);
        Assert.EndsWith("…", long_);
        Assert.Equal(3, new ClipEntry("a\nb\nc\n", Now).LineCount);
    }

    [Fact]
    public void Typing_narrows_to_the_items_with_every_word_newest_first()
    {
        var history = new ClipHistory();
        history.Add("export TOKEN=abc", Now);
        history.Add("git status", Now.AddMinutes(1));
        history.Add("Café au lait token", Now.AddMinutes(2));
        Assert.Equal(["Café au lait token", "export TOKEN=abc"], history.Find("token").Select(item => item.Text));
        Assert.Equal(["Café au lait token"], history.Find("cafe TOKEN").Select(item => item.Text));
        Assert.Equal(3, history.Find("  ").Count);
        Assert.Equal("Nothing in the clipboard history matches xyz", history.Nothing(" xyz "));
        Assert.Equal("git status, 2 of 3", ClipHistory.Spoken(history.Items[1], 1, 3));
        Assert.Equal("The clipboard history is empty", new ClipHistory().Nothing("x"));
    }

    [Fact]
    public void Remove_and_clear()
    {
        var history = new ClipHistory([new ClipEntry("a", Now), new ClipEntry("b", Now), new ClipEntry("a", Now)]);
        Assert.Equal(["a", "b"], history.Items.Select(item => item.Text));
        history.Remove(history.Items[0]);
        Assert.Equal(["b"], history.Items.Select(item => item.Text));
        history.Clear();
        Assert.Empty(history.Items);
    }

    [Theory]
    [InlineData("text", false, null, false, true)]
    [InlineData("text", true, null, false, false)]
    [InlineData("text", false, 0, false, false)]
    [InlineData("text", false, 1, false, true)]
    [InlineData("text", false, null, true, false)]
    [InlineData("   ", false, null, false, false)]
    [InlineData("", false, null, false, false)]
    [InlineData(null, false, null, false, false)]
    public void A_copy_is_kept_unless_marked_paused_or_empty(string? text, bool excluded, int? historyValue, bool paused, bool kept) =>
        Assert.Equal(kept, ClipPolicy.Keeps(text, excluded, historyValue, paused));

    [Fact]
    public void A_copy_over_a_million_characters_is_not_kept() =>
        Assert.False(ClipPolicy.Keeps(new string('x', ClipPolicy.MaxLength + 1), false, null, false));

    [Fact]
    public void The_settings_keep_the_history_only_while_asked()
    {
        using var temp = new TempFolder("axclip-tests");
        var folder = temp.Path;
        var path = Path.Combine(folder, "axClip.json");
        var settings = ClipSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal("Ctrl+Shift+Comma", settings.HotkeyText);
        Assert.False(settings.KeepHistory);
        settings.Keep([new ClipEntry("secret", Now)]);
        Assert.Empty(settings.History);

        settings.KeepHistory = true;
        settings.Keep([new ClipEntry("kept", Now)]);
        settings.Save(path);
        var back = ClipSettings.Load(path, out _);
        Assert.Equal("kept", back.History.Single().Text);
        // S-21: the file holds the history only encrypted for this Windows account.
        Assert.DoesNotContain("kept", File.ReadAllText(path), StringComparison.Ordinal);

        File.WriteAllText(path, """{ "hotkey": "Shift+V", "keepHistory": false, "history": [ { "text": "left over", "copied": "2026-10-23T14:00:00" } ] }""");
        var mended = ClipSettings.Load(path, out _);
        Assert.Equal("Ctrl+Shift+Comma", mended.HotkeyText);
        Assert.Empty(mended.History);
    }

    /// <summary>CP-D1: Windows+Alt+V, the key until 2026-11-14, Ctrl+Shift+Semicolon, until 2026-11-23 (axTemplate's now), and Ctrl+Shift+Apostrophe, until 2026-11-24, are taken over to the new one and saved so; a key the user chose is kept.</summary>
    [Fact]
    public void A_file_with_the_retired_hotkey_is_taken_over_and_a_chosen_key_is_kept()
    {
        using var temp = new TempFolder("axclip-tests");
        var path = Path.Combine(temp.Path, "axClip.json");
        File.WriteAllText(path, """{ "hotkey": "Ctrl+Shift+Semicolon" }""");
        Assert.Equal("Ctrl+Shift+Comma", ClipSettings.Load(path, out _).HotkeyText);
        // Ctrl+Shift+Apostrophe, the key from 2026-11-23 to 2026-11-24, given to axAutomate's launcher since.
        File.WriteAllText(path, """{ "hotkey": "Ctrl+Shift+Apostrophe" }""");
        Assert.Equal("Ctrl+Shift+Comma", ClipSettings.Load(path, out _).HotkeyText);
        File.WriteAllText(path, """{ "hotkey": "Windows+Alt+V", "sayFirstMatch": false }""");
        var settings = ClipSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal("Ctrl+Shift+Comma", settings.HotkeyText);
        Assert.Equal(new AxKit.Core.Hotkey(AxKit.Core.Hotkey.Control | AxKit.Core.Hotkey.Shift, 0xBC), settings.Key);
        // Nothing else of the file changes.
        Assert.False(settings.SayFirstMatch);

        settings.Save(path);
        // A plus sign may be written escaped, so only the key's name is looked for.
        var text = File.ReadAllText(path);
        Assert.Contains("Comma", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Alt", text, StringComparison.Ordinal);
        Assert.Equal("Ctrl+Shift+Comma", ClipSettings.Load(path, out _).HotkeyText);

        File.WriteAllText(path, """{ "hotkey": "Ctrl+Alt+Shift+V" }""");
        Assert.Equal("Ctrl+Alt+Shift+V", ClipSettings.Load(path, out _).HotkeyText);
        File.WriteAllText(path, """{ "hotkey": "Windows+Alt+C" }""");
        Assert.Equal("Windows+Alt+C", ClipSettings.Load(path, out _).HotkeyText);
    }

    /// <summary>CP-3.6 (bundle repairs.md R-69): a kept history Windows does not give back is kept once as .bad, the history starts empty and the settings say so for one start.</summary>
    [Theory]
    [InlineData("bm90IHByb3RlY3RlZCBmb3IgdGhpcyBhY2NvdW50")]
    [InlineData("not even base64")]
    public void A_kept_history_that_cannot_be_opened_is_kept_once_as_bad_and_is_said(string kept)
    {
        using var temp = new TempFolder("axclip-tests");
        var path = Path.Combine(temp.Path, "axClip.json");
        var text = $$"""{ "hotkey": "Ctrl+Alt+Shift+V", "keepHistory": true, "protectedHistory": "{{kept}}" }""";
        File.WriteAllText(path, text);

        var settings = ClipSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.True(settings.HistoryNotRead);
        Assert.Empty(settings.History);
        Assert.True(settings.KeepHistory);
        Assert.Equal("Ctrl+Alt+Shift+V", settings.HotkeyText);
        Assert.Equal(text, File.ReadAllText(path + ".bad"));

        // Read again before a save: the copy is not made again.
        var copied = File.GetLastWriteTimeUtc(path + ".bad");
        Assert.True(ClipSettings.Load(path, out _).HistoryNotRead);
        Assert.Equal(copied, File.GetLastWriteTimeUtc(path + ".bad"));

        // The save writes the empty history, so the next start has nothing to say, and the copy stays as it was.
        settings.Keep([new ClipEntry("new", Now)]);
        settings.Save(path);
        var back = ClipSettings.Load(path, out _);
        Assert.False(back.HistoryNotRead);
        Assert.Equal("new", back.History.Single().Text);
        Assert.Equal(text, File.ReadAllText(path + ".bad"));
    }

    /// <summary>CP-3.6: a history this account protected that is no list of items is not read either; one that is off or absent says nothing.</summary>
    [Fact]
    public void What_Windows_gives_back_must_be_a_list_of_items_and_no_kept_history_says_nothing()
    {
        using var temp = new TempFolder("axclip-tests");
        var path = Path.Combine(temp.Path, "axClip.json");
        var notAList = AxKit.Core.UserSecret.Protect("{ \"text\": 5 }");
        File.WriteAllText(path, $$"""{ "keepHistory": true, "protectedHistory": "{{notAList}}" }""");
        Assert.True(ClipSettings.Load(path, out _).HistoryNotRead);

        File.WriteAllText(path, """{ "keepHistory": true }""");
        Assert.False(ClipSettings.Load(path, out _).HistoryNotRead);
        File.WriteAllText(path, """{ "keepHistory": false, "protectedHistory": "bm90IHByb3RlY3RlZA==" }""");
        Assert.False(ClipSettings.Load(path, out _).HistoryNotRead);
        Assert.Equal("The kept clipboard history could not be read on this computer and starts empty.", ClipSettings.HistoryNotReadText);
    }

    /// <summary>CP-3.6: the kept total has a limit, and the oldest items are the ones left out.</summary>
    [Fact]
    public void The_kept_history_has_a_limit_and_the_oldest_are_dropped_first()
    {
        Assert.Equal(8 * 1024 * 1024, ClipSettings.MaxKeptCharacters);
        List<ClipEntry> newestFirst = [new("aaaa", Now), new("bbbb", Now), new("cc", Now), new("d", Now)];
        Assert.Equal(["aaaa", "bbbb"], ClipSettings.WithinTheLimit(newestFirst, maxCharacters: 9).Select(entry => entry.Text));
        // No newer item is kept behind a dropped one: "d" would fit, and is older than "cc", which does not.
        Assert.Equal(["aaaa", "bbbb", "cc"], ClipSettings.WithinTheLimit(newestFirst, maxCharacters: 10).Select(entry => entry.Text));
        Assert.Equal(4, ClipSettings.WithinTheLimit(newestFirst).Count);

        // Ten copies of a million characters: eight are kept, the newest ones, through a save and a load.
        using var temp = new TempFolder("axclip-tests");
        var path = Path.Combine(temp.Path, "axClip.json");
        var settings = ClipSettings.Load(path, out _);
        settings.KeepHistory = true;
        settings.Keep(Enumerable.Range(0, 10).Select(i => new ClipEntry(new string((char)('a' + i), ClipPolicy.MaxLength), Now)));
        Assert.Equal(8, settings.History.Count);
        settings.Save(path);
        var back = ClipSettings.Load(path, out _);
        Assert.Equal("abcdefgh", string.Concat(back.History.Select(entry => entry.Text[0])));
    }
}
