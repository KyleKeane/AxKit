using System.Globalization;
using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>
/// Which app's window each line of a script reaches (docs/axKit/user-workflows.md WF-2.11, WF-7.3), read off the
/// script's own window steps, as <see cref="Run"/> plays them: a <c>start</c> opens a window and makes it the current
/// one, <c>adopt</c> takes one the app opened and makes it current, <c>use &lt;n&gt;</c> makes the n-th of them current
/// again; <c>launch</c> hands over to an open window and changes none. Pure: no window is asked.
/// </summary>
public static class ScriptApps
{
    /// <summary>
    /// For each line of a script, numbered from 1 at index 0: the name of the app whose window a step on that line
    /// reaches (a window step's own line: the window it makes current), or null when it cannot be read.
    /// <c>start &lt;verb&gt; ...</c> names the app of <see cref="Dispatch.Apps"/> with that verb (without regard to case,
    /// as <see cref="Dispatch.Decide"/> reads it); a start whose first word is no verb (a file alone) and <c>adopt</c>
    /// make an unknown one; <c>use &lt;n&gt;</c> the n-th of them, unknown when there is none; an <c>iffound</c> before
    /// a step is passed over. The lines before the first start reach none.
    /// </summary>
    public static IReadOnlyList<string?> ByLine(IReadOnlyList<string> fileLines)
    {
        var apps = new string?[fileLines.Count];
        var windows = new List<string?>();
        string? current = null;
        for (var i = 0; i < fileLines.Count; i++)
        {
            var line = fileLines[i].Trim();
            while (line.StartsWith("iffound ", StringComparison.Ordinal))
            {
                line = line[8..].TrimStart();
            }

            var space = line.IndexOf(' ');
            var word = space < 0 ? line : line[..space];
            var rest = space < 0 ? string.Empty : line[(space + 1)..].Trim();
            switch (word)
            {
                case "start":
                    var first = rest.Split(' ', 2)[0];
                    current = Dispatch.Apps.FirstOrDefault(app => string.Equals(app.Verb, first, StringComparison.OrdinalIgnoreCase))?.Name;
                    windows.Add(current);
                    break;
                case "adopt":
                    current = null;
                    windows.Add(null);
                    break;
                case "use":
                    current = int.TryParse(rest, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= 1 && number <= windows.Count
                        ? windows[number - 1]
                        : null;
                    break;
            }

            apps[i] = current;
        }

        return apps;
    }
}
