using System.Text.Json;

namespace AxKit.Core.Ai;

/// <summary>
/// What Claude said (docs/axKit/ai-help.md AH-2.4): the text, whether Claude Code reported an error, and the session a
/// follow-up question resumes (AH-6.3).
/// </summary>
public sealed record AiAnswer(string Text, bool IsError, string? SessionId)
{
    /// <summary>
    /// The answer in what <c>claude -p --output-format json</c> printed: one JSON object with <c>result</c>,
    /// <c>is_error</c> and <c>session_id</c>, the last object on standard output should anything precede it. Nothing
    /// readable is an error with what was printed on either stream, or the exit code, so that the user hears Claude
    /// Code's own words.
    /// </summary>
    public static AiAnswer Parse(string output, string error, int exitCode)
    {
        var text = output.Replace("\r\n", "\n").Trim();
        var start = text.LastIndexOf("\n{", StringComparison.Ordinal);
        var candidate = start >= 0 ? text[(start + 1)..] : text;
        if (candidate.StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(candidate);
                var root = document.RootElement;
                var isError = root.TryGetProperty("is_error", out var flag) && flag.ValueKind == JsonValueKind.True;
                var result = root.TryGetProperty("result", out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
                var session = root.TryGetProperty("session_id", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
                if (result.Length > 0 || isError)
                {
                    return new AiAnswer(result.Length > 0 ? result.Trim() : "Claude Code reported an error without saying what", isError, session);
                }
            }
            catch (JsonException)
            {
                // Not the answer's object: said as printed below.
            }
        }

        var said = string.IsNullOrWhiteSpace(error) ? text : error.Trim();
        return new AiAnswer(said.Length > 0 ? said : $"Claude Code ended with exit code {exitCode} and printed nothing", true, null);
    }
}
