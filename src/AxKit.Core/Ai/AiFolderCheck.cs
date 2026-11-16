using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AxKit.Core.Ai;

/// <summary>A file in Claude's working folder that Claude Code obeys: its path from the folder and what it holds, as a SHA-256 in hex.</summary>
public sealed record ObeyedFile(string Path, string Hash);

/// <summary>The files of one working folder the user accepted, in <see cref="AiFolderAccepted"/>.</summary>
public sealed class AcceptedFolder
{
    public string Folder { get; set; } = string.Empty;

    public List<ObeyedFile> Files { get; set; } = [];
}

/// <summary>The accepted files of every working folder (<see cref="AppPaths.AiFolderFilesFile"/>), with the settings' seal over all of it.</summary>
public sealed class AiFolderAccepted
{
    public List<AcceptedFolder> Folders { get; set; } = [];

    public string Seal { get; set; } = string.Empty;
}

/// <summary>
/// The files in Claude's working folder that can make Claude Code run something by itself and that axKit did not
/// write (docs/axKit/ai-help.md AH-7.8; repairs.md R-54): found by name and place alone, each with a hash, and
/// compared with the list the user last accepted, which is kept under the settings' seal
/// (<see cref="SettingsSeal"/>). Pure but for the files it is given; no network, no reasoning. What counts:
/// <c>.mcp.json</c>; <c>.claude\settings.json</c> and <c>.claude\settings.local.json</c> only for their
/// <c>hooks</c> member (or when they are no JSON), since their other members are the user's answers and change in
/// normal use; every file under <c>.claude\skills</c>, <c>commands</c>, <c>output-styles</c>, <c>plugins</c>,
/// <c>agents</c> and <c>rules</c>, but axKit's two rule files and a file the caller wrote that still holds the
/// caller's text (axHelp's agent). Not counted, by decision: <c>CLAUDE.md</c>, <c>CLAUDE.local.md</c>, and anything
/// outside <c>.claude</c> but <c>.mcp.json</c>. Tested in <c>tests/AxKit.Tests/AiFolderCheckTests</c>.
/// </summary>
public static class AiFolderCheck
{
    /// <summary>Claude Code's file of a folder's own servers.</summary>
    public const string ServersFile = ".mcp.json";

    /// <summary>Claude Code's folder inside a project folder.</summary>
    public const string ClaudeFolder = ".claude";

    /// <summary>Claude Code's two settings files of a folder, inside <see cref="ClaudeFolder"/>.</summary>
    public static readonly IReadOnlyList<string> SettingsFiles = ["settings.json", "settings.local.json"];

    /// <summary>The member of a settings file that names commands Claude Code runs by itself.</summary>
    public const string HooksMember = "hooks";

    /// <summary>The folders inside <see cref="ClaudeFolder"/> whose every file Claude Code loads or can be led by.</summary>
    public static readonly IReadOnlyList<string> Folders = ["skills", "commands", "output-styles", "plugins", "agents", "rules"];

    /// <summary>The hash of a file that is there and could not be read: named until accepted, like any other.</summary>
    public const string NotRead = "not read";

    /// <summary>The notice's title (AH-7.8).</summary>
    public const string NoticeTitle = "Claude's working folder holds files that can run commands";

    /// <summary>How many names the notice lists before "and N more".</summary>
    public const int NamesShown = 20;

