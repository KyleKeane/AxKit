using System.Text;
using System.Text.Json;

namespace AxKit.Core;

/// <summary>One app's key: Ctrl with function key <see cref="Number"/>, with Shift for an access layer.</summary>
public sealed record AppLaunchKey(App App, bool Shift, int Number)
{
    /// <summary>The key as the bundle writes it: "Ctrl+F2", "Ctrl+Shift+F5".</summary>
    public string Text => (Shift ? "Ctrl+Shift+F" : "Ctrl+F") + Number;

    /// <summary>The key as a chord of <see cref="KeyChord"/>, "ctrl+f2", "ctrl+shift+f5", for comparing with other keys.</summary>
    public string Chord => (Shift ? "ctrl+shift+f" : "ctrl+f") + Number;
}

/// <summary>The keys the user's file gives (<c>AppLaunchKeys.Resolve</c>) and each entry it refused, with the reason, as the log says it.</summary>
public sealed record AppKeysResolved(IReadOnlyList<AppLaunchKey> Keys, IReadOnlyList<string> Refused);

/// <summary>
/// The keys that open an axKit app from any axKit window (docs/axKit/SPEC.md AX-8.18 rule 6; the maintainer's
/// choices of 2026-11-23): Ctrl with a function key opens a standalone app, Ctrl+Shift with a function key an access
/// layer, so that the key itself says which kind of app it opens. Each group of apps (<see cref="AppGroup"/>) takes
/// one block of four function keys, as the keyboard groups them, F1 to F4, F5 to F8, F9 to F12: the first group of
/// its kind the first block, and so on; within a block the apps stand in the bundle's order
/// (<see cref="Dispatch.Apps"/>, the Window menu's). An app past the fourth of its group, or a group past the third
/// block, has no key. Not registered for all of Windows: only the axKit windows take them. These are the defaults; the
/// user's own table (the maintainer's decision of 2026-11-25) is <c>app-keys.json</c> in the settings folder
/// (<see cref="AppPaths.AppKeysFile"/>), read once as a process starts (<see cref="Load"/>), which gives an app
/// another key of the same shape or none (<c>Resolve</c>); <see cref="Current"/> is the table that holds.
/// </summary>
public static class AppLaunchKeys
{
    /// <summary>The function keys there are.</summary>
    public const int Count = 12;

    /// <summary>The function keys of one block, one group of apps.</summary>
    public const int Block = 4;

    /// <summary>The value that takes an app's key away.</summary>
    public const string None = "none";

    /// <summary>The member of the file that explains it, which names no app (JSON has no comments).</summary>
    public const string AboutMember = "about";

    /// <summary>The sentence of <see cref="AboutMember"/> in the file <see cref="Template"/> writes.</summary>
    public const string AboutText = "Each app's key in every axKit window. A standalone app takes Ctrl with a function key, F1 to F12; an access layer Ctrl+Shift with one; none takes the key away; an app left out keeps its default. Windows opened after a change have it.";

    /// <summary>Every app's key, standalone apps on Ctrl, access layers on Ctrl+Shift, a block of four per group.</summary>
    public static IReadOnlyList<AppLaunchKey> Default { get; } = For(Dispatch.Apps);

    /// <summary>The keys that hold in this process: <see cref="Default"/> until <see cref="Load"/> has read the user's file; the tests never load it.</summary>
    public static IReadOnlyList<AppLaunchKey> Current { get; private set; } = Default;

    /// <summary>The keys of the apps given, in their order: the rule of <see cref="Default"/>, for any list.</summary>
    public static IReadOnlyList<AppLaunchKey> For(IEnumerable<App> apps)
    {
        var keys = new List<AppLaunchKey>();
        foreach (var kind in apps.GroupBy(app => app.IsAccessLayer))
        {
            var block = 0;
            foreach (var group in kind.GroupBy(app => app.Group))
            {
                var place = 0;
                foreach (var app in group)
                {
                    var number = (block * Block) + ++place;
                    if (place <= Block && number <= Count)
                    {
                        keys.Add(new AppLaunchKey(app, kind.Key, number));
                    }
                }

                block++;
            }
        }

        return keys;
    }

