using System.Text;
using AxKit.Core.Transcript;

namespace AxKit.Core.Vt;

internal sealed class Row
{
    /// <summary>Marks the second cell of a two-cell wide character.</summary>
    public const string Continuation = "\0";

    public readonly string?[] Cells;

    /// <summary>
    /// Each cell's background as one number (<see cref="Screen"/>'s <c>Background</c> codes), on the alternate
    /// screen's rows alone: null on the main screen's, so that the conversation's path pays nothing for it
    /// (docs/axTerminal/SPEC.md AT-9.4).
    /// </summary>
    public readonly int[]? Backgrounds;

    public Line? Line;
    public bool Dirty;

    public Row(int columns)
    {
        Cells = new string?[columns];
    }

    /// <summary>A row of the alternate screen, every cell of the given background.</summary>
    public Row(int columns, int background)
    {
        Cells = new string?[columns];
        Backgrounds = new int[columns];
        Array.Fill(Backgrounds, background);
    }

    public string Text()
    {
        var sb = new StringBuilder(Cells.Length);
        foreach (var cell in Cells)
        {
            if (cell is null)
            {
                sb.Append(' ');
            }
            else if (cell != Continuation)
            {
                sb.Append(cell);
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>Blanks the cells; on the alternate screen they take the background given, the one in force, as a terminal's erase does.</summary>
    public void Clear(int from, int toExclusive, int background)
    {
        for (var i = Math.Max(0, from); i < Math.Min(Cells.Length, toExclusive); i++)
        {
            Cells[i] = null;
            if (Backgrounds is not null)
            {
                Backgrounds[i] = background;
            }
        }

        Dirty = true;
    }
}

/// <summary>
/// A headless terminal screen: a grid of cells, a cursor and a scroll region, driven by <see cref="VtParser"/>.
/// Rows carry a stable line identity so that in-place rewrites update existing transcript lines.
/// </summary>
public sealed class Screen
{
    // One shared string per printable ASCII character: a repaint prints thousands of cells per frame.
    private static readonly string[] Ascii = Enumerable.Range(0, 128).Select(c => ((char)c).ToString()).ToArray();

    // A background as one number (docs/axTerminal/SPEC.md AT-9.4): 0 the default; a colour of the 256 palette its
    // index with PaletteBit; a colour of 24 bits its red, green and blue with RgbBit; reverse video adds ReverseBit.
    private const int PaletteBit = 0x100;
    private const int RgbBit = 0x1000000;
    internal const int ReverseBit = 0x2000000;

    private readonly LineStore _store;

    /// <summary>The main screen's rows, the ones with transcript lines.</summary>
    private readonly Row[] _main;

    /// <summary>The alternate screen's rows (AT-9.1), made the first time a program switches to it; they never get a line.</summary>
    private Row[]? _alternate;

    /// <summary>The rows the program writes on: the main screen's or, while <see cref="Alternate"/>, the alternate one's.</summary>
    private Row[] _rows;

    private int _cx;
    private int _cy;
    private int _savedX;
    private int _savedY;
    private int _top;
    private int _bottom;
    private bool _pendingWrap;

    // The pen's background and reverse video, from SGR (ESC[...m); written into the cells of the alternate screen only.
    private int _background;
    private bool _reverse;

    // The main screen's cursor while the alternate one is in use: what CursorRow answers, and where ?1049l returns.
    private int _mainX;
    private int _mainY;

    public Screen(int columns, int rows, LineStore store)
    {
        if (columns < 2 || rows < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), "The screen needs at least 2 columns and 2 rows.");
        }

        Columns = columns;
        Rows = rows;
        _store = store;
        _main = new Row[rows];
        for (var i = 0; i < rows; i++)
        {
            _main[i] = new Row(columns);
        }

        _rows = _main;
        _bottom = rows - 1;
    }

    public int Columns { get; }
    public int Rows { get; }

    /// <summary>The main screen's cursor row, the row of the transcript's lines; while <see cref="Alternate"/>, where it stood when the program switched.</summary>
    public int CursorRow => Alternate ? _mainY : _cy;

    /// <summary>
    /// The program writes on the alternate screen (<c>ESC[?1049h</c>, <c>?1047h</c>, <c>?47h</c>; docs/axTerminal/SPEC.md
    /// AT-9.1): a full-screen program. The main screen's rows and their lines stay as they were until it switches back;
    /// what is written meanwhile is read with <see cref="ScreenPage"/>, and never reaches the transcript (AT-9.6). The
    /// cursor homed and the screen erased are not raised while it lasts, since they speak of the main screen.
    /// </summary>
    public bool Alternate { get; private set; }

    /// <summary>
    /// Whether the alternate screen is kept apart at all (AT-9.1): on where a window shows the page (axTerminal's model
    /// turns it on for its screen view, AT-9.9), off by default (axClaude), so that a full-screen program writes on the
    /// main rows as it always did and stays in the conversation, garbled but there, rather than vanishing where nothing
    /// shows the page.
    /// </summary>
    public bool KeepsAlternateApart { get; set; }

    /// <summary>
    /// Whether a program's colour questions are answered (AT-9.10, <see cref="QueryAnswers"/>) while it holds the
    /// alternate screen: on in axTerminal's model, so that Windows' edit takes keys at once instead of waiting about
    /// four seconds; off by default (axClaude), so that what Claude Code is told stays as it was. On the main screen a
    /// question is never answered, since a shell's own line editor would take the answer as typed text.
    /// </summary>
    public bool AnswersQueries { get; set; }

    /// <summary>The answer to a program's question, to be written to its input as it is (<see cref="AnswersQueries"/>).</summary>
    public event Action<string>? Answer;

    /// <summary>
    /// The program that held the alternate screen ended without switching back (AT-9.10; a model knows it from the
    /// shell's own mark): the main screen again, as on <c>ESC[?1049l</c>, its rows and their lines as they were and
    /// the cursor where it stood, and what the program set let go: its scroll region, its background and its reverse
    /// video (until 2026-11-27 they stayed, and the shell's output scrolled inside the dead program's region).
    /// Nothing when the main screen is in use.
    /// </summary>
    public void LeaveAlternate()
    {
        if (!Alternate)
        {
            return;
        }

        SwitchScreen(1049, false);
        _top = 0;
        _bottom = Rows - 1;
        _background = 0;
        _reverse = false;
        _pendingWrap = false;
    }

    /// <summary>The program switched to the alternate screen or back since the last frame ended (<c>ESC[?25h</c>), so that the frame end knows the view changes.</summary>
    public bool AlternateChangedSinceFrameEnd { get; private set; }

    /// <summary>The alternate screen's rows while it is in use, for <see cref="ScreenPage"/>; null otherwise.</summary>
    internal Row[]? AlternateRows => Alternate ? _rows : null;

    /// <summary>The cursor where the program writes, row and column, on whichever screen is in use.</summary>
    internal (int Row, int Column) WritingCursor => (_cy, _cx);

    /// <summary>A cell's number for the pen in force (<see cref="Row.Backgrounds"/>).</summary>
    private int PenCell => _reverse ? _background | ReverseBit : _background;

    /// <summary>A new blank row for the screen in use: on the alternate screen with its backgrounds, of the pen's background.</summary>
    private Row NewRow() => Alternate ? new Row(Columns, _background) : new Row(Columns);

    /// <summary>The cursor was shown again (<c>ESC[?25h</c>): the frame is complete.</summary>
    public event Action? FrameEnd;
    public event Action? Bell;
    public event Action<string>? TitleChanged;

    /// <summary>
    /// The cursor was put in the top left corner (<c>ESC[H</c>). With the screen not cleared since the last frame and
    /// conversation on it, this is Claude repainting everything from the top (SessionModel).
    /// </summary>
    public event Action? Homed;

    /// <summary>
    /// The whole screen is about to be erased (<c>ESC[2J</c>, <c>ESC[3J</c>), raised before the rows lose their
    /// text: a model that keeps what was on screen (axTerminal, where cls starts the screen afresh) lets the rows go
    /// here (<see cref="DetachRows"/>).
    /// </summary>
    public event Action? Cleared;

    /// <summary>
    /// A shell integration mark, OSC 133 (docs/axTerminal/SPEC.md AT-1.3): the letter (A the prompt starts, B the
    /// command starts, C the output starts, D the command finished) and what follows the letter's semicolon (a D's
    /// exit code), or null. axClaude listens too: its turn marks, A when Claude takes a prompt up, C and D when the turn
    /// ends (SessionModel.Marked; docs/axClaude/events-and-grounding.md EG-1.1).
    /// </summary>
    public event Action<char, string?>? Mark;

    /// <summary>OSC 9;9: the shell's current directory, as Windows Terminal's shell integration reports it, without quotes.</summary>
    public event Action<string>? DirectoryChanged;

    /// <summary>The whole screen was erased (<c>ESC[2J</c>, <c>ESC[3J</c>) since the last frame ended.</summary>
    public bool ClearedSinceFrameEnd { get; private set; }

    /// <summary>The line of a row of the main screen (the alternate one's rows have none).</summary>
    public Line? LineAt(int row) => _main[row].Line;

    /// <summary>The row the line is on now, or -1 when it is not on the screen (scrolled off, let go, or never a row's): a model that keeps a line from an earlier frame finds its row again by identity, since the rows move as the screen scrolls (docs/axTerminal/SPEC.md AT-4.2).</summary>
    public int RowOf(Line line)
    {
        for (var r = 0; r < Rows; r++)
        {
            if (ReferenceEquals(_main[r].Line, line))
            {
                return r;
            }
        }

        return -1;
    }

    /// <summary>The row's line, made first (with lines for the blank rows above it) when it has none yet: for a model that marks a row before anything is printed on it, the prompt's A mark (AT-4.2).</summary>
    public Line EnsureLine(int row)
    {
        EnsureLines(row);
        return _main[row].Line!;
    }

    /// <summary>
    /// At the end of a frame: the text of every row written since the last pull goes to its line through the store
    /// (<see cref="LineStore.TextChanged"/>), where the model decides what a rewritten row means.
    /// </summary>
    public void PullRowText()
    {
        foreach (var row in _main)
        {
            if (row.Line is { } line && row.Dirty)
            {
                var text = row.Text();
                row.Dirty = false;
                if (text != line.Text)
                {
                    _store.TextChanged(line, text);
                }
            }
        }
    }

    // ---- what the parser calls ----

    internal void Print(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            var width = Wcwidth.Of(rune.Value);
            var s = rune.Value < 128 ? Ascii[rune.Value] : rune.ToString();
            if (width == 0)
            {
                AttachCombining(s);
                continue;
            }

            if (_pendingWrap)
            {
                _pendingWrap = false;
                _cx = 0;
                LineFeed();
            }

            if (width == 2 && _cx == Columns - 1)
            {
                ClearCell(_rows[_cy], _cx);
                _rows[_cy].Dirty = true;
                _cx = 0;
                LineFeed();
            }

            if (!Alternate)
            {
                EnsureLines(_cy);
            }

            var row = _rows[_cy];
            ClearCell(row, _cx);
            row.Cells[_cx] = s;
            if (width == 2)
            {
                ClearCell(row, _cx + 1);
                row.Cells[_cx + 1] = Row.Continuation;
            }

            if (row.Backgrounds is { } backgrounds)
            {
                backgrounds[_cx] = PenCell;
                if (width == 2)
                {
                    backgrounds[_cx + 1] = PenCell;
                }
            }

            row.Dirty = true;
            _cx += width;
            if (_cx >= Columns)
            {
                _cx = Columns - 1;
                _pendingWrap = true;
            }
        }
    }

