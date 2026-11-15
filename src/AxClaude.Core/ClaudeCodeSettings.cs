using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxClaude.Core;

/// <summary>How a setting of Claude Code's reaches it (SPEC.md FR-14).</summary>
public enum ClaudeCodeSettingKind
{
    /// <summary>A slash command typed into Claude Code; without an argument it opens Claude Code's own picker, which the control area shows.</summary>
    SlashCommand,

    /// <summary>A top-level key of Claude Code's user settings file (<c>~/.claude/settings.json</c>), written by axClaude at the user's menu choice.</summary>
    UserSettingsKey,
}

/// <summary>When a change reaches the Claude Code that runs.</summary>
public enum ClaudeCodeSettingEffect
{
    /// <summary>At once: Claude Code watches its settings files and reloads them when they change.</summary>
    AtOnce,

    /// <summary>From the next message the user sends.</summary>
    NextMessage,
}

/// <summary>
/// One value a settings key can be given from the menu: the words said when it is chosen (and the item's text where
/// the setting has several), and the value as JSON, or null for "the key removed", which is Claude Code's own default.
/// </summary>
public sealed record ClaudeCodeSettingChoice(string Text, string? Json);

/// <summary>
/// A setting of Claude Code's that axClaude offers (SPEC.md FR-14): its id; its menu text, mnemonic marked; its group
/// in the menu; how it reaches Claude Code; the slash command or the settings key; the values it can be given, the
/// first always Claude Code's own default (a key's removal); Claude Code's default in words; the default as JSON
/// when the file may hold it explicitly; and when a change applies.
/// </summary>
public sealed record ClaudeCodeSetting(
    string Id,
    string Name,
    string Group,
    ClaudeCodeSettingKind Kind,
    string Target,
    IReadOnlyList<ClaudeCodeSettingChoice> Choices,
    string Default,
    string? DefaultJson,
    ClaudeCodeSettingEffect Effect);

/// <summary>What setting one key of the user settings file gives: the file's new text, whether it changed, or why nothing was written.</summary>
public sealed record ClaudeCodeSettingsChange(string? Text, bool Changed, string? Error);

/// <summary>
/// The one place of Claude Code's settings in axClaude (root CLAUDE.md rule 12, beside <c>ClaudeCodeTexts</c> and
/// <c>ClaudeCodeKeys</c>; SPEC.md FR-14): the table of the settings the menus offer, where the user settings file is,
/// and the pure change of one top-level key of it that keeps every other key and value. Checked against Claude Code's
/// documentation (code.claude.com/docs/en/settings, settings-reference, env-vars, data-usage, commands) and the
/// installed Claude Code of <see cref="CheckedWith"/>; checked again when <c>tools/upstream.ps1</c> reports a new
/// version. Defaults are Claude Code's own: nothing here is written unless the user chooses it in the menu.
/// Never offered: anything that turns screen reader mode off or changes how Claude Code draws (the renderer, the
/// fullscreen view, the status line, line widths), vim mode, and the modes that skip permission checks.
/// </summary>
public static class ClaudeCodeSettings
{
    /// <summary>The Claude Code version the table was checked against (<c>claude --version</c>, 2026-11-15).</summary>
    public const string CheckedWith = "2.1.325";

    /// <summary>The variable that moves Claude Code's configuration folder (env-vars: "default: ~/.claude").</summary>
    public const string ConfigDirVariable = "CLAUDE_CONFIG_DIR";

    /// <summary>The copy of the file as it was before axClaude's last change, beside it, written again at every change.</summary>
    public const string BackupSuffix = ".axkit-backup";

    public const string ModelGroup = "Model and effort";
    public const string FeedbackGroup = "Feedback to Anthropic";
    public const string QuietGroup = "What Claude Code shows";
    public const string ShellGroup = "Shell commands";

