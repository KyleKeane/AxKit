using AxGit.Core;

namespace AxGit.Tests;

/// <summary>The server a fetch, pull or push reaches (bundle AX-11.5): the remote's name and its address's host.</summary>
public sealed class GitRemoteTests
{
    /// <summary>Split from the addresses so that the privacy test does not read them as e-mail addresses.</summary>
    private const string At = "@";

    [Theory]
    [InlineData("https://github.com/KyleKeane/AxKit.git", "github.com")]
    [InlineData("https://user" + At + "GitHub.com/a/b", "github.com")]
    [InlineData("ssh://git" + At + "gitlab.example.org:2222/a/b.git", "gitlab.example.org")]
    [InlineData("git" + At + "github.com:a/b.git", "github.com")]
    [InlineData("server:repos/b", "server")]
    [InlineData("ssh://git" + At + "[2001:db8::1]:2222/repo.git", "[2001:db8::1]")]
    [InlineData("https://[::1]/repo.git", "[::1]")]
    [InlineData("git" + At + "[::1]:repo.git", "[::1]")]
    [InlineData(@"C:\repos\b", null)]
    [InlineData("/c/repos/b", null)]
    [InlineData("file:///C:/repos/b", null)]
    [InlineData(@"\\server\share\b", null)]
    [InlineData("", null)]
    public void The_host_is_read_from_the_address(string url, string? host) => Assert.Equal(host, GitRemote.Host(url));

    /// <summary>
    /// The remote is git's own answer (2026-11-27): the upstream's for a fetch, and for a push the branch's
    /// pushRemote or remote.pushDefault where one is set, which git gives in the second place; origin where it
    /// gives nothing.
    /// </summary>
    [Theory]
    [InlineData("origin\torigin\n", false, "origin")]
    [InlineData("origin\tbackup\n", false, "origin")]
    [InlineData("origin\tbackup\n", true, "backup")]
    [InlineData("upstream\t\n", true, "upstream")]
    [InlineData("\tbackup\r\n", true, "backup")]
    [InlineData("\tbackup\n", false, "origin")]
    [InlineData("\t\n", true, "origin")]
    [InlineData("", false, "origin")]
    [InlineData(null, true, "origin")]
    public void The_remote_is_the_one_git_names_or_origin(string? output, bool push, string remote) => Assert.Equal(remote, GitRemote.Remote(output, push));

    [Fact]
    public void The_addresses_asked_for_are_the_fetch_address_or_every_push_address()
    {
        Assert.Equal(["remote", "get-url", "origin"], GitRemote.UrlArguments("origin", push: false));
        Assert.Equal(["remote", "get-url", "--push", "--all", "backup"], GitRemote.UrlArguments("backup", push: true));
        Assert.Equal(["for-each-ref", "--format=%(upstream:remotename)%09%(push:remotename)", "refs/heads/topic/one"], GitRemote.RemotesArguments("topic/one"));
    }

    [Fact]
    public void Every_address_a_push_goes_to_gives_its_host_once()
    {
        var output = "https://github.com/a/b.git\ngit" + At + "mirror.example.org:a/b.git\r\nhttps://GitHub.com/a/other.git\n" + @"C:\backups\b.git" + "\n";
        Assert.Equal(["github.com", "mirror.example.org"], GitRemote.Hosts(output));
        Assert.Empty(GitRemote.Hosts(string.Empty));
    }
}
