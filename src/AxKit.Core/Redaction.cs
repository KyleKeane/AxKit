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
    // or WSL (/mnt/c/Users) spell the drive; the placeholder names below are left alone.
    [GeneratedRegex(@"(?i)((?:[A-Z]:|/mnt/[A-Z]|/[A-Z])(?:\\{1,2}|/)Users(?:\\{1,2}|/))(?!<user>|Public\b|Default\b|runneradmin\b)[^\\/\s""'<>|:*?]+")]
    private static partial Regex UserPathRegex();

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
    /// <c>&lt;pipe&gt;</c>, <c>&lt;id&gt;</c> and <c>&lt;name&gt;</c>.
    /// </summary>
    public static string Redact(string text, IEnumerable<string>? names = null)
    {
        text = EmailRegex().Replace(text, "<email>");
        text = PipeRegex().Replace(text, "<pipe>");
        text = UserPathRegex().Replace(text, "$1<user>");
        text = OneDriveRegex().Replace(text, "OneDrive - <org>");
        text = IdRegex().Replace(text, "<id>");
        foreach (var name in (names ?? DefaultNames()).Where(IsDistinctive))
        {
            text = Regex.Replace(text, $@"(?i)\b{Regex.Escape(name)}\b", "<name>");
        }

        return text;
    }

    // The subscription tier Claude Code's header line names after the model ("· Claude Max").
    [GeneratedRegex(@"(?<=Claude )(Max|Pro|Team|Enterprise|Ultra)\b")]
    private static partial Regex TierRegex();

    // When a usage limit resets, after Claude Code's row "You've used 76% of your weekly limit · resets 11pm
    // (America/Los_Angeles)" (2026-11-26): the hour and the account's time zone, up to the next control character. Read
    // as Latin-1, as MaskRecording reads the bytes, so the middle dot is its two bytes of UTF-8.
    [GeneratedRegex(@"(?<=You've used \d{1,3}% of your [a-z ]{1,40} limit Â· resets )[^\x00-\x1F\x7F]+")]
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

    /// <summary>The Windows account name, which paths and prompts can show, and the computer's and the domain's names (2026-10-25).</summary>
    public static IReadOnlyList<string> DefaultNames() => [Environment.UserName, Environment.MachineName, Environment.UserDomainName];

    // A name short or common enough to be part of ordinary words is not replaced.
    private static bool IsDistinctive(string name) =>
        name.Length >= 3 && !name.Equals("user", StringComparison.OrdinalIgnoreCase) && !name.Equals("admin", StringComparison.OrdinalIgnoreCase);
}
