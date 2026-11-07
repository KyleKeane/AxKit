using System.Net;
using System.Text;
using AxKit.Core;
using AxKit.Core.Reports;

namespace AxKit.Tests;

/// <summary>The sender over a Reports folder and canned GitHub answers (docs/axkit/reports.md RP-6), and the token's file form (RP-6.2).</summary>
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
        ReportSender.SendUnsentAsync(_folder, _settings, "5.1.0", token, new HttpClient(fake), () => _now, CancellationToken.None);

    [Fact]
    public async Task Unsent_reports_go_oldest_first_as_issues_and_are_marked_sent()
    {
        var older = Save("Older", "first", minutesAgo: 20);
        var newer = Save("Newer", "second", ReportKind.Feature, minutesAgo: 5);
        var fake = new Fake().Answer(HttpStatusCode.NotFound, "no blocklist").Answer(HttpStatusCode.Created, Issue(12)).Answer(HttpStatusCode.Created, Issue(13));

        var outcome = await Send(fake);
        Assert.Equal(2, outcome.Sent);
        Assert.Equal(0, outcome.Left);
        Assert.Null(outcome.Reason);
        Assert.Equal(13, outcome.Last?.Number);

        Assert.Equal(3, fake.Requests.Count);
        Assert.Equal("https://raw.githubusercontent.com/KyleKeane/AxKit-reports/main/blocklist.json", fake.Requests[0].Url);
        Assert.Contains("\"title\":\"Older\"", fake.Requests[1].Body);
        Assert.Contains("\"labels\":[\"bug\"]", fake.Requests[1].Body);
        Assert.Contains("\"labels\":[\"feature\"]", fake.Requests[2].Body);
        var reporter = ReportSender.ReporterId(_settings);
        Assert.Contains($"reporter {reporter:D}", fake.Requests[1].Body);

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
    public async Task A_blocked_install_sends_nothing()
    {
        var reporter = ReportSender.ReporterId(_settings);
        Save("Blocked", "words");
        var fake = new Fake().Answer(HttpStatusCode.OK, $"{{\"reporters\": [\"{reporter:D}\"], \"versions\": []}}");
        var outcome = await Send(fake);
        Assert.Equal(ReportSender.BlockedReason, outcome.Reason);
        Assert.Single(fake.Requests);
        Assert.Equal(1, outcome.Left);
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
        var outcome = await ReportSender.SendUnsentAsync(_folder, _settings, "5.1.0", "tok", new HttpClient(new Throwing()), () => _now, CancellationToken.None);
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
        Assert.Null(ReporterToken.Token);
    }
}
