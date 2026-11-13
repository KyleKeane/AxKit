namespace AxKit.Core.Reports;

/// <summary>The reports' own settings (docs/axKit/reports.md RP-6.3): the install's reporter id, the blocklist last read, the last send.</summary>
public sealed class ReportSettings
{
    public Guid? ReporterId { get; set; }

    public string? Blocklist { get; set; }

    public DateTime? BlocklistReadUtc { get; set; }

    public DateTime? LastSentUtc { get; set; }
}

/// <summary>
/// What one run of the sender did (RP-6.7): how many reports went, how many are still waiting, why it stopped in a
/// few words when it did, and the issue made last.
/// </summary>
public sealed record SendOutcome(int Sent, int Left, string? Reason, IssueCreated? Last)
{
    public static readonly SendOutcome Nothing = new(0, 0, null, null);
}

/// <summary>
/// Sends the reports that wait in the Reports list (RP-6): the reporter id made once and kept, the blocklist read
/// before a send and kept a day, each unsent item posted as an issue of the reports repository as the reporter
/// account, oldest first, and marked Sent with its issue, or marked with the reason it could not go. Pure apart from
/// the files it is given: the client and the clock are handed in.
/// </summary>
public static class ReportSender
{
    public const string Repository = "KyleKeane/AxKit-reports";

    /// <summary>The reasons the sender itself gives (the others come from GitHub, <see cref="GitHubIssues.ReasonOf"/>).</summary>
    public const string NoTokenReason = "this build cannot send reports";
    public const string BlockedReason = "reports from this install or version are blocked";
    public const string GapReason = "wait half a minute between reports";

    public static readonly TimeSpan BlocklistAge = TimeSpan.FromDays(1);
    public static readonly TimeSpan RequestLimit = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MinimumGap = TimeSpan.FromSeconds(30);

    /// <summary>The install's reporter id (RP-6.3), made and kept on the first call.</summary>
    public static Guid ReporterId(string settingsFile)
    {
        var settings = SettingsFile.Load<ReportSettings>(settingsFile, out _);
        if (settings.ReporterId is not { } id || id == Guid.Empty)
        {
            id = Guid.NewGuid();
            settings.ReporterId = id;
            SettingsFile.Save(settingsFile, settings);
        }

        return id;
    }

    /// <summary>
    /// Sends every unsent report of the folder (RP-6.7). Without a token nothing goes and every item says so; a blocked
    /// install or version sends nothing; a send within half a minute of the last waits; the first failure stops the
    /// run, its reason written into that item, the rest left as they were for the next run.
    /// </summary>
    public static async Task<SendOutcome> SendUnsentAsync(string folder, string settingsFile, string version, string? token, HttpClient http, Func<DateTime>? clock, CancellationToken cancellation)
    {
        var unsent = ReportFiles.Unsent(folder);
        if (unsent.Count == 0)
        {
            return SendOutcome.Nothing;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            Mark(unsent, NoTokenReason);
            return new SendOutcome(0, unsent.Count, NoTokenReason, null);
        }

        var now = clock?.Invoke() ?? DateTime.UtcNow;
        var settings = SettingsFile.Load<ReportSettings>(settingsFile, out _);
        if (settings.ReporterId is not { } reporter || reporter == Guid.Empty)
        {
            reporter = Guid.NewGuid();
            settings.ReporterId = reporter;
        }

        if (settings.LastSentUtc is { } lastSent && now - lastSent < MinimumGap && now >= lastSent)
        {
            SettingsFile.Save(settingsFile, settings);
            return new SendOutcome(0, unsent.Count, GapReason, null);
        }

        Blocklist blocklist;
        try
        {
            blocklist = await BlocklistAsync(http, settings, now, cancellation).ConfigureAwait(false);
        }
        catch (Exception ex) when (Failed(ex, cancellation))
        {
            var reason = GitHubIssues.ReasonOf(ex);
            Mark(unsent, reason);
            SettingsFile.Save(settingsFile, settings);
            return new SendOutcome(0, unsent.Count, reason, null);
        }

        if (blocklist.Blocks(reporter, version))
        {
            Mark(unsent, BlockedReason);
            SettingsFile.Save(settingsFile, settings);
            return new SendOutcome(0, unsent.Count, BlockedReason, null);
        }

        var sent = 0;
        IssueCreated? last = null;
        string? stopped = null;
        foreach (var (path, item) in unsent)
        {
            if (ReportFiles.FromItem(item) is not { } report)
            {
                continue;
            }

            var body = ReportBody.Markdown(report, version, reporter);
            try
            {
                using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                limit.CancelAfter(RequestLimit);
                last = await GitHubIssues.CreateAsync(http, Repository, token, report.Title, body, ReportKinds.Label(report.Kind), limit.Token).ConfigureAwait(false);
                settings.LastSentUtc = clock?.Invoke() ?? DateTime.UtcNow;
                sent++;
            }
            catch (Exception ex) when (Failed(ex, cancellation))
            {
                stopped = GitHubIssues.ReasonOf(ex);
                ReportFiles.Write(path, ReportFiles.WithStatus(item, ReportStatus.NotSent(stopped)));
                break;
            }

            // The issue exists from here on: a failure to write that down must never mark the item as not sent, or
            // the next run posts it a second time (docs/axKit/repairs.json R-29). The run stops, and says why.
            try
            {
                ReportFiles.Write(path, ReportFiles.WithSent(item, last.Number, last.Url));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Error($"A report was sent as issue {last.Number}, but its item could not be marked Sent", ex);
                stopped = $"sent as issue {last.Number}, but its item could not be marked Sent ({ex.Message}); mark it by hand before sending again";
                break;
            }
        }

        SettingsFile.Save(settingsFile, settings);
        return new SendOutcome(sent, unsent.Count - sent, stopped, last);
    }

    /// <summary>The blocklist kept a day in the settings, read again from GitHub when older (RP-6.4).</summary>
    private static async Task<Blocklist> BlocklistAsync(HttpClient http, ReportSettings settings, DateTime now, CancellationToken cancellation)
    {
        if (settings.Blocklist is { } cached && settings.BlocklistReadUtc is { } read && now - read < BlocklistAge && now >= read)
        {
            return Blocklist.Parse(cached);
        }

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        limit.CancelAfter(RequestLimit);
        var blocklist = await GitHubIssues.ReadBlocklistAsync(http, Repository, limit.Token).ConfigureAwait(false);
        settings.Blocklist = blocklist.ToJson();
        settings.BlocklistReadUtc = now;
        return blocklist;
    }

    /// <summary>A failure of the send itself, not the caller's own cancellation, which goes through.</summary>
    private static bool Failed(Exception ex, CancellationToken cancellation) =>
        !cancellation.IsCancellationRequested && ex is GitHubException or HttpRequestException or OperationCanceledException or IOException;

    private static void Mark(IReadOnlyList<(string Path, ItemDocument Item)> items, string reason)
    {
        foreach (var (path, item) in items)
        {
            ReportFiles.Write(path, ReportFiles.WithStatus(item, ReportStatus.NotSent(reason)));
        }
    }
}
