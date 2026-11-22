using AxKit.Core.Transcript;

namespace AxCalc.Core;

/// <summary>
/// axCalc's lines for the bundle's conversation window (SPEC.md AC-2.1; docs/axKit/framework.md step 9,
/// 2026-11-22): the history as a text, one line an answer, "2 + 3 = 5", in the order they were given, with the
/// app's own system lines and the reader's bookmarks among them. A line store without a console: nothing is fed
/// to it and it has no screen; the window adds an answer's line when it gives the answer. Which entry a line is
/// stays the window's, by the line's words; this class holds lines alone.
/// </summary>
public sealed class CalcTranscript : LineStore
{
    /// <summary>The answers' lines, in order; a system line and a bookmark are not among them.</summary>
    public IReadOnlyList<string> Answers => [.. _lines.Where(line => line.Kind == LineKind.Plain).Select(line => line.Text)];

    /// <summary>An answer's line at the end of the text.</summary>
    public void AddAnswer(string label)
    {
        _lines.Add(Answer(label));
        RaiseChanged();
    }

    /// <summary>The answers kept from the last run, at a start: their lines in order, shown at once.</summary>
    public void Load(IEnumerable<string> labels)
    {
        _lines.AddRange(labels.Select(Answer));
        RaiseChanged();
    }

    /// <summary>The oldest answer's line taken away, when the history is longer than it may be; a bookmark in front of it goes with it.</summary>
    public void RemoveOldestAnswer()
    {
        var index = _lines.FindIndex(line => line.Kind == LineKind.Plain);
        if (index < 0)
        {
            return;
        }

        _lines.RemoveAt(index);
        if (index > 0 && _lines[index - 1].Kind == LineKind.Bookmark)
        {
            _lines.RemoveAt(index - 1);
        }

        RaiseChanged();
    }

    /// <summary>Clear history: every answer's line and every bookmark gone, the bookmarks counting from 1 again; the app's system lines stay, since they are about the window and not about an answer.</summary>
    public void ClearAnswers()
    {
        _lines.RemoveAll(line => line.Kind is LineKind.Plain or LineKind.Bookmark);
        ResetBookmarks();
        RaiseChanged();
    }

    /// <summary>A line of the app's own, "System: …", at the end of the text.</summary>
    public override void AddSystemLine(string text)
    {
        _lines.Add(NewLine(LineKind.System, "System: " + text));
        RaiseChanged();
    }

    /// <summary>No console draws here, so there is no frame to complete: the lines are as they were added.</summary>
    protected override void CompleteFrame() => RaiseChanged();

    private Line Answer(string label)
    {
        var line = NewLine(LineKind.Plain, label);
        line.Committed = true;
        return line;
    }
}
