using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The last actions of a window for a report (docs/axKit/reports.md RP-4.1).</summary>
public sealed class RecentActionsTests
{
    private static readonly DateTime Now = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Lines_come_oldest_first_with_their_kind_and_age()
    {
        var recent = new RecentActions();
        recent.Add(RecentActionKind.Command, "Find, Ctrl+F", Now.AddSeconds(-12));
        recent.Add(RecentActionKind.Said, "3 matches", Now.AddSeconds(-11));
        recent.Add(RecentActionKind.Notice, "Save changes?", Now.AddMinutes(-2));
        Assert.Equal(["12 s ago: Find, Ctrl+F", "11 s ago, said: 3 matches", "2 min ago, notice: Save changes?"], recent.Lines(Now));
    }

    [Fact]
    public void Twenty_are_kept_and_the_oldest_go()
    {
        var recent = new RecentActions();
        for (var i = 0; i < 25; i++)
        {
            recent.Add(RecentActionKind.Said, $"line {i}", Now.AddSeconds(-30 + i));
        }

        Assert.Equal(RecentActions.Capacity, recent.Count);
        var lines = recent.Lines(Now);
        Assert.Equal("25 s ago, said: line 5", lines[0]);
        Assert.Equal("6 s ago, said: line 24", lines[^1]);
    }

    [Fact]
    public void Blank_text_is_not_recorded()
    {
        var recent = new RecentActions();
        recent.Add(RecentActionKind.Said, "  ", Now);
        recent.Add(RecentActionKind.Said, "", Now);
        Assert.Equal(0, recent.Count);
    }

    [Theory]
    [InlineData("&Find...", "Ctrl+F", "Find, Ctrl+F")]
    [InlineData("&Word wrap", null, "Word wrap")]
    [InlineData("Say the &status", "F8.", "Say the status, F8")]
    [InlineData("Save &as...", "", "Save as")]
    public void A_command_is_written_by_its_name_and_its_key(string label, string? key, string expected)
    {
        Assert.Equal(expected, RecentActions.CommandText(label, ["&Session"], key));
    }

    [Fact]
    public void An_entry_of_a_list_of_the_user_s_things_is_written_by_its_menus_and_its_number()
    {
        // RP-4.2a: a recent file's or a window's name may name the user's file; the report holds where it was chosen.
        Assert.Equal("Session, Open recent file, entry 2", RecentActions.CommandText(AxKitWindows.Label(1, @"notes.md (C:\notes\notes.md)"), ["&Session", "Open &recent file"], null));
        Assert.Equal("Window, entry b", RecentActions.CommandText(AxKitWindows.Label(10, "Tasks - axTask"), ["&Window"], null));
        Assert.Equal("Session, Remove project folder, entry 12", RecentActions.CommandText("12. Tasks (C:\\Tasks)", ["&Session", "Remove project folder"], null));
        // A menu above that is such an entry is not named either; a command of a fixed name is written by its name alone.
        Assert.Equal("Open with, entry 2, entry 1", RecentActions.CommandText("&1. notes.md", ["Open &with", "&2. axDown (C:\\axDown.exe)"], null));
        Assert.Equal("Close", RecentActions.CommandText("&Close", ["&Window", "&1. notes.md - axDown"], null));
    }

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(4, "just now")]
    [InlineData(5, "5 s ago")]
    [InlineData(59, "59 s ago")]
    [InlineData(60, "1 min ago")]
    [InlineData(3599, "59 min ago")]
    [InlineData(3600, "1 h ago")]
    [InlineData(86399, "23 h ago")]
    [InlineData(86400, "1 day ago")]
    [InlineData(3 * 86400, "3 days ago")]
    public void Ages_read_as_people_say_them(int seconds, string expected)
    {
        Assert.Equal(expected, RecentActions.Age(TimeSpan.FromSeconds(seconds)));
    }
}