    internal void Control(char c)
    {
        switch (c)
        {
            case '\a':
                Bell?.Invoke();
                break;
            case '\b':
                if (_cx > 0)
                {
                    _cx--;
                }

                _pendingWrap = false;
                break;
            case '\t':
                _cx = Math.Min(Columns - 1, (_cx / 8 + 1) * 8);
                _pendingWrap = false;
                break;
            case '\n':
            case '\v':
            case '\f':
                LineFeed();
                break;
            case '\r':
                _cx = 0;
                _pendingWrap = false;
                break;
        }
    }

    internal void Escape(char final, string intermediates)
    {
        if (intermediates.Length > 0)
        {
            return; // charset designations and similar
        }

        switch (final)
        {
            case '7':
                SaveCursor();
                break;
            case '8':
                RestoreCursor();
                break;
            case 'D':
                LineFeed();
                break;
            case 'E':
                _cx = 0;
                LineFeed();
                break;
            case 'M':
                ReverseIndex();
                break;
            case 'c':
                Reset();
                break;
        }
    }

    internal void Csi(string parameters, string intermediates, char final)
    {
        if (intermediates.Length > 0)
        {
            if (final == 'p' && intermediates == "!")
            {
                Reset();
            }

            return;
        }

        var isPrivate = parameters.Length > 0 && parameters[0] is '?' or '>' or '<' or '=';
        var values = ParseParameters(isPrivate ? parameters.AsSpan(1) : parameters.AsSpan());
        if (isPrivate)
        {
            // Two private modes matter: the alternate screen (AT-9.1), and ESC[?25h, which ends a frame. Every other
            // private mode is ignored.
            if (parameters[0] == '?' && final is 'h' or 'l')
            {
                foreach (var mode in values)
                {
                    if (mode is 1049 or 1047 or 47 && KeepsAlternateApart)
                    {
                        SwitchScreen(mode, final == 'h');
                    }
                }
            }

            if (parameters[0] == '?' && final == 'h' && values.Contains(25))
            {
                FrameEnd?.Invoke();
                ClearedSinceFrameEnd = false;
                AlternateChangedSinceFrameEnd = false;
            }

            return;
        }

        int P(int index, int fallback) => index < values.Count && values[index] > 0 ? values[index] : fallback;
        var first = values[0]; // ParseParameters always yields at least one value; 0 is the default of J and K.

        switch (final)
        {
            case 'm':
                SelectGraphicRendition(parameters);
                break;
            case 'A':
                MoveCursor(_cx, _cy - P(0, 1));
                break;
            case 'B':
                MoveCursor(_cx, _cy + P(0, 1));
                break;
            case 'C':
                MoveCursor(_cx + P(0, 1), _cy);
                break;
            case 'D':
                MoveCursor(_cx - P(0, 1), _cy);
                break;
            case 'E':
                MoveCursor(0, _cy + P(0, 1));
                break;
            case 'F':
                MoveCursor(0, _cy - P(0, 1));
                break;
            case 'G':
            case '`':
                MoveCursor(P(0, 1) - 1, _cy);
                break;
            case 'H':
            case 'f':
                MoveCursor(P(1, 1) - 1, P(0, 1) - 1);
                if (_cx == 0 && _cy == 0 && !Alternate)
                {
                    Homed?.Invoke();
                }

                break;
            case 'd':
                MoveCursor(_cx, P(0, 1) - 1);
                break;
            case 'J':
                EraseDisplay(first);
                break;
            case 'K':
                EraseLine(first);
                break;
            case 'X':
                _rows[_cy].Clear(_cx, _cx + P(0, 1), _background);
                break;
            case '@':
                InsertCells(P(0, 1));
                break;
            case 'P':
                DeleteCells(P(0, 1));
                break;
            case 'L':
                InsertLines(P(0, 1));
                break;
            case 'M':
                DeleteLines(P(0, 1));
                break;
            // Clamped to the scroll region: a count of 65535 would otherwise make and move rows 65535 times (S-41).
            case 'S':
                ScrollUp(Math.Min(P(0, 1), _bottom - _top + 1));
                break;
            case 'T':
                ScrollDown(Math.Min(P(0, 1), _bottom - _top + 1));
                break;
            case 'r':
                SetScrollRegion(P(0, 1), P(1, Rows));
                break;
            case 's':
                SaveCursor();
                break;
            case 'u':
                RestoreCursor();
                break;
        }
    }

