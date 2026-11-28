using AxGit.Core;
using AxKit.Core;

namespace AxGit.Tests;

/// <summary>
/// The seal over axGIT's trusted repositories and what each trust is for (SPEC.md AG-1.6, AG-8.1a, bundle AX-5.6;
/// repairs.md R-53): a repository something other than axKit wrote into the list is not trusted, and a trusted one
/// that would run something else than when it was trusted is asked about again. A trust is kept with the scan's
/// mark (<see cref="TrustScan.Mark"/>) and, since the review's seventh pass (T1), its git folder's identity
/// (<see cref="FolderIdentity"/>); here marks of lines and identities made up stand for them.
/// </summary>
public sealed class GitSealTests
{
    private static string M(params string[] lines) => GitTrust.Mark(lines);

    private static readonly string Hook = M("hook post-checkout");

    /// <summary>A git folder on NTFS, whose identity lasts.</summary>
    private static readonly FolderIdentity Folder = new("NTFS", "9CC676A6C6768076", "0000000000000000003C0000000B00E3");

    private static string Saved(TempFolder temp)
    {
        var path = Path.Combine(temp.Path, "axGIT.json");
        var settings = new GitSettings { FontSize = 14 };
        settings.RememberRepository(@"C:\repo");
        settings.Trust(@"C:\repo", Hook, Folder);
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
        Assert.True(settings.Trusts(@"c:\REPO", Hook, Folder));

        // Trusting another repository through the window is sealed again by its save.
        Assert.True(settings.Trust(@"C:\other", M("core.fsmonitor"), Folder));
        settings.Save(path);
        settings = GitSettings.Load(path, out error);
        Assert.Null(error);
        Assert.True(settings.Trusts(@"C:\other", M("core.fsmonitor"), Folder));
        Assert.True(settings.Trusts(@"C:\repo", Hook, Folder));
    }

    [Fact]
    public void A_trusted_repository_that_gains_a_hook_is_asked_about_again()
    {
        var settings = new GitSettings();
        Assert.False(settings.Trusts(@"C:\repo", Hook, Folder));
        Assert.False(settings.WasTrusted(@"C:\repo"));

        Assert.True(settings.Trust(@"C:\repo", Hook, Folder));
        Assert.True(settings.Trusts(@"C:\repo", Hook, Folder));
        Assert.False(settings.Trust(@"C:\repo", Hook, Folder));

        // One hook more, one configured program more, or another hook in its place: not what was trusted.
        Assert.False(settings.Trusts(@"C:\repo", M("hook post-checkout", "hook pre-commit"), Folder));
        Assert.False(settings.Trusts(@"C:\repo", M("core.fsmonitor", "hook post-checkout"), Folder));
        Assert.False(settings.Trusts(@"C:\repo", M("hook pre-push"), Folder));
        Assert.True(settings.WasTrusted(@"C:\repo"));
        Assert.False(settings.TrustedAnother(@"C:\repo", Folder));

        // Trusted again with what it runs now: one entry, for the new mark.
        Assert.True(settings.Trust(@"C:\repo", M("hook post-checkout", "hook pre-commit"), Folder));
        Assert.Single(settings.Trusted);
        Assert.True(settings.Trusts(@"C:\repo", M("hook pre-commit", "hook post-checkout"), Folder));
        Assert.False(settings.Trusts(@"C:\repo", Hook, Folder));
    }

    /// <summary>
    /// T1 (the seventh pass): a trust is for the git folder it was given in. Another folder at the same path, with the
    /// same mark, is not trusted, and the notice can say so; a volume whose identities do not last keeps no trust (the
    /// decision for FAT, exFAT and a share that names no file system, tested here without such a volume).
    /// </summary>
    [Fact]
    public void A_trust_holds_only_in_the_git_folder_it_was_given_in()
    {
        var settings = new GitSettings();
        Assert.True(settings.Trust(@"C:\repo", Hook, Folder));
        var another = Folder with { FileId = "0000000000000000003C0000000B00E4" };
        Assert.False(settings.Trusts(@"C:\repo", Hook, another));
        Assert.True(settings.TrustedAnother(@"C:\repo", another));
        Assert.True(settings.TrustedAnother(@"C:\repo", null));
        Assert.False(settings.Trusts(@"C:\repo", Hook, null));

        // The rule of the file systems: NTFS and ReFS last; every other, or none read, does not.
        foreach (var lasting in new[] { "NTFS", "ReFS", "ntfs" })
        {
            Assert.True(FolderIdentity.IsLasting(lasting), lasting);
        }

        foreach (var passing in new[] { "FAT", "FAT32", "exFAT", "UDF", "CDFS", string.Empty })
        {
            Assert.False(FolderIdentity.IsLasting(passing), passing);
            var there = new FolderIdentity(passing, "0000000000000001", "00000000000000000000000000000002");
            Assert.False(there.Lasting);
            Assert.False(settings.Trust(@"C:\stick", Hook, there));
            Assert.False(settings.Trusts(@"C:\stick", Hook, there));
        }

        Assert.False(settings.WasTrusted(@"C:\stick"));
        Assert.False(new FolderIdentity("NTFS", "0000000000000001", string.Empty).Lasting);
        Assert.False(settings.Trust(@"C:\stick", Hook, null));

        // A trust from before the identities, with a mark that matches: asked about once more.
        settings.Trusted.Add(new TrustedRepository(@"C:\old", Hook));
        Assert.False(settings.Trusts(@"C:\old", Hook, Folder));
        Assert.True(settings.WasTrusted(@"C:\old"));
        Assert.False(settings.TrustedAnother(@"C:\old", Folder));
    }