    /// <summary>The key of an app, by its name, as it holds (<see cref="Current"/>); null for an app without one.</summary>
    public static AppLaunchKey? Of(string appName) =>
        Current.FirstOrDefault(key => string.Equals(key.App.Name, appName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The user's file read and made the table of this process (<see cref="Current"/>), once, as the process starts
    /// and before any window exists. No file: the defaults. A file that is not a JSON object: the defaults, and the
    /// log says why. Each refused entry is a line of the log; nothing is ever said in a window and nothing throws.
    /// </summary>
    /// <param name="path">The file, <see cref="AppPaths.AppKeysFile"/>.</param>
    /// <param name="bundleKeys">The bundle's keys other than the app keys, each with its name and its chord (<see cref="KeyChord"/>).</param>
    public static void Load(string path, IReadOnlyList<(string Name, string Chord)> bundleKeys)
    {
        string text;
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"{Path.GetFileName(path)} could not be read; the default app keys hold", ex);
            return;
        }

        var resolved = Resolve(text, bundleKeys);
        foreach (var refused in resolved.Refused)
        {
            Log.Info($"{Path.GetFileName(path)}: {refused}");
        }

        Current = resolved.Keys;
    }

    /// <summary>
    /// The table a file's text gives (AX-8.18 rule 6): each member names an app (its name, any case) and gives it a
    /// key in the words the key texts use, "Ctrl+F2", or <see cref="None"/>; an app the file leaves out keeps its
    /// default. An entry is refused, and the app keeps its default, when it names no app, names an app a second time,
    /// is no key that can be read, is not the shape of the rule (Ctrl with a function key for a standalone app,
    /// Ctrl+Shift with one for an access layer), is a bundle key, or is the key another app ends up with (AX-8.4's
    /// collision rule): an entry that takes the key of an app the file leaves out, or a key an entry before it took.
    /// The keys come in the order of <see cref="Default"/>, each app at its default's place.
    /// </summary>
    public static AppKeysResolved Resolve(string text, IReadOnlyList<(string Name, string Chord)> bundleKeys) =>
        Resolve(text, Default, bundleKeys);

    /// <summary>The same rule over any defaults, for the tests.</summary>
    public static AppKeysResolved Resolve(string text, IReadOnlyList<AppLaunchKey> defaults, IReadOnlyList<(string Name, string Chord)> bundleKeys)
    {
        var refused = new List<string>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException ex)
        {
            return new AppKeysResolved(defaults, [$"the file is no JSON ({ex.Message}); the default app keys hold"]);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new AppKeysResolved(defaults, ["the file is no JSON object of app names and keys; the default app keys hold"]);
            }

