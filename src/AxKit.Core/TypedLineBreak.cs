namespace AxKit.Core;

/// <summary>
/// Whether a key put a line break into a field (docs/axKit/SPEC.md AX-7.8, 2026-11-11): NVDA says nothing for Enter,
/// so a field says "new line" when a line break was typed. The field keeps its text and selection as the key went
/// down and compares them with its text and caret once the key has been handled: a line break was typed when the
/// selection was replaced by a line break, with only the spaces or tabs of an indent after it, and the caret stands
/// after that. Enter that sends a message, closes a notice or meets a read-only field changes nothing of the kind.
/// </summary>
public static class TypedLineBreak
{
    /// <summary>The words said, through the window's one announcement.</summary>
    public const string Said = "new line";

    /// <param name="before">The text as the key went down.</param>
    /// <param name="start">The selection's start then.</param>
    /// <param name="length">The selection's length then.</param>
    /// <param name="after">The text once the key was handled.</param>
    /// <param name="caret">The caret then.</param>
    public static bool Inserted(string before, int start, int length, string after, int caret)
    {
        if (start < 0 || length < 0 || start + length > before.Length)
        {
            return false;
        }

        var prefix = before[..start];
        var suffix = before[(start + length)..];
        if (after.Length < prefix.Length + suffix.Length || !after.StartsWith(prefix, StringComparison.Ordinal) || !after.EndsWith(suffix, StringComparison.Ordinal))
        {
            return false;
        }

        var inserted = after.Substring(prefix.Length, after.Length - prefix.Length - suffix.Length);
        if (caret != prefix.Length + inserted.Length || inserted.Length == 0)
        {
            return false;
        }

        return (inserted[0] == '\r' || inserted[0] == '\n') && inserted.All(c => c is '\r' or '\n' or ' ' or '\t');
    }
}
