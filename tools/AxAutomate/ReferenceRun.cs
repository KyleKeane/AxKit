using System.Text;
using AxAutomate.Core;

namespace AxAutomate;

/// <summary>
/// The pages for people written or checked (docs/axKit/user-workflows.md WF-7.1, WF-7.2, section 6.4):
/// <c>AxAutomate --reference [--check]</c>, which <c>tools/workflows.ps1</c> runs. It plays nothing and takes no lock.
/// It reads every script of <c>tests/scripts</c> and <c>tests/nvda-scripts</c> and the command inventory
/// (<c>docs/axKit/window-commands.json</c>, none when the file is missing), and <see cref="Reference.Pages"/> makes the
/// pages from them. Written, each page has CRLF line ends and is UTF-8 without a byte order mark, and a <c>.md</c> file
/// of <c>docs/axKit/workflows</c> that is no page any more is deleted. With <c>--check</c> nothing is written or
/// deleted: each page is compared with its file, line ends aside. The exit code is 0 when the pages were written or are
/// current, 1 when they are not current or cannot be made (a script without its label, an inventory that is no
/// inventory).
/// </summary>
internal static class ReferenceRun
{
    /// <summary>The folders of the scripts, from the repository's root: every <c>*.txt</c> under them is a workflow.</summary>
    private static readonly string[] ScriptFolders = ["tests/scripts", "tests/nvda-scripts"];

    /// <summary>
    /// The pages written into <paramref name="repository"/>, or with <paramref name="check"/> compared with what is
    /// there; each line of the outcome is said. Returns the exit code.
    /// </summary>
    public static int Run(string repository, bool check, Action<string> say)
    {
        var scripts = Scripts(repository);
        var (inventory, unreadable) = Inventory(repository);
        if (unreadable is not null)
        {
            say(unreadable);
            return 1;
        }

        IReadOnlyList<ReferencePage> pages;
        (int Workflows, int Goals) counts;
        try
        {
            pages = Reference.Pages(scripts, inventory);
            counts = Reference.Counts(scripts);
        }
        catch (InvalidOperationException refused)
        {
            // A script without its label: the generator names every one (docs/axAutomate/SPEC.md AU-5.2a).
            say(refused.Message);
            say("A script's first line is its label, # Workflow: <a task in the user's words> (docs/axAutomate/SPEC.md AU-5.2a).");
            return 1;
        }

        var leftOver = LeftOver(repository, pages);
        return check ? Check(repository, pages, leftOver, counts, say) : Write(repository, pages, leftOver, counts, say);
    }

    /// <summary>Every page compared with its file, line ends aside; each page missing or different, and each one left over, a line.</summary>
    private static int Check(string repository, IReadOnlyList<ReferencePage> pages, IReadOnlyList<string> leftOver, (int Workflows, int Goals) counts, Action<string> say)
    {
        var notCurrent = new List<string>();
        foreach (var page in pages)
        {
            var file = Full(repository, page.Path);
            if (!File.Exists(file))
            {
                notCurrent.Add($"{page.Path} is missing.");
            }
            else if (Lf(File.ReadAllText(file)) != page.Text)
            {
                notCurrent.Add($"{page.Path} is not what the scripts and the command inventory give.");
            }
        }

        notCurrent.AddRange(leftOver.Select(path => $"{path} is no page any more: no folder of scripts makes it."));
        if (notCurrent.Count > 0)
        {
            foreach (var line in notCurrent)
            {
                say(line);
            }

            say("Run tools\\workflows.ps1, and commit the pages it writes with the change that made them differ (docs/axKit/user-workflows.md WF-11.4).");
            return 1;
        }

        say($"{Reference.IndexPath} and {pages.Count - 1} pages are current: {counts.Workflows} workflows, {counts.Goals} goals.");
        return 0;
    }

    /// <summary>The pages left over deleted, then every page written with CRLF, UTF-8 without a byte order mark; one line per page.</summary>
    private static int Write(string repository, IReadOnlyList<ReferencePage> pages, IReadOnlyList<string> leftOver, (int Workflows, int Goals) counts, Action<string> say)
    {
        // First the files left over: a group renamed in case alone ("axGit" to "axGIT") would otherwise keep the old
        // file's name, since Windows writes into the file of the same name in another case.
        foreach (var path in leftOver)
        {
            File.Delete(Full(repository, path));
            say($"Deleted {path}: no folder of scripts makes it any more.");
        }

        foreach (var page in pages)
        {
            var file = Full(repository, page.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var text = page.Text.Replace("\n", "\r\n", StringComparison.Ordinal);
            File.WriteAllText(file, text, new UTF8Encoding(false));
            say($"Wrote {page.Path}: {Encoding.UTF8.GetByteCount(text):N0} bytes.");
        }

        say($"{counts.Workflows} workflows, {counts.Goals} goals.");
        return 0;
    }

    /// <summary>Every script of the two folders, its path from the repository's root with forward slashes.</summary>
    private static List<ReferenceScript> Scripts(string repository) =>
    [
        .. ScriptFolders
            .Select(folder => Full(repository, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.GetFiles(folder, "*.txt", SearchOption.AllDirectories))
            .Select(file => new ReferenceScript(Path.GetRelativePath(repository, file).Replace('\\', '/'), File.ReadAllLines(file))),
    ];

    /// <summary>
    /// The command inventory: none for a missing file (WP-B8); a file that is there and is no inventory of the format
    /// gives why the pages cannot be made, since its pages would name no command.
    /// </summary>
    private static (IReadOnlyList<InventoryApp> Apps, string? Unreadable) Inventory(string repository)
    {
        var file = Full(repository, InventoryRun.File);
        if (!File.Exists(file))
        {
            return ([], null);
        }

        var text = File.ReadAllText(file);
        var apps = CommandInventory.Read(text);
        return apps.Count == 0 && Lf(text) != CommandInventory.Write([])
            ? ([], $"{InventoryRun.File} is not a command inventory of the format \"{CommandInventory.Format}\": put it right, then run tools\\workflows.ps1 again.")
            : (apps, null);
    }

    /// <summary>The <c>.md</c> files of the pages' folder that are no page, by their path from the repository's root, compared by case as well.</summary>
    private static List<string> LeftOver(string repository, IReadOnlyList<ReferencePage> pages)
    {
        var folder = Full(repository, Reference.PagesFolder);
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var paths = pages.Select(page => page.Path).ToHashSet(StringComparer.Ordinal);
        return
        [
            .. Directory.GetFiles(folder, "*.md")
                .Where(file => file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .Select(file => Reference.PagesFolder + "/" + Path.GetFileName(file))
                .Where(path => !paths.Contains(path))
                .Order(StringComparer.Ordinal),
        ];
    }

    /// <summary>A path from the repository's root, with forward slashes, as a full path of this computer.</summary>
    private static string Full(string repository, string path) =>
        Path.Combine(repository, path.Replace('/', Path.DirectorySeparatorChar));

    private static string Lf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
