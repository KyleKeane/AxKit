using System.Security.Cryptography;
using System.Text;

namespace AxKit.Core.Mcp;

/// <summary>A client the user allowed (docs/axKit/mcp.md MC-6.4): known by its program file; its name as it gave it, for display.</summary>
public sealed record McpClient(string Program, string Name, string Paired);

/// <summary>What a window answered the pairing request with (<see cref="McpAccess.PairingOf"/>, MC-6.4).</summary>
public enum PairingAnswer
{
    Allowed,
    Busy,
    Denied,
    NoAnswer,
}

/// <summary>The shape of <see cref="AppPaths.McpFile"/>: off until the user turns it on, and the paired clients.</summary>
public sealed class McpSettings
{
    public bool Enabled { get; set; }

    public List<McpClient> Clients { get; set; } = [];

    /// <summary>Whether AI tools may run a window's commands (docs/axKit/mcp.md MC-7.2): off until the user turns it on in the axKit menu, AI tools, Let AI tools run commands; sealed with the rest.</summary>
    public bool RunCommands { get; set; }

    /// <summary>HMAC-SHA256 of <see cref="Enabled"/> and the clients under the key (<see cref="McpAccess.SealOf"/>), so that a hand-written file counts for nothing.</summary>
    public string Seal { get; set; } = string.Empty;
}

/// <summary>
/// Whether AI tools may use axKit, which clients, and with what key (docs/axKit/mcp.md MC-6.1, MC-6.3, MC-6.4): the
/// bundle's one setting, read again at every connection so that turning it off in one window ends every client's
/// access at its next call, and the key the broker answers the windows' challenges with. The file is written whole
/// through <see cref="SettingsFile"/>, sealed with an HMAC under the key: a file whose seal does not check, written by
/// anything but these methods (an AI tool's own file writing, say), reads as off with no clients. The pure parts (the
/// client rule, the proof, the seal, the hosts refused) are tested in <c>tests/AxKit.Tests</c>. The HMAC, the
/// comparison and the reading of a key file are <see cref="SettingsSeal"/>'s since 2026-11-16 (repairs.md R-53); the
/// seal's value, this file and its key are as they were.
/// </summary>
public static class McpAccess
{
    /// <summary>
    /// Programs that are never paired (MC-6.4): hosts that run whatever script they are given, so that a pairing would
    /// trust every script; a client is a program of its own (Claude Code's). Node stays allowed, since Claude Code
    /// installed through npm runs as node (MC-6.4); the shells, terminals and runtimes after it came with the security
    /// review of 2026-10-25 (S-52).
    /// </summary>
    public static readonly IReadOnlyList<string> RefusedHosts =
    [
        "cmd.exe", "powershell.exe", "pwsh.exe", "wscript.exe", "cscript.exe", "mshta.exe", "rundll32.exe", "regsvr32.exe",
        "python.exe", "pythonw.exe", "py.exe", "explorer.exe", "conhost.exe",
        "bash.exe", "sh.exe", "wsl.exe", "mintty.exe", "git-bash.exe", "wt.exe", "openconsole.exe", "dotnet.exe",
        "perl.exe", "ruby.exe", "java.exe", "javaw.exe", "bun.exe", "deno.exe",
    ];

    /// <summary>When the setting last changed (turned on or off, a pairing, Forget): what a window heard before it is never handed to a client (S-49).</summary>
    public static DateTime ChangedAtUtc => File.Exists(AppPaths.McpFile) ? File.GetLastWriteTimeUtc(AppPaths.McpFile) : DateTime.MaxValue;

    /// <summary>Whether a program may be paired at all.</summary>
    public static bool MayPair(string program) =>
        !RefusedHosts.Contains(Path.GetFileName(program), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The pairing notice's first paragraph (MC-6.4): who asks and which program file it is, the path with its hidden
    /// and reordering characters made visible (<see cref="DisplayText"/>; review, 2026-11-27), since the program is
    /// what the user allows. The name comes made visible already (<see cref="McpProtocol.ClientName"/>).
    /// </summary>
    public static string PairingAsks(string name, string program) => $"{name} asks to use axKit. It runs as {DisplayText.Visible(program)}.";

    /// <summary>What a client not yet paired is told when no axKit window is open to ask the user in (MC-6.4).</summary>
    public static string NoWindowToAllowIn(string client) => $"Open any axKit window to allow {client}.";

    /// <summary>What a client is told when the window asked about the pairing gave no answer of the user's (MC-6.4): it may ask again at once.</summary>
    public const string WindowDidNotAnswer = "The axKit window did not answer; ask again.";

    /// <summary>
    /// What a window's answer to the pairing request means (MC-6.4). Only an answer that says <c>allowed</c> is the
    /// user's: true allows, false is the user's Deny or a notice left unanswered, and <c>busy</c> beside it is a
    /// window that shows another notice. No answer at all, an answer that does not read, and an answer of another
    /// shape (the window's thread was busy, the window is hidden or failed) are <see cref="PairingAnswer.NoAnswer"/>:
    /// until 2026-11-27 they counted as the user's Deny, with its hold of a minute and "The user did not allow".
    /// </summary>
    public static PairingAnswer PairingOf(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return PairingAnswer.NoAnswer;
        }

        try
        {
            if (System.Text.Json.Nodes.JsonNode.Parse(answer) is not System.Text.Json.Nodes.JsonObject said
                || said["allowed"] is not System.Text.Json.Nodes.JsonValue allowed || !allowed.TryGetValue<bool>(out var yes))
            {
                return PairingAnswer.NoAnswer;
            }

            if (yes)
            {
                return PairingAnswer.Allowed;
            }

            return said["busy"] is System.Text.Json.Nodes.JsonValue busy && busy.TryGetValue<bool>(out var shows) && shows
                ? PairingAnswer.Busy
                : PairingAnswer.Denied;
        }
        catch (System.Text.Json.JsonException)
        {
            return PairingAnswer.NoAnswer;
        }
    }

