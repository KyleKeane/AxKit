using System.Globalization;

namespace AxKit.Core;

/// <summary>
/// One thing the user agreed to (AX-11.5): an access layer's first connection to a program (App "axClaude", Target
/// "Claude Code"), or the terms of an update (App <see cref="Consent.UpdateApp"/>, Target "axKit 4.0.0"), with axKit's
/// version at the time and the date.
/// </summary>
public sealed record ConsentRecord(string App, string Target, string Version, DateTime When);

/// <summary>
/// A program or service an access layer connects to (AX-11.4): its name, which is the consent's target, what it does,
/// whose terms apply and where they are.
/// </summary>
public sealed record ConsentTarget(string Program, string Work, string Terms, string TermsUrl)
{
    /// <summary>
    /// What the notice's sentences and its check box call the program: <see cref="Program"/> itself, unless the target
    /// is longer than a name (an MCP server's, which holds the whole of what would run, repairs.md R-61).
    /// </summary>
    public string Name { get; init; } = Program;

    public static readonly ConsentTarget ClaudeCode = new(
        "Claude Code",
        "Claude Code, Anthropic's program, and Anthropic's service behind it do the work under your own Anthropic account: everything you type goes to them, and Claude Code can create, change and delete files, run commands and use your account's usage or money.",
        "Anthropic's terms and privacy policy",
        "https://www.anthropic.com/legal");

    /// <summary>
    /// AI help (docs/axKit/ai-help.md AH-1.2): Claude Code in its print mode with read-only tools, recorded under the
    /// app "AI help", one agreement for every window (axHelp's conversation is axClaude's, under axClaude's own agreement).
    /// </summary>
    public static readonly ConsentTarget AiHelp = new(
        "Claude Code",
        "Claude Code, Anthropic's program, answers under your own Anthropic account: your question, what the window gathered about itself (its name, its keyboard shortcuts and guide, the names of its controls and your last actions there, never the text of a document), a picture of the screen when you ask for one, and the files Claude reads in the folders you allowed go to Anthropic's service and use your account's usage or money. Here Claude Code runs with read-only tools: it changes no file and runs no command.",
        "Anthropic's terms and privacy policy",
        "https://www.anthropic.com/legal");

    public static readonly ConsentTarget Git = new(
        "Git",
        "Git, a program you install, does the work on your repository: its commands change, remove and publish work.",
        "Git's licence",
        "https://git-scm.com/about/free-and-open-source");

    public static readonly ConsentTarget KeePassXC = new(
        "KeePassXC",
        "KeePassXC's command-line tool, a program you install, reads, encrypts and writes your password database; your master password goes to it.",
        "KeePassXC's licence",
        "https://keepassxc.org/");

    /// <summary>
    /// The reports (docs/axKit/reports.md RP-6.6): GitHub, where a report is stored encrypted in a private repository;
    /// recorded under the app name "axKit", since every window sends the same way. The target's name changed on
    /// 2026-11-14 with the terms (until then "GitHub", <see cref="GitHubReportsBefore"/>, for a public issue), so that
    /// a user who agreed to the old words is asked again with the new ones.
    /// </summary>
    public static readonly ConsentTarget GitHubReports = new(
        "GitHub, encrypted reports",
        "Your report is encrypted on your computer before it is sent. It is stored as an issue in a private repository on GitHub that only axKit's maintainer can open, and only the maintainer's key can read it. "
        + "GitHub, a Microsoft company, stores the encrypted text and sees when and from which network address it was sent. "
        + "It is posted with axKit's reporter token, never with an account of yours, and carries a random id of this install. Nothing in it names you or your files unless you write it or check the box for more detail. "
        + "The maintainer may quote or summarise a report in axKit's public repository when fixing it, without anything that identifies you.",
        "GitHub's terms and privacy statement",
        "https://docs.github.com/site-policy");

    /// <summary>The reports' target of 5.1.0 to 5.8.0, whose words said "public issue": an agreement to it does not count for <see cref="GitHubReports"/> and is taken out when the new one is recorded.</summary>
    public const string GitHubReportsBefore = "GitHub";

