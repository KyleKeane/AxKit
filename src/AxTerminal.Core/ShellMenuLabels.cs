namespace AxTerminal.Core;

/// <summary>
/// The labels of the user's own shells in Session, Switch shell (SPEC.md AT-1.6a, bundle AX-6): "1. name (command
/// line)", numbered from 1, the number the mnemonic only while it is one digit that no fixed item of the submenu has
/// (PowerShell &amp;7 keeps 7), and no mnemonic otherwise, so that no two items of the submenu share one.
/// </summary>
public static class ShellMenuLabels
{
    /// <summary>The Switch shell item of the user's shell at this index; <paramref name="taken"/> holds the fixed items' mnemonics, lower case.</summary>
    public static string Switch(int index, UserShell shell, IReadOnlyCollection<char> taken)
    {
        var number = index + 1;
        var text = $"{number}. {shell.Name} ({shell.CommandLine})".Replace("&", "&&");
        var digit = (char)('0' + number);
        return number <= 9 && !taken.Contains(digit) ? "&" + text : text;
    }

    /// <summary>The Remove a shell item of the user's shell at this index: "1. Remove name", its own submenu's numbers as its mnemonics.</summary>
    public static string Remove(int index, UserShell shell) => AxKit.Core.AxKitWindows.Label(index, "Remove " + shell.Name);

    /// <summary>The mnemonic of a menu item's text, lower case; null when it has none ("&amp;&amp;" is an ampersand).</summary>
    public static char? Mnemonic(string text)
    {
        for (var i = 0; i < text.Length - 1; i++)
        {
            if (text[i] != '&')
            {
                continue;
            }

            if (text[i + 1] == '&')
            {
                i++;
                continue;
            }

            return char.ToLowerInvariant(text[i + 1]);
        }

        return null;
    }
}
