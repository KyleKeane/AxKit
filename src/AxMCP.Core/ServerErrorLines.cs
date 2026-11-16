namespace AxMCP.Core;

/// <summary>
/// The lines a running server wrote to its standard error (SPEC.md MP-10; bundle repairs.md R-74): the last
/// <see cref="Kept"/> are kept in memory for the window, each cut at <see cref="MaxLineLength"/> characters, and gone
/// when the window closes. The log gets <see cref="Logged"/>, the server's name with the line's number and length,
/// never the text: a server writes what it likes there, a path, an address or a token among it, and the fuller form
/// of a report carries the log. Written by the thread that reads the server's standard error and read by the
/// window, so every access is under a lock.
/// </summary>
public sealed class ServerErrorLines
{
    public const int Kept = 50;
    public const int MaxLineLength = 2000;

    private readonly List<string> _lines = [];
    private int _count;

    /// <summary>How many lines the server has written since it started, the ones no longer kept included.</summary>
    public int Count
    {
        get
        {
            lock (_lines)
            {
                return _count;
            }
        }
    }

    /// <summary>Keeps the line and returns what the log holds of it.</summary>
    public string Add(string server, string line)
    {
        lock (_lines)
        {
            _count++;
            _lines.Add(line.Length > MaxLineLength ? line[..MaxLineLength] + "..." : line);
            if (_lines.Count > Kept)
            {
                _lines.RemoveAt(0);
            }

            return Logged(server, _count, line.Length);
        }
    }

    /// <summary>The newest lines kept, at most <paramref name="most"/>, the oldest of them first.</summary>
    public IReadOnlyList<string> Last(int most)
    {
        lock (_lines)
        {
            return _lines.Skip(Math.Max(0, _lines.Count - most)).ToList();
        }
    }

    /// <summary>"fake: error line 3, 41 characters": what the log holds of a line.</summary>
    public static string Logged(string server, int number, int length) => $"{server}: error line {number}, {length} characters";

    /// <summary>The detail pane's first line about them: "Error lines: none", "Error lines: 1, shown below", "Error lines: 80, the last 10 shown below".</summary>
    public static string Summary(int count, int shown) =>
        count == 0 ? "Error lines: none" : count <= shown ? $"Error lines: {count}, shown below" : $"Error lines: {count}, the last {shown} shown below";
}
