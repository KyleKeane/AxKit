using System.IO.Enumeration;
using System.Security.Cryptography;

namespace AxGit.Core;

/// <summary>
/// What the reads of <see cref="GitTrustScan"/> gave: what in the repository is a risk (<see cref="GitTrust"/>'s
/// rules; those of the repositories inside it named with them), or, in <paramref name="Problem"/>, why its files
/// could not be read, with which nothing may run. <paramref name="Mark"/>: the mark of what it would run
/// (<see cref="GitTrust.Mark"/>), which a trust is kept with. <paramref name="Hooks"/>: the hooks folder of the
/// repository opened, as a full path. <paramref name="InTree"/>: the settings whose program, or hooks folder, lies
/// in the work tree, where a switch or a pull can change it while git runs (T4); a changing command runs with
/// them set aside (<see cref="GitTrust.Overrides"/>).
/// </summary>
public sealed record TrustScan(IReadOnlyList<string> Risks, string? Problem, string Mark = "", string? Hooks = null, IReadOnlyList<string>? InTree = null)
{
    /// <summary>Whether what the repository would run is known: its files could be read.</summary>
    public bool Known => Problem is null;
}

/// <summary>
/// The reads that say what a repository would run (SPEC.md AG-1.6), before any git command runs in it and again
/// after every command that changes it, before every fetch, pull and push and at F5 (AG-1.6b). Since the review's
/// fourth pass (2026-11-28, T1) no git command runs in the repository for it: axKit's own code reads the files,
/// and git is run only as a parser of one settings file given by its path (<see cref="GitTrust.ParserArguments"/>),
/// with no repository (<c>GIT_DIR</c> a folder that does not exist), which runs nothing.
///
/// What is read, of the repository opened and of every repository inside it:
/// <list type="bullet">
/// <item>its <c>.git</c> entry: a folder, or a file with the folder it names; a link there, or a folder outside
/// the work tree, is a risk, and a folder on another computer is named and not read;</item>
/// <item>the <c>commondir</c> file of a linked work tree, the same way;</item>
/// <item>every entry of the <c>.git</c> folder for a link (never followed), the alternates, and the legacy
/// <c>remotes</c> and <c>branches</c> files git still obeys;</item>
/// <item>the settings files of the repository's own scopes (<c>config</c>, <c>config.worktree</c>) through the
/// parser, each setting's name and value, and the files its includes name (for the mark alone: an include is a
/// risk by its name);</item>
/// <item>the hooks folder: the default one, or the one <c>core.hooksPath</c> names;</item>
/// <item>the files the other settings name, for the mark and for <see cref="TrustScan.InTree"/>.</item>
/// </list>
/// The repositories inside are found by a walk of the work tree for <c>.git</c> entries (<see cref="Walk"/>) and
/// under <c>.git/modules</c> (<see cref="GitTrust.ModuleFolders"/>). The walk does not read <c>.gitignore</c>: it
/// goes into every folder, the ignored ones too, so that it finds every repository git could run itself in (a
/// submodule's entry can lie in an ignored folder; reading git's ignore rules in C# would be a second parser that
/// can differ from git's). It finds more than git would use, which asks about more and never lets a risk through.
/// It never follows a link: a link in the work tree is a risk.
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
    /// The folder of the repository that holds <paramref name="path"/> (a folder, or a file whose folder is taken)
    /// as git finds it going up: the first folder with a <c>.git</c> entry that is a repository (a <c>.git</c>
    /// folder with HEAD, or a <c>.git</c> file); null when there is none, and when the path lies inside a
    /// repository's own folder or a bare repository, which have no work tree (AG-1.2). Read in C#, a link's own
    /// attributes and never what it points at: a <c>.git</c> that is a link is the repository's entry, which the
    /// scan then names as a risk.
    /// </summary>
    public static string? RootOf(string path)
    {
        try
        {
            var folder = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            for (var at = folder is null ? null : Path.GetFullPath(folder); at is not null; at = Path.GetDirectoryName(at))
            {
                var dot = Path.Combine(at, ".git");
                if (Attributes(dot) is { } kind && (kind.HasFlag(FileAttributes.ReparsePoint) || !kind.HasFlag(FileAttributes.Directory) || IsRepositoryFolder(dot)))
                {
                    return at;
                }

                if (IsRepositoryFolder(at))
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

    /// <summary>A folder git takes for a repository: HEAD and the objects and refs, or HEAD and a commondir (a linked work tree's), each read by its own attributes.</summary>
    private static bool IsRepositoryFolder(string folder) =>
        Attributes(Path.Combine(folder, "HEAD")) is not null
        && ((Attributes(Path.Combine(folder, "objects")) is not null && Attributes(Path.Combine(folder, "refs")) is not null) || Attributes(Path.Combine(folder, "commondir")) is not null);

    /// <summary>An entry's own attributes, a link's and not what it points at; null when there is none.</summary>
    private static FileAttributes? Attributes(string path)
    {
        try
        {
            return File.GetAttributes(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Whether a drive letter is a network drive, as Windows says.</summary>
    private static bool IsNetworkDrive(char letter)
    {
        try
        {
            return new DriveInfo(letter + ":\\").DriveType == DriveType.Network;
        }
        catch (ArgumentException)
        {
            return false;
        }
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

        public string? Problem { get; set; }

        public string? Hooks { get; set; }

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

        public string? Elsewhere(string value) => GitTrust.ElsewhereOf(value, Computer, IsNetworkDrive);

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

    private static async Task<TrustScan> ReadOnTaskAsync(string root, CancellationToken cancellation, IReadOnlyDictionary<string, string>? environment)
    {
        var reading = new Reading(root, GitTrust.ComputerOf(root, IsNetworkDrive), environment, cancellation);
        if (Attributes(root) is null)
        {
            return new TrustScan([], $"{root} is not there.");
        }

        // The repository opened: its .git entry, then everything of it.
        var gitDir = Entry(reading, root, string.Empty);
        if (reading.Problem is not null)
        {
            return new TrustScan([], reading.Problem);
        }

        var inside = new List<(string Name, string GitDir, string? WorkTree)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? common = null;
        if (gitDir is not null)
        {
            common = await RepositoryAsync(reading, gitDir, root, string.Empty, inside: false);
            if (reading.Problem is not null)
            {
                return new TrustScan([], reading.Problem);
            }

            seen.Add(gitDir);
        }

        // The repositories inside: the work trees the walk finds, then those git keeps under .git/modules.
        foreach (var (name, folder) in Walk(reading))
        {
            if (Entry(reading, folder, name) is { } found && seen.Add(found))
            {
                inside.Add((name, found, folder));
            }
        }

        if (common is not null)
        {
            var modules = Path.Combine(common, "modules");
            if (Attributes(modules) is { } kind && !kind.HasFlag(FileAttributes.ReparsePoint))
            {
                var (found, tooDeep) = GitTrust.ModuleFolders(modules);
                foreach (var folder in found.Where(seen.Add))
                {
                    inside.Add((Path.GetRelativePath(modules, folder).Replace('\\', '/'), folder, null));
                }

                foreach (var folder in tooDeep)
                {
                    reading.Add(Path.GetRelativePath(modules, folder).Replace('\\', '/'), [GitTrust.NotRead]);
                }
            }
        }

        foreach (var some in inside.Chunk(AtOnce))
        {
            await Task.WhenAll(some.Select(one => RepositoryAsync(reading, one.GitDir, one.WorkTree, one.Name, inside: true)));
        }

        var risks = reading.Risks.Distinct(StringComparer.Ordinal).ToList();
        var lines = reading.Lines.Concat(risks.Select(risk => "risk " + risk));
        return new TrustScan(risks, null, GitTrust.Mark(lines), reading.Hooks, [.. reading.InTree.Distinct(StringComparer.Ordinal)]);
    }

    /// <summary>
    /// A work tree's <c>.git</c> entry, as the repository folder to read; null when it has none or names none (git
    /// runs nothing there), or when it is a risk read no further: a link, or a folder on another computer. For the
    /// repository opened a <c>.git</c> file that cannot be read is the scan's problem.
    /// </summary>
    private static string? Entry(Reading reading, string worktree, string name)
    {
        var dot = Path.Combine(worktree, ".git");
        if (Attributes(dot) is not { } kind)
        {
            if (name.Length == 0)
            {
                reading.Problem = $"{worktree} holds no .git folder or file.";
            }

            return null;
        }

        if (kind.HasFlag(FileAttributes.ReparsePoint))
        {
            reading.Add(name, [GitTrust.Link(reading.Shown(dot))]);
            return null;
        }

        if (kind.HasFlag(FileAttributes.Directory))
        {
            return Path.GetFullPath(dot);
        }

        // A .git file: the folder it names, worked out from its text alone.
        string text;
        try
        {
            using var reader = new StreamReader(dot);
            var buffer = new char[4096];
            text = new string(buffer, 0, reader.ReadBlock(buffer, 0, buffer.Length)).Split('\n')[0].Trim();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (name.Length == 0)
            {
                reading.Problem = $"Its .git file could not be read: {ex.Message}";
            }
            else
            {
                reading.Add(name, [GitTrust.NotRead]);
            }

            return null;
        }

        if (!text.StartsWith("gitdir:", StringComparison.Ordinal) || text[7..].Trim() is not { Length: > 0 } named)
        {
            if (name.Length == 0)
            {
                reading.Problem = "Its .git file names no repository.";
            }

            return null;
        }

        if (reading.Elsewhere(named) is { } host)
        {
            reading.Add(name, [GitTrust.OnAnotherComputer("its .git file", host)]);
            return null;
        }

        return Path.GetFullPath(named, worktree);
    }

    /// <summary>
    /// One repository's folder read (the list of <see cref="GitTrustScan"/>), its risks named with
    /// <paramref name="name"/> (empty for the repository opened); gives its common folder, where the modules are,
    /// or null when it was not read. A failure of the repository opened is the scan's problem; one of a repository
    /// inside names it with <see cref="GitTrust.NotRead"/>.
    /// </summary>
    private static async Task<string?> RepositoryAsync(Reading reading, string gitDir, string? workTree, string name, bool inside)
    {
        void Fail(string problem)
        {
            if (inside)
            {
                reading.Add(name, [GitTrust.NotRead]);
            }
            else
            {
                reading.Problem = problem;
            }
        }

        var risks = new List<string>();
        if (!Under(reading.Root, gitDir))
        {
            risks.Add(GitTrust.OutsideTheWorkTree);
        }

        if (Attributes(gitDir) is not { } kind || !kind.HasFlag(FileAttributes.Directory))
        {
            reading.Add(name, risks);
            Fail($"Its repository folder {gitDir} is not there.");
            return null;
        }

        // A linked work tree's common folder, named in its commondir file.
        var common = gitDir;
        if (Small(Path.Combine(gitDir, "commondir")) is { } commonText && commonText.Split('\n')[0].Trim() is { Length: > 0 } commonPath)
        {
            if (reading.Elsewhere(commonPath) is { } host)
            {
                reading.Add(name, [.. risks, GitTrust.OnAnotherComputer("its commondir file", host)]);
                return null;
            }

            common = Path.GetFullPath(commonPath, gitDir);
            if (!Under(reading.Root, common) && !risks.Contains(GitTrust.OutsideTheWorkTree))
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
            if (Attributes(Path.Combine(folder, "objects", "info", "alternates")) is not null || Attributes(Path.Combine(folder, "objects", "info", "http-alternates")) is not null)
            {
                risks.Add(GitTrust.Alternates);
            }

            foreach (var legacy in new[] { "remotes", "branches" })
            {
                risks.AddRange(FileNames(Path.Combine(folder, legacy)).Select(file => GitTrust.Legacy(legacy, file)));
            }
        }

        // The settings of the repository's own scopes, through git's parser.
        var files = common == gitDir
            ? new[] { Path.Combine(gitDir, "config"), Path.Combine(gitDir, "config.worktree") }
            : [Path.Combine(common, "config"), Path.Combine(gitDir, "config.worktree")];
        var settings = new List<FileSetting>();
        foreach (var file in files)
        {
            if (Attributes(file) is not { } fileKind || fileKind.HasFlag(FileAttributes.ReparsePoint))
            {
                // Not there, or a link, named above and never read.
                continue;
            }

            if (await ParseAsync(reading, file) is not { } parsed)
            {
                reading.Add(name, risks);
                Fail($"git could not read its settings file {file}.");
                return null;
            }

            settings.AddRange(parsed);
        }

        risks.AddRange(GitTrust.SettingRisks(settings, inside));
        risks.AddRange(GitTrust.ValueRisks(settings, reading.Computer, IsNetworkDrive));
        reading.Add(name, risks);

        // For the mark: every setting that is not harmless with its value, and the settings files its includes name.
        var start = workTree ?? gitDir;
        var named = new List<FileSetting>(settings);
        foreach (var setting in settings.Where(setting => !GitTrust.IsInert(setting.Name, inside)))
        {
            reading.Line($"value {name} {setting.Name}={setting.Value}");
        }

        await IncludesAsync(reading, settings, common, named, 0);

        // The hooks folder: core.hooksPath where the repository's own files set it, git's own otherwise.
        var hooksPaths = named.Where(setting => setting.Name.Equals("core.hookspath", StringComparison.OrdinalIgnoreCase) && setting.Value is { Length: > 0 }).Select(setting => setting.Value!).ToList();
        var ownHooks = settings.LastOrDefault(setting => setting.Name.Equals("core.hookspath", StringComparison.OrdinalIgnoreCase) && setting.Value is { Length: > 0 })?.Value;
        var hooks = ownHooks is null ? Path.Combine(common, "hooks") : Resolve(ownHooks, start);
        var folders = new List<string?> { hooks };
        folders.AddRange(hooksPaths.Where(path => path != ownHooks).Select(path => Resolve(path, start)));
        foreach (var folder in folders.OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            HooksIn(reading, folder, name, primary: folder == hooks);
            if (reading.InWorkTree(folder))
            {
                reading.Tree(name, "core.hookspath");
            }
        }

        if (!inside)
        {
            reading.Hooks = hooks;
        }

        // The files the other settings name: hashed for the mark, and a program in the work tree noted (T4).
        foreach (var setting in named.Where(setting => !GitTrust.IsInert(setting.Name, inside) && setting.Value is not null))
        {
            foreach (var word in GitTrust.PathWords(setting.Value!))
            {
                if (reading.Elsewhere(word) is not null || Resolve(word, start) is not { } path)
                {
                    continue;
                }

                reading.Line($"file {path} {Hash(path)}");
                if (GitTrust.RunsAProgram(setting.Name) && reading.InWorkTree(path))
                {
                    reading.Tree(name, setting.Name);
                }
            }
        }

        return common;
    }

    /// <summary>
    /// The settings files the includes of <paramref name="settings"/> name, each hashed for the mark and read
    /// through the parser in its turn (its settings join <paramref name="named"/>, for the files they name), up to
    /// <see cref="GitTrust.Deep"/> levels; a path on another computer is never read (it is a risk by its value).
    /// Every include counts, whatever its condition: git decides that at each command.
    /// </summary>
    private static async Task IncludesAsync(Reading reading, IReadOnlyList<FileSetting> settings, string folder, List<FileSetting> named, int depth)
    {
        foreach (var include in settings.Where(setting => setting.Value is { Length: > 0 } && GitTrust.Shape(setting.Name) is "include.path" or "includeif.*.path"))
        {
            var value = include.Value!.Trim();
            if (reading.Elsewhere(value) is not null || value.StartsWith("%(", StringComparison.Ordinal) || Resolve(value, folder) is not { } path)
            {
                reading.Line($"include {value}");
                continue;
            }

            reading.Line($"include {path} {Hash(path)}");
            if (depth < GitTrust.Deep && Attributes(path) is { } kind && !kind.HasFlag(FileAttributes.ReparsePoint) && !kind.HasFlag(FileAttributes.Directory)
                && await ParseAsync(reading, path) is { } more)
            {
                named.AddRange(more);
                await IncludesAsync(reading, more, Path.GetDirectoryName(path)!, named, depth + 1);
            }
        }
    }

    /// <summary>A hooks folder: each hook a risk (of the folder git uses, <paramref name="primary"/>) and hashed, the listing marked; a link there named and not followed.</summary>
    private static void HooksIn(Reading reading, string folder, string name, bool primary)
    {
        if (reading.Elsewhere(folder) is not null || Attributes(folder) is not { } kind || !kind.HasFlag(FileAttributes.Directory))
        {
            reading.Line($"hooks {folder} none");
            return;
        }

        if (kind.HasFlag(FileAttributes.ReparsePoint))
        {
            reading.Add(name, [GitTrust.Link(reading.Shown(folder))]);
            return;
        }

        var entries = FileNames(folder, files: false);
        reading.Line($"hooks {folder}: {string.Join("/", entries)}");
        foreach (var file in FileNames(folder).Where(GitTrust.IsHook))
        {
            if (primary)
            {
                reading.Add(name, [GitTrust.Hook(file)]);
            }

            reading.Line($"hook {Path.Combine(folder, file)} {Hash(Path.Combine(folder, file))}");
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
            foreach (var (entry, isFolder, attributes) in Entries(at))
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
    /// The folders of the work tree that hold a <c>.git</c> entry, each with its path from the root ("libs/zip"),
    /// the repository opened's own left out: every folder is gone into, ignored or not, and none is passed over
    /// (see the class's summary); a link to a folder is a risk and is not followed, a folder deeper than
    /// <see cref="GitTrust.TreeDeep"/> levels that holds folders is one too. A folder that cannot be listed is one git
    /// cannot go into either.
    /// </summary>
    private static List<(string Name, string Folder)> Walk(Reading reading)
    {
        var found = new List<(string, string)>();
        var stack = new Stack<(string Folder, int Depth)>();
        stack.Push((reading.Root, 0));
        while (stack.Count > 0)
        {
            reading.Cancellation.ThrowIfCancellationRequested();
            var (at, depth) = stack.Pop();
            foreach (var (entry, isFolder, attributes) in Entries(at))
            {
                var path = Path.Combine(at, entry);
                if (entry.Equals(".git", StringComparison.OrdinalIgnoreCase))
                {
                    if (depth > 0)
                    {
                        found.Add((reading.Shown(at), at));
                    }

                    continue;
                }

                if (!isFolder)
                {
                    continue;
                }

                if (attributes.HasFlag(FileAttributes.ReparsePoint) && IsLink(path))
                {
                    reading.Add(string.Empty, [GitTrust.Link(reading.Shown(path))]);
                }
                else if (depth + 1 > GitTrust.TreeDeep)
                {
                    reading.Add(string.Empty, [GitTrust.TooDeep]);
                }
                else
                {
                    stack.Push((path, depth + 1));
                }
            }
        }

        return found;
    }

    /// <summary>Whether a folder with a reparse point is a link to another place (a symbolic link or a junction); a folder of a cloud drive carries one too, and git goes into it as into any folder.</summary>
    private static bool IsLink(string path)
    {
        try
        {
            return new DirectoryInfo(path).LinkTarget is not null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    /// <summary>A folder's entries, each with whether it is a folder and its own attributes; none when the folder cannot be listed.</summary>
    private static List<(string Name, bool IsFolder, FileAttributes Attributes)> Entries(string folder)
    {
        try
        {
            return [.. new FileSystemEnumerable<(string, bool, FileAttributes)>(folder,
                (ref FileSystemEntry entry) => (entry.FileName.ToString(), entry.IsDirectory, entry.Attributes),
                new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = true, ReturnSpecialDirectories = false })];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>The names of a folder's files (or, <paramref name="files"/> false, of every entry), in order; none when there is no such folder.</summary>
    private static List<string> FileNames(string folder, bool files = true) =>
        [.. Entries(folder).Where(entry => !files || !entry.IsFolder).Select(entry => entry.Name).Order(StringComparer.OrdinalIgnoreCase)];

    /// <summary>Whether a full path is <paramref name="root"/> or lies under it.</summary>
    private static bool Under(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        return relative == "." || (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative));
    }

    /// <summary>A path of a setting as git reads it: <c>~/</c> under the user's folder, a relative one from <paramref name="start"/>; null for one that is no path here.</summary>
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

    /// <summary>A file's content as its SHA-256; "none" when there is no such file, "link" for one that is a link (never read through), "unread" when it cannot be read.</summary>
    private static string Hash(string path)
    {
        if (Attributes(path) is not { } kind || kind.HasFlag(FileAttributes.Directory))
        {
            return "none";
        }

        if (kind.HasFlag(FileAttributes.ReparsePoint))
        {
            return "link";
        }

        try
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "unread";
        }
    }

    /// <summary>A small file of the repository's (commondir), its first 4 KB as text; null when it is not there, is a link or cannot be read.</summary>
    private static string? Small(string path)
    {
        if (Attributes(path) is not { } kind || kind.HasFlag(FileAttributes.ReparsePoint) || kind.HasFlag(FileAttributes.Directory))
        {
            return null;
        }

        try
        {
            using var reader = new StreamReader(path);
            var buffer = new char[4096];
            return new string(buffer, 0, reader.ReadBlock(buffer, 0, buffer.Length)).Replace("\r", string.Empty, StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// The folder <c>GIT_DIR</c> names for the parser's runs: one that never exists (inside the hooks folder of
    /// axKit's own, which is emptied before every use), so that git looks for no repository at all.
    /// </summary>
    private static string NoRepository => Path.Combine(GitTrust.NoHooksFolder, "no-repository");

    /// <summary>One settings file through git's parser (<see cref="GitTrust.ParserArguments"/>), with no repository; null when git could not read it.</summary>
    private static async Task<IReadOnlyList<FileSetting>?> ParseAsync(Reading reading, string file)
    {
        var arguments = GitTrust.ParserArguments(file);
        lock (RecordGate)
        {
            Record?.Add(GitRunner.Command(null, arguments));
        }

        var environment = new Dictionary<string, string>(reading.Environment ?? new Dictionary<string, string>()) { ["GIT_DIR"] = NoRepository };
        var result = await GitRunner.RunAsync(null, arguments, GitRunner.Short, reading.Cancellation, environment: environment);
        return result.Ok && !result.OutputHeldOpen ? GitTrust.ParseSettings(result.Output) : null;
    }
}