    internal void Osc(string content)
    {
        var separator = content.IndexOf(';');
        var code = separator < 0 ? content : content[..separator];
        var payload = separator < 0 ? string.Empty : content[(separator + 1)..];
        if (code is "0" or "2")
        {
            TitleChanged?.Invoke(payload);
        }
        else if (code == "133" && payload.Length > 0)
        {
            // A shell integration mark (docs/axTerminal/SPEC.md AT-1.3): the letter, then what follows its semicolon.
            Mark?.Invoke(payload[0], payload.Length > 2 && payload[1] == ';' ? payload[2..] : null);
        }
        else if (code == "9" && payload.StartsWith("9;", StringComparison.Ordinal))
        {
            // The shell's current directory (Windows Terminal's convention); PowerShell's snippet quotes it.
            DirectoryChanged?.Invoke(payload[2..].Trim().Trim('"'));
        }
        else if (AnswersQueries && Alternate)
        {
            // AT-9.10: a full-screen program's colour questions (OSC 4, 10, 11), answered as a terminal does.
            foreach (var answer in QueryAnswers.To(content))
            {
                Answer?.Invoke(answer);
            }
        }
    }

    // ---- screen operations ----

    private static List<int> ParseParameters(ReadOnlySpan<char> text)
    {
        var values = new List<int>(4);
        var current = 0;
        var any = false;
        foreach (var c in text)
        {
            if (c >= '0' && c <= '9')
            {
                current = Math.Min(65535, current * 10 + (c - '0'));
                any = true;
            }
            else if (c == ';')
            {
                values.Add(any ? current : 0);
                current = 0;
                any = false;
            }
            // ':' and other characters are ignored
        }

        values.Add(any ? current : 0);
        return values;
    }

