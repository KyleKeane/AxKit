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

    /// <summary>The hotkey of the settings, or the default when the text is not a usable one.</summary>
    public Hotkey Key => Hotkey.Parse(HotkeyText) is { } key && key.Refusal() is null ? key : DefaultHotkey;

    /// <summary>After loading: the window's settings sanitised, and the hotkey's text as the key it reads as.</summary>
    protected void SanitizeHotkey()
    {
        SanitizeWindow();
        HotkeyText = Key.ToString();
    }
}