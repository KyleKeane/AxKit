using AxPass.Core;

namespace AxPass.Tests;

/// <summary>axPass's settings (docs/axPass/SPEC.md PW-6): the values kept, nonsense mended, blank paths dropped.</summary>
public sealed class PassSettingsTests
{
    [Fact]
    public void Settings_keep_their_values_and_mend_nonsense()
    {
        using var temp = new TempFolder("axpass-settings");
        var path = Path.Combine(temp.Path, "axPass.json");
        var settings = PassSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal(PassSettings.DefaultLockMinutes, settings.LockMinutes);
        Assert.Null(settings.LastDatabase);
        Assert.Null(settings.CliPath);

        settings.LockMinutes = 30;
        settings.LastDatabase = Path.Combine(temp.Path, "mine.kdbx");
        settings.Save(path);
        var back = PassSettings.Load(path, out _);
        Assert.Equal(30, back.LockMinutes);
        Assert.Equal(Path.Combine(temp.Path, "mine.kdbx"), back.LastDatabase);

        File.WriteAllText(path, """{ "lockMinutes": 5000, "lastDatabase": "  ", "cliPath": "" }""");
        var mended = PassSettings.Load(path, out _);
        Assert.Equal(PassSettings.DefaultLockMinutes, mended.LockMinutes);
        Assert.Null(mended.LastDatabase);
        Assert.Null(mended.CliPath);
    }
}