    private void MoveCursor(int x, int y)
    {
        _cx = Math.Clamp(x, 0, Columns - 1);
        _cy = Math.Clamp(y, 0, Rows - 1);
        _pendingWrap = false;
    }

    private void SaveCursor()
    {
        _savedX = _cx;
        _savedY = _cy;
    }

    private void RestoreCursor() => MoveCursor(_savedX, _savedY);

    private void LineFeed()
    {
        _pendingWrap = false;
        if (_cy == _bottom)
        {
            ScrollUp(1);
        }
        else if (_cy < Rows - 1)
        {
            _cy++;
        }
    }

    private void ReverseIndex()
    {
        _pendingWrap = false;
        if (_cy == _top)
        {
            ScrollDown(1);
        }
        else if (_cy > 0)
        {
            _cy--;
        }
    }

    /// <summary><c>ESC c</c> and <c>ESC[!p</c>: the screen as at the start. A model that keeps what was on screen lets the rows go first, as for an erase (<see cref="Cleared"/>; until 2026-10-23 a reset cleared the rows without the event).</summary>
    private void Reset()
    {
        // The model hears of the erase first, and as an erase of its own: a model that keeps the rows on their lines
        // after a switch of screen in the same frame (the console's repaint, AT-9.9) must still let them go here,
        // or the clearing below blanks the lines already read. Until 2026-11-27 the event came after the switch.
        var switched = AlternateChangedSinceFrameEnd;
        AlternateChangedSinceFrameEnd = false;
        Cleared?.Invoke();
        AlternateChangedSinceFrameEnd = switched;

        // A reset leaves the alternate screen, as xterm's does, and drops the pen.
        if (Alternate)
        {
            SwitchScreen(47, false);
        }

        _background = 0;
        _reverse = false;
        foreach (var row in _rows)
        {
            row.Clear(0, Columns, 0);
        }

        _top = 0;
        _bottom = Rows - 1;
        MoveCursor(0, 0);
        ClearedSinceFrameEnd = true;
    }

