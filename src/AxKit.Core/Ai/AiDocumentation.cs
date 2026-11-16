using System.Text;

namespace AxKit.Core.Ai;

/// <summary>
/// axKit's own guides in Claude's working folder (docs/axKit/ai-help.md AH-7; the user's ask of 2026-11-15: "a better
/// developed RAG system by copying the relevant documentation for axKit into the axHelp folder"): the folder
/// <c>documentation</c> with the bundle's guide, one guide per app under the app's name and an index, and the rule
/// file <c>.claude\rules\axkit-documentation.md</c>, which Claude Code reads at every start beside the user's
/// CLAUDE.md and which tells Claude to read the guides before it answers a question about axKit; and, since AH-8, the
/// rule file of axKit's AI tools beside it (<see cref="AiToolsRule"/>), kept current the same way; and, since
/// docs/axKit/user-workflows.md WF-8, the workflow pages beside the guides, from which the rule tells Claude to give a
/// goal's steps as they are written (<see cref="WorkflowPages"/>). Claude Code's own
/// Read, Grep and Glob do the finding: no index service, no embeddings, no network (AH-7.5). The files are axKit's,
/// not the user's: written where missing, and all written again when the version in the folder's marker is not this
/// axKit's, so that they follow each release; a file of the user's own in the folder is never touched. The texts are
/// embedded by AxKit.Core.csproj once more (an app's project keeps its own copy for Help, User guide; this project
/// may not reference an app).
/// </summary>
public static class AiDocumentation
{
    /// <summary>The folder inside the working folder.</summary>
    public const string FolderName = "documentation";

    /// <summary>The list of the guides, one line each, which Claude reads first.</summary>
    public const string IndexName = "index.md";

    /// <summary>The bundle's guide, docs/axKit/README.md.</summary>
    public const string BundleName = "axKit.md";

    /// <summary>The marker: the version of the axKit that wrote the files, alone on one line.</summary>
    public const string VersionName = "axkit-version.txt";

    /// <summary>The rule file, from the working folder: Claude Code loads every .md file of .claude\rules at its start, as it loads CLAUDE.md.</summary>
    public static readonly string RulePath = Path.Combine(".claude", "rules", "axkit-documentation.md");

    /// <summary>The index of the workflow pages (docs/axKit/workflows.md, WF-8.1): what a user can do in each app, as workflows and their goals.</summary>
    public const string WorkflowsName = "workflows.md";

    /// <summary>The commands page (docs/axKit/window-commands.md, WF-8.1), under the name it has in the repository; never commands.md, which there is the developer's commands.</summary>
    public const string WindowCommandsName = "window-commands.md";

    /// <summary>
    /// What the rule file says (AH-7.3), and since WF-8.3 how to answer from the workflow pages: their sentences
    /// follow the guides' word for word. None tells Claude to offer to do the steps (WF-D-24): when Claude may run a
    /// command stays the AI tools' rule file's.
    /// </summary>
    public const string Rule = """
        # axKit's guides

        This file is axKit's: axKit writes it again with each new version, so a change to it is lost. Your own
        instructions for Claude belong in CLAUDE.md, in the folder above.

        The folder documentation in this working folder holds axKit's own guides. index.md there lists them: axKit.md
        is the guide of axKit as a whole, and each app has one file under its name (axFile.md, axClaude.md).

        For a question about axKit or one of its apps (what an app does, a key, a menu, a setting, how to do
        something in a window), read documentation/index.md and the guide that fits with your Read tool before you
        answer, and answer from the guide. When you do not know which guide holds the answer, search the folder with
        your Grep tool. When the guides do not cover the question, say so, and say what you answer from instead.

        workflows.md lists what a user can do in each app: its workflows, and under each its goals. The steps of an
        app's goals, with the keys as they are in this version, are in workflows-<app>.md (workflows-axFile.md).
        window-commands.md lists every command of every window with its menu and its key, one a line.

        When the user asks what axKit or one of its apps can do, answer from workflows.md: name the workflows and
        goals that fit.

        When the user asks how to do something, find its goal in the app's workflows file (search for the goal's
        title with your Grep tool, the files are long) and give its steps as they are written there, keys included.

        The steps come from axKit's own checks: a file name or a word they type is an example. Say so in a few words
        and put the user's own in its place; change nothing else, not a key, not a menu, not the order.

        Never invent a key or a menu: a key you name comes from a workflows file, window-commands.md or a guide.
        Where a guide and window-commands.md name different keys for one command, window-commands.md is right: it is
        written from the windows themselves.

        When no goal covers the question, look in the app's guide and in window-commands.md, and say when you are not
        sure.
        """;

