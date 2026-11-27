using System.Text.RegularExpressions;

namespace AxGit.Core;

/// <summary>
/// Git's own words that belong to no record of a view (root CLAUDE.md rule 12): the commands the window runs that
/// act on the whole repository, and the two texts of git's output that an announcement is chosen by. With the
/// classes beside it this folder is the one place for everything git is told and everything read from it:
/// <see cref="GitRunner"/> starts git (the program, the fixed configuration, the environment, the timeout);
/// <see cref="GitStatus"/>, <see cref="GitLog"/>, <see cref="GitBranches"/>, <see cref="GitTrust"/> and
/// <see cref="GitRemote"/> each hold the arguments of one command next to the parser of its output, and the records
/// they make (<see cref="ChangedFile"/>, <see cref="Commit"/>, <see cref="Branch"/>) hold the arguments of the
/// commands on one file, commit or branch. The window passes these argument lists on and reads the records; it
/// writes no option of git's and matches no text of git's itself, which <c>GitTextsTests</c> checks off the sources.
/// When git changes an option or a word, the entry here or in the parser is adjusted, and what the user hears and
/// presses stays. Nothing here is an abstraction over programs.
/// </summary>
public static partial class GitTexts
{
    // ---- Commands on the whole repository (SPEC.md AG-3.2, AG-3.5, AG-5.4) ----

    /// <summary>Stage all: every change of the working tree into the index, new and deleted files too.</summary>
    public static readonly IReadOnlyList<string> StageAllArguments = ["add", "-A"];

    /// <summary>Unstage all: the index back to HEAD, the working tree untouched.</summary>
    public static readonly IReadOnlyList<string> UnstageAllArguments = ["restore", "--staged", "."];

    // ---- Commands on several files at once (SPEC.md AG-3.2a, AG-3.4a; the selection, bundle AX-8.16, 2026-11-22) ----

    /// <summary>How many paths one git call takes: several calls for more, so that a command line never grows past what Windows allows.</summary>
    public const int PathsPerCall = 50;

    /// <summary>Stage the files given, each whole: new and deleted files too. The paths come after "--", so that none is read as an option.</summary>
    public static IReadOnlyList<string> StageArguments(IEnumerable<string> paths) => ["add", "--", .. paths];

    /// <summary>Unstage the files given: the index back to HEAD for them, the working tree untouched.</summary>
    public static IReadOnlyList<string> UnstageArguments(IEnumerable<string> paths) => ["restore", "--staged", "--", .. paths];

    /// <summary>Discard the changes to the files given: index and working tree back to HEAD. Never asked for an untracked file, which git would not know.</summary>
    public static IReadOnlyList<string> DiscardArguments(IEnumerable<string> paths) => ["restore", "--staged", "--worktree", "--", .. paths];

    /// <summary>Commit what is staged with the message given, which is one argument, never a file or an editor.</summary>
    public static IReadOnlyList<string> CommitArguments(string message) => ["commit", "-m", message];

    // Fetch, pull and push never go into submodules (the review of 2026-11-27, bundle AX-11.5 and rule 8): a
    // submodule has remotes of its own, on servers the consent did not name, and git would reach them by itself
    // (a fetch "on demand", a pull or push where the repository's settings say to recurse). axGIT's commands reach
    // the one remote the consent was asked for; a submodule is fetched and pushed in a terminal.

    /// <summary>What keeps a fetch, a pull and a push out of the submodules, whatever the repository's settings say.</summary>
    private const string NoSubmodules = "--no-recurse-submodules";

    // The remote is on the command line of every fetch, pull and push (the review's second pass of 2026-11-27,
    // AG-5.4a): the one the reminder was read for (GitReach), after "--", so that a name like an option is a name.
    // Run without one, git chose for itself, and not always the remote that had been asked about.

    /// <summary>What keeps a fetch to the one remote named, whatever <c>fetch.all</c> says.</summary>
    private const string OneRemote = "--no-all";

    // The remote's own programs are named on the command line, also in a trusted repository (the review's third
    // pass of 2026-11-27, D2): git takes the first remote.name.uploadpack it reads, the repository's, so an empty
    // one set after it set nothing aside, and the command line comes before every setting. For a server these are
    // the names its shell runs, git's own; for a folder or a share, git's own programs on this computer.

    /// <summary>The upload program a fetch or pull runs on the other side: git's own.</summary>
    private const string UploadPack = "--upload-pack=git-upload-pack";

