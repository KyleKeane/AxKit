using System.Text;

namespace AxMCP.Core;

/// <summary>
/// A command line typed by the user into its program and arguments (SPEC.md MP-1.3), the way Windows programs read
/// theirs: words separated by spaces, a word in double quotes keeping its spaces, a backslash before a quote keeping
/// the quote. Nothing more: no variables, no pipes; the server's definition in Claude Code's file is already split.
/// </summary>
public static class CommandLines
{
    public static IReadOnlyList<string> Split(string commandLine)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        var inWord = false;
        var quoted = false;
        for (var i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];
            if (c == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '"')
            {
                word.Append('"');
                inWord = true;
                i++;
            }
            else if (c == '"')
            {
                quoted = !quoted;
                inWord = true;
            }
            else if (char.IsWhiteSpace(c) && !quoted)
            {
                if (inWord)
                {
                    words.Add(word.ToString());
                    word.Clear();
                    inWord = false;
                }
            }
            else
            {
                word.Append(c);
                inWord = true;
            }
        }

        if (inWord)
        {
            words.Add(word.ToString());
        }

        return words;
    }

    /// <summary>A word back onto a line: quoted when it holds a space or a quote, so that <see cref="Split"/> gives it back whole.</summary>
    public static string Quote(string word) =>
        word.Length > 0 && !word.Any(c => char.IsWhiteSpace(c) || c == '"') ? word : "\"" + word.Replace("\"", "\\\"") + "\"";
}
