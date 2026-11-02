namespace AxGit.Core;

/// <summary>
/// The server a fetch, pull or push reaches (bundle AX-11.5): each server is its own consent, asked before the first
/// command that reaches it. Pure: the remote's name from the upstream, and the host from the remote's address.
/// </summary>
public static class GitRemote
{
    /// <summary>The remote a plain fetch, pull or push uses: the upstream's ("origin/main" is origin's), else origin.</summary>
    public static string NameOf(string? upstream)
    {
        var slash = upstream?.IndexOf('/') ?? -1;
        return slash > 0 ? upstream![..slash] : "origin";
    }

    /// <summary>git remote get-url: the remote's address, which runs nothing.</summary>
    public static IReadOnlyList<string> UrlArguments(string remote) => ["remote", "get-url", remote];

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
