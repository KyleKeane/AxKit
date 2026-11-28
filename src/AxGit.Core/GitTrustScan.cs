namespace AxGit.Core;

/// <summary>
/// What the reads of <see cref="GitTrustScan"/> gave: what in the repository is a risk (<see cref="GitTrust"/>'s
/// rules; those of the repositories inside it named with them), or, in <paramref name="Problem"/>, why its files
/// could not be read, with which nothing may run. <paramref name="Mark"/>: the mark of what it would run
/// (<see cref="GitTrust.Mark"/>), which a trust is kept with. <paramref name="Hooks"/>: every folder git may take for
/// the hooks folder of the repository opened (its own or the default one, and each one an included file names), as
/// full paths. <paramref name="InTree"/>: the settings whose program, or hooks folder, lies in the work tree, where a
/// switch or a pull can change it while git runs (T4); a changing command runs with them set aside
/// (<see cref="GitTrust.Overrides"/>). <paramref name="GitDir"/>: the git folder that was read for the repository
/// opened, which every run of git is given (P1). <paramref name="Programs"/>: each setting that names a program, with
/// its whole value, "core.sshcommand: ssh -v", for the notice (P5).
/// </summary>
public sealed record TrustScan(IReadOnlyList<string> Risks, string? Problem, string Mark = "", IReadOnlyList<string>? Hooks = null, IReadOnlyList<string>? InTree = null, string? GitDir = null, IReadOnlyList<string>? Programs = null)
{
    /// <summary>Whether what the repository would run is known: its files could be read.</summary>
    public bool Known => Problem is null;
}

/// <summary>
/// The reads that say what a repository would run (SPEC.md AG-1.6), before any git command runs in it, before and
/// after every command that changes it, and at F5 (AG-1.6b). No git command runs in the repository for it (the
/// review's fourth pass, T1): axKit's own code reads the files, each through <see cref="RepositoryFiles"/> (P2 of the
/// fifth pass: only in a positive local form), and git is run only as a parser of one settings file given by its
/// path (<see cref="GitTrust.ParserArguments"/>), with no repository (<c>GIT_DIR</c> a folder that does not exist),
/// which runs nothing.
///
/// What is read, of the repository opened and of every repository inside it:
/// <list type="bullet">
/// <item>its <c>.git</c> entry: a folder, or a file with the folder it names, which must be a repository by git's
/// own rule (<see cref="IsGitDirectory"/>: a HEAD that is a reference or an object id, a commondir that names a folder
/// there, objects and refs); for the repository opened anything else is the scan's problem, since git, run there,
/// would work on another repository or none (P1); a link there, or a folder outside the work tree, is a risk;</item>
/// <item>the <c>commondir</c> file of a linked work tree, the same way;</item>
/// <item>every entry of the <c>.git</c> folder for a link (never followed), the alternates, and the legacy
/// <c>remotes</c> and <c>branches</c> files git still obeys;</item>
/// <item>the settings files of the repository's own scopes (<c>config</c>, <c>config.worktree</c>) through the
/// parser, each setting's name and value, and the files its includes name, each relative one from the folder of the
/// file that names it (for the mark, and for what they set: an include is a risk by its name);</item>
/// <item>the hooks folders: the default one, or the one <c>core.hooksPath</c> names, and each one an included file
/// names;</item>
/// <item>the files the other settings name, for the mark and for <see cref="TrustScan.InTree"/>;</item>
/// <item>the index, for its gitlinks (<see cref="GitIndex"/>).</item>
/// </list>
/// The repositories inside are those git enters (P4 of the fifth pass; tried with git 2.53 on every command axGIT
/// has): the gitlinks of each repository's index and the paths its <c>.gitmodules</c> names, each followed into its
/// own index, <see cref="GitTrust.Deep"/> levels down; and those git keeps under <c>.git/modules</c>
/// (<see cref="ModuleFolders"/>). An untracked repository, and one in an ignored folder, git never enters; once
/// Stage all has made one a gitlink, the check after it finds it. Until the fifth pass the whole work tree was walked
/// for <c>.git</c> entries.
///
/// All of it runs off the caller's thread (root CLAUDE.md rule 3), with <see cref="AtOnce"/> repositories inside
/// read at a time.
/// </summary>
public static class GitTrustScan
{
    /// <summary>How many repositories inside are read at once.</summary>
    public const int AtOnce = 4;

    /// <summary>The scan, on a task of its own from its first step: the caller's thread waits for nothing of it. <paramref name="environment"/>: variables for the parser's runs (the tests' settings files).</summary>
    public static Task<TrustScan> ReadAsync(string root, CancellationToken cancellation = default, IReadOnlyDictionary<string, string>? environment = null) =>
        Task.Run(() => ReadOnTaskAsync(Path.GetFullPath(root), cancellation, environment), cancellation);