    /// <summary>The receive program a push runs on the other side: git's own.</summary>
    private const string ReceivePack = "--receive-pack=git-receive-pack";

    /// <summary>Fetch from the remote named, and forget the remote branches that are gone there.</summary>
    public static IReadOnlyList<string> FetchArguments(string remote) => ["fetch", "--prune", NoSubmodules, OneRemote, UploadPack, "--", remote];

    /// <summary>Pull from the remote named, only when the branch can be moved forward: never a merge commit or a rebase made by axGIT.</summary>
    public static IReadOnlyList<string> PullArguments(string remote) => ["pull", "--ff-only", NoSubmodules, OneRemote, UploadPack, "--", remote];

    /// <summary>The remote a branch without an upstream is pushed to, and tied to.</summary>
    public const string DefaultRemote = GitRemote.Default;

    /// <summary>
    /// Push the branch, and only it, to the branch of the same name (the review's third pass, D6): the place is
    /// given whole, <c>refs/heads/name:refs/heads/name</c>, whatever <c>push.default</c> says, and a branch
    /// named <c>+main</c> is that branch (as "+main" alone it was read as a forced push of main, M4). To
    /// <paramref name="remote"/> when the branch has an upstream (a branch that follows one of another name is
    /// refused before, by <see cref="GitReach"/>); a branch without one is pushed to <see cref="DefaultRemote"/>
    /// and tied to it. There is no push without a branch: <see cref="GitReach"/> says so before one is asked for.
    /// </summary>
    public static IReadOnlyList<string> PushArguments(string remote, string? upstream, string branch) =>
        upstream is null
            ? ["push", NoSubmodules, ReceivePack, "--set-upstream", "--", DefaultRemote, BranchPlace(branch)]
            : ["push", NoSubmodules, ReceivePack, "--", remote, BranchPlace(branch)];

    /// <summary>A branch pushed to the branch of its own name, each side the full name of the reference.</summary>
    public static string BranchPlace(string branch) => $"refs/heads/{branch}:refs/heads/{branch}";

    /// <summary>
    /// Why a push of <paramref name="branch"/> does not start when the branch follows a branch of another name
    /// (<c>branch.name.merge</c>, read from <paramref name="settings"/>); null when it follows one of its own name
    /// or none. git's own default refuses there too; axGIT pushes a branch to its own name alone.
    /// </summary>
    public static string? FollowsAnotherName(IReadOnlyList<ListedSetting> settings, string branch)
    {
        var merge = settings.LastOrDefault(setting => setting.Name.Equals($"branch.{branch}.merge", StringComparison.Ordinal))?.Value.Trim();
        if (merge is null or { Length: 0 } || merge == $"refs/heads/{branch}")
        {
            return null;
        }

        var name = merge.StartsWith("refs/heads/", StringComparison.Ordinal) ? merge["refs/heads/".Length..] : merge;
        return $"This branch follows a branch of another name, {name}, and axGIT pushes a branch to the branch of its own name. Push it in a terminal.";
    }

    /// <summary>
    /// The remote <see cref="PushArguments"/> reaches, for the consent before it (<see cref="GitReach"/>):
    /// <see cref="DefaultRemote"/> for a branch without an upstream, otherwise the one the settings give
    /// (<see cref="GitRemote.PushRemote"/>).
    /// </summary>
    public static string PushRemote(string? upstream, string branch, IReadOnlyList<ListedSetting> settings) =>
        upstream is null ? DefaultRemote : GitRemote.PushRemote(settings, branch);

    // ---- What is read from a command's output outside the parsers ----

    /// <summary>What <c>git pull</c> prints when there was nothing to bring in ("Already up to date."); matched without case.</summary>
    public const string AlreadyUpToDate = "Already up to date";

    /// <summary>Whether a pull's output says that nothing came in.</summary>
    public static bool SaysUpToDate(string pullOutput) => pullOutput.Contains(AlreadyUpToDate, StringComparison.OrdinalIgnoreCase);

    /// <summary>"[main 3a1f2c0] Subject" at the start of a line of <c>git commit</c>'s output: group 1 is the short hash.</summary>
    [GeneratedRegex(@"^\[[^\]]+ ([0-9a-f]{7,})\]", RegexOptions.Multiline)]
    public static partial Regex CommitSummary();

    /// <summary>The short hash of the commit just made, from <c>git commit</c>'s output; null when git printed no such line.</summary>
    public static string? CommittedHash(string commitOutput) =>
        CommitSummary().Match(commitOutput) is { Success: true } match ? match.Groups[1].Value : null;
}
