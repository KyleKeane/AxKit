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

    /// <summary>Commit what is staged with the message given, which is one argument, never a file or an editor.</summary>
    public static IReadOnlyList<string> CommitArguments(string message) => ["commit", "-m", message];

    /// <summary>Fetch from the branch's remote, and forget the remote branches that are gone there.</summary>
    public static readonly IReadOnlyList<string> FetchArguments = ["fetch", "--prune"];

    /// <summary>Pull only when the branch can be moved forward: never a merge commit or a rebase made by axGIT.</summary>
    public static readonly IReadOnlyList<string> PullArguments = ["pull", "--ff-only"];

    /// <summary>The remote a branch without an upstream is pushed to, and tied to.</summary>
    public const string DefaultRemote = "origin";

    /// <summary>
    /// Push: plain when the branch has an upstream (or HEAD is detached, which git then refuses in its own words);
    /// a branch without one is pushed to <see cref="DefaultRemote"/> and tied to it.
    /// </summary>
    public static IReadOnlyList<string> PushArguments(string? upstream, string? branch) =>
        upstream is null && branch is not null ? ["push", "--set-upstream", DefaultRemote, branch] : ["push"];

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
