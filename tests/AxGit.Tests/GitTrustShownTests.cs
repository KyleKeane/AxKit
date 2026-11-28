using System.Diagnostics;
using AxGit.Core;
using AxKit.Core;

namespace AxGit.Tests;

/// <summary>
/// The review's seventh pass of 2026-11-28 (batch B4g; docs/axGIT/SPEC.md AG-1.6): a trust covers the repository the
/// user was shown, as it was shown. T1: a trust names the git folder itself, by its identity on its volume, so that
/// another repository at the same path is asked about as a new one. T2: every risk that names a file, a link or a value
/// marks its source, so that a changed source is asked about again; one class test over every kind of risk. T3: the
/// mark keeps values as written. L1: a root reached through a junction opens after Trust. Against git, in repositories
/// the test makes; nothing here depends on git's version.
/// </summary>
[Collection(GitProgram.Collection)]
public sealed class GitTrustShownTests : IDisposable
{
    private readonly TempFolder _dir = new("axgit-shown");
    private readonly Dictionary<string, string> _env;

    public GitTrustShownTests() => _env = GitTests.Environment(_dir);

    public void Dispose() => _dir.Dispose();

    private string Git(string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo(GitRunner.Program) { WorkingDirectory = folder };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        GitTests.Put(start, _env);
        var result = ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
        Assert.True(!result.Stopped && result.ExitCode == 0, $"git {string.Join(' ', arguments)}: {result.Output} {result.Error}");
        return result.Output;
    }

    private string Repository(string path, string content = "one\n")
    {
        var folder = Directory.CreateDirectory(Path.Combine(_dir.Path, path)).FullName;
        Git(folder, "init", "-q");
        File.WriteAllText(Path.Combine(folder, "a.txt"), content);
        Git(folder, "add", "a.txt");
        Git(folder, "commit", "-q", "-m", "one");
        return folder;
    }

    private Task<TrustScan> ScanAsync(string root) => GitTrustScan.ReadAsync(root, default, _env);

    // ---- T1: a trust names the git folder ----

    /// <summary>
    /// T1 (H1): repository A trusted at a path, then B put at the same path. Two clean repositories at one path have
    /// the same mark, so the mark alone took B for A. Now B is asked about as a new repository, in a session too; the
    /// same A reopened is not; and a clone made again at the same path is asked about.
    /// </summary>
    [GitFact]
    public async Task T1_another_repository_at_the_same_path_is_asked_and_the_same_one_reopened_is_not()
    {
        var path = Repository("same", "A\n");
        var settings = new GitSettings();
        GitSession Open() => new(path, scan => settings.Trusts(path, scan.Mark, scan.Identity), _env);

        var first = Open();
        await first.CheckAsync();
        Assert.False(first.Trusted);
        Assert.True(await first.TrustAsync(first.Scan));
        Assert.True(first.Scan.Identity is { Lasting: true }, $"the test folder's volume gives no lasting identity: {first.Scan.Identity}");
        Assert.True(settings.Trust(path, first.Scan.Mark, first.Scan.Identity));

        // The same A, opened again: trusted, no notice.
        var again = Open();
        await again.CheckAsync();
        Assert.True(again.Trusted);

        // B at the same path: the same mark, another git folder; asked about, and the window can say that nothing
        // was trusted for it.
        var aside = Path.Combine(_dir.Path, "A-aside");
        Directory.Move(path, aside);
        Repository("same", "B, another history\n");
        var b = Open();
        await b.CheckAsync();
        Assert.Equal(first.Scan.Mark, b.Scan.Mark);
        Assert.False(b.Trusted);
        Assert.NotNull(b.Refused);
        Assert.True(settings.TrustedAnother(path, b.Scan.Identity));

        // The session that trusted A, with B now in its place: its next check is not trusted either.
        await first.CheckAsync();
        Assert.False(first.Trusted);

        // A clone of A made again at the same path: asked about.
        Directory.Move(path, Path.Combine(_dir.Path, "B-aside"));
        Git(_dir.Path, "clone", "-q", aside, path);
        var clone = Open();
        await clone.CheckAsync();
        Assert.False(clone.Trusted);
    }

