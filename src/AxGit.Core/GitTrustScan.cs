namespace AxGit.Core;

/// <summary>
/// What the reads of <see cref="GitTrustScan"/> gave: what the repository would run (<see cref="GitTrust.Risks"/>,
/// those of the repositories inside it named with them), or, in <paramref name="Problem"/>, why that could not be
/// read. With a problem nothing is known, and no command may run in the repository.
/// </summary>
public sealed record TrustScan(IReadOnlyList<string> Risks, string? Problem);

/// <summary>
/// The reads that say what a repository would run (SPEC.md AG-1.6), before any other command runs in it: its own
/// settings and hooks, then the repositories inside it, each submodule's own settings and hooks, since git runs
/// itself inside a submodule and obeys what it finds there. Four kinds of git command and no other, none of which
/// runs a program a repository names: <c>config --list</c> reads files, <c>rev-parse --git-path</c> prints two
/// paths, <c>ls-files --stage</c> reads the index. The rules are <see cref="GitTrust"/>'s, where they are tested;
/// this is their order, tested against git itself where git is installed.
///
/// A repository inside is found two ways, and looked at once: an entry of the index that is another repository and
/// has its work tree here (also one inside another, to any depth), and a repository git keeps under
/// <c>.git/modules</c>, with or without a work tree. A read that fails for the repository opened is the scan's
/// problem; one that fails for a repository inside (git itself cannot start there) names that submodule with
/// <see cref="GitTrust.NotRead"/>, so that the repository is asked about and never taken as running nothing.
/// </summary>
public static class GitTrustScan
{
    public static async Task<TrustScan> ReadAsync(string root, CancellationToken cancellation = default)
    {
        var config = await GitRunner.RunAsync(root, GitTrust.ConfigArguments, GitRunner.Short, cancellation);
        if (!config.Ok)
        {
            return new TrustScan([], config.FirstLine);
        }

        var folders = await GitRunner.RunAsync(root, GitTrust.FoldersArguments, GitRunner.Short, cancellation);
        if (!folders.Ok)
        {
            return new TrustScan([], folders.FirstLine);
        }

        var (hooks, modules) = GitTrust.Folders(folders.Output, root);
        var risks = new List<string>(GitTrust.Risks(config.Output, hooks));

        // The repositories inside, each by its own folder, once.
        var inside = new List<(string Name, string Folder)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (modules is not null)
        {
            foreach (var folder in GitTrust.ModuleFolders(modules))
            {
                if (seen.Add(folder))
                {
                    inside.Add((Path.GetRelativePath(modules, folder).Replace('\\', '/'), folder));
                }
            }
        }

        var trees = new Queue<(string Path, string Name)>();
        var looked = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetFullPath(root) };
        trees.Enqueue((root, string.Empty));
        while (trees.TryDequeue(out var tree))
        {
            var links = await GitRunner.RunAsync(tree.Path, GitTrust.LinksArguments, GitRunner.Short, cancellation);
            if (!links.Ok)
            {
                if (tree.Name.Length == 0)
                {
                    return new TrustScan([], links.FirstLine);
                }

                risks.AddRange(GitTrust.Inside(tree.Name, [GitTrust.NotRead]));
                continue;
            }

            foreach (var link in GitTrust.Links(links.Output))
            {
                var path = Path.GetFullPath(Path.Combine(tree.Path, link.Replace('/', Path.DirectorySeparatorChar)));
                if (GitTrust.GitFolderOf(path) is not { } folder)
                {
                    continue;
                }

                var name = tree.Name.Length == 0 ? link : tree.Name + "/" + link;
                if (seen.Add(folder))
                {
                    inside.Add((name, folder));
                }

                if (looked.Add(path))
                {
                    trees.Enqueue((path, name));
                }
            }
        }

        foreach (var (name, folder) in inside)
        {
            var own = await GitRunner.RunAsync(folder, GitTrust.ConfigArguments, GitRunner.Short, cancellation);
            risks.AddRange(GitTrust.Inside(name, own.Ok ? GitTrust.Risks(own.Output, Path.Combine(folder, "hooks")) : [GitTrust.NotRead]));
        }

        return new TrustScan([.. risks.Distinct(StringComparer.Ordinal)], null);
    }
}
