using AxKit.Core;

namespace AxKit.Tests;

public enum SampleKind
{
    One,
    TwoWords,
}

/// <summary>A settings class as every app writes one: plain properties with defaults.</summary>
public sealed class SampleSettings
{
    public string Name { get; set; } = "default";
    public int Size { get; set; } = 3;
    public SampleKind Kind { get; set; }
    public List<string> Items { get; set; } = [];
}

/// <summary>The shared read-and-write pattern of a settings file (docs/axKit/SPEC.md AX-5.3) and the take-over of an old file (AX-5.2).</summary>
public sealed class SettingsFileTests
{
    private static string TempFolder() => Path.Combine(Path.GetTempPath(), "axkit-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Round_trip_writes_camel_case_json_and_leaves_no_temporary_file()
    {
        var folder = TempFolder();
        var path = Path.Combine(folder, "sample.json");
        try
        {
            SettingsFile.Save(path, new SampleSettings { Name = "Consolas", Size = 14, Kind = SampleKind.TwoWords });
            var json = File.ReadAllText(path);
            Assert.Contains("\"name\": \"Consolas\"", json);
            Assert.Contains("\"kind\": \"twoWords\"", json);
            Assert.False(File.Exists(path + ".tmp"));

            var loaded = SettingsFile.Load<SampleSettings>(path, out var error);
            Assert.Null(error);
            Assert.Equal("Consolas", loaded.Name);
            Assert.Equal(14, loaded.Size);
            Assert.Equal(SampleKind.TwoWords, loaded.Kind);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Missing_file_gives_defaults_without_an_error()
    {
        var loaded = SettingsFile.Load<SampleSettings>(Path.Combine(TempFolder(), "sample.json"), out var error);
        Assert.Null(error);
        Assert.Equal("default", loaded.Name);
        Assert.Equal(3, loaded.Size);
    }

    [Fact]
    public void Broken_file_gives_defaults_and_names_the_file()
    {
        using var temp = new TempFolder("axkit-tests");
        var folder = temp.Path;
        var path = Path.Combine(folder, "sample.json");
        File.WriteAllText(path, "{ not json");
        var loaded = SettingsFile.Load<SampleSettings>(path, out var error);
        Assert.NotNull(error);
        Assert.Contains(path, error);
        Assert.Equal(3, loaded.Size);

        // The app saves soon after loading: the broken file is kept for mending, and the error says where.
        Assert.Equal("{ not json", File.ReadAllText(path + ".bad"));
        Assert.Contains(path + ".bad", error);
    }

    [Fact]
    public void A_file_that_cannot_be_opened_is_never_replaced_by_the_defaults()
    {
        // R-51: another program holds the file while the app starts; the app's next save must not write over it.
        using var temp = new TempFolder("axkit-tests");
        var path = Path.Combine(temp.Path, "sample.json");
        File.WriteAllText(path, "{ \"name\": \"mine\", \"size\": 7 }");
        var before = File.ReadAllBytes(path);
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var loaded = SettingsFile.Load<SampleSettings>(path, out var error);
            Assert.Equal("default", loaded.Name);
            Assert.NotNull(error);
            Assert.Contains(SettingsFile.NotSaved, error);
            Assert.False(File.Exists(path + ".bad"));
        }

        SettingsFile.Save(path, new SampleSettings());
        Assert.Equal(before, File.ReadAllBytes(path));

        // Read once it is free: the settings are the user's again, and saving works again.
        var again = SettingsFile.Load<SampleSettings>(path, out var none);
        Assert.Null(none);
        Assert.Equal("mine", again.Name);
        again.Size = 9;
        SettingsFile.Save(path, again);
        Assert.Equal(9, SettingsFile.Load<SampleSettings>(path, out _).Size);
    }

    [Fact]
    public void One_value_that_does_not_read_costs_that_value_alone()
    {
        // R-52: a typed word where a number belongs, and a kind that does not exist.
        using var temp = new TempFolder("axkit-tests");
        var path = Path.Combine(temp.Path, "sample.json");
        File.WriteAllText(path, "{ \"name\": \"mine\", \"size\": \"wide\", \"kind\": \"twoWords\" }");
        var loaded = SettingsFile.Load<SampleSettings>(path, out var error);
        Assert.Equal("mine", loaded.Name);
        Assert.Equal(3, loaded.Size);
        Assert.Equal(SampleKind.TwoWords, loaded.Kind);
        Assert.NotNull(error);
        Assert.Contains("at their defaults: size.", error);
        Assert.Contains(path + ".bad", error);
        Assert.True(File.Exists(path + ".bad"));

        File.WriteAllText(path, "{ \"name\": \"mine\", \"size\": 5, \"kind\": \"no such kind\" }");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));
        loaded = SettingsFile.Load<SampleSettings>(path, out error);
        Assert.Equal(5, loaded.Size);
        Assert.Equal(SampleKind.One, loaded.Kind);
        Assert.Contains("at their defaults: kind.", error);
    }