    /// <summary>
    /// The target of a program Install it now installs (<see cref="ProgramInstall"/>'s name), so that agreeing there
    /// counts for the first connection. A shell installed for axTerminal passes its own target (Git for Windows is
    /// "Git Bash" there, not "Git"); an unknown name is a mistake, never Windows' terms by default (review, 2026-11-02).
    /// </summary>
    public static ConsentTarget For(string program) => program switch
    {
        "Claude Code" => ClaudeCode,
        "Git" => Git,
        "KeePassXC" => KeePassXC,
        "PowerShell 7" => Shell("PowerShell 7"),
        _ => throw new ArgumentException($"No consent target for the program {program}", nameof(program)),
    };

    /// <summary>A shell of axTerminal by its name as ShellLauncher says it: each is its own consent (AX-11.5). A custom shell has no address for its terms.</summary>
    public static ConsentTarget Shell(string name) => new(
        name,
        $"{name} runs every command you type, with your rights, on your computer and on any network the commands reach.",
        name switch
        {
            "PowerShell 7" => "PowerShell's licence",
            "Git Bash" => "Git for Windows' licence",
            "Custom shell" => "the terms of the program you set",
            _ => "Microsoft's licence terms for Windows",
        },
        name switch
        {
            "PowerShell 7" => "https://github.com/PowerShell/PowerShell/blob/master/LICENSE.txt",
            "Git Bash" => "https://gitforwindows.org/",
            "Custom shell" => "",
            _ => "https://www.microsoft.com/useterms",
        });

    /// <summary>
    /// A shell started from a command line of the user's own (axTerminal's Custom shell and the user's shells, AT-1.6a;
    /// repairs.md R-53): the consent's target is the name with the command line, so that agreeing to one command line
    /// is not agreeing to another, and a command line put under an agreed name is asked about. No address for its terms.
    /// </summary>
    public static ConsentTarget CommandLineShell(string name, string commandLine) => new(
        $"{name}, command line {commandLine}",
        $"{name} is a shell of your own, started with the command line {commandLine}. It runs every command you type, with your rights, on your computer and on any network the commands reach.",
        "the terms of the program you set",
        string.Empty);

    /// <summary>A server a repository's remote points at (github.com): a new service with its own terms, asked before the first fetch, pull or push that reaches it.</summary>
    public static ConsentTarget Server(string host) => new(
        host,
        $"{host} receives what you push and sends what you fetch or pull, under your account there if it asks for one.",
        $"the terms of {host}",
        $"https://{host}/");

    /// <summary>
    /// An AI tool paired with axKit's windows (AX-9, docs/axKit/mcp.md): it reads what the windows show. Any program may
    /// pair, so its maker's terms have no one address here (an earlier version named Anthropic's for every tool).
    /// </summary>
    public static ConsentTarget AiTool(string name) => new(
        name,
        $"{name} reads what axKit's windows show and can open apps; what it does with what it reads is under its own terms, and it may send it to its maker's service.",
        $"the terms of {name}'s maker",
        string.Empty);
}

/// <summary>The shape of the file: one list, oldest first.</summary>
internal sealed class ConsentList
{
    public List<ConsentRecord> Items { get; set; } = [];

    /// <summary>The seal over the agreements (AX-5.6, AX-11.5): written at every write, checked at every read.</summary>
    public string Seal { get; set; } = string.Empty;

    /// <summary>The members of the file this version does not know, kept and written back (AX-5.8; repairs.md R-67); under no seal.</summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? OtherMembers { get; set; }

    /// <summary>The sealed fields: each agreement as its app, its target and axKit's version; not the date, which reads differently in another time zone.</summary>
    public string SealedText() =>
        SettingsSeal.Text((Items ?? []).SelectMany(item => new[] { item?.App, item?.Target, item?.Version }));
}

