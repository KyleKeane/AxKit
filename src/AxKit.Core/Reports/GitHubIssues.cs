using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxKit.Core.Reports;

/// <summary>An issue just created (docs/axKit/reports.md RP-6.1): its number and web address.</summary>
public sealed record IssueCreated(int Number, string Url);

/// <summary>GitHub answered with an error, or could not be reached: <see cref="Reason"/> says so in a few words for the Status field.</summary>
public sealed class GitHubException(string reason, HttpStatusCode? status, string? detail) : Exception(detail is null ? reason : $"{reason} ({detail})")
{
    public string Reason { get; } = reason;

    public HttpStatusCode? Status { get; } = status;
}

/// <summary>
/// GitHub's REST API as the report sender uses it (RP-6): an issue created as the reporter account and the blocklist
/// read, nothing else (axTask's GitHub lists, which listed and changed issues here, were removed 2026-11-14, RP-7). Pure:
/// the client is given, so that the tests hand in canned answers. Every call is to GitHub over HTTPS (AX-11.11).
/// </summary>
public static class GitHubIssues
{
    public const string ApiVersion = "2022-11-28";

    /// <summary>
    /// The whole address of the browser's form may be this long at most: the Windows shell hands a longer one to the
    /// browser cut off (the user's browser opened a page that did not load, 2026-11-07), so the body is cut to fit, the
    /// user's words first; the issue the sender posts is never cut.
    /// </summary>
    public const int FormUrlLength = 2000;

    private static string IssuesApi(string repository) => $"https://api.github.com/repos/{repository}/issues";

    private static string BlocklistUrl(string repository) => $"https://raw.githubusercontent.com/{repository}/main/blocklist.json";

    /// <summary>
    /// The browser's new-issue form of the repository, prefilled (RP-6.5), for a user with a GitHub account of their own:
    /// the title, the label and as much of the body as keeps the address within <see cref="FormUrlLength"/>, the
    /// user's words coming first in it; a cut body ends by saying where the rest is.
    /// </summary>
    public static string NewIssueFormUrl(string repository, string title, string body, string label)
    {
        var head = $"https://github.com/{repository}/issues/new?title={Uri.EscapeDataString(title)}&labels={Uri.EscapeDataString(label)}&body=";
        const string note = "\n\n(cut: the rest is in the report saved on your computer, Help, Your reports)";
        var text = body.Trim();
        var encoded = Uri.EscapeDataString(text);
        if (head.Length + encoded.Length <= FormUrlLength)
        {
            return head + encoded;
        }

        var room = Math.Max(0, FormUrlLength - head.Length - Uri.EscapeDataString(note).Length);
        var cut = text;
        while (cut.Length > 0 && Uri.EscapeDataString(cut).Length > room)
        {
            cut = cut[..Math.Max(0, cut.Length - Math.Max(1, cut.Length / 10))].TrimEnd();
        }

        return head + Uri.EscapeDataString(cut + note);
    }

    /// <summary>Creates an issue as the token's account (RP-6.1): the title, the Markdown body and one label.</summary>
    public static async Task<IssueCreated> CreateAsync(HttpClient http, string repository, string token, string title, string body, string label, CancellationToken cancellation)
    {
        var payload = new JsonObject { ["title"] = title, ["body"] = body, ["labels"] = new JsonArray(JsonValue.Create(label)) };
        using var request = Request(HttpMethod.Post, IssuesApi(repository), token, payload);
        // Outside call, network, GitHub: posts a report as an issue of the reports repository, as axKit's own reporter
        // account, with what the user read and edited in the report notice and the install's random reporter id; only
        // when the user sends a report (docs/axKit/reports.md RP-6.1, PRIVACY.md).
        using var response = await http.SendAsync(request, cancellation).ConfigureAwait(false);
        using var json = await ReadAsync(response, cancellation).ConfigureAwait(false);
        return new IssueCreated(Number(json.RootElement), JsonElements.GetString(json.RootElement, "html_url") ?? string.Empty);
    }

