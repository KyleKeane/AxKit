using AxDescribe.Core;

namespace AxDescribe.Tests;

/// <summary>axDescribe's pure code (docs/axdescribe/SPEC.md): the list's lines, the title and the settings file.</summary>
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
    public void The_list_always_has_a_line()
    {
        Assert.Equal([DescribeLines.NothingOpen], DescribeLines.For(null));
        Assert.Equal(["C:\\work\\notes.md"], DescribeLines.For("C:\\work\\notes.md"));
    }

    [Fact]
    public void The_title_is_the_name_of_what_is_open()
    {
        Assert.Equal("Describe", DescribeLines.Title(null));
        Assert.Equal("notes.md", DescribeLines.Title("C:\\work\\notes.md"));
        Assert.Equal("work", DescribeLines.Title("C:\\work\\"));
    }

    [Fact]
    public void A_missing_settings_file_gives_the_defaults()
    {
        var settings = DescribeSettings.Load(Path.Combine(_folder, "axdescribe.json"), out var error);
        Assert.Null(error);
        Assert.NotNull(settings);
    }
}
