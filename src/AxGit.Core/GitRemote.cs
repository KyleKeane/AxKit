using System.Text.RegularExpressions;

namespace AxGit.Core;

/// <summary>Where an address leads: a folder of this computer or a share, a server, or nothing axGIT can read.</summary>
public enum AddressKind
{
    /// <summary>A path on this computer or a share, or a <c>file://</c> address: no service of anyone else's.</summary>
    Local,

    /// <summary>A server, whose host is known.</summary>
    Server,

    /// <summary>An address whose server cannot be read with certainty, or that starts a program: nothing may be sent to it.</summary>
    NotRead,
}

/// <summary>An address as <see cref="GitRemote.Read"/> reads it; <paramref name="Host"/>, in lower case, for a server.</summary>
public sealed record Address(AddressKind Kind, string? Host);

/// <summary>One setting as git lists it with <see cref="GitRemote.SettingsArguments"/>: the file's kind it comes from, its name, its value.</summary>
public sealed record ListedSetting(string Scope, string Name, string Value);

/// <summary>
/// The servers a fetch, pull or push reaches (bundle AX-11.5; SPEC.md AG-5.4a): each server is its own consent, asked
/// before the first command that reaches it. Pure: the commands that ask git for its settings and for where a
/// remote's addresses lead, and the readers of its answers. The order they are asked in is <see cref="GitReach"/>.
///
/// The remote is named by axGIT and put on the command line (the review's second pass of 2026-11-27). Until then
/// git was asked which remote it would use and then run without one, and the two did not always agree: a branch
/// with a remote and no branch to follow, <c>fetch.all</c>, a push from a detached HEAD each reached a server
/// that was not asked about. Now the remote is read from the settings by git's own order (for a fetch or pull
/// the branch's <c>remote</c>, else the one remote there is, else origin; for a push the branch's
/// <c>pushRemote</c>, else <c>remote.pushDefault</c>, else the same), and that name is on the command line of
/// the fetch, pull or push, so the server asked about is the one reached whatever a setting says. Its addresses
/// are git's own answer: every <c>pushurl</c> the remote has, else its <c>url</c>, each as git rewrites it
/// (<c>insteadOf</c>, <c>pushInsteadOf</c>). None of these commands reaches a server or runs a program the
/// repository names.
/// </summary>
public static partial class GitRemote
{
    /// <summary>The remote git uses when none is named and none is set: origin.</summary>
    public const string Default = "origin";

    /// <summary>The remote that is the repository itself: a branch that follows another branch of this repository. Nothing is reached.</summary>
    public const string Here = ".";

    // ---- The settings a remote is chosen by ----

    /// <summary>Every setting with the kind of file it comes from, each entry ended by a zero character: the kind, a zero, then the name, a line end and the value.</summary>
    public static readonly IReadOnlyList<string> SettingsArguments = ["config", "--show-scope", "--null", "--list"];

    /// <summary>
    /// The settings from git's answer to <see cref="SettingsArguments"/>, in git's order, which is the order it
    /// obeys them in: the last one of a name is the one in force. A setting without a value (a bare name, which
    /// git reads as true) has an empty one here.
    /// </summary>
    public static IReadOnlyList<ListedSetting> Settings(string output)
    {
        var settings = new List<ListedSetting>();
        var parts = output.Split('\0');
        for (var at = 0; at + 1 < parts.Length; at += 2)
        {
            var line = parts[at + 1].IndexOf('\n');
            settings.Add(line < 0
                ? new ListedSetting(parts[at], parts[at + 1], string.Empty)
                : new ListedSetting(parts[at], parts[at + 1][..line], parts[at + 1][(line + 1)..]));
        }

        return settings;
    }

    /// <summary>The setting of a name that is in force, the last one listed; null when none is. The name is as git lists it: the section and the variable in lower case, a middle part as written.</summary>
    private static ListedSetting? InForce(IReadOnlyList<ListedSetting> settings, string name) =>
        settings.LastOrDefault(setting => setting.Name.Equals(name, StringComparison.Ordinal));

