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

    /// <summary>Ctrl+Shift+Slash, the question mark's key (HE-4.1, bundle AX-8.11): the conversation.</summary>
    protected override Hotkey DefaultHotkey => new(Hotkey.Control | Hotkey.Shift, 0xBF);

    /// <summary>Windows+Alt+H, the conversation's default until 2026-11-14 (HE-D5): a file that holds it is taken over to the new one.</summary>
    protected override Hotkey? RetiredDefault => new(Hotkey.Windows | Hotkey.Alt, 'H');

    /// <summary>Ctrl+Shift+Comma, the less-than sign's key (HE-4.1, bundle AX-8.11): the screenshot into the conversation.</summary>
    public static Hotkey DefaultScreenshotHotkey => new(Hotkey.Control | Hotkey.Shift, 0xBC);

    /// <summary>Windows+Alt+S, the screenshot's default until 2026-11-14 (HE-D5), taken over as the conversation's is.</summary>
    public static Hotkey RetiredScreenshotHotkey => new(Hotkey.Windows | Hotkey.Alt, 'S');

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

    /// <summary>
    /// Options, Keep the conversation ready in the background (HE-2.7), off at first (HE-D9, the user's decision of
    /// 2026-11-15: by default Claude Code never starts, and so never contacts Anthropic, before the user acts). On,
    /// axHelp started hidden, as at sign-in, starts the conversation's window hidden too, so that Claude Code is
    /// loaded at the first press of the hotkey. The file's <c>keepConversationReady</c>.
    /// </summary>
    [JsonPropertyName("keepConversationReady")]
    public bool KeepConversationReady { get; set; }

    /// <summary>
    /// Whether axHelp starts the conversation hidden as it starts (HE-2.7): only when axHelp itself started hidden,
    /// the setting is on, the user has already agreed to axClaude's reminder for Claude Code (Claude Code is never
    /// started before that agreement) and Claude Code is installed. All four, or nothing is started until the hotkey.
    /// </summary>
    public static bool StartsConversationHidden(bool startedHidden, bool keepReady, bool agreed, bool claudeInstalled) =>
        startedHidden && keepReady && agreed && claudeInstalled;

    public static string DefaultPath => AppPaths.SettingsFile("axHelp");

    public static HelpSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<HelpSettings>(path, out error);
        settings.SanitizeHotkey();
        settings.ScreenshotHotkeyText = TakenOver(settings.ScreenshotHotkeyText, RetiredScreenshotHotkey, DefaultScreenshotHotkey) ?? string.Empty;
        settings.ScreenshotHotkeyText = settings.ScreenshotKey.ToString();
        settings.Model = string.IsNullOrWhiteSpace(settings.Model) ? FastModel : settings.Model.Trim();
        return settings;
    }

    /// <summary>
    /// The arguments of <c>AxKit.exe claude</c> for the conversation (HE-2.1): the working folder, the title "axHelp",
    /// the model on every start of Claude Code, and the message for its field when there is one; Claude Code continues the folder's last conversation, or starts one, as axClaude does by default (FR-9.1).
    /// Always with <c>--hide-on-close</c> (HE-1.3: closing the conversation hides it, axClaude FR-9.6) and
    /// <c>--ai-tools</c> (HE-2.8: axClaude gives Claude Code axKit's AI tools at each of its starts while the user
    /// has them on, and nothing while they are off; axHelp itself reads no setting for it);
    /// <paramref name="hidden"/> adds <c>--hidden</c>, the start in the background (HE-2.7).
    /// </summary>
    public static IReadOnlyList<string> ConversationArguments(string folder, string model, string? message, bool hidden = false)
    {
        var arguments = new List<string> { folder, "--title", "axHelp", "--model", model, "--hide-on-close", "--ai-tools" };
        if (hidden)
        {
            arguments.Add("--hidden");
        }

        if (message is not null)
        {
            arguments.Add("--message");
            arguments.Add(message);
        }

        return arguments;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);
}
