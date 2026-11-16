using System.Security.Cryptography;
using System.Text;
using AxKit.Core;
using AxKit.Core.Ai;
using AxKit.Core.Mcp;

namespace AxKit.Tests;

/// <summary>
/// The seal on the settings that decide what is run or what Claude may read (docs/axKit/SPEC.md AX-5.6; repairs.md
/// R-53): the piece itself, AI help's settings (ai-help.md AH-1.1a) and the consents (AX-11.5). Every file is in a
/// folder of the test's own, where the piece keeps its key beside the file: nothing of the user's is read or written.
/// </summary>
public sealed class SettingsSealTests
{
    // ---- the piece ----

    [Fact]
    public void Fields_cannot_run_into_each_other_and_none_is_not_empty()
    {
        Assert.NotEqual(SettingsSeal.Text(["ab", "c"]), SettingsSeal.Text(["a", "bc"]));
        Assert.NotEqual(SettingsSeal.Text(["a\n1:b"]), SettingsSeal.Text(["a", "b"]));
        Assert.NotEqual(SettingsSeal.Text([null]), SettingsSeal.Text([string.Empty]));
        Assert.NotEqual(SettingsSeal.Text([]), SettingsSeal.Text([null]));
        Assert.Equal("2:ab\n-\n0:\n", SettingsSeal.Text(["ab", null, string.Empty]));
    }

    [Fact]
    public void The_key_file_of_a_test_is_beside_its_settings_file_and_axKits_own_is_in_its_local_folder()
    {
        using var temp = new TempFolder("axkit-seal");
        Assert.Equal(Path.Combine(temp.Path, SettingsSeal.KeyFileName), SettingsSeal.KeyFileOf(Path.Combine(temp.Path, "axTerminal.json")));
        Assert.Equal(AppPaths.SettingsSealFile, SettingsSeal.KeyFileOf(AppPaths.SettingsFile("axTerminal")));
        Assert.Equal(AppPaths.SettingsSealFile, SettingsSeal.KeyFileOf(AppPaths.ConsentFile));
        Assert.Equal(AppPaths.SettingsSealFile, SettingsSeal.KeyFileOf(AppPaths.AiHelpFile));
        Assert.Equal(Path.GetDirectoryName(AppPaths.McpKeyFile), Path.GetDirectoryName(AppPaths.SettingsSealFile));
    }

    [Fact]
    public void A_seal_is_for_one_file_and_one_text()
    {
        using var temp = new TempFolder("axkit-seal");
        var one = Path.Combine(temp.Path, "one.json");
        var two = Path.Combine(temp.Path, "two.json");
        var seal = SettingsSeal.Of(one, "text");
        Assert.NotEqual(string.Empty, seal);
        Assert.True(SettingsSeal.Checks(one, seal, "text", "default"));
        Assert.False(SettingsSeal.Checks(one, seal, "other", "default"));
        Assert.False(SettingsSeal.Checks(two, seal, "text", "default"));
        // The defaults need no seal: there is nothing in them to refuse.
        Assert.True(SettingsSeal.Checks(one, "nonsense", "default", "default"));
    }

    [Fact]
    public void A_file_without_a_seal_counts_until_this_install_has_sealed_it()
    {
        using var temp = new TempFolder("axkit-seal");
        var one = Path.Combine(temp.Path, "one.json");
        var two = Path.Combine(temp.Path, "two.json");
        // Never sealed, no key yet: a file from before the seal.
        Assert.True(SettingsSeal.Checks(one, string.Empty, "text", "default"));
        Assert.False(File.Exists(SettingsSeal.KeyFileOf(one)));

        // Sealed and written: from now on it must carry its seal; the other file, never sealed, still counts.
        var seal = SettingsSeal.Of(one, "text");
        SettingsSeal.Remember(one);
        Assert.True(SettingsSeal.Checks(one, seal, "text", "default"));
        Assert.False(SettingsSeal.Checks(one, string.Empty, "text", "default"));
        Assert.False(SettingsSeal.Checks(one, null, "text", "default"));
        Assert.True(SettingsSeal.Checks(two, string.Empty, "text", "default"));
        // The name is what counts, in any case.
        Assert.False(SettingsSeal.Checks(Path.Combine(temp.Path, "ONE.json"), string.Empty, "text", "default"));
    }

