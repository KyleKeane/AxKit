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
    public async Task The_blocklist_is_read_without_a_token_from_its_one_address_and_a_missing_one_blocks_nothing()
    {
        var fake = new Fake()
            .Answer(HttpStatusCode.OK, "{\"reporters\": [\"6f1c2d3e-4a5b-4c6d-8e7f-901234567890\", \"not a guid\"], \"versions\": [\"5.1.0\", \"\"]}")
            .Answer(HttpStatusCode.NotFound, "404: Not Found");
        var list = await GitHubIssues.ReadBlocklistAsync(Client(fake), CancellationToken.None);
        Assert.Equal([Reporter], list.Reporters);
        Assert.Equal(["5.1.0"], list.Versions);
        Assert.True(list.Blocks(Reporter, "5.2.0"));
        Assert.True(list.Blocks(Guid.NewGuid(), "5.1.0"));
        Assert.False(list.Blocks(Guid.NewGuid(), "5.2.0"));
        Assert.Equal("https://raw.githubusercontent.com/KyleKeane/AxKit-blocklist/main/blocklist.json", GitHubIssues.BlocklistUrl);
        Assert.Equal(GitHubIssues.BlocklistUrl, fake.Requests[0].Url);
        Assert.Null(fake.Requests[0].Authorization);

        var none = await GitHubIssues.ReadBlocklistAsync(Client(fake), CancellationToken.None);
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

    /// <summary>
    /// The blocklist's address is written in one place, and the browser's form, which would post into a repository
    /// that is private since 2026-11-14, is gone (docs/axKit/reports.md RP-6.5).
    /// </summary>
    [Fact]
    public void The_reports_code_names_the_blocklist_once_and_no_browser_form()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("The repository's root was not found.");
        }

        var sources = string.Concat(Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .Select(File.ReadAllText));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(sources, "raw\\.githubusercontent"));
        Assert.DoesNotContain("issues/new", sources);
    }
}
