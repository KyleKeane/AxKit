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
}