    /// <summary>
    /// L1: a root reached through a junction. git prints the git folder it opens by its real path, which the first
    /// trust compared with the scan's as text, so such a repository never opened. The comparison is by the folders'
    /// identities now.
    /// </summary>
    [GitFact]
    public async Task L1_a_root_through_a_junction_opens_after_trust()
    {
        var real = Repository("real");
        var through = Path.Combine(_dir.Path, "through");
        Assert.True(GitBoundsTests.Junction(through, real), "a junction could not be made");
        var session = new GitSession(through, _ => true, _env);
        var scan = await session.CheckAsync();
        Assert.True(scan.Known, scan.Problem);
        Assert.True(session.Trusted);
        Assert.Equal(RepositoryFiles.Identity(Path.Combine(real, ".git"), real), scan.Identity);
        Assert.True((await session.RunAsync(GitStatus.Arguments, GitRunner.Short)).Ok);
    }

    // ---- T2: every kind of risk marks its source ----

    /// <summary>A gitlink "sub" in the index of <paramref name="root"/>, with the folder made, for a .git file of a test's own.</summary>
    private string Gitlink(string root)
    {
        var hash = Git(root, "rev-parse", "HEAD").Trim();
        Git(root, "update-index", "--add", "--cacheinfo", $"160000,{hash},sub");
        return Directory.CreateDirectory(Path.Combine(root, "sub")).FullName;
    }

    /// <summary>
    /// For each kind of risk: a repository given the risk (the setup, from a fresh repository's root) and the change
    /// of its source that keeps the risk (what the setup gives back). The test fails when a kind of
    /// <see cref="GitTrust.RiskKind"/> has no row here.
    /// </summary>
    private Dictionary<GitTrust.RiskKind, Func<string, Action>> Rows() => new()
    {
        [GitTrust.RiskKind.Setting] = root =>
        {
            Git(root, "config", "core.fsmonitor", "echo one");
            return () => Git(root, "config", "core.fsmonitor", "echo two");
        },
        [GitTrust.RiskKind.FetchOutside] = root =>
        {
            Git(root, "config", "remote.origin.url", "https://one.example.invalid/r.git");
            Git(root, "config", "remote.origin.fetch", "+refs/heads/*:refs/x/*");
            return () => Git(root, "config", "remote.origin.fetch", "+refs/heads/*:refs/y/*");
        },
        [GitTrust.RiskKind.OnAnotherComputer] = root =>
        {
            Git(root, "config", "remote.origin.url", "//one.invalid/share/a.git");
            return () => Git(root, "config", "remote.origin.url", "//one.invalid/share/b.git");
        },
        [GitTrust.RiskKind.NotReadPlace] = root =>
        {
            var file = Path.Combine(root, ".gitmodules");
            File.WriteAllText(file, "[submodule \"x\"]\n\tpath = a:b\n");
            return () => File.WriteAllText(file, "[submodule \"x\"]\n\tpath = c:d\n");
        },
        [GitTrust.RiskKind.Hook] = root =>
        {
            var hook = Path.Combine(root, ".git", "hooks", "pre-commit");
            File.WriteAllText(hook, "#!/bin/sh\necho one\n");
            return () => File.WriteAllText(hook, "#!/bin/sh\necho two\n");
        },
        [GitTrust.RiskKind.Link] = root =>
        {
            var link = Path.Combine(root, ".git", "j");
            var one = Directory.CreateDirectory(Path.Combine(_dir.Path, "link-one")).FullName;
            var two = Directory.CreateDirectory(Path.Combine(_dir.Path, "link-two")).FullName;
            Assert.True(GitBoundsTests.Junction(link, one));
            return () =>
            {
                Directory.Delete(link);
                Assert.True(GitBoundsTests.Junction(link, two));
            };
        },
        [GitTrust.RiskKind.Alternates] = root =>
        {
            var file = Path.Combine(root, ".git", "objects", "info", "alternates");
            File.WriteAllText(file, "C:/one/objects\n");
            return () => File.WriteAllText(file, "C:/two/objects\n");
        },
        [GitTrust.RiskKind.OutsideTheWorkTree] = root =>
        {
            var one = Repository("outside-one");
            var two = Repository("outside-two");
            var dot = Path.Combine(Gitlink(root), ".git");
            File.WriteAllText(dot, $"gitdir: {Path.Combine(one, ".git")}\n");
            return () => File.WriteAllText(dot, $"gitdir: {Path.Combine(two, ".git")}\n");
        },
        [GitTrust.RiskKind.Legacy] = root =>
        {
            var file = Path.Combine(Directory.CreateDirectory(Path.Combine(root, ".git", "remotes")).FullName, "origin");
            File.WriteAllText(file, "URL: https://one.example.invalid/r.git\n");
            return () => File.WriteAllText(file, "URL: https://two.example.invalid/r.git\n");
        },
        [GitTrust.RiskKind.GitFileNotRead] = root =>
        {
            var dot = Path.Combine(Gitlink(root), ".git");
            File.WriteAllText(dot, "gitdir:one\n");
            return () => File.WriteAllText(dot, "gitdir:two\n");
        },
        [GitTrust.RiskKind.WorkTreeElsewhere] = root =>
        {
            var library = Repository("library-" + Guid.NewGuid().ToString("N")[..8]);
            Git(root, "-c", "protocol.file.allow=always", "submodule", "--quiet", "add", library, "zip");
            Git(root, "commit", "-q", "-m", "a submodule");
            var module = Path.Combine(root, ".git", "modules", "zip");
            var one = Directory.CreateDirectory(Path.Combine(_dir.Path, "elsewhere-one")).FullName;
            var two = Directory.CreateDirectory(Path.Combine(_dir.Path, "elsewhere-two")).FullName;
            Git(module, "config", "core.worktree", one.Replace('\\', '/'));
            return () => Git(module, "config", "core.worktree", two.Replace('\\', '/'));
        },
        [GitTrust.RiskKind.NotRead] = _ => () => { },
        [GitTrust.RiskKind.IndexNotRead] = _ => () => { },
        [GitTrust.RiskKind.TooDeep] = _ => () => { },
    };

