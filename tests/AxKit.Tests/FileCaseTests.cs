using AxKit.Core;

namespace AxKit.Tests;

/// <summary>A settings or log file left in another case by an earlier version takes the spelling the bundle writes (AX-5.1).</summary>
public sealed class FileCaseTests
{
    [Fact]
    public void A_file_in_another_case_is_renamed_and_a_matching_or_missing_one_is_left_alone()
    {
        using var temp = new TempFolder("axkit-case-tests");
        var old = Path.Combine(temp.Path, "axdown.json");
        File.WriteAllText(old, "{}");
        var wanted = Path.Combine(temp.Path, "axDown.json");

        FileCase.Match(wanted);
        Assert.Equal(["axDown.json"], Directory.GetFiles(temp.Path).Select(Path.GetFileName));
        Assert.Equal("{}", File.ReadAllText(wanted));

        // Already right: nothing happens; missing: nothing happens.
        FileCase.Match(wanted);
        Assert.Equal(["axDown.json"], Directory.GetFiles(temp.Path).Select(Path.GetFileName));
        FileCase.Match(Path.Combine(temp.Path, "axJSON.json"));
        Assert.Equal(["axDown.json"], Directory.GetFiles(temp.Path).Select(Path.GetFileName));
        FileCase.Match(Path.Combine(temp.Path, "nowhere", "axJSON.json"));
    }

    [Fact]
    public void Loading_a_settings_file_takes_its_case_from_the_path()
    {
        using var temp = new TempFolder("axkit-case-tests");
        File.WriteAllText(Path.Combine(temp.Path, "ai-help.json"), "{ \"enabled\": true }");
        var settings = SettingsFile.Load<AxKit.Core.Ai.AiSettings>(Path.Combine(temp.Path, "AI-Help.json"), out var error);
        Assert.Null(error);
        Assert.True(settings.Enabled);
        Assert.Equal(["AI-Help.json"], Directory.GetFiles(temp.Path).Select(Path.GetFileName));
    }
}
