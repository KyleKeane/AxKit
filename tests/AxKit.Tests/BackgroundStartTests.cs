using AxKit.Core;

namespace AxKit.Tests;

/// <summary>A background app that does not answer (docs/axKit/SPEC.md AX-1.9; repairs.md R-65): what a second start does, and which processes are the app's copy.</summary>
public sealed class BackgroundStartTests
{
    [Fact]
    public void A_copy_that_answered_ends_the_second_start_and_one_that_did_not_is_asked_about()
    {
        // The window found and answered: nothing to ask.
        Assert.Equal(SecondStart.End, BackgroundStart.Decide(windowFound: true, answered: true, hidden: false, testRun: false));
        // Found and silent: the copy hangs.
        Assert.Equal(SecondStart.Ask, BackgroundStart.Decide(windowFound: true, answered: false, hidden: false, testRun: false));
        // Not found, yet this start is the second, so the mutex is held: a copy without its window.
        Assert.Equal(SecondStart.Ask, BackgroundStart.Decide(windowFound: false, answered: false, hidden: false, testRun: false));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void A_start_at_sign_in_and_a_start_in_a_test_run_never_ask(bool windowFound, bool answered)
    {
        Assert.Equal(SecondStart.End, BackgroundStart.Decide(windowFound, answered, hidden: true, testRun: false));
        Assert.Equal(SecondStart.End, BackgroundStart.Decide(windowFound, answered, hidden: false, testRun: true));
    }

    [Fact]
    public void Only_a_command_line_with_the_apps_verb_is_the_apps_copy()
    {
        Assert.True(BackgroundStart.IsCopyOf(@"""C:\Program Files\AxKit\AxKit.exe"" clip --hidden", Dispatch.AxClip));
        Assert.True(BackgroundStart.IsCopyOf(@"C:\Tools\AxKit.exe CLIP", Dispatch.AxClip));
        Assert.True(BackgroundStart.IsCopyOf(@"AxKit.exe  ""start""", Dispatch.AxStart));
        // Another app of the same program file, a path that holds the verb, the program alone, a verb that only comes later, nothing at all.
        Assert.False(BackgroundStart.IsCopyOf(@"""C:\Program Files\AxKit\AxKit.exe"" start --hidden", Dispatch.AxClip));
        Assert.False(BackgroundStart.IsCopyOf(@"""C:\Program Files\AxKit\AxKit.exe"" file C:\clip", Dispatch.AxClip));
        Assert.False(BackgroundStart.IsCopyOf(@"""C:\clip\AxKit.exe""", Dispatch.AxClip));
        Assert.False(BackgroundStart.IsCopyOf(@"""C:\Program Files\AxKit\AxKit.exe"" claude clip", Dispatch.AxClip));
        Assert.False(BackgroundStart.IsCopyOf(null, Dispatch.AxClip));
        Assert.False(BackgroundStart.IsCopyOf("  ", Dispatch.AxClip));
    }

    [Fact]
    public void The_question_and_the_failure_name_the_app()
    {
        Assert.StartsWith("axClip is running", BackgroundStart.Question("axClip"));
        Assert.EndsWith("?", BackgroundStart.Question("axClip"));
        Assert.StartsWith("axHelp could not be ended", BackgroundStart.NotEnded("axHelp"));
    }
}