    [Fact]
    public void A_seal_found_on_a_file_puts_it_on_the_list()
    {
        using var temp = new TempFolder("axkit-seal");
        var one = Path.Combine(temp.Path, "one.json");
        // Sealed, the list not written (two windows at once, the write lost): the load that finds the seal lists it.
        var seal = SettingsSeal.Of(one, "text");
        Assert.True(SettingsSeal.Checks(one, string.Empty, "text", "default"));
        Assert.True(SettingsSeal.Checks(one, seal, "text", "default"));
        Assert.False(SettingsSeal.Checks(one, string.Empty, "text", "default"));
    }

    [Fact]
    public void A_key_file_that_cannot_be_read_refuses_everything_until_a_save_makes_a_new_one()
    {
        using var temp = new TempFolder("axkit-seal");
        var one = Path.Combine(temp.Path, "one.json");
        var two = Path.Combine(temp.Path, "two.json");
        var seal = SettingsSeal.Of(one, "text");
        SettingsSeal.Remember(one);

        // The key file damaged, or one of another user or computer (a profile copied from elsewhere).
        File.WriteAllText(SettingsSeal.KeyFileOf(one), "not a key");
        Assert.False(SettingsSeal.Checks(one, seal, "text", "default"));
        Assert.False(SettingsSeal.Checks(one, string.Empty, "text", "default"));
        Assert.False(SettingsSeal.Checks(two, string.Empty, "text", "default"));

        // The next save makes a new key; the list went with the old one, so every file must carry a seal from now on.
        var again = SettingsSeal.Of(one, "text");
        Assert.NotEqual(seal, again);
        Assert.True(SettingsSeal.Checks(one, again, "text", "default"));
        Assert.False(SettingsSeal.Checks(one, seal, "text", "default"));
        Assert.False(SettingsSeal.Checks(two, string.Empty, "text", "default"));
        Assert.True(SettingsSeal.Checks(two, SettingsSeal.Of(two, "text"), "text", "default"));
    }

    [Fact]
    public void A_sealed_file_without_its_key_file_is_refused()
    {
        using var temp = new TempFolder("axkit-seal");
        var one = Path.Combine(temp.Path, "one.json");
        var seal = SettingsSeal.Of(one, "text");
        // The settings roam and the key does not: the file reaches a computer that never sealed it.
        File.Delete(SettingsSeal.KeyFileOf(one));
        Assert.False(SettingsSeal.Checks(one, seal, "text", "default"));
    }

    [Fact]
    public void The_key_file_shows_neither_the_key_nor_the_names()
    {
        using var temp = new TempFolder("axkit-seal");
        var one = Path.Combine(temp.Path, "one.json");
        SettingsSeal.Of(one, "text");
        SettingsSeal.Remember(one);
        var text = File.ReadAllText(SettingsSeal.KeyFileOf(one));
        Assert.DoesNotContain("one.json", text);
        Assert.NotNull(UserSecret.Unprotect(text));
    }

    [Fact]
    public void A_refusal_is_told_from_a_file_that_could_not_be_read()
    {
        const string sentence = "Your things in axTest's settings were changed outside axKit and are not used.";
        Assert.Equal(sentence + " " + SettingsSeal.CopyKept, SettingsSeal.Refused(null, sentence));
        Assert.Equal(sentence + " " + SettingsSeal.CopyKept + "\nfile: a value was not understood", SettingsSeal.Refused("file: a value was not understood", sentence));
        Assert.True(SettingsSeal.IsRefusal(sentence));
        Assert.False(SettingsSeal.IsRefusal("C:\\x.json: 'x' is an invalid start of a value."));
    }

