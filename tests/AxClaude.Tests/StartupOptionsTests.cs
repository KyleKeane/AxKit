using AxClaude.Core;

namespace AxClaude.Tests;

/// <summary>The command line (FR-9): the options, their errors, and everything after <c>--</c> passed on verbatim.</summary>
public class StartupOptionsTests
{
    private static readonly string Folder = Path.GetTempPath();

    [Fact]
    public void Everything_after_the_double_dash_goes_to_claude_and_a_bare_one_starts_afresh()
    {
        var options = StartupOptions.Parse([Folder, "--", "--resume", "abc", "--cols"]);
        Assert.Equal(["--resume", "abc", "--cols"], options.ClaudeArgs!);
        Assert.Empty(StartupOptions.Parse([Folder, "--"]).ClaudeArgs!);
        Assert.Null(StartupOptions.Parse([Folder]).ClaudeArgs);
    }

    /// <summary>FR-8.1: the app's own folder and Windows' own are never taken as the project, from the current directory or from the remembered folder.</summary>
    [Fact]
    public void A_message_and_a_title_are_taken_for_axHelps_conversation()
    {
        var options = StartupOptions.Parse([Folder, "--title", "axHelp", "--model", "haiku", "--message", "Screenshot: C:\\a.png"]);
        Assert.Equal("axHelp", options.Title);
        Assert.Equal("haiku", options.Model);
        Assert.Null(options.ClaudeArgs);
        Assert.Equal("Screenshot: C:\\a.png", options.Message);
        Assert.Null(StartupOptions.Parse([Folder]).Title);
        Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, "--title"]));
    }

    /// <summary>FR-9.6: a window that hides on closing, and its hidden start, which counts only with it and a folder.</summary>
    [Fact]
    public void Hide_on_close_and_a_hidden_start_are_taken_and_hidden_alone_starts_on_view()
    {
        var plain = StartupOptions.Parse([Folder]);
        Assert.False(plain.HideOnClose);
        Assert.False(plain.StartsHidden);

        var hides = StartupOptions.Parse([Folder, "--title", "axHelp", "--hide-on-close"]);
        Assert.True(hides.HideOnClose);
        Assert.False(hides.StartsHidden);

        var hidden = StartupOptions.Parse([Folder, "--hide-on-close", "--hidden"]);
        Assert.True(hidden.StartsHidden);

        // Hidden without hide on close would be a window nothing brings back: it starts on view.
        Assert.False(StartupOptions.Parse([Folder, "--hidden"]).StartsHidden);
        Assert.False((hidden with { Folder = null }).StartsHidden);
        Assert.Contains("--hide-on-close", StartupOptions.Usage);
    }

    /// <summary>FR-9.7: axKit's AI tools for the window's Claude are asked for by an option, never by default.</summary>
    [Fact]
    public void The_ai_tools_are_asked_for_by_their_option_alone()
    {
        Assert.False(StartupOptions.Parse([Folder]).AiTools);
        Assert.False(StartupOptions.Parse([Folder, "--title", "axHelp", "--hide-on-close"]).AiTools);
        Assert.True(StartupOptions.Parse([Folder, "--title", "axHelp", "--ai-tools"]).AiTools);
        Assert.Contains("--ai-tools", StartupOptions.Usage);
    }

    /// <summary>docs/axClaude/stream-mode.md SM-9.1: the source of one start's window, by its option before the setting; the screen whenever neither names the stream.</summary>
    [Fact]
    public void The_source_is_the_options_word_and_else_the_settings()
    {
        var screen = new AppSettings();
        var stream = new AppSettings { ClaudeSource = AppSettings.SourceStream };

        Assert.Null(StartupOptions.Parse([Folder]).Source);
        Assert.False(StartupOptions.Parse([Folder]).Streams(screen));
        Assert.True(StartupOptions.Parse([Folder]).Streams(stream));
        Assert.True(StartupOptions.Parse([Folder, "--source", "stream"]).Streams(screen));
        Assert.False(StartupOptions.Parse([Folder, "--source", "screen"]).Streams(stream));

        // A word that is neither is refused, and so is the option without its word: a mistyped word starts nothing else.
        Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, "--source", "Stream"]));
        Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, "--source", "pipes"]));
        Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, "--source"]));
        Assert.Contains("--source", StartupOptions.Usage);
    }

    [Fact]
    public void Windows_own_folders_and_the_apps_own_are_never_a_project()
    {
        Assert.True(StartupOptions.IsNeverAProject(Environment.SystemDirectory));
        Assert.True(StartupOptions.IsNeverAProject(Environment.SystemDirectory.ToLowerInvariant() + "\\"));
        Assert.True(StartupOptions.IsNeverAProject(Environment.GetFolderPath(Environment.SpecialFolder.Windows)));
        Assert.True(StartupOptions.IsNeverAProject(AppContext.BaseDirectory));
        Assert.False(StartupOptions.IsNeverAProject(Folder));
    }

    [Fact]
    public void The_folder_is_made_absolute_and_must_exist()
    {
        var options = StartupOptions.Parse([Folder]);
        Assert.Equal(Path.GetFullPath(Folder), options.Folder);
        Assert.Equal(options.Folder, StartupOptions.Parse(["--project", Folder]).Folder);
        var missing = Path.Combine(Folder, "axclaude-" + Guid.NewGuid().ToString("N"));
        Assert.Contains("does not exist", Assert.Throws<ArgumentException>(() => StartupOptions.Parse([missing])).Message);
        Assert.Contains("Only one folder", Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, Folder])).Message);
    }

    [Fact]
    public void The_console_size_has_bounds_and_options_need_their_values()
    {
        var options = StartupOptions.Parse([Folder, "--cols", "120", "--rows", "40", "--no-ax", "--claude", "claude.cmd"]);
        Assert.Equal(120, options.Columns);
        Assert.Equal(40, options.Rows);
        Assert.True(options.NoAx);
        Assert.Equal("claude.cmd", options.ClaudePath);
        Assert.Contains("--cols must be a number between 20 and 1000", Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, "--cols", "5"])).Message);
        Assert.Contains("--rows needs a value", Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, "--rows"])).Message);
        Assert.Contains("Unknown option --colour", Assert.Throws<ArgumentException>(() => StartupOptions.Parse([Folder, "--colour"])).Message);
    }

    [Fact]
    public void The_recording_path_is_made_absolute()
    {
        var options = StartupOptions.Parse([Folder, "--record", "session.vt"]);
        Assert.True(Path.IsPathRooted(options.RecordPath));
        Assert.EndsWith("session.vt", options.RecordPath);
    }

    /// <summary>
    /// FR-9.1, FR-9.6: after Update and restart the window comes back as the window it was. axHelp's conversation
    /// came back without <c>--ai-tools</c> until 2026-11-22, so Claude there had lost axKit's AI tools.
    /// </summary>
    [Fact]
    public void A_restart_starts_the_window_another_program_keeps_as_that_window()
    {
        var kept = StartupOptions.Parse([Folder, "--title", "axHelp", "--model", "haiku", "--hide-on-close", "--hidden", "--ai-tools", "--source", "screen", "--message", "a picture"]);
        var restart = kept.RestartArguments(kept.Folder, messageSent: true);
        Assert.Equal(["claude", kept.Folder!, "--title", "axHelp", "--model", "haiku", "--hide-on-close", "--ai-tools", "--source", "screen", "--", "--continue"], restart);

        // Read again as a command line, it is the same window, on view and with an empty field.
        var again = StartupOptions.Parse([.. restart.Skip(1)]);
        Assert.Equal((kept.Folder, "axHelp", "haiku", true, true, "screen"), (again.Folder, again.Title, again.Model, again.HideOnClose, again.AiTools, again.Source));
        Assert.False(again.Hidden);
        Assert.Null(again.Message);
        Assert.Equal(["--continue"], again.ClaudeArgs!);
    }

    [Fact]
    public void A_restart_of_a_plain_window_names_its_folder_and_nothing_else()
    {
        var plain = StartupOptions.Parse([Folder]);
        Assert.Equal(["claude", plain.Folder!], plain.RestartArguments(plain.Folder, messageSent: false));
        Assert.Equal(["claude"], plain.RestartArguments(null, messageSent: true));
    }
}
