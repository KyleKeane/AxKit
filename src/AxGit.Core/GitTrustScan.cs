namespace AxGit.Core;

/// <summary>
/// What the reads of <see cref="GitTrustScan"/> gave: what in the repository is a risk (<see cref="GitTrust.Risks"/>,
/// those of the repositories inside it named with them), the filters git knows there
/// (<see cref="GitTrust.Filters"/>, for <see cref="GitTrust.Overrides"/>), or, in <paramref name="Problem"/>, why
/// that could not be read. With a problem nothing is known, and no command may run in the repository.
/// <paramref name="Elsewhere"/>: the computer, other than the one the repository lies on, that its own files, or
/// those of a repository inside it, point git at (<see cref="GitTrust.PointsElsewhere"/>, D9); then nothing more
/// was read, and nothing may run either. <paramref name="Hooks"/>: the hooks folder git names for the repository
/// opened, as a full path.
/// </summary>
public sealed record TrustScan(IReadOnlyList<string> Risks, IReadOnlyList<string> Filters, string? Problem, string? Elsewhere = null, string? Hooks = null)
{
    /// <summary>Whether what the repository would run is known: no problem, and nothing on another computer.</summary>
    public bool Known => Problem is null && Elsewhere is null;
}

/// <summary>
/// The reads that say what a repository would run (SPEC.md AG-1.6), before any other command runs in it and again
/// before every later read of its lists (AG-1.6b): its own settings and hooks, then the repositories inside it,
/// each one's own settings and hooks, since git runs itself inside a submodule and obeys what it finds there. Four
/// kinds of git command and no other, none of which runs a program a repository names: <c>config --list</c> reads
/// files, <c>rev-parse --git-path</c> prints two paths, <c>ls-files --stage</c> reads the index and
/// <c>ls-files --others</c> the names in the work tree. They run with nothing set aside, since what is asked is
/// what the repository itself says (the hooks folder among it). The rules are <see cref="GitTrust"/>'s, where they
/// are tested; this is their order, tested against git itself where git is installed.
///
/// A repository inside is found three ways, and looked at once: an entry of the index that is another repository
/// and has its work tree here; a folder with a repository of its own that the index does not hold yet (the first
/// Stage all makes it an entry); and a repository git keeps under <c>.git/modules</c>, with or without a work
/// tree. One inside another is followed <see cref="GitTrust.Deep"/> levels down. A read that fails for the
/// repository opened is the scan's problem; one that fails for a repository inside (git itself cannot start
/// there), and a repository deeper than the scan follows, names that submodule with <see cref="GitTrust.NotRead"/>,
/// so that the repository is asked about and never taken as running nothing.
///
/// All of it runs off the caller's thread (root CLAUDE.md rule 3; the review's second pass of 2026-11-27: the
/// folder reads between the git runs were on the window's thread, and 200 submodules took 18 seconds there), with
/// <see cref="AtOnce"/> git runs at a time.
/// </summary>
public static class GitTrustScan
{
    /// <summary>How many of the scan's git runs go at once.</summary>
    public const int AtOnce = 4;

    /// <summary>
    /// The scan, on a task of its own from its first step: the caller's thread waits for nothing of it.
    /// <paramref name="environment"/>: variables for git's runs, given to each run (the tests' own settings files).
    /// </summary>
    public static Task<TrustScan> ReadAsync(string root, CancellationToken cancellation = default, IReadOnlyDictionary<string, string>? environment = null) =>
        Task.Run(() => ReadOnTaskAsync(root, cancellation, environment), cancellation);

    /// <summary>
    /// The question for the hooks folder runs with nothing given (git would name the folder given); every other
    /// run of the scan gets the empty hooks folder of an untrusted repository (L4), though none of the four
    /// commands runs a hook.
    /// </summary>
    private static bool AsksForHooks(IReadOnlyList<string> arguments) => ReferenceEquals(arguments, GitTrust.FoldersArguments);