    /// <summary>The file of an app's guide in the folder: the app's name as shown, so that Claude finds it by the name the user says ("axFile.md").</summary>
    public static string GuideName(App app) => app.Name + ".md";

    /// <summary>An app's user guide as AxKit.Core.csproj embeds it (<c>Guide.axFile.md</c>); null for an app whose guide is not embedded, which <c>AiDocumentationTests</c> fails on.</summary>
    public static string? Guide(App app) => Embedded("Guide." + app.Name + ".md");

    /// <summary>The file of a group's workflow page in the folder (WF-8.1): <c>workflows-axFile.md</c> for the page <c>docs/axKit/workflows/axFile.md</c>.</summary>
    public static string WorkflowPageName(string page) => "workflows-" + page + ".md";

    /// <summary>
    /// The workflow pages of the groups as AxKit.Core.csproj embeds them (<c>Workflows.axFile.md</c>), in the order
    /// of <see cref="Dispatch.Apps"/> and then the AI tools' and the NVDA-only page (the page names of
    /// AxAutomate.Core's <c>Reference.PageName</c>): each page's name, the words the index names its goals by, and its
    /// text. Found by trying each name, never by listing the resources; a group without a page is left out.
    /// </summary>
    public static IReadOnlyList<(string Page, string Goals, string Text)> WorkflowPages()
    {
        var pages = new List<(string Page, string Goals, string Text)>();
        var names = Dispatch.Apps.Select(app => (Page: app.Name, Goals: app.Name + "'s goals"))
            .Append(("ai-tools", "the AI tools' goals"))
            .Append(("nvda-only", "the NVDA-only goals"));
        foreach (var (page, goals) in names)
        {
            if (Embedded("Workflows." + page + ".md") is { } text)
            {
                pages.Add((page, goals, text));
            }
        }

        return pages;
    }

    /// <summary>
    /// Every file axKit keeps in the working folder for the guides, each with its path from the working folder and
    /// its text: the bundle's guide, a guide per app of <see cref="Dispatch.Apps"/>, the workflow pages (WF-8.1: the
    /// index of the workflows, each group's page and the commands page), the index, which names
    /// <paramref name="version"/>, and the rule file.
    /// </summary>
    public static IReadOnlyList<(string Path, string Text)> Files(string version)
    {
        var files = new List<(string Path, string Text)>();
        var index = new StringBuilder();
        index.Append("# axKit's guides\n\n");
        index.Append("axKit's own guides, written here by axKit ").Append(version).Append(" for Claude to read. axKit writes these files again with each new version, so a change to one of them is lost; a file of your own in this folder is left alone.\n\n");
        if (Embedded("AxKitGuide.md") is { } bundle)
        {
            files.Add((Path.Combine(FolderName, BundleName), bundle));
            index.Append("- ").Append(BundleName).Append(": axKit as a whole: what the apps are, installing and updating, the menus, keys and notices every window shares, the settings, AI help and the AI tools.\n");
        }

        foreach (var app in Dispatch.Apps)
        {
            if (Guide(app) is { } guide)
            {
                files.Add((Path.Combine(FolderName, GuideName(app)), guide));
                index.Append("- ").Append(GuideName(app)).Append(": ").Append(app.Description).Append(".\n");
            }
        }

        if (Embedded("Workflows.md") is { } workflows)
        {
            files.Add((Path.Combine(FolderName, WorkflowsName), workflows));
            index.Append("- ").Append(WorkflowsName).Append(": what a user can do in each app, as workflows and their goals.\n");
        }

        foreach (var (page, goals, text) in WorkflowPages())
        {
            files.Add((Path.Combine(FolderName, WorkflowPageName(page)), text));
            index.Append("- ").Append(WorkflowPageName(page)).Append(": the steps of ").Append(goals).Append(" with their keys.\n");
        }

        if (Embedded("WindowCommands.md") is { } commands)
        {
            files.Add((Path.Combine(FolderName, WindowCommandsName), commands));
            index.Append("- ").Append(WindowCommandsName).Append(": every command of every window with its menu and its key, one a line.\n");
        }

        files.Insert(0, (Path.Combine(FolderName, IndexName), index.ToString()));
        files.Add((RulePath, Rule));
        // AH-8: the second rule file, when to use which of axKit's AI tools; kept current as the guides are.
        files.Add((AiToolsRule.RulePath, AiToolsRule.Text));
        return files;
    }