    /// <summary>
    /// The alternate screen on or off (AT-9.1), as xterm does it: 1049 saves the main screen's cursor and clears the
    /// alternate screen on the way in and puts the cursor back on the way out; 1047 clears the alternate screen on the
    /// way out; 47 only switches. A switch to the screen already in use does nothing.
    /// </summary>
    private void SwitchScreen(int mode, bool on)
    {
        if (on == Alternate)
        {
            return;
        }

        if (on)
        {
            _mainX = _cx;
            _mainY = _cy;
            if (_alternate is null)
            {
                _alternate = new Row[Rows];
                for (var i = 0; i < Rows; i++)
                {
                    _alternate[i] = new Row(Columns, 0);
                }
            }
            else if (mode == 1049)
            {
                foreach (var row in _alternate)
                {
                    row.Clear(0, Columns, _background);
                }
            }

            _rows = _alternate;
            Alternate = true;
        }
        else
        {
            if (mode == 1047)
            {
                foreach (var row in _rows)
                {
                    row.Clear(0, Columns, _background);
                }
            }

            _rows = _main;
            Alternate = false;
            if (mode == 1049)
            {
                MoveCursor(_mainX, _mainY);
            }
        }

        AlternateChangedSinceFrameEnd = true;
    }

    /// <summary>
    /// SGR (<c>ESC[...m</c>): only what makes a cell's background (AT-9.4), the background colour and reverse video;
    /// every other attribute and the foreground are read past. A group with colons is one attribute with its
    /// sub-parameters (<c>48:2::r:g:b</c>, <c>4:3</c>); <c>38</c> and <c>48</c> with semicolons take the groups after
    /// them. Nothing is allocated: the parameters are read in place.
    /// </summary>
    private void SelectGraphicRendition(string parameters)
    {
        Span<int> group = stackalloc int[6];
        var at = 0;
        while (at <= parameters.Length)
        {
            var count = NextGroup(parameters, ref at, group);
            var code = group[0];
            if (code is 38 or 48)
            {
                int colour;
                if (count > 1)
                {
                    colour = ExtendedColour(group[1..count]);
                }
                else
                {
                    var kind = NextValue(parameters, ref at);
                    colour = kind switch
                    {
                        5 => PaletteBit | Math.Min(255, NextValue(parameters, ref at)),
                        2 => Rgb(NextValue(parameters, ref at), NextValue(parameters, ref at), NextValue(parameters, ref at)),
                        _ => 0,
                    };
                }

                if (code == 48)
                {
                    _background = colour;
                }

                continue;
            }

            if (count > 1)
            {
                continue; // a sub-parameter form of another attribute, such as 4:3, the curly underline
            }

            switch (code)
            {
                case 0:
                    _background = 0;
                    _reverse = false;
                    break;
                case 7:
                    _reverse = true;
                    break;
                case 27:
                    _reverse = false;
                    break;
                case 49:
                    _background = 0;
                    break;
                case >= 40 and <= 47:
                    _background = PaletteBit | (code - 40);
                    break;
                case >= 100 and <= 107:
                    _background = PaletteBit | (code - 100 + 8);
                    break;
            }
        }
    }