    /// <summary>
    /// Every git command the scans of this process ran, in order, for the class test (<c>GitTrustScanTests</c>):
    /// each one's argument list as given to <see cref="GitRunner"/>. Kept while <see cref="Record"/> is set.
    /// </summary>
    public static List<IReadOnlyList<string>>? Record { get; set; }

    private static readonly object RecordGate = new();

    /// <summary>
    /// The folder of the repository that holds <paramref name="path"/> (a folder, or a file whose folder is taken),
    /// found going up: the first folder with a <c>.git</c> entry. A <c>.git</c> folder counts only when it is a
    /// repository by git's own rule (<see cref="IsGitDirectory"/>); one that is not ends the look with none, since git
    /// would go on up to another repository, which was not read (P1 of the fifth pass: with a HEAD that was no
    /// reference, git ran the hooks of the repository above). A <c>.git</c> file or link is the repository's entry,
    /// which the scan reads or refuses. Null also when the path lies inside a repository's own folder or a bare
    /// repository, which have no work tree (AG-1.2). Read in C#, a link's own attributes and never what it points at.
    /// </summary>
    public static string? RootOf(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            var start = RepositoryFiles.Attributes(full, full) is { } kind && kind.HasFlag(FileAttributes.Directory) ? full : Path.GetDirectoryName(full);
            for (var at = start; at is not null; at = Path.GetDirectoryName(at))
            {
                var dot = Path.Combine(at, ".git");
                if (RepositoryFiles.Attributes(dot, at) is { } entry)
                {
                    return entry.HasFlag(FileAttributes.ReparsePoint) || !entry.HasFlag(FileAttributes.Directory) || IsGitDirectory(dot, at) ? at : null;
                }

                if (IsGitDirectory(at, at))
                {
                    return null;
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            // A path git could not use either.
        }

        return null;
    }

    /// <summary>
    /// Whether git takes <paramref name="folder"/> for a repository (git's own rule, setup.c): its HEAD is a
    /// reference (<c>ref: refs/…</c>) or an object id; its common folder, the one its <c>commondir</c> file names or
    /// itself, holds <c>objects</c> and <c>refs</c>. Every file read through <see cref="RepositoryFiles"/>, from
    /// <paramref name="root"/>: a commondir that names a place axGIT does not read is no repository here.
    /// </summary>
    public static bool IsGitDirectory(string folder, string root)
    {
        if (RepositoryFiles.Text(Path.Combine(folder, "HEAD"), root, 256) is not { } head || !IsHead(head.Text))
        {
            return false;
        }

        var common = folder;
        if (RepositoryFiles.Attributes(Path.Combine(folder, "commondir"), root) is not null)
        {
            if (CommonOf(folder, root) is not { } named)
            {
                return false;
            }

            common = named;
        }

        return RepositoryFiles.Attributes(Path.Combine(common, "objects"), root) is not null && RepositoryFiles.Attributes(Path.Combine(common, "refs"), root) is not null;
    }

    /// <summary>The folder a commondir file names, as a full path, when it is a folder that exists and may be read; null otherwise.</summary>
    private static string? CommonOf(string folder, string root)
    {
        if (RepositoryFiles.Text(Path.Combine(folder, "commondir"), root) is not { } text || text.Text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n')[0].Trim() is not { Length: > 0 } named)
        {
            return null;
        }

        try
        {
            var full = Path.GetFullPath(named, folder);
            return RepositoryFiles.Place(full, root) is not null && RepositoryFiles.Attributes(full, root) is { } kind && kind.HasFlag(FileAttributes.Directory) ? full : null;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>A HEAD git reads as one: "ref:" and a name under refs/, or an object id of 40 or 64 hexadecimal digits.</summary>
    private static bool IsHead(string text)
    {
        if (text.StartsWith("ref:", StringComparison.Ordinal))
        {
            return text[4..].TrimStart().StartsWith("refs/", StringComparison.Ordinal);
        }

        // git reads the first 40 or 64 digits and nothing after them.
        return text.TakeWhile(char.IsAsciiHexDigit).Count() >= 40;
    }

    /// <summary>What one scan keeps while it reads: the repository's root, the computer it lies on, and what was found.</summary>
    private sealed class Reading(string root, string? computer, IReadOnlyDictionary<string, string>? environment, CancellationToken cancellation)
    {
        /// <summary>The repositories inside are read side by side, and each adds to the lists here.</summary>
        private readonly object _gate = new();

        public string Root { get; } = root;

        public string? Computer { get; } = computer;

        public IReadOnlyDictionary<string, string>? Environment { get; } = environment;

        public CancellationToken Cancellation { get; } = cancellation;

        public List<string> Risks { get; } = [];

        public List<string> Lines { get; } = [];

        public List<string> InTree { get; } = [];

        public List<string> Programs { get; } = [];

        public string? Problem { get; set; }

        public IReadOnlyList<string>? Hooks { get; set; }

        public void Add(string name, IEnumerable<string> risks)
        {
            lock (_gate)
            {
                Risks.AddRange(name.Length == 0 ? risks : GitTrust.Inside(name, risks));
            }
        }

        public void Line(string line)
        {
            lock (_gate)
            {
                Lines.Add(line);
            }
        }

        public void Tree(string name, string setting)
        {
            lock (_gate)
            {
                InTree.Add(name.Length == 0 ? setting : GitTrust.Inside(name, [setting])[0]);
            }
        }

        public void Program(string name, string line)
        {
            lock (_gate)
            {
                Programs.Add(name.Length == 0 ? line : GitTrust.Inside(name, [line])[0]);
            }
        }

        public string? Elsewhere(string value) => GitTrust.ElsewhereOf(value, Computer, RepositoryFiles.IsNetworkDrive);

        /// <summary>Whether a full path lies in the work tree: under the root, and in no <c>.git</c> folder (git writes none of those on a checkout).</summary>
        public bool InWorkTree(string path)
        {
            var relative = Path.GetRelativePath(Root, path);
            return relative != "." && !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative)
                && !relative.Split(Path.DirectorySeparatorChar).Contains(".git", StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>A place as the notice names it: relative to the root where it lies under it, its full path otherwise.</summary>
        public string Shown(string path)
        {
            var relative = Path.GetRelativePath(Root, path);
            return relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative) ? path : relative.Replace('\\', '/');
        }
    }

    /// <summary>One repository to read: its name from the root (empty for the repository opened), its git folder, its work tree (none for one git keeps without), and how deep it lies.</summary>
    private sealed record Nested(string Name, string GitDir, string? WorkTree, int Depth);

    private static async Task<TrustScan> ReadOnTaskAsync(string root, CancellationToken cancellation, IReadOnlyDictionary<string, string>? environment)
    {
        var reading = new Reading(root, GitTrust.ComputerOf(root, RepositoryFiles.IsNetworkDrive), environment, cancellation);
        if (RepositoryFiles.Attributes(root, root) is null)
        {
            return new TrustScan([], $"{root} is not there.");
        }

        // The repository opened: its .git entry, which must be a repository by git's rule, then everything of it.
        var gitDir = Entry(reading, root, string.Empty);
        if (reading.Problem is not null || gitDir is null)
        {
            return new TrustScan([], reading.Problem ?? $"{root} holds no repository axGIT can read.");
        }

        if (!IsGitDirectory(gitDir, root))
        {
            return new TrustScan([], $"Its git folder {gitDir} is not a repository as git reads one: git would work on another repository or on none.");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { gitDir };
        var (common, inside) = await RepositoryAsync(reading, new Nested(string.Empty, gitDir, root, 0));
        if (reading.Problem is not null)
        {
            return new TrustScan([], reading.Problem);
        }

        // The repositories git keeps under .git/modules, with or without a work tree.
        var queue = new List<Nested>(inside.Where(one => seen.Add(one.GitDir)));
        if (common is not null)
        {
            var modules = Path.Combine(common, "modules");
            if (RepositoryFiles.Attributes(modules, root) is { } kind && !kind.HasFlag(FileAttributes.ReparsePoint))
            {
                var (found, tooDeep) = ModuleFolders(modules, root);
                queue.AddRange(found.Where(seen.Add).Select(folder => new Nested(Path.GetRelativePath(modules, folder).Replace('\\', '/'), folder, null, 1)));
                foreach (var folder in tooDeep)
                {
                    reading.Add(Path.GetRelativePath(modules, folder).Replace('\\', '/'), [GitTrust.NotRead]);
                }
            }
        }

        // Level by level: each repository inside read, and the gitlinks of its own index followed in their turn.
        while (queue.Count > 0)
        {
            var next = new List<Nested>();
            foreach (var some in queue.Chunk(AtOnce))
            {
                var read = await Task.WhenAll(some.Select(one => RepositoryAsync(reading, one)));
                next.AddRange(read.SelectMany(one => one.Inside).Where(one => seen.Add(one.GitDir)));
            }

            queue = next;
        }

        var risks = reading.Risks.Distinct(StringComparer.Ordinal).ToList();
        var lines = reading.Lines.Concat(risks.Select(risk => "risk " + risk));
        return new TrustScan(risks, null, GitTrust.Mark(lines), reading.Hooks, [.. reading.InTree.Distinct(StringComparer.Ordinal)], gitDir, [.. reading.Programs.Distinct(StringComparer.Ordinal)]);
    }

    /// <summary>
    /// A work tree's <c>.git</c> entry, as the repository folder to read; null when it has none or names none (git
    /// runs nothing there), or when it is a risk read no further: a link, or a folder on another computer or in a
    /// place axGIT does not read. For the repository opened each of these is the scan's problem (P1: git would work
    /// on a folder that was not read).
    /// </summary>
    private static string? Entry(Reading reading, string worktree, string name)
    {
        var top = name.Length == 0;
        void Refuse(string risk)
        {
            if (top)
            {
                reading.Problem = $"{char.ToUpperInvariant(risk[0])}{risk[1..]}, so axGIT does not open it.";
            }
            else
            {
                reading.Add(name, [risk]);
            }
        }

        var dot = Path.Combine(worktree, ".git");
        if (RepositoryFiles.Attributes(dot, reading.Root) is not { } kind)
        {
            if (top)
            {
                reading.Problem = $"{worktree} holds no .git folder or file.";
            }

            return null;
        }

        if (kind.HasFlag(FileAttributes.ReparsePoint))
        {
            Refuse(top ? "its .git is a link, which axGIT does not follow" : GitTrust.Link(reading.Shown(dot)));
            return null;
        }

        if (kind.HasFlag(FileAttributes.Directory))
        {
            return Path.GetFullPath(dot);
        }

        // A .git file: the folder it names, worked out from its text alone.
        if (RepositoryFiles.Text(dot, reading.Root) is not { } read)
        {
            if (top)
            {
                reading.Problem = "Its .git file could not be read.";
            }
            else
            {
                reading.Add(name, [GitTrust.NotRead]);
            }

            return null;
        }

        var text = read.Text.Split('\n')[0].Trim();
        if (!text.StartsWith("gitdir:", StringComparison.Ordinal) || text[7..].Trim() is not { Length: > 0 } named)
        {
            if (top)
            {
                reading.Problem = "Its .git file names no repository.";
            }

            return null;
        }

        if (reading.Elsewhere(named) is { } host)
        {
            Refuse(GitTrust.OnAnotherComputer("its .git file", host));
            return null;
        }

        string? full;
        try
        {
            full = RepositoryFiles.Place(Path.GetFullPath(named, worktree), reading.Root);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            full = null;
        }

        if (full is null)
        {
            Refuse(GitTrust.NotReadPlace("its .git file"));
        }

        return full;
    }

    /// <summary>
    /// One repository's folder read (the list of <see cref="GitTrustScan"/>), its risks named with its name (empty
    /// for the repository opened); gives its common folder, where the modules are, or null when it was not read, and
    /// the repositories inside it that its index and its <c>.gitmodules</c> name. A failure of the repository opened
    /// is the scan's problem; one of a repository inside names it with <see cref="GitTrust.NotRead"/>.
    /// </summary>
    private static async Task<(string? Common, IReadOnlyList<Nested> Inside)> RepositoryAsync(Reading reading, Nested one)
    {
        var (name, gitDir, workTree) = (one.Name, one.GitDir, one.WorkTree);
        var inside = name.Length > 0;
        (string?, IReadOnlyList<Nested>) Fail(string problem)
        {
            if (inside)
            {
                reading.Add(name, [GitTrust.NotRead]);
            }
            else
            {
                reading.Problem = problem;
            }

            return (null, []);
        }

        var risks = new List<string>();
        if (!RepositoryFiles.Under(reading.Root, gitDir))
        {
            risks.Add(GitTrust.OutsideTheWorkTree);
        }

        if (RepositoryFiles.Attributes(gitDir, reading.Root) is not { } kind || !kind.HasFlag(FileAttributes.Directory))
        {
            reading.Add(name, risks);
            return Fail($"Its repository folder {gitDir} is not there.");
        }

        // A linked work tree's common folder, named in its commondir file.
        var common = gitDir;
        if (RepositoryFiles.Text(Path.Combine(gitDir, "commondir"), reading.Root) is { } commonText && commonText.Text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n')[0].Trim() is { Length: > 0 } commonPath)
        {
            if (reading.Elsewhere(commonPath) is { } host)
            {
                reading.Add(name, [.. risks, GitTrust.OnAnotherComputer("its commondir file", host)]);
                return Fail($"Its {GitTrust.OnAnotherComputer("commondir file", host)}, so axGIT does not open it.");
            }

            if (CommonOf(gitDir, reading.Root) is not { } commonFolder)
            {
                reading.Add(name, [.. risks, GitTrust.NotReadPlace("its commondir file")]);
                return Fail("Its commondir file names no folder axGIT reads, so axGIT does not open it.");
            }

            common = commonFolder;
            if (!RepositoryFiles.Under(reading.Root, common) && !risks.Contains(GitTrust.OutsideTheWorkTree))
            {
                risks.Add(GitTrust.OutsideTheWorkTree);
            }
        }

        // Links anywhere in the folder, never followed; the repositories under modules are read as their own.
        foreach (var folder in new[] { gitDir, common }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            risks.AddRange(LinksIn(reading, folder));
        }

        foreach (var folder in new[] { gitDir, common }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (RepositoryFiles.Attributes(Path.Combine(folder, "objects", "info", "alternates"), reading.Root) is not null
                || RepositoryFiles.Attributes(Path.Combine(folder, "objects", "info", "http-alternates"), reading.Root) is not null)
            {
                risks.Add(GitTrust.Alternates);
            }

            foreach (var legacy in new[] { "remotes", "branches" })
            {
                risks.AddRange(FileNames(reading, Path.Combine(folder, legacy)).Select(file => GitTrust.Legacy(legacy, file)));
            }
        }

        // The settings of the repository's own scopes, through git's parser, each file with its includes beside it
        // (L5: a relative include is resolved from the folder of the file that names it).
        var files = new[] { Path.Combine(common, "config"), Path.Combine(gitDir, "config.worktree") };
        var settings = new List<FileSetting>();
        var named = new List<FileSetting>();
        var start = workTree ?? gitDir;
        foreach (var file in files)
        {
            if (RepositoryFiles.Attributes(file, reading.Root) is not { } fileKind || fileKind.HasFlag(FileAttributes.ReparsePoint))
            {
                // Not there, or a link, named above and never read.
                continue;
            }

            if (await ParseAsync(reading, file) is not { } parsed)
            {
                reading.Add(name, risks);
                return Fail($"git could not read its settings file {file}.");
            }

            settings.AddRange(parsed);
            named.AddRange(parsed);
            named.AddRange(await IncludesAsync(reading, name, parsed, Path.GetDirectoryName(file)!, 0));
        }

        risks.AddRange(GitTrust.SettingRisks(settings, inside));
        risks.AddRange(GitTrust.ValueRisks(settings, reading.Computer, RepositoryFiles.IsNetworkDrive));
        reading.Add(name, risks);

        // For the mark: every setting that is not harmless with its value; for the notice, each program with its value (P5).
        foreach (var setting in named.Where(setting => !GitTrust.IsInert(setting.Name, inside)))
        {
            reading.Line($"value {name} {setting.Name}={setting.Value}");
            if (GitTrust.RunsAProgram(setting.Name) && setting.Value is not null)
            {
                reading.Program(name, $"{setting.Name}: {setting.Value}");
            }
        }

        // The hooks folders: core.hooksPath where the repository's own files or the files they include set it, git's
        // own otherwise (L5: an include may set it, also one whose condition holds only on some branches).
        var ownHooks = settings.LastOrDefault(setting => IsHooksPath(setting) && setting.Value is { Length: > 0 })?.Value;
        var folders = new List<string?> { ownHooks is null ? Path.Combine(common, "hooks") : Resolve(ownHooks, start) };
        folders.AddRange(named.Except(settings).Where(setting => IsHooksPath(setting) && setting.Value is { Length: > 0 }).Select(setting => Resolve(setting.Value!, start)));
        var hooksFolders = folders.OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var folder in hooksFolders)
        {
            HooksIn(reading, folder, name);
            if (reading.InWorkTree(folder))
            {
                reading.Tree(name, "core.hookspath");
            }
        }

        if (!inside)
        {
            reading.Hooks = hooksFolders;
        }

        // The files the other settings name: hashed for the mark, and a program in the work tree noted (T4).
        foreach (var setting in named.Where(setting => !GitTrust.IsInert(setting.Name, inside) && setting.Value is not null))
        {
            foreach (var word in GitTrust.PathWords(setting.Value!))
            {
                if (reading.Elsewhere(word) is not null || word.Contains("://", StringComparison.Ordinal) || Resolve(word, start) is not { } path)
                {
                    continue;
                }

                if (RepositoryFiles.Place(path, reading.Root) is null)
                {
                    reading.Add(name, [GitTrust.NotReadPlace(setting.Name)]);
                    reading.Line($"file {path} not read");
                    continue;
                }

                reading.Line($"file {path} {RepositoryFiles.Hash(path, reading.Root)}");
                if (GitTrust.RunsAProgram(setting.Name) && reading.InWorkTree(path))
                {
                    reading.Tree(name, setting.Name);
                }
            }
        }

        // The repositories inside that git enters: the gitlinks of the index and the paths of .gitmodules (P4).
        var found = new List<Nested>();
        if (workTree is not null)
        {
            if (one.Depth >= GitTrust.Deep)
            {
                reading.Add(name, [GitTrust.TooDeep]);
            }
            else
            {
                var paths = Gitlinks(reading, name, gitDir, settings).Concat(await ModulePathsAsync(reading, workTree)).Distinct(StringComparer.Ordinal);
                foreach (var path in paths)
                {
                    var folder = Path.Combine(workTree, path.Replace('/', Path.DirectorySeparatorChar));
                    var shown = reading.Shown(folder);
                    if (RepositoryFiles.Place(folder, reading.Root) is null)
                    {
                        reading.Add(name, [GitTrust.NotReadPlace("submodule " + path)]);
                    }
                    else if (Entry(reading, folder, shown) is { } dir)
                    {
                        found.Add(new Nested(shown, dir, folder, one.Depth + 1));
                    }
                }
            }
        }

        return (common, found);
    }

    private static bool IsHooksPath(FileSetting setting) => setting.Name.Equals("core.hookspath", StringComparison.OrdinalIgnoreCase);

    /// <summary>The gitlinks of a repository's index, and of the shared index a split index names; an index axGIT does not read is a risk (P4).</summary>
    private static IReadOnlyList<string> Gitlinks(Reading reading, string name, string gitDir, IReadOnlyList<FileSetting> settings)
    {
        var hashLength = settings.LastOrDefault(setting => setting.Name.Equals("extensions.objectformat", StringComparison.OrdinalIgnoreCase))?.Value?.Trim().Equals("sha256", StringComparison.OrdinalIgnoreCase) == true ? 32 : 20;
        var links = new List<string>();
        string? IndexOf(string file)
        {
            var path = Path.Combine(gitDir, file);
            if (RepositoryFiles.Attributes(path, reading.Root) is null)
            {
                return file == "index" ? null : "missing";
            }

            if (RepositoryFiles.Bytes(path, reading.Root, GitIndex.Largest) is not { } bytes || GitIndex.Read(bytes, hashLength) is not { Known: true } entries)
            {
                return "unknown";
            }

            links.AddRange(entries.Gitlinks);
            return entries.SharedIndex;
        }

        var shared = IndexOf("index");
        if (shared is "unknown" || (shared is { } hex && IndexOf("sharedindex." + hex) is "unknown" or "missing"))
        {
            reading.Add(name, [GitTrust.IndexNotRead]);
        }

        return links;
    }

    /// <summary>The paths a work tree's <c>.gitmodules</c> names (<c>submodule.&lt;name&gt;.path</c>), read through the parser; none when it has none.</summary>
    private static async Task<IReadOnlyList<string>> ModulePathsAsync(Reading reading, string workTree)
    {
        var file = Path.Combine(workTree, ".gitmodules");
        if (RepositoryFiles.Attributes(file, reading.Root) is not { } kind || kind.HasFlag(FileAttributes.Directory) || kind.HasFlag(FileAttributes.ReparsePoint))
        {
            return [];
        }

        var settings = await ParseAsync(reading, file) ?? [];
        return [.. settings.Where(setting => GitTrust.Shape(setting.Name) == "submodule.*.path" && setting.Value is { Length: > 0 }).Select(setting => setting.Value!.Trim().Replace('\\', '/').Trim('/'))];
    }

    /// <summary>
    /// The settings files the includes of <paramref name="settings"/> name, each hashed for the mark and read
    /// through the parser in its turn, a relative one from <paramref name="folder"/>, the folder of the file that
    /// names it, up to <see cref="GitTrust.Deep"/> levels; a path on another computer or in a place axGIT does not
    /// read is never read (a risk by its value). Every include counts, whatever its condition: git decides that at
    /// each command. Gives what they set, in order.
    /// </summary>
    private static async Task<List<FileSetting>> IncludesAsync(Reading reading, string name, IReadOnlyList<FileSetting> settings, string folder, int depth)
    {
        var more = new List<FileSetting>();
        foreach (var include in settings.Where(setting => setting.Value is { Length: > 0 } && GitTrust.Shape(setting.Name) is "include.path" or "includeif.*.path"))
        {
            var value = include.Value!.Trim();
            if (reading.Elsewhere(value) is not null || value.StartsWith("%(", StringComparison.Ordinal) || Resolve(value, folder) is not { } path)
            {
                reading.Line($"include {value}");
                continue;
            }

            if (RepositoryFiles.Place(path, reading.Root) is null)
            {
                reading.Add(name, [GitTrust.NotReadPlace(include.Name)]);
                reading.Line($"include {value} not read");
                continue;
            }

            reading.Line($"include {path} {RepositoryFiles.Hash(path, reading.Root)}");
            if (depth < GitTrust.Deep && RepositoryFiles.Attributes(path, reading.Root) is { } kind && !kind.HasFlag(FileAttributes.ReparsePoint) && !kind.HasFlag(FileAttributes.Directory)
                && await ParseAsync(reading, path) is { } parsed)
            {
                more.AddRange(parsed);
                more.AddRange(await IncludesAsync(reading, name, parsed, Path.GetDirectoryName(path)!, depth + 1));
            }
        }

        return more;
    }

    /// <summary>A hooks folder: each hook a risk and hashed, the listing marked; a link there named and not followed, a place axGIT does not read named.</summary>
    private static void HooksIn(Reading reading, string folder, string name)
    {
        if (reading.Elsewhere(folder) is not null)
        {
            reading.Line($"hooks {folder} none");
            return;
        }

        if (RepositoryFiles.Place(folder, reading.Root) is null)
        {
            if (RepositoryFiles.Attributes(folder, reading.Root) is { } link && link.HasFlag(FileAttributes.ReparsePoint))
            {
                reading.Add(name, [GitTrust.Link(reading.Shown(folder))]);
            }
            else
            {
                reading.Add(name, [GitTrust.NotReadPlace("core.hookspath")]);
            }

            reading.Line($"hooks {folder} not read");
            return;
        }

        if (RepositoryFiles.Attributes(folder, reading.Root) is not { } kind || !kind.HasFlag(FileAttributes.Directory))
        {
            reading.Line($"hooks {folder} none");
            return;
        }

        var entries = FileNames(reading, folder, files: false);
        reading.Line($"hooks {folder}: {string.Join("/", entries)}");
        foreach (var file in FileNames(reading, folder).Where(GitTrust.IsHook))
        {
            reading.Add(name, [GitTrust.Hook(file)]);
            reading.Line($"hook {Path.Combine(folder, file)} {RepositoryFiles.Hash(Path.Combine(folder, file), reading.Root)}");
        }
    }

    /// <summary>
    /// The links in a repository's folder, each a risk; never followed. The folder is walked <see cref="GitTrust.TreeDeep"/>
    /// levels down, and <c>modules</c> at its top is left to the repositories under it, each read as its own.
    /// </summary>
    private static List<string> LinksIn(Reading reading, string folder)
    {
        var risks = new List<string>();
        var stack = new Stack<(string Folder, int Depth)>();
        stack.Push((folder, 0));
        while (stack.Count > 0)
        {
            reading.Cancellation.ThrowIfCancellationRequested();
            var (at, depth) = stack.Pop();
            foreach (var (entry, isFolder, attributes) in RepositoryFiles.Entries(at, reading.Root))
            {
                var path = Path.Combine(at, entry);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    risks.Add(GitTrust.Link(reading.Shown(path)));
                }
                else if (isFolder && !(depth == 0 && entry.Equals("modules", StringComparison.OrdinalIgnoreCase)))
                {
                    if (depth + 1 > GitTrust.TreeDeep)
                    {
                        risks.Add(GitTrust.TooDeep);
                    }
                    else
                    {
                        stack.Push((path, depth + 1));
                    }
                }
            }
        }

        return risks;
    }

    /// <summary>
    /// The repositories under the folder git keeps the submodules' in (<c>.git/modules</c>), also one that has no
    /// work tree now: a folder with a <c>config</c> and a <c>HEAD</c> file is one, and below it only its own
    /// <c>modules</c> folder is looked into. A link to another folder is not followed. The folders are followed
    /// <see cref="GitTrust.Deep"/> levels down and no further (a submodule's name with slashes is as many folders, and
    /// a repository's own <c>modules</c> folder one more); a folder at that depth that holds folders still is given
    /// in <c>TooDeep</c>, so that it is named and never taken as holding nothing. Read through
    /// <see cref="RepositoryFiles"/>, from <paramref name="root"/>.
    /// </summary>
    public static (IReadOnlyList<string> Found, IReadOnlyList<string> TooDeep) ModuleFolders(string modules, string root)
    {
        var found = new List<string>();
        var tooDeep = new List<string>();
        void Look(string folder, int depth)
        {
            var inside = RepositoryFiles.Entries(folder, root).Where(entry => entry.IsFolder).OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToList();
            if (depth > GitTrust.Deep)
            {
                if (inside.Count > 0)
                {
                    tooDeep.Add(Path.GetFullPath(folder));
                }

                return;
            }

            foreach (var (name, _, attributes) in inside)
            {
                var one = Path.Combine(folder, name);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                if (RepositoryFiles.Attributes(Path.Combine(one, "config"), root) is not null && RepositoryFiles.Attributes(Path.Combine(one, "HEAD"), root) is not null)
                {
                    found.Add(Path.GetFullPath(one));
                    if (RepositoryFiles.Attributes(Path.Combine(one, "modules"), root) is not null)
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

        if (RepositoryFiles.Attributes(modules, root) is { } kind && kind.HasFlag(FileAttributes.Directory))
        {
            Look(modules, 1);
        }

        return (found, tooDeep);
    }

    /// <summary>The names of a folder's files (or, <paramref name="files"/> false, of every entry), in order; none when there is no such folder.</summary>
    private static List<string> FileNames(Reading reading, string folder, bool files = true) =>
        [.. RepositoryFiles.Entries(folder, reading.Root).Where(entry => !files || !entry.IsFolder).Select(entry => entry.Name).Order(StringComparer.OrdinalIgnoreCase)];

    /// <summary>A path of a setting as git reads it: <c>~/</c> under the user's folder, a relative one from <paramref name="start"/>; null for one that is no path here. Worked out from the text alone.</summary>
    private static string? Resolve(string value, string start)
    {
        try
        {
            var path = value.Trim().Trim('"');
            if (path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith(@"~\", StringComparison.Ordinal))
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]);
            }

            // Git for Windows' own form of a drive, /c/work.
            if (path.Length >= 3 && path[0] == '/' && char.IsAsciiLetter(path[1]) && path[2] == '/')
            {
                path = $"{path[1]}:{path[2..]}";
            }

            return Path.GetFullPath(path, start);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>
    /// The folder <c>GIT_DIR</c> names for the parser's runs: one that never exists (inside the hooks folder of
    /// axKit's own, which is emptied before every use), so that git looks for no repository at all.
    /// </summary>
    private static string NoRepository => Path.Combine(GitTrust.NoHooksFolder, "no-repository");

    /// <summary>The settings git's parser gave for a file's content, by the file's full path, with the SHA-256 of that content; at most <see cref="ParsedKept"/> files.</summary>
    private static readonly Dictionary<string, (string Hash, IReadOnlyList<FileSetting> Settings)> Parsed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>How many files' settings are kept between checks; past that, all are let go.</summary>
    private const int ParsedKept = 4096;

    /// <summary>The largest settings file whose settings are kept between checks.</summary>
    private const long ParsedLargest = 1024 * 1024;

    /// <summary>
    /// One settings file through git's parser (<see cref="GitTrust.ParserArguments"/>), with no repository; null when
    /// git could not read it, or when it lies in a place axGIT does not read (git would read it there). A file whose
    /// content is the same as at an earlier check (its SHA-256, read now) is not given to git again: the parser
    /// reads that one file and nothing else, so the same content gives the same settings (the review's fifth pass:
    /// the check before every changing command, P3, read every submodule's settings anew, about 1.9 seconds for 50).
    /// </summary>
    private static async Task<IReadOnlyList<FileSetting>?> ParseAsync(Reading reading, string file)
    {
        if (RepositoryFiles.Place(file, reading.Root) is not { } full)
        {
            return null;
        }

        var hash = RepositoryFiles.Bytes(full, reading.Root, ParsedLargest) is { } bytes ? Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) : null;
        lock (Parsed)
        {
            if (hash is not null && Parsed.TryGetValue(full, out var kept) && kept.Hash == hash)
            {
                return kept.Settings;
            }
        }

        var arguments = GitTrust.ParserArguments(full);
        lock (RecordGate)
        {
            Record?.Add(GitRunner.Command(null, null, arguments));
        }

        var environment = new Dictionary<string, string>(reading.Environment ?? new Dictionary<string, string>()) { ["GIT_DIR"] = NoRepository };
        var result = await GitRunner.RunAsync(null, arguments, GitRunner.Short, reading.Cancellation, environment: environment);
        if (!result.Ok || result.OutputHeldOpen)
        {
            return null;
        }

        var settings = GitTrust.ParseSettings(result.Output);
        // Kept only when the content git read is the content hashed: read again after the parse, the same.
        if (hash is not null && RepositoryFiles.Bytes(full, reading.Root, ParsedLargest) is { } after && Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(after)) == hash)
        {
            lock (Parsed)
            {
                if (Parsed.Count >= ParsedKept)
                {
                    Parsed.Clear();
                }

                Parsed[full] = (hash, settings);
            }
        }

        return settings;
    }
}