    /// <summary>The blocklist of the reports repository (RP-6.4); empty when there is none yet (404).</summary>
    public static async Task<Blocklist> ReadBlocklistAsync(HttpClient http, string repository, CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BlocklistUrl(repository));
        // Outside call, network, GitHub: reads the reports repository's blocklist before a report is sent, so that a
        // blocked install or version sends nothing; one GET with the version in the User-Agent and nothing else about
        // the user (docs/axKit/reports.md RP-6.4).
        using var response = await http.SendAsync(request, cancellation).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Blocklist.Empty;
        }

        await ThrowOnErrorAsync(response, cancellation).ConfigureAwait(false);
        return Blocklist.Parse(await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false));
    }

    /// <summary>What went wrong, in a few words for the Status field and the announcement (RP-6.4, RP-6.7).</summary>
    public static string ReasonOf(Exception exception) => exception switch
    {
        GitHubException github => github.Reason,
        OperationCanceledException => "GitHub took too long",
        HttpRequestException => "no network",
        _ => exception.Message,
    };

    private static HttpRequestMessage Request(HttpMethod method, string url, string token, JsonObject payload)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", ApiVersion);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        return request;
    }

    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response, CancellationToken cancellation)
    {
        await ThrowOnErrorAsync(response, cancellation).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException ex)
        {
            throw new GitHubException("GitHub answered with something that is not JSON", response.StatusCode, ex.Message);
        }
    }

    private static async Task ThrowOnErrorAsync(HttpResponseMessage response, CancellationToken cancellation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var status = response.StatusCode;
        var reason = status switch
        {
            HttpStatusCode.Unauthorized => "this version can no longer send reports, update axKit",
            HttpStatusCode.Forbidden => "GitHub refused (403): the account cannot do this, or too many requests",
            HttpStatusCode.NotFound => "GitHub found nothing there (404)",
            HttpStatusCode.UnprocessableEntity => "GitHub refused the content (422)",
            _ => $"GitHub said {(int)status}",
        };
        string? detail = null;
        try
        {
            var text = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
            using var json = JsonDocument.Parse(text);
            detail = JsonElements.GetString(json.RootElement, "message");
        }
        catch (Exception ex) when (ex is JsonException or HttpRequestException)
        {
            // GitHub's error page was not JSON: the status alone says enough.
        }

        throw new GitHubException(reason, status, detail);
    }

    private static int Number(JsonElement element) =>
        element.TryGetProperty("number", out var number) && number.ValueKind == JsonValueKind.Number ? number.GetInt32() : 0;
}

/// <summary>
/// The reports repository's <c>blocklist.json</c> (RP-6.4): the reporter ids and the axKit versions whose reports are
/// not sent. <c>{"reporters": ["6f1c…"], "versions": ["5.1.0"]}</c>; a file that is not that shape blocks nothing.
/// The maintainer changes the file on GitHub itself; axKit only reads it, and <see cref="ToJson"/> is the copy the
/// report sender keeps for a day.
/// </summary>
public sealed record Blocklist(IReadOnlyList<Guid> Reporters, IReadOnlyList<string> Versions)
{
    public static readonly Blocklist Empty = new([], []);

    public bool Blocks(Guid reporter, string version) =>
        Reporters.Contains(reporter) || Versions.Any(blocked => string.Equals(blocked.Trim(), version.Trim(), StringComparison.OrdinalIgnoreCase));

    public static Blocklist Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Empty;
            }

            var reporters = new List<Guid>();
            var versions = new List<string>();
            if (document.RootElement.TryGetProperty("reporters", out var ids) && ids.ValueKind == JsonValueKind.Array)
            {
                foreach (var id in ids.EnumerateArray())
                {
                    if (id.ValueKind == JsonValueKind.String && Guid.TryParse(id.GetString(), out var guid))
                    {
                        reporters.Add(guid);
                    }
                }
            }

            if (document.RootElement.TryGetProperty("versions", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var version in list.EnumerateArray())
                {
                    if (version.ValueKind == JsonValueKind.String && version.GetString() is { Length: > 0 } text)
                    {
                        versions.Add(text);
                    }
                }
            }

            return new Blocklist(reporters, versions);
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    public string ToJson()
    {
        var root = new JsonObject
        {
            ["reporters"] = new JsonArray(Reporters.Select(id => (JsonNode)JsonValue.Create(id.ToString("D"))).ToArray()),
            ["versions"] = new JsonArray(Versions.Select(version => (JsonNode)JsonValue.Create(version)).ToArray()),
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }
}