    /// <summary>The setting as written by axKit: off with no clients when there is no key or the seal does not check.</summary>
    public static McpSettings Load()
    {
        var settings = SettingsFile.Load<McpSettings>(AppPaths.McpFile, out _);
        return ReadKey() is { } key && Checks(key, settings) ? settings : new McpSettings();
    }

    /// <summary>What a window says when <see cref="Problem"/> finds the setting changed outside axKit (repairs.md R-56).</summary>
    public const string ChangedOutside = "The AI tools' setting was changed outside axKit, or its key is gone: AI tools are off, and every AI tool must be allowed again.";

    /// <summary>
    /// Why the setting reads as off although its file says otherwise (repairs.md R-56): the file holds an access, a run
    /// switch, a client or a seal, and there is no key for this user or the seal does not check (a hand edit, a file
    /// put back from a backup, a key deleted, a password reset that closed the key). Null when the setting is as axKit
    /// wrote it, or was never turned on. The safe default stays what <see cref="Load"/> gives; this is so that the
    /// user learns of it. Logged once in a process.
    /// </summary>
    public static string? Problem()
    {
        if (!File.Exists(AppPaths.McpFile))
        {
            return null;
        }

        if (!ChangedOutsideAxKit(SettingsFile.Load<McpSettings>(AppPaths.McpFile, out _), ReadKey()))
        {
            return null;
        }

        if (!_problemLogged)
        {
            _problemLogged = true;
            Log.Error(ChangedOutside);
        }

        return ChangedOutside;
    }

    private static bool _problemLogged;

    /// <summary>Whether a file's setting says something (an access, a run switch, a client, a seal) that the key does not bear out; the key null when there is none for this user.</summary>
    public static bool ChangedOutsideAxKit(McpSettings settings, byte[]? key)
    {
        var says = settings.Enabled || settings.RunCommands || settings.Clients.Count > 0 || settings.Seal.Length > 0;
        return says && (key is null || !Checks(key, settings));
    }

    public static bool Enabled => Load().Enabled;

    /// <summary>MC-7.2: whether AI tools may run commands, read from the sealed file; false whenever the access itself is off.</summary>
    public static bool RunsCommands => RunsCommandsIn(Load());

    /// <summary>MC-7.2: whether these settings let AI tools run commands: the switch on and the access itself on.</summary>
    public static bool RunsCommandsIn(McpSettings settings) => settings is { Enabled: true, RunCommands: true };

    /// <summary>MC-6.1, MC-7.2: the access turned on or off; off turns the run switch off too, so that turning the access on again never brings back running commands unsaid.</summary>
    public static McpSettings Switched(McpSettings settings, bool enabled)
    {
        settings.Enabled = enabled;
        if (!enabled)
        {
            settings.RunCommands = false;
        }

        return settings;
    }

    /// <summary>MC-6.1, MC-7.2: Forget AI tools takes every permission back: every client forgotten and the run switch off.</summary>
    public static McpSettings Forgotten(McpSettings settings)
    {
        settings.Clients = [];
        settings.RunCommands = false;
        return settings;
    }

    /// <summary>MC-7.2: the switch Let AI tools run commands, written sealed; on makes the key first, as turning the access on does.</summary>
    public static void SetRunCommands(bool on)
    {
        if (on)
        {
            MakeKey();
        }

        var settings = Load();
        settings.RunCommands = on;
        Save(settings);
    }

