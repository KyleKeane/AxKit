namespace AxKit.Core;

/// <summary>One app's key: Ctrl with function key <see cref="Number"/>, with Shift for an access layer.</summary>
public sealed record AppLaunchKey(App App, bool Shift, int Number)
{
    /// <summary>The key as the bundle writes it: "Ctrl+F2", "Ctrl+Shift+F4".</summary>
    public string Text => (Shift ? "Ctrl+Shift+F" : "Ctrl+F") + Number;
}

/// <summary>
/// The keys that open an axKit app from any axKit window (docs/axKit/SPEC.md AX-8.18 rule 6; the maintainer's
/// choices of 2026-11-23): Ctrl with a function key opens a standalone app, Ctrl+Shift with a function key an access
/// layer, so that the key itself says which kind of app it opens, the work-out-of-the-box apps apart from those that
/// need a program installed. Each in the bundle's order (<see cref="Dispatch.Apps"/>, the Window menu's), F1 first;
/// an app past F12 has no key. Not registered for all of Windows: only the axKit windows take them. The table is
/// this one, in code; a table of the user's own, as he chose ("changeable later"), is a later step.
/// </summary>
public static class AppLaunchKeys
{
    /// <summary>The function keys there are.</summary>
    public const int Count = 12;

    /// <summary>Every app's key, standalone apps on Ctrl, access layers on Ctrl+Shift, each in the bundle's order.</summary>
    public static IReadOnlyList<AppLaunchKey> Default { get; } = For(Dispatch.Apps);

    /// <summary>The keys of the apps given, in their order: the rule of <see cref="Default"/>, for any list.</summary>
    public static IReadOnlyList<AppLaunchKey> For(IEnumerable<App> apps)
    {
        var keys = new List<AppLaunchKey>();
        var standalone = 0;
        var layers = 0;
        foreach (var app in apps)
        {
            var number = app.IsAccessLayer ? ++layers : ++standalone;
            if (number <= Count)
            {
                keys.Add(new AppLaunchKey(app, app.IsAccessLayer, number));
            }
        }

        return keys;
    }

    /// <summary>The key of an app, by its name; null for an app without one.</summary>
    public static AppLaunchKey? Of(string appName) =>
        Default.FirstOrDefault(key => string.Equals(key.App.Name, appName, StringComparison.OrdinalIgnoreCase));
}
