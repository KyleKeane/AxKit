namespace AxClaude.Core.Streaming;

/// <summary>
/// What a tool printed, as a conversation shows it under the tool's call (docs/axClaude/stream-mode.md SM-4.11): the
/// first lines, and how many more there are, as Claude Code's screen shows a call's preview rows. Measured with Claude
/// Code 2.1.332 on 2026-11-26: a command that printed twelve lines shows three, then "… +9 lines (ctrl+o to
/// expand)"; here the count stays and the hint goes, since the window does not send Ctrl+O. The stream gives the whole
/// output (the fixture tool-output), and it is kept whole (<see cref="Text"/>), so that a key that reads all of it can be
/// made on it; none is yet. Pure: no word of any program's, and no window.
/// </summary>
public sealed class ToolOutput
{
    /// <summary>How many lines are shown under the call: the screen's preview rows (measured, 2.1.332).</summary>
    public const int ShownLines = 3;

    /// <summary>The whole output as it came, its line ends made <c>\n</c> and the line ends at its end left off.</summary>
    public string Text { get; }

    /// <summary>Every line of the output, in order.</summary>
    public IReadOnlyList<string> Lines { get; }

    public ToolOutput(string text)
    {
        Text = text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
        Lines = Text.Length == 0 ? [] : Text.Split('\n');
    }

    /// <summary>How many lines are not shown under the call.</summary>
    public int Hidden => Math.Max(0, Lines.Count - ShownLines);

    /// <summary>
    /// The lines a conversation shows under the call: the first <see cref="ShownLines"/>, each without the spaces at its
    /// end, and when there are more, a line that says how many ("… +9 lines"); none for an output that is empty.
    /// </summary>
    public IReadOnlyList<string> Shown
    {
        get
        {
            var shown = Lines.Take(ShownLines).Select(line => line.TrimEnd()).ToList();
            if (Hidden > 0)
            {
                shown.Add(MoreLines(Hidden));
            }

            return shown;
        }
    }

    /// <summary>The line that says how many lines are not shown, in the form of the screen's preview ("… +9 lines").</summary>
    public static string MoreLines(int count) => $"… +{count} {(count == 1 ? "line" : "lines")}";
}