    public static readonly IReadOnlyList<ClaudeCodeSetting> All =
    [
        // Slash commands without an argument: Claude Code's own picker shows the current choice and comes up as a
        // question in the control area (SlashCommands: Picker, recorded with 2.1.281). /model saves the choice as the
        // default of new sessions; /effort's max is for the session only (docs: commands, model-config).
        new("model", "Choose the m&odel...", ModelGroup, ClaudeCodeSettingKind.SlashCommand, "/model", [],
            "the default model of the account", null, ClaudeCodeSettingEffect.NextMessage),
        new("effort", "Choose the e&ffort...", ModelGroup, ClaudeCodeSettingKind.SlashCommand, "/effort", [],
            "the model's own effort level", null, ClaudeCodeSettingEffect.NextMessage),

        // The session quality survey: 0 keeps it from appearing; unset, Claude Code uses the rate Anthropic sets
        // (settings-reference, data-usage). Its transcript follow-up comes only after a rating, so it goes too.
        new("feedback-survey", "&Block Claude Code's feedback requests", FeedbackGroup, ClaudeCodeSettingKind.UserSettingsKey, "feedbackSurveyRate",
        [
            new("Claude Code's feedback requests back to Claude Code's own default", null),
            new("Claude Code's feedback requests blocked", "0"),
        ], "unset: the rate Anthropic sets", null, ClaudeCodeSettingEffect.AtOnce),
        // Claude-drafted feedback: "notify" shows a card above the prompt, "quiet" counts the drafts in the footer
        // only, "off" removes the tool that drafts them (settings-reference; user or managed settings only).
        // Named as /config names it, "Claude-drafted feedback" (Claude Code's own list is ClaudeCodeTexts.FeedbackDraftsTitle).
        new("feedback-drafts", "Claude-drafted feedback", FeedbackGroup, ClaudeCodeSettingKind.UserSettingsKey, "feedbackDrafts",
        [
            new("Claude-drafted feedback: a &card for each, Claude Code's default", null),
            new("Claude-drafted feedback: &quiet, counted without a card", "\"quiet\""),
            new("Claude-drafted feedback: &off, Claude drafts none", "\"off\""),
        ], "\"notify\"", "\"notify\"", ClaudeCodeSettingEffect.AtOnce),

        // Rows Claude Code adds around the work; hiding them takes rows away and changes no row axClaude reads.
        new("spinner-tips", "Hide &tips while Claude works", QuietGroup, ClaudeCodeSettingKind.UserSettingsKey, "spinnerTipsEnabled",
        [
            new("Tips while Claude works back to Claude Code's own default", null),
            new("Tips while Claude works hidden", "false"),
        ], "true", "true", ClaudeCodeSettingEffect.AtOnce),
        new("turn-duration", "Hide the turn &duration after each reply", QuietGroup, ClaudeCodeSettingKind.UserSettingsKey, "showTurnDuration",
        [
            new("Turn duration back to Claude Code's own default", null),
            new("Turn duration hidden", "false"),
        ], "true", "true", ClaudeCodeSettingEffect.AtOnce),
        new("session-recap", "Hide the session &recap after time away", QuietGroup, ClaudeCodeSettingKind.UserSettingsKey, "awaySummaryEnabled",
        [
            new("Session recap back to Claude Code's own default", null),
            new("Session recap hidden", "false"),
        ], "unset: the recap is on", "true", ClaudeCodeSettingEffect.AtOnce),

        // A ! line (the user's own, or one an AI tool offered, FR-2.12): its output goes into the conversation, and
        // Claude replies to it unless this is false (settings-reference).
        new("bash-replies", "&No reply from Claude after my ! commands", ShellGroup, ClaudeCodeSettingKind.UserSettingsKey, "respondToBashCommands",
        [
            new("Claude's reply after ! commands back to Claude Code's own default", null),
            new("No reply from Claude after ! commands", "false"),
        ], "true", "true", ClaudeCodeSettingEffect.AtOnce),
    ];

    /// <summary>The setting of that id; the table's ids are fixed, so an unknown one is a mistake in the code.</summary>
    public static ClaudeCodeSetting Find(string id) => All.First(setting => setting.Id == id);

