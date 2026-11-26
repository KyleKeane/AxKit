using System.Text.Json;

namespace AxKit.Core;

/// <summary>
/// The settings of the whole bundle (docs/axKit/SPEC.md AX-3.1; the maintainer's answer of 2026-11-24, "One switch for
/// all"): whether the windows ask GitHub for a newer version at start, one switch in the axKit menu that every app
/// follows, since every app is the same program file and one version. <c>%APPDATA%\AxKit\bundle.json</c> through
/// <see cref="SettingsFile"/>. Until that day each app had its own Check for updates at start (Options, General); the
/// first time the file is not there, the switch starts off if any app had its own turned off, so that no check the user
/// had refused begins (<see cref="FromApps"/>). Read again at every use, since another window may have changed it.
/// Since 2026-11-25 it holds the second switch of the bundle, Keep a log of events (<see cref="KeepEventLog"/>).
/// Tested in <c>tests/AxKit.Tests/BundleSettingsTests</c>.
/// </summary>
public sealed class BundleSettings
{
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>
    /// axKit menu, Keep a log of events (docs/axKit/events.md section 9 point 5, 2026-11-25): each event's time and kind
    /// written to <see cref="AppPaths.EventLogFile"/>, never its words (<see cref="Events.EventLogger"/>). Off by default.
    /// </summary>
    public bool KeepEventLog { get; set; }

    /// <summary>The members of the file this version does not know, kept as they were (AX-5.8).</summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, JsonElement>? OtherMembers { get; set; }

    public static string DefaultPath => AppPaths.BundleSettingsFile;

    /// <summary>The settings from the file; without the file, from what the apps' own files said (<see cref="FromApps"/>).</summary>
    public static BundleSettings Load(string path, Func<IEnumerable<string?>> appFiles)
    {
        if (File.Exists(path))
        {
            return SettingsFile.Load<BundleSettings>(path, out _);
        }

        return new BundleSettings { CheckForUpdates = FromApps(appFiles()) };
    }

    /// <summary>The settings as every window reads them: the bundle's file, or the apps' own files of before.</summary>
    public static BundleSettings Load() =>
        Load(DefaultPath, () => Dispatch.Apps.Select(app => AppPaths.SettingsFile(app.Name)).Select(ReadOrNull));

    public void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>
    /// The switch as the apps' own files of before give it, each file's text or null for none: on unless one of them
    /// says <c>"checkForUpdates": false</c>. A file that does not read counts for nothing.
    /// </summary>
    public static bool FromApps(IEnumerable<string?> texts)
    {
        foreach (var text in texts)
        {
            if (text is null)
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("checkForUpdates", out var value) && value.ValueKind == JsonValueKind.False)
                {
                    return false;
                }
            }
            catch (JsonException)
            {
                // A file that does not read is the app's to report; it decides nothing here.
            }
        }

        return true;
    }

    private static string? ReadOrNull(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
