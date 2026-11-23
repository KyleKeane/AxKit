using System.Diagnostics;
using Microsoft.Win32;

namespace AxKit.Core;

/// <summary>
/// Start with Windows for a background app (docs/axStart/SPEC.md AS-1.3; from axStart on its second use, axClip,
/// 2026-10-23): a value under the user's Run key, "AxKit axStart" with <c>"…\AxKit.exe" start --hidden</c>, made or
/// removed when the app's setting flips and checked at each start. Only the installed program writes it, naming
/// itself: a development build never puts itself, or an older installed copy, into the sign-in. The Store's copy
/// writes nothing (docs/axKit/SPEC.md AX-13.4): its package declares a startup task per app, which Windows lists under
/// Settings, Apps, Startup, where the user turns it on or off; <see cref="OpenWindowsStartupSettings"/> opens that page.
/// </summary>
public static class SignInStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>The value's name for an app: "AxKit axStart".</summary>
    private static string ValueName(string appName) => "AxKit " + appName;

    /// <summary>The value as the setting says: written when on and AxKit is installed, removed when off; nothing in the Store's copy.</summary>
    public static void Apply(string appName, string verb, bool on)
    {
        if (PackageIdentity.IsPackaged)
        {
            return;
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (on && IsInstalledProgram())
            {
                key.SetValue(ValueName(appName), $"\"{AppPaths.InstalledExe}\" {verb} --hidden");
            }
            else if (!on)
            {
                key.DeleteValue(ValueName(appName), throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            Log.Error("The sign-in start could not be changed", ex);
        }
    }

    /// <summary>
    /// Whether an app's settings file says that its Start with Windows is off: the JSON text holds
    /// <c>"startWithWindows": false</c>. No text, a text that is not JSON, and a file without the value are not off:
    /// the setting is on at first. For a background app without a startup task of its own in the Store's copy, which
    /// is started with the others there and has only its own setting to say no (<see cref="StoreFirstStart"/>).
    /// </summary>
    public static bool TurnedOff(string? settingsText)
    {
        if (string.IsNullOrWhiteSpace(settingsText))
        {
            return false;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(settingsText, new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                && document.RootElement.EnumerateObject().Any(member =>
                    string.Equals(member.Name, "startWithWindows", StringComparison.OrdinalIgnoreCase) && member.Value.ValueKind == System.Text.Json.JsonValueKind.False);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Windows' Startup apps page (Settings, Apps, Startup), where the Store's copy's sign-in start is turned on or off
    /// (AX-13.4). False when Windows could not open it.
    /// </summary>
    public static bool OpenWindowsStartupSettings()
    {
        try
        {
            // Outside call, program, Windows: opens Windows' Startup apps page in the Settings app through the Windows
            // shell (the ms-settings: address), from the Store's copy's Start with Windows item (AX-13.4).
            Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true })?.Dispose();
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Log.Error("Windows' Startup apps page could not be opened", ex);
            return false;
        }
    }

    /// <summary>
    /// Whether this process is the installed program: only then is the value written, since an installed copy older
    /// than the app would take its verb for a path and greet every sign-in with an error.
    /// </summary>
    private static bool IsInstalledProgram() =>
        Environment.ProcessPath is { } path && string.Equals(Path.GetFullPath(path), Path.GetFullPath(AppPaths.InstalledExe), StringComparison.OrdinalIgnoreCase);
}
