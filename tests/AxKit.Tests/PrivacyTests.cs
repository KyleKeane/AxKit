using System.Text.RegularExpressions;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// Personal data never enters the repository (the root CLAUDE.md, rule 6): <c>AxKit.Core/Redaction</c> takes it out of
/// what a tool prints, and no file a commit can carry holds an e-mail address, a path into a Windows account, a
/// GitHub token or a private key. In this project since 2026-11-16 (in tests/AxClaude.Tests before), so that
/// <c>.\test.ps1 axKit</c>, the bundle's own checks, runs it.
/// </summary>
public partial class PrivacyTests
{
    [Fact]
    public void Redact_replaces_what_status_prints_about_the_account()
    {
        var text = Redaction.Redact(
            // Built from parts, so that the scan below does not find these samples in this file.
            "Email: someone" + "@example.com\nPeer address: uds:\\\\.\\pipe\\LOCAL\\cc-msg-e875aa\ncwd: C:\\" + "Users\\Someone\\Desktop\\x\n"
            + "Session ID: e585e125-b63e-4a5e-af77-b2eb1f6ddaa2\nHello Someone.",
            ["Someone"]);
        Assert.Equal(
            "Email: <email>\nPeer address: uds:<pipe>\ncwd: C:\\Users\\<user>\\Desktop\\x\nSession ID: <id>\nHello <name>.", text);
    }

    [Fact]
    public void Redact_leaves_short_names_and_placeholders_alone()
    {
        Assert.Equal("C:\\Users\\<user>\\x, an Al fact", Redaction.Redact("C:\\Users\\<user>\\x, an Al fact", ["Al"]));
    }

    [Fact]
    public void Redact_covers_the_unix_spellings_of_a_drive()
    {
        // Git Bash and WSL paths, as a tool result or a Bash command in the conversation prints them; built from parts for the scan below.
        Assert.Equal("cd /c/Users/<user>/x", Redaction.Redact("cd /c/" + "Users/Someone/x", []));
        Assert.Equal("ls /mnt/c/Users/<user>", Redaction.Redact("ls /mnt/c/" + "Users/Someone", []));
        Assert.Equal(["/mnt/c/" + "Users/Someone"], Redaction.Find("see /mnt/c/" + "Users/Someone and /home/someone"));
    }

    [Fact]
    public void Redact_hides_a_work_onedrive_and_the_computer_and_domain_names()
    {
        Assert.Equal("D:\\OneDrive - <org>\\notes.md on <name>",
            Redaction.Redact("D:\\OneDrive - Contoso Ltd\\notes.md on DESKTOP-Q7Z", ["DESKTOP-Q7Z"]));
        Assert.Contains(Environment.MachineName, Redaction.DefaultNames());
        Assert.Contains(Environment.UserDomainName, Redaction.DefaultNames());
    }

    /// <summary>
    /// No file in the repository holds an e-mail address or a path into a Windows account: recordings of /status carry
    /// the account's address, and paths name the account. The licence line names the author and is the one exception.
    /// </summary>
    [Fact]
    public void The_repository_holds_no_email_address_or_account_path()
    {
        var root = LayoutTests.RepositoryRoot();
        var found = new List<string>();
        foreach (var file in RepositoryFiles(root))
        {
            foreach (var hit in Redaction.Find(File.ReadAllText(file)))
            {
                found.Add($"{Path.GetRelativePath(root, file)}: {hit}");
            }
        }

        Assert.Empty(found);
    }

    /// <summary>
    /// No file in the repository holds a GitHub token or a private key (the root CLAUDE.md: a key, a passphrase or a
    /// token is never written into this repository). A token is known by its prefix and a run of letters and digits
    /// after it, so that a document may name the prefix; a key by the first line of its PEM text. The failure names
    /// the file and the kind and never the text itself, which would put the secret into the test's output.
    /// </summary>
    [Fact]
    public void The_repository_holds_no_token_or_private_key()
    {
        var root = LayoutTests.RepositoryRoot();
        var found = new List<string>();
        foreach (var file in RepositoryFiles(root))
        {
            var text = File.ReadAllText(file);
            if (GitHubToken().IsMatch(text))
            {
                found.Add($"{Path.GetRelativePath(root, file)}: a GitHub token");
            }

            if (PrivateKey().IsMatch(text))
            {
                found.Add($"{Path.GetRelativePath(root, file)}: a private key");
            }
        }

        Assert.True(found.Count == 0, "A secret in a file a commit can carry (take it out, and revoke it: it has been on disk in the clear): " + string.Join("; ", found));
    }

    /// <summary>
    /// Text files outside build output and git's own folder, which is what a commit can carry. The Claude Code folder
    /// .claude is read too (the shared settings and the skills are committed), but for the two things in it that never
    /// are: the worktrees, each a checkout of its own, and settings.local.json, the computer's own permissions, which
    /// hold its paths.
    /// </summary>
    private static IEnumerable<string> RepositoryFiles(string root)
    {
        string[] skipped = ["bin", "obj", ".git", ".vs", "publish", "artifacts", "TestResults"];
        string[] binary = [".ico", ".png", ".dll", ".exe", ".zip", ".pdb"];
        var worktrees = Path.Combine(root, ".claude", "worktrees");
        var localSettings = Path.Combine(root, ".claude", "settings.local.json");
        var pending = new Stack<string>([root]);
        while (pending.Count > 0)
        {
            var dir = pending.Pop();
            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                if (!skipped.Contains(Path.GetFileName(sub), StringComparer.OrdinalIgnoreCase)
                    && !string.Equals(sub, worktrees, StringComparison.OrdinalIgnoreCase))
                {
                    pending.Push(sub);
                }
            }

            foreach (var file in Directory.EnumerateFiles(dir))
            {
                // In a worktree, .git is a file naming the main checkout's path: git's own, never committed.
                if (!binary.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)
                    && !skipped.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)
                    && !string.Equals(file, localSettings, StringComparison.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }
    }

    /// <summary>GitHub's tokens: a classic personal token, an OAuth token (the gh tool's), a fine-grained personal token.</summary>
    [GeneratedRegex(@"\b(ghp|gho)_[A-Za-z0-9]{20,}|\bgithub_pat_[A-Za-z0-9_]{20,}")]
    private static partial Regex GitHubToken();

    /// <summary>The first line of a private key in PEM text, of any kind (RSA, EC, OPENSSH, none).</summary>
    [GeneratedRegex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----")]
    private static partial Regex PrivateKey();
}