    [Fact]
    public void The_AI_tools_seal_is_the_value_it_was()
    {
        // McpAccess takes the HMAC from the shared piece (R-53); a file sealed by an earlier version must still check.
        var key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var settings = new McpSettings { Enabled = true, RunCommands = true, Clients = [new McpClient(@"C:\tools\claude.exe", "Claude Code", "2026-11-01 10:00")] };
        var text = "on run\nC:\\tools\\claude.exe\tClaude Code\t2026-11-01 10:00";
        Assert.Equal(Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(text))), McpAccess.SealOf(key, settings));
        Assert.Equal(Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes("challenge"))), McpAccess.Proof(key, "challenge"));
        Assert.True(McpAccess.Proves(key, "challenge", McpAccess.Proof(key, "challenge")));
        Assert.False(McpAccess.Proves(key, "challenge", McpAccess.Proof(key, "another")));
    }

    // ---- AI help's settings (AH-1.1a) ----

    private static string SavedAi(TempFolder temp)
    {
        var path = Path.Combine(temp.Path, "ai-help.json");
        new AiSettings { Enabled = true, Folders = [@"C:\notes"], Model = "a-model", ClaudePath = @"C:\tools\claude.exe", WorkingFolder = @"C:\claude" }.Save(path);
        return path;
    }

    [Fact]
    public void AI_help_sealed_loads_as_saved_and_a_change_through_axKit_is_sealed_again()
    {
        using var temp = new TempFolder("axkit-seal");
        var path = SavedAi(temp);
        Assert.True(SettingsText.HasSeal(path));
        var settings = AiSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.True(settings.Enabled);
        Assert.Equal([@"C:\notes"], settings.Folders);
        Assert.Equal(@"C:\tools\claude.exe", settings.ClaudePath);
        Assert.Equal(@"C:\claude", settings.WorkingFolder);

        settings.Folders.Add(@"C:\work");
        settings.Save(path);
        Assert.Equal([@"C:\notes", @"C:\work"], AiSettings.Load(path, out error).Folders);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("folders")]
    [InlineData("claudePath")]
    [InlineData("workingFolder")]
    [InlineData("enabled")]
    public void AI_help_changed_by_hand_is_not_used_and_the_model_stays(string field)
    {
        using var temp = new TempFolder("axkit-seal");
        var path = SavedAi(temp);
        if (field == "enabled")
        {
            // Saved off, then switched on by something else.
            var off = AiSettings.Load(path, out _);
            off.Enabled = false;
            off.Save(path);
        }

        SettingsText.Change(path, whole =>
        {
            switch (field)
            {
                case "folders":
                    whole["folders"]!.AsArray().Add(@"C:\Users");
                    break;
                case "enabled":
                    whole["enabled"] = true;
                    break;
                default:
                    whole[field] = @"C:\elsewhere\other.exe";
                    break;
            }
        });

        var settings = AiSettings.Load(path, out var error);
        Assert.Equal(AiSettings.NotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.True(SettingsSeal.IsRefusal(error!));
        Assert.False(settings.Enabled);
        Assert.Empty(settings.Folders);
        Assert.Null(settings.ClaudePath);
        Assert.Null(settings.WorkingFolder);
        Assert.Equal(AiSettings.DefaultWorkingFolder, settings.ResolvedWorkingFolder());
        Assert.Equal("a-model", settings.Model);
    }

    [Fact]
    public void AI_help_from_before_the_seal_is_accepted_sealed_by_the_next_save_and_then_must_keep_its_seal()
    {
        using var temp = new TempFolder("axkit-seal");
        var path = temp.File("ai-help.json", """{"enabled": true, "folders": ["C:\\notes"], "claudePath": "fake-claude.cmd"}""");
        var settings = AiSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.True(settings.Enabled);
        Assert.Equal([@"C:\notes"], settings.Folders);
        Assert.Equal(Path.Combine(temp.Path, "fake-claude.cmd"), settings.ResolvedClaudePath());

        settings.Save(path);
        Assert.True(SettingsText.HasSeal(path));
        Assert.True(AiSettings.Load(path, out error).Enabled);
        Assert.Null(error);

        SettingsText.RemoveSeal(path);
        settings = AiSettings.Load(path, out error);
        Assert.Equal(AiSettings.NotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.False(settings.Enabled);
        Assert.Empty(settings.Folders);
    }

    // ---- the consents (AX-11.5) ----

    [Fact]
    public void A_consent_another_program_added_is_not_the_users_agreement()
    {
        using var temp = new TempFolder("axkit-seal");
        var file = Path.Combine(temp.Path, "consent.json");
        Consent.Record(file, "axGIT", "Git", "5.9.0", new DateTime(2026, 11, 16));
        Assert.True(SettingsText.HasSeal(file));
        Assert.True(Consent.Given(file, "axGIT", "Git"));
        Assert.Null(Consent.Problem(file));

        SettingsText.Change(file, whole => whole["items"]!.AsArray().Add(new System.Text.Json.Nodes.JsonObject
        {
            ["app"] = "axTerminal", ["target"] = "Custom shell, command line other.exe", ["version"] = "5.9.0", ["when"] = "2026-11-16T00:00:00",
        }));
        Assert.False(Consent.Given(file, "axTerminal", "Custom shell, command line other.exe"));
        // The file as a whole is not axKit's any more: every app asks again, and the question says why.
        Assert.False(Consent.Given(file, "axGIT", "Git"));
        Assert.Empty(Consent.Read(file));
        Assert.Equal(Consent.NotUsed, Consent.Problem(file));

        // The next agreement writes the file anew, sealed, with that agreement alone.
        Consent.Record(file, "axGIT", "Git", "5.9.0", new DateTime(2026, 11, 16));
        Assert.Null(Consent.Problem(file));
        Assert.Single(Consent.Read(file));
    }

    [Fact]
    public void Consents_from_before_the_seal_count_until_the_next_one_seals_the_file()
    {
        using var temp = new TempFolder("axkit-seal");
        var file = temp.File("consent.json", """{"items":[{"app":"axGIT","target":"Git","version":"5.8.0","when":"2026-10-31T00:00:00"}]}""");
        Assert.True(Consent.Given(file, "axGIT", "Git"));
        Assert.Null(Consent.Problem(file));

        Consent.Record(file, "axTerminal", "Command Prompt", "5.9.0", new DateTime(2026, 11, 16));
        Assert.True(SettingsText.HasSeal(file));
        Assert.True(Consent.Given(file, "axGIT", "Git"));
        Assert.True(Consent.Given(file, "axTerminal", "Command Prompt"));

        SettingsText.RemoveSeal(file);
        Assert.False(Consent.Given(file, "axGIT", "Git"));
        Assert.Equal(Consent.NotUsed, Consent.Problem(file));
    }

    [Fact]
    public void A_withdrawn_consent_and_an_empty_file_need_no_question()
    {
        using var temp = new TempFolder("axkit-seal");
        var file = Path.Combine(temp.Path, "consent.json");
        Assert.Null(Consent.Problem(file));
        Consent.Record(file, "axGIT", "Git", "5.9.0", new DateTime(2026, 11, 16));
        Consent.Withdraw(file, "axGIT", "Git");
        Assert.Empty(Consent.Read(file));
        Assert.Null(Consent.Problem(file));
        // Emptied by hand: nothing is agreed to, which is what the file says, so nothing is refused.
        SettingsText.RemoveSeal(file);
        Assert.Null(Consent.Problem(file));
    }
}
