using System.Net;
using System.Text;
using AxKit.Core.Reports;

namespace AxKit.Tests;

/// <summary>GitHub's REST API as the report sender uses it (docs/axKit/reports.md RP-6), against canned answers.</summary>
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
        var list = new Blocklist([Reporter], ["5.1.0"]);
        var back = Blocklist.Parse(list.ToJson());
        Assert.Equal(list.Reporters, back.Reporters);
        Assert.Equal(["5.1.0"], back.Versions);
        Assert.Equal(Blocklist.Empty, Blocklist.Parse("not json") with { Reporters = [], Versions = [] });
        Assert.Empty(Blocklist.Parse("[1, 2]").Reporters);
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
        Assert.EndsWith(Uri.EscapeDataString("(cut: the rest is in the report saved on your computer, Help, Your reports)"), longUrl);
    }
}