    /// <summary>
    /// Whether the working folder has never had the guides: the index or one of the two rule files is missing (the
    /// tools' rule in a folder an axKit before it wrote). Then they must be there before Claude Code starts, which
    /// reads the rules at its start; otherwise a refresh can follow in the background, the files of the version
    /// before serving until it is done.
    /// </summary>
    public static bool IsMissing(string workingFolder) =>
        !File.Exists(Path.Combine(workingFolder, FolderName, IndexName))
        || !File.Exists(Path.Combine(workingFolder, RulePath))
        || !File.Exists(Path.Combine(workingFolder, AiToolsRule.RulePath));

    /// <summary>
    /// The files written into <paramref name="workingFolder"/> (AH-7.1): those that are missing, and every one when
    /// the folder's marker does not hold <paramref name="version"/> or <paramref name="rewrite"/> asks for it (a
    /// development build, whose guides change without its version). The marker holds the version on its first line
    /// and then the path of every file axKit wrote, one a line; it is written last, so that a write that failed half
    /// way is done again. A file an earlier axKit wrote and this one no longer has (an app that left the bundle) is
    /// deleted, by the marker's list alone: a file the marker does not name is the user's and is never read, changed
    /// or deleted. Returns the paths written, from the working folder; empty when everything was current. Throws as
    /// <see cref="File.WriteAllText(string, string)"/> does.
    /// </summary>
    public static IReadOnlyList<string> Write(string workingFolder, string version, bool rewrite = false)
    {
        var marker = Path.Combine(workingFolder, FolderName, VersionName);
        var before = File.Exists(marker) ? File.ReadAllLines(marker) : [];
        var current = !rewrite && before.Length > 0 && before[0].Trim() == version;
        var files = Files(version);
        var written = new List<string>();
        foreach (var (name, text) in files)
        {
            var path = Path.Combine(workingFolder, name);
            if (current && File.Exists(path))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text.ReplaceLineEndings("\r\n").TrimEnd() + "\r\n");
            written.Add(name);
        }

        if (!current)
        {
            var names = files.Select(file => file.Path).ToList();
            foreach (var old in before.Skip(1).Select(line => line.Trim()).Where(line => IsOurs(line) && !names.Contains(line, StringComparer.OrdinalIgnoreCase)))
            {
                File.Delete(Path.Combine(workingFolder, old));
            }

            File.WriteAllLines(marker, [version, .. names]);
        }

        return written;
    }

    /// <summary>
    /// Whether a path a marker names is one axKit could have written: a Markdown file directly in the documentation
    /// folder, or one of the two rule files. A marker is a file in the user's folder and could hold anything; nothing
    /// outside these places is ever deleted on its word.
    /// </summary>
    public static bool IsOurs(string path) =>
        string.Equals(path, RulePath, StringComparison.OrdinalIgnoreCase)
        || string.Equals(path, AiToolsRule.RulePath, StringComparison.OrdinalIgnoreCase)
        || (string.Equals(Path.GetDirectoryName(path), FolderName, StringComparison.OrdinalIgnoreCase)
            && Path.GetFileName(path) is { Length: > 3 } name
            && name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0);

    private static string? Embedded(string name)
    {
        using var stream = typeof(AiDocumentation).Assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