    /// <summary>
    /// T2 (H2), the class: for every kind of risk the scan can name, a repository with it, its source changed (the
    /// content of the file, the target of the link, the value of the setting), the risk still named, and the mark
    /// another. Until the seventh pass an alternates file, a legacy remotes file and a link's target changed without
    /// the mark: a trusted repository then read another object store, reached another server or another folder. The
    /// kinds that say axGIT did not read something are not marked, each with its reason.
    /// </summary>
    [GitFact]
    public async Task T2_every_kind_of_risk_marks_its_source()
    {
        var rows = Rows();
        Assert.Equal(Enum.GetValues<GitTrust.RiskKind>().Order(), rows.Keys.Order());
        foreach (var (kind, setup) in rows)
        {
            if (!GitTrust.Marked(kind))
            {
                Assert.False(string.IsNullOrWhiteSpace(GitTrust.NotMarked(kind)), kind.ToString());
                continue;
            }

            var root = Repository("kind-" + kind);
            var change = setup(root);
            var before = await ScanAsync(root);
            Assert.True(before.Known, $"{kind}: {before.Problem}");
            Assert.True(before.Risks.Any(risk => GitTrust.KindOf(risk) == kind), $"{kind} not named: {string.Join(" | ", before.Risks)}");
            change();
            var after = await ScanAsync(root);
            Assert.True(after.Known, $"{kind}: {after.Problem}");
            Assert.True(after.Risks.Any(risk => GitTrust.KindOf(risk) == kind), $"{kind} not named after the change: {string.Join(" | ", after.Risks)}");
            Assert.True(before.Mark != after.Mark, $"{kind}: the source changed and the mark did not");
        }
    }

    /// <summary>T2: each kind of risk is told by its words, a submodule's too; words of no kind are none, and the scan refuses such a risk.</summary>
    [Theory]
    [InlineData("core.fsmonitor", GitTrust.RiskKind.Setting)]
    [InlineData("diff.a b.textconv", GitTrust.RiskKind.Setting)]
    [InlineData("submodule libs/zip: hook post-checkout", GitTrust.RiskKind.Hook)]
    [InlineData("hook pre-commit.exe", GitTrust.RiskKind.Hook)]
    [InlineData("remote.origin.fetch writes outside refs/remotes", GitTrust.RiskKind.FetchOutside)]
    [InlineData("core.excludesfile names another computer, server", GitTrust.RiskKind.OnAnotherComputer)]
    [InlineData("its .git file names a device path", GitTrust.RiskKind.OnAnotherComputer)]
    [InlineData("include.path names a place axGIT does not read", GitTrust.RiskKind.NotReadPlace)]
    [InlineData("submodule x names a place axGIT does not read", GitTrust.RiskKind.NotReadPlace)]
    [InlineData("a link at .git/hooks", GitTrust.RiskKind.Link)]
    [InlineData("a legacy remotes file origin", GitTrust.RiskKind.Legacy)]
    [InlineData("a legacy branches file main", GitTrust.RiskKind.Legacy)]
    [InlineData(GitTrust.Alternates, GitTrust.RiskKind.Alternates)]
    [InlineData(GitTrust.OutsideTheWorkTree, GitTrust.RiskKind.OutsideTheWorkTree)]
    [InlineData(GitTrust.GitFileNotRead, GitTrust.RiskKind.GitFileNotRead)]
    [InlineData("submodule zip: core.worktree", GitTrust.RiskKind.WorkTreeElsewhere)]
    [InlineData(GitTrust.NotRead, GitTrust.RiskKind.NotRead)]
    [InlineData(GitTrust.IndexNotRead, GitTrust.RiskKind.IndexNotRead)]
    [InlineData(GitTrust.TooDeep, GitTrust.RiskKind.TooDeep)]
    public void Each_kind_of_risk_is_told_by_its_words(string risk, GitTrust.RiskKind kind) => Assert.Equal(kind, GitTrust.KindOf(risk));

