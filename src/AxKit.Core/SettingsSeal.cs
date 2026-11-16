using System.Security.Cryptography;
using System.Text;

namespace AxKit.Core;

/// <summary>
/// The seal on the settings that decide what is run or what Claude may read (docs/axKit/SPEC.md AX-5.6; repairs.md
/// R-53): an HMAC-SHA256 over those fields under a key kept with Windows' protection for this user
/// (<see cref="UserSecret"/>). A settings class lists its sealed fields in one small method, writes the seal at every
/// save (<see cref="Of"/>, then <see cref="Remember"/>) and asks <see cref="Checks"/> at every load; when the seal does
/// not check, the class puts those fields to their defaults and says so through the load's error text
/// (<see cref="Refused"/>). The one file of this piece (<see cref="AppPaths.SettingsSealFile"/>) holds the key and the
/// names of the files this install has sealed, so that taking a seal out of a file is not a way around it: a file that
/// was never sealed is accepted as it is and sealed at its next save, a file on the list must carry its seal.
/// The limit: a program running as the user can ask Windows for the same key; the seal holds against a file edited by
/// another program or by hand, a profile copied or synced from elsewhere, and an AI tool writing files.
/// The AI tools' own seal (<see cref="Mcp.McpAccess"/>) keeps its file and its key and takes <see cref="Hmac"/>,
/// <see cref="Same"/> and <see cref="ReadKey"/> from here. Tested in <c>tests/AxKit.Tests/SettingsSealTests</c>.
/// </summary>
public static class SettingsSeal
{
    /// <summary>The name of the key file beside a settings file that is not in axKit's settings folder (a test's folder).</summary>
    public const string KeyFileName = "settings-seal.key";

    /// <summary>The words every refusal holds, by which a window tells it from a file that could not be read (<see cref="IsRefusal"/>).</summary>
    public const string ChangedOutside = "changed outside axKit and";

    /// <summary>The line of the key file that says every settings file counts as sealed before: written when the file was there and could not be read.</summary>
    private const string Every = "*";

    /// <summary>What the key file holds: the key, the names of the sealed files in lower case, and whether every file counts.</summary>
    private sealed record State(byte[] Key, IReadOnlyList<string> Files, bool All);

