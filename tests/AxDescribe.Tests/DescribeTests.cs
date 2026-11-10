using AxDescribe.Core;

namespace AxDescribe.Tests;

/// <summary>axDescribe's pure code (docs/axDescribe/SPEC.md DE-3, DE-5): the descriptions kept, their labels and matches, the settings file.</summary>
public sealed class DescribeTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axdescribe-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void The_last_ten_descriptions_are_kept_newest_first()
    {
        var settings = new DescribeSettings();
        for (var i = 0; i < 12; i++)
        {
            settings.Remember($"window {i}", "notepad", $"description {i}", $"s{i}", new DateTime(2026, 11, 9, 14, i, 0));
        }

        Assert.Equal(DescribeSettings.Kept, settings.Descriptions.Count);
        Assert.Equal("window 11", settings.Descriptions[0].Title);
        Assert.Equal("window 2", settings.Descriptions[^1].Title);
    }

    [Fact]
    public void The_label_is_the_time_and_the_title_or_the_program_and_the_matches_hold_every_typed_word()
    {
        var when = new DateTime(2026, 11, 9, 14, 2, 0);
        IReadOnlyList<Description> descriptions =
        [
            new("notes.md - axDown", "AxKit", "A text editor.", "s1", when),
            new(string.Empty, "notepad", "An empty window.", null, when.AddMinutes(3)),
        ];
        Assert.Equal("14:02 notes.md - axDown", DescribeLines.Label(descriptions[0]));
        Assert.Equal("14:05 notepad", DescribeLines.Label(descriptions[1]));
        Assert.Equal(2, DescribeLines.Matches(descriptions, string.Empty).Count);
        Assert.Equal(["14:05 notepad"], DescribeLines.Matches(descriptions, "note").Where(d => d.Title.Length == 0).Select(DescribeLines.Label));
        Assert.Equal(["notes.md - axDown"], DescribeLines.Matches(descriptions, "axDown").Select(d => d.Title));
        Assert.Empty(DescribeLines.Matches(descriptions, "zzqq"));
        Assert.Equal("14:02 notes.md - axDown, 1 of 2", DescribeLines.Spoken(descriptions[0], 0, 2));
        Assert.Equal("No earlier description matches zzqq", DescribeLines.NoMatch(" zzqq "));
    }

    [Fact]
    public void The_settings_round_trip_with_the_hotkey_default_and_the_descriptions()
    {
        var path = Path.Combine(_folder, "axDescribe.json");
        var settings = DescribeSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal("Windows+Alt+S", settings.Key.ToString());
        Assert.Empty(settings.Descriptions);
        // DE-3.3: nothing is said while a question is typed unless the file says so.
        Assert.False(settings.SayFirstMatch);

        settings.Remember("Untitled - Notepad", "notepad", "An empty window.", "s", new DateTime(2026, 11, 9, 12, 0, 0));
        // A picture file of the user's (DE-2.7) keeps its path; a window's description has none.
        settings.Remember("chart.png", PictureFile.Program, "A bar chart.", "s2", new DateTime(2026, 11, 9, 12, 5, 0), @"C:\pictures\chart.png");
        settings.Save(path);
        var loaded = DescribeSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Equal(2, loaded.Descriptions.Count);
        Assert.Equal("chart.png", loaded.Descriptions[0].Title);
        Assert.Equal(@"C:\pictures\chart.png", loaded.Descriptions[0].Picture);
        Assert.Equal("Untitled - Notepad", loaded.Descriptions[1].Title);
        Assert.Equal("s", loaded.Descriptions[1].SessionId);
        Assert.Null(loaded.Descriptions[1].Picture);
        // The file writes "+" as + (System.Text.Json's default escaping), so the hotkey is checked as read back.
        Assert.Equal("Windows+Alt+S", loaded.Key.ToString());
        Assert.Contains("\"hotkey\":", File.ReadAllText(path));

        // A file written before 5.6.0 has no picture: it reads as null.
        File.WriteAllText(path, """{"descriptions": [{"title": "old", "program": null, "answer": "Text.", "sessionId": null, "when": "2026-11-09T12:00:00"}]}""");
        var old = DescribeSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Null(Assert.Single(old.Descriptions).Picture);
    }

    [Fact]
    public void A_picture_file_is_one_of_the_kinds_claude_reads_and_anything_else_is_refused()
    {
        Assert.True(PictureFile.IsPicture(@"C:\pictures\chart.PNG"));
        Assert.True(PictureFile.IsPicture("photo.jpeg"));
        Assert.False(PictureFile.IsPicture("notes.txt"));
        Assert.False(PictureFile.IsPicture("picture"));
        Assert.Equal("notes.txt is not a picture axDescribe can describe: a .png, .jpg, .jpeg, .gif or .webp file", PictureFile.Refusal(@"C:\notes.txt"));

        Directory.CreateDirectory(_folder);
        var file = Path.Combine(_folder, "blocks.png");
        Assert.Equal(file + " was not found", PictureFile.Refusal(file));
        File.WriteAllBytes(file, [1]);
        Assert.Null(PictureFile.Refusal(file));

        Assert.Equal("Pictures (*.png, *.jpg, *.jpeg, *.gif, *.webp)|*.png;*.jpg;*.jpeg;*.gif;*.webp|All files (*.*)|*.*", PictureFile.Filter);
        Assert.Equal("picture file", PictureFile.Program);
    }
}
