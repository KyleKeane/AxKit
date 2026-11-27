using System.Text.RegularExpressions;

namespace AxGit.Core;

/// <summary>
/// A repository's own programs (SPEC.md AG-1.6; the security audit of 2026-10-24 S-2). A repository that comes in
/// an archive, on a USB stick or a share brings its own <c>.git/config</c> and <c>.git/hooks</c>, which git obeys: a
/// file monitor run by <c>git status</c>, diff and filter programs, hooks on commit and switch, an ssh command on fetch.
/// axGIT reads which of these the repository names (reading the configuration runs nothing) and, until the user trusts
/// the repository, runs every git command with each of them set to nothing. Pure, tested; the reads themselves, in
/// their order, are <see cref="GitTrustScan"/>.
///
/// What is read (the review of 2026-11-27): every setting git takes from the repository itself, which is its own
/// file, what that includes, and the work tree's own file when the repository has one
/// (<c>extensions.worktreeConfig</c>; <c>--local</c>, asked until then, left that file out); the hooks folder; and
/// the same for every repository inside it, a submodule's, since git runs itself inside a submodule on a plain
/// <c>git status</c> and obeys that repository's settings there (tried with git 2.53: the submodule's file monitor
/// ran). The settings axGIT gives on the command line reach those runs too, so a name found in a submodule is set
/// to nothing for all of them.
/// </summary>
public static partial class GitTrust
{
    /// <summary>
    /// The configuration's names with where each comes from, one per line, the scope, a tab, the name: every file
    /// git would read for this repository, the repository's own, what it includes and the work tree's own among them.
    /// </summary>
    public static readonly IReadOnlyList<string> ConfigArguments = ["config", "--show-scope", "--includes", "--name-only", "--list"];

    /// <summary>The scopes that are not the repository's: the computer's file, the user's file, and what axGIT itself gives on the command line.</summary>
    private static readonly string[] OtherScopes = ["system", "global", "command"];

    /// <summary>
    /// The names the repository itself sets, from git's answer to <see cref="ConfigArguments"/>: every line whose
    /// scope is not the computer's, the user's or the command line's. A line without a scope counts as the
    /// repository's, so that nothing is passed over for its form.
    /// </summary>
    public static IReadOnlyList<string> OwnNames(string output)
    {
        var names = new List<string>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tab = line.IndexOf('\t');
            if (tab < 0)
            {
                names.Add(line);
            }
            else if (!OtherScopes.Contains(line[..tab], StringComparer.OrdinalIgnoreCase))
            {
                names.Add(line[(tab + 1)..].Trim());
            }
        }

