using System.Net;
using System.Text;
using AxKit.Core.Reports;

namespace AxKit.Tests;

/// <summary>GitHub's REST API as the reports use it (docs/axkit/reports.md RP-6, RP-7), against canned answers.</summary>
public sealed class GitHubIssuesTests
{
    private const string Repository = "KyleKeane/AxKit-reports";
    private static readonly Guid Reporter = Guid.Parse("6f1c2d3e-4a5b-4c6d-8e7f-901234567890");

    /// <summary>Answers each request from a queue and keeps what was asked.</summary>
    private sealed class Fake : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _answers = new();

        public List<(HttpMethod Method, string Url, string? Body, string? Authorization, string? Accept, string? ApiVersion, string? UserAgent)> Requests { get; } = [];

        public Fake Answer(HttpStatusCode status, string body)
        {
            _answers.Enqueue((status, body));
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method, request.RequestUri!.ToString(), body, request.Headers.Authorization?.ToString(), request.Headers.Accept.ToString(),
                request.Headers.TryGetValues("X-GitHub-Api-Version", out var versions) ? string.Join(",", versions) : null, request.Headers.UserAgent.ToString()));
            var (status, text) = _answers.Count > 0 ? _answers.Dequeue() : (HttpStatusCode.InternalServerError, "{\"message\":\"no canned answer\"}");
            return new HttpResponseMessage(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }

    private static HttpClient Client(Fake fake)
    {
        var client = new HttpClient(fake);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AxKit/5.1.0");
        return client;
    }

    [Fact]
    public async Task Create_posts_the_issue_with_the_token_and_reads_its_number_and_address()
    {
        var fake = new Fake().Answer(HttpStatusCode.Created, "{\"number\": 12, \"html_url\": \"https://github.com/KyleKeane/AxKit-reports/issues/12\", \"title\": \"x\"}");
        var created = await GitHubIssues.CreateAsync(Client(fake), Repository, "tok", "The list loses my place", "words\n", "bug", CancellationToken.None);
        Assert.Equal(12, created.Number);
        Assert.Equal("https://github.com/KyleKeane/AxKit-reports/issues/12", created.Url);

        var request = Assert.Single(fake.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.github.com/repos/KyleKeane/AxKit-reports/issues", request.Url);
        Assert.Equal("Bearer tok", request.Authorization);
        Assert.Equal("application/vnd.github+json", request.Accept);
        Assert.Equal(GitHubIssues.ApiVersion, request.ApiVersion);
        Assert.Equal("AxKit/5.1.0", request.UserAgent);
        Assert.Equal("{\"title\":\"The list loses my place\",\"body\":\"words\\n\",\"labels\":[\"bug\"]}", request.Body);
    }

    [Fact]
    public async Task An_error_answer_becomes_a_reason_in_a_few_words()
    {
        var fake = new Fake().Answer(HttpStatusCode.Unauthorized, "{\"message\":\"Bad credentials\"}");
        var ex = await Assert.ThrowsAsync<GitHubException>(() => GitHubIssues.CreateAsync(Client(fake), Repository, "old", "t", "b", "bug", CancellationToken.None));
        Assert.Equal("this version can no longer send reports, update axKit", ex.Reason);
        Assert.Equal(HttpStatusCode.Unauthorized, ex.Status);
        Assert.Contains("Bad credentials", ex.Message);
        Assert.Equal(ex.Reason, GitHubIssues.ReasonOf(ex));
        Assert.Equal("no network", GitHubIssues.ReasonOf(new HttpRequestException("dns")));
        Assert.Equal("GitHub took too long", GitHubIssues.ReasonOf(new TaskCanceledException()));
    }

    [Fact]
    public async Task The_blocklist_is_read_without_a_token_and_a_missing_one_blocks_nothing()
    {
        var fake = new Fake()
            .Answer(HttpStatusCode.OK, "{\"reporters\": [\"6f1c2d3e-4a5b-4c6d-8e7f-901234567890\", \"not a guid\"], \"versions\": [\"5.1.0\", \"\"]}")
            .Answer(HttpStatusCode.NotFound, "404: Not Found");
        var list = await GitHubIssues.ReadBlocklistAsync(Client(fake), Repository, CancellationToken.None);
        Assert.Equal([Reporter], list.Reporters);
        Assert.Equal(["5.1.0"], list.Versions);
        Assert.True(list.Blocks(Reporter, "5.2.0"));
        Assert.True(list.Blocks(Guid.NewGuid(), "5.1.0"));
        Assert.False(list.Blocks(Guid.NewGuid(), "5.2.0"));
        Assert.Equal("https://raw.githubusercontent.com/KyleKeane/AxKit-reports/main/blocklist.json", fake.Requests[0].Url);
        Assert.Null(fake.Requests[0].Authorization);

        var none = await GitHubIssues.ReadBlocklistAsync(Client(fake), Repository, CancellationToken.None);
        Assert.Empty(none.Reporters);
        Assert.False(none.Blocks(Reporter, "5.1.0"));
    }

    [Fact]
    public void A_blocklist_round_trips_and_a_broken_one_blocks_nothing()
    {
        var list = Blocklist.Empty.WithReporter(Reporter).WithVersion("5.1.0").WithVersion("5.1.0");
        var back = Blocklist.Parse(list.ToJson());
        Assert.Equal(list.Reporters, back.Reporters);
        Assert.Equal(["5.1.0"], back.Versions);
        Assert.Equal(Blocklist.Empty, Blocklist.Parse("not json") with { Reporters = [], Versions = [] });
        Assert.Empty(Blocklist.Parse("[1, 2]").Reporters);
    }

    [Fact]
    public async Task Listing_pages_through_the_issues_and_leaves_pull_requests_out()
    {
        var page = new StringBuilder("[");
        for (var i = 1; i <= GitHubIssues.PageSize; i++)
        {
            page.Append(i > 1 ? "," : string.Empty).Append($"{{\"number\": {i}, \"title\": \"Issue {i}\", \"body\": \"b\", \"state\": \"open\", \"labels\": [{{\"name\": \"bug\"}}], \"user\": {{\"login\": \"axkit-reporter\"}}, \"created_at\": \"2026-11-07T10:00:00Z\", \"updated_at\": \"2026-11-07T11:00:00Z\", \"html_url\": \"https://github.com/KyleKeane/AxKit-reports/issues/{i}\"}}");
        }

        page.Append(']');
        var fake = new Fake()
            .Answer(HttpStatusCode.OK, page.ToString())
            .Answer(HttpStatusCode.OK, "[{\"number\": 101, \"title\": \"A pull request\", \"pull_request\": {}, \"state\": \"open\"}, {\"number\": 102, \"title\": \"Last\", \"state\": \"open\", \"labels\": []}]");
        var issues = await GitHubIssues.ListAsync(Client(fake), Repository, "open", null, CancellationToken.None);
        Assert.Equal(GitHubIssues.PageSize + 1, issues.Count);
        Assert.Equal(2, fake.Requests.Count);
        Assert.Contains("state=open&per_page=100&page=1", fake.Requests[0].Url);
        Assert.Contains("page=2", fake.Requests[1].Url);
        Assert.Null(fake.Requests[0].Authorization);

        var first = issues[0];
        Assert.Equal(1, first.Number);
        Assert.Equal(["bug"], first.Labels);
        Assert.Equal("axkit-reporter", first.User);
        Assert.Equal(new DateTime(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc), first.CreatedAt);
        Assert.Equal(102, issues[^1].Number);
        Assert.Empty(issues[^1].Labels);
    }

    [Fact]
    public async Task Patch_and_comment_go_as_the_user_with_their_token()
    {
        var fake = new Fake().Answer(HttpStatusCode.OK, "{}").Answer(HttpStatusCode.Created, "{}");
        await GitHubIssues.PatchAsync(Client(fake), Repository, "mine", 12, ["feature"], "closed", CancellationToken.None);
        await GitHubIssues.CommentAsync(Client(fake), Repository, "mine", 12, "Fixed in 5.1.1", CancellationToken.None);
        Assert.Equal(HttpMethod.Patch, fake.Requests[0].Method);
        Assert.Equal("https://api.github.com/repos/KyleKeane/AxKit-reports/issues/12", fake.Requests[0].Url);
        Assert.Equal("{\"labels\":[\"feature\"],\"state\":\"closed\"}", fake.Requests[0].Body);
        Assert.Equal("Bearer mine", fake.Requests[0].Authorization);
        Assert.Equal("https://api.github.com/repos/KyleKeane/AxKit-reports/issues/12/comments", fake.Requests[1].Url);
        Assert.Equal("{\"body\":\"Fixed in 5.1.1\"}", fake.Requests[1].Body);
    }

    [Fact]
    public async Task Writing_the_blocklist_reads_its_version_first_and_puts_the_whole_file()
    {
        var fake = new Fake()
            .Answer(HttpStatusCode.OK, "{\"sha\": \"abc123\", \"content\": \"\"}")
            .Answer(HttpStatusCode.OK, "{}");
        var list = Blocklist.Empty.WithVersion("5.1.0");
        await GitHubIssues.WriteBlocklistAsync(Client(fake), Repository, "mine", list, "Block 5.1.0", CancellationToken.None);
        Assert.Equal(HttpMethod.Get, fake.Requests[0].Method);
        Assert.Equal(HttpMethod.Put, fake.Requests[1].Method);
        Assert.Equal("https://api.github.com/repos/KyleKeane/AxKit-reports/contents/blocklist.json", fake.Requests[1].Url);
        Assert.Contains("\"sha\":\"abc123\"", fake.Requests[1].Body);
        Assert.Contains("\"message\":\"Block 5.1.0\"", fake.Requests[1].Body);
        var content = System.Text.Json.JsonDocument.Parse(fake.Requests[1].Body!).RootElement.GetProperty("content").GetString();
        Assert.Equal(list.ToJson(), Encoding.UTF8.GetString(Convert.FromBase64String(content!)));

        // No file yet: no sha in the put.
        var fresh = new Fake().Answer(HttpStatusCode.NotFound, "{\"message\":\"Not Found\"}").Answer(HttpStatusCode.Created, "{}");
        await GitHubIssues.WriteBlocklistAsync(Client(fresh), Repository, "mine", list, "Start the blocklist", CancellationToken.None);
        Assert.DoesNotContain("\"sha\"", fresh.Requests[1].Body);
    }

    [Fact]
    public void The_browser_form_is_prefilled_and_a_long_body_is_cut()
    {
        var url = GitHubIssues.NewIssueFormUrl(Repository, "A title", "Line one\nLine two", "feedback");
        Assert.StartsWith("https://github.com/KyleKeane/AxKit-reports/issues/new?title=A%20title&labels=feedback&body=Line%20one%0ALine%20two", url);
        // A long body is cut so that the whole address stays within what the Windows shell hands a browser whole.
        var words = string.Join(' ', Enumerable.Repeat("my words", 600));
        var longUrl = GitHubIssues.NewIssueFormUrl(Repository, "t", words + "\n\n### Context\n\n- App: axFile", "bug");
        Assert.True(longUrl.Length <= GitHubIssues.FormUrlLength, longUrl.Length.ToString());
        Assert.StartsWith("https://github.com/KyleKeane/AxKit-reports/issues/new?title=t&labels=bug&body=my%20words", longUrl);
        Assert.EndsWith(Uri.EscapeDataString("(cut: the rest is in the report saved on your computer, axKit menu, Your reports)"), longUrl);
    }
}