    [Theory]
    [InlineData("something new")]
    [InlineData("hook ")]
    [InlineData("a link at ")]
    [InlineData("")]
    public void Words_of_no_kind_are_none(string risk) => Assert.Null(GitTrust.KindOf(risk));

    // ---- T4: the statements say exactly what is read before trust ----

    /// <summary>
    /// T4 (M1, M2), the class: what is read before trust is said in one text, <see cref="GitTrust.TrustLimit"/>, and
    /// its first sentence stands in each statement of the bundle's rule 8 and in axGIT's own documents and the
    /// Outside call comment, in the same words (line breaks, comment marks and the case of its first letter aside);
    /// the words of before ("runs no git in it and reaches nothing", "with no repository, and nothing is reached") are
    /// in none of them.
    /// </summary>
    [Theory]
    [InlineData("PRIVACY.md")]
    [InlineData("README.md")]
    [InlineData("SECURITY.md")]
    [InlineData("docs/axKit/README.md")]
    [InlineData("docs/axKit/SPEC.md")]
    [InlineData("docs/axGIT/SPEC.md")]
    [InlineData("docs/axGIT/user-guide.md")]
    [InlineData("src/AxGit.Core/GitRunner.cs")]
    public void The_statements_say_what_is_read_before_trust_in_the_one_text(string file)
    {
        static string Plain(string text) =>
            System.Text.RegularExpressions.Regex.Replace(text.Replace("///", " ", StringComparison.Ordinal).Replace("//", " ", StringComparison.Ordinal), @"\s+", " ");

        var first = GitTrust.TrustLimit[..(GitTrust.TrustLimit.IndexOf(". ", StringComparison.Ordinal) + 1)];
        var core = first["Until you trust a repository, ".Length..^1];
        Assert.StartsWith("Until you trust a repository, axGIT runs no git in it and reaches no server", first, StringComparison.Ordinal);
        var text = Plain(File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), file)));
        Assert.Contains(core, text, StringComparison.Ordinal);
        Assert.DoesNotContain("runs no git in it and reaches nothing", text, StringComparison.Ordinal);
        Assert.DoesNotContain("with no repository, and nothing is reached", text, StringComparison.Ordinal);
    }

    // ---- T3: the mark keeps values as written ----

    /// <summary>
    /// T3 (H3): the mark put every line in lower case, so that a value changed in case alone (a program's arguments,
    /// a path on a file system that tells case, a filter named in other case) kept the trust. Now the values are as
    /// written; a setting's name is in git's own form, its middle part as written ([filter "X"] is another filter for
    /// git than [filter "x"]).
    /// </summary>
    [GitFact]
    public async Task T3_a_value_changed_in_case_alone_changes_the_mark()
    {
        var root = Repository("case");
        Git(root, "config", "core.fsmonitor", "echo ABC");
        var upper = await ScanAsync(root);
        Git(root, "config", "core.fsmonitor", "echo abc");
        Assert.NotEqual(upper.Mark, (await ScanAsync(root)).Mark);

        var config = Path.Combine(root, ".git", "config");
        var plain = File.ReadAllText(config);
        File.WriteAllText(config, plain + "[filter \"X\"]\n\tclean = cat\n");
        var big = await ScanAsync(root);
        File.WriteAllText(config, plain + "[filter \"x\"]\n\tclean = cat\n");
        var small = await ScanAsync(root);
        Assert.NotEqual(big.Mark, small.Mark);

        // A name git reads without case, written in other case: the same setting, the same mark.
        File.WriteAllText(config, plain + "[FILTER \"x\"]\n\tCLEAN = cat\n");
        Assert.Equal(small.Mark, (await ScanAsync(root)).Mark);
    }
}
