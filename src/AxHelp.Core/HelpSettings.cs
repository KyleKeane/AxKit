using System.Text.Json.Serialization;
using AxKit.Core;

namespace AxHelp.Core;

/// <summary>
/// Everything axHelp remembers between runs (SPEC.md HE-5), in <c>%APPDATA%\AxKit\axHelp.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the conversation's hotkey and Start with Windows from
/// <see cref="HotkeySettings"/>, the screenshot's hotkey, and the font and the update check from
/// <see cref="WindowSettings"/>. The conversation itself is axClaude's and Claude Code's, in Claude's working folder
/// (<c>AxKit.Core.Ai.AiSettings.WorkingFolder</c>); nothing of it is kept here. A file written before 5.7.0 carried
/// the last questions as <c>exchanges</c>; they are no longer read.
/// </summary>
public sealed class HelpSettings : HotkeySettings
{
    /// <summary>How many screenshots the working folder keeps (HE-3.2); the oldest beyond them is deleted as a new one is taken.</summary>
    public const int ScreenshotsKept = 20;

    /// <summary>Windows+Alt+H (HE-4.1): the conversation.</summary>
    protected override Hotkey DefaultHotkey => new(Hotkey.Windows | Hotkey.Alt, 'H');

    /// <summary>Windows+Alt+S (HE-4.1): the screenshot into the conversation (axDescribe's key until 5.7.0).</summary>
    public static Hotkey DefaultScreenshotHotkey => new(Hotkey.Windows | Hotkey.Alt, 'S');

    /// <summary>The screenshot's hotkey as text, the file's <c>screenshotHotkey</c>; <see cref="ScreenshotKey"/> reads it.</summary>
    [JsonPropertyName("screenshotHotkey")]
    public string ScreenshotHotkeyText { get; set; } = DefaultScreenshotHotkey.ToString();

    /// <summary>The screenshot's hotkey, or its default when the text is not a usable one.</summary>
    [JsonIgnore]
    public Hotkey ScreenshotKey => Hotkey.Parse(ScreenshotHotkeyText) is { } key && key.Refusal() is null ? key : DefaultScreenshotHotkey;

    /// <summary>The fast model a conversation starts with (HE-2.3): Claude Code's alias for its quickest.</summary>
    public const string FastModel = "haiku";

    /// <summary>The models Options, Conversation model offers (HE-2.3), Claude Code's aliases, the fast one first.</summary>
    public static readonly IReadOnlyList<(string Model, string Label)> Models =
    [
        ("haiku", "&Fast (Haiku)"),
        ("sonnet", "&Balanced (Sonnet)"),
        ("opus", "&Strongest (Opus)"),
    ];

    /// <summary>
    /// The conversation's model (HE-2.3), given to Claude Code as <c>--model</c> at its start, so that it is axHelp's
    /// own whatever Claude Code's saved default is; a change counts from the conversation's next start.
    /// </summary>
    public string Model { get; set; } = FastModel;

    public static string DefaultPath => AppPaths.SettingsFile("axHelp");

    public static HelpSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<HelpSettings>(path, out error);
        settings.SanitizeHotkey();
        settings.ScreenshotHotkeyText = settings.ScreenshotKey.ToString();
        settings.Model = string.IsNullOrWhiteSpace(settings.Model) ? FastModel : settings.Model.Trim();
        return settings;
    }

    /// <summary>
    /// The arguments of <c>AxKit.exe claude</c> for the conversation (HE-2.1): the working folder, the title "axHelp",
    /// the message for its field when there is one, then for Claude Code the folder's last conversation and the model.
    /// </summary>
    public static IReadOnlyList<string> ConversationArguments(string folder, string model, string? message)
    {
        var arguments = new List<string> { folder, "--title", "axHelp" };
        if (message is not null)
        {
            arguments.Add("--message");
            arguments.Add(message);
        }

        arguments.AddRange(["--", "--continue", "--model", model]);
        return arguments;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);
}
