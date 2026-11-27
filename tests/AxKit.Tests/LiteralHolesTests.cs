using System.Text.RegularExpressions;
using AxKit.TestShared;

namespace AxKit.Tests;

/// <summary>
/// A key's name in a text is made from its table (<c>{BundleKeys.Text(AxTaskKeys.FindEverywhere)}</c>,
/// <see cref="HandKeyTests"/>), which only happens in a string that is interpolated. One that lost its dollar sign
/// reads its own source code aloud: axTask's detail of a project folder said "BundleKeys dot Text, AxTaskKeys dot
/// FindEverywhere, finds words in every list" until 2026-11-27, and the compiler has no word against it. This test
/// reads every string literal of <c>src</c> that is not interpolated and fails on one that holds such a hole.
/// </summary>
public sealed partial class LiteralHolesTests
{
    [GeneratedRegex(@"\{\s*(BundleKeys|Ax\w*Keys|AppLaunchKeys|SelectionKeys|ItemKeys)\.")]
    private static partial Regex Hole();

    [Fact]
    public void No_string_that_is_not_interpolated_holds_a_key_tables_hole()
    {
        var root = SourceTexts.RepositoryRoot();
        var found = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var (at, literal) in PlainLiterals(text))
            {
                if (Hole().IsMatch(literal))
                {
                    found.Add($"{relative}:{text.AsSpan(0, at).Count('\n') + 1}: {Hole().Match(literal).Value}");
                }
            }
        }

        Assert.True(found.Count == 0, "A string without its dollar sign holds a key's hole, and would be shown or spoken as written:\n" + string.Join("\n", found));
    }

    [Fact]
    public void The_reading_tells_a_plain_string_from_an_interpolated_one()
    {
        const string source = """"
            // a comment with "{BundleKeys.A}" in it
            var a = "plain {BundleKeys.A}";
            var b = $"holed {BundleKeys.B} and {Call("inner {BundleKeys.C}")}";
            var c = @"verbatim ""quoted"" {BundleKeys.D}";
            var d = $@"both {BundleKeys.E}";
            var e = '"';
            var f = """
                raw {BundleKeys.F}
                """;
            var g = $$"""
                raw holed {{BundleKeys.G}} and { literal }
                """;
            /* a block "{BundleKeys.H}" */
            var h = "after \" an escape {BundleKeys.I}";
            """";

        var plain = PlainLiterals(source).Select(found => found.Literal).Where(literal => Hole().IsMatch(literal))
            .Select(literal => literal.Substring(literal.IndexOf("Keys.", StringComparison.Ordinal) + 5, 1)).ToList();
        Assert.Equal(["A", "C", "D", "F", "I"], plain);
    }

    /// <summary>
    /// Every string literal of a C# text that is not interpolated, with where it starts: regular, verbatim and raw
    /// ones, those inside the holes of an interpolated string included. Comments and character literals are read
    /// past; an interpolated string's own text is not given.
    /// </summary>
    private static List<(int At, string Literal)> PlainLiterals(string text)
    {
        var found = new List<(int, string)>();
        var i = 0;
        Code(text, ref i, found, closing: 0);
        return found;
    }

    /// <summary>Code read from <paramref name="i"/>: to the end of the text, or, inside a hole, to the braces that close it (<paramref name="closing"/> of them, at depth 0).</summary>
    private static void Code(string text, ref int i, List<(int, string)> found, int closing)
    {
        var depth = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                var end = text.IndexOf('\n', i);
                i = end < 0 ? text.Length : end;
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? text.Length : end + 2;
            }
            else if (c == '\'')
            {
                // A character: '\x' forms run to the next apostrophe, 'x' is three characters; anything else is read on.
                if (i + 1 < text.Length && text[i + 1] == '\\')
                {
                    var end = text.IndexOf('\'', i + 3);
                    i = end < 0 ? text.Length : end + 1;
                }
                else
                {
                    i += i + 2 < text.Length && text[i + 2] == '\'' ? 3 : 1;
                }
            }
            else if (c is '"' or '$' or '@' && StringStart(text, i, out var dollars, out var verbatim, out var quotes, out var open))
            {
                var start = open;
                i = open;
                var literal = Literal(text, ref i, found, dollars, verbatim, quotes);
                if (dollars == 0)
                {
                    found.Add((start, literal));
                }
            }
            else if (closing > 0 && c == '{')
            {
                depth++;
                i++;
            }
            else if (closing > 0 && c == '}')
            {
                if (depth == 0)
                {
                    i += closing;
                    return;
                }

                depth--;
                i++;
            }
            else
            {
                i++;
            }
        }
    }

    /// <summary>Whether a string starts at <paramref name="i"/>: its dollar signs, whether it is verbatim, how many quotes open it, and where its text starts.</summary>
    private static bool StringStart(string text, int i, out int dollars, out bool verbatim, out int quotes, out int open)
    {
        dollars = 0;
        verbatim = false;
        quotes = 0;
        var at = i;
        while (at < text.Length && text[at] is '$' or '@')
        {
            dollars += text[at] == '$' ? 1 : 0;
            verbatim |= text[at] == '@';
            at++;
        }

        while (at < text.Length && text[at] == '"')
        {
            quotes++;
            at++;
        }

        // A verbatim string opens with one quote, whatever follows it (@"""x" starts with a quote written twice);
        // two quotes are an empty string, not a raw one; three or more open a raw string.
        if (quotes > 1 && (verbatim || quotes == 2))
        {
            at -= quotes - 1;
            quotes = 1;
        }

        open = at;
        return quotes > 0;
    }

    /// <summary>The text of the literal whose first character is at <paramref name="i"/>; on return <paramref name="i"/> is after its closing quotes. The holes of an interpolated one are read as code.</summary>
    private static string Literal(string text, ref int i, List<(int, string)> found, int dollars, bool verbatim, int quotes)
    {
        var start = i;
        var raw = quotes >= 3;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '"')
            {
                if (raw)
                {
                    var run = 0;
                    while (i + run < text.Length && text[i + run] == '"')
                    {
                        run++;
                    }

                    if (run >= quotes)
                    {
                        var end = i + run - quotes;
                        i += run;
                        return text[start..end];
                    }

                    i += run;
                    continue;
                }

                if (verbatim && i + 1 < text.Length && text[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }

                i++;
                return text[start..(i - 1)];
            }

            if (c == '\\' && !verbatim && !raw)
            {
                i += 2;
                continue;
            }

            if (c == '{' && dollars > 0)
            {
                var run = 0;
                while (i + run < text.Length && text[i + run] == '{')
                {
                    run++;
                }

                if (raw ? run >= dollars : run % 2 == 1)
                {
                    // A hole: in a raw string the last braces of the run open it, in the others the odd one.
                    i += run;
                    Code(text, ref i, found, closing: raw ? dollars : 1);
                    continue;
                }

                i += run;
                continue;
            }

            i++;
        }

        return text[start..];
    }
}
