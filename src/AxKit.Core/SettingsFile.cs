using System.Text.Json;
using System.Text.Json.Serialization;

namespace AxKit.Core;

/// <summary>
/// How every app's settings file is read and written (docs/axKit/SPEC.md AX-5.3): JSON with camel-case keys and enum
/// names, comments and trailing commas tolerated. A missing or unreadable file gives the defaults and, when it was
/// unreadable, an error text for the app to report once. Each app's settings class is a plain class of properties
/// with defaults; it calls these two methods and does its own sanity checks after loading.
/// </summary>
public static class SettingsFile
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Loads the file, or returns defaults. <paramref name="error"/> describes an unreadable file.</summary>
    public static T Load<T>(string path, out string? error) where T : class, new()
    {
        var settings = Read<T>(path, out error, out var opened);
        if (opened)
        {
            // R-62: what this window holds as loaded, against which its next save tells what it changed itself.
            View(path, JsonSerializer.Serialize(settings, JsonOptions));
        }

        return settings;
    }

    /// <summary>The file read; <paramref name="opened"/> is false when it was there and could not be opened (R-51).</summary>
    private static T Read<T>(string path, out string? error, out bool opened) where T : class, new()
    {
        error = null;
        opened = true;
        // A file an earlier version left in another case takes the path's spelling (AX-5.1, 5.6.0).
        FileCase.Match(path);
        if (!File.Exists(path))
        {
            return new T();
        }

        string text;
        try
        {
            text = ReadText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // R-51: the file is there and could not be opened (a sync client, a backup or a virus scanner held it).
            // The defaults serve this run and are never written over it: Save passes the path over until a later
            // Load reads it.
            lock (NotRead)
            {
                NotRead.Add(Path.GetFullPath(path));
            }

            opened = false;
            error = $"{path}: {ex.Message} {NotSaved}";
            Log.Error("Settings not readable, using defaults and saving none: " + error);
            return new T();
        }

        lock (NotRead)
        {
            NotRead.Remove(Path.GetFullPath(path));
        }

        try
        {
            return JsonSerializer.Deserialize<T>(text, JsonOptions) ?? new T();
        }
        catch (JsonException ex)
        {
            // R-52: one value that does not read costs that value alone, when the text is an object at all.
            var (settings, dropped) = WithoutWhatDoesNotRead<T>(text);
            error = settings is null
                ? $"{path}: {ex.Message}"
                : $"{path}: {NotUnderstood}: {string.Join(", ", dropped)}.";
            if (KeepBadCopy(path))
            {
                error += $" A copy is kept as {path}.bad.";
                // Logged here once per file, for every app, when the copy is made; the window says it too, as a notice.
                Log.Error("Settings not readable, using defaults: " + error);
            }

            return settings ?? new T();
        }
    }

    /// <summary>What the error text says before the names of the values that did not read (R-52); axHelp's guide quotes it, so that Claude can explain it.</summary>
    public const string NotUnderstood = "these settings were not understood and are at their defaults";

    /// <summary>What the error text of a file that could not be opened ends with (R-51).</summary>
    public const string NotSaved = "Settings are not saved in this run, so that the file is not replaced. Close the window and start it again.";

    /// <summary>
    /// What a window says when its settings file could not be written (AX-5.9; repairs.md R-63): why, in the system's
    /// words, and what that means for the user.
    /// </summary>
    public static string NotWritten(string reason) =>
        $"Settings could not be saved: {reason.Trim().TrimEnd('.')}. What you change lasts until this window closes.";

    /// <summary><see cref="NotWritten"/> the first time in this process, null after that: it is said once in a run, and the log holds every failure.</summary>
    public static string? NotWrittenOnce(string reason)
    {
        lock (NotRead)
        {
            if (_notWrittenSaid)
            {
                return null;
            }

            _notWrittenSaid = true;
        }

        return NotWritten(reason);
    }

    private static bool _notWrittenSaid;

    /// <summary>The files that were there and could not be opened in this process, by full path: <see cref="Save{T}"/> writes none of them.</summary>
    private static readonly HashSet<string> NotRead = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The file's text; a file another program holds is tried again three times, a tenth of a second apart, before the failure counts.</summary>
    private static string ReadText(string path)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (IOException) when (attempt < 3)
            {
                Thread.Sleep(100);
            }
        }
    }

    /// <summary>
    /// The settings of a text one or more of whose values do not read (R-52): each top-level property is tried by
    /// itself, those that do not read are left out and named, and the rest is loaded. Null when the text is no JSON
    /// object or still does not read without them.
    /// </summary>
    private static (T? Settings, IReadOnlyList<string> Dropped) WithoutWhatDoesNotRead<T>(string text) where T : class, new()
    {
        var dropped = new List<string>();
        try
        {
            var options = new System.Text.Json.Nodes.JsonNodeOptions { PropertyNameCaseInsensitive = true };
            var reading = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
            if (System.Text.Json.Nodes.JsonNode.Parse(text, options, reading) is not System.Text.Json.Nodes.JsonObject whole)
            {
                return (null, dropped);
            }

            var kept = new System.Text.Json.Nodes.JsonObject();
            foreach (var (name, value) in whole)
            {
                var one = new System.Text.Json.Nodes.JsonObject { [name] = value?.DeepClone() };
                try
                {
                    JsonSerializer.Deserialize<T>(one.ToJsonString(), JsonOptions);
                    kept[name] = value?.DeepClone();
                }
                catch (JsonException)
                {
                    dropped.Add(name);
                }
            }

            return dropped.Count == 0 ? (null, dropped) : (JsonSerializer.Deserialize<T>(kept.ToJsonString(), JsonOptions) ?? new T(), dropped);
        }
        catch (JsonException)
        {
            return (null, dropped);
        }
    }

    /// <summary>
    /// The unreadable file copied next to itself as .bad for mending, since the app saves its settings soon after
    /// loading, which would write the defaults over the one typo. Once: not again while the copy is as new as the
    /// file (the Window menu reads the recently closed list at every opening; until 2026-10-23 it copied and logged
    /// each time). False when the copy is there already or cannot be made.
    /// </summary>
    private static bool KeepBadCopy(string path)
    {
        var bad = path + ".bad";
        try
        {
            if (File.Exists(bad) && File.GetLastWriteTimeUtc(bad) >= File.GetLastWriteTimeUtc(path))
            {
                return false;
            }

            File.Copy(path, bad, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// What each window of this process holds of a settings file, by full path, as JSON text: the settings as
    /// <see cref="Load{T}"/> gave them, then as each <see cref="Save{T}"/> was given them (R-62). A member that
    /// differs from it at a save is one this window changed.
    /// </summary>
    private static readonly Dictionary<string, string> Views = new(StringComparer.OrdinalIgnoreCase);

    private static void View(string path, string text)
    {
        lock (Views)
        {
            Views[Path.GetFullPath(path)] = text;
        }
    }

    /// <summary>
    /// What a save writes when the file may hold what another window saved meanwhile (AX-5.7; repairs.md R-62), as a
    /// JSON object: for each top-level member the file's value, unless this window changed the member, which is when
    /// <paramref name="mine"/> differs from <paramref name="view"/>, what this window held as loaded or last saved.
    /// A member the file does not have keeps this window's value; a member the file gained that this window never
    /// held is kept, and one this window held and writes no longer is dropped. A list both
    /// windows changed is this window's, whole: lists are not merged. Null when the file's text is no JSON object,
    /// or when nothing of it differs from <paramref name="mine"/>.
    /// </summary>
    public static System.Text.Json.Nodes.JsonObject? Merged(string view, string mine, string file)
    {
        var options = new System.Text.Json.Nodes.JsonNodeOptions { PropertyNameCaseInsensitive = true };
        var reading = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        System.Text.Json.Nodes.JsonObject? held, own, there;
        try
        {
            held = System.Text.Json.Nodes.JsonNode.Parse(view, options, reading) as System.Text.Json.Nodes.JsonObject;
            own = System.Text.Json.Nodes.JsonNode.Parse(mine, options, reading) as System.Text.Json.Nodes.JsonObject;
            there = System.Text.Json.Nodes.JsonNode.Parse(file, options, reading) as System.Text.Json.Nodes.JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }

        if (held is null || own is null || there is null)
        {
            return null;
        }

        var merged = new System.Text.Json.Nodes.JsonObject();
        var differs = false;
        foreach (var (name, value) in own)
        {
            var changedHere = !held.ContainsKey(name) || !System.Text.Json.Nodes.JsonNode.DeepEquals(value, held[name]);
            if (changedHere || !there.ContainsKey(name))
            {
                merged[name] = value?.DeepClone();
            }
            else
            {
                merged[name] = there[name]?.DeepClone();
                differs |= !System.Text.Json.Nodes.JsonNode.DeepEquals(value, there[name]);
            }
        }

        foreach (var (name, value) in there)
        {
            // A member this window does not write: kept when the file gained it since this window loaded it (another
            // window's, a newer version's), dropped when this window held it and writes it no longer (axGIT's old
            // list of trusted paths, read once and then gone).
            if (!own.ContainsKey(name) && !held.ContainsKey(name))
            {
                merged[name] = value?.DeepClone();
                differs = true;
            }
        }

        return differs ? merged : null;
    }

    /// <summary>
    /// The settings a save writes (R-62): <paramref name="settings"/> themselves, or, when this process loaded the
    /// file and the file now holds something else that reads, those with what the file gained (<see cref="Merged"/>).
    /// A file <paramref name="fileChecks"/> refuses (its seal does not check, AX-5.6) gives nothing.
    /// </summary>
    private static T WithWhatTheFileGained<T>(string path, T settings, string mine, Func<T, bool>? fileChecks) where T : class
    {
        string? view;
        lock (Views)
        {
            Views.TryGetValue(Path.GetFullPath(path), out view);
        }

        if (view is null || !File.Exists(path))
        {
            return settings;
        }

        try
        {
            var file = ReadText(path);
            if (file == mine || Merged(view, mine, file) is not { } merged)
            {
                return settings;
            }

            if (fileChecks is not null && (JsonSerializer.Deserialize<T>(file, JsonOptions) is not { } there || !fileChecks(there)))
            {
                return settings;
            }

            return JsonSerializer.Deserialize<T>(merged.ToJsonString(), JsonOptions) ?? settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            // The file cannot be opened, a value of it does not read, or it names a member twice: this window's settings are written as they are.
            return settings;
        }
    }

    /// <summary>
    /// Writes the file whole or not at all (<see cref="SafeFile"/>): a temporary file of its own name next to it is
    /// put in its place. False when the file was passed over (R-51), so that a sealed file joins the list of sealed
    /// files only once it is written (<see cref="SettingsSeal"/>). What another window of the app saved since this
    /// one loaded the file is kept (R-62, <see cref="Merged"/>). A sealed class gives <paramref name="fileChecks"/>,
    /// whether the file's sealed fields check, so that nothing is taken from a file whose seal does not, and
    /// <paramref name="seal"/>, which writes the seal into the settings: it is called on what is written when that
    /// is not <paramref name="settings"/> themselves.
    /// </summary>
    public static bool Save<T>(string path, T settings, Func<T, bool>? fileChecks = null, Action<T>? seal = null) where T : class
    {
        // R-51: a file that was there and could not be opened is not replaced by the defaults that stood in for it.
        lock (NotRead)
        {
            if (NotRead.Contains(Path.GetFullPath(path)))
            {
                return false;
            }
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var mine = JsonSerializer.Serialize(settings, JsonOptions);
        var written = WithWhatTheFileGained(path, settings, mine, fileChecks);
        if (ReferenceEquals(written, settings))
        {
            SafeFile.WriteAllText(path, mine);
        }
        else
        {
            seal?.Invoke(written);
            SafeFile.WriteAllText(path, JsonSerializer.Serialize(written, JsonOptions));
        }

        // This window's own settings, not what was written: a member another window changed stays one this window did not.
        View(path, mine);
        return true;
    }

    /// <summary>
    /// The first start under a new file name takes over the file of an earlier version (AX-5.2): copied, not moved,
    /// so that the earlier version still finds its own. Nothing happens when the new file exists or the old one does not.
    /// </summary>
    public static void TakeOverOldFile(string oldPath, string path)
    {
        if (File.Exists(path) || !File.Exists(oldPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(oldPath, path);
    }
}
