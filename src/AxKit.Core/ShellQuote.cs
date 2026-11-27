namespace AxKit.Core;

/// <summary>
/// One command line for a shell, made of a program and its arguments, each element quoted by that shell's own rule
/// so that it stays one element whatever it holds (docs/axTask/forms-framework.md AF-4.5). It is for a line a
/// person reads before sending it himself (axTask's terminal actions put it into axTerminal's command field): a
/// program axKit starts itself is never started through a shell, and takes its arguments as a list
/// (<see cref="SystemPrograms.LineFor"/>).
///
/// One tested function per shell, and no guess for a shell without one: PowerShell (Windows PowerShell and
/// PowerShell 7), where an element goes in single quotes with every single quote doubled, and cmd.exe, which has no
/// quoting that makes every character plain, so an element holding one cmd.exe would act on is refused, as Claude
/// Code's command line is (bundle repairs R-24).
/// </summary>
public static class ShellQuote
{
    /// <summary>
    /// The characters PowerShell reads as a single quote: the plain one and four typographic ones. Inside a
    /// single-quoted text each of them ends the text unless it is doubled.
    /// </summary>
    private static readonly char[] SingleQuotes = ['\'', '\u2018', '\u2019', '\u201A', '\u201B'];

    /// <summary>The characters cmd.exe acts on even inside double quotes, or that end them: an element with one is refused.</summary>
    private static readonly char[] CmdRefused = ['%', '!', '"', '^', '&', '|', '<', '>'];

    /// <summary>
    /// The command line for the shell whose program is <paramref name="shell"/> (its path or its name:
    /// <c>powershell.exe</c>, <c>pwsh.exe</c>, <c>cmd.exe</c>), or why there is none: a shell without a rule, no
    /// element at all, or an element the shell's rule cannot make plain. A control character, a line break included,
    /// is refused for every shell: a line break would send what stands before it.
    /// </summary>
    public static (string? Line, string? Refused) CommandLine(string shell, IReadOnlyList<string> elements)
    {
        if (elements.Count == 0 || elements[0].Trim().Length == 0)
        {
            return (null, "there is no program to name");
        }

        foreach (var element in elements)
        {
            if (element.Any(char.IsControl))
            {
                return (null, "a part of the command holds a line break or another control character");
            }
        }

        return Path.GetFileNameWithoutExtension(shell.Trim()).ToLowerInvariant() switch
        {
            "powershell" or "pwsh" => PowerShell(elements),
            "cmd" => Cmd(elements),
            var name => (null, $"axKit has no quoting rule for the shell {(name.Length == 0 ? "of this window" : name)}"),
        };
    }

    /// <summary>
    /// PowerShell: an element of plain characters stands as it is, any other in single quotes with every single
    /// quote in it doubled, where nothing is expanded or acted on. A program that needs quotes is called with the
    /// call operator, since a quoted text alone would only be written out.
    ///
    /// The single quotes are PowerShell's own reading of the line; what it then hands to the program is another
    /// step, and Windows PowerShell 5.1 (and PowerShell 7 before 7.3, or set to its older way) does that step
    /// without care (tried by a real run, 2026-11-27, <c>ShellQuoteTests</c>): a double quote in an element is
    /// lost and splits the element at its spaces; a backslash at the end of an element with a space takes the
    /// next element into this one; an empty element is not handed on at all. PowerShell 7.3 and later hand all
    /// three on whole, so no one line is right for both, and an element of one of these three kinds is refused
    /// for both, as cmd.exe's are, with the reason.
    /// </summary>
    private static (string? Line, string? Refused) PowerShell(IReadOnlyList<string> elements)
    {
        var parts = new List<string>();
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            if (element.Contains('"'))
            {
                return (null, "a part of the command holds a double quote, which PowerShell hands to a program whole in some versions and not in others");
            }

            if (element.Length == 0)
            {
                return (null, "a part of the command is empty, which Windows PowerShell does not hand to a program");
            }

            if (element.EndsWith('\\') && element.Any(char.IsWhiteSpace))
            {
                return (null, "a part of the command has a space in it and a backslash at its end, which Windows PowerShell hands to a program joined with the next part; leave the last backslash out");
            }

            // An option such as --repo or -n stands as it is. One with a full stop or a colon is quoted: PowerShell
            // splits an unquoted -a.b into two arguments.
            var bare = Plain(element) && (!element.StartsWith('-') || (i > 0 && element.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')));
            if (bare)
            {
                parts.Add(element);
                continue;
            }

            var quoted = new System.Text.StringBuilder("'");
            foreach (var c in element)
            {
                quoted.Append(c);
                if (Array.IndexOf(SingleQuotes, c) >= 0)
                {
                    quoted.Append(c);
                }
            }

            quoted.Append('\'');
            parts.Add(i == 0 ? "& " + quoted : quoted.ToString());
        }

        return (string.Join(' ', parts), null);
    }

    /// <summary>
    /// cmd.exe: an element with a character cmd.exe would act on is refused; one with a space or a separator goes in
    /// double quotes, the backslashes before the closing quote doubled so that the program reads them as written.
    /// </summary>
    private static (string? Line, string? Refused) Cmd(IReadOnlyList<string> elements)
    {
        var parts = new List<string>();
        foreach (var element in elements)
        {
            if (element.IndexOfAny(CmdRefused) is var at and >= 0)
            {
                return (null, $"a part of the command holds {element[at]}, which the Command Prompt acts on; use a PowerShell window for it");
            }

            if (element.Length > 0 && Plain(element))
            {
                parts.Add(element);
                continue;
            }

            var trailing = element.Length - element.TrimEnd('\\').Length;
            parts.Add("\"" + element + new string('\\', trailing) + "\"");
        }

        return (string.Join(' ', parts), null);
    }

    /// <summary>Whether an element is made only of characters no shell here gives a meaning: letters, digits and a few marks of paths and options.</summary>
    private static bool Plain(string element) =>
        element.Length > 0 && element.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '/' or '\\' or ':' or '-' or '+');
}
