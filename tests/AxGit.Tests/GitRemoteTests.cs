using AxGit.Core;

namespace AxGit.Tests;

/// <summary>The server a fetch, pull or push reaches (bundle AX-11.5; docs/axGIT/SPEC.md AG-5.4a): the remote's name and its address's host.</summary>
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
    [InlineData(@"C:\repos\b", null)]
    [InlineData("/c/repos/b", null)]
    [InlineData("file:///C:/repos/b", null)]
    [InlineData(@"\\server\share\b", "server")]
    [InlineData("", null)]
    public void The_host_is_read_from_the_address(string url, string? host) => Assert.Equal(host, GitRemote.Host(url));

    /// <summary>
    /// The hard addresses (the review's second and third passes of 2026-11-27, D4): an address is read only in one
    /// of the strict forms, and everything else is not read, so that nothing is sent to it. Each row is the kind
    /// and the host; the rows of the third pass are the addresses where a host was named and git reached another,
    /// or a folder was named and git ran ssh (tried with git 2.53 and an ssh that printed its arguments).
    /// </summary>
    [Theory]
    // A host ends at the first slash, and nothing but a host stands before it.
    [InlineData("https://evil.example#" + At + "github.com/x.git", AddressKind.NotRead, null)]
    [InlineData("https://evil.example?" + At + "github.com/x.git", AddressKind.NotRead, null)]
    [InlineData("https://evil.example/" + At + "github.com/x.git", AddressKind.Server, "evil.example")]
    [InlineData("https://user:secret" + At + "host.example:8443/x", AddressKind.Server, "host.example")]
    [InlineData("HTTPS://Host.Example/x", AddressKind.Server, "host.example")]
    [InlineData("https://host.example", AddressKind.NotRead, null)]
    [InlineData("https://host.example:/x", AddressKind.NotRead, null)]
    [InlineData("https://host.example:0/x", AddressKind.NotRead, null)]
    [InlineData("https://host.example:65536/x", AddressKind.NotRead, null)]
    [InlineData("https://[::1]:8080/x", AddressKind.Server, "[::1]")]
    [InlineData("git://host.example/x.git", AddressKind.Server, "host.example")]
    [InlineData("git://user" + At + "host.example/x.git", AddressKind.NotRead, null)]
    [InlineData("ssh://git:secret" + At + "host.example/x", AddressKind.NotRead, null)]
    [InlineData("ssh+git://git" + At + "host.example/x", AddressKind.NotRead, null)]
    [InlineData("ftp://host.example/x", AddressKind.NotRead, null)]
    [InlineData("host.example:8080:x", AddressKind.Server, "host.example")]
    [InlineData("ab:c", AddressKind.Server, "ab")]
    // Two at signs: programs disagree on which one ends the user.
    [InlineData("https://a" + At + "b" + At + "host.example/x", AddressKind.NotRead, null)]
    // A percent sign is decoded by git for ssh and not by curl; a backslash is a slash to some readers.
    [InlineData("ssh://evil.example%2F" + At + "github.com/x", AddressKind.NotRead, null)]
    [InlineData("https://evil.example\\" + At + "github.com/x", AddressKind.NotRead, null)]
    [InlineData("https://host.example:port/x", AddressKind.NotRead, null)]
    [InlineData("https://[::1/x", AddressKind.NotRead, null)]
    [InlineData("https:///x", AddressKind.NotRead, null)]
    [InlineData("https://host.example\n.evil.example/x", AddressKind.NotRead, null)]
    [InlineData("https://host_name.example/x", AddressKind.NotRead, null)]
    // A transport that starts a program: ext runs any command, every other name the program git-remote-name.
    [InlineData("ext::sh -c touch% x", AddressKind.NotRead, null)]
    [InlineData("fd::17", AddressKind.NotRead, null)]
    [InlineData("foo://host.example/x", AddressKind.NotRead, null)]
    // A host like an option of ssh, and a bracketed host outside a form with a scheme.
    [InlineData("-oProxyCommand=calc:x", AddressKind.NotRead, null)]
    [InlineData("ssh://-oProxyCommand=calc/x", AddressKind.NotRead, null)]
    [InlineData("[example.org:22]:a/b", AddressKind.NotRead, null)]
    [InlineData("git" + At + "[::1]:repo.git", AddressKind.NotRead, null)]
    [InlineData("[127.0.0.1]:x" + At + "[evil.invalid]:repo.git", AddressKind.NotRead, null)]
    [InlineData("  ", AddressKind.NotRead, null)]
    // A drive letter and no valid Windows path after it: git ran ssh to the host c or d.
    [InlineData("c:nul", AddressKind.NotRead, null)]
    [InlineData("c:a:b", AddressKind.NotRead, null)]
    [InlineData("c:repo.", AddressKind.NotRead, null)]
    [InlineData("d:x<y", AddressKind.NotRead, null)]
    [InlineData("h:path", AddressKind.NotRead, null)]
    [InlineData("user" + At + "h:path", AddressKind.NotRead, null)]
    [InlineData(@"C:\repos\nul\b", AddressKind.NotRead, null)]
    [InlineData(@"C:\repos\b.", AddressKind.NotRead, null)]
    [InlineData(@"C:\repos\a|b", AddressKind.NotRead, null)]
    // A host in letters outside ASCII: its punycode form alone.
    [InlineData("https://b\u00fccher.example/x", AddressKind.NotRead, null)]
    [InlineData("https://xn--bcher-kva.example/x", AddressKind.Server, "xn--bcher-kva.example")]
    [InlineData("git" + At + "b\u00fccher.example:x", AddressKind.NotRead, null)]
    // Paths on this computer.
    [InlineData("c:/ok/path", AddressKind.Local, null)]
    [InlineData(@"C:\repos\b.git", AddressKind.Local, null)]
    [InlineData("../other", AddressKind.Local, null)]
    [InlineData(@".\vendor\mirror", AddressKind.Local, null)]
    [InlineData("./vendor/mirror", AddressKind.Local, null)]
    [InlineData(".", AddressKind.Local, null)]
    [InlineData("..", AddressKind.Local, null)]
    [InlineData("/c/repos/b", AddressKind.Local, null)]
    [InlineData("file:///C:/repos/b", AddressKind.Local, null)]
    [InlineData("file:///nowhere", AddressKind.NotRead, null)]
    // What git takes for a path but is in no form here: not read.
    [InlineData("user" + At + "host.example", AddressKind.NotRead, null)]
    [InlineData("a:b", AddressKind.NotRead, null)]
    [InlineData("dir/with:colon", AddressKind.NotRead, null)]
    [InlineData("vendor/mirror", AddressKind.NotRead, null)]
    // Shares: the computer they lie on is a server (M1).
    [InlineData("file://server/share/b.git", AddressKind.Server, "server")]
    [InlineData("file://localhost/c$/x", AddressKind.Server, "localhost")]
    [InlineData("//Host.Example/share/x", AddressKind.Server, "host.example")]
    [InlineData(@"\\host\share\x", AddressKind.Server, "host")]
    [InlineData(@"\\host", AddressKind.NotRead, null)]
    [InlineData("//-host/share", AddressKind.NotRead, null)]
    public void A_hard_address_gives_the_host_git_reaches_or_is_not_read(string address, AddressKind kind, string? host)
    {
        var read = GitRemote.Read(address);
        Assert.Equal((kind, host), (read.Kind, read.Host));
    }

    /// <summary>The port an address gives is kept, for the reminder; a folder or a share keeps the path git reaches it by, which a push reads first (AG-5.4c).</summary>
    [Fact]
    public void A_port_is_kept_for_the_reminder_and_a_folder_for_the_push()
    {
        Assert.Equal(2222, GitRemote.Read("ssh://git" + At + "host.example:2222/x").Port);
        Assert.Equal(8443, GitRemote.Read("https://[::1]:8443/x").Port);
        Assert.Null(GitRemote.Read("git" + At + "host.example:x").Port);
        Assert.Equal("host.example, port 2222", GitRemote.Place("host.example", 2222));
        Assert.Equal("host.example", GitRemote.Place("host.example", null));
        Assert.Equal("./vendor/mirror", GitRemote.Read("./vendor/mirror").Folder);
        Assert.Equal("C:/repos/b", GitRemote.Read("file:///C:/repos/b").Folder);
        Assert.Equal("//server/share/b.git", GitRemote.Read("file://server/share/b.git").Folder);
        Assert.Equal(@"\\host\share\x", GitRemote.Read(@"\\host\share\x").Folder);
        Assert.Null(GitRemote.Read("https://host.example/x").Folder);
    }

    [Fact]
    public void The_settings_are_read_with_their_kind_of_file_and_the_last_of_a_name_is_in_force()
    {
        Assert.Equal(["config", "--show-scope", "--null", "--list"], GitRemote.SettingsArguments);
        var settings = GitRemote.Settings("global\0user.name\nA B\0local\0core.bare\0local\0remote.origin.url\nhttps://one.example/a\nb\0command\0color.ui\nnever\0");
        Assert.Equal(
            [new ListedSetting("global", "user.name", "A B"), new ListedSetting("local", "core.bare", string.Empty), new ListedSetting("local", "remote.origin.url", "https://one.example/a\nb"), new ListedSetting("command", "color.ui", "never")],
            settings);
        Assert.Empty(GitRemote.Settings(string.Empty));
    }

    private static IReadOnlyList<ListedSetting> Local(params (string Name, string Value)[] settings) =>
        [.. settings.Select(setting => new ListedSetting("local", setting.Name, setting.Value))];

    /// <summary>git's own order (its remote.c): the branch's remote; else the one remote there is; else origin.</summary>
    [Fact]
    public void A_fetch_is_from_the_branchs_remote_else_the_one_remote_else_origin()
    {
        var two = Local(("remote.origin.url", "a"), ("remote.evil.url", "b"), ("remote.evil.fetch", "c"));
        Assert.Equal("origin", GitRemote.FetchRemote(two, "main"));
        Assert.Equal("origin", GitRemote.FetchRemote(two, null));
        // The branch's remote counts with or without a branch to follow (the first case of the review).
        Assert.Equal("evil", GitRemote.FetchRemote([.. two, new ListedSetting("local", "branch.main.remote", "evil")], "main"));
        // A name's middle part is as written: another branch's, or another case, is another name.
        Assert.Equal("origin", GitRemote.FetchRemote([.. two, new ListedSetting("local", "branch.Main.remote", "evil")], "main"));
        Assert.Equal("upstream", GitRemote.FetchRemote(Local(("remote.upstream.url", "a"), ("remote.upstream.fetch", "b")), "main"));
        Assert.Equal("origin", GitRemote.FetchRemote([], "main"));
        // The last one listed is in force, as for git.
        Assert.Equal("second", GitRemote.FetchRemote(Local(("branch.main.remote", "first"), ("branch.main.remote", "second")), "main"));
        Assert.Equal(".", GitRemote.FetchRemote(Local(("branch.topic.remote", ".")), "topic"));
    }

    [Fact]
    public void A_push_is_to_the_branchs_push_remote_else_the_default_one_else_where_it_is_fetched_from()
    {
        var settings = Local(("remote.origin.url", "a"), ("remote.backup.url", "b"), ("branch.main.remote", "origin"));
        Assert.Equal("origin", GitRemote.PushRemote(settings, "main"));
        Assert.Equal("backup", GitRemote.PushRemote([.. settings, new ListedSetting("local", "remote.pushdefault", "backup")], "main"));
        Assert.Equal("third", GitRemote.PushRemote([.. settings, new ListedSetting("local", "remote.pushdefault", "backup"), new ListedSetting("local", "branch.main.pushremote", "third")], "main"));
    }

    [Fact]
    public void A_remotes_name_that_is_a_group_of_remotes_is_known()
    {
        var settings = Local(("remote.origin.url", "a"), ("remotes.origin", "evil third"));
        Assert.True(GitRemote.IsGroup(settings, "origin"));
        Assert.False(GitRemote.IsGroup(settings, "evil"));
    }

    [Fact]
    public void The_addresses_asked_for_are_the_fetch_address_or_every_push_address_the_name_after_two_dashes()
    {
        Assert.Equal(["remote", "get-url", "--", "origin"], GitRemote.UrlArguments("origin", push: false));
        Assert.Equal(["remote", "get-url", "--push", "--all", "--", "-x"], GitRemote.UrlArguments("-x", push: true));
    }

    [Fact]
    public void Every_address_a_push_goes_to_gives_its_host_once()
    {
        var output = "https://github.com/a/b.git\ngit" + At + "mirror.example.org:a/b.git\r\nhttps://GitHub.com/a/other.git\n" + @"C:\backups\b.git" + "\n";
        Assert.Equal(["github.com", "mirror.example.org"], GitRemote.Hosts(output));
        Assert.Empty(GitRemote.Hosts(string.Empty));
        Assert.Equal([AddressKind.Server, AddressKind.NotRead, AddressKind.Local], GitRemote.Addresses("https://a.example/x\next::cmd\n/c/repo\n").Select(address => address.Kind));
    }

    /// <summary>
    /// A proxy the repository's own settings name is where the bytes go first (AG-5.4b): its host is read; the
    /// user's own proxy, and the computer's, is the user's and is not named.
    /// </summary>
    [Fact]
    public void A_proxy_is_named_when_the_repositorys_own_settings_set_it()
    {
        Assert.Equal(["config", "--show-scope", "--get-urlmatch", "http.proxy", "https://a.example/x"], GitRemote.ProxyArguments("https://a.example/x"));
        Assert.Equal("proxy.example", GitRemote.RepositoryProxy([], "origin", "local\thttp://user:secret" + At + "Proxy.Example:8080\n")!.Host);
        Assert.Equal("proxy.example", GitRemote.RepositoryProxy([], "origin", "worktree\tproxy.example:3128\n")!.Host);
        Assert.Equal("proxy.example", GitRemote.RepositoryProxy([], "origin", "local\tsocks5h://proxy.example\n")!.Host);
        Assert.Null(GitRemote.RepositoryProxy([], "origin", "global\thttp://mine.example:8080\n"));
        Assert.Null(GitRemote.RepositoryProxy([], "origin", "command\t\n"));
        Assert.Null(GitRemote.RepositoryProxy([], "origin", null));
        // The remote's own proxy comes before any other, as for git; an empty one is no proxy.
        var own = Local(("remote.origin.proxy", "http://own.example"));
        Assert.Equal(new Address(AddressKind.Server, "own.example"), GitRemote.RepositoryProxy(own, "origin", "local\thttp://other.example\n"));
        Assert.Equal(new Address(AddressKind.Server, "other.example"), GitRemote.RepositoryProxy(own, "backup", "local\thttp://other.example\n"));
        Assert.Null(GitRemote.RepositoryProxy(Local(("remote.origin.proxy", string.Empty)), "origin", "local\thttp://other.example\n"));
        // A proxy whose server cannot be read is not read: nothing is sent.
        Assert.Equal(AddressKind.NotRead, GitRemote.RepositoryProxy([], "origin", "local\tfoo://proxy.example\n")!.Kind);
        Assert.Equal(AddressKind.NotRead, GitRemote.RepositoryProxy([], "origin", "local\thttp://a" + At + "b" + At + "proxy.example\n")!.Kind);
        Assert.True(GitRemote.UsesHttp("HTTPS://a.example/x"));
        Assert.False(GitRemote.UsesHttp("git" + At + "a.example:x"));
    }

    /// <summary>
    /// The review's second pass, finding 11: git names a remote it does not know with its whole address, and the
    /// first line of a failed command goes to the log. Every text of git's for the log or the user is without
    /// the sign-in.
    /// </summary>
    [Fact]
    public void What_git_says_goes_to_the_log_and_the_user_without_the_sign_in_of_an_address()
    {
        var said = "error: No such remote 'https://user:secret" + At + "addr.example.invalid/a.git'";
        Assert.Equal("error: No such remote 'https://addr.example.invalid/a.git'", GitRemote.WithoutSignIn(said));
        Assert.Equal("To ssh://host.example:22/a and https://b.example/c", GitRemote.WithoutSignIn("To ssh://git" + At + "host.example:22/a and https://token" + At + "b.example/c"));
        // What is no address with a scheme stays: a mail address in a commit, the scp form (which holds no password).
        Assert.Equal("Author: A <a" + At + "b.example>", GitRemote.WithoutSignIn("Author: A <a" + At + "b.example>"));
        Assert.Equal("https://host.example/path with " + At + " in it", GitRemote.WithoutSignIn("https://host.example/path with " + At + " in it"));
        // L11: a sign-in with a tab in it, in an address shown as not read, and a proxy written without its kind.
        Assert.DoesNotContain("secret", GitRemote.WithoutSignIn("address: https://us\ter:secret" + At + "host.example/x"));
        Assert.Equal("proxy host.example:8080 refused", GitRemote.WithoutSignIn("proxy user:secret" + At + "host.example:8080 refused"));
        Assert.Equal("'host.example:3128'", GitRemote.WithoutSignIn("'user:secret" + At + "host.example:3128'"));

        var result = new GitResult(2, "fatal: also https://user:secret" + At + "out.example/x\n", said + "\nsecond line");
        Assert.DoesNotContain("secret", result.Error);
        Assert.DoesNotContain("secret", result.FirstLine);
        Assert.DoesNotContain("secret", result.AllText);
        Assert.Equal("error: No such remote 'https://addr.example.invalid/a.git'", result.FirstLine);
        // With no complaint the first line is the output's, and that is without the sign-in too.
        Assert.DoesNotContain("secret", new GitResult(1, "https://user:secret" + At + "out.example/x", string.Empty).FirstLine);
        // A run as the runner makes its result: the same.
        Assert.DoesNotContain("secret", GitRunner.Result(new AxKit.Core.ProcessResult(2, string.Empty, said, false, false), TimeSpan.FromSeconds(1)).Error);
    }
}
