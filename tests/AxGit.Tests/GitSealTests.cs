using AxGit.Core;
using AxKit.Core;

namespace AxGit.Tests;

/// <summary>
/// The seal over axGIT's trusted repositories and what each trust is for (SPEC.md AG-1.6, AG-8.1a, bundle AX-5.6;
/// repairs.md R-53): a repository something other than axKit wrote into the list is not trusted, and a trusted one
/// that would run something else than when it was trusted is asked about again. A trust is kept with the scan's
/// mark (<see cref="TrustScan.Mark"/>); here marks of lines stand for it.
/// </summary>
public sealed class GitSealTests
{
    private static string M(params string[] lines) => GitTrust.Mark(lines);

    private static readonly string Hook = M("hook post-checkout");

    private static string Saved(TempFolder temp)
    {
        var path = Path.Combine(temp.Path, "axGIT.json");
        var settings = new GitSettings { FontSize = 14 };
        settings.RememberRepository(@"C:\repo");
        settings.Trust(@"C:\repo", Hook);
        settings.Save(path);
        return path;
    }

    [Fact]
    public void A_sealed_file_loads_as_it_was_saved()
    {
        using var temp = new TempFolder("axgit-seal");
        var path = Saved(temp);
        Assert.True(SettingsText.HasSeal(path));
        Assert.DoesNotContain("trustedRepositories", File.ReadAllText(path));

        var settings = GitSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.True(settings.Trusts(@"c:\REPO", Hook));

        // Trusting another repository through the window is sealed again by its save.
        Assert.True(settings.Trust(@"C:\other", M("core.fsmonitor")));
        settings.Save(path);
        settings = GitSettings.Load(path, out error);
        Assert.Null(error);
        Assert.True(settings.Trusts(@"C:\other", M("core.fsmonitor")));
        Assert.True(settings.Trusts(@"C:\repo", Hook));
    }

    [Fact]
    public void A_trusted_repository_that_gains_a_hook_is_asked_about_again()
    {
        var settings = new GitSettings();
        Assert.False(settings.Trusts(@"C:\repo", Hook));
        Assert.False(settings.WasTrusted(@"C:\repo"));

        Assert.True(settings.Trust(@"C:\repo", Hook));
        Assert.True(settings.Trusts(@"C:\repo", Hook));
        Assert.False(settings.Trust(@"C:\repo", Hook));

        // One hook more, one configured program more, or another hook in its place: not what was trusted.
        Assert.False(settings.Trusts(@"C:\repo", M("hook post-checkout", "hook pre-commit")));
        Assert.False(settings.Trusts(@"C:\repo", M("core.fsmonitor", "hook post-checkout")));
        Assert.False(settings.Trusts(@"C:\repo", M("hook pre-push")));
        Assert.True(settings.WasTrusted(@"C:\repo"));

        // Trusted again with what it runs now: one entry, for the new mark.
        Assert.True(settings.Trust(@"C:\repo", M("hook post-checkout", "hook pre-commit")));
        Assert.Single(settings.Trusted);
        Assert.True(settings.Trusts(@"C:\repo", M("hook pre-commit", "hook post-checkout")));
        Assert.False(settings.Trusts(@"C:\repo", Hook));
    }

    [Fact]
    public void The_mark_is_of_the_lines_whatever_their_order_and_fields_cannot_run_together()
    {
        Assert.Equal(M("a", "b"), M("B", "A"));
        Assert.NotEqual(M("ab"), M("a", "b"));
        Assert.NotEqual(M(), M("a"));
        Assert.Equal(64, Hook.Length);
    }

    [Fact]
    public void A_repository_added_by_hand_is_not_trusted_and_the_other_settings_stay()
    {
        using var temp = new TempFolder("axgit-seal");
        var path = Saved(temp);
        SettingsText.Change(path, whole => whole["trusted"]!.AsArray().Add(new System.Text.Json.Nodes.JsonObject { ["path"] = @"C:\elsewhere", ["risks"] = string.Empty }));

        var settings = GitSettings.Load(path, out var error);
        Assert.Equal(GitSettings.TrustNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.True(SettingsSeal.IsRefusal(error!));
        Assert.Empty(settings.Trusted);
        Assert.False(settings.Trusts(@"C:\elsewhere", Hook));
        Assert.False(settings.Trusts(@"C:\repo", Hook));
        Assert.Equal(14, settings.FontSize);
        Assert.Equal([@"C:\repo"], settings.RecentRepositories);

        // The older key written into a sealed file is refused the same way.
        path = Saved(temp);
        SettingsText.Change(path, whole => whole["trustedRepositories"] = new System.Text.Json.Nodes.JsonArray(@"C:\elsewhere"));
        settings = GitSettings.Load(path, out error);
        Assert.Equal(GitSettings.TrustNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.False(settings.Trusts(@"C:\elsewhere", Hook));

        // A mark changed by hand, to make the trust fit other programs: refused.
        path = Saved(temp);
        SettingsText.Change(path, whole => whole["trusted"]![0]!["risks"] = M("hook pre-push"));
        settings = GitSettings.Load(path, out error);
        Assert.Equal(GitSettings.TrustNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.False(settings.Trusts(@"C:\repo", M("hook pre-push")));
    }

    [Fact]
    public void A_file_from_before_the_seal_keeps_its_trusts_binds_them_and_is_sealed_by_the_next_save()
    {
        using var temp = new TempFolder("axgit-seal");
        var path = temp.File("axGIT.json", """{"recentRepositories":["C:\\repo"],"trustedRepositories":["C:\\repo","c:\\REPO"," "]}""");

        var settings = GitSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal([new TrustedRepository(@"C:\repo", string.Empty)], settings.Trusted);
        // A trust from before the marks counts for what the repository runs at its next opening, and is bound to it.
        Assert.True(settings.Trusts(@"C:\repo", Hook));
        Assert.True(settings.Trust(@"C:\repo", Hook));
        Assert.False(settings.Trusts(@"C:\repo", M("hook pre-push")));

        settings.Save(path);
        Assert.True(SettingsText.HasSeal(path));
        Assert.DoesNotContain("trustedRepositories", File.ReadAllText(path));
        settings = GitSettings.Load(path, out error);
        Assert.Null(error);
        Assert.True(settings.Trusts(@"C:\repo", Hook));

        // Taking the seal out afterwards is no way around it.
        SettingsText.RemoveSeal(path);
        settings = GitSettings.Load(path, out error);
        Assert.Equal(GitSettings.TrustNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.False(settings.Trusts(@"C:\repo", Hook));
    }
}
