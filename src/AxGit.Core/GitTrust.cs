using System.Text.RegularExpressions;

namespace AxGit.Core;

/// <summary>One setting of a settings file as git's parser read it (<see cref="GitTrust.ParseSettings"/>): its name as git lists it, and its value; null for a name written without one, which git reads as true.</summary>
public sealed record FileSetting(string Name, string? Value);

/// <summary>
/// A repository's own programs (SPEC.md AG-1.6; the security audit of 2026-10-24 S-2). A repository that comes in
/// an archive, on a USB stick or a share brings its own <c>.git/config</c> and <c>.git/hooks</c>, which git obeys: a
/// file monitor run by <c>git status</c>, diff and filter programs, hooks on commit and switch, an ssh command on
/// fetch, a settings file or an object store on another computer. This class holds the rules: what is a risk, how
/// a risk is named and said, what is set aside; the reads of a repository's files, in their order, are
/// <see cref="GitTrustScan"/>.
///
/// The line since the review's fourth pass (2026-11-28, AG-1.6): before the user trusts a repository, axGIT runs no
/// git command in it at all; since the sixth pass (G1) that holds for every repository, also one where nothing was
/// found, which is asked about once like any other. Three rounds had each found a way in which git, run in such a
/// repository with its programs set aside by name, still ran one or reached another computer (core.unsetenvvars
/// cleared every setting given to a submodule's run; a push into a folder ran that other repository's hooks; a
/// legacy remotes file; an include on a share). What the check needs is read by axKit's own code from the files on
/// disk, and the settings files with git as a parser of one file given by its path
/// (<see cref="ParserArguments"/>), which runs nothing of the repository. Whatever cannot be read that way is a
/// risk named in the notice.
///
/// What counts (the review's second pass, AG-1.6c): every setting whose name is not in the table of the harmless
/// ones (<see cref="Inert"/>), since a list of the names that run a program can never be complete; every hook; and
/// the places (<see cref="Places"/>): a link, an object store or a settings file elsewhere, a legacy remotes file,
/// a value that names another computer.
/// </summary>
public static partial class GitTrust
{
    // ---- git as a parser of one file (AG-1.6, the review's fourth pass, T1) ----

    /// <summary>
    /// git as a parser of the settings file at <paramref name="file"/>, a full path: every setting in it, its
    /// includes not followed, each ended by a zero character (name, a line break, the value). Run with no
    /// repository at all (<see cref="GitTrustScan"/> gives <c>GIT_DIR</c> a folder that does not exist), it reads
    /// that one file and runs nothing (tried with git 2.53 on a file that set every name of
    /// <c>git help --config</c> to a program, core.unsetenvvars and core.fsmonitor among them; the class test in
    /// <c>GitTrustScanTests</c> holds it).
    /// </summary>
    public static IReadOnlyList<string> ParserArguments(string file) => ["config", "--file", file, "--no-includes", "--list", "-z"];

    /// <summary>The settings of git's answer to <see cref="ParserArguments"/>, in the order of the file.</summary>
    public static IReadOnlyList<FileSetting> ParseSettings(string output)
    {
        var settings = new List<FileSetting>();
        foreach (var entry in output.Split('\0'))
        {
            if (entry.Trim().Length == 0)
            {
                continue;
            }

            var line = entry.IndexOf('\n');
            settings.Add(line < 0 ? new FileSetting(entry.Trim(), null) : new FileSetting(entry[..line].Trim(), entry[(line + 1)..]));
        }

        return settings;
    }

    // ---- The hooks folder of a run that must run none ----

    /// <summary>
    /// The hooks folder a run is given when it must run no hook (<see cref="Careful"/>): a folder of axKit's own
    /// under the local application data, <c>%LOCALAPPDATA%\AxKit\axGIT-no-hooks</c>, a full path, made when missing
    /// and emptied before each use (<see cref="EmptyHooksFolder"/>). The tests point it at a folder of their own.
    /// </summary>
    public static string NoHooksFolder { get; set; } = Path.Combine(Path.GetDirectoryName(AxKit.Core.AppPaths.LogsFolder)!, "axGIT-no-hooks");

    /// <summary>What the name of the fresh hooks folder starts with that is given when <see cref="NoHooksFolder"/> cannot be emptied, in the temporary folder; the tests add their run's mark (test.ps1 counts them).</summary>
    public static string FallbackPrefix { get; set; } = "axGIT-no-hooks-";

