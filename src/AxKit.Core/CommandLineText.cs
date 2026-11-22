namespace AxKit.Core;

/// <summary>
/// A command line as the log may hold it (security audit S-54): the arguments joined by spaces, with the value of
/// <c>--message</c>, a text that may be a stranger's words (until 2026-11-14 axTask's Open in axClaude put a GitHub
/// issue's body there), replaced by its length. The log's last lines go into every F4 report and Copy diagnostics.
/// </summary>
public static class CommandLineText
{
    public static string ForLog(IReadOnlyList<string> args)
    {
        var parts = new List<string>(args.Count);
        for (var i = 0; i < args.Count; i++)
        {
            parts.Add(args[i]);
            if (args[i] == "--message" && i + 1 < args.Count)
            {
                parts.Add($"<message, {args[++i].Length} characters>");
            }
            else if (args[i] == "--command")
            {
                // A command handed to axTerminal (AT-1.5a) holds an item's values: everything after the option is it.
                parts.Add($"<command, {args.Count - i - 1} parts>");
                break;
            }
        }

        return string.Join(" ", parts);
    }
}