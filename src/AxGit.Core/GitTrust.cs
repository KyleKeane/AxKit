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
/// ran). The settings axGIT gives reach those runs too, so a name found in a submodule is set to nothing for all
/// of them.
///
/// What counts (the review's second pass of 2026-11-27, AG-1.6c): every setting whose name is not in the table of
/// the harmless ones (<see cref="Inert"/>), since a list of the names that run a program can never be complete
/// (Git LFS's <c>lfs.extension.name.clean</c> and <c>gc.recentObjectsHook</c> were not on it, and ran). What is
/// set aside is given to git through its environment (<see cref="Overrides"/>), never as <c>-c name=value</c>.
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
    /// <c>modules</c> folder is looked into. A link to another folder is not followed. The folders are followed
    /// <see cref="Deep"/> levels down and no further (a submodule's name with slashes is as many folders, and a
    /// repository's own <c>modules</c> folder one more); a folder at that depth that holds folders still is
    /// given in <c>TooDeep</c>, so that it is named and never taken as holding nothing.
    /// </summary>
    public static (IReadOnlyList<string> Found, IReadOnlyList<string> TooDeep) ModuleFolders(string modules)
    {
        var found = new List<string>();
        var tooDeep = new List<string>();
        void Look(string folder, int depth)
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

            if (depth > Deep)
            {
                if (inside.Any())
                {
                    tooDeep.Add(Path.GetFullPath(folder));
                }

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
                        Look(Path.Combine(one, "modules"), depth + 1);
                    }
                }
                else
                {
                    Look(one, depth + 1);
                }
            }
        }

        if (Directory.Exists(modules))
        {
            Look(modules, 1);
        }

        return (found, tooDeep);
    }

    /// <summary>How many levels of folders, and of repositories inside each other, the scan follows; what lies deeper is named as not read.</summary>
    public const int Deep = 12;

    /// <summary>
    /// The untracked entries of a work tree that git itself would show, ended by a zero character: a folder that
    /// is a repository of its own comes as one entry with a slash at its end, since git does not go into it.
    /// Reading them runs nothing.
    /// </summary>
    public static readonly IReadOnlyList<string> OthersArguments = ["ls-files", "--others", "--exclude-standard", "-z"];

    /// <summary>
    /// The folders that are a repository of their own with no entry in the index, from git's answer to
    /// <see cref="OthersArguments"/>: the entries that end in a slash, without it. Nothing runs in one yet; the
    /// first Stage all makes it an entry of the index, and from then on git runs itself inside it on every
    /// status. So it is read with the submodules, and the notice comes when the repository is opened.
    /// </summary>
    public static IReadOnlyList<string> Nested(string output) =>
        [.. output.Split('\0', StringSplitOptions.RemoveEmptyEntries).Where(entry => entry.Length > 1 && entry.EndsWith('/')).Select(entry => entry[..^1])];

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

    // ---- The settings known to be harmless (AG-1.6c) ----

    /// <summary>
    /// A setting known to run nothing and to redirect nothing: its name as git lists it, in lower case, with a star
    /// for a middle part of any text (<c>remote.*.url</c>), and why it is harmless, in one line.
    /// <paramref name="InsideOnly"/>: harmless in a repository inside the one opened and not in that one itself.
    /// </summary>
    public sealed record InertSetting(string Name, string Reason, bool InsideOnly = false);

    /// <summary>
    /// The settings a repository may carry without being asked about (the review's second pass of 2026-11-27).
    /// Until then the rule was a list of the names that run a program, and such a list is never complete: git and
    /// the programs it starts (Git LFS, maintenance, credential helpers) gain names without notice. So the rule is
    /// turned around: a setting is harmless only when its name is here, each with its reason, and every other name
    /// is a risk (<see cref="Risks"/>). What an everyday repository carries is here, as made by <c>git init</c>,
    /// <c>git clone</c>, <c>git submodule</c>, Git for Windows, GitHub's gh, Visual Studio Code and Git LFS, so that
    /// an ordinary repository opens without the notice. <c>include.path</c> and <c>includeIf</c> are never here:
    /// what they bring in changes with the branch and with any file they name. A new entry needs a reason that
    /// shows it runs no program, reaches no server and moves no file's place; <c>GitTrustTests</c> holds the form.
    /// </summary>
    public static readonly IReadOnlyList<InertSetting> Inert =
    [
        new("core.repositoryformatversion", "The format of the repository's own files, a number git checks."),
        new("core.filemode", "Whether a file's executable bit counts as a change."),
        new("core.bare", "Whether the repository has a work tree."),
        new("core.logallrefupdates", "Whether the moves of branches are written into the reflog."),
        new("core.symlinks", "Whether a link is checked out as a link or as a small file."),
        new("core.ignorecase", "Whether file names that differ in case alone are one file."),
        new("core.autocrlf", "Line ends converted by git itself on the way in and out."),
        new("core.eol", "Which line ends git itself writes into the work tree."),
        new("core.safecrlf", "Whether git refuses a conversion of line ends that cannot be undone."),
        new("core.precomposeunicode", "How accented file names are compared, on a Mac."),
        new("core.longpaths", "Whether Git for Windows uses paths over 260 characters."),
        new("core.fscache", "A cache of folder reads inside Git for Windows."),
        new("core.untrackedcache", "A cache of the untracked files, kept in the index."),
        new("core.preloadindex", "The index compared with the disk on several threads."),
        new("core.sparsecheckout", "Whether only some tracked files are written to the work tree: a list of paths."),
        new("core.sparsecheckoutcone", "The form of that list of paths."),
        new("core.worktree", "Where a submodule's work tree lies, written by git for each one; git only reads through it, since axGIT writes nothing in a repository inside.", InsideOnly: true),
        new("remote.*.url", "A remote's address: read through git and named in the reminder before axGIT reaches it (AG-5.4a)."),
        new("remote.*.pushurl", "A remote's address for a push: read through git and named in the reminder (AG-5.4a)."),
        new("remote.*.fetch", "Which branches of the remote a fetch brings, and under which names."),
        new("remote.*.tagopt", "Whether a fetch brings the remote's tags."),
        new("remote.*.gh-resolved", "A note of GitHub's gh tool; git does not read it."),
        new("remote.pushdefault", "Which remote a push goes to: axGIT reads it, names that remote's servers and puts it on the command line."),
        new("branch.*.remote", "Which remote a branch follows: axGIT reads it, names that remote's servers and puts it on the command line."),
        new("branch.*.merge", "Which branch of the remote a branch follows."),
        new("branch.*.rebase", "How a pull joins two histories; axGIT pulls only where the branch can be moved forward, which joins none."),
        new("branch.*.pushremote", "Which remote a branch is pushed to: axGIT reads it, names that remote's servers and puts it on the command line."),
        new("branch.*.description", "A text about the branch."),
        new("branch.*.vscode-merge-base", "A note of Visual Studio Code; git does not read it."),
        new("branch.*.github-pr-owner-number", "A note of Visual Studio Code's GitHub extension; git does not read it."),
        new("branch.*.gh-merge-base", "A note of GitHub's gh tool; git does not read it."),
        new("push.default", "Which branches a push sends to the remote the reminder named."),
        new("pull.rebase", "How a pull joins two histories; axGIT pulls only where the branch can be moved forward, which joins none."),
        new("pull.ff", "Whether a pull may make a merge commit; axGIT pulls only where the branch can be moved forward."),
        new("user.name", "The name written into a commit."),
        new("user.email", "The address written into a commit."),
        new("init.defaultbranch", "The first branch's name in a new repository."),
        new("submodule.*.url", "A submodule's address, used by git submodule alone, which axGIT never runs; its fetch, pull and push stay out of submodules."),
        new("submodule.*.active", "Whether git submodule works on a submodule; axGIT never runs it."),
        new("extensions.worktreeconfig", "That the work tree has a settings file of its own, which is read here like the repository's."),
        new("extensions.objectformat", "The hash the repository's objects are named by."),
        new("extensions.refstorage", "The format the branches' places are stored in."),
        new("lfs.repositoryformatversion", "The format of Git LFS's own folder, a number it checks."),
        new("lfs.*.access", "How Git LFS signs in to the server the name itself holds, written by Git LFS after a transfer."),
    ];

    private static readonly Dictionary<string, InertSetting> InertByName = Inert.ToDictionary(setting => setting.Name, StringComparer.Ordinal);

    /// <summary>
    /// A setting's name in the form of the table: the section and the variable in lower case, and a star for a
    /// middle part ("remote.*.url" from remote.Origin.URL). The middle part is what lies between the first dot
    /// and the last, as git reads a name, and may hold dots itself.
    /// </summary>
    public static string Shape(string name)
    {
        name = name.Trim();
        var first = name.IndexOf('.');
        var last = name.LastIndexOf('.');
        if (first < 0)
        {
            return name.ToLowerInvariant();
        }

        // An empty middle part ("remote..url") is no name of the table's: given back as it is.
        if (last == first + 1)
        {
            return name.ToLowerInvariant();
        }

        var section = name[..first].ToLowerInvariant();
        var variable = name[(last + 1)..].ToLowerInvariant();
        return first == last ? $"{section}.{variable}" : $"{section}.*.{variable}";
    }

    /// <summary>Whether a setting is known to be harmless (<see cref="Inert"/>); <paramref name="inside"/>: in a repository inside the one opened.</summary>
    public static bool IsInert(string name, bool inside = false) =>
        InertByName.TryGetValue(Shape(name), out var setting) && (inside || !setting.InsideOnly);

    // ---- What runs a program, and what is set to nothing by its name ----

    /// <summary>Whether a configuration name makes git run a program: said so in the notice, and set to nothing by its name.</summary>
    public static bool RunsAProgram(string name) => ProgramNames().IsMatch(name.Trim());

    /// <summary>Whether a configuration name sends git's connection through a proxy: set to nothing by its name while the repository is not trusted (AG-5.4b).</summary>
    public static bool NamesAProxy(string name) => ProxyNames().IsMatch(name.Trim());

    /// <summary>What a risk that is a hook starts with: "hook pre-commit".</summary>
    private const string HookMark = "hook ";

    /// <summary>
    /// The risks that are a program git would run, for the notice: a hook, or a setting whose name runs one
    /// (<see cref="RunsAProgram"/>). The rest (<see cref="NotKnown"/>) are settings axGIT does not know to be harmless.
    /// </summary>
    public static IReadOnlyList<string> Programs(IReadOnlyList<string> risks) => [.. risks.Where(IsProgram)];

    /// <summary>The risks that are no program by their name: a setting that is not in <see cref="Inert"/>, or a repository inside whose settings could not be read.</summary>
    public static IReadOnlyList<string> NotKnown(IReadOnlyList<string> risks) => [.. risks.Where(risk => !IsProgram(risk))];

    private static bool IsProgram(string risk) =>
        Own(risk) is var own && (own.StartsWith(HookMark, StringComparison.Ordinal) || RunsAProgram(own));

    /// <summary>
    /// What in a repository is a risk (AG-1.6c): every setting the repository itself sets
    /// (<paramref name="config"/>, git's answer to <see cref="ConfigArguments"/>) whose name is not known to be
    /// harmless (<see cref="IsInert"/>), and "hook name" for each hook in <paramref name="hooksFolder"/> that is
    /// not one of git's own samples. <paramref name="inside"/>: the repository is one inside the one opened.
    /// </summary>
    public static IReadOnlyList<string> Risks(string config, string? hooksFolder, bool inside = false)
    {
        // Each once, by its exact text: a name's middle part ("pdf" in diff.pdf.textconv) is another name in other case.
        var risks = OwnNames(config)
            .Where(name => !IsInert(name, inside))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (hooksFolder is not null && Directory.Exists(hooksFolder))
        {
            risks.AddRange(Directory.EnumerateFiles(hooksFolder)
                .Select(Path.GetFileName)
                .Where(name => name is not null && !name.EndsWith(".sample", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(name => HookMark + name));
        }

        return risks;
    }

    /// <summary>The parts of a filter's settings that name its programs.</summary>
    private static readonly string[] FilterPrograms = ["clean", "smudge", "process"];

    /// <summary>
    /// The filters git knows here, from its answer to <see cref="ConfigArguments"/>: the middle part of every
    /// <c>filter.name.clean</c>, <c>smudge</c> and <c>process</c>, of every scope, the user's and the computer's
    /// too (Git LFS is set up there), each once by its exact text.
    /// </summary>
    public static IReadOnlyList<string> Filters(string output)
    {
        var filters = new List<string>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var name = line[(line.IndexOf('\t') + 1)..].Trim();
            var first = name.IndexOf('.');
            var last = name.LastIndexOf('.');
            if (first > 0 && last > first + 1 && name[..first].Equals("filter", StringComparison.OrdinalIgnoreCase)
                && FilterPrograms.Contains(name[(last + 1)..], StringComparer.OrdinalIgnoreCase)
                && name[(first + 1)..last] is var filter && !filters.Contains(filter, StringComparer.Ordinal))
            {
                filters.Add(filter);
            }
        }

        return filters;
    }

    /// <summary>
    /// What every run of git gets in a repository that is not trusted: no hooks (<see cref="NoHooks"/>) and no
    /// credential helper (an empty value empties git's list of them, the user's own among them, so a fetch from a
    /// server that needs a sign-in fails until the repository is trusted). The scan's own runs get neither: they
    /// ask git where the hooks folder is, and would be told this one.
    /// </summary>
    public static readonly IReadOnlyList<GitSetting> Careful = [new("credential.helper", string.Empty), new("core.hooksPath", NoHooks)];

    /// <summary>
    /// The settings git is run with while the repository is not trusted (AG-1.6, AG-1.6c), given through the
    /// environment (<see cref="GitRunner.SetOverrides"/>), where a name may hold an equals sign; as <c>-c</c>
    /// arguments, until the review's second pass, <c>filter.a=b.clean=</c> set nothing aside. Empty when
    /// <paramref name="risks"/> is, which is also what a trusted repository runs with.
    /// <list type="bullet">
    /// <item>Every name that runs a program or names a proxy, set to nothing, once, whichever repository it was
    /// found in (the settings reach git's runs inside a submodule, tried with git 2.53).</item>
    /// <item><c>protocol.git.allow=never</c> when <c>core.gitProxy</c> is named: git takes the first proxy command
    /// it reads, the repository's, so an empty one after it sets nothing aside; without the transport none starts.</item>
    /// <item>When a risk is no hook and no such name (a setting axGIT does not know, a repository inside that
    /// could not be read): every filter of <paramref name="filters"/> set to nothing and not required. A filter
    /// is a program of the user's own, and it may obey the repository's settings: Git LFS runs the programs of
    /// <c>lfs.extension.name.clean</c>. No filter runs, so none can.</item>
    /// <item><see cref="Careful"/>: no hooks, no credential helper.</item>
    /// </list>
    /// Automatic maintenance, which runs <c>gc.recentObjectsHook</c>, is off for every run (<see cref="GitRunner.Command"/>).
    /// </summary>
    public static IReadOnlyList<GitSetting> Overrides(IReadOnlyList<string> risks, IReadOnlyList<string> filters)
    {
        if (risks.Count == 0)
        {
            return [];
        }

        var list = new List<GitSetting>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var gitProxy = false;
        var notKnown = false;
        foreach (var risk in risks.Select(Own))
        {
            if (risk.StartsWith(HookMark, StringComparison.Ordinal))
            {
                continue;
            }

            if (!RunsAProgram(risk) && !NamesAProxy(risk))
            {
                notKnown = true;
                continue;
            }

            gitProxy |= risk.Equals("core.gitproxy", StringComparison.OrdinalIgnoreCase);
            // The hooks folder is set below, for every repository that is not trusted.
            if (!risk.Equals("core.hookspath", StringComparison.OrdinalIgnoreCase) && names.Add(risk))
            {
                list.Add(new GitSetting(risk, string.Empty));
            }
        }

        if (gitProxy)
        {
            list.Add(new GitSetting("protocol.git.allow", "never"));
        }

        if (notKnown)
        {
            foreach (var filter in filters.Distinct(StringComparer.Ordinal))
            {
                list.AddRange(FilterPrograms.Select(program => new GitSetting($"filter.{filter}.{program}", string.Empty)));
                list.Add(new GitSetting($"filter.{filter}.required", "false"));
            }
        }

        list.AddRange(Careful);
        return list;
    }

    /// <summary>
    /// The mark of what a repository would run (AG-1.6, repairs.md R-53): the SHA-256, as hexadecimal, of the risks in
    /// lower case and sorted, each with its length in front. A trust is kept with it, so that a repository that gains
    /// or loses a hook, a configured program or a setting axGIT does not know is asked about again. It marks the
    /// names, not what a hook's file or a setting holds.
    /// </summary>
    public static string Mark(IReadOnlyList<string> risks) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            AxKit.Core.SettingsSeal.Text(risks.Select(risk => risk.ToLowerInvariant()).Order(StringComparer.Ordinal)))));

    /// <summary>
    /// The names known to run a program when one of the commands axGIT runs needs it: the core programs; the
    /// external diff; diff, merge and filter drivers and the tools' commands; a pager for one command; credential
    /// helpers; the signing programs and the command that finds an ssh signing key; the editors; a commit
    /// trailer's command; a hook set in the configuration; the remote's own programs and its helper
    /// (<c>vcs</c>). Since the review's second pass this list decides no longer whether a repository is asked
    /// about (every name outside <see cref="Inert"/> is): it says which risks the notice calls programs, and which
    /// are set to nothing by their name.
    /// </summary>
    [GeneratedRegex(@"^(core\.(fsmonitor|hookspath|sshcommand|pager|editor|askpass|gitproxy|alternaterefscommand)|diff\.external|(diff|merge|filter|difftool|mergetool)\..+\.(command|textconv|driver|clean|smudge|process|cmd|path)|pager\..+|credential(\..+)?\.helper|gpg(\..+)?\.program|gpg\.ssh\.defaultkeycommand|sequence\.editor|trailer\..+\.(command|cmd)|hook\..+\.command|remote\..+\.(uploadpack|receivepack|proxy|vcs)|uploadpack\.packobjectshook)$", RegexOptions.IgnoreCase)]
    private static partial Regex ProgramNames();

    /// <summary>A proxy for http and https, for every address or for those under one (<c>http.proxy</c>, <c>http.address.proxy</c>); a remote's own, <c>remote.name.proxy</c>, is among the program names.</summary>
    [GeneratedRegex(@"^http(\..+)?\.proxy$", RegexOptions.IgnoreCase)]
    private static partial Regex ProxyNames();
}
