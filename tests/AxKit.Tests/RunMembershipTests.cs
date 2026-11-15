using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// Which processes a window may reach (RunMembership, docs/axKit/automation.md "Only the run's own windows",
/// 2026-11-15): in a test run only the processes that carry the run's mark, so that a script's Close all windows,
/// window list or hand-over never reaches the user's own windows; outside a run every window of the bundle, as ever.
/// </summary>
public sealed class RunMembershipTests
{
    private static readonly string Mark = AppPaths.MarkOf(@"C:\Temp\axkit-automate\1234\home");
    private static readonly string Other = AppPaths.MarkOf(@"C:\Temp\axkit-automate\5678\home");

    [Fact]
    public void Outside_a_run_every_window_is_reached_as_ever()
    {
        Assert.True(RunMembership.Reaches(null, otherCarriesMark: false));
        Assert.True(RunMembership.Reaches(null, otherCarriesMark: true));
    }

    [Fact]
    public void In_a_run_only_a_process_that_carries_the_runs_mark_is_reached()
    {
        Assert.True(RunMembership.Reaches(Mark, otherCarriesMark: true));
        Assert.False(RunMembership.Reaches(Mark, otherCarriesMark: false));
    }

    [Fact]
    public void The_name_holds_the_mark_and_the_process_and_is_the_sessions_own()
    {
        Assert.Equal(@"Local\AxKit.Run." + Mark + ".4242", RunMembership.Name(Mark, 4242));
        Assert.NotEqual(RunMembership.Name(Mark, 4242), RunMembership.Name(Other, 4242));
        Assert.NotEqual(RunMembership.Name(Mark, 4242), RunMembership.Name(Mark, 4243));
    }

    [Fact]
    public void A_process_carries_the_mark_only_while_it_holds_it_and_only_its_own_runs()
    {
        // A process id no process of this test has: the mark is only a name, and the test holds it itself.
        const int process = 0x7FFF_FF01;
        Assert.False(RunMembership.Carries(Mark, process));
        using (RunMembership.Hold(Mark, process))
        {
            Assert.True(RunMembership.Carries(Mark, process));
            // Another run's window asks: not its run's. Another process of the same run: not marked.
            Assert.False(RunMembership.Carries(Other, process));
            Assert.False(RunMembership.Carries(Mark, process + 1));
        }

        // The holder gone, as when its process ends: Windows removed the name, so a reused process id carries nothing.
        Assert.False(RunMembership.Carries(Mark, process));
    }

    [Fact]
    public void The_test_process_is_in_no_run_and_reaches_every_process()
    {
        // The tests are started without a run's variables: AppPaths.RunMark is null and nothing is asked of Windows.
        Assert.Null(AppPaths.RunMark);
        Assert.True(RunMembership.Reaches(Environment.ProcessId));
        Assert.True(RunMembership.Reaches(0x7FFF_FF02));
    }
}