            // The entries that read, in the file's order: an app with its key, or with null for none.
            var given = new List<(App App, AppLaunchKey? Key)>();
            var named = new HashSet<App>();
            foreach (var member in document.RootElement.EnumerateObject())
            {
                if (string.Equals(member.Name, AboutMember, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Dispatch.ByName(member.Name) is not { } app)
                {
                    refused.Add($"\"{member.Name}\" names no app of axKit; the entry is skipped");
                    continue;
                }

                if (!named.Add(app))
                {
                    refused.Add($"{app.Name} is named a second time; the second entry is skipped");
                    continue;
                }

                if (member.Value.ValueKind != JsonValueKind.String)
                {
                    refused.Add($"{app.Name}: \"{member.Value.GetRawText()}\" is no key axKit can read, such as Ctrl+F2; {app.Name} keeps its default key");
                    continue;
                }

                var value = member.Value.GetString()!;
                if (string.Equals(value.Trim(), None, StringComparison.OrdinalIgnoreCase))
                {
                    given.Add((app, null));
                    continue;
                }

                if (Refusal(app, value, bundleKeys, out var key) is { } reason)
                {
                    refused.Add($"{app.Name}: {reason}; {app.Name} keeps its default key");
                    continue;
                }

                var earlier = given.Where(entry => entry.Key?.Chord == key!.Chord).Select(entry => entry.App).FirstOrDefault();
                if (earlier is not null)
                {
                    refused.Add($"{app.Name}: {key!.Text} is given to {earlier.Name} before it in the file; {app.Name} keeps its default key");
                    continue;
                }

                given.Add((app, key));
            }

            // An entry that takes the key of an app the file leaves out is refused, and its app keeps its default,
            // which may in turn be the key of another entry: until no two apps share a key.
            while (true)
            {
                var kept = defaults.Where(key => !given.Any(entry => entry.App == key.App)).ToList();
                var clash = given.FirstOrDefault(entry => entry.Key is { } key && kept.Any(other => other.Chord == key.Chord));
                if (clash.App is null)
                {
                    break;
                }

                var owner = kept.First(other => other.Chord == clash.Key!.Chord).App;
                refused.Add($"{clash.App.Name}: {clash.Key!.Text} is {owner.Name}'s key; give {owner.Name} another key or none in the same file; {clash.App.Name} keeps its default key");
                given.Remove(clash);
            }

            var keys = new List<AppLaunchKey>();
            foreach (var app in defaults.Select(key => key.App).Concat(given.Select(entry => entry.App)).Distinct())
            {
                var entries = given.Where(entry => entry.App == app).ToList();
                var key = entries.Count == 0 ? defaults.First(other => other.App == app) : entries[0].Key;
                if (key is not null)
                {
                    keys.Add(key);
                }
            }

            return new AppKeysResolved(keys, refused);
        }
    }

    /// <summary>Why <paramref name="value"/> cannot be <paramref name="app"/>'s key, or null with the key it is.</summary>
    private static string? Refusal(App app, string value, IReadOnlyList<(string Name, string Chord)> bundleKeys, out AppLaunchKey? key)
    {
        key = null;
        if (KeyChord.Parse(value.Replace(" ", string.Empty, StringComparison.Ordinal)) is not { } chord)
        {
            return $"\"{value}\" is no key axKit can read, such as Ctrl+F2";
        }

        var number = chord.Code - KeyChord.F1 + 1;
        if (!chord.Ctrl || chord.Alt || number is < 1 or > Count || chord.Shift != app.IsAccessLayer)
        {
            return app.IsAccessLayer
                ? $"{value} is not Ctrl+Shift with a function key, F1 to F12, an access layer's kind of key"
                : $"{value} is not Ctrl with a function key, F1 to F12, a standalone app's kind of key";
        }

        var made = new AppLaunchKey(app, chord.Shift, number);
        if (bundleKeys.FirstOrDefault(bundle => bundle.Chord == made.Chord) is { Name: { } name })
        {
            return $"{made.Text} is the bundle's key for {name}";
        }

        key = made;
        return null;
    }

    /// <summary>
    /// The file as axKit writes it when there is none (axKit menu, Edit app keys file): the explanation, then every
    /// app with its key in <paramref name="keys"/> (the window passes <see cref="Current"/>), none for an app without
    /// one, in the order of the defaults.
    /// </summary>
    public static string Template(IReadOnlyList<AppLaunchKey> keys)
    {
        var text = new StringBuilder();
        text.Append("{\r\n");
        // The sentence holds no quotation mark and no backslash, so it is written as it is.
        text.Append("  \"").Append(AboutMember).Append("\": \"").Append(AboutText).Append("\",\r\n");
        var apps = Default.Select(key => key.App).Concat(Dispatch.Apps).Distinct().ToList();
        for (var i = 0; i < apps.Count; i++)
        {
            var key = keys.FirstOrDefault(other => other.App == apps[i]);
            text.Append("  \"").Append(apps[i].Name).Append("\": \"").Append(key?.Text ?? None).Append('"').Append(i < apps.Count - 1 ? "," : string.Empty).Append("\r\n");
        }

        text.Append("}\r\n");
        return text.ToString();
    }

    /// <summary>
    /// The keys of <paramref name="keys"/> that differ from the defaults, in the words of the F1 texts: "axFile
    /// Ctrl+F11, axCalc no key"; empty when they are the defaults.
    /// </summary>
    public static string Changes(IReadOnlyList<AppLaunchKey> keys)
    {
        var changed = new List<string>();
        foreach (var app in Default.Select(key => key.App).Concat(keys.Select(key => key.App)).Distinct())
        {
            var was = Default.FirstOrDefault(key => key.App == app);
            var now = keys.FirstOrDefault(key => key.App == app);
            if (was?.Text != now?.Text)
            {
                changed.Add($"{app.Name} {now?.Text ?? "no key"}");
            }
        }

        return string.Join(", ", changed);
    }
}