    private static async Task<TrustScan> ReadOnTaskAsync(string root, CancellationToken cancellation, IReadOnlyDictionary<string, string>? environment)
    {
        // D9: nothing is asked of git while the repository's own files point it at another computer.
        if (GitTrust.PointsElsewhere(root) is { } away)
        {
            return new TrustScan([], [], null, away);
        }

        var first = await RunAsync(
            [(root, GitTrust.ConfigArguments), (root, GitTrust.FoldersArguments), (root, GitTrust.LinksArguments), (root, GitTrust.OthersArguments), (root, GitTrust.ValueArguments)],
            cancellation, environment);
        if (first.Take(4).FirstOrDefault(result => !result.Ok) is { } failed)
        {
            return new TrustScan([], [], failed.FirstLine);
        }

        if (ValuesOf(first[4]) is not { } values)
        {
            return new TrustScan([], [], first[4].FirstLine);
        }

        var (hooks, modules) = GitTrust.Folders(first[1].Output, root);
        var risks = new List<string>(GitTrust.Risks(first[0].Output, hooks));
        risks.AddRange(GitTrust.ValueRisks(values).Where(risk => !risk.StartsWith("core.worktree ", StringComparison.Ordinal)));
        var filters = new List<string>(GitTrust.Filters(first[0].Output));

        // The repositories inside, each by its own folder, once.
        var inside = new List<(string Name, string Folder)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (modules is not null)
        {
            var (found, tooDeep) = GitTrust.ModuleFolders(modules);
            foreach (var folder in found.Where(seen.Add))
            {
                inside.Add((Path.GetRelativePath(modules, folder).Replace('\\', '/'), folder));
            }

            foreach (var folder in tooDeep)
            {
                risks.AddRange(GitTrust.Inside(Path.GetRelativePath(modules, folder).Replace('\\', '/'), [GitTrust.NotRead]));
            }
        }

        // The work trees inside, level by level: those of the repository opened, then those inside each of them.
        var looked = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetFullPath(root) };
        var level = Trees(root, string.Empty, first[2].Output, first[3].Output);
        for (var depth = 1; level.Count > 0; depth++)
        {
            var fresh = new List<(string Path, string Name)>();
            foreach (var tree in level)
            {
                if (GitTrust.GitFolderOf(tree.Path) is not { } folder)
                {
                    continue;
                }

                if (seen.Add(folder))
                {
                    inside.Add((tree.Name, folder));
                }

                if (looked.Add(tree.Path))
                {
                    fresh.Add(tree);
                }
            }

            level = [];
            if (depth >= GitTrust.Deep)
            {
                // Deeper than the scan follows: named, never taken as holding nothing.
                risks.AddRange(fresh.SelectMany(tree => GitTrust.Inside(tree.Name, [GitTrust.NotRead])));
                break;
            }

            if (fresh.Select(tree => GitTrust.PointsElsewhere(tree.Path)).FirstOrDefault(host => host is not null) is { } elsewhere)
            {
                return new TrustScan([], [], null, elsewhere);
            }

            var reads = await RunAsync([.. fresh.SelectMany(tree => new[] { (tree.Path, GitTrust.LinksArguments), (tree.Path, GitTrust.OthersArguments) })], cancellation, environment);
            for (var at = 0; at < fresh.Count; at++)
            {
                var (links, others) = (reads[2 * at], reads[(2 * at) + 1]);
                if (links.Ok && others.Ok)
                {
                    level.AddRange(Trees(fresh[at].Path, fresh[at].Name, links.Output, others.Output));
                }
                else
                {
                    risks.AddRange(GitTrust.Inside(fresh[at].Name, [GitTrust.NotRead]));
                }
            }
        }

        if (inside.Select(one => GitTrust.PointsElsewhere(one.Folder)).FirstOrDefault(host => host is not null) is { } away2)
        {
            return new TrustScan([], [], null, away2);
        }

        // Each repository inside: its settings, the hooks folder git names for it (L3: a linked work tree's hooks
        // are in its common folder, and core.hooksPath names another), and the values that decide.
        var owns = await RunAsync([.. inside.SelectMany(one => new[] { (one.Folder, GitTrust.ConfigArguments), (one.Folder, GitTrust.FoldersArguments), (one.Folder, GitTrust.ValueArguments) })], cancellation, environment);
        for (var at = 0; at < inside.Count; at++)
        {
            var (name, folder) = inside[at];
            var (config, folders, valueRun) = (owns[3 * at], owns[(3 * at) + 1], owns[(3 * at) + 2]);
            if (!config.Ok || !folders.Ok || ValuesOf(valueRun) is not { } insideValues)
            {
                risks.AddRange(GitTrust.Inside(name, [GitTrust.NotRead]));
                continue;
            }

            risks.AddRange(GitTrust.Inside(name, [.. GitTrust.Risks(config.Output, GitTrust.Folders(folders.Output, folder).Hooks, inside: true), .. GitTrust.ValueRisks(insideValues)]));
            filters.AddRange(GitTrust.Filters(config.Output));
        }

        return new TrustScan([.. risks.Distinct(StringComparer.Ordinal)], [.. filters.Distinct(StringComparer.Ordinal)], null, Hooks: hooks);
    }

    /// <summary>The output of <see cref="GitTrust.ValueArguments"/>: what git printed, empty for exit code 1 with nothing printed (none of the names set), null when git failed otherwise.</summary>
    private static string? ValuesOf(GitResult result) =>
        result.Ok ? result.Output
        : result is { ExitCode: 1, TimedOut: false } && result.Output.Length == 0 && result.Error.Trim().Length == 0 ? string.Empty
        : null;

    /// <summary>
    /// The folders inside a work tree that may be a repository of their own: the index's entries that are one
    /// (<see cref="GitTrust.Links"/>) and the folders git does not go into because they are one
    /// (<see cref="GitTrust.Nested"/>), each with its full path and its name from the repository opened.
    /// </summary>
    private static List<(string Path, string Name)> Trees(string tree, string name, string links, string others) =>
        [.. GitTrust.Links(links).Concat(GitTrust.Nested(others)).Select(inside => (
            Path.GetFullPath(Path.Combine(tree, inside.Replace('/', Path.DirectorySeparatorChar))),
            name.Length == 0 ? inside : name + "/" + inside))];

    /// <summary>The git runs given, <see cref="AtOnce"/> at a time, their results in the order asked.</summary>
    private static async Task<IReadOnlyList<GitResult>> RunAsync(IReadOnlyList<(string Folder, IReadOnlyList<string> Arguments)> runs, CancellationToken cancellation, IReadOnlyDictionary<string, string>? environment)
    {
        var results = new List<GitResult>(runs.Count);
        foreach (var some in runs.Chunk(AtOnce))
        {
            results.AddRange(await Task.WhenAll(some.Select(run => GitRunner.RunAsync(
                run.Folder, run.Arguments, GitRunner.Short, cancellation,
                AsksForHooks(run.Arguments) ? null : [new GitSetting("core.hooksPath", GitTrust.EmptyHooksFolder())],
                environment))));
        }

        return results;
    }
}