    [Fact]
    public void Old_file_is_copied_once_and_never_over_a_new_one()
    {
        using var temp = new TempFolder("axkit-tests");
        var folder = temp.Path;
        var oldPath = Path.Combine(folder, "old", "settings.json");
        var path = Path.Combine(folder, "new", "app.json");
        Directory.CreateDirectory(Path.GetDirectoryName(oldPath)!);
        SettingsFile.TakeOverOldFile(oldPath, path);
        Assert.False(File.Exists(path));

        File.WriteAllText(oldPath, "{ \"size\": 7 }");
        SettingsFile.TakeOverOldFile(oldPath, path);
        Assert.Equal(7, SettingsFile.Load<SampleSettings>(path, out _).Size);
        Assert.True(File.Exists(oldPath));

        File.WriteAllText(path, "{ \"size\": 9 }");
        SettingsFile.TakeOverOldFile(oldPath, path);
        Assert.Equal(9, SettingsFile.Load<SampleSettings>(path, out _).Size);
    }

    /// <summary>Another window's save, as the file's text: its settings are written to another file and the text put in this one's place, so that this process's view of the file stays the first window's.</summary>
    private static void SavedByAnotherWindow(TempFolder temp, string path, SampleSettings settings)
    {
        var other = Path.Combine(temp.Path, "other.json");
        SettingsFile.Save(other, settings);
        File.WriteAllText(path, File.ReadAllText(other));
    }

    [Fact]
    public void A_save_keeps_what_another_window_saved_since_this_one_loaded_the_file()
    {
        // R-62 (AX-5.7): two windows of one app; each save keeps the other's changes and its own.
        using var temp = new TempFolder("axkit-tests");
        var path = Path.Combine(temp.Path, "sample.json");
        SettingsFile.Save(path, new SampleSettings { Name = "first", Size = 5, Items = ["a"] });
        var mine = SettingsFile.Load<SampleSettings>(path, out _);

        // The other window changed the name and added to the list; this one changes the size alone.
        SavedByAnotherWindow(temp, path, new SampleSettings { Name = "theirs", Size = 5, Items = ["a", "b"] });
        mine.Size = 9;
        SettingsFile.Save(path, mine);
        var now = SettingsFile.Load<SampleSettings>(path, out var error);
        Assert.Null(error);
        Assert.Equal("theirs", now.Name);
        Assert.Equal(9, now.Size);
        Assert.Equal(["a", "b"], now.Items);
    }

    [Fact]
    public void A_later_save_of_the_same_window_does_not_take_back_what_the_other_window_changed()
    {
        // This window's memory still holds the old name after the first merge: it is not one this window changed.
        using var temp = new TempFolder("axkit-tests");
        var path = Path.Combine(temp.Path, "sample.json");
        SettingsFile.Save(path, new SampleSettings { Name = "first", Size = 5 });
        var mine = SettingsFile.Load<SampleSettings>(path, out _);
        SavedByAnotherWindow(temp, path, new SampleSettings { Name = "theirs", Size = 5 });
        mine.Size = 9;
        SettingsFile.Save(path, mine);
        mine.Kind = SampleKind.TwoWords;
        SettingsFile.Save(path, mine);

        var now = SettingsFile.Load<SampleSettings>(path, out _);
        Assert.Equal("theirs", now.Name);
        Assert.Equal(9, now.Size);
        Assert.Equal(SampleKind.TwoWords, now.Kind);

        // A setting this window changed and changed back is this window's still: the file's later value loses.
        mine = SettingsFile.Load<SampleSettings>(path, out _);
        mine.Name = "mine";
        SettingsFile.Save(path, mine);
        SavedByAnotherWindow(temp, path, new SampleSettings { Name = "other again", Size = 9, Kind = SampleKind.TwoWords });
        mine.Name = "theirs";
        SettingsFile.Save(path, mine);
        Assert.Equal("theirs", SettingsFile.Load<SampleSettings>(path, out _).Name);
    }