    /// <summary>The colour of <c>38:...</c> or <c>48:...</c>: <c>5:n</c>, <c>2:r:g:b</c> or <c>2:space:r:g:b</c>.</summary>
    private static int ExtendedColour(ReadOnlySpan<int> rest) => rest switch
    {
        [5, var index, ..] => PaletteBit | Math.Min(255, index),
        [2, .., var r, var g, var b] => Rgb(r, g, b),
        _ => 0,
    };

    private static int Rgb(int r, int g, int b) => RgbBit | (Math.Min(255, r) << 16) | (Math.Min(255, g) << 8) | Math.Min(255, b);

    /// <summary>One group of SGR's parameters from <paramref name="at"/> to the next semicolon, its colon-separated values into <paramref name="values"/> (an empty one is 0); the count of them. Past the end, <paramref name="at"/> is beyond the text.</summary>
    private static int NextGroup(string text, ref int at, Span<int> values)
    {
        var count = 0;
        var current = 0;
        while (at < text.Length && text[at] != ';')
        {
            var c = text[at++];
            if (c == ':')
            {
                if (count < values.Length)
                {
                    values[count++] = current;
                }

                current = 0;
            }
            else if (c >= '0' && c <= '9')
            {
                current = Math.Min(65535, current * 10 + (c - '0'));
            }
        }

        if (count < values.Length)
        {
            values[count++] = current;
        }

        at++; // past the semicolon, or past the end
        return count;
    }