/// <summary>
/// What the user has agreed to (docs/axKit/SPEC.md AX-11.4 to AX-11.8), in one file for the bundle
/// (<see cref="AppPaths.ConsentFile"/>). An access layer asks <see cref="Given"/> before it connects to a program and
/// calls <see cref="Record"/> when the user checks the box and connects; About, Consents lists <see cref="Read"/> and
/// <see cref="Withdraw"/> takes one back, so that the app asks again. A missing or damaged file means nothing was agreed,
/// so the user is asked again rather than taken to have agreed. Tested in <c>tests/AxKit.Tests</c>.
/// </summary>
public static class Consent
{
    /// <summary>The App of an update's acceptance (AX-11.7).</summary>
    public const string UpdateApp = "Update";

    /// <summary>What a window says with its question when <see cref="Problem"/> finds the file changed outside axKit (AX-11.5, repairs.md R-53).</summary>
    public const string NotUsed = "The record of what you agreed to was changed outside axKit and is not used, so axKit asks again.";

    /// <summary>
    /// The agreements axKit wrote: none when the file's seal does not check (<see cref="SettingsSeal"/>), since an
    /// agreement another program wrote into the file is not the user's. A file from before the seal counts as it is
    /// until the next agreement is recorded, which seals it.
    /// </summary>
    public static IReadOnlyList<ConsentRecord> Read(string file)
    {
        var list = SettingsFile.Load<ConsentList>(file, out _);
        if (SettingsSeal.Checks(file, list.Seal, list.SealedText(), new ConsentList().SealedText()))
        {
            return [.. (list.Items ?? []).Where(item => item is not null)];
        }

        SettingsSeal.Refused(null, NotUsed);
        return [];
    }

    /// <summary><see cref="NotUsed"/> when the file holds agreements that are not used, for the question that follows; null otherwise.</summary>
    public static string? Problem(string file)
    {
        var list = SettingsFile.Load<ConsentList>(file, out _);
        return SettingsSeal.Checks(file, list.Seal, list.SealedText(), new ConsentList().SealedText()) ? null : NotUsed;
    }

    public static bool Given(string file, string app, string target) => Read(file).Any(item => Same(item, app, target));

    /// <summary>Keeps the agreement, replacing an earlier one for the same app and program. A file that cannot be written is logged: the user is then asked again next time (the window keeps the agreement for the session).</summary>
    public static void Record(string file, string app, string target, string version, DateTime when)
    {
        var items = Read(file).Where(item => !Same(item, app, target)).Append(new ConsentRecord(app, target, version, when));
        if (Write(file, items))
        {
            Log.Info($"Consent recorded: {app}, {target}, axKit {version}");
        }
    }

    public static void Withdraw(string file, string app, string target)
    {
        if (Write(file, Read(file).Where(item => !Same(item, app, target))))
        {
            Log.Info($"Consent withdrawn: {app}, {target}");
        }
    }

    /// <summary>Every agreement of one app taken back in one write (Forget AI tools, docs/axKit/mcp.md).</summary>
    public static void WithdrawAll(string file, string app)
    {
        if (Write(file, Read(file).Where(item => !string.Equals(item.App, app, StringComparison.OrdinalIgnoreCase))))
        {
            Log.Info($"Consents withdrawn: {app}");
        }
    }

    /// <summary>"axClaude, Claude Code, 29 October 2026, axKit 4.0.0"; "Update to axKit 4.0.0, 29 October 2026".</summary>
    public static string Line(ConsentRecord item)
    {
        var date = item.When.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        return item.App == UpdateApp ? $"Update to {item.Target}, {date}" : $"{item.App}, {item.Target}, {date}, axKit {item.Version}";
    }

    private static bool Write(string file, IEnumerable<ConsentRecord> items)
    {
        try
        {
            // The file as it is, with the agreements replaced: what a newer version keeps beside them stays (AX-5.8).
            List<ConsentRecord> agreed = [.. items];
            var list = SettingsFile.Load<ConsentList>(file, out _);
            list.Items = agreed;
            list.Seal = SettingsSeal.Of(file, list.SealedText());
            if (SettingsFile.Save(file, list))
            {
                SettingsSeal.Remember(file);
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The consent file was not written", ex);
            return false;
        }
    }

    private static bool Same(ConsentRecord item, string app, string target) =>
        string.Equals(item.App, app, StringComparison.OrdinalIgnoreCase) && string.Equals(item.Target, target, StringComparison.OrdinalIgnoreCase);
}