    /// <summary>On or off; on makes the key first, so that the file can be sealed and a broker started next finds it.</summary>
    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            MakeKey();
        }

        Save(Switched(Load(), enabled));
    }

    /// <summary>The client paired for this program file, or null.</summary>
    public static McpClient? Find(McpSettings settings, string program) =>
        settings.Clients.FirstOrDefault(client => string.Equals(client.Program, program, StringComparison.OrdinalIgnoreCase));

    /// <summary>The client added, or its earlier pairing replaced.</summary>
    public static void Pair(string program, string name)
    {
        var settings = Load();
        settings.Clients = [.. settings.Clients.Where(client => !string.Equals(client.Program, program, StringComparison.OrdinalIgnoreCase)), new McpClient(program, new string(name.Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim(), DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture))];
        Save(settings);
    }

    /// <summary>axKit → AI tools → Forget AI tools: every client forgotten, so that each must be allowed again, and the run switch off (<see cref="Forgotten"/>).</summary>
    public static void ForgetAll()
    {
        Save(Forgotten(Load()));
    }

    /// <summary>The setting's file written with its seal (<see cref="Write"/>), under this user's key.</summary>
    private static void Save(McpSettings settings) => Write(AppPaths.McpFile, settings, ReadKey());

    /// <summary>What <see cref="Write"/> throws with when the file was passed over: the callers put "was not changed" before it.</summary>
    public const string NotWritten = "the setting's file could not be opened a moment ago, so it is left as it was. Try again.";

    /// <summary>
    /// A setting's file written with its seal; without a key (the access never turned on) it is written off and
    /// empty. Throws <see cref="IOException"/> when nothing was written, because the file was there and could not be
    /// opened at the load just before (repairs.md R-51): "AI tools off", Forget AI tools and the run switch are never
    /// said of a file that still says otherwise (review, 2026-11-27). What another window wrote meanwhile is taken
    /// only from a file whose seal checks, and what is then written is sealed anew, as <c>AiSettings.Save</c> does
    /// (R-62): a merge was written under the seal of this window's values until then, and read as changed outside.
    /// </summary>
    public static void Write(string file, McpSettings settings, byte[]? key)
    {
        if (key is null)
        {
            settings = new McpSettings();
        }
        else
        {
            settings.Seal = SealOf(key, settings);
        }

        var written = SettingsFile.Save(file, settings,
            there => key is not null && Checks(key, there),
            merged => merged.Seal = key is null ? string.Empty : SealOf(key, merged));
        if (!written)
        {
            throw new IOException(NotWritten);
        }
    }

    /// <summary>
    /// The seal of the setting and the clients: one line each, in order, under HMAC-SHA256. The switch of MC-7.2 adds
    /// " run" to the first line only while it is on, so that a file written before it existed still checks.
    /// </summary>
    public static string SealOf(byte[] key, McpSettings settings)
    {
        var text = new StringBuilder(settings.Enabled ? "on" : "off");
        if (settings.RunCommands)
        {
            text.Append(" run");
        }

        foreach (var client in settings.Clients)
        {
            text.Append('\n').Append(OneField(client.Program)).Append('\t').Append(OneField(client.Name)).Append('\t').Append(OneField(client.Paired));
        }

        return SettingsSeal.Hmac(key, text.ToString());
    }

    /// <summary>
    /// A field as the seal reads it: every control character is one replacement character (docs/axKit/repairs.json
    /// R-29). The seal's lines are fields between tabs, and a client chooses its own name: a name that held a tab or a
    /// line break could have been sealed as one client and read back as two, the second a program the user never
    /// allowed. A file with no such character, every file axKit wrote so far, seals as before.
    /// </summary>
    private static string OneField(string field) => new(field.Select(c => char.IsControl(c) ? '\uFFFD' : c).ToArray());

    /// <summary>Whether the file's seal is the one its content has under the key, compared in constant time.</summary>
    public static bool Checks(byte[] key, McpSettings settings) =>
        settings.Seal.Length > 0 && SettingsSeal.Same(SealOf(key, settings), settings.Seal);

    /// <summary>The key, made once: 32 random bytes kept with DPAPI for this user (MC-6.3).</summary>
    private static void MakeKey()
    {
        if (ReadKey() is not null)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.McpKeyFile)!);
        SafeFile.WriteAllText(AppPaths.McpKeyFile, UserSecret.Protect(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
    }

    /// <summary>The key, or null when there is none or it was made for another user.</summary>
    public static byte[]? ReadKey() => SettingsSeal.ReadKey(AppPaths.McpKeyFile);

    /// <summary>A fresh challenge for a connection: 32 random bytes as base64.</summary>
    public static string NewChallenge() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>The answer to a challenge: HMAC-SHA256 of its text under the key, as base64.</summary>
    public static string Proof(byte[] key, string challenge) =>
        SettingsSeal.Hmac(key, challenge);

    /// <summary>Whether the answer proves the key, compared in constant time.</summary>
    public static bool Proves(byte[] key, string challenge, string? answer) =>
        answer is not null && SettingsSeal.Same(Proof(key, challenge), answer);
}
