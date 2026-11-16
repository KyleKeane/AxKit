namespace AxAutomate;

/// <summary>
/// The command line of a run, read once (docs/axKit/user-workflows.md section 6.4):
/// <c>AxAutomate &lt;script or folder&gt; (--exe &lt;AxKit.exe&gt; | --build) [--report &lt;file.md&gt;] [--keep]
/// [--nvda] [--repository &lt;folder&gt;] [--inventory check|write|rewrite|off] [--traces check|write|off]</c>.
/// <paramref name="Repository"/> is the folder named by <c>--repository</c>, else the one above the program that holds
/// <c>AxKit.sln</c> (<see cref="FindRepository"/>), else null: without one the command inventory and the traces are
/// off. <paramref name="Inventory"/> is one of <see cref="Check"/> (the default), <see cref="Write"/>,
/// <see cref="Rewrite"/> and <see cref="Off"/>. <paramref name="Traces"/> (WF-5.5, WF-5.8) is <see cref="Check"/>
/// (the default), <see cref="Write"/> or <see cref="Off"/>, and always <see cref="Off"/> on the NVDA tier and without
/// a repository: only the hidden desktop writes and compares kept traces (WF-5.4). <paramref name="TraceReport"/>:
/// the report holds every script's trace, which is so when <c>--report</c> and <c>--traces check</c> or
/// <c>write</c> are both given by name.
/// The command line's other form, <c>AxAutomate --reference [--check] [--repository &lt;folder&gt;]</c>, plays nothing
/// and is read by <see cref="ReadReference"/>.
/// </summary>
internal sealed record RunOptions(string Target, string? Exe, bool Build, string? Report, bool Keep, bool Nvda, string? Repository, string Inventory, string Traces, bool TraceReport)
{
    public const string Check = "check";
    public const string Write = "write";
    public const string Rewrite = "rewrite";
    public const string Off = "off";

    /// <summary>The first word of the reference's form: the workflow pages written, or checked (ReferenceRun, WF-7.1).</summary>
    public const string ReferenceOption = "--reference";

    /// <summary>
    /// The options of 6.4 that a later package of user-workflows.md builds, with that package: refused until then, so
    /// that a run never seems to do what it does not.
    /// </summary>
    private static readonly Dictionary<string, string> NotBuilt = new(StringComparer.Ordinal)
    {
        ["--record"] = "WP-B13",
        ["--convert"] = "WP-B15",
        ["--ai-tools"] = "WP-B16",
    };

