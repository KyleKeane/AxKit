using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AxGit.Core;
using AxKit.Core;

namespace AxGit.Tests;

/// <summary>
/// The review's sixth pass of 2026-11-28 (batch B4f; docs/axGIT/SPEC.md AG-1.6): every repository is asked about
/// once before git runs in it (G1), and the scan reads strictly and fails closed (G3). Each High of the pass was a
/// repository the scan found nothing in, which was then trusted without asking, and git ran a program or read a share
/// in it. Here each of those repositories, and a clean one: the session refuses every command until the user's
/// trust, and no git process runs in it (git's own trace, GIT_TRACE2_EVENT, lists every git started: only the parser
/// of one settings file, with no repository); the half that shows the harm is git itself in a console. Against git,
/// in repositories the test makes; a share is this computer's own, //localhost/C$.
/// </summary>
[Collection(GitProgram.Collection)]
public sealed class GitAskedOnceTests : IDisposable
{
    private readonly TempFolder _dir = new("axgit-asked");
    private readonly Dictionary<string, string> _env;

    public GitAskedOnceTests() => _env = GitTests.Environment(_dir);

    public void Dispose() => _dir.Dispose();

    private ProcessResult Plain(string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo(GitRunner.Program) { WorkingDirectory = folder };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        GitTests.Put(start, _env);
        return ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
    }

    private string Git(string folder, params string[] arguments)
    {
        var result = Plain(folder, arguments);
        Assert.True(!result.Stopped && result.ExitCode == 0, $"git {string.Join(' ', arguments)}: {result.Output} {result.Error}");
        return result.Output;
    }

    private string Marks => Path.Combine(_dir.Path, "marks.log");

    private string[] Ran()
    {
        if (!File.Exists(Marks))
        {
            return [];
        }

        var lines = File.ReadAllLines(Marks).Distinct().ToArray();
        File.Delete(Marks);
        return lines;
    }

    /// <summary>A filter's command that leaves <paramref name="line"/> in the test's file and passes its input on.</summary>
    private string Marker(string line)
    {
        var script = _dir.File("marker.sh", $"#!/bin/sh\necho \"$1\" >> \"{Marks.Replace('\\', '/')}\"\ncat\n");
        return $"sh \"{script.Replace('\\', '/')}\" {line}";
    }

