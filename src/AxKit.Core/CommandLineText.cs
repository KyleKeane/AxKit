namespace AxKit.Core;

/// <summary>
/// A command line as the log may hold it (security audit S-54): the arguments joined by spaces, with the value of
/// <c>--message</c>, a text that may be a stranger's words from GitHub (axTask's Open in axClaude puts an issue's
/// body there), replaced by its length. The log's last lines go into every F4 report and Copy diagnostics.
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
        }

        return string.Join(" ", parts);
    }
}