using AxHelp.Core;

namespace AxHelp.Tests;

/// <summary>axHelp's pure code (docs/axhelp/SPEC.md): the list's lines, the title and the settings file.</summary>
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
    public void The_list_always_has_a_line()
    {
        Assert.Equal([HelpLines.NothingOpen], HelpLines.For(null));
        Assert.Equal(["C:\\work\\notes.md"], HelpLines.For("C:\\work\\notes.md"));
    }

    [Fact]
    public void The_title_is_the_name_of_what_is_open()
    {
        Assert.Equal("Help", HelpLines.Title(null));
        Assert.Equal("notes.md", HelpLines.Title("C:\\work\\notes.md"));
        Assert.Equal("work", HelpLines.Title("C:\\work\\"));
    }

    [Fact]
    public void A_missing_settings_file_gives_the_defaults()
    {
        var settings = HelpSettings.Load(Path.Combine(_folder, "axhelp.json"), out var error);
        Assert.Null(error);
        Assert.NotNull(settings);
    }
}