    /// <summary>
    /// Claude Code's user settings file for a Claude Code started with <paramref name="childEnvironment"/> (the
    /// environment axClaude gives it): <c>settings.json</c> in its <see cref="ConfigDirVariable"/> folder when that is
    /// set to a full path, else in <c>.claude</c> under <paramref name="home"/> (settings: "On Windows, ~/.claude means
    /// %USERPROFILE%\.claude"). Never a project's files. The window passes the user profile, or a test run's own folder.
    /// </summary>
    public static string UserSettingsFile(string home, IReadOnlyDictionary<string, string> childEnvironment) =>
        childEnvironment.TryGetValue(ConfigDirVariable, out var folder) && Path.IsPathFullyQualified(folder.Trim())
            ? Path.Combine(folder.Trim(), "settings.json")
            : Path.Combine(home, ".claude", "settings.json");

    private static readonly JsonDocumentOptions ReadOptions = new() { AllowDuplicateProperties = false };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        // The user's own text (a path, a word in another language) is written as it was, not as \u escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// The settings file's text with one top-level key set to <paramref name="valueJson"/>, or removed when it is null;
    /// every other key and value stays (formatting is normalised to two-space indentation). A missing or empty file is
    /// an empty object. A file that is not one JSON object (a syntax error, a comment, a trailing comma, a key twice)
    /// is never rewritten: the change carries the reason and no text. Nothing to change gives the text as it was.
    /// </summary>
    public static ClaudeCodeSettingsChange WithKey(string? fileText, string key, string? valueJson)
    {
        if (Parse(fileText) is not { } file)
        {
            return new(null, false, Problem(fileText));
        }

        var value = valueJson is null ? null : JsonNode.Parse(valueJson);
        if (file.TryGetPropertyValue(key, out var current) ? value is not null && Same(current, value) : value is null)
        {
            return new(fileText, false, null);
        }

        if (value is null)
        {
            file.Remove(key);
        }
        else
        {
            file[key] = value;
        }

        return new(file.ToJsonString(WriteOptions) + "\n", true, null);
    }

    /// <summary>
    /// Which of the setting's choices the file holds: 0, Claude Code's own default, when the key is absent or holds the
    /// default explicitly; the choice whose value it holds; -1 when it holds another value (set by hand), or the file
    /// is not one JSON object, so that no item is checked.
    /// </summary>
    public static int ChoiceIn(ClaudeCodeSetting setting, string? fileText)
    {
        if (Parse(fileText) is not { } file)
        {
            return -1;
        }

        if (!file.TryGetPropertyValue(setting.Target, out var value))
        {
            return 0;
        }

        for (var i = 1; i < setting.Choices.Count; i++)
        {
            if (Same(value, JsonNode.Parse(setting.Choices[i].Json!)!))
            {
                return i;
            }
        }

        return setting.DefaultJson is { } defaultJson && Same(value, JsonNode.Parse(defaultJson)!) ? 0 : -1;
    }

    /// <summary>The file as an object; an empty object for no file or an empty one; null when it is not one JSON object.</summary>
    private static JsonObject? Parse(string? fileText)
    {
        if (string.IsNullOrWhiteSpace(fileText))
        {
            return [];
        }

        try
        {
            return JsonNode.Parse(fileText, documentOptions: ReadOptions) as JsonObject;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Why the file cannot be changed, in a few words after "it".</summary>
    private static string Problem(string? fileText)
    {
        try
        {
            using var document = JsonDocument.Parse(fileText!, ReadOptions);
            return "holds no JSON object";
        }
        catch (JsonException ex) when (ex.LineNumber is { } line)
        {
            return $"is not valid JSON at line {(line + 1).ToString(CultureInfo.InvariantCulture)}";
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            return "is not valid JSON";
        }
    }

    /// <summary>Two values the same for Claude Code: numbers by value (0 and 0.0), everything else as written.</summary>
    private static bool Same(JsonNode? a, JsonNode b)
    {
        if (a is JsonValue left && b is JsonValue right && left.GetValueKind() == JsonValueKind.Number && right.GetValueKind() == JsonValueKind.Number)
        {
            return left.TryGetValue<decimal>(out var x) && right.TryGetValue<decimal>(out var y) && x == y;
        }

        return JsonNode.DeepEquals(a, b);
    }
}