    /// <summary>
    /// <see cref="NoHooksFolder"/>, made when missing and with anything found in it removed, as a full path. When
    /// what is in it cannot be removed, a fresh empty folder under the temporary folder is given instead, which
    /// <see cref="ReleaseHooksFolder"/> deletes after the run: a folder that holds a file is never a hooks folder of
    /// axGIT's.
    /// </summary>
    public static string EmptyHooksFolder()
    {
        try
        {
            var folder = Directory.CreateDirectory(NoHooksFolder);
            foreach (var entry in folder.EnumerateFileSystemInfos())
            {
                if (entry is DirectoryInfo inside)
                {
                    inside.Delete(recursive: true);
                }
                else
                {
                    entry.Attributes = FileAttributes.Normal;
                    entry.Delete();
                }
            }

            if (!folder.EnumerateFileSystemInfos().Any())
            {
                return folder.FullName;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A file another program holds: a fresh folder below.
        }

        return Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), FallbackPrefix + Guid.NewGuid().ToString("N"))).FullName;
    }

    /// <summary>A hooks folder of <see cref="EmptyHooksFolder"/> after its run: the fresh one of the temporary folder deleted, <see cref="NoHooksFolder"/> left.</summary>
    public static void ReleaseHooksFolder(string folder)
    {
        if (!Path.GetFileName(folder).StartsWith(FallbackPrefix, StringComparison.Ordinal)
            || !string.Equals(Path.GetDirectoryName(Path.GetFullPath(folder)), Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            Directory.Delete(folder, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left for the next start's own fallback; it holds nothing of the repository's.
        }
    }

    // ---- How deep the scan goes ----

    /// <summary>How many levels of folders under <c>.git/modules</c>, and of repositories inside each other, the scan follows; what lies deeper is named as not read.</summary>
    public const int Deep = 12;

    /// <summary>How many levels of folders of a <c>.git</c> folder the scan walks for links; a folder deeper than this that holds folders is a risk (<see cref="TooDeep"/>).</summary>
    public const int TreeDeep = 64;

    // ---- How a risk is named ----

    /// <summary>What a risk inside a submodule starts with: "submodule libs/zip: core.sshcommand".</summary>
    private const string SubmoduleMark = "submodule ";

    /// <summary>A repository inside whose settings git's parser could not read: nothing of it is known, so it is named as it is.</summary>
    public const string NotRead = "settings that could not be read";

    /// <summary>An index whose form axGIT does not read (<see cref="GitIndex"/>; the fifth pass, P4; read strictly since the sixth, G3): which repositories inside git would enter, or which places it looks at, is not known.</summary>
    public const string IndexNotRead = "an index axGIT does not read";

    /// <summary>A repository inside whose .git file is not written as git reads one (the sixth pass, H6, G3): where git would go from it is not known.</summary>
    public const string GitFileNotRead = "a .git file axGIT does not read";

    /// <summary>
    /// A repository inside whose core.worktree names another folder than the one of the gitlink it was reached from
    /// (the sixth pass, H3): git, entering it, works on that folder. Named as git lists the setting.
    /// </summary>
    public const string WorkTreeElsewhere = "core.worktree";

    /// <summary>A path of the repository's that is not in a positive local form, which is never read (<see cref="RepositoryFiles"/>; the fifth pass, P2): "include.path names a place axGIT does not read".</summary>
    public static string NotReadPlace(string what) => $"{what} names {RepositoryFiles.NotRead}";

    /// <summary>Folders or repositories deeper than the scan goes (<see cref="TreeDeep"/>, <see cref="Deep"/>): what is in them is not known.</summary>
    public const string TooDeep = "folders deeper than axGIT looks";

    /// <summary>An object store of another repository that git reads (<c>objects/info/alternates</c>).</summary>
    public const string Alternates = "object stores of other repositories (alternates)";

    /// <summary>A <c>.git</c> file, or a <c>commondir</c> file, that names a folder outside the work tree: git reads the repository there.</summary>
    public const string OutsideTheWorkTree = "a .git file or commondir file that names a folder outside the work tree";

    /// <summary>A link (a symbolic link or a junction) in the <c>.git</c> folder or the work tree, which the scan does not follow: "a link at .git/hooks".</summary>
    public static string Link(string place) => $"a link at {place}";

    /// <summary>A file of git's older way of naming remotes (<c>.git/remotes</c>, <c>.git/branches</c>), which git still obeys: "a legacy remotes file origin".</summary>
    public static string Legacy(string kind, string name) => $"a legacy {kind} file {name}";

    /// <summary>A place that is not on this computer, in a setting's value or a <c>.git</c> file: "core.excludesfile names another computer, server".</summary>
    public static string OnAnotherComputer(string what, string host) =>
        host == "." ? $"{what} names a device path" : $"{what} names another computer, {host}";

    /// <summary>The risks of a repository inside the one opened, each named with it: "submodule libs/zip: hook post-checkout".</summary>
    public static IReadOnlyList<string> Inside(string submodule, IEnumerable<string> risks) =>
        [.. risks.Select(risk => $"{SubmoduleMark}{submodule}: {risk}")];

    /// <summary>The risk without the submodule it is in. A path on Windows holds no colon after a drive, so the first ": " ends the submodule's name.</summary>
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
    /// is a risk (<see cref="SettingRisks"/>). What an everyday repository carries is here, as made by <c>git init</c>,
    /// <c>git clone</c>, <c>git submodule</c>, Git for Windows, GitHub's gh, Visual Studio Code and Git LFS, so that
    /// the notice of an ordinary repository says it found nothing of concern (since the sixth pass, G1, every
    /// repository is asked about once; until then such a one opened without the notice). <c>include.path</c> and <c>includeIf</c> are never here:
    /// what they bring in changes with the branch and with any file they name. A new entry needs a reason that
    /// shows it runs no program, reaches no server and moves no file's place; <c>GitTrustTests</c> holds the form.
    /// A value of a harmless name that names another computer is a risk all the same (<see cref="ValueRisks"/>).
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
        new("core.worktree", "Where a submodule's work tree lies, written by git for each one; harmless only where it names the folder of the gitlink the submodule was reached from, which the scan checks (any other is a risk, the sixth pass, H3).", InsideOnly: true),
        new("remote.*.url", "A remote's address: read through git and named in the reminder before axGIT reaches it (AG-5.4a)."),
        new("remote.*.pushurl", "A remote's address for a push: read through git and named in the reminder (AG-5.4a)."),
        new("remote.*.fetch", "Which branches of the remote a fetch brings, and under which names; axGIT's fetch names its own (AG-5.4)."),
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

    // ---- What runs a program, and how the notice says the risks ----

    /// <summary>
    /// What trust means, said plainly and once, in these words wherever it is said (the review's sixth pass,
    /// 2026-11-28, G2; the notice says it every time, AG-1.6 and the statements of the bundle's rule 8 in the same
    /// terms). Before trust axGIT runs no git in the repository and reaches nothing; after it git works there as git
    /// does: it runs what the repository's settings and hooks name, and it reads the files and places its settings
    /// and its index name, which may lie on another computer. axGIT's scan reads the repository's files itself and
    /// names what it finds, and a reading of git's formats in C# is never complete, so the trust is the line, not the
    /// scan (until the fifth pass a repository with nothing found was trusted without asking).
    /// </summary>
    public const string TrustLimit = "Until you trust a repository, axGIT runs no git in it and reaches nothing. Once you trust it, git works in it as git does: it runs the programs and hooks the repository's settings name, whatever they run in turn, and axGIT sets aside only those whose path is in the work tree, on the commands that change the repository; and git reads the files and places those settings and its index name, which can be on another computer.";

    /// <summary>What the notice says of a repository whose scan found nothing (G1): asked about all the same.</summary>
    public const string NothingFound = "axGIT found nothing of concern in it. It reads the repository's files itself and cannot promise that it found everything.";

    /// <summary>Whether a configuration name makes git run a program: said so in the notice, and set to nothing by its name where it lies in the work tree (<see cref="Overrides"/>).</summary>
    public static bool RunsAProgram(string name) => ProgramNames.IsMatch(name.Trim());

    /// <summary>Whether a configuration name sends git's connection through a proxy (AG-5.4b).</summary>
    public static bool NamesAProxy(string name) => ProxyNames().IsMatch(name.Trim());

    /// <summary>What a risk that is a hook starts with: "hook pre-commit".</summary>
    private const string HookMark = "hook ";

    /// <summary>
    /// The risks that are a program git would run, for the notice: a hook, or a setting whose name runs one
    /// (<see cref="RunsAProgram"/>).
    /// </summary>
    public static IReadOnlyList<string> Programs(IReadOnlyList<string> risks) => [.. risks.Where(IsProgram)];

    /// <summary>
    /// The risks that are a place git would go to that the scan does not read: a link, an object store or a
    /// settings file elsewhere, a legacy remotes file, a value on another computer, a repository inside that could
    /// not be read, folders too deep. Each is named in words that are no setting's name (a setting's name has its
    /// first dot before any space), or as a setting's name followed by what its value names.
    /// </summary>
    public static IReadOnlyList<string> Places(IReadOnlyList<string> risks) => [.. risks.Where(risk => !IsProgram(risk) && IsPlace(Own(risk)))];

    /// <summary>The risks that are neither: settings that are not in <see cref="Inert"/>.</summary>
    public static IReadOnlyList<string> NotKnown(IReadOnlyList<string> risks) => [.. risks.Where(risk => !IsProgram(risk) && !IsPlace(Own(risk)))];

    private static bool IsProgram(string risk) =>
        Own(risk) is var own && (own.StartsWith(HookMark, StringComparison.Ordinal) || RunsAProgram(own));

    private static bool IsPlace(string own) =>
        own.IndexOf('.') is var dot && own.IndexOf(' ') is var space && (dot < 0 || (space >= 0 && space < dot)
            || own.Contains(" names another computer, ", StringComparison.Ordinal) || own.EndsWith(" names a device path", StringComparison.Ordinal)
            || own.EndsWith(" names " + RepositoryFiles.NotRead, StringComparison.Ordinal));

    /// <summary>
    /// The names of the settings of one repository (<paramref name="settings"/>, from its own files) that are not
    /// known to be harmless (<see cref="IsInert"/>), each once by its exact text: a name's middle part ("pdf" in
    /// diff.pdf.textconv) is another name in other case. <paramref name="inside"/>: a repository inside the one opened.
    /// </summary>
    public static IReadOnlyList<string> SettingRisks(IReadOnlyList<FileSetting> settings, bool inside = false) =>
        [.. settings.Select(setting => setting.Name).Where(name => !IsInert(name, inside)).Distinct(StringComparer.Ordinal)];

    /// <summary>Whether a hooks folder's entry is a hook: every file but git's own samples.</summary>
    public static bool IsHook(string fileName) => !fileName.EndsWith(".sample", StringComparison.OrdinalIgnoreCase);

    /// <summary>A hook as a risk: "hook pre-commit".</summary>
    public static string Hook(string fileName) => HookMark + fileName;

    // ---- Where the value decides the harm (D5; the review's fourth pass, M2) ----

    /// <summary>
    /// The risks in the values of a repository's settings:
    /// <list type="bullet">
    /// <item>a fetch line that writes outside <c>refs/remotes/</c>: "remote.origin.fetch writes outside
    /// refs/remotes". axGIT's own fetch names its line since the fourth pass (T3), so such a line no longer moves
    /// a branch; it is still asked about, since a fetch in a console obeys it.</item>
    /// <item>any value that names a place on another computer (<see cref="ElsewhereOf"/>): a share, a path through
    /// <c>\\?\</c> other than a drive's, a drive that is a network drive, a <c>file://</c> address with a host.
    /// git reads such a place at its first command (tried with git 2.53 through <c>//localhost/C$</c>:
    /// core.excludesFile, an include, the work tree, the attributes, the mail map, the commit template).</item>
    /// </list>
    /// <paramref name="own"/>: the computer the repository itself lies on (<see cref="ComputerOf"/>), whose places
    /// are its own; <paramref name="isNetworkDrive"/> says whether a drive letter is a network drive.
    /// </summary>
    public static IReadOnlyList<string> ValueRisks(IReadOnlyList<FileSetting> settings, string? own, Func<char, bool> isNetworkDrive)
    {
        var risks = new List<string>();
        foreach (var setting in settings)
        {
            var name = setting.Name.Trim();
            var value = setting.Value ?? string.Empty;
            string? risk = null;
            if (Shape(name) == "remote.*.fetch" && !FetchesUnderRemotes(value))
            {
                risk = $"{name} writes outside refs/remotes";
            }
            else if (ElsewhereOf(value, own, isNetworkDrive) is { } host)
            {
                risk = OnAnotherComputer(name, host);
            }

            if (risk is not null && !risks.Contains(risk, StringComparer.Ordinal))
            {
                risks.Add(risk);
            }
        }

        return risks;
    }

    /// <summary>Whether a fetch line writes under refs/remotes/ alone: no destination, a line that leaves branches out (a caret), or a destination there with no step up.</summary>
    private static bool FetchesUnderRemotes(string refspec)
    {
        var line = refspec.Trim().TrimStart('+');
        if (line.StartsWith('^'))
        {
            return true;
        }

        var colon = line.IndexOf(':');
        if (colon < 0)
        {
            return true;
        }

        var destination = line[(colon + 1)..].Trim();
        return destination.StartsWith("refs/remotes/", StringComparison.Ordinal) && !destination.Split('/').Contains("..");
    }

    // ---- Places on another computer (D9; the fourth pass, M2) ----

    /// <summary>
    /// The computer a path names when it is a share (<c>\\host\share</c>, <c>//host/share</c>, <c>\\?\UNC\host</c>),
    /// in lower case; "." for a device path (<c>\\?\</c> other than a drive's, <c>\\.\</c>); null for a path on
    /// this computer. Read from the text alone: the path is never touched.
    /// </summary>
    public static string? OtherComputer(string path)
    {
        var text = path.Trim().Trim('"');
        if (text.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
        {
            text = @"\\" + text[8..];
        }
        else if (text.StartsWith(@"\\?\", StringComparison.Ordinal) || text.StartsWith(@"\\.\", StringComparison.Ordinal))
        {
            return text.Length > 6 && text[5] == ':' ? null : ".";
        }

        if (text.Length < 3 || text[0] is not ('/' or '\\') || text[1] is not ('/' or '\\'))
        {
            return null;
        }

        var end = text.IndexOfAny(['/', '\\'], 2);
        var host = (end < 0 ? text[2..] : text[2..end]).ToLowerInvariant();
        return host.Length == 0 ? "." : host;
    }

    /// <summary>
    /// The computer a value names when it is not <paramref name="own"/>'s: <see cref="OtherComputer"/> of the
    /// value, of the path in a <c>file://</c> address (a host there is another computer, also localhost, which is
    /// reached through a share), or "drive Z:" for a path on a drive <paramref name="isNetworkDrive"/> calls a
    /// network drive; null for a value that names nothing elsewhere. Read from the text alone.
    /// </summary>
    public static string? ElsewhereOf(string value, string? own, Func<char, bool> isNetworkDrive)
    {
        var text = value.Trim().Trim('"', '\'');
        if (text.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            var rest = text[7..];
            if (!rest.StartsWith('/') && rest.IndexOf('/') is var slash && (slash < 0 ? rest : rest[..slash]) is { Length: > 0 } host)
            {
                return host.ToLowerInvariant() == own ? null : host.ToLowerInvariant();
            }

            text = rest.Length > 2 && rest[0] == '/' && rest[2] == ':' ? rest[1..] : rest;
        }

        var computer = OtherComputer(text);
        if (computer is null && text.Length >= 2 && text[1] == ':' && char.IsAsciiLetter(text[0]) && isNetworkDrive(char.ToUpperInvariant(text[0])))
        {
            computer = $"drive {char.ToUpperInvariant(text[0])}:";
        }

        return computer is not null && computer != own ? computer : null;
    }

    /// <summary>The computer a repository's own folder lies on, as <see cref="ElsewhereOf"/> names one; null for this computer.</summary>
    public static string? ComputerOf(string root, Func<char, bool> isNetworkDrive) => ElsewhereOf(root, null, isNetworkDrive);

    /// <summary>
    /// The words of a setting's value that may name a file: those with a slash or a backslash in them, the quotes
    /// taken off (<c>sh "./tools/clean.sh" --x</c> gives <c>./tools/clean.sh</c>), and since the sixth pass (M1) those
    /// that start with <c>~</c> or <c>%(</c>, or with a drive and a colon (<c>C:tool</c>), which name a file in a way
    /// the scan reads only in part. A bare program name is looked for by git in the PATH, whose folders are the user's.
    /// </summary>
    public static IReadOnlyList<string> PathWords(string value)
    {
        var words = new List<string>();
        var word = new System.Text.StringBuilder();
        char? quote = null;
        void End()
        {
            if (word.Length > 0 && word.ToString() is var text
                && (text.Contains('/') || text.Contains('\\') || text[0] == '~' || text.StartsWith("%(", StringComparison.Ordinal)
                    || (text.Length >= 2 && char.IsAsciiLetter(text[0]) && text[1] == ':')))
            {
                words.Add(text);
            }

            word.Clear();
        }

        foreach (var c in value)
        {
            if (quote is null && (c == '"' || c == '\''))
            {
                quote = c;
            }
            else if (c == quote)
            {
                quote = null;
            }
            else if (quote is null && char.IsWhiteSpace(c))
            {
                End();
            }
            else
            {
                word.Append(c);
            }
        }

        End();
        return words;
    }

    // ---- What git is run with where a program lies in the work tree (T4) ----

    /// <summary>
    /// No hooks for a run (an empty folder of axKit's own, <see cref="EmptyHooksFolder"/>, emptied again for each
    /// run, the caller giving it back with <see cref="ReleaseHooksFolder"/>). Until the fourth pass this held an
    /// empty credential helper too, for the runs of a repository that was not trusted; there are none now (T1).
    /// </summary>
    public static IReadOnlyList<GitSetting> Careful => [new("core.hooksPath", EmptyHooksFolder())];

    /// <summary>
    /// The settings a run is given to set <paramref name="risks"/> aside, through the environment
    /// (<see cref="GitRunner.SetOverrides"/>), where a name may hold an equals sign; empty when
    /// <paramref name="risks"/> is. The second line behind T1: since the fourth pass no run is made in a repository
    /// the user has not trusted, and this is what a trusted repository's changing commands run with when a program
    /// or its hooks folder lies in its work tree (<see cref="TrustScan.InTree"/>, T4), where a switch or a pull can
    /// change them while git runs (a switch brought a post-checkout hook and ran it, M3).
    /// <list type="bullet">
    /// <item>Every name that runs a program or names a proxy, set to nothing, once, whichever repository it was
    /// found in (the settings reach git's runs inside a submodule; with core.unsetenvvars given on the command line
    /// by every run, <see cref="GitRunner.Command"/>, a repository's own can no longer clear them, H1).</item>
    /// <item><c>protocol.git.allow=never</c> when <c>core.gitProxy</c> is named: git takes the first proxy command
    /// it reads, the repository's, so an empty one after it sets nothing aside.</item>
    /// <item>When a risk is no hook and no such name: every filter of <paramref name="filters"/> set to nothing and
    /// not required (a filter may obey the repository's settings, as Git LFS does).</item>
    /// <item><see cref="Careful"/>: no hooks.</item>
    /// </list>
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
            if (SettingsHook().Match(risk) is { Success: true } hook)
            {
                // L7 (the fifth pass): a hook of the settings (git 2.54 and later) with an empty command is one git
                // cannot start, and the command stopped; git documents two switches for it instead (2.54.0, 2.56.1):
                // an empty event empties its list of events, and enabled=false turns it off. Older git knows neither.
                // The sixth pass (L2) read git-hook's page again (git-scm.com, 2.55.0, unchanged to 2.56.1): it also
                // documents hook.<event>.enabled=false, "no hooks fire for that event", the broader switch, which turns
                // off every hook of an event, the repository's own hooks folder's too, and needs each event named;
                // the two per-hook switches are the plainer ones, and stay. Not runnable on git 2.53, which knows none.
                if (names.Add(risk))
                {
                    list.Add(new GitSetting($"hook.{hook.Groups[1].Value}.event", string.Empty));
                    list.Add(new GitSetting($"hook.{hook.Groups[1].Value}.enabled", "false"));
                }

                continue;
            }

            // The hooks folder is set below.
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

    /// <summary>The parts of a filter's settings that name its programs.</summary>
    private static readonly string[] FilterPrograms = ["clean", "smudge", "process"];

    /// <summary>
    /// The mark of what a repository would run (AG-1.6, repairs.md R-53; the fourth pass, T4): the SHA-256, as
    /// hexadecimal, of the lines given in lower case and sorted, each with its length in front. The scan gives
    /// the risks and, beside them, the value of every setting that is not harmless, the content's hash of every
    /// hook and of every file such a setting names (included settings files among them), and each hooks folder's
    /// listing (<see cref="TrustScan.Mark"/>). A trust is kept with it, so that a repository whose hook, program
    /// or included file changed is asked about again; until the fourth pass it marked the names alone.
    /// </summary>
    public static string Mark(IEnumerable<string> lines) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            AxKit.Core.SettingsSeal.Text(lines.Select(line => line.ToLowerInvariant()).Order(StringComparer.Ordinal)))));

    /// <summary>A kind of setting that names a program: its pattern (a regular expression over the whole name, without case) and names of that kind, written out.</summary>
    public sealed record ProgramName(string Pattern, IReadOnlyList<string> Examples);

    /// <summary>The sections whose drivers and tools name programs, and the variables that do.</summary>
    private static readonly string[] DriverSections = ["diff", "merge", "filter", "difftool", "mergetool"];

    private static readonly string[] DriverVariables = ["command", "textconv", "driver", "clean", "smudge", "process", "cmd", "path"];

    /// <summary>
    /// The names known to run a program when one of the commands axGIT runs needs it: the core programs; the
    /// external diff; diff, merge and filter drivers and the tools' commands; a pager for one command; credential
    /// helpers; the signing programs and the command that finds an ssh signing key; the editors; a commit
    /// trailer's command; a hook set in the configuration; the remote's own programs and its helper
    /// (<c>vcs</c>). This list decides no longer whether a repository is asked about (every name outside
    /// <see cref="Inert"/> is): it says which risks the notice calls programs, and which are set to nothing by
    /// their name where they lie in the work tree (T4). Each kind carries names of it written out, and the test of
    /// the second line (<c>GitSessionTests</c>) sets every one of them to a program that leaves a mark and runs
    /// every command axGIT has with <see cref="Overrides"/>: the mark never comes.
    /// </summary>
    public static readonly IReadOnlyList<ProgramName> ProgramNamesTable =
    [
        new(@"core\.(fsmonitor|hookspath|sshcommand|pager|editor|askpass|gitproxy|alternaterefscommand)",
            ["core.fsmonitor", "core.hooksPath", "core.sshCommand", "core.pager", "core.editor", "core.askPass", "core.gitProxy", "core.alternateRefsCommand"]),
        new(@"diff\.external", ["diff.external"]),
        new($@"({string.Join('|', DriverSections)})\..+\.({string.Join('|', DriverVariables)})",
            [.. DriverSections.SelectMany(section => DriverVariables.Select(variable => $"{section}.x.{variable}"))]),
        new(@"pager\..+", ["pager.status", "pager.log", "pager.diff", "pager.show", "pager.branch", "pager.commit"]),
        new(@"credential(\..+)?\.helper", ["credential.helper", "credential.https://cred.example.invalid.helper"]),
        new(@"gpg(\..+)?\.program", ["gpg.program", "gpg.openpgp.program", "gpg.ssh.program", "gpg.x509.program"]),
        new(@"gpg\.ssh\.defaultkeycommand", ["gpg.ssh.defaultKeyCommand"]),
        new(@"sequence\.editor", ["sequence.editor"]),
        new(@"trailer\..+\.(command|cmd)", ["trailer.x.command", "trailer.x.cmd"]),
        new(@"hook\..+\.command", ["hook.x.command"]),
        new(@"remote\..+\.(uploadpack|receivepack|proxy|vcs)", ["remote.origin.uploadpack", "remote.origin.receivepack", "remote.origin.proxy", "remote.origin.vcs"]),
        new(@"uploadpack\.packobjectshook", ["uploadpack.packObjectsHook"]),
    ];

    /// <summary>The kinds of <see cref="ProgramNamesTable"/> as one expression over the whole name.</summary>
    private static readonly Regex ProgramNames =
        new("^(" + string.Join('|', ProgramNamesTable.Select(kind => kind.Pattern)) + ")$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>A proxy for http and https, for every address or for those under one (<c>http.proxy</c>, <c>http.address.proxy</c>); a remote's own, <c>remote.name.proxy</c>, is among the program names.</summary>
    [GeneratedRegex(@"^http(\..+)?\.proxy$", RegexOptions.IgnoreCase)]
    private static partial Regex ProxyNames();

    /// <summary>A hook of the settings, <c>hook.&lt;name&gt;.command</c>, with its name (which may hold dots) as git lists it.</summary>
    [GeneratedRegex(@"^hook\.(.+)\.command$", RegexOptions.IgnoreCase)]
    private static partial Regex SettingsHook();
}
