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
        error = null;
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
                : $"{path}: these settings were not understood and are at their defaults: {string.Join(", ", dropped)}.";
            if (KeepBadCopy(path))
            {
                error += $" A copy is kept as {path}.bad.";
                // Logged here once per file, for every app, when the copy is made; the window says it too, as a notice.
                Log.Error("Settings not readable, using defaults: " + error);
            }

            return settings ?? new T();
        }
    }

    /// <summary>What the error text of a file that could not be opened ends with (R-51).</summary>
    public const string NotSaved = "Settings are not saved in this run, so that the file is not replaced. Close the window and start it again.";

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

    /// <summary>Writes the file whole or not at all (<see cref="SafeFile"/>): a temporary file of its own name next to it is put in its place.</summary>
    public static void Save<T>(string path, T settings) where T : class
    {
        // R-51: a file that was there and could not be opened is not replaced by the defaults that stood in for it.
        lock (NotRead)
        {
            if (NotRead.Contains(Path.GetFullPath(path)))
            {
                return;
            }
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        SafeFile.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
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
