using AxProcess.Core;

namespace AxProcess.Tests;

/// <summary>axProcess's settings (docs/axProcess/SPEC.md PR-7): the sort order kept; a value outside the choices goes back to Memory.</summary>
public sealed class ProcessSettingsTests
{
    [Fact]
    public void The_sort_order_is_kept_and_nonsense_mended()
    {
        using var temp = new TempFolder("axprocess-settings");
        var path = Path.Combine(temp.Path, "axProcess.json");
        var settings = ProcessSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal(ProcessSort.Memory, settings.SortBy);

        settings.SortBy = ProcessSort.Name;
        settings.Save(path);
        Assert.Equal(ProcessSort.Name, ProcessSettings.Load(path, out _).SortBy);

        File.WriteAllText(path, """{ "sortBy": 42 }""");
        Assert.Equal(ProcessSort.Memory, ProcessSettings.Load(path, out _).SortBy);
    }
}