    private static string? Value(IReadOnlyList<ListedSetting> settings, string name) =>
        InForce(settings, name)?.Value.Trim() is { Length: > 0 } value ? value : null;

    /// <summary>The remotes the settings name, each once: the middle part of every <c>remote.name.something</c>.</summary>
    private static IReadOnlyList<string> Remotes(IReadOnlyList<ListedSetting> settings) =>
        [.. settings.Select(setting => setting.Name)
            .Where(name => name.StartsWith("remote.", StringComparison.Ordinal) && name.LastIndexOf('.') > "remote.".Length)
            .Select(name => name["remote.".Length..name.LastIndexOf('.')])
            .Distinct(StringComparer.Ordinal)];

    /// <summary>
    /// The remote a fetch or pull of <paramref name="branch"/> uses, by git's own order: the branch's
    /// <c>remote</c>; else the one remote the repository has; else origin. Without a branch (a detached HEAD) the
    /// last two.
    /// </summary>
    public static string FetchRemote(IReadOnlyList<ListedSetting> settings, string? branch) =>
        (branch is null ? null : Value(settings, $"branch.{branch}.remote"))
        ?? (Remotes(settings) is { Count: 1 } one ? one[0] : Default);

    /// <summary>
    /// The remote a push of <paramref name="branch"/> uses, by git's own order: the branch's <c>pushRemote</c>;
    /// else <c>remote.pushDefault</c>; else the remote it is fetched from (<see cref="FetchRemote"/>).
    /// </summary>
    public static string PushRemote(IReadOnlyList<ListedSetting> settings, string branch) =>
        Value(settings, $"branch.{branch}.pushremote") ?? Value(settings, "remote.pushdefault") ?? FetchRemote(settings, branch);

    /// <summary>
    /// Whether a remote's name is also the name of a group of remotes (<c>remotes.name</c>): git fetches a group's
    /// remotes in place of the one named, each on a server of its own (tried with git 2.53), so axGIT fetches
    /// from no such name.
    /// </summary>
    public static bool IsGroup(IReadOnlyList<ListedSetting> settings, string remote) =>
        InForce(settings, "remotes." + remote) is not null;

    // ---- A remote's addresses ----

    /// <summary>
    /// git remote get-url: the remote's addresses as git would use them, rewritten, which runs nothing: the one a
    /// fetch uses, or every one a push goes to, one per line. The name comes after "--", so that a remote named
    /// like an option is a name (<c>-x</c> stopped the command with a complaint about a switch, and the push ran
    /// with nobody asked).
    /// </summary>
    public static IReadOnlyList<string> UrlArguments(string remote, bool push) =>
        push ? ["remote", "get-url", "--push", "--all", "--", remote] : ["remote", "get-url", "--", remote];

