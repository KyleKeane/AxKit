using AxKit.Core.Ai;

namespace AxKit.Tests;

/// <summary>The pure parts of AI help (docs/axKit/ai-help.md AH-5): the settings file, the command line, the answer read, the prompts.</summary>
public sealed class AiTests
{
    [Fact]
    public void The_answer_is_read_from_the_json_result_with_its_session()
    {
        var answer = AiAnswer.Parse("""{"type":"result","subtype":"success","is_error":false,"duration_ms":1200,"result":"Ctrl+Shift+W turns word wrap off and on.","session_id":"abc-123"}""", string.Empty, 0);
        Assert.False(answer.IsError);
        Assert.Equal("Ctrl+Shift+W turns word wrap off and on.", answer.Text);
        Assert.Equal("abc-123", answer.SessionId);

        // A line printed before the object (a warning) is passed over; the last object is the answer.
        var late = AiAnswer.Parse("Some warning\r\n{\"type\":\"result\",\"is_error\":false,\"result\":\"Yes.\",\"session_id\":\"s2\"}\r\n", string.Empty, 0);
        Assert.Equal("Yes.", late.Text);
        Assert.Equal("s2", late.SessionId);
    }

    [Fact]
    public void An_error_result_a_broken_output_and_nothing_at_all_are_errors_with_the_words_printed()
    {
        var reported = AiAnswer.Parse("""{"type":"result","subtype":"error_during_execution","is_error":true,"result":"Not logged in","session_id":"s3"}""", string.Empty, 1);
        Assert.True(reported.IsError);
        Assert.Equal("Not logged in", reported.Text);

        var broken = AiAnswer.Parse("{not json", "claude: unknown option --foo", 2);
        Assert.True(broken.IsError);
        Assert.Equal("claude: unknown option --foo", broken.Text);
        Assert.Null(broken.SessionId);

        var plain = AiAnswer.Parse("Please log in first", string.Empty, 1);
        Assert.True(plain.IsError);
        Assert.Equal("Please log in first", plain.Text);

        var nothing = AiAnswer.Parse(string.Empty, string.Empty, 3);
        Assert.True(nothing.IsError);
        Assert.Contains("exit code 3", nothing.Text);
    }

    [Fact]
    public void The_command_line_is_print_mode_with_the_read_tools_the_other_folders_the_model_and_the_session()
    {
        var arguments = AiRequest.Arguments([@"C:\notes", @"C:\work"], @"C:\notes", null, null);
        Assert.Equal(["-p", "--output-format", "json", "--allowedTools", "Read,Glob,Grep", "--add-dir", @"C:\work"], arguments);

        var full = AiRequest.Arguments([@"C:\notes"], @"c:\NOTES", "haiku", "s1");
        Assert.Equal(["-p", "--output-format", "json", "--allowedTools", "Read,Glob,Grep", "--model", "haiku", "--resume", "s1"], full);

        // Claude Code starts in its working folder (AH-2.1), which is never an --add-dir of its own.
        var start = AiRequest.StartInfo(@"C:\bin\claude.exe", @"C:\work\claude", [@"C:\work\claude", @"C:\notes"], null, null);
        Assert.Equal(@"C:\bin\claude.exe", start.FileName);
        Assert.Equal(@"C:\work\claude", start.WorkingDirectory);
        Assert.Equal("-p", start.ArgumentList[0]);
        Assert.Equal(["--add-dir", @"C:\notes"], start.ArgumentList.Skip(5));
    }

