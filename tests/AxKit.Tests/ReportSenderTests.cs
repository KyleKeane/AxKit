using System.Net;
using System.Text;
using AxKit.Core;
using AxKit.Core.Reports;

namespace AxKit.Tests;

/// <summary>The sender over a Reports folder and canned GitHub answers (docs/axKit/reports.md RP-6), and the token's file form (RP-6.2).</summary>
public sealed class ReportSenderTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axkit-tests", "sender-" + Guid.NewGuid().ToString("N"));
    private readonly string _settings;
    private DateTime _now = new(2026, 11, 7, 15, 0, 0, DateTimeKind.Utc);

    public ReportSenderTests()
    {
        Directory.CreateDirectory(_folder);
        _settings = Path.Combine(_folder, "reports.json");
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private sealed class Fake : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _answers = new();

        public List<(HttpMethod Method, string Url, string? Body)> Requests { get; } = [];

        public Fake Answer(HttpStatusCode status, string body)
        {
            _answers.Enqueue((status, body));
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!.ToString(), request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            var (status, text) = _answers.Count > 0 ? _answers.Dequeue() : (HttpStatusCode.InternalServerError, "{\"message\":\"no canned answer\"}");
            return new HttpResponseMessage(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }

    private static string Issue(int number) => $"{{\"number\": {number}, \"html_url\": \"https://github.com/KyleKeane/AxKit-reports/issues/{number}\"}}";

    private string Save(string title, string words, ReportKind kind = ReportKind.Bug, int minutesAgo = 10) =>
        ReportFiles.Save(new Report(title, kind, words, "axFile 5.1.0", "Files, item 1 of 3", ["5 s ago: Find, Ctrl+F"], [], _now.AddMinutes(-minutesAgo)), ReportStatus.NotSent(null), _folder);

    private Task<SendOutcome> Send(Fake fake, string? token = "tok") =>
        ReportSender.SendUnsentAsync(_folder, _settings, "5.1.0", token, ReportSealTests.Keys.Public, new HttpClient(fake), () => _now, CancellationToken.None);

    /// <summary>The title and the body of the issue a request asked GitHub to make.</summary>
    private static (string Title, string Body) Posted(string? request)
    {
        using var json = System.Text.Json.JsonDocument.Parse(request!);
        return (json.RootElement.GetProperty("title").GetString()!, json.RootElement.GetProperty("body").GetString()!);
    }

    [Fact]
    public async Task Unsent_reports_go_oldest_first_as_sealed_issues_and_are_marked_sent()
    {
        var older = Save("Older heron", "first marmalade", minutesAgo: 20);
        var newer = Save("Newer", "second", ReportKind.Feature, minutesAgo: 5);
        var fake = new Fake().Answer(HttpStatusCode.NotFound, "no blocklist").Answer(HttpStatusCode.Created, Issue(12)).Answer(HttpStatusCode.Created, Issue(13));

        var outcome = await Send(fake);
        Assert.Equal(2, outcome.Sent);
        Assert.Equal(0, outcome.Left);
        Assert.Null(outcome.Reason);
        Assert.Equal(13, outcome.Last?.Number);

        Assert.Equal(3, fake.Requests.Count);
        Assert.Equal(GitHubIssues.BlocklistUrl, fake.Requests[0].Url);
        Assert.Null(fake.Requests[0].Body);
        fake.Requests.RemoveAt(0);
        Assert.All(fake.Requests, request => Assert.Equal("https://api.github.com/repos/KyleKeane/AxKit-reports/issues", request.Url));
        Assert.All(fake.Requests, request => Assert.Equal(HttpMethod.Post, request.Method));
        Assert.Contains("\"labels\":[\"bug\"]", fake.Requests[0].Body);
        Assert.Contains("\"labels\":[\"feature\"]", fake.Requests[1].Body);

        // The title is the kind and the version; the body is the envelope, with the reporter id and the version in the
        // clear and nothing of the report: not its title, its words, its context or its actions.
        var reporter = ReportSender.ReporterId(_settings);
        var (title, body) = Posted(fake.Requests[0].Body);
        Assert.Equal("Bug report, axKit 5.1.0", title);
        Assert.Equal("Feature request, axKit 5.1.0", Posted(fake.Requests[1].Body).Title);
        var envelope = ReportSeal.Read(body);
        Assert.NotNull(envelope);
        Assert.Equal(reporter, envelope.Reporter);
        Assert.Equal("5.1.0", envelope.Version);
        Assert.Equal("bug", envelope.Kind);
        Assert.Equal(ReportSeal.Fingerprint(ReportSealTests.Keys.Public), envelope.Key);
        foreach (var words in new[] { "Older", "heron", "marmalade", "axFile", "item 1 of 3", "Ctrl+F", "Sent from axKit" })
        {
            Assert.DoesNotContain(words, fake.Requests[0].Body);
        }

        // The key's other half reads it all.
        var plain = ReportSeal.Open(body, ReportSealTests.Keys.Private);
        Assert.StartsWith("Title: Older heron\nKind: Bug\n\nfirst marmalade\n", plain);
        Assert.Contains("- App and version: axFile 5.1.0\n", plain);
        Assert.Contains("5 s ago: Find, Ctrl+F", plain);
        Assert.EndsWith($"Sent from axKit 5.1.0 with F4; reporter {reporter:D}\n", plain);

        // The item on the computer stays as the user wrote it, plain.
        Assert.Contains("first marmalade", File.ReadAllText(older));

        var olderItem = ItemDocument.Parse(File.ReadAllText(older));
        Assert.Equal(ReportStatus.Sent, ReportFiles.Status(olderItem));
        Assert.Contains("issues/12", olderItem.ToText());
        Assert.Equal(ReportStatus.Sent, ReportFiles.Status(ItemDocument.Parse(File.ReadAllText(newer))));
        Assert.Empty(ReportFiles.Unsent(_folder));

        // The blocklist is kept a day: the next run reads it from the settings, and the gap rule holds for half a minute.
        Save("Third", "third", minutesAgo: 1);
        _now = _now.AddSeconds(10);
        var soon = await Send(new Fake().Answer(HttpStatusCode.Created, Issue(14)));
        Assert.Equal(ReportSender.GapReason, soon.Reason);
        Assert.Equal(0, soon.Sent);
        _now = _now.AddMinutes(1);
        var later = new Fake().Answer(HttpStatusCode.Created, Issue(14));
        var third = await Send(later);
        Assert.Equal(1, third.Sent);
        Assert.Single(later.Requests);
        Assert.Contains("/issues", later.Requests[0].Url);
    }

    [Fact]
    public async Task Without_a_token_nothing_goes_and_every_item_says_so()
    {
        var path = Save("No token", "words");
        var fake = new Fake();
        var outcome = await Send(fake, token: null);
        Assert.Equal(ReportSender.NoTokenReason, outcome.Reason);
        Assert.Equal(1, outcome.Left);
        Assert.Empty(fake.Requests);
        Assert.Equal("Not sent yet: this build cannot send reports", ReportFiles.Status(ItemDocument.Parse(File.ReadAllText(path))));
    }

    [Fact]
    public async Task A_report_that_cannot_be_sealed_is_never_sent_plain()
    {
        var path = Save("Unsealed", "words that stay here");
        var fake = new Fake().Answer(HttpStatusCode.NotFound, "no blocklist").Answer(HttpStatusCode.Created, Issue(12));
        var outcome = await ReportSender.SendUnsentAsync(_folder, _settings, "5.1.0", "tok", "not a key", new HttpClient(fake), () => _now, CancellationToken.None);
        Assert.Equal(0, outcome.Sent);
        Assert.Equal(1, outcome.Left);
        Assert.Equal(ReportSender.SealReason, outcome.Reason);
        // The blocklist was asked for, and no issue: nothing of the report went anywhere.
        Assert.Equal(GitHubIssues.BlocklistUrl, Assert.Single(fake.Requests).Url);
        Assert.Equal("Not sent yet: the report could not be encrypted", ReportFiles.Status(ItemDocument.Parse(File.ReadAllText(path))));
    }

    [Fact]
    public async Task A_blocked_install_posts_nothing_of_any_unsent_report_and_sends_again_within_a_day_of_its_unblocking()
    {
        var reporter = ReportSender.ReporterId(_settings);
        var older = Save("Older", "words", minutesAgo: 600);
        var newer = Save("Newer", "words");
        var fake = new Fake().Answer(HttpStatusCode.OK, $"{{\"reporters\": [\"{reporter:D}\"], \"versions\": []}}").Answer(HttpStatusCode.Created, Issue(12));
        var outcome = await Send(fake);
        Assert.Equal(ReportSender.BlockedInstallReason, outcome.Reason);
        Assert.Equal(0, outcome.Sent);
        Assert.Equal(2, outcome.Left);
        // The list was read, from the public repository of its own, and nothing was posted: not the new report, not the older one.
        var read = Assert.Single(fake.Requests);
        Assert.Equal(HttpMethod.Get, read.Method);
        Assert.Equal("https://raw.githubusercontent.com/KyleKeane/AxKit-blocklist/main/blocklist.json", read.Url);
        Assert.Equal("Not sent yet: reports from this install are not being accepted", ReportFiles.Status(ItemDocument.Parse(File.ReadAllText(older))));
        Assert.Equal("Not sent yet: reports from this install are not being accepted", ReportFiles.Status(ItemDocument.Parse(File.ReadAllText(newer))));

        // Within the day the copy answers: still blocked, and no request at all.
        _now = _now.AddHours(23);
        var still = new Fake().Answer(HttpStatusCode.Created, Issue(12));
        Assert.Equal(ReportSender.BlockedInstallReason, (await Send(still)).Reason);
        Assert.Empty(still.Requests);

        // After a day the list is asked again; the install is off it, and both reports go, without the user doing anything else.
        _now = _now.AddHours(2);
        var unblocked = new Fake().Answer(HttpStatusCode.OK, "{\"reporters\": [], \"versions\": []}").Answer(HttpStatusCode.Created, Issue(12)).Answer(HttpStatusCode.Created, Issue(13));
        var after = await Send(unblocked);
        Assert.Equal(2, after.Sent);
        Assert.Null(after.Reason);
        Assert.Equal(3, unblocked.Requests.Count);
        Assert.Equal(GitHubIssues.BlocklistUrl, unblocked.Requests[0].Url);
        Assert.Empty(ReportFiles.Unsent(_folder));
    }

    [Fact]
    public async Task A_blocked_version_posts_nothing_and_says_to_update()
    {
        Save("Blocked version", "words");
        var fake = new Fake().Answer(HttpStatusCode.OK, "{\"reporters\": [], \"versions\": [\"5.1.0\"]}").Answer(HttpStatusCode.Created, Issue(12));
        var outcome = await Send(fake);
        Assert.Equal(ReportSender.BlockedVersionReason, outcome.Reason);
        Assert.Contains("update axKit", outcome.Reason);
        Assert.Equal(HttpMethod.Get, Assert.Single(fake.Requests).Method);
        Assert.Equal(1, outcome.Left);
    }

    [Fact]
    public void The_block_list_has_a_public_repository_of_its_own_and_its_page_is_where_a_blocked_install_asks()
    {
        Assert.Equal("KyleKeane/AxKit-blocklist", ReportSender.BlocklistRepository);
        Assert.NotEqual(ReportSender.Repository, ReportSender.BlocklistRepository);
        Assert.Equal("https://github.com/KyleKeane/AxKit-blocklist/issues", ReportSender.BlocklistIssuesUrl);
        Assert.Equal("https://raw.githubusercontent.com/KyleKeane/AxKit-blocklist/main/blocklist.json", GitHubIssues.BlocklistUrl);

        // The notice of a blocked install names the id, offers to copy it and opens that page, never the reports repository.
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("The repository's root was not found.");
        }

        var flow = File.ReadAllText(Path.Combine(root, "src", "AxKit.Forms", "ReportFlow.cs"));
        Assert.Contains("case ReportSender.BlockedInstallReason:", flow);
        Assert.Contains("case ReportSender.BlockedVersionReason:", flow);
        Assert.Contains("Your install id: {id}", flow);
        Assert.Contains("\"Copy &install id\"", flow);
        Assert.Contains("_form.OpenUrl(ReportSender.BlocklistIssuesUrl)", flow);
        Assert.DoesNotContain("OpenUrl(GitHubIssues", flow);
        Assert.DoesNotContain("AxKit-reports", flow);
    }

    [Fact]
    public async Task A_refusal_stops_the_run_and_writes_its_reason_into_the_item()
    {
        var first = Save("First", "words", minutesAgo: 20);
        var second = Save("Second", "words", minutesAgo: 5);
        var fake = new Fake().Answer(HttpStatusCode.NotFound, "none").Answer(HttpStatusCode.Unauthorized, "{\"message\":\"Bad credentials\"}");
        var outcome = await Send(fake);
        Assert.Equal(0, outcome.Sent);
        Assert.Equal(2, outcome.Left);
        Assert.Equal("this version can no longer send reports, update axKit", outcome.Reason);
        Assert.Equal("Not sent yet: this version can no longer send reports, update axKit", ReportFiles.Status(ItemDocument.Parse(File.ReadAllText(first))));
        Assert.Equal("Not sent yet", ReportFiles.Status(ItemDocument.Parse(File.ReadAllText(second))));
        Assert.Equal(2, fake.Requests.Count);
    }

    [Fact]
    public async Task No_network_is_said_in_two_words_and_nothing_is_lost()
    {
        var path = Save("Offline", "words");
        var outcome = await ReportSender.SendUnsentAsync(_folder, _settings, "5.1.0", "tok", ReportSealTests.Keys.Public, new HttpClient(new Throwing()), () => _now, CancellationToken.None);
        Assert.Equal("no network", outcome.Reason);
        Assert.Equal("Not sent yet: no network", ReportFiles.Status(ItemDocument.Parse(File.ReadAllText(path))));
        Assert.Single(ReportFiles.Unsent(_folder));
    }

    private sealed class Throwing : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("The remote name could not be resolved");
    }

    [Fact]
    public void The_reporter_id_is_made_once_and_kept()
    {
        var first = ReportSender.ReporterId(_settings);
        Assert.NotEqual(Guid.Empty, first);
        Assert.Equal(first, ReportSender.ReporterId(_settings));
        Assert.Contains(first.ToString("D"), File.ReadAllText(_settings));
    }

    [Theory]
    [InlineData("github_pat_11ABCDEF_example")]
    [InlineData("ghp_short")]
    public void The_token_file_form_round_trips_and_is_not_the_token_itself(string token)
    {
        var encoded = ReporterToken.Encode(token);
        Assert.DoesNotContain(token, encoded);
        Assert.Equal(token, ReporterToken.Decode(encoded));
        Assert.Equal(token, ReporterToken.Decode(" " + encoded + "\r\n"));
        Assert.Null(ReporterToken.Decode(string.Empty));
        Assert.Null(ReporterToken.Decode("not base64!"));
        // ReporterToken.Token itself is the build's: null in a build without the file, the token on a computer where
        // tools/reporter-token.ps1 wrote it; neither is asserted, since the tests run on both.
    }
}