    /// <summary>The first value of the next group, or 0 past the end.</summary>
    private static int NextValue(string text, ref int at)
    {
        if (at > text.Length)
        {
            return 0;
        }

        Span<int> one = stackalloc int[1];
        NextGroup(text, ref at, one);
        return one[0];
    }

    private void SetScrollRegion(int top, int bottom)
    {
        top = Math.Clamp(top, 1, Rows);
        bottom = Math.Clamp(bottom, 1, Rows);
        if (top < bottom)
        {
            _top = top - 1;
            _bottom = bottom - 1;
        }

        MoveCursor(0, 0);
    }

    private void EraseDisplay(int mode)
    {
        switch (mode)
        {
            case 0:
                _rows[_cy].Clear(_cx, Columns, _background);
                for (var r = _cy + 1; r < Rows; r++)
                {
                    _rows[r].Clear(0, Columns, _background);
                }

                break;
            case 1:
                for (var r = 0; r < _cy; r++)
                {
                    _rows[r].Clear(0, Columns, _background);
                }

                _rows[_cy].Clear(0, _cx + 1, _background);
                break;
            default:
                if (Alternate)
                {
                    // The alternate screen erased: nothing of the main screen's, so nothing to raise (AT-9.1).
                    foreach (var row in _rows)
                    {
                        row.Clear(0, Columns, _background);
                    }

                    break;
                }

                Cleared?.Invoke();
                foreach (var row in _rows)
                {
                    row.Clear(0, Columns, _background);
                }

                ClearedSinceFrameEnd = true;
                break;
        }
    }

    /// <summary>
    /// Lets go of every row's line, as if the rows had scrolled away: the lines stay in the transcript with the text
    /// they had at the last frame end, not what was written on their rows since, and what is printed on the rows
    /// next gets lines of its own. The rows start blank: a row the program does not rewrite would otherwise come
    /// back as a new line with the old text, and a shorter rewrite would read the old tail after the new text
    /// (2026-10-23).
    /// </summary>
    public void DetachRows()
    {
        foreach (var row in _main)
        {
            if (row.Line is { } line)
            {
                row.Dirty = false;
                _store.Commit(line);
                row.Line = null;
            }

            row.Clear(0, Columns, 0);
            row.Dirty = false;
        }
    }

    private void EraseLine(int mode)
    {
        var row = _rows[_cy];
        switch (mode)
        {
            case 0:
                row.Clear(_cx, Columns, _background);
                break;
            case 1:
                row.Clear(0, _cx + 1, _background);
                break;
            default:
                row.Clear(0, Columns, _background);
                break;
        }
    }

    private void InsertCells(int count)
    {
        var row = _rows[_cy];
        count = Math.Min(count, Columns - _cx);
        for (var x = Columns - 1; x >= _cx + count; x--)
        {
            row.Cells[x] = row.Cells[x - count];
            if (row.Backgrounds is { } backgrounds)
            {
                backgrounds[x] = backgrounds[x - count];
            }
        }

        row.Clear(_cx, _cx + count, _background);
    }

    private void DeleteCells(int count)
    {
        var row = _rows[_cy];
        count = Math.Min(count, Columns - _cx);
        for (var x = _cx; x < Columns - count; x++)
        {
            row.Cells[x] = row.Cells[x + count];
            if (row.Backgrounds is { } backgrounds)
            {
                backgrounds[x] = backgrounds[x + count];
            }
        }

        row.Clear(Columns - count, Columns, _background);
    }

    private void InsertLines(int count)
    {
        if (_cy < _top || _cy > _bottom)
        {
            return;
        }

        count = Math.Min(count, _bottom - _cy + 1);
        for (var k = 0; k < count; k++)
        {
            DiscardRow(_bottom);
            for (var r = _bottom; r > _cy; r--)
            {
                _rows[r] = _rows[r - 1];
            }

            _rows[_cy] = NewRow();
        }

        _cx = 0;
        _pendingWrap = false;
    }