    [Fact]
    public void A_list_both_windows_changed_is_this_windows_whole()
    {
        // The limit AX-5.7 states: lists are not merged.
        using var temp = new TempFolder("axkit-tests");
        var path = Path.Combine(temp.Path, "sample.json");
        SettingsFile.Save(path, new SampleSettings { Items = ["a"] });
        var mine = SettingsFile.Load<SampleSettings>(path, out _);
        SavedByAnotherWindow(temp, path, new SampleSettings { Items = ["a", "theirs"] });
        mine.Items.Add("mine");
        SettingsFile.Save(path, mine);
        Assert.Equal(["a", "mine"], SettingsFile.Load<SampleSettings>(path, out _).Items);
    }

    [Fact]
    public void A_file_that_is_no_object_or_holds_a_value_that_does_not_read_gives_nothing_to_a_save()
    {
        using var temp = new TempFolder("axkit-tests");
        var path = Path.Combine(temp.Path, "sample.json");
        SettingsFile.Save(path, new SampleSettings { Name = "first" });
        var mine = SettingsFile.Load<SampleSettings>(path, out _);
        mine.Size = 9;

        File.WriteAllText(path, "not json at all");
        SettingsFile.Save(path, mine);
        Assert.Equal("first", SettingsFile.Load<SampleSettings>(path, out var error).Name);
        Assert.Null(error);

        mine = SettingsFile.Load<SampleSettings>(path, out _);
        File.WriteAllText(path, "{ \"name\": \"theirs\", \"size\": \"large\" }");
        mine.Kind = SampleKind.TwoWords;
        SettingsFile.Save(path, mine);
        var now = SettingsFile.Load<SampleSettings>(path, out error);
        Assert.Null(error);
        Assert.Equal("first", now.Name);
        Assert.Equal(9, now.Size);
    }

    [Fact]
    public void The_merge_takes_the_files_value_unless_this_window_changed_the_member()
    {
        var merged = SettingsFile.Merged(
            view: """{"a": 1, "b": [1], "c": "x", "gone": true, "retired": 1}""",
            mine: """{"a": 1, "b": [1, 2], "c": "x", "gone": true, "new": 5}""",
            file: """{"A": 2, "b": [1, 3], "c": "x", "added": "kept", "retired": 1}""")!;
        Assert.Equal(2, merged["a"]!.GetValue<int>());
        Assert.Equal("[1,2]", merged["b"]!.ToJsonString());
        Assert.Equal("x", merged["c"]!.GetValue<string>());
        // A member the file lost keeps this window's value; one this window gained is its own; one only the file has is kept.
        Assert.True(merged["gone"]!.GetValue<bool>());
        Assert.Equal(5, merged["new"]!.GetValue<int>());
        Assert.Equal("kept", merged["added"]!.GetValue<string>());
        // A member this window held as loaded and writes no longer stays gone.
        Assert.False(merged.ContainsKey("retired"));

        Assert.Null(SettingsFile.Merged("{\"a\": 1}", "{\"a\": 2}", "{\"a\": 1}"));
        Assert.Null(SettingsFile.Merged("{\"a\": 1}", "{\"a\": 1}", "[1]"));
    }

    [Fact]
    public void A_file_that_cannot_be_written_is_said_once_in_a_run_with_the_reason()
    {
        // R-63 (AX-5.9): a read-only file refuses the save; the window says the sentence once and logs every time.
        using var temp = new TempFolder("axkit-tests");
        var path = Path.Combine(temp.Path, "sample.json");
        SettingsFile.Save(path, new SampleSettings { Name = "first" });
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            var refused = Assert.ThrowsAny<Exception>(() => SettingsFile.Save(path, new SampleSettings { Name = "second" }));
            Assert.True(refused is IOException or UnauthorizedAccessException);
            Assert.Equal(SettingsFile.NotWritten(refused.Message), SettingsFile.NotWrittenOnce(refused.Message));
            Assert.Null(SettingsFile.NotWrittenOnce(refused.Message));
            Assert.Equal("first", SettingsFile.Load<SampleSettings>(path, out _).Name);
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }

        // The reason ends with one full stop, whether the system gave one or not.
        Assert.Equal(SettingsFile.NotWritten("The disk is full"), SettingsFile.NotWritten("The disk is full. "));
        Assert.Contains(": The disk is full. ", SettingsFile.NotWritten("The disk is full"));
    }
}
