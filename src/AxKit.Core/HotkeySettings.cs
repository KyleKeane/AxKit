using System.Text.Json.Serialization;

namespace AxKit.Core;

/// <summary>
/// What the two background apps, axStart and axClip, remember beyond <see cref="WindowSettings"/>
/// (docs/axStart/SPEC.md AS-5.1, docs/axClip/SPEC.md CP-6.1): the hotkey as text, Start with Windows and Say the first
/// match while typing. Their shared window (<c>AxKit.Forms/HotkeyForm</c>) reads these without knowing the app; each
/// app's settings class derives from it, names its default hotkey and adds its own, every property keeping the JSON
/// name it had (the two files were written with the same names before the class existed, 2026-11-09).
/// </summary>
public abstract class HotkeySettings : WindowSettings
{
    protected HotkeySettings()
    {
        // The app's default, which only the derived class knows; a constant of its own, so the call during construction is safe.
        HotkeyText = DefaultHotkey.ToString();
    }

    /// <summary>The hotkey as text, "Ctrl+Shift+Space" (AS-4.1), the file's <c>hotkey</c>; <see cref="Key"/> reads it.</summary>
    [JsonPropertyName("hotkey")]
    public string HotkeyText { get; set; }

    /// <summary>The app starts hidden at sign-in (AS-1.3, CP-1.3).</summary>
    public bool StartWithWindows { get; set; } = true;

    /// <summary>The first match and the count said after the typing pauses (AS-3.2, CP-2.3).</summary>
    public bool SayFirstMatch { get; set; } = true;

    /// <summary>The app's own default hotkey, which <see cref="Key"/> falls back to.</summary>
    protected abstract Hotkey DefaultHotkey { get; }

    /// <summary>
    /// The defaults of earlier versions that no longer are (axHelp's Windows+Alt+H, retired 2026-11-14, bundle
    /// AX-8.11; axClip's Ctrl+Shift+Semicolon and axTemplate's Ctrl+Shift+Apostrophe, swapped 2026-11-23), none by
    /// default: a file that still holds one is taken over to <see cref="DefaultHotkey"/> on loading. So a retired
    /// default is never a key of the user's choice for that app: each is another app's default now, or retired.
    /// </summary>
    protected virtual IReadOnlyList<Hotkey> RetiredDefaults => [];

    /// <summary>
    /// A hotkey's text taken over from a retired default (AX-8.11): the text of <paramref name="now"/> when
    /// <paramref name="text"/> reads as <paramref name="retired"/>, in any order and case of its parts; any other
    /// text, a key the user chose among them, comes back as it is.
    /// </summary>
    public static string? TakenOver(string? text, Hotkey? retired, Hotkey now) =>
        retired is not null && Hotkey.Parse(text) == retired ? now.ToString() : text;

    /// <summary>The hotkey of the settings, or the default when the text is not a usable one.</summary>
    public Hotkey Key => Hotkey.Parse(HotkeyText) is { } key && key.Refusal() is null ? key : DefaultHotkey;

    /// <summary>After loading: the window's settings sanitised, a retired default taken over, and the hotkey's text as the key it reads as.</summary>
    protected void SanitizeHotkey()
    {
        SanitizeWindow();
        foreach (var retired in RetiredDefaults)
        {
            HotkeyText = TakenOver(HotkeyText, retired, DefaultHotkey) ?? string.Empty;
        }

        HotkeyText = Key.ToString();
    }
}