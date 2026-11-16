using AxKit.Core;
using AxMCP.Core;

namespace AxMCP.Tests;

/// <summary>The seal over Your servers (SPEC.md MP-8.2, bundle AX-5.6; repairs.md R-53): a server something other than axKit wrote into the settings file is not listed, so never started.</summary>
public sealed class MCPSealTests
{
    private static string Saved(TempFolder temp)
    {
        var path = Path.Combine(temp.Path, "axMCP.json");
        var settings = new MCPSettings { FontSize = 14 };
        settings.Servers.Add(new OwnServer { Name = "mine", CommandLine = "node server.js", Folder = @"C:\work" });
        settings.Save(path);
        return path;
    }

    [Fact]
    public void A_sealed_file_loads_as_it_was_saved()
    {
        using var temp = new TempFolder("axmcp-seal");
        var path = Saved(temp);
        Assert.True(SettingsText.HasSeal(path));
        var settings = MCPSettings.Load(path, out var error);
        Assert.Null(error);
        var only = Assert.Single(settings.Servers);
        Assert.Equal("node server.js", only.CommandLine);

        // A rename and a second server through the window are sealed again by their save.
        only.Name = "renamed";
        settings.Servers.Add(new OwnServer { Name = "two", CommandLine = "two.exe" });
        settings.Save(path);
        settings = MCPSettings.Load(path, out error);
        Assert.Null(error);
        Assert.Equal(["renamed", "two"], settings.Servers.Select(server => server.Name));
    }

    [Theory]
    [InlineData("commandLine")]
    [InlineData("folder")]
    [InlineData("name")]
    public void A_server_changed_by_hand_is_not_used_and_the_other_settings_stay(string field)
    {
        using var temp = new TempFolder("axmcp-seal");
        var path = Saved(temp);
        SettingsText.Change(path, whole => whole["servers"]![0]![field] = @"C:\elsewhere\other.exe");

        var settings = MCPSettings.Load(path, out var error);
        Assert.Equal(MCPSettings.ServersNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.True(SettingsSeal.IsRefusal(error!));
        Assert.Empty(settings.Servers);
        Assert.Empty(settings.OwnServers());
        Assert.Equal(14, settings.FontSize);
    }

    [Fact]
    public void A_file_from_before_the_seal_is_accepted_sealed_by_the_next_save_and_then_must_keep_its_seal()
    {
        using var temp = new TempFolder("axmcp-seal");
        var path = temp.File("axMCP.json", """{"checkForUpdates": false, "servers": [{"name": "fake", "commandLine": "powershell -NoProfile -File fake.ps1"}]}""");
        var settings = MCPSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Single(settings.Servers);

        settings.Save(path);
        Assert.True(SettingsText.HasSeal(path));
        Assert.Single(MCPSettings.Load(path, out error).Servers);
        Assert.Null(error);

        SettingsText.RemoveSeal(path);
        settings = MCPSettings.Load(path, out error);
        Assert.Equal(MCPSettings.ServersNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.Empty(settings.Servers);
        Assert.False(settings.CheckForUpdates);
    }
}
