using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Tests;

/// <summary>
/// The log of events (docs/axKit/events.md section 9 point 5, 10): off by default, nothing written while it is off, a
/// line of a time and a kind and nothing else, bounded by its roll-over, and the switch read from the bundle's file.
/// </summary>
public sealed class EventLoggerTests
{
    [Fact]
    public void The_switch_is_off_by_default()
    {
        Assert.False(new BundleSettings().KeepEventLog);
    }

    [Fact]
    public void A_line_is_a_time_and_a_kind_alone()
    {
        var line = EventLogger.Line(EventKind.LandedOnHeading, new DateTime(2026, 11, 25, 14, 3, 5, 123));
        Assert.Equal("2026-11-25 14:03:05.123 landed-on-heading", line);
    }

    [Fact]
    public void Nothing_is_written_while_it_is_off_and_kinds_alone_while_it_is_on()
    {
        using var temp = new TempFolder("event-log-tests");
        var path = Path.Combine(temp.Path, "events.log");
        var settings = Path.Combine(temp.Path, "bundle.json");
        new BundleSettings { KeepEventLog = true }.Save(settings);
        var before = (EventLogger.On, EventLogger.FilePath, EventLogger.SettingsPath);
        try
        {
            EventLogger.FilePath = path;
            EventLogger.SettingsPath = settings;
            EventLogger.On = false;
            EventLogger.Note(EventKind.Refused);
            EventLogger.Flush();
            Assert.False(File.Exists(path));

            EventLogger.On = true;
            EventLogger.Note(EventKind.Refused);
            EventLogger.Note(EventKind.Said);
            EventLogger.Flush();
            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.Matches(@"^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d\.\d{3} refused$", lines[0]);
            Assert.Matches(@"^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d\.\d{3} said$", lines[1]);
        }
        finally
        {
            EventLogger.On = before.On;
            EventLogger.FilePath = before.FilePath;
            EventLogger.SettingsPath = before.SettingsPath;
        }
    }

    [Fact]
    public void What_waits_is_dropped_once_the_switch_was_turned_off_in_another_window()
    {
        // Review, 2026-11-27: a window that was open and not activated went on writing after the log was turned off
        // elsewhere. The write now reads the switch first.
        using var temp = new TempFolder("event-log-tests");
        var path = Path.Combine(temp.Path, "events.log");
        var settings = Path.Combine(temp.Path, "bundle.json");
        var before = (EventLogger.On, EventLogger.FilePath, EventLogger.SettingsPath);
        try
        {
            EventLogger.FilePath = path;
            EventLogger.SettingsPath = settings;
            new BundleSettings { KeepEventLog = true }.Save(settings);
            EventLogger.FollowSettings(settings);
            Assert.True(EventLogger.On);

            // Another window turns the log off: the file says so, and this process has not looked yet.
            new BundleSettings { KeepEventLog = false }.Save(settings);
            File.SetLastWriteTimeUtc(settings, File.GetLastWriteTimeUtc(settings).AddMinutes(1));
            EventLogger.Note(EventKind.Refused);
            EventLogger.Flush();

            Assert.False(EventLogger.On);
            Assert.False(File.Exists(path));
        }
        finally
        {
            EventLogger.On = before.On;
            EventLogger.FilePath = before.FilePath;
            EventLogger.SettingsPath = before.SettingsPath;
        }
    }

    [Fact]
    public void A_full_file_becomes_the_old_one_and_a_new_one_begins()
    {
        using var temp = new TempFolder("event-log-tests");
        var path = Path.Combine(temp.Path, "events.log");
        File.WriteAllText(path, new string('x', (int)EventLogger.RollOverBytes + 1));

        EventLogger.Write(path, ["2026-11-25 14:03:05.123 done"]);

        Assert.Equal("2026-11-25 14:03:05.123 done\r\n", File.ReadAllText(path));
        Assert.Equal(EventLogger.RollOverBytes + 1, new FileInfo(Path.Combine(temp.Path, "events.old.log")).Length);
    }

    [Fact]
    public void The_switch_is_read_from_the_bundles_file()
    {
        using var temp = new TempFolder("event-log-tests");
        var path = Path.Combine(temp.Path, "bundle.json");
        var before = EventLogger.On;
        try
        {
            // Without the file the log is off.
            EventLogger.On = true;
            EventLogger.FollowSettings(path);
            Assert.False(EventLogger.On);

            new BundleSettings { KeepEventLog = true }.Save(path);
            EventLogger.FollowSettings(path);
            Assert.True(EventLogger.On);
        }
        finally
        {
            EventLogger.On = before;
        }
    }
}