    [Fact]
    public void The_settings_round_trip_and_a_relative_claude_path_is_taken_from_the_settings_folder()
    {
        using var temp = new TempFolder("axkit-ai-tests");
        var path = Path.Combine(temp.Path, "ai-help.json");
        var missing = AiSettings.Load(path, out var error);
        Assert.Null(error);
        Assert.False(missing.Enabled);
        Assert.Empty(missing.Folders);
        Assert.Null(missing.Model);
        Assert.Null(missing.ResolvedClaudePath());
        // The working folder (AH-2.1, AH-D6): Documents\axKit Claude unless the file names one.
        Assert.Null(missing.WorkingFolder);
        Assert.Equal(AiSettings.DefaultWorkingFolder, missing.ResolvedWorkingFolder());
        Assert.Equal("axKit Claude", Path.GetFileName(AiSettings.DefaultWorkingFolder));

        missing.Enabled = true;
        missing.Folders = [@"C:\notes", @" C:\notes ", string.Empty, @"C:\work"];
        missing.Model = " ";
        missing.ClaudePath = "fake-claude.cmd";
        missing.WorkingFolder = " " + Path.Combine(temp.Path, "claude") + " ";
        missing.Save(path);
        var text = File.ReadAllText(path);
        Assert.Contains("\"enabled\": true", text);
        Assert.DoesNotContain("\"folder\"", text);

        var loaded = AiSettings.Load(path, out error);
        Assert.Null(error);
        Assert.True(loaded.Enabled);
        Assert.Equal([@"C:\notes", @"C:\work"], loaded.Folders);
        Assert.Null(loaded.Model);
        Assert.Equal(Path.Combine(temp.Path, "claude"), loaded.WorkingFolder);
        Assert.False(Directory.Exists(loaded.ResolvedWorkingFolder()));
        Assert.Equal(Path.Combine(temp.Path, "claude"), loaded.EnsureWorkingFolder());
        Assert.True(Directory.Exists(Path.Combine(temp.Path, "claude")));
        Assert.Equal(Path.Combine(temp.Path, "fake-claude.cmd"), loaded.ResolvedClaudePath());
        Assert.Null(loaded.FindClaude());
        File.WriteAllText(Path.Combine(temp.Path, "fake-claude.cmd"), "@echo off");
        Assert.Equal(Path.Combine(temp.Path, "fake-claude.cmd"), loaded.FindClaude());
    }

    [Fact]
    public void The_prompts_end_with_the_users_own_words_and_say_who_reads_the_answer()
    {
        var context = new WindowContext("axDown", "notes.md - axDown", "notes.md, line 3 of 10", "## In every window\r\n  F1: this list", "# axDown user guide\r\nText.", ["12 s ago: Find, Ctrl+F"]);
        var prompt = AiPrompts.AboutWindow(context, "  how do I turn word wrap off?  ");
        Assert.EndsWith(AiPrompts.Marker + "\nhow do I turn word wrap off?", prompt);
        Assert.Contains(AiPrompts.Reader, prompt);
        Assert.Contains("The app: axDown.", prompt);
        Assert.Contains("12 s ago: Find, Ctrl+F", prompt);
        Assert.Contains("F1: this list", prompt);
        Assert.Contains("# axDown user guide\nText.", prompt);

        var withoutGuide = AiPrompts.AboutWindow(context with { Guide = null, Recent = [] }, "q");
        Assert.DoesNotContain("user guide:", withoutGuide);
        Assert.DoesNotContain("last things", withoutGuide);

        var anywhere = AiPrompts.Anywhere("WINWORD", "report.docx - Word", "where did I save the budget?");
        Assert.EndsWith(AiPrompts.Marker + "\nwhere did I save the budget?", anywhere);
        Assert.Contains("WINWORD, its window titled \"report.docx - Word\"", anywhere);
        Assert.DoesNotContain("program in front", AiPrompts.Anywhere(null, null, "q"));

        var screen = AiPrompts.Screen(@"C:\pictures\last.png", "notepad", "Untitled - Notepad");
        Assert.Contains(@"C:\pictures\last.png", screen);
        Assert.Contains("notepad and is titled \"Untitled - Notepad\"", screen);
        Assert.Contains(AiPrompts.Reader, screen);

        // A picture file of the user's (axDescribe DE-2.7): the path, no window named, the kinds of picture considered.
        var picture = AiPrompts.Picture(@"C:\pictures\chart.png");
        Assert.Contains(@"C:\pictures\chart.png", picture);
        Assert.Contains(AiPrompts.Reader, picture);
        Assert.Contains("a chart", picture);
        Assert.DoesNotContain("The window belongs to", picture);

        Assert.Equal("say more", AiPrompts.FollowUp(" say more "));
    }
}