    private string Repository(string path)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_dir.Path, path)).FullName;
        Git(folder, "init", "-q");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "one\n");
        File.WriteAllText(Path.Combine(folder, "b.txt"), "two\n");
        Git(folder, "add", "a.txt", "b.txt");
        Git(folder, "commit", "-q", "-m", "one");
        return folder;
    }

    /// <summary>A repository whose every .txt goes through its own filter x, a marker, with s.txt committed and touched since: a status in it runs the filter.</summary>
    private string Filtered(string path, string line)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_dir.Path, path)).FullName;
        Git(folder, "init", "-q");
        File.WriteAllText(Path.Combine(folder, ".gitattributes"), "*.txt filter=x\n");
        File.WriteAllText(Path.Combine(folder, "s.txt"), "inside\n");
        Git(folder, "add", ".");
        Git(folder, "commit", "-q", "-m", "inside");
        Git(folder, "config", "filter.x.clean", Marker(line));
        Touch(Path.Combine(folder, "s.txt"));
        Ran();
        return folder;
    }

    private static DateTime _touched = DateTime.UtcNow;

    /// <summary>A file's time moved on, its content the same, so that git must read it again (through its filter).</summary>
    private static void Touch(string file) => File.SetLastWriteTimeUtc(file, _touched = _touched.AddSeconds(3));

    /// <summary>
    /// G1's guarantee for one repository: a session on it whose user has trusted nothing is not trusted and refuses
    /// every command axGIT has; no mark comes; and every git process started, the check's included, is the parser of
    /// one settings file with no repository (git's trace of every process it starts). Gives the check.
    /// </summary>
    private async Task<TrustScan> NothingRunsUntilTrustAsync(string root)
    {
        var trace = Path.Combine(_dir.Path, $"trace-{Guid.NewGuid():N}.json");
        var environment = new Dictionary<string, string>(_env) { ["GIT_TRACE2_EVENT"] = trace };
        Ran();
        var session = new GitSession(root, _ => false, environment);
        var scan = await session.CheckAsync();
        Assert.False(session.Trusted, "trusted without asking");
        Assert.NotNull(session.Refused);
        var results = await GitTests.EveryCommandAsync(root, "origin", (arguments, change) => change ? session.ChangeAsync(arguments, GitRunner.Short) : session.RunAsync(arguments, GitRunner.Short));
        Assert.All(results, one => Assert.True(one.Result.NotRun, string.Join(' ', one.Arguments)));
        Assert.Empty(Ran());
        foreach (var started in Started(trace, "start"))
        {
            Assert.True(started.Contains("config") && started.Contains("--file") && started.Contains("--no-includes") && !started.Contains("-C"), "a git that is no parser ran: " + string.Join(' ', started));
        }

        // L2 (the review's seventh pass): and none of them started a program of its own.
        Assert.Empty(Started(trace, "child_start"));
        return scan;
    }

    /// <summary>The argument lists of every event of <paramref name="kind"/> in git's trace: "start" for a git process, "child_start" for a program a git started.</summary>
    private static List<List<string>> Started(string trace, string kind)
    {
        var started = new List<List<string>>();
        if (!File.Exists(trace))
        {
            return started;
        }

        foreach (var line in File.ReadAllLines(trace))
        {
            using var json = JsonDocument.Parse(line);
            if (json.RootElement.TryGetProperty("event", out var name) && name.GetString() == kind)
            {
                started.Add([.. json.RootElement.TryGetProperty("argv", out var argv) ? argv.EnumerateArray().Select(argument => argument.GetString() ?? string.Empty) : []]);
            }
        }

        return started;
    }

    /// <summary>A path through this computer's own share, //localhost/C$/...</summary>
    private static string Shared(string path) => "//localhost/" + path[0] + "$" + path[2..].Replace('\\', '/');

    // ---- G1: every repository asked about once ----

    /// <summary>
    /// G1: a repository with nothing of concern is asked about too, and runs nothing until trusted; trusted for its
    /// mark, it runs, and a trust kept with that mark (the window's settings, a trust given before this update among
    /// them) holds at the next opening without the notice.
    /// </summary>
    [GitFact]
    public async Task A_clean_repository_is_asked_about_and_runs_nothing_until_trusted()
    {
        var root = Repository("clean");
        var scan = await NothingRunsUntilTrustAsync(root);
        Assert.True(scan.Known);
        Assert.Empty(scan.Risks);

        var session = new GitSession(root, _ => false, _env);
        await session.CheckAsync();
        Assert.True(await session.TrustAsync(session.Scan));
        Assert.True((await session.RunAsync(GitStatus.Arguments, GitRunner.Short)).Ok);

        var kept = session.Scan.Mark;
        var again = new GitSession(root, scan => scan.Mark == kept, _env);
        await again.CheckAsync();
        Assert.True(again.Trusted);
    }

    // ---- The six repositories of the sixth pass ----

    /// <summary>
    /// H1: a split index, where a gitlink that replaces an entry of the shared index is written with an empty name.
    /// The scan read that name as the work tree itself and missed the repository inside; git entered it on a status
    /// and ran its filter. Now an empty name makes the index one axGIT does not read, a risk named.
    /// </summary>
    [GitFact]
    public async Task H1_a_split_index_that_replaces_an_entry_with_a_gitlink()
    {
        var root = Repository("h1");
        File.WriteAllText(Path.Combine(root, "sub"), "a file first\n");
        Git(root, "add", "sub");
        Git(root, "commit", "-q", "-m", "sub");
        Git(root, "update-index", "--split-index");
        File.Delete(Path.Combine(root, "sub"));
        var sub = Filtered(Path.Combine("h1", "sub"), "h1-filter");
        Git(root, "update-index", "--cacheinfo", $"160000,{Git(sub, "rev-parse", "HEAD").Trim()},sub");

        var scan = await NothingRunsUntilTrustAsync(root);
        Assert.Contains(GitTrust.IndexNotRead, scan.Risks);

        // The way it ran: git's status enters the repository and runs its filter.
        Plain(root, "status", "--porcelain");
        Assert.Equal(["h1-filter"], Ran());
    }

    /// <summary>
    /// H2: version 4 of the index writes each path as bytes dropped from the one before and the rest; the drop was
    /// taken in characters, so a gitlink after a path with a letter outside ASCII was read as another path, and its
    /// repository never entered by the scan. git entered it and ran its filter.
    /// </summary>
    [GitFact]
    public async Task H2_a_version_4_index_after_a_path_outside_ascii()
    {
        var root = Repository("h2");
        Directory.CreateDirectory(Path.Combine(root, "x"));
        File.WriteAllText(Path.Combine(root, "x", "ä"), "letter\n");
        Git(root, "add", "x");
        Git(root, "commit", "-q", "-m", "x");
        Git(root, "update-index", "--index-version", "4");
        var sub = Filtered(Path.Combine("h2", "x", "ösub"), "h2-filter");
        Git(root, "-c", "advice.addEmbeddedRepo=false", "add", "x/ösub");
        Assert.Contains("x/ösub", GitIndex.Read(File.ReadAllBytes(Path.Combine(root, ".git", "index")), 20).Gitlinks);
        Touch(Path.Combine(sub, "s.txt"));

        var scan = await NothingRunsUntilTrustAsync(root);
        Assert.Contains("submodule x/ösub: filter.x.clean", scan.Risks);

        Touch(Path.Combine(sub, "s.txt"));
        Plain(root, "status", "--porcelain");
        Assert.Equal(["h2-filter"], Ran());
    }

    /// <summary>
    /// H3: a submodule's core.worktree that names another folder than the one its gitlink is in: git, entering the
    /// submodule, works on that folder. It was harmless by the table; now it is a risk unless it is the gitlink's own
    /// folder, as git writes it. A submodule git made, also one inside another, holds no such risk.
    /// </summary>
    [GitFact]
    public async Task H3_a_submodules_work_tree_elsewhere()
    {
        var library = Repository("h3-library");
        var inner = Repository("h3-inner");
        Git(library, "-c", "protocol.file.allow=always", "submodule", "--quiet", "add", inner, "deep");
        Git(library, "commit", "-q", "-m", "deep");
        var root = Repository("h3");
        Git(root, "-c", "protocol.file.allow=always", "submodule", "--quiet", "add", library, "libs/zip");
        Git(root, "-c", "protocol.file.allow=always", "submodule", "--quiet", "update", "--init", "--recursive");
        Git(root, "commit", "-q", "-m", "a submodule");
        Assert.Empty((await GitTrustScan.ReadAsync(root, default, _env)).Risks);

        var elsewhere = Directory.CreateDirectory(Path.Combine(_dir.Path, "h3-elsewhere")).FullName;
        foreach (var file in Directory.GetFiles(library))
        {
            File.Copy(file, Path.Combine(elsewhere, Path.GetFileName(file)));
        }

        File.AppendAllText(Path.Combine(elsewhere, "a.txt"), "changed elsewhere\n");
        var module = Path.Combine(root, ".git", "modules", "libs", "zip");
        Git(module, "config", "core.worktree", elsewhere.Replace('\\', '/'));

        var scan = await NothingRunsUntilTrustAsync(root);
        Assert.Contains("submodule libs/zip: core.worktree", scan.Risks);

        // The way it ran: git's status reads the other folder as the submodule's work tree, and finds it changed.
        Assert.Contains("libs/zip", Git(root, "status", "--porcelain"));
    }

    /// <summary>
    /// H4: an untracked folder whose .git file names a repository on a share: git's status reads it there, to tell
    /// whether the folder holds a repository (tried with git 2.53: the folder is listed as one only when the share's
    /// target is a repository). The scan reads no untracked folder; the repository is asked about all the same, and
    /// what trust lets git do is said (G2).
    /// </summary>
    [GitFact]
    public async Task H4_an_untracked_git_file_that_names_a_share()
    {
        var target = Repository("h4-target");
        var root = Repository("h4");
        var untracked = Directory.CreateDirectory(Path.Combine(root, "u")).FullName;
        File.WriteAllText(Path.Combine(untracked, "k.txt"), "k\n");
        File.WriteAllText(Path.Combine(untracked, ".git"), $"gitdir: {Shared(Path.Combine(target, ".git"))}\n");

        var scan = await NothingRunsUntilTrustAsync(root);
        Assert.True(scan.Known);

        // The way it ran: the status lists the folder as a repository, read through the share.
        Assert.Contains("? u/\n", Git(root, "status", "--porcelain=v2", "--untracked-files=all").Replace("\r", string.Empty, StringComparison.Ordinal));
    }

    /// <summary>
    /// H5: a path of the index that names a share. git looks at every path of the index on a status, so it read the
    /// share; git's own check would never have let such a path in. Any path that check refuses makes the index one
    /// axGIT does not read.
    /// </summary>
    [GitFact]
    public async Task H5_a_path_of_the_index_on_a_share()
    {
        var root = Repository("h5");
        var far = _dir.File("h5-far.txt", "far\n");
        var index = Path.Combine(root, ".git", "index");
        File.WriteAllBytes(index, GitIndexTests.Index(2, GitIndexTests.File(Shared(far)), GitIndexTests.File("a.txt"), GitIndexTests.File("b.txt")));

        var scan = await NothingRunsUntilTrustAsync(root);
        Assert.Contains(GitTrust.IndexNotRead, scan.Risks);

        // The way it ran: git's status says the path is there or not, as the share has it.
        var there = Plain(root, "status", "--porcelain=v2").Output;
        File.Delete(far);
        var gone = Plain(root, "status", "--porcelain=v2").Output;
        Assert.NotEqual(there, gone);
    }

    /// <summary>
    /// H6: a HEAD with a byte order mark before "ref:". git takes no such folder for a repository; the scan read it as
    /// text, with the mark taken off, and found a repository. git, finding the repository itself at the first trust,
    /// took the folder above for a bare one, and read its settings. HEAD, commondir and a .git file are read as bytes
    /// by git's own rule now: such a folder is not opened, and nothing runs.
    /// </summary>
    [GitFact]
    public async Task H6_a_head_with_a_byte_order_mark()
    {
        var root = Repository("h6");
        var head = Path.Combine(root, ".git", "HEAD");
        File.WriteAllBytes(head, [0xEF, 0xBB, 0xBF, .. File.ReadAllBytes(head)]);
        Git(_dir.Path, "init", "-q", "--bare", "h6-bare");
        foreach (var name in new[] { "HEAD", "config" })
        {
            File.Copy(Path.Combine(_dir.Path, "h6-bare", name), Path.Combine(root, name));
        }

        foreach (var name in new[] { "objects", "refs" })
        {
            Directory.CreateDirectory(Path.Combine(root, name, "x"));
        }

        Assert.Null(GitTrustScan.RootOf(root));
        var scan = await NothingRunsUntilTrustAsync(root);
        Assert.False(scan.Known);

        // The way it ran: git, finding the repository itself, takes the folder for a bare one.
        var start = new ProcessStartInfo(GitRunner.Program) { WorkingDirectory = root };
        start.ArgumentList.Add("rev-parse");
        start.ArgumentList.Add("--absolute-git-dir");
        GitTests.Put(start, _env);
        start.Environment["GIT_CEILING_DIRECTORIES"] = _dir.Path;
        var found = ProcessRun.Run(start, [], TimeSpan.FromSeconds(60)).Output ?? string.Empty;
        Assert.Equal(Path.GetFullPath(root), Path.GetFullPath(found.Trim()), ignoreCase: true);
    }

    // ---- G3: HEAD, commondir and a .git file read as git reads them ----

    /// <summary>
    /// G3 (H6's class): git reads HEAD, a commondir file and a .git file as bytes: no byte order mark, ASCII
    /// whitespace alone after "ref:", "gitdir: " with its one space, the whole content less its trailing line ends.
    /// What git would read otherwise than the scan is no repository the scan opens, never one it reads its own way.
    /// </summary>
    [GitFact]
    public async Task Head_commondir_and_git_files_are_read_as_git_reads_them()
    {
        var root = Repository("strict");
        var head = Path.Combine(root, ".git", "HEAD");
        // The second of each pair: whether the scan opens it (a form git might still read, such as a space at the end that Windows drops, is not opened: closed, never read another way).
        foreach (var (text, git) in new[] { ("ref: refs/heads/main\n", true), ("ref:refs/heads/main", true), ("ref: \t refs/heads/main\r\n", true), ("ref:\u00A0refs/heads/main\n", false), ("ref: \vrefs/heads/main\n", false), (" ref: refs/heads/main\n", false), (new string('a', 40) + "\n", true), (new string('a', 39) + "\n", false) })
        {
            File.WriteAllText(head, text, new UTF8Encoding(false));
            Assert.True((GitTrustScan.RootOf(root) is not null) == git, text);
        }

        File.WriteAllText(head, "ref: refs/heads/main\n");
        var real = Path.Combine(root, ".git");
        var tree = Directory.CreateDirectory(Path.Combine(_dir.Path, "linked")).FullName;
        var gitFile = Path.Combine(tree, ".git");
        foreach (var (text, git) in new[] { ($"gitdir: {real}\n", true), ($"gitdir: {real}\r\n\r\n", true), ($"gitdir:{real}\n", false), ($"gitdir:  {real}\n", false), ($"gitdir: {real} \n", false), ($"\uFEFFgitdir: {real}\n", false), ($"gitdir: {real}\nmore\n", false) })
        {
            File.WriteAllText(gitFile, text, new UTF8Encoding(false));
            Assert.True((await GitTrustScan.ReadAsync(tree, default, _env)).Known == git, text);
        }

        // A linked work tree's commondir, the same way.
        var second = Path.Combine(_dir.Path, "strict-second");
        Git(root, "worktree", "add", "-q", second, "-b", "second");
        var commondir = Path.Combine(real, "worktrees", "strict-second", "commondir");
        foreach (var (text, git) in new[] { ("../..\n", true), ("../..\r\n", true), (" ../..\n", false), ("../.. \n", false), ("\uFEFF../..\n", false), ("../..\nmore\n", false) })
        {
            File.WriteAllText(commondir, text, new UTF8Encoding(false));
            Assert.True((await GitTrustScan.ReadAsync(second, default, _env)).Known == git, text);
        }
    }

    /// <summary>
    /// G3 (H6): the first trust asks git, finding the repository itself, which git folder it opens; a git folder
    /// other than the one the scan read is not opened. Here a fake git in git's place that names another folder;
    /// every other command goes to git itself.
    /// </summary>
    [GitFact]
    public async Task A_git_folder_git_finds_other_than_the_one_read_is_not_opened()
    {
        var root = Repository("other-folder");
        var real = GitRunner.Program;
        GitRunner.Program = _dir.File("git.cmd",
            "@echo off\r\necho %* | findstr /C:\"--absolute-git-dir\" >nul && (echo C:/elsewhere/.git& exit /b 0)\r\n" + $"\"{real}\" %*\r\n");
        try
        {
            var session = new GitSession(root, _ => true, _env);
            await session.CheckAsync();
            Assert.False(session.Trusted);
            Assert.True(session.Closed);
            Assert.Contains("C:/elsewhere/.git", session.Scan.Problem ?? string.Empty);
        }
        finally
        {
            GitRunner.Program = real;
        }

        // With git itself the same repository opens.
        var plain = new GitSession(root, _ => true, _env);
        await plain.CheckAsync();
        Assert.True(plain.Trusted);
    }

    // ---- G3, M1: a path value read as git reads it, or named ----

    /// <summary>
    /// M1: a path in a setting with whitespace at its ends, a drive without a separator (C:x, relative to that drive's
    /// own folder), a ~ that is not ~/ (another user's folder) or a %(prefix) of git's own install: the scan cannot
    /// say which file git takes, so it names the place, and reads none. An include with a space at its start named the
    /// file " extra.cfg" for git and "extra.cfg" for the scan. A relative include is read beside its own file.
    /// </summary>
    [GitFact]
    public async Task A_path_value_the_scan_cannot_read_as_git_is_named()
    {
        var number = 0;
        foreach (var value in new[] { " extra.cfg", "extra.cfg ", "C:extra.cfg", "~other/extra.cfg", "~", "%(prefix)/extra.cfg" })
        {
            var root = Repository("m1-" + number++);
            File.WriteAllText(Path.Combine(root, ".git", "extra.cfg"), "[user]\n\tname = x\n");
            File.AppendAllText(Path.Combine(root, ".git", "config"), $"[include]\n\tpath = \"{value}\"\n");
            var scan = await GitTrustScan.ReadAsync(root, default, _env);
            Assert.True(scan.Risks.Contains(GitTrust.NotReadPlace("include.path")), $"{value}: {string.Join(", ", scan.Risks)}");
        }

        // The same for a hooks folder and a file a setting names.
        var hooks = Repository("m1-hooks");
        File.AppendAllText(Path.Combine(hooks, ".git", "config"), "[core]\n\thooksPath = \" .git/hooks\"\n\texcludesFile = C:ignored\n");
        var named = await GitTrustScan.ReadAsync(hooks, default, _env);
        Assert.Contains(GitTrust.NotReadPlace("core.hookspath"), named.Risks);
        Assert.Contains(GitTrust.NotReadPlace("core.excludesfile"), named.Risks);

        // A plain relative include is read beside the file that names it: its content is in the mark.
        var plain = Repository("m1-plain");
        var extra = Path.Combine(plain, ".git", "extra.cfg");
        File.WriteAllText(extra, "[user]\n\tname = x\n");
        Git(plain, "config", "include.path", "extra.cfg");
        var first = await GitTrustScan.ReadAsync(plain, default, _env);
        Assert.DoesNotContain(GitTrust.NotReadPlace("include.path"), first.Risks);
        File.WriteAllText(extra, "[user]\n\tname = y\n");
        Assert.NotEqual(first.Mark, (await GitTrustScan.ReadAsync(plain, default, _env)).Mark);

        // The way it ran: git reads the file with the space, " extra.cfg", which the scan's trimmed path missed.
        var spaced = Repository("m1-spaced");
        File.WriteAllText(Path.Combine(spaced, ".git", " extra.cfg"), "[user]\n\tname = spaced\n");
        File.AppendAllText(Path.Combine(spaced, ".git", "config"), "[include]\n\tpath = \" extra.cfg\"\n");
        Assert.Equal("spaced", Git(spaced, "config", "user.name").Trim());
    }
}