    /// <summary>
    /// Every obeyed file of the folder that axKit did not write, in the order of their paths.
    /// <paramref name="written"/> are files the caller wrote there (path from the folder, text as written): one of
    /// them is left out while it holds exactly that text.
    /// </summary>
    public static IReadOnlyList<ObeyedFile> Obeyed(string folder, IReadOnlyList<(string Path, string Text)> written)
    {
        var found = new List<ObeyedFile>();
        var servers = Path.Combine(folder, ServersFile);
        if (File.Exists(servers))
        {
            found.Add(new ObeyedFile(ServersFile, HashOfFile(servers)));
        }

        foreach (var name in SettingsFiles)
        {
            var path = Path.Combine(folder, ClaudeFolder, name);
            if (File.Exists(path) && HooksHash(path) is { } hash)
            {
                found.Add(new ObeyedFile(Path.Combine(ClaudeFolder, name), hash));
            }
        }

        // Hidden and system files count too; the depth is bounded, so that a link back into the folder ends.
        var all = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0, MaxRecursionDepth = 16, IgnoreInaccessible = true };
        foreach (var name in Folders)
        {
            var start = Path.Combine(folder, ClaudeFolder, name);
            if (!Directory.Exists(start))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(start, "*", all))
            {
                var relative = Path.GetRelativePath(folder, path);
                if (IsAxKits(relative) || written.Any(file => Same(file.Path, relative) && Holds(path, file.Text)))
                {
                    continue;
                }

                found.Add(new ObeyedFile(relative, HashOfFile(path)));
            }
        }

        return found.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>The obeyed files that are not on the accepted list of this folder with the hash they have now: new ones and changed ones.</summary>
    public static IReadOnlyList<ObeyedFile> NewOrChanged(string folder, string acceptedFile, IReadOnlyList<(string Path, string Text)> written)
    {
        var accepted = AcceptedOf(Load(acceptedFile), folder)?.Files ?? [];
        return Obeyed(folder, written)
            .Where(file => !accepted.Any(known => Same(known.Path, file.Path) && known.Hash == file.Hash))
            .ToList();
    }

    /// <summary>
    /// The user's "Start anyway and remember these files": the files join the folder's accepted list as they were
    /// shown, so that one changed since is named again, and the list is written with its seal. False when it could
    /// not be written; the answer still counts for this start.
    /// </summary>
    public static bool Accept(string folder, string acceptedFile, IReadOnlyList<ObeyedFile> files)
    {
        try
        {
            var list = Load(acceptedFile);
            var entry = AcceptedOf(list, folder);
            if (entry is null)
            {
                entry = new AcceptedFolder { Folder = Path.GetFullPath(folder) };
                list.Folders.Add(entry);
            }

            foreach (var file in files)
            {
                entry.Files.RemoveAll(known => Same(known.Path, file.Path));
                entry.Files.Add(file);
            }

            list.Seal = SettingsSeal.Of(acceptedFile, SealedText(list));
            if (!SettingsFile.Save(acceptedFile, list))
            {
                return false;
            }

            SettingsSeal.Remember(acceptedFile);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The list of files accepted in Claude's working folder could not be written: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// The notice's text (AH-7.8): how many, their names one a line (the first <see cref="NamesShown"/>), that
    /// axKit did not write them, that Claude Code would obey them from its start, that they may be the user's own,
    /// and what the three choices do.
    /// </summary>
    public static string NoticeText(IReadOnlyList<ObeyedFile> files)
    {
        var text = new StringBuilder();
        text.Append(files.Count == 1
            ? "1 file in Claude's working folder can make Claude Code run commands by itself, and axKit did not write it:\n"
            : $"{files.Count} files in Claude's working folder can make Claude Code run commands by themselves, and axKit did not write them:\n");
        foreach (var file in files.Take(NamesShown))
        {
            text.Append(file.Path).Append('\n');
        }

        if (files.Count > NamesShown)
        {
            text.Append("and ").Append(files.Count - NamesShown).Append(" more.\n");
        }

        text.Append(files.Count == 1
            ? "Claude Code would obey it from its start. It may be your own. "
            : "Claude Code would obey them from its start. They may be your own. ");
        text.Append("Open the folder shows the folder in axFile. Start anyway starts the conversation and does not ask again until one of these files changes or a new one comes. Cancel starts nothing.");
        return text.ToString();
    }

    /// <summary>What the log says when the list's seal does not check; the list is then empty, and every file is asked about again.</summary>
    public const string ListNotUsed = "The list of files accepted in Claude's working folder was changed outside axKit and is not used: every such file is asked about again.";

    /// <summary>The accepted list read; empty when it is missing, cannot be read, or its seal does not check.</summary>
    private static AiFolderAccepted Load(string acceptedFile)
    {
        var list = SettingsFile.Load<AiFolderAccepted>(acceptedFile, out var error);
        list.Folders = (list.Folders ?? []).Where(entry => entry is { Folder: not null, Files: not null } && entry.Files.All(file => file is { Path: not null, Hash: not null })).ToList();
        if (!SettingsSeal.Checks(acceptedFile, list.Seal, SealedText(list), SealedText(new AiFolderAccepted())))
        {
            SettingsSeal.Refused(error, ListNotUsed);
            list.Folders = [];
        }

        return list;
    }

    private static AcceptedFolder? AcceptedOf(AiFolderAccepted list, string folder) =>
        list.Folders.FirstOrDefault(entry => Same(entry.Folder, Path.GetFullPath(folder)));

    /// <summary>The sealed text: each folder, the number of its files, then each file's path and hash.</summary>
    private static string SealedText(AiFolderAccepted list)
    {
        var fields = new List<string?>();
        foreach (var entry in list.Folders)
        {
            fields.Add(entry.Folder);
            fields.Add(entry.Files.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            foreach (var file in entry.Files)
            {
                fields.Add(file.Path);
                fields.Add(file.Hash);
            }
        }

        return SettingsSeal.Text(fields);
    }

    /// <summary>axKit's two rule files, which the self-repair keeps at axKit's text before every start (AH-7.7).</summary>
    private static bool IsAxKits(string relative) =>
        Same(relative, AiDocumentation.RulePath) || Same(relative, AiToolsRule.RulePath);

    private static bool Same(string path, string other) => string.Equals(path, other, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the file holds exactly the text; a file that cannot be read does not.</summary>
    private static bool Holds(string path, string text)
    {
        try
        {
            return File.ReadAllText(path) == text;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string HashOfFile(string path)
    {
        try
        {
            return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return NotRead;
        }
    }

    /// <summary>
    /// What a settings file counts for: the hash of its <c>hooks</c> member's text when that names anything (an
    /// "always allow" answer beside it changes nothing here), the hash of the whole file when it is no JSON (Claude
    /// Code's reading of it cannot be known), <see cref="NotRead"/> when it cannot be read, and null when it is JSON
    /// without hooks, which is the user's in normal use.
    /// </summary>
    private static string? HooksHash(string path)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return NotRead;
        }

        try
        {
            using var json = JsonDocument.Parse(bytes);
            if (json.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Every member of the name, should the file hold it twice: whichever Claude Code takes is under the hash.
            var hooks = new StringBuilder();
            foreach (var member in json.RootElement.EnumerateObject())
            {
                if (member.NameEquals(HooksMember) && !IsEmpty(member.Value))
                {
                    hooks.Append(member.Value.GetRawText()).Append('\n');
                }
            }

            return hooks.Length == 0 ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hooks.ToString())));
        }
        catch (JsonException)
        {
            return Convert.ToHexString(SHA256.HashData(bytes));
        }
    }

    /// <summary>A hooks member that names nothing: null, an empty object or an empty list.</summary>
    private static bool IsEmpty(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => true,
        JsonValueKind.Object => !value.EnumerateObject().Any(),
        JsonValueKind.Array => value.GetArrayLength() == 0,
        _ => false,
    };
}