    private void DeleteLines(int count)
    {
        if (_cy < _top || _cy > _bottom)
        {
            return;
        }

        count = Math.Min(count, _bottom - _cy + 1);
        for (var k = 0; k < count; k++)
        {
            DiscardRow(_cy);
            for (var r = _cy; r < _bottom; r++)
            {
                _rows[r] = _rows[r + 1];
            }

            _rows[_bottom] = NewRow();
        }

        _cx = 0;
        _pendingWrap = false;
    }

    private void ScrollUp(int count)
    {
        for (var k = 0; k < count; k++)
        {
            var top = _rows[_top];
            if (top.Line is { } line)
            {
                if (_top == 0)
                {
                    // The row may have been written and scrolled away inside one frame: its text goes to the line through
                    // the store, as at a frame's end (PullRowText), so that the model learns of a rewritten row here
                    // too. Set directly until 2026-11-13, a line rewritten and scrolled in one frame kept the hidden
                    // flags and the kind of its old text (docs/axKit/repairs.json R-21).
                    if (top.Dirty)
                    {
                        top.Dirty = false;
                        var text = top.Text();
                        if (text != line.Text)
                        {
                            _store.TextChanged(line, text);
                        }
                    }

                    _store.Commit(line);
                }
                else
                {
                    _store.Remove(line);
                }

                line.Row = null;
                top.Line = null;
            }

            for (var r = _top; r < _bottom; r++)
            {
                _rows[r] = _rows[r + 1];
            }

            _rows[_bottom] = NewRow();
        }
    }

    private void ScrollDown(int count)
    {
        for (var k = 0; k < count; k++)
        {
            DiscardRow(_bottom);
            for (var r = _bottom; r > _top; r--)
            {
                _rows[r] = _rows[r - 1];
            }

            _rows[_top] = NewRow();
        }
    }

    private void DiscardRow(int index)
    {
        var row = _rows[index];
        if (row.Line is { } line)
        {
            _store.Remove(line);
            row.Line = null;
        }
    }

    /// <summary>Gives the main screen's row, and every row above it, a transcript line so interior blank rows keep their place.</summary>
    private void EnsureLines(int index)
    {
        if (_main[index].Line is not null)
        {
            return;
        }

        for (var i = 0; i <= index; i++)
        {
            if (_main[i].Line is not null)
            {
                continue;
            }

            Line? before = null;
            for (var j = i + 1; j < Rows; j++)
            {
                if (_main[j].Line is { } below)
                {
                    before = below;
                    break;
                }
            }

            var line = _store.CreateLine(before);
            line.Row = _main[i];
            _main[i].Line = line;
            _main[i].Dirty = true;
        }
    }

    private void AttachCombining(string s)
    {
        var x = _pendingWrap ? _cx : _cx - 1;
        if (x < 0)
        {
            return;
        }

        var row = _rows[_cy];
        if (row.Cells[x] == Row.Continuation && x > 0)
        {
            x--;
        }

        // A cell takes a few combining characters and no more: a run of them without end grew one cell with every
        // character, a quadratic cost a hostile file could stall the window with (docs/axKit/repairs.json R-27).
        var cell = row.Cells[x] ?? " ";
        if (cell.Length >= MaxCellLength)
        {
            return;
        }

        row.Cells[x] = cell + s;
        row.Dirty = true;
    }

    /// <summary>The most characters one cell holds: its base character and the combining ones on it.</summary>
    private const int MaxCellLength = 16;

    private void ClearCell(Row row, int x)
    {
        if (x < 0 || x >= Columns)
        {
            return;
        }

        if (row.Cells[x] == Row.Continuation && x > 0)
        {
            row.Cells[x - 1] = null;
        }

        if (x + 1 < Columns && row.Cells[x + 1] == Row.Continuation)
        {
            row.Cells[x + 1] = null;
        }

        row.Cells[x] = null;
    }
}
