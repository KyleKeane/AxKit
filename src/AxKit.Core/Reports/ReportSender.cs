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

    /// <summary>The waiting items of the folder that the report form did not write (RP-6.9): never sent, never changed, counted here.</summary>
    public int NotOfTheForm { get; init; }
}

/// <summary>
/// Sends the reports that wait in the Reports list (RP-6): the reporter id made once and kept, the blocklist read
/// before a send and kept a day, each unsent item sealed for the maintainer's key (<see cref="ReportSeal"/>) and
/// posted as an issue of the reports repository as the reporter account, oldest first, and marked Sent with its
/// issue, or marked with the reason it could not go. Pure apart from the files it is given: the client, the key and
/// the clock are handed in.
/// </summary>
public static class ReportSender
{
    public const string Repository = "KyleKeane/AxKit-reports";

    /// <summary>
    /// The block list's repository (RP-6.4, RP-D12): public, of its own, holding nothing but the list of blocked
    /// install ids and versions, so that the sender reads it without a sign-in while the reports repository is
    /// private. <see cref="GitHubIssues.BlocklistUrl"/> is the list's address in it.
    /// </summary>
    public const string BlocklistRepository = "KyleKeane/AxKit-blocklist";

    /// <summary>Where a blocked install asks, with its install id, when the block is a mistake: that repository's issues, in the browser.</summary>
    public const string BlocklistIssuesUrl = "https://github.com/" + BlocklistRepository + "/issues";

    /// <summary>The reasons the sender itself gives (the others come from GitHub, <see cref="GitHubIssues.ReasonOf"/>).</summary>
    public const string NoTokenReason = "this build cannot send reports";
    public const string BlockedInstallReason = "reports from this install are not being accepted";
    public const string BlockedVersionReason = "reports from this version of axKit are not being accepted, update axKit";
    public const string GapReason = "wait half a minute between reports";
    public const string SealReason = "the report could not be encrypted";

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
    /// Sends every unsent report of the folder (RP-6.7), each sealed for <paramref name="publicKey"/> before the
    /// request is made, so that nothing of the user's leaves the computer in the clear. Without a token nothing goes
    /// and every item says so; a blocked install or version sends nothing; a send within half a minute of the last
    /// waits; the first failure stops the run, its
    /// reason written into that item, the rest left as they were for the next run. A report that cannot be sealed is
    /// never sent plain: the run stops with <see cref="SealReason"/>. Only the items the report form wrote go
    /// (RP-6.9, <see cref="ReportWritten"/>, <paramref name="writtenFile"/>): another waiting item of the folder is
    /// neither sent nor marked, and the outcome counts it.
    /// </summary>
    public static async Task<SendOutcome> SendUnsentAsync(string folder, string settingsFile, string writtenFile, string version, string? token, string publicKey, HttpClient http, Func<DateTime>? clock, CancellationToken cancellation)
    {
        var (unsent, others) = ReportFiles.UnsentOfTheForm(folder, writtenFile);
        if (unsent.Count == 0)
        {
            return others == 0 ? SendOutcome.Nothing : SendOutcome.Nothing with { NotOfTheForm = others };
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            Mark(unsent, NoTokenReason);
            return new SendOutcome(0, unsent.Count, NoTokenReason, null) { NotOfTheForm = others };
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
            return new SendOutcome(0, unsent.Count, GapReason, null) { NotOfTheForm = others };
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
            return new SendOutcome(0, unsent.Count, reason, null) { NotOfTheForm = others };
        }

        // A blocked install or version posts nothing, of this report or of an older unsent one; the list is asked
        // again when its copy is a day old, so an install taken off the list sends again by itself.
        if (blocklist.Blocks(reporter, version))
        {
            var blocked = blocklist.BlocksReporter(reporter) ? BlockedInstallReason : BlockedVersionReason;
            Mark(unsent, blocked);
            SettingsFile.Save(settingsFile, settings);
            return new SendOutcome(0, unsent.Count, blocked, null) { NotOfTheForm = others };
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

            // Sealed here, on the user's computer, before any request: the issue holds nothing of the user's in the
            // clear, its title the kind and the version alone (RP-6.1).
            string body;
            try
            {
                body = ReportBody.Sealed(report, version, reporter, publicKey);
            }
            catch (ReportSealException ex)
            {
                Log.Error("A report could not be encrypted, so it was not sent", ex);
                stopped = SealReason;
                ReportFiles.Write(path, ReportFiles.WithStatus(item, ReportStatus.NotSent(stopped)));
                break;
            }

            try
            {
                using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                limit.CancelAfter(RequestLimit);
                last = await GitHubIssues.CreateAsync(http, Repository, token, ReportBody.IssueTitle(report.Kind, version), body, ReportKinds.Label(report.Kind), limit.Token).ConfigureAwait(false);
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
                stopped = $"sent as report {last.Number}, but its item could not be marked Sent ({ex.Message}); mark it by hand before sending again";
                break;
            }
        }

        SettingsFile.Save(settingsFile, settings);
        return new SendOutcome(sent, unsent.Count - sent, stopped, last) { NotOfTheForm = others };
    }

    /// <summary>The blocklist kept a day in the settings, read again from its public repository when older (RP-6.4).</summary>
    private static async Task<Blocklist> BlocklistAsync(HttpClient http, ReportSettings settings, DateTime now, CancellationToken cancellation)
    {
        if (settings.Blocklist is { } cached && settings.BlocklistReadUtc is { } read && now - read < BlocklistAge && now >= read)
        {
            return Blocklist.Parse(cached);
        }

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        limit.CancelAfter(RequestLimit);
        var blocklist = await GitHubIssues.ReadBlocklistAsync(http, limit.Token).ConfigureAwait(false);
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
