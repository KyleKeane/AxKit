using System.Text.RegularExpressions;

namespace AxGit.Core;

/// <summary>Where an address leads: a folder of this computer, a server (a share among them), or nothing axGIT can read.</summary>
public enum AddressKind
{
    /// <summary>A path on this computer, or a <c>file:///</c> address with a drive: no service of anyone else's.</summary>
    Local,

    /// <summary>A server, whose host is known: a host of a transport, or the computer a share lies on.</summary>
    Server,

    /// <summary>An address that is not in one of the forms axGIT reads (<see cref="GitRemote.Read"/>): nothing may be sent to it.</summary>
    NotRead,
}

/// <summary>
/// An address as <see cref="GitRemote.Read"/> reads it: <paramref name="Host"/>, in lower case, for a server;
/// <paramref name="Port"/> when the address gives one; <paramref name="Folder"/> when git reaches the address as a
/// folder, with its own transport for a repository on disk (a path on this computer, or a share): there git runs
/// the other repository's own programs on a push, so it is read before one (AG-5.4c).
/// </summary>
public sealed record Address(AddressKind Kind, string? Host, int? Port = null, string? Folder = null);

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

    /// <summary>The transports whose address axGIT reads, each with whether a user may stand before the host and whether a password may stand after that user.</summary>
    private static readonly (string Scheme, bool User, bool Password)[] ServerSchemes =
        [("https", true, true), ("http", true, true), ("ssh", true, false), ("git", false, false)];

    /// <summary>The kinds of proxy git's curl speaks to, as an address's first part.</summary>
    private static readonly string[] ProxySchemes = ["http", "https", "socks4", "socks4a", "socks5", "socks5h"];

    private static readonly Address NotRead = new(AddressKind.NotRead, null);

    /// <summary>
    /// Where a remote's address leads (AG-5.4a; the review's third pass of 2026-11-27, D4). The rule is turned
    /// around, as the settings' was: an address is read only in one of the forms below, and every other text is
    /// <see cref="AddressKind.NotRead"/>, to which nothing is sent. Until then the reader followed git's own rules
    /// and was lenient where they are, and each lenience was a way to name one host and reach another
    /// (<c>[127.0.0.1]:x@[evil.invalid]:repo.git</c> read as 127.0.0.1 while git ran ssh to evil.invalid;
    /// <c>c:nul</c>, <c>c:a:b</c>, <c>c:repo.</c> and <c>d:x&lt;y</c> read as folders while git, which takes them
    /// for no valid Windows path, ran ssh to the host c or d). The forms:
    /// <list type="bullet">
    /// <item><c>https://host[:port]/path</c> and <c>http://</c>, a user, and a password after a colon, allowed
    /// before the host; <c>ssh://[user@]host[:port]/path</c>; <c>git://host[:port]/path</c>. The scheme in any
    /// case; the path starts with a slash.</item>
    /// <item>The scp form <c>[user@]host:path</c>, with no slash before the colon, a host of more than one letter
    /// (one letter and a colon is a drive) and a path after it.</item>
    /// <item>A path on this computer: a drive, its colon and a slash or backslash, then a valid Windows path;
    /// <c>.</c>, <c>..</c>, and a path starting with <c>./</c>, <c>../</c> (or with backslashes); an absolute path
    /// starting with one slash; <c>file:///</c> and a drive.</item>
    /// <item>A share, <c>//host/share/...</c>, <c>\\host\share\...</c> or <c>file://host/share/...</c>: a server,
    /// the computer the share lies on, asked about like any other (M1; until then a share counted as this
    /// computer).</item>
    /// </list>
    /// A user and a host are ASCII letters, digits, dots and hyphens (a user an underscore too, a password the
    /// characters that need no escape in an address), never starting with a hyphen or a dot: a host written in
    /// letters outside ASCII is read only in its punycode form (<c>xn--</c>), which is what reaches the network
    /// anyway. An IPv6 host stands in brackets, in a form with a scheme only. A port is 1 to 65535.
    /// </summary>
    public static Address Read(string address)
    {
        var url = address.Trim();
        if (url.Length == 0 || url.Any(char.IsControl))
        {
            return NotRead;
        }

        if (url is "." or "..")
        {
            return new Address(AddressKind.Local, null, Folder: url);
        }

        // name::rest starts the program git-remote-name (ext:: runs any command): never read.
        var transport = url.IndexOf("::", StringComparison.Ordinal);
        if (transport > 0 && url[..transport].All(letter => char.IsAsciiLetterOrDigit(letter) || letter is '+' or '.' or '-'))
        {
            return NotRead;
        }

        // A share, with slashes, backslashes, or file:// and a host.
        if (url.Length > 2 && IsSeparator(url[0]) && IsSeparator(url[1]))
        {
            return Share(url[2..], url);
        }

        var schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd > 0)
        {
            var scheme = url[..schemeEnd].ToLowerInvariant();
            var rest = url[(schemeEnd + 3)..];
            if (scheme == "file")
            {
                // file:///C:/path is a folder here; file://host/share/path is a share, which git reaches as //host/share/path.
                return rest.StartsWith('/')
                    ? (IsDrivePath(rest[1..]) ? new Address(AddressKind.Local, null, Folder: rest[1..]) : NotRead)
                    : Share(rest, "//" + rest);
            }

            return ServerSchemes.FirstOrDefault(known => known.Scheme == scheme) is { Scheme: not null } form
                ? WithScheme(rest, form.User, form.Password)
                : NotRead;
        }

        if (IsDrivePath(url) || IsRelativePath(url) || IsAbsolutePath(url))
        {
            return new Address(AddressKind.Local, null, Folder: url);
        }

        return Scp(url);
    }

    private static bool IsSeparator(char letter) => letter is '/' or '\\';

    /// <summary>The part of a form with a scheme after "://": the authority up to the first slash, which must be there, then the path.</summary>
    private static Address WithScheme(string rest, bool user, bool password)
    {
        var slash = rest.IndexOf('/');
        if (slash <= 0)
        {
            return NotRead;
        }

        var authority = rest[..slash];
        var at = authority.IndexOf('@');
        if (at >= 0)
        {
            if (!user || !IsSignIn(authority[..at], password))
            {
                return NotRead;
            }

            authority = authority[(at + 1)..];
        }

        if (authority.StartsWith('['))
        {
            var close = authority.IndexOf(']');
            if (close < 3 || !authority[1..close].All(letter => char.IsAsciiHexDigit(letter) || letter is ':' or '.') || !authority[1..close].Contains(':'))
            {
                return NotRead;
            }

            return PortOf(authority[(close + 1)..]) is var (fits, port) && fits
                ? new Address(AddressKind.Server, authority[..(close + 1)].ToLowerInvariant(), port)
                : NotRead;
        }

        var colon = authority.IndexOf(':');
        var host = colon < 0 ? authority : authority[..colon];
        var (portFits, number) = colon < 0 ? (true, null) : PortOf(authority[colon..]);
        return portFits && IsHost(host) ? new Address(AddressKind.Server, host.ToLowerInvariant(), number) : NotRead;
    }

    /// <summary>The scp form, <c>[user@]host:path</c>: a host of two letters or more, no slash before its colon, a path after it.</summary>
    private static Address Scp(string url)
    {
        var colon = url.IndexOf(':');
        if (colon <= 0 || colon == url.Length - 1 || url[..colon].IndexOfAny(['/', '\\', '[', ']']) >= 0)
        {
            return NotRead;
        }

        var authority = url[..colon];
        var at = authority.IndexOf('@');
        if (at >= 0 && !IsSignIn(authority[..at], password: false))
        {
            return NotRead;
        }

        var host = authority[(at + 1)..];
        return host.Length > 1 && IsHost(host) ? new Address(AddressKind.Server, host.ToLowerInvariant()) : NotRead;
    }

    /// <summary>A share after its two separators: the host up to the next separator, and a share's name after it.</summary>
    private static Address Share(string rest, string folder)
    {
        var end = rest.IndexOfAny(['/', '\\']);
        if (end <= 0 || end == rest.Length - 1 || !IsHost(rest[..end]) || !IsWindowsPath(rest[(end + 1)..]))
        {
            return NotRead;
        }

        return new Address(AddressKind.Server, rest[..end].ToLowerInvariant(), Folder: folder);
    }

    /// <summary>A host: ASCII letters, digits, dots and hyphens, not starting with a hyphen or a dot.</summary>
    private static bool IsHost(string host) =>
        host.Length > 0 && host[0] is not ('-' or '.') && host.All(letter => char.IsAsciiLetterOrDigit(letter) || letter is '.' or '-');

    /// <summary>A user before an at sign, and after an http address's user a colon and a password of the characters an address needs no escape for.</summary>
    private static bool IsSignIn(string text, bool password)
    {
        var colon = text.IndexOf(':');
        var user = colon < 0 ? text : text[..colon];
        if (user.Length == 0 || user[0] is '-' or '.' || !user.All(letter => char.IsAsciiLetterOrDigit(letter) || letter is '.' or '-' or '_'))
        {
            return false;
        }

        return colon < 0 || (password && text[(colon + 1)..].All(letter => char.IsAsciiLetterOrDigit(letter) || letter is '.' or '-' or '_' or '~'));
    }

    /// <summary>What follows a host in a form with a scheme: nothing, or a colon and a port of 1 to 65535.</summary>
    private static (bool Fits, int? Port) PortOf(string rest)
    {
        if (rest.Length == 0)
        {
            return (true, null);
        }

        return rest[0] == ':' && rest.Length is > 1 and <= 6 && rest[1..].All(char.IsAsciiDigit) && int.Parse(rest[1..], System.Globalization.CultureInfo.InvariantCulture) is >= 1 and <= 65535 and var port
            ? (true, port)
            : (false, null);
    }

    /// <summary>A drive, its colon and a separator, then a valid Windows path (git takes anything else after a drive letter for the scp form).</summary>
    private static bool IsDrivePath(string url) =>
        url.Length >= 3 && char.IsAsciiLetter(url[0]) && url[1] == ':' && IsSeparator(url[2]) && IsWindowsPath(url[3..]);

    /// <summary>A path that starts with ./ or ../, with either separator, then a valid Windows path.</summary>
    private static bool IsRelativePath(string url) =>
        (url.StartsWith("./", StringComparison.Ordinal) || url.StartsWith(".\\", StringComparison.Ordinal) || url.StartsWith("../", StringComparison.Ordinal) || url.StartsWith("..\\", StringComparison.Ordinal))
        && IsWindowsPath(url);

    /// <summary>A path from the root of the current drive: one separator, then a valid Windows path.</summary>
    private static bool IsAbsolutePath(string url) =>
        url.Length > 1 && IsSeparator(url[0]) && !IsSeparator(url[1]) && IsWindowsPath(url[1..]);

    /// <summary>The characters Windows allows in no file name.</summary>
    private static readonly char[] NotInNames = ['<', '>', ':', '"', '|', '?', '*'];

    /// <summary>The names Windows gives to devices, which are no file in any folder: with or without an extension.</summary>
    private static readonly string[] DeviceNames =
        ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"];

    /// <summary>
    /// A path Windows can hold, as git for Windows checks one (is_valid_win32_path): no name with a character
    /// Windows refuses, none ending in a dot or a space (but . and ..), none that is a device's name.
    /// </summary>
    private static bool IsWindowsPath(string path) =>
        path.Split('/', '\\').All(name =>
            name is "" or "." or ".."
            || (name.IndexOfAny(NotInNames) < 0 && !name.EndsWith('.') && !name.EndsWith(' ')
                && !DeviceNames.Contains(name.Split('.')[0].TrimEnd(), StringComparer.OrdinalIgnoreCase)));

    /// <summary>
    /// The reminder's words for a server (AX-11.6): its host, and the port when the address gives one, so that
    /// "host, port 2222" is said where that is what git reaches.
    /// </summary>
    public static string Place(string host, int? port) => port is { } number ? $"{host}, port {number}" : host;

    /// <summary>The server of the part of a proxy's address that holds the user and the host, and a port.</summary>
    private static Address Authority(string text)
    {
        var at = text.LastIndexOf('@');
        if (at != text.IndexOf('@') || (at >= 0 && !IsSignIn(text[..at], password: true)))
        {
            return NotRead;
        }

        var host = text[(at + 1)..];
        if (host.StartsWith('['))
        {
            var close = host.IndexOf(']');
            return close >= 3 && host[1..close].All(letter => char.IsAsciiHexDigit(letter) || letter is ':' or '.') && PortOf(host[(close + 1)..]).Fits
                ? new Address(AddressKind.Server, host[..(close + 1)].ToLowerInvariant(), PortOf(host[(close + 1)..]).Port)
                : NotRead;
        }

        var colon = host.IndexOf(':');
        var name = colon < 0 ? host : host[..colon];
        var (fits, port) = colon < 0 ? (true, null) : PortOf(host[colon..]);
        return fits && IsHost(name) ? new Address(AddressKind.Server, name.ToLowerInvariant(), port) : NotRead;
    }

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

        var end = rest.IndexOf('/');
        return Authority(end < 0 ? rest : rest[..end]);
    }

    // ---- What git says, without a sign-in ----

    /// <summary>
    /// A text of git's without the user and the password of any address in it ("https://host/a.git" from an
    /// address with a user, a colon, a password and an at sign before the host): every line of git's that goes
    /// to the log or to the user passes here (<see cref="GitResult"/>). Also a sign-in with a tab or another
    /// control character in it, which an address shown as not read can hold, and a proxy written without its
    /// kind in front, <c>user:secret@host:port</c> (the review's third pass, L11).
    /// </summary>
    public static string WithoutSignIn(string text) =>
        text.Contains('@') ? BareSignIn().Replace(SignIn().Replace(text, string.Empty), string.Empty) : text;

    /// <summary>What stands between "scheme://" and the host: everything up to the last at sign before a slash, a space or a line's end.</summary>
    [GeneratedRegex(@"(?<=\b[a-z][a-z0-9+.\-]*://)[^/\r\n ]*@", RegexOptions.IgnoreCase)]
    private static partial Regex SignIn();

    /// <summary>A user, a colon, a password and an at sign at the start of a word: a proxy's sign-in written without "scheme://".</summary>
    [GeneratedRegex(@"(?<=^|[\s'""=(<\[,])[A-Za-z0-9._~!$&*+;%-]+:[^\s/@'""]*@")]
    private static partial Regex BareSignIn();
}