    /// <summary>The addresses git printed, one per line, each read (<see cref="Read"/>).</summary>
    public static IReadOnlyList<Address> Addresses(string output) =>
        [.. output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Read)];

    /// <summary>The hosts of the addresses git printed, each once, in git's order; an address on this computer or a share, and one that cannot be read, is none.</summary>
    public static IReadOnlyList<string> Hosts(string output) =>
        [.. Addresses(output).Select(address => address.Host).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <summary>The host of an address that leads to a server, lower case; null for every other address.</summary>
    public static string? Host(string url) => Read(url).Host;

    /// <summary>The transports whose address holds the server's name and that git itself or its own curl program speaks.</summary>
    private static readonly string[] ServerSchemes = ["http", "https", "ftp", "ftps", "git", "ssh", "git+ssh", "ssh+git"];

    /// <summary>The kinds of proxy git's curl speaks to, as an address's first part.</summary>
    private static readonly string[] ProxySchemes = ["http", "https", "socks4", "socks4a", "socks5", "socks5h"];

    private static readonly Address NotRead = new(AddressKind.NotRead, null);
    private static readonly Address Local = new(AddressKind.Local, null);

    /// <summary>
    /// Where a remote's address leads, by the rules git itself reads one with (its transport.c and connect.c), and
    /// strict where programs disagree (the review's second pass: the host was ended at a slash or a question
    /// mark and not at a number sign, so "https://evil.example#" in front of an at sign and "github.com" read as
    /// github.com, while git reached evil.example):
    /// <list type="bullet">
    /// <item><c>name::rest</c> starts the program git-remote-name (<c>ext::</c> runs any command): not read.</item>
    /// <item><c>scheme://</c>: <c>file</c> is a folder; http, https, ftp, ftps, git, ssh and the two spellings
    /// of git over ssh hold a server, whose host ends at the first slash, question mark or number sign; every
    /// other scheme would start a program of its name: not read.</item>
    /// <item>No scheme: a path when there is no colon, a slash before the first colon, or a drive letter with
    /// its colon; otherwise the scp form, the host before the first colon after a bracketed part.</item>
    /// <item>The part with the host may hold one at sign, the user before it, never two; no percent sign (git
    /// decodes one for ssh, curl does not) and no backslash; the host itself letters, digits, dots, hyphens and
    /// underscores, not starting with a hyphen, or an IPv6 literal in brackets, kept whole; a port digits
    /// alone. Anything else is not read, and nothing is sent to an address that is not read.</item>
    /// </list>
    /// </summary>
    public static Address Read(string address)
    {
        var url = address.Trim();
        if (url.Length == 0 || url.Any(char.IsControl))
        {
            return NotRead;
        }

        var scheme = 0;
        while (scheme < url.Length && (char.IsAsciiLetterOrDigit(url[scheme]) || (scheme > 0 && url[scheme] is '+' or '.' or '-')))
        {
            scheme++;
        }

        if (scheme > 0 && url.AsSpan(scheme).StartsWith("::"))
        {
            return NotRead;
        }

        if (scheme > 0 && url.AsSpan(scheme).StartsWith("://"))
        {
            var name = url[..scheme].ToLowerInvariant();
            if (name == "file")
            {
                return Local;
            }

            var rest = url[(scheme + 3)..];
            var end = rest.IndexOfAny(['/', '?', '#']);
            return ServerSchemes.Contains(name) ? Authority(end < 0 ? rest : rest[..end], withPort: true) : NotRead;
        }

        // git's own rule for an address without a scheme (connect.c, url_is_local_not_ssh).
        var colon = url.IndexOf(':');
        var slash = url.IndexOf('/');
        if (colon < 0 || (slash >= 0 && slash < colon) || (char.IsAsciiLetter(url[0]) && url[1] == ':'))
        {
            return Local;
        }

        // The scp form: the host, with a user before an at sign perhaps, then a colon and the path; an IPv6
        // literal in brackets has colons of its own.
        var open = url.StartsWith('[') ? 0 : url.IndexOf("@[", StringComparison.Ordinal) is >= 0 and var found ? found + 1 : -1;
        var close = open < 0 ? -1 : url.IndexOf(']', open);
        var separator = url.IndexOf(':', close < 0 ? 0 : close);
        return separator > 0 ? Authority(url[..separator], withPort: false) : NotRead;
    }

    /// <summary>The server of the part of an address that holds the user and the host, and after a scheme a port.</summary>
    private static Address Authority(string text, bool withPort)
    {
        if (text.Contains('%') || text.Contains('\\') || text.Any(char.IsWhiteSpace))
        {
            return NotRead;
        }

        var at = text.LastIndexOf('@');
        if (at != text.IndexOf('@'))
        {
            return NotRead;
        }

        var host = text[(at + 1)..];
        if (host.StartsWith('['))
        {
            var close = host.IndexOf(']');
            if (close < 2 || !PortOrNothing(host[(close + 1)..], withPort) || !host[1..close].All(letter => char.IsAsciiHexDigit(letter) || letter is ':' or '.'))
            {
                return NotRead;
            }

            return new Address(AddressKind.Server, host[..(close + 1)].ToLowerInvariant());
        }

        var colon = host.IndexOf(':');
        if (colon >= 0)
        {
            if (!PortOrNothing(host[colon..], withPort))
            {
                return NotRead;
            }

            host = host[..colon];
        }

        return host.Length > 0 && host[0] != '-' && host.All(letter => char.IsLetterOrDigit(letter) || letter is '.' or '-' or '_')
            ? new Address(AddressKind.Server, host.ToLowerInvariant())
            : NotRead;
    }

    /// <summary>What follows a host: nothing, or, where a port may stand, a colon and digits (or the colon alone).</summary>
    private static bool PortOrNothing(string rest, bool withPort) =>
        rest.Length == 0 || (withPort && rest[0] == ':' && rest[1..].All(char.IsAsciiDigit));

    // ---- A proxy the settings name (AG-5.4b) ----

    /// <summary>The proxy git's settings give for an http or https address, the most fitting <c>http.proxy</c> or <c>http.address.proxy</c>, with the kind of file it comes from: one line, the kind, a tab, the proxy; no line and exit code 1 when there is none.</summary>
    public static IReadOnlyList<string> ProxyArguments(string address) => ["config", "--show-scope", "--get-urlmatch", "http.proxy", address];

    /// <summary>Whether an address goes through git's curl program, which is what a proxy of the settings is for.</summary>
    public static bool UsesHttp(string address) =>
        address.TrimStart().StartsWith("http://", StringComparison.OrdinalIgnoreCase) || address.TrimStart().StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>The kinds of file that are not the repository's: the computer's, the user's, and what the command itself was given.</summary>
    private static readonly string[] OtherScopes = ["system", "global", "command"];

    /// <summary>
    /// The proxy a fetch, pull or push to <paramref name="remote"/> goes through when the repository's own settings
    /// name it: the remote's own (<c>remote.name.proxy</c>, which git takes before any other), else git's answer
    /// to <see cref="ProxyArguments"/> (<paramref name="answer"/>; null or empty for none). A proxy of the
    /// user's own settings, or the computer's, is the user's and is not named. Null when there is none of the
    /// repository's; an address of <see cref="AddressKind.NotRead"/> when its host cannot be read.
    /// </summary>
    public static Address? RepositoryProxy(IReadOnlyList<ListedSetting> settings, string remote, string? answer)
    {
        var (scope, proxy) = InForce(settings, $"remote.{remote}.proxy") is { } own
            ? (own.Scope, own.Value.Trim())
            : (answer ?? string.Empty).Split('\n')[0].TrimEnd('\r').Split('\t', 2) is { Length: 2 } parts ? (parts[0], parts[1].Trim()) : (string.Empty, string.Empty);
        if (proxy.Length == 0 || OtherScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        // A proxy is written with or without its kind in front: "proxy.example:8080" is an http proxy.
        var kind = proxy.IndexOf("://", StringComparison.Ordinal);
        var rest = kind < 0 ? proxy : proxy[(kind + 3)..];
        if (kind >= 0 && !ProxySchemes.Contains(proxy[..kind], StringComparer.OrdinalIgnoreCase))
        {
            return NotRead;
        }

        var end = rest.IndexOfAny(['/', '?', '#']);
        return Authority(end < 0 ? rest : rest[..end], withPort: true);
    }

    // ---- What git says, without a sign-in ----

    /// <summary>
    /// A text of git's without the user and the password of any address in it ("https://host/a.git" from an
    /// address with a user, a colon, a password and an at sign before the host): every line of git's that goes
    /// to the log or to the user passes here (<see cref="GitResult"/>).
    /// </summary>
    public static string WithoutSignIn(string text) => text.Contains('@') ? SignIn().Replace(text, string.Empty) : text;

    /// <summary>What stands between "scheme://" and the host: everything up to the last at sign before a slash or a space.</summary>
    [GeneratedRegex(@"(?<=\b[a-z][a-z0-9+.\-]*://)[^/\s]*@", RegexOptions.IgnoreCase)]
    private static partial Regex SignIn();
}
