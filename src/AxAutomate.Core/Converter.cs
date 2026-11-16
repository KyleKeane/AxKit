using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>
/// One press the converter moves to a <c>do</c> line (docs/axKit/user-workflows.md WF-10): the line of the file, from
/// 1; the press on that line's <c>keys</c> step, from 0; and the canonical name of the command the press ran.
/// </summary>
public sealed record Conversion(int Line, int Press, string Name);

/// <summary>
/// The converter's pure half (docs/axKit/user-workflows.md WF-10.1 to WF-10.3): whether a press of a <c>keys</c> line
/// is a command the live window lists (<see cref="NameFor"/>), asked by <see cref="Run"/> at the moment of the press,
/// and a script's lines with those presses written as <c>do</c> lines (<see cref="Rewrite"/>). The command line plays
/// the script, writes the lines and proves them by a second play (<c>ConvertRun</c> in <c>tools/AxAutomate</c>).
/// Tested in <c>tests/AxAutomate.Tests/ConverterTests</c>.
/// </summary>
public static class Converter
{
    private const string If = "iffound ";
    private const string Keys = "keys ";

    /// <summary>
    /// The canonical name of the command a press runs (WF-10.1), or null when the press stays a <c>keys</c> press:
    /// no listing, or one taken while a notice shows; no row, or several rows, with the chord as their <c>key</c> and
    /// as their <c>now</c> (both in <see cref="KeyChord.Normal"/>'s order), so a chord that is only a further key of
    /// its command stays, and one that does not act at this moment; and a name that the step <c>do</c> would not
    /// answer with this very press (<see cref="DoStep.Decide"/>: two rows of one label and one menus), since the
    /// press must not change. The name is <see cref="CommandNames.CanonicalOf"/> over the listing's rows.
    /// </summary>
    public static string? NameFor(WindowCommands? listing, string chord)
    {
        if (listing is null || listing.Notice is not null || string.IsNullOrWhiteSpace(chord))
        {
            return null;
        }

        var wanted = KeyChord.Normal(chord.Trim());
        var rows = Enumerable.Range(0, listing.Commands.Count)
            .Where(i => Same(listing.Commands[i].Key, wanted) && Same(listing.Commands[i].Now, wanted))
            .ToList();
        if (rows.Count != 1)
        {
            return null;
        }

        var name = CommandNames.CanonicalOf([.. listing.Commands.Select(row => new CommandName(row.Label, row.Menu))], rows[0]);
        var decision = DoStep.Decide(listing, name);
        return decision.Kind == DoKind.Press && Same(decision.Chord, wanted) ? name : null;
    }

    /// <summary>
    /// The file's lines with the converted presses moved (WF-10.3). A <c>keys</c> line that holds one is written as
    /// several lines in the order of its presses: each converted press a <c>do &lt;name&gt;</c> line, each run of
    /// other presses one <c>keys</c> line. The line's indentation, an <c>iffound</c> before its step and its line end
    /// (a carriage return a line carries when the file was split at its line feeds) stand on every line it becomes.
    /// Every other line stays as it is, character for character: comments, blank lines, a line no conversion names,
    /// and a line that is no <c>keys</c> step or has no press at the place a conversion gives.
    /// </summary>
    public static IReadOnlyList<string> Rewrite(IReadOnlyList<string> fileLines, IReadOnlyList<Conversion> conversions)
    {
        var result = new List<string>();
        for (var i = 0; i < fileLines.Count; i++)
        {
            var line = fileLines[i];
            var body = line.TrimEnd();
            var end = line[body.Length..];
            var step = body.TrimStart();
            var before = body[..(body.Length - step.Length)];
            while (step.StartsWith(If, StringComparison.Ordinal))
            {
                before += If;
                step = step[If.Length..];
            }

            var presses = step.StartsWith(Keys, StringComparison.Ordinal) ? step[Keys.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries) : [];
            var names = new string?[presses.Length];
            foreach (var conversion in conversions.Where(conversion => conversion.Line == i + 1 && conversion.Press >= 0 && conversion.Press < presses.Length))
            {
                names[conversion.Press] ??= conversion.Name;
            }

            if (names.All(name => name is null))
            {
                result.Add(line);
                continue;
            }

            // A last line without a line end: the lines before the last it becomes end as the line above it does.
            var between = end.Length > 0 || i == 0 ? end : fileLines[i - 1][fileLines[i - 1].TrimEnd().Length..];
            var written = new List<string>();
            var run = new List<string>();
            for (var press = 0; press < presses.Length; press++)
            {
                if (names[press] is not { } name)
                {
                    run.Add(presses[press]);
                    continue;
                }

                if (run.Count > 0)
                {
                    written.Add(before + Keys + string.Join(' ', run));
                    run.Clear();
                }

                written.Add(before + "do " + name);
            }

            if (run.Count > 0)
            {
                written.Add(before + Keys + string.Join(' ', run));
            }

            for (var at = 0; at < written.Count; at++)
            {
                result.Add(written[at] + (at < written.Count - 1 ? between : end));
            }
        }

        return result;
    }

    private static bool Same(string chord, string wanted) =>
        chord.Length > 0 && string.Equals(KeyChord.Normal(chord), wanted, StringComparison.OrdinalIgnoreCase);
}
