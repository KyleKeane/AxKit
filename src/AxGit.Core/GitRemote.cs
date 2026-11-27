namespace AxGit.Core;

/// <summary>
/// The servers a fetch, pull or push reaches (bundle AX-11.5): each server is its own consent, asked before the first
/// command that reaches it. Pure: the commands that ask git which remote it would use and where that remote's
/// addresses lead, and the readers of its answers.
///
/// git is asked, never guessed for (the review of 2026-11-27; until then the consent was for the address a fetch
/// uses, of the remote the upstream names, while a push may go elsewhere): the remote a push uses is the branch's
/// <c>pushRemote</c>, else <c>remote.pushDefault</c>, else the upstream's, else origin; its addresses are every
/// <c>pushurl</c> the remote has, else its <c>url</c>, each as git rewrites it (<c>url.&lt;base&gt;.pushInsteadOf</c>,
/// <c>insteadOf</c>), and a push goes to all of them. A fetch or pull uses the upstream's remote, else origin, and
/// its first <c>url</c>, rewritten. None of these commands reaches a server or runs a program the repository names.
/// </summary>
public static class GitRemote
{
    /// <summary>The remote git uses when none is named and none is set: origin.</summary>
    private const string Default = "origin";

    /// <summary>The remotes git would fetch a branch from and push it to, as one line, the two names with a tab between; a name is empty where nothing is set.</summary>
    public static IReadOnlyList<string> RemotesArguments(string branch) =>
        ["for-each-ref", "--format=%(upstream:remotename)%09%(push:remotename)", "refs/heads/" + branch];

    /// <summary>
    /// The remote a plain fetch or pull uses (<paramref name="push"/> false) or a plain push uses, from git's answer
    /// to <see cref="RemotesArguments"/>: the name it gives, origin where it gives none (no branch, a branch with
    /// nothing set, or an answer that could not be had).
    /// </summary>
    public static string Remote(string? output, bool push)
    {
        var names = (output ?? string.Empty).Split('\n')[0].TrimEnd('\r').Split('\t');
        var name = push
            ? (names.Length > 1 && names[1].Trim().Length > 0 ? names[1] : names[0])
            : names[0];
        return name.Trim() is { Length: > 0 } set ? set : Default;
    }

    /// <summary>
    /// git remote get-url: the remote's addresses as git would use them, rewritten, which runs nothing: the one a
    /// fetch uses, or every one a push goes to, one per line.
    /// </summary>
    public static IReadOnlyList<string> UrlArguments(string remote, bool push) =>
        push ? ["remote", "get-url", "--push", "--all", remote] : ["remote", "get-url", remote];

    /// <summary>The hosts of the addresses git printed, one per line, each once, in git's order; an address on this computer or a share is none.</summary>
    public static IReadOnlyList<string> Hosts(string output) =>
        [.. output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Host).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// The host of a remote's address, lower case: "github.com" from https://github.com/a/b.git, from an ssh:// address
    /// with a user and a port, and from the scp-like user, at sign, host, colon, path; null for a path on this computer
    /// or a share (C:\repos\b, /c/repos/b, file://…, \\server\share\b), which reaches no service of anyone else's.
    /// </summary>
    public static string? Host(string url)
    {
        url = url.Trim();
        if (url.Length == 0 || url.StartsWith("file:", StringComparison.OrdinalIgnoreCase) || url.StartsWith(@"\\", StringComparison.Ordinal)
            || url.StartsWith('/') || (url.Length > 1 && url[1] == ':'))
        {
            return null;
        }

        var scheme = url.IndexOf("://", StringComparison.Ordinal);
        if (scheme > 0)
        {
            var rest = url[(scheme + 3)..];
            var end = rest.IndexOfAny(['/', '?']);
            var authority = end < 0 ? rest : rest[..end];
            var at = authority.LastIndexOf('@');
            var host = WithoutPort(at < 0 ? authority : authority[(at + 1)..]);
            return host.Length == 0 ? null : host.ToLowerInvariant();
        }

        // scp-like: [user@]host:path, the host an IPv6 literal in brackets perhaps, whose colons are its own.
        var close = url.StartsWith('[') || url.Contains("@[", StringComparison.Ordinal) ? url.IndexOf(']') : -1;
        var separator = url.IndexOf(':', close < 0 ? 0 : close);
        if (separator > 0)
        {
            var authority = url[..separator];
            var at = authority.LastIndexOf('@');
            var host = at < 0 ? authority : authority[(at + 1)..];
            return host.Length == 0 ? null : host.ToLowerInvariant();
        }

        return null;
    }

    /// <summary>The host without its port: "[2001:db8::1]" keeps its brackets and its colons (review, 2026-11-02).</summary>
    private static string WithoutPort(string host)
    {
        if (host.StartsWith('['))
        {
            var close = host.IndexOf(']');
            return close < 0 ? host : host[..(close + 1)];
        }

        var colon = host.IndexOf(':');
        return colon < 0 ? host : host[..colon];
    }
}