    [Fact]
    public void The_mark_is_of_the_lines_whatever_their_order_and_fields_cannot_run_together()
    {
        Assert.Equal(M("a", "b"), M("b", "a"));
        Assert.NotEqual(M("ab"), M("a", "b"));
        Assert.NotEqual(M(), M("a"));
        Assert.Equal(64, Hook.Length);

        // T3 (the seventh pass): a line in other case is another line.
        Assert.NotEqual(M("a", "b"), M("B", "A"));
    }

    [Fact]
    public void A_repository_added_by_hand_is_not_trusted_and_the_other_settings_stay()
    {
        using var temp = new TempFolder("axgit-seal");
        var path = Saved(temp);
        SettingsText.Change(path, whole => whole["trusted"]!.AsArray().Add(new System.Text.Json.Nodes.JsonObject { ["path"] = @"C:\elsewhere", ["risks"] = Hook, ["identity"] = Folder.Text }));

        var settings = GitSettings.Load(path, out var error);
        Assert.Equal(GitSettings.TrustNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.True(SettingsSeal.IsRefusal(error!));
        Assert.Empty(settings.Trusted);
        Assert.False(settings.Trusts(@"C:\elsewhere", Hook, Folder));
        Assert.False(settings.Trusts(@"C:\repo", Hook, Folder));
        Assert.Equal(14, settings.FontSize);
        Assert.Equal([@"C:\repo"], settings.RecentRepositories);

        // The older key written into a sealed file is refused the same way.
        path = Saved(temp);
        SettingsText.Change(path, whole => whole["trustedRepositories"] = new System.Text.Json.Nodes.JsonArray(@"C:\elsewhere"));
        settings = GitSettings.Load(path, out error);
        Assert.Equal(GitSettings.TrustNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.False(settings.Trusts(@"C:\elsewhere", Hook, Folder));

        // A mark changed by hand, to make the trust fit other programs: refused.
        path = Saved(temp);
        SettingsText.Change(path, whole => whole["trusted"]![0]!["risks"] = M("hook pre-push"));
        settings = GitSettings.Load(path, out error);
        Assert.Equal(GitSettings.TrustNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.False(settings.Trusts(@"C:\repo", M("hook pre-push"), Folder));

        // T1: an identity changed by hand, to make the trust fit another git folder: refused; and one taken out.
        path = Saved(temp);
        SettingsText.Change(path, whole => whole["trusted"]![0]!["identity"] = (Folder with { FileId = "1" }).Text);
        settings = GitSettings.Load(path, out error);
        Assert.Equal(GitSettings.TrustNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        path = Saved(temp);
        SettingsText.Change(path, whole => whole["trusted"]![0]!.AsObject().Remove("identity"));
        settings = GitSettings.Load(path, out error);
        Assert.Equal(GitSettings.TrustNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
    }

    /// <summary>T1: a file sealed before the identities, its trusts with no identity, keeps its seal: no refusal after the update, and each repository asked about once more.</summary>
    [Fact]
    public void A_file_sealed_before_the_identities_keeps_its_seal()
    {
        using var temp = new TempFolder("axgit-seal");
        var path = Path.Combine(temp.Path, "axGIT.json");
        var settings = new GitSettings();
        settings.Trusted.Add(new TrustedRepository(@"C:\repo", Hook));
        settings.Save(path);
        Assert.DoesNotContain("\"identity\": \"", File.ReadAllText(path));

        settings = GitSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal([new TrustedRepository(@"C:\repo", Hook)], settings.Trusted);
        Assert.False(settings.Trusts(@"C:\repo", Hook, Folder));
        Assert.True(settings.Trust(@"C:\repo", Hook, Folder));
        settings.Save(path);
        settings = GitSettings.Load(path, out error);
        Assert.Null(error);
        Assert.True(settings.Trusts(@"C:\repo", Hook, Folder));
    }

    [Fact]
    public void A_file_from_before_the_seal_keeps_its_repositories_asks_about_them_again_and_is_sealed_by_the_next_save()
    {
        using var temp = new TempFolder("axgit-seal");
        var path = temp.File("axGIT.json", """{"recentRepositories":["C:\\repo"],"trustedRepositories":["C:\\repo","c:\\REPO"," "]}""");

        var settings = GitSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.Equal([new TrustedRepository(@"C:\repo", string.Empty)], settings.Trusted);
        // L5 (the fifth pass): a trust from before the marks, with no mark, is no trust: the repository is asked about
        // once more, as one that was trusted, and the trust given then is bound to its mark.
        Assert.False(settings.Trusts(@"C:\repo", Hook, Folder));
        Assert.False(settings.Trusts(@"C:\repo", string.Empty, Folder));
        Assert.True(settings.WasTrusted(@"C:\repo"));
        Assert.True(settings.Trust(@"C:\repo", Hook, Folder));
        Assert.False(settings.Trusts(@"C:\repo", M("hook pre-push"), Folder));

        settings.Save(path);
        Assert.True(SettingsText.HasSeal(path));
        Assert.DoesNotContain("trustedRepositories", File.ReadAllText(path));
        settings = GitSettings.Load(path, out error);
        Assert.Null(error);
        Assert.True(settings.Trusts(@"C:\repo", Hook, Folder));

        // Taking the seal out afterwards is no way around it.
        SettingsText.RemoveSeal(path);
        settings = GitSettings.Load(path, out error);
        Assert.Equal(GitSettings.TrustNotUsed + " " + AxKit.Core.SettingsSeal.CopyKept, error);
        Assert.False(settings.Trusts(@"C:\repo", Hook, Folder));
    }
}
