using System.Text.Json.Nodes;
using AxClaude.Core;
using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>Claude Code's settings as axClaude offers them (SPEC.md FR-14): the table, the user settings file's path and the change of one key.</summary>
public class ClaudeCodeSettingsTests
{
    [Fact]
    public void Every_setting_has_its_mechanism_its_default_and_choices_that_fit_it()
    {
        Assert.NotEmpty(ClaudeCodeSettings.All);
        Assert.Equal(ClaudeCodeSettings.All.Count, ClaudeCodeSettings.All.Select(setting => setting.Id).Distinct().Count());
        foreach (var setting in ClaudeCodeSettings.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(setting.Name), setting.Id);
            Assert.False(string.IsNullOrWhiteSpace(setting.Group), setting.Id);
            Assert.False(string.IsNullOrWhiteSpace(setting.Default), setting.Id);
            Assert.False(string.IsNullOrWhiteSpace(setting.Target), setting.Id);
            if (setting.Kind == ClaudeCodeSettingKind.SlashCommand)
            {
                // A command the app knows, which opens a picker the control area shows (D33).
                Assert.StartsWith("/", setting.Target);
                Assert.Empty(setting.Choices);
                Assert.Equal(CommandInteraction.Picker, SlashCommands.Find(setting.Target)?.Interaction);
                continue;
            }

            // A key: Claude Code's default first, as the key's removal, then values that are JSON.
            Assert.True(setting.Choices.Count >= 2, setting.Id);
            Assert.Null(setting.Choices[0].Json);
            Assert.All(setting.Choices.Skip(1), choice => Assert.NotNull(JsonNode.Parse(choice.Json!)));
            Assert.Equal(ClaudeCodeSettingEffect.AtOnce, setting.Effect);
            Assert.DoesNotContain('.', setting.Target);
        }
    }

    [Fact]
    public void The_feedback_requests_item_writes_a_rate_of_zero_and_its_off_is_Claude_Codes_default()
    {
        var survey = ClaudeCodeSettings.Find("feedback-survey");

        Assert.Equal("feedbackSurveyRate", survey.Target);
        Assert.Equal("&Block Claude Code's feedback requests", survey.Name);
        Assert.Equal("0", survey.Choices[1].Json);
        // Nothing written: Claude Code's own experience, the user's decision of 2026-11-15.
        Assert.Equal(0, ClaudeCodeSettings.ChoiceIn(survey, null));
        Assert.Equal("\"notify\"", ClaudeCodeSettings.Find("feedback-drafts").DefaultJson);
    }

    [Fact]
    public void Nothing_from_the_risks_is_offered()
    {
        // What turns screen reader mode off, changes how Claude Code draws, takes the keys of the input, or skips the
        // permission checks (docs/axClaude/SPEC.md FR-14.4).
        string[] never =
        [
            "axScreenReader", "tui", "/tui", "viewMode", "/focus", "editorMode", "/vim", "statusLine", "/statusline",
            "maxProseWidth", "syntaxHighlightingDisabled", "prefersReducedMotion", "theme", "/theme", "permissions",
            "skipDangerousModePermissionPrompt", "env", "/config", "/permissions", "verbose", "showThinkingSummaries",
        ];
        foreach (var setting in ClaudeCodeSettings.All)
        {
            Assert.DoesNotContain(setting.Target, never);
            Assert.All(setting.Choices, choice => Assert.DoesNotContain("bypass", choice.Json ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void The_user_settings_file_is_in_the_home_unless_Claude_Code_is_given_another_folder()
    {
        var none = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(Path.Combine(@"D:\home", ".claude", "settings.json"), ClaudeCodeSettings.UserSettingsFile(@"D:\home", none));

        var moved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["claude_config_dir"] = @"E:\claude-work" };
        Assert.Equal(Path.Combine(@"E:\claude-work", "settings.json"), ClaudeCodeSettings.UserSettingsFile(@"D:\home", moved));

        // A relative or empty folder is not one Claude Code could mean: the home's.
        var relative = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["CLAUDE_CONFIG_DIR"] = "~/.claude-work" };
        Assert.Equal(Path.Combine(@"D:\home", ".claude", "settings.json"), ClaudeCodeSettings.UserSettingsFile(@"D:\home", relative));
    }

    [Fact]
    public void Setting_a_key_keeps_every_other_key_and_nested_value()
    {
        const string Text = """
            {
              "permissions": { "allow": ["Bash(git status)"], "deny": [] },
              "env": { "SOME_VARIABLE": "é" },
              "unknownKey": [1, 2.50, {"deep": null}],
              "model": "opus"
            }
            """;

        var change = ClaudeCodeSettings.WithKey(Text, "feedbackSurveyRate", "0");

        Assert.True(change.Changed);
        Assert.Null(change.Error);
        var before = JsonNode.Parse(Text)!.AsObject();
        var after = JsonNode.Parse(change.Text!)!.AsObject();
        Assert.Equal(0, after["feedbackSurveyRate"]!.GetValue<int>());
        after.Remove("feedbackSurveyRate");
        Assert.True(JsonNode.DeepEquals(before, after));
        // The user's own characters stay as they were, not escaped.
        Assert.Contains("\"é\"", change.Text);
        Assert.EndsWith("\n", change.Text);
    }

    [Fact]
    public void Removing_a_key_restores_the_default_and_keeps_the_rest()
    {
        const string Text = """{"feedbackSurveyRate": 0, "theme": "dark", "hooks": {"Stop": []}}""";

        var change = ClaudeCodeSettings.WithKey(Text, "feedbackSurveyRate", null);

        Assert.True(change.Changed);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{"theme": "dark", "hooks": {"Stop": []}}"""), JsonNode.Parse(change.Text!)));
    }

    [Fact]
    public void A_key_replaced_takes_its_new_value_in_its_place()
    {
        var change = ClaudeCodeSettings.WithKey("""{"a": 1, "feedbackDrafts": "notify", "b": 2}""", "feedbackDrafts", "\"quiet\"");

        Assert.Equal(["a", "feedbackDrafts", "b"], JsonNode.Parse(change.Text!)!.AsObject().Select(pair => pair.Key));
        Assert.Equal("quiet", JsonNode.Parse(change.Text!)!["feedbackDrafts"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n")]
    public void A_missing_or_empty_file_becomes_an_object_with_the_key(string? file)
    {
        var change = ClaudeCodeSettings.WithKey(file, "feedbackSurveyRate", "0");

        Assert.True(change.Changed);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{"feedbackSurveyRate": 0}"""), JsonNode.Parse(change.Text!)));
    }

    [Fact]
    public void Nothing_to_change_leaves_the_text_as_it_was()
    {
        const string Holds = "{ \"feedbackSurveyRate\":0.0 }";
        Assert.Equal(new ClaudeCodeSettingsChange(Holds, false, null), ClaudeCodeSettings.WithKey(Holds, "feedbackSurveyRate", "0"));

        const string Lacks = "{ \"theme\": \"dark\" }";
        Assert.Equal(new ClaudeCodeSettingsChange(Lacks, false, null), ClaudeCodeSettings.WithKey(Lacks, "feedbackSurveyRate", null));
        Assert.Equal(new ClaudeCodeSettingsChange(null, false, null), ClaudeCodeSettings.WithKey(null, "feedbackSurveyRate", null));
    }

    [Theory]
    [InlineData("{ \"theme\": \"dark\", }")]
    [InlineData("// mine\n{ \"theme\": \"dark\" }")]
    [InlineData("{ \"theme\": ")]
    [InlineData("{ \"theme\": \"dark\", \"theme\": \"light\" }")]
    [InlineData("[1, 2]")]
    [InlineData("\"text\"")]
    public void A_file_that_is_not_one_JSON_object_is_never_rewritten(string file)
    {
        var change = ClaudeCodeSettings.WithKey(file, "feedbackSurveyRate", "0");

        Assert.False(change.Changed);
        Assert.Null(change.Text);
        Assert.False(string.IsNullOrWhiteSpace(change.Error));
        Assert.Equal(-1, ClaudeCodeSettings.ChoiceIn(ClaudeCodeSettings.Find("feedback-survey"), file));
    }

    [Fact]
    public void A_syntax_error_is_named_with_its_line()
    {
        Assert.Equal("is not valid JSON at line 3", ClaudeCodeSettings.WithKey("{\n  \"a\": 1,\n  \"b\" 2\n}", "x", "0").Error);
    }

    [Fact]
    public void The_choice_is_read_from_the_file()
    {
        var survey = ClaudeCodeSettings.Find("feedback-survey");
        Assert.Equal(0, ClaudeCodeSettings.ChoiceIn(survey, "{}"));
        Assert.Equal(1, ClaudeCodeSettings.ChoiceIn(survey, """{"feedbackSurveyRate": 0}"""));
        Assert.Equal(1, ClaudeCodeSettings.ChoiceIn(survey, """{"feedbackSurveyRate": 0.0}"""));
        // A rate the user set by hand is neither choice: no item is checked.
        Assert.Equal(-1, ClaudeCodeSettings.ChoiceIn(survey, """{"feedbackSurveyRate": 0.05}"""));

        var drafts = ClaudeCodeSettings.Find("feedback-drafts");
        Assert.Equal(0, ClaudeCodeSettings.ChoiceIn(drafts, null));
        Assert.Equal(0, ClaudeCodeSettings.ChoiceIn(drafts, """{"feedbackDrafts": "notify"}"""));
        Assert.Equal(1, ClaudeCodeSettings.ChoiceIn(drafts, """{"feedbackDrafts": "quiet"}"""));
        Assert.Equal(2, ClaudeCodeSettings.ChoiceIn(drafts, """{"feedbackDrafts": "off"}"""));

        var tips = ClaudeCodeSettings.Find("spinner-tips");
        Assert.Equal(0, ClaudeCodeSettings.ChoiceIn(tips, """{"spinnerTipsEnabled": true}"""));
        Assert.Equal(1, ClaudeCodeSettings.ChoiceIn(tips, """{"spinnerTipsEnabled": false}"""));
    }
}
