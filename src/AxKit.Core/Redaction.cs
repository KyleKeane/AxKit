using System.Text.RegularExpressions;

namespace AxKit.Core;

/// <summary>
/// Personal data in text read off Claude's screen: `/status` prints the account's e-mail address, session
/// id and a pipe name, and paths name the Windows account. <see cref="Redact"/> replaces each with a placeholder, so
/// that what a tool prints about a recording can be shown and shared (PtyCapture --dump); <see cref="Find"/> lists them,
/// for the test that keeps them out of the repository.
/// </summary>
public static partial class Redaction
{
    // Without backtracking: a long run of letters and dots would otherwise take time growing with its square (S-45).
    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.NonBacktracking)]
    private static partial Regex EmailRegex();

    // The account folder under a drive's Users folder, also with doubled or forward slashes and as Git Bash (/c/Users)
    // or WSL (/mnt/c/Users) spell the drive; the placeholder names below are left alone. The folder's name ends at a
    // space, so a profile folder with a space in its name is replaced whole before this runs (WithoutProfile, and
    // the names loop).
    [GeneratedRegex(@"(?i)((?:[A-Z]:|/mnt/[A-Z]|/[A-Z])(?:\\{1,2}|/)Users(?:\\{1,2}|/))(?!<user>|Public\b|Default\b|runneradmin\b)[^\\/\s""'<>|:*?]+")]
    private static partial Regex UserPathRegex();

    // An account folder the names loop replaced before the pattern above ran: <user> there too, as it always was.
    [GeneratedRegex(@"(?i)((?:[A-Z]:|/mnt/[A-Z]|/[A-Z])(?:\\{1,2}|/)Users(?:\\{1,2}|/))<name>")]
    private static partial Regex NamedUserRegex();

    [GeneratedRegex(@"\\\\\.\\pipe\\[^\s""'<>|]+")]
    private static partial Regex PipeRegex();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b")]
    private static partial Regex IdRegex();

    // A work OneDrive's folder names the organisation ("OneDrive - Contoso"; security review 2026-10-25).
    [GeneratedRegex(@"OneDrive - [^\\/""'<>|:*?\r\n]+")]
    private static partial Regex OneDriveRegex();

    /// <summary>
    /// The text with e-mail addresses, the account folder in paths under Users, pipe names, session ids and the
    /// given names (the Windows account name, by default) replaced by <c>&lt;email&gt;</c>, <c>&lt;user&gt;</c>,
    /// <c>&lt;pipe&gt;</c>, <c>&lt;id&gt;</c> and <c>&lt;name&gt;</c>. The profile folder itself
    /// (<paramref name="profile"/>, this account's by default) goes first and whole, and the names go before the
    /// pattern for paths under Users, which ends a folder's name at a space: a profile folder of two words lost its
    /// first word alone until 2026-11-27, and the rest stayed in the text.
    /// </summary>
    public static string Redact(string text, IEnumerable<string>? names = null, string? profile = null)
    {
        profile ??= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        text = EmailRegex().Replace(text, "<email>");
        text = PipeRegex().Replace(text, "<pipe>");
        text = WithoutProfile(text, profile);
        // The longest first, so that a name of two words is not left with its second word by a shorter name inside it.
        foreach (var name in (names ?? DefaultNames(profile)).Where(IsDistinctive).OrderByDescending(name => name.Length))
        {
            text = Regex.Replace(text, $@"(?i)\b{Regex.Escape(name)}\b", "<name>");
        }

        text = NamedUserRegex().Replace(text, "$1<user>");
        text = UserPathRegex().Replace(text, "$1<user>");
        text = OneDriveRegex().Replace(text, "OneDrive - <org>");
        text = IdRegex().Replace(text, "<id>");
        return text;
    }

    /// <summary>
    /// The text with every session id replaced by <c>&lt;id&gt;</c> and nothing else changed: for a log line that
    /// names a program's command line, which can hold one (<c>claude --resume &lt;id&gt;</c>). An id is never logged
    /// (until 2026-11-27 the line that says a program was started wrote it out).
    /// </summary>
    public static string WithoutIds(string text) => IdRegex().Replace(text, "<id>");

    /// <summary>
    /// The text with the profile folder's own name replaced by <c>&lt;user&gt;</c> wherever the folder's path stands
    /// in it: with single or doubled backslashes or forward slashes, the drive also as Git Bash and WSL spell it,
    /// without regard to case. The name is taken whole, spaces and all, which the pattern for paths under Users cannot
    /// do. A profile that is no full path of two parts or more changes nothing.
    /// </summary>
    public static string WithoutProfile(string text, string? profile)
    {
        var parts = (profile ?? string.Empty).Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !Path.IsPathFullyQualified(profile!))
        {
            return text;
        }

        const string separator = @"(?:\\{1,2}|/)";
        var drive = parts[0].Length == 2 && parts[0][1] == ':' && char.IsAsciiLetter(parts[0][0])
            ? $"(?:{parts[0][0]}:|/mnt/{parts[0][0]}|/{parts[0][0]})"
            : Regex.Escape(parts[0]);
        var between = string.Concat(parts[1..^1].Select(part => separator + Regex.Escape(part)));
        return Regex.Replace(text, $@"(?i)({drive}{between}{separator}){Regex.Escape(parts[^1])}(?!\w)", "$1<user>");
    }

    // The subscription tier Claude Code's header line names after the model ("· Claude Max").
    [GeneratedRegex(@"(?<=Claude )(Max|Pro|Team|Enterprise|Ultra)\b")]
    private static partial Regex TierRegex();

    // When a usage limit resets, after Claude Code's row "You've used 76% of your weekly limit · resets 11pm
    // (America/Los_Angeles)" (2026-11-26): the hour and the account's time zone, up to the next control character. Read
    // as Latin-1, as MaskRecording reads the bytes, so the middle dot is its two bytes of UTF-8.
    // The kind of limit is any words up to the dot ("weekly", "5-hour", "Opus"), as the classifier's pattern reads it.
    [GeneratedRegex(@"(?<=You've used \d{1,3}% of your [^\x00-\x1F\x7F\xC2]{1,60} limit Â· resets )[^\x00-\x1F\x7F]+")]
    private static partial Regex UsageResetRegex();

    /// <summary>
    /// A recording's bytes with every session id, the subscription tier and the time a usage limit resets masked, each
    /// by as many bytes as it had (zeros for an id's digits, x for the tier's letters and for the reset's hour and time
    /// zone), so that every cursor position of the recording stays right (the item "the recordings under
    /// tests/fixtures hold Claude Code session ids and the subscription tier", 2026-11-25; the reset, which names the
    /// account's time zone, since 2026-11-26). The bytes are read one character each (Latin-1), so nothing else of
    /// them changes.
    /// </summary>
    public static byte[] MaskRecording(byte[] recording)
    {
        var text = System.Text.Encoding.Latin1.GetString(recording);
        text = IdRegex().Replace(text, match => Regex.Replace(match.Value, "[0-9a-fA-F]", "0"));
        text = TierRegex().Replace(text, match => new string('x', match.Length));
        text = UsageResetRegex().Replace(text, match => new string('x', match.Length));
        return System.Text.Encoding.Latin1.GetBytes(text);
    }

    /// <summary>The e-mail addresses and account paths in <paramref name="text"/>: what must never be committed.</summary>
    public static IEnumerable<string> Find(string text) =>
        EmailRegex().Matches(text).Concat(UserPathRegex().Matches(text)).Select(match => match.Value);

    /// <summary>
    /// The Windows account name, which paths and prompts can show, the computer's and the domain's names (2026-10-25),
    /// and the name of the profile folder, which is not always the account's (2026-11-27).
    /// </summary>
    public static IReadOnlyList<string> DefaultNames(string? profile = null)
    {
        var folder = Path.GetFileName((profile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).TrimEnd('\\', '/'));
        return folder.Length == 0
            ? [Environment.UserName, Environment.MachineName, Environment.UserDomainName]
            : [Environment.UserName, Environment.MachineName, Environment.UserDomainName, folder];
    }

    // A name short or common enough to be part of ordinary words is not replaced.
    private static bool IsDistinctive(string name) =>
        name.Length >= 3 && !name.Equals("user", StringComparison.OrdinalIgnoreCase) && !name.Equals("admin", StringComparison.OrdinalIgnoreCase);
}
