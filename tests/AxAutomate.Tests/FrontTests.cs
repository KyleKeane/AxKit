using AxAutomate.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The NVDA tier's rules of the foreground (Front, docs/axKit/automation.md section 3, 2026-11-15): when the runner
/// asks, when it takes the foreground itself, when the whole run stops, and when Escape leaves a menu.
/// </summary>
public sealed class FrontTests
{
    [Fact]
    public void The_window_is_asked_first_and_the_runner_takes_the_foreground_twice_at_most()
    {
        var moves = Enumerable.Range(0, Front.Looks).Select(Front.At).ToList();
        Assert.Equal(FrontMove.Ask, moves[0]);
        Assert.Equal(1, moves.Count(move => move == FrontMove.Ask));
        // Each take is an Alt key press that reaches the window in front: two a step, never more.
        Assert.Equal(2, moves.Count(move => move == FrontMove.Take));
        Assert.True(moves.IndexOf(FrontMove.Take) > 0);
        // After the last take there are looks left to see the window come up.
        Assert.True(moves.LastIndexOf(FrontMove.Take) < Front.Looks - 5);
    }

    [Fact]
    public void The_run_stops_only_when_its_first_shown_window_cannot_come_to_the_front()
    {
        Assert.True(Front.StopsRun(inFront: false, everInFront: false, windowShows: true));
        // In front: nothing to stop.
        Assert.False(Front.StopsRun(inFront: true, everInFront: false, windowShows: true));
        // A window of the run was in front before: this failure is one step's, and the run goes on.
        Assert.False(Front.StopsRun(inFront: false, everInFront: true, windowShows: true));
        // A background app started hidden shows no window and is never brought to the front.
        Assert.False(Front.StopsRun(inFront: false, everInFront: false, windowShows: false));
    }

    [Fact]
    public void A_stopped_run_says_one_plain_line_that_nvda_pass_shows()
    {
        var line = Front.CannotTake("explorer");
        Assert.StartsWith("The NVDA tier could not bring its window to the front: ", line, StringComparison.Ordinal);
        Assert.Contains("explorer", line, StringComparison.Ordinal);
        Assert.DoesNotContain('\n', line);
    }

    [Fact]
    public void Escape_leaves_a_menu_only_in_the_runs_own_window_in_front()
    {
        Assert.True(Front.LeavesMenu(windowInFront: true, menuActive: true));
        Assert.False(Front.LeavesMenu(windowInFront: true, menuActive: false));
        // Never a key to a window outside the run.
        Assert.False(Front.LeavesMenu(windowInFront: false, menuActive: true));
    }

    [Fact]
    public void Nvda_pass_shows_the_stopped_runs_lines()
    {
        var script = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "nvda-pass.ps1"));
        Assert.Contains("|STOPPED|", script, StringComparison.Ordinal);
        Assert.Contains("|The NVDA tier|", script, StringComparison.Ordinal);
    }
}