        return names;
    }

    /// <summary>The folders git keeps the hooks and the submodules' repositories in: two lines.</summary>
    public static readonly IReadOnlyList<string> FoldersArguments = ["rev-parse", "--git-path", "hooks", "--git-path", "modules"];

    /// <summary>
    /// The hooks folder and the folder of the submodules' repositories as full paths, from git's answer to
    /// <see cref="FoldersArguments"/>: two lines, each a path that is relative to the repository's root unless the
    /// configuration names another; null for a line git did not print.
    /// </summary>
    public static (string? Hooks, string? Modules) Folders(string output, string root)
    {
        var lines = output.Split('\n', StringSplitOptions.TrimEntries);
        string? Full(int at) => lines.Length > at && lines[at].Length > 0 ? Path.GetFullPath(lines[at], root) : null;
        return (Full(0), Full(1));
    }

    /// <summary>
    /// A hooks folder that holds nothing: git looks for <c>NUL\pre-commit</c> and finds no file, on every Windows
    /// (a folder of axKit's own could be filled by another program).
    /// </summary>
    public const string NoHooks = "NUL";

    // ---- The repositories inside a repository (submodules) ----

    /// <summary>The index with each entry's kind, the entries ended by a zero character so that no path is quoted.</summary>
    public static readonly IReadOnlyList<string> LinksArguments = ["ls-files", "--stage", "-z"];

    /// <summary>What the index calls an entry that is another repository's commit: a submodule's place.</summary>
    private const string LinkMode = "160000";

    /// <summary>The paths, with forward slashes as git prints them, of the entries that are another repository (git's answer to <see cref="LinksArguments"/>: "mode object stage", a tab, the path).</summary>
    public static IReadOnlyList<string> Links(string output)
    {
        var paths = new List<string>();
        foreach (var entry in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var tab = entry.IndexOf('\t');
            if (tab > 0 && entry.StartsWith(LinkMode + " ", StringComparison.Ordinal))
            {
                paths.Add(entry[(tab + 1)..]);
            }
        }

        return paths;
    }

    /// <summary>What a work tree's <c>.git</c> file starts with when the repository lies elsewhere.</summary>
    private const string GitFileMark = "gitdir:";

    /// <summary>
    /// The repository of a folder that may be a submodule's work tree: its <c>.git</c> folder, or the folder its
    /// <c>.git</c> file names (a path relative to the work tree); null when it has neither, which is a submodule
    /// that was never fetched, where git runs nothing. A <c>.git</c> file that cannot be read is given as it is,
    /// so that the read of its settings fails and is named.
    /// </summary>
    public static string? GitFolderOf(string worktree)
    {
        var dot = Path.Combine(worktree, ".git");
        if (Directory.Exists(dot))
        {
            return Path.GetFullPath(dot);
        }

        if (!File.Exists(dot))
        {
            return null;
        }

        try
        {
            // The first line alone, and no more than a path can be long: the file is the repository's own.
            using var reader = new StreamReader(dot);
            var buffer = new char[4096];
            var line = new string(buffer, 0, reader.ReadBlock(buffer, 0, buffer.Length)).Split('\n')[0].Trim();
            return line.StartsWith(GitFileMark, StringComparison.Ordinal) && line[GitFileMark.Length..].Trim() is { Length: > 0 } path
                ? Path.GetFullPath(path, Path.GetFullPath(worktree))
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Path.GetFullPath(dot);
        }
    }

    /// <summary>
    /// The repositories under the folder git keeps the submodules' in (<c>.git/modules</c>), also one that has no
    /// work tree now: a folder with a <c>config</c> and a <c>HEAD</c> file is one, and below it only its own
    /// <c>modules</c> folder is looked into. A link to another folder is not followed.
    /// </summary>
    public static IReadOnlyList<string> ModuleFolders(string modules)
    {
        var found = new List<string>();
        void Look(string folder)
        {
            IEnumerable<string> inside;
            try
            {
                inside = Directory.GetDirectories(folder).Order(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return;
            }

            foreach (var one in inside)
            {
                if (new DirectoryInfo(one).Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                if (File.Exists(Path.Combine(one, "config")) && File.Exists(Path.Combine(one, "HEAD")))
                {
                    found.Add(Path.GetFullPath(one));
                    if (Directory.Exists(Path.Combine(one, "modules")))
                    {
                        Look(Path.Combine(one, "modules"));
                    }
                }
                else
                {
                    Look(one);
                }
            }
        }

        if (Directory.Exists(modules))
        {
            Look(modules);
        }

        return found;
    }

    /// <summary>What a risk inside a submodule starts with: "submodule libs/zip: core.sshcommand".</summary>
    private const string SubmoduleMark = "submodule ";

    /// <summary>A submodule whose settings git itself could not list: nothing of it can be set to nothing by name, so it is named as it is, and its hooks are set aside.</summary>
    public const string NotRead = "settings that could not be read";

    /// <summary>The risks of a repository inside the one opened, each named with it: "submodule libs/zip: hook post-checkout".</summary>
    public static IReadOnlyList<string> Inside(string submodule, IEnumerable<string> risks) =>
        [.. risks.Select(risk => $"{SubmoduleMark}{submodule}: {risk}")];

    /// <summary>The risk without the submodule it is in: the configuration name, "hook name", or <see cref="NotRead"/>. A path on Windows holds no colon, so the first one ends the submodule's.</summary>
    private static string Own(string risk) =>
        risk.StartsWith(SubmoduleMark, StringComparison.Ordinal) && risk.IndexOf(": ", StringComparison.Ordinal) is > 0 and var colon ? risk[(colon + 2)..] : risk;

    // ---- What runs a program ----

    /// <summary>Whether a configuration name makes git run a program.</summary>
    public static bool RunsAProgram(string name) => ProgramNames().IsMatch(name.Trim());

    /// <summary>
    /// What in a repository runs a program: the configuration names that do, among those the repository itself sets
    /// (<paramref name="config"/>, git's answer to <see cref="ConfigArguments"/>), and "hook name" for each hook
    /// in <paramref name="hooksFolder"/> that is not one of git's own samples.
    /// </summary>
    public static IReadOnlyList<string> Risks(string config, string? hooksFolder)
    {
        // Each once, by its exact text: a name's middle part ("pdf" in diff.pdf.textconv) is another name in other case.
        var risks = OwnNames(config)
            .Where(RunsAProgram)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (hooksFolder is not null && Directory.Exists(hooksFolder))
        {
            risks.AddRange(Directory.EnumerateFiles(hooksFolder)
                .Select(Path.GetFileName)
                .Where(name => name is not null && !name.EndsWith(".sample", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(name => "hook " + name));
        }

        return risks;
    }

    /// <summary>
    /// The <c>-c</c> settings that make git ignore what <paramref name="risks"/> names: every such name set to nothing,
    /// once, whichever repository it was found in (the settings of the command line reach git's runs inside a
    /// submodule), and the hooks folder to <see cref="NoHooks"/> when any hook or hooks folder is named, or a
    /// submodule could not be read. Empty for a trusted repository.
    /// </summary>
    public static IReadOnlyList<string> Overrides(IReadOnlyList<string> risks)
    {
        var list = new List<string>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var hooks = false;
        foreach (var risk in risks.Select(Own))
        {
            if (risk.StartsWith("hook ", StringComparison.Ordinal) || risk.Equals("core.hookspath", StringComparison.OrdinalIgnoreCase) || risk == NotRead)
            {
                hooks = true;
                continue;
            }

            if (names.Add(risk))
            {
                list.Add("-c");
                list.Add(risk + "=");
            }
        }

        if (hooks)
        {
            list.Add("-c");
            list.Add("core.hooksPath=" + NoHooks);
        }

        return list;
    }

    /// <summary>
    /// The mark of what a repository would run (AG-1.6, repairs.md R-53): the SHA-256, as hexadecimal, of the risks in
    /// lower case and sorted, each with its length in front. A trust is kept with it, so that a repository that gains
    /// or loses a hook or a configured program is asked about again. It marks the names, not what a hook's file holds.
    /// </summary>
    public static string Mark(IReadOnlyList<string> risks) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            AxKit.Core.SettingsSeal.Text(risks.Select(risk => risk.ToLowerInvariant()).Order(StringComparer.Ordinal)))));

    /// <summary>
    /// The names that run a program when one of the commands axGIT runs needs it (status, diff, show, log, add,
    /// restore, commit, switch, branch, fetch, pull, push): the core programs; the external diff; diff, merge and
    /// filter drivers and the tools' commands; a pager for one command; credential helpers; the signing programs
    /// and the command that finds an ssh signing key; the editors; a commit trailer's command; a hook set in the
    /// configuration; the remote's own programs and its helper (<c>vcs</c>). An included file's names are listed
    /// among the repository's own (<c>--includes</c>), so <c>include.path</c> and <c>includeIf</c> name no
    /// program themselves. Left out, since no command axGIT runs reaches them: an alias (run only when called by
    /// name), <c>submodule.name.update</c> with a command (run by <c>git submodule update</c> alone, which axGIT
    /// never runs; its fetch, pull and push do not go into submodules, <see cref="GitTexts.FetchArguments"/>),
    /// and the programs of <c>git send-email</c>, <c>git help</c>, <c>git gui</c> and <c>git add -p</c>.
    /// </summary>
    [GeneratedRegex(@"^(core\.(fsmonitor|hookspath|sshcommand|pager|editor|askpass|gitproxy|alternaterefscommand)|diff\.external|(diff|merge|filter|difftool|mergetool)\..+\.(command|textconv|driver|clean|smudge|process|cmd|path)|pager\..+|credential(\..+)?\.helper|gpg(\..+)?\.program|gpg\.ssh\.defaultkeycommand|sequence\.editor|trailer\..+\.(command|cmd)|hook\..+\.command|remote\..+\.(uploadpack|receivepack|proxy|vcs)|(uploadpack|receivepack)\..+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ProgramNames();
}
