namespace AxKit.Core;

/// <summary>
/// Several announcements said as one (docs/axKit/SPEC.md AX-7.3): what a window joins when one comes on the heels of
/// another, when words wait to go ahead of the next ("Claude ran Word wrap", docs/axKit/mcp.md MC-7.5) and when a
/// window says what waited for its first activation. Pure; the window base does the waiting
/// (<c>AxKit.Forms/AxKitForm.Notices.cs</c>). Tested in <c>tests/AxKit.Tests/SaidTogetherTests</c>.
/// </summary>
public static class SaidTogether
{
    /// <summary>
    /// "Ready. Sent. Done": the pieces joined by one full stop and a space. A piece that is followed by another loses
    /// its own trailing full stops and spaces, so that the join never doubles them, and a piece of nothing else is
    /// left out. The last piece arrives as it was given, its own full stop included: the words of a command run for
    /// an AI tool are the words its key gives (docs/axKit/mcp.md MC-7.5; until 2026-11-16 the last piece lost its
    /// full stop too, so "Block quote, A block quote, line one." ended differently after "Claude ran Block quote").
    /// </summary>
    public static string Sentence(IEnumerable<string> pieces)
    {
        var kept = pieces.Where(piece => piece.TrimEnd('.', ' ').Length > 0).ToList();
        for (var i = 0; i < kept.Count - 1; i++)
        {
            kept[i] = kept[i].TrimEnd('.', ' ');
        }

        return string.Join(". ", kept);
    }
}