    /// <summary>
    /// The options of a command line, or why it cannot run: an option not known, one without its value, an inventory
    /// or traces mode not known, a repository without <c>AxKit.sln</c>, an option not built yet ("not built yet"), a
    /// writing inventory or <c>--traces write</c> without a repository, and <c>--traces check</c> or <c>write</c>
    /// with <c>--nvda</c>. Every refusal ends the run with exit code 2.
    /// </summary>
    public static (RunOptions? Options, string Refusal) Read(IReadOnlyList<string> args)
    {
        if (NotBuilt.TryGetValue(args[0], out var later))
        {
            return (null, $"{args[0]} is not built yet (docs/axKit/user-workflows.md {later}).");
        }

        if (args[0].StartsWith("--", StringComparison.Ordinal))
        {
            return (null, $"Name the script or folder first, then the options: {args[0]} came first.");
        }

        string? exe = null;
        string? report = null;
        string? repository = null;
        var keep = false;
        var build = false;
        var nvda = false;
        var inventory = Check;
        string? traces = null;
        for (var i = 1; i < args.Count; i++)
        {
            var option = args[i];
            if (NotBuilt.TryGetValue(option, out var package))
            {
                return (null, $"{option} is not built yet (docs/axKit/user-workflows.md {package}).");
            }

            if (option is "--exe" or "--report" or "--repository" or "--inventory" or "--traces" && i + 1 >= args.Count)
            {
                return (null, $"{option} needs a value.");
            }

            switch (option)
            {
                case "--exe": exe = args[++i]; break;
                case "--build": build = true; break;
                case "--report": report = args[++i]; break;
                case "--keep": keep = true; break;
                case "--nvda": nvda = true; break;
                case "--repository": repository = args[++i]; break;
                case "--inventory":
                    inventory = args[++i];
                    if (inventory is not (Check or Write or Rewrite or Off))
                    {
                        return (null, $"--inventory takes check, write, rewrite or off, not {inventory}.");
                    }

                    break;
                case "--traces":
                    traces = args[++i];
                    if (traces is not (Check or Write or Off))
                    {
                        return (null, $"--traces takes check, write or off, not {traces}.");
                    }

                    break;
                case ReferenceOption or "--check":
                    return (null, $"{option} goes with the form that writes the workflow pages and plays nothing: AxAutomate {ReferenceOption} [--check] [--repository <folder>].");
                default:
                    return (null, $"Unknown option: {option}.");
            }
        }

        if (repository is not null)
        {
            repository = Path.GetFullPath(repository);
            if (!File.Exists(Path.Combine(repository, "AxKit.sln")))
            {
                return (null, $"--repository names a folder without AxKit.sln: {repository}.");
            }
        }

        repository ??= FindRepository();
        if (repository is null && inventory is Write or Rewrite)
        {
            return (null, $"--inventory {inventory} writes the repository's docs/axKit/window-commands.json, and this program is not in a repository: name it with --repository.");
        }

        // The kept traces (WF-5.4, WF-5.8): the hidden desktop's alone, so off on the NVDA tier, and off without a
        // repository, where there is no file to hold a run against. What cannot be done is refused when asked for.
        if (nvda && traces is Check or Write)
        {
            return (null, $"--traces {traces} is for the hidden desktop alone: the NVDA tier neither writes nor compares kept traces (docs/axKit/user-workflows.md WF-5.4).");
        }

        if (repository is null && traces == Write)
        {
            return (null, "--traces write writes the repository's tests/traces, and this program is not in a repository: name it with --repository.");
        }

        // Every script's trace goes into the report only when the traces were asked for by name: a traced script is
        // asked once more after each line, and a run that names no --traces plays its unmarked scripts as before.
        var asked = traces is Check or Write;
        traces = nvda || repository is null ? Off : traces ?? Check;
        return (new RunOptions(args[0], exe, build, report, keep, nvda, repository, inventory, traces, report is not null && asked && traces != Off), string.Empty);
    }

    /// <summary>
    /// The reference's form read (6.4, WF-7.1): <c>AxAutomate --reference [--check] [--repository &lt;folder&gt;]</c>,
    /// the first word <see cref="ReferenceOption"/>. Whether it checks or writes, and the repository whose pages it
    /// writes, <c>--repository</c> or else the one above the program (<see cref="FindRepository"/>). The repository is
    /// null with why it cannot run: an option not known, a repository without <c>AxKit.sln</c>, none found; every
    /// refusal ends the run with exit code 2.
    /// </summary>
    public static (string? Repository, bool CheckPages, string Refusal) ReadReference(IReadOnlyList<string> args)
    {
        string? repository = null;
        var checkPages = false;
        for (var i = 1; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--check":
                    checkPages = true;
                    break;
                case "--repository" when i + 1 < args.Count:
                    repository = Path.GetFullPath(args[++i]);
                    if (!File.Exists(Path.Combine(repository, "AxKit.sln")))
                    {
                        return (null, false, $"--repository names a folder without AxKit.sln: {repository}.");
                    }

                    break;
                case "--repository":
                    return (null, false, "--repository needs a value.");
                default:
                    return (null, false, $"{ReferenceOption} takes --check and --repository <folder> alone, not {args[i]}.");
            }
        }

        repository ??= FindRepository();
        return repository is null
            ? (null, false, $"{ReferenceOption} writes the pages of a repository, and this program is not in one: name it with --repository.")
            : (repository, checkPages, string.Empty);
    }

    /// <summary>
    /// The folder above the program that holds <c>AxKit.sln</c>, or null when none does (6.4): a runner built into
    /// <c>%TEMP%</c>, as <c>nvda-pass.ps1</c> builds it, never reads or writes the files of whatever folder it was
    /// started in. The build keeps its own fallback to the current folder.
    /// </summary>
    public static string? FindRepository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AxKit.sln")))
            {
                return dir.FullName;
            }
        }

        return null;
    }
}
