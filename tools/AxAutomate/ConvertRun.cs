using System.Text;
using AxAutomate.Core;

namespace AxAutomate;

/// <summary>
/// The converter in the command line (docs/axKit/user-workflows.md WF-10, WP-B15): under <c>--convert</c> each script
/// is played with the window asked before every press that may be a command's (<see cref="PlayOptions.Convert"/>),
/// and a script that passed has those presses written as <c>do</c> lines (<see cref="Converter.Rewrite"/>). The file
/// as rewritten is then played once more, without the option: when it passes it stays, and the rows its <c>do</c>
/// steps ran are merged into the command inventory (<see cref="InventoryRun.Converted"/>, WF-10.2); when it does not,
/// the file is put back byte for byte and the script is named (WF-10.4). A script that did not pass is never touched.
/// One line a script says which; the run's last lines count them. The pure part is <see cref="Converter"/>.
/// </summary>
internal static class ConvertRun
{
    private static int _presses;
    private static int _scripts;

    /// <summary>The scripts put back as they were, each with why: said again after the summary.</summary>
    private static readonly List<string> PutBack = [];

    /// <summary>Whether the run converts (<see cref="RunOptions.Convert"/>): the engine then collects each script's conversions.</summary>
    public static bool On { get; private set; }

    public static void Start(bool convert)
    {
        On = convert;
        _presses = 0;
        _scripts = 0;
        PutBack.Clear();
    }

    /// <summary>
    /// After a script played with the option: its result as the conversion leaves it. <paramref name="name"/> is the
    /// script as the run's lines name it; <paramref name="playAgain"/> plays the file as it then stands, without the
    /// option and with fresh windows. The result is the second play's when the conversion stands, since that is the
    /// file as it now is; else the first play's, the file being what it was.
    /// </summary>
    public static ScriptResult After(string script, string name, ScriptResult result, Func<ScriptResult> playAgain, Action<string> say)
    {
        if (!On)
        {
            return result;
        }

        if (script.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase))
        {
            say($"{name}: left as it is: a workflow file, not a script");
            return result;
        }

        if (!result.Ok || result.Stopped)
        {
            // WF-10.4: a script that does not pass is not converted.
            say($"{name}: left as it is: it did not pass");
            return result;
        }

        // The file's own bytes: its line ends stay with its lines (split at the line feeds alone), and a byte order
        // mark, if it has one, is written again.
        var bytes = File.ReadAllBytes(script);
        var marked = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var lines = new UTF8Encoding(false).GetString(bytes, marked ? 3 : 0, bytes.Length - (marked ? 3 : 0)).Split('\n');
        var rewritten = Converter.Rewrite(lines, result.Conversions);
        var presses = rewritten.Count(IsDo) - lines.Count(IsDo);
        if (presses <= 0)
        {
            say($"{name}: no press to convert");
            return result;
        }

        var counted = presses == 1 ? "1 press" : $"{presses} presses";
        File.WriteAllText(script, string.Join('\n', rewritten), new UTF8Encoding(marked));
        say($"{name}: {counted} written as do lines; played once more as it now stands");
        ScriptResult again;
        try
        {
            again = playAgain();
        }
        catch
        {
            // Whatever ended the second play: the file is never left rewritten and unproved.
            File.WriteAllBytes(script, bytes);
            throw;
        }

        if (!again.Ok || again.Stopped)
        {
            File.WriteAllBytes(script, bytes);
            var why = again.Failed is { } failed
                ? $"line {failed.Line.Number}, {failed.Line.Text}"
                : again.Problems.Count > 0 ? "after the steps: " + again.Problems[0] : "it was stopped";
            var line = $"{name}: put back as it was: with {counted} as do lines it did not pass ({why})";
            PutBack.Add(line);
            say(line);
            return result;
        }

        // WF-10.2: the one row of each do step of the proving play, as the window listed it at that step.
        InventoryRun.Converted(again.Commands.Where(snapshot => snapshot.Commands.Count == 1));
        _presses += presses;
        _scripts++;
        say($"{name}: {counted} converted");
        return again;
    }

    /// <summary>The run's last lines of the converter, none when the run does not convert: the presses and scripts converted, then each script that was put back.</summary>
    public static IReadOnlyList<string> End()
    {
        if (!On)
        {
            return [];
        }

        var lines = new List<string> { $"Converted: {_presses} press(es) in {_scripts} script(s) now do lines; {PutBack.Count} script(s) put back as they were." };
        lines.AddRange(PutBack.Select(line => "  " + line));
        return lines;
    }

    /// <summary>Whether a line of a script is a <c>do</c> step, after any <c>iffound</c>.</summary>
    private static bool IsDo(string line)
    {
        var text = line.Trim();
        while (text.StartsWith("iffound ", StringComparison.Ordinal))
        {
            text = text[8..].TrimStart();
        }

        return text.StartsWith("do ", StringComparison.Ordinal);
    }
}
