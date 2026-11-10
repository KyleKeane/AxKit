using AxKit.Core;

namespace AxHelp.Core;

/// <summary>The list under axHelp's field (SPEC.md HE-3): the earlier questions that match what is typed, and what is said of them.</summary>
public static class HelpLines
{
    /// <summary>The list's one line while nothing was ever asked.</summary>
    public const string None = "No questions yet: type one and press Enter";

    /// <summary>The list's one line when no earlier question holds the typed words.</summary>
    public static string NoMatch(string typed) => $"No earlier question matches {typed.Trim()}";

    /// <summary>The exchanges whose question holds every word typed, newest first; all of them for nothing typed (HE-3.2).</summary>
    public static IReadOnlyList<HelpExchange> Matches(IReadOnlyList<HelpExchange> exchanges, string typed)
    {
        var words = TextMatch.Words(typed);
        return words.Count == 0 ? exchanges : exchanges.Where(exchange => TextMatch.Rank(TextMatch.Fold(exchange.Question), words) is not null).ToList();
    }

    /// <summary>"what does Ctrl+Shift+W do, 1 of 3": a match as the selection reaches it (HE-3.3).</summary>
    public static string Spoken(HelpExchange exchange, int index, int count) => $"{exchange.Question}, {index + 1} of {count}";
}