    /// <summary>
    /// The key file of a settings file: axKit's own for a file in axKit's settings folder, and one beside the file
    /// anywhere else, so that a test's folder has its own key and list and never touches the user's (a test run's
    /// home holds both in one folder anyway).
    /// </summary>
    public static string KeyFileOf(string settingsFile)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(settingsFile)) ?? string.Empty;
        return string.Equals(folder, AppPaths.SettingsFolder, StringComparison.OrdinalIgnoreCase)
            ? AppPaths.SettingsSealFile
            : Path.Combine(folder, KeyFileName);
    }

    /// <summary>
    /// The fields as one text, in the order given: each as its length, a colon, the text and a line break, and "-"
    /// for none, so that no field can run into the next and an empty one is not a missing one.
    /// </summary>
    public static string Text(IEnumerable<string?> fields)
    {
        var text = new StringBuilder();
        foreach (var field in fields)
        {
            if (field is null)
            {
                text.Append("-\n");
            }
            else
            {
                text.Append(field.Length).Append(':').Append(field).Append('\n');
            }
        }

        return text.ToString();
    }

    /// <summary>HMAC-SHA256 of a text's UTF-8 bytes under a key, as base64.</summary>
    public static string Hmac(byte[] key, string text) =>
        Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(text)));

    /// <summary>Whether two seals are the same, compared in constant time.</summary>
    public static bool Same(string seal, string other) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(seal), Encoding.UTF8.GetBytes(other));

    /// <summary>A key of 32 bytes read from a file that holds it as base64 under Windows' protection; null when there is none or it was made for another user.</summary>
    public static byte[]? ReadKey(string keyFile)
    {
        try
        {
            if (!File.Exists(keyFile) || UserSecret.Unprotect(File.ReadAllText(keyFile).Trim()) is not { } text)
            {
                return null;
            }

            var key = Convert.FromBase64String(text);
            return key.Length == 32 ? key : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// The seal a settings file is saved with: the HMAC of the file's name and its sealed fields. The key is made at
    /// the first save. Empty when no key can be had (Windows' protection fails, the folder takes no file): the file
    /// is then written without a seal, as before.
    /// </summary>
    public static string Of(string settingsFile, string text)
    {
        var keyFile = KeyFileOf(settingsFile);
        var state = Read(keyFile) ?? Make(keyFile);
        return state is null ? string.Empty : Hmac(state.Key, Sealed(settingsFile, text));
    }

    /// <summary>The settings file was written with its seal: its name joins the list of the files this install has sealed.</summary>
    public static void Remember(string settingsFile)
    {
        var keyFile = KeyFileOf(settingsFile);
        var name = NameOf(settingsFile);
        if (Read(keyFile) is not { } state || state.All || state.Files.Contains(name))
        {
            return;
        }

        Write(keyFile, state with { Files = [.. state.Files, name] });
    }

    /// <summary>
    /// Whether a loaded file's sealed fields are used. Yes when they are the defaults (nothing to refuse: a missing
    /// file, one that could not be read), when the seal is the one the fields have under the key, and when the file
    /// carries no seal and this install never sealed it (a file from before the seal, sealed at its next save). No
    /// when the seal is wrong, when it is missing from a file this install has sealed, and when a seal or the key
    /// file is there and the key cannot be read.
    /// </summary>
    public static bool Checks(string settingsFile, string? seal, string text, string defaultText)
    {
        if (BornOut(settingsFile, seal, text, defaultText))
        {
            return true;
        }

        KeepChangedCopy(settingsFile);
        return false;
    }

    /// <summary>
    /// Whether this install has sealed the file before: its name is on the list, or the list went with a key that
    /// could not be read. For a file whose defaults are not the safe case (the list of the report files axKit wrote,
    /// which is "not kept yet" until its first save): deleting such a file does not bring the time before it back.
    /// </summary>
    public static bool WasSealed(string settingsFile)
    {
        var keyFile = KeyFileOf(settingsFile);
        return Read(keyFile) is { } state ? state.All || state.Files.Contains(NameOf(settingsFile)) : File.Exists(keyFile);
    }

    /// <summary>What a refusal's text ends with: where the file as it was found is kept.</summary>
    public const string CopyKept = "The file as it was found is kept beside it, with .changed at the end of its name.";

    /// <summary>
    /// The refused file copied beside itself as <c>.changed</c>, since the app's next save writes the defaults of the
    /// sealed fields over it: nothing a user had there is lost, whoever changed the file (another program, a hand
    /// edit, an older axKit on the same computer that wrote the file without its seal). Once: not again while the
    /// copy is as new as the file. A copy that cannot be made is passed over.
    /// </summary>
    private static void KeepChangedCopy(string settingsFile)
    {
        var copy = settingsFile + ".changed";
        try
        {
            if (!File.Exists(settingsFile) || (File.Exists(copy) && File.GetLastWriteTimeUtc(copy) >= File.GetLastWriteTimeUtc(settingsFile)))
            {
                return;
            }

            File.Copy(settingsFile, copy, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // No copy: the refusal stands, and the log holds the sentence.
        }
    }

    private static bool BornOut(string settingsFile, string? seal, string text, string defaultText)
    {
        if (text == defaultText)
        {
            return true;
        }

        var keyFile = KeyFileOf(settingsFile);
        var state = Read(keyFile);
        if (!string.IsNullOrEmpty(seal))
        {
            if (state is null || !Same(Hmac(state.Key, Sealed(settingsFile, text)), seal))
            {
                return false;
            }

            // A list that lost this name (two windows adding at once) gets it back.
            Remember(settingsFile);
            return true;
        }

        return state is null ? !File.Exists(keyFile) : !state.All && !state.Files.Contains(NameOf(settingsFile));
    }

    /// <summary>
    /// The error text of a load whose sealed fields were not used: the sentence, then what the file's reading said,
    /// if anything. The sentence is logged once in a process.
    /// </summary>
    public static string Refused(string? error, string sentence)
    {
        lock (Logged)
        {
            if (Logged.Add(sentence))
            {
                Log.Error(sentence);
            }
        }

        return error is null ? sentence + " " + CopyKept : sentence + " " + CopyKept + "\n" + error;
    }

    /// <summary>Whether a load's error text is a refusal of sealed fields, which a window says as it is, and not a file that could not be read.</summary>
    public static bool IsRefusal(string error) => error.Contains(ChangedOutside, StringComparison.Ordinal);

    private static readonly HashSet<string> Logged = [];

    /// <summary>The text under the seal: the file's name first, so that one file's seal is no other file's.</summary>
    private static string Sealed(string settingsFile, string text) => Text([NameOf(settingsFile), text]);

    private static string NameOf(string settingsFile) => Path.GetFileName(settingsFile).ToLowerInvariant();

    /// <summary>The key file read; null when it is missing or cannot be read (made for another user, damaged).</summary>
    private static State? Read(string keyFile)
    {
        try
        {
            if (!File.Exists(keyFile) || UserSecret.Unprotect(File.ReadAllText(keyFile).Trim()) is not { } text)
            {
                return null;
            }

            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0 || Convert.FromBase64String(lines[0]) is not { Length: 32 } key)
            {
                return null;
            }

            return new State(key, [.. lines.Skip(1).Where(line => line != Every)], lines.Skip(1).Contains(Every));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// A new key. Where there was no key file, the file is put there only if none came meanwhile, so that two windows
    /// saving for the first time end with one key; where there was one that cannot be read, it is replaced and every
    /// settings file counts as sealed before, since the list went with it. Null when it cannot be written.
    /// </summary>
    private static State? Make(string keyFile)
    {
        try
        {
            var fresh = !File.Exists(keyFile);
            var state = new State(RandomNumberGenerator.GetBytes(32), [], All: !fresh);
            Directory.CreateDirectory(Path.GetDirectoryName(keyFile)!);
            if (fresh)
            {
                var temporary = $"{keyFile}.{Guid.NewGuid():N}.tmp";
                File.WriteAllText(temporary, Protected(state));
                try
                {
                    File.Move(temporary, keyFile, overwrite: false);
                }
                catch (IOException)
                {
                    File.Delete(temporary);
                }
            }
            else
            {
                SafeFile.WriteAllText(keyFile, Protected(state));
            }

            return Read(keyFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Log.Error("The key of the settings' seal could not be written", ex);
            return null;
        }
    }

    private static void Write(string keyFile, State state)
    {
        try
        {
            SafeFile.WriteAllText(keyFile, Protected(state));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Log.Error("The list of sealed settings files could not be written", ex);
        }
    }

    /// <summary>The key file's text under Windows' protection: the key as base64, then one name a line, and the line for every file.</summary>
    private static string Protected(State state)
    {
        IEnumerable<string> lines = [Convert.ToBase64String(state.Key), .. state.Files];
        return UserSecret.Protect(string.Join('\n', state.All ? lines.Append(Every) : lines));
    }
}
