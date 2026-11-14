using AxProcess.Core;
using Microsoft.Win32;

namespace AxProcess.Tests;

/// <summary>
/// Turn off, Turn on and Remove in the Startup view (docs/axProcess/SPEC.md PR-5.4 to PR-5.8). The decisions, the bytes
/// and the words are tested from data, without the registry. The registry and the folders are tested on a stand-in of
/// this test's own (<see cref="StartupPlaces.Under"/>): a key under HKEY_CURRENT_USER\Software\AxKit\Tests and a
/// folder under the temporary folder, both deleted at the end. No test here may change a real start-up entry of the
/// computer, not even one of its own making: none names the real places for a change, which
/// <see cref="No_test_names_the_real_places"/> holds to.
/// </summary>
public sealed class StartupChangeTests : IDisposable
{
    private static readonly StartupEntry UserEntry = new("KeePassXC", "\"C:\\Program Files\\KeePassXC\\KeePassXC.exe\"", StartupPlace.UserRunKey, true);
    private static readonly StartupEntry ComputerEntry = new("Updater", "C:\\Tools\\updater.exe", StartupPlace.MachineRunKey, true);
    private static readonly StartupEntry FolderEntry = new("Morning", "C:\\Startup\\Morning.lnk", StartupPlace.UserFolder, false);

    private readonly string _root = StartupPlaces.StandInParent + @"Tests\Startup-" + Guid.NewGuid().ToString("N");
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "axkit-startup-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_root, throwOnMissingSubKey: false);
        // The keys above it go too once they are empty, so that nothing of the tests stays in the registry.
        foreach (var parent in new[] { StartupPlaces.StandInParent + "Tests", StartupPlaces.StandInParent.TrimEnd('\\') })
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(parent);
                if (key is not { SubKeyCount: 0, ValueCount: 0 })
                {
                    break;
                }

                Registry.CurrentUser.DeleteSubKey(parent, throwOnMissingSubKey: false);
            }
            catch (InvalidOperationException)
            {
                // Another test's key came meanwhile: the last one to end takes the parent away.
                break;
            }
        }

        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    // ---- the decisions, the bytes and the words: no registry ----

    [Theory]
    [InlineData(StartupPlace.UserRunKey, false)]
    [InlineData(StartupPlace.UserFolder, false)]
    [InlineData(StartupPlace.MachineRunKey, true)]
    [InlineData(StartupPlace.MachineRunKey32, true)]
    [InlineData(StartupPlace.CommonFolder, true)]
    public void Only_the_entries_of_the_whole_computer_need_administrator_rights(StartupPlace place, bool needs) =>
        Assert.Equal(needs, StartupChange.NeedsAdministrator(place));

    [Fact]
    public void The_switch_is_Task_Managers_twelve_bytes()
    {
        Assert.Equal(new byte[] { 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, StartupChange.ApprovedBytes(on: true, DateTime.UtcNow));

        var when = new DateTime(2026, 11, 14, 9, 30, 0, DateTimeKind.Utc);
        var off = StartupChange.ApprovedBytes(on: false, when);
        Assert.Equal(12, off.Length);
        Assert.Equal(new byte[] { 0x03, 0, 0, 0 }, off[..4]);
        Assert.Equal(when.ToFileTimeUtc(), BitConverter.ToInt64(off, 4));

        // What is written reads back as the list reads it (PR-5.2).
        Assert.False(StartupList.IsEnabled(off));
        Assert.True(StartupList.IsEnabled(StartupChange.ApprovedBytes(on: true, when)));
    }

    [Fact]
    public void The_switch_is_named_after_the_value_or_the_file()
    {
        Assert.Equal("KeePassXC", StartupChange.ApprovedName(UserEntry));
        Assert.Equal("Morning.lnk", StartupChange.ApprovedName(FolderEntry));
    }

    [Fact]
    public void What_is_refused_before_anything_is_tried()
    {
        // The user's own entries: only what is already so.
        Assert.Null(StartupChange.Refusal(StartupAction.TurnOff, UserEntry, administrator: false));
        Assert.Equal(StartupOutcome.Already, StartupChange.Refusal(StartupAction.TurnOn, UserEntry, administrator: false));
        Assert.Equal(StartupOutcome.Already, StartupChange.Refusal(StartupAction.TurnOff, FolderEntry, administrator: false));
        Assert.Null(StartupChange.Refusal(StartupAction.TurnOn, FolderEntry, administrator: false));
        Assert.Null(StartupChange.Refusal(StartupAction.Remove, UserEntry, administrator: false));

        // The whole computer's: every change, unless the process has administrator rights.
        foreach (var action in Enum.GetValues<StartupAction>())
        {
            Assert.Equal(StartupOutcome.NeedsAdministrator, StartupChange.Refusal(action, ComputerEntry, administrator: false));
        }

        Assert.Null(StartupChange.Refusal(StartupAction.TurnOff, ComputerEntry, administrator: true));
        Assert.Null(StartupChange.Refusal(StartupAction.Remove, ComputerEntry, administrator: true));
        Assert.Equal(StartupOutcome.Already, StartupChange.Refusal(StartupAction.TurnOn, ComputerEntry, administrator: true));
    }

    [Fact]
    public void Every_outcome_has_its_one_sentence()
    {
        static string Said(StartupAction action, StartupEntry entry, StartupOutcome outcome, string? error = null) =>
            StartupChange.Said(action, entry, new StartupResult(outcome, error));

        Assert.Equal("KeePassXC turned off: it no longer starts with Windows", Said(StartupAction.TurnOff, UserEntry, StartupOutcome.Done));
        Assert.Equal("KeePassXC turned on: it starts with Windows again", Said(StartupAction.TurnOn, UserEntry, StartupOutcome.Done));
        Assert.Equal("KeePassXC removed from the programs that start with Windows", Said(StartupAction.Remove, UserEntry, StartupOutcome.Done));
        Assert.Equal("KeePassXC is already turned off", Said(StartupAction.TurnOff, UserEntry, StartupOutcome.Already));
        Assert.Equal("KeePassXC is already turned on", Said(StartupAction.TurnOn, UserEntry, StartupOutcome.Already));
        Assert.Equal(
            "Updater is set for every user of this computer: turning it off needs administrator rights. Use Task Manager's Startup apps tab, or Settings, Apps, Startup",
            Said(StartupAction.TurnOff, ComputerEntry, StartupOutcome.NeedsAdministrator));
        Assert.Equal(
            "Updater is set for every user of this computer: turning it on needs administrator rights. Use Task Manager's Startup apps tab, or Settings, Apps, Startup",
            Said(StartupAction.TurnOn, ComputerEntry, StartupOutcome.NeedsAdministrator));
        Assert.Equal(
            "Updater is set for every user of this computer: removing it needs administrator rights. Task Manager's Startup apps tab, or Settings, Apps, Startup, turns it off",
            Said(StartupAction.Remove, ComputerEntry, StartupOutcome.NeedsAdministrator));
        Assert.Equal("KeePassXC is no longer among the programs that start with Windows. Nothing changed", Said(StartupAction.TurnOff, UserEntry, StartupOutcome.Gone));
        Assert.Equal("Could not turn off KeePassXC: access is denied. Nothing changed", Said(StartupAction.TurnOff, UserEntry, StartupOutcome.Denied));
        Assert.Equal("Could not turn on KeePassXC: access is denied. Nothing changed", Said(StartupAction.TurnOn, UserEntry, StartupOutcome.Denied));
        Assert.Equal("Could not remove KeePassXC: the disk is gone", Said(StartupAction.Remove, UserEntry, StartupOutcome.Failed, "the disk is gone"));
    }

    [Fact]
    public void The_question_before_a_removal_names_the_entry_its_command_and_its_place()
    {
        Assert.Equal("Remove KeePassXC from the programs that start with Windows?", StartupChange.RemoveQuestion(UserEntry));
        Assert.Equal(
            "Command \"C:\\Program Files\\KeePassXC\\KeePassXC.exe\"\n"
            + "Set in current user's Run key\n"
            + "The entry is deleted from the Run key for good. The program itself stays on the computer.\n"
            + "Turn off is the gentler way: it keeps the entry, and Turn on brings it back.",
            StartupChange.RemoveText(UserEntry));
        Assert.Contains("The file in the Startup folder is deleted for good, not sent to the Recycle Bin.", StartupChange.RemoveText(FolderEntry));
        Assert.Contains("Set in your Startup folder", StartupChange.RemoveText(FolderEntry));
    }

    // ---- the places: Windows' own, or a stand-in (PR-5.8) ----

    [Fact]
    public void A_test_runs_places_are_its_own_key_and_folder()
    {
        var places = StartupPlaces.For("0123456789AB", @"C:\run\home");
        Assert.Equal(@"Software\AxKit\Runs\0123456789AB\Startup", places.Root);
        Assert.Equal(@"C:\run\home\AxKit\startup\yours", places.UserFolder);
        Assert.Equal(@"C:\run\home\AxKit\startup\everyone", places.CommonFolder);
        Assert.False(places.Administrator);

        // Every key of a stand-in is the user's, under the stand-in's root: never a key Windows starts anything from.
        foreach (var place in Enum.GetValues<StartupPlace>())
        {
            foreach (var (hive, path) in new[] { places.RunKeyOf(place), places.ApprovedKeyOf(place) })
            {
                Assert.Equal("HKEY_CURRENT_USER", hive.Name);
                Assert.StartsWith(@"Software\AxKit\Runs\0123456789AB\Startup\", path);
            }
        }

        Assert.Equal(@"Software\AxKit\Runs\0123456789AB\Startup\User\Software\Microsoft\Windows\CurrentVersion\Run", places.RunKeyOf(StartupPlace.UserRunKey).Path);
        Assert.Equal(@"Software\AxKit\Runs\0123456789AB\Startup\Computer\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", places.RunKeyOf(StartupPlace.MachineRunKey32).Path);
    }

    [Fact]
    public void Windows_own_places_are_the_keys_Task_Manager_uses()
    {
        // Looked at, never written: the paths and hives of the real places.
        var places = StartupPlaces.For(null, null);
        Assert.Null(places.Root);
        Assert.Equal(("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Run"), Named(places.RunKeyOf(StartupPlace.UserRunKey)));
        Assert.Equal(("HKEY_LOCAL_MACHINE", @"Software\Microsoft\Windows\CurrentVersion\Run"), Named(places.RunKeyOf(StartupPlace.MachineRunKey)));
        Assert.Equal(("HKEY_LOCAL_MACHINE", @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"), Named(places.RunKeyOf(StartupPlace.MachineRunKey32)));
        Assert.Equal(("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"), Named(places.ApprovedKeyOf(StartupPlace.UserRunKey)));
        Assert.Equal(("HKEY_LOCAL_MACHINE", @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"), Named(places.ApprovedKeyOf(StartupPlace.MachineRunKey)));
        Assert.Equal(("HKEY_LOCAL_MACHINE", @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32"), Named(places.ApprovedKeyOf(StartupPlace.MachineRunKey32)));
        Assert.Equal(("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder"), Named(places.ApprovedKeyOf(StartupPlace.UserFolder)));
        Assert.Equal(("HKEY_LOCAL_MACHINE", @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder"), Named(places.ApprovedKeyOf(StartupPlace.CommonFolder)));

        static (string, string) Named((RegistryKey Hive, string Path) key) => (key.Hive.Name, key.Path);
    }

    [Theory]
    [InlineData(@"Software\Microsoft\Windows\CurrentVersion")]
    [InlineData(@"Software\AxKit\")]
    [InlineData(@"Software\AxKit\..\Microsoft")]
    [InlineData("")]
    public void A_stand_in_outside_axKits_own_key_is_refused(string root) =>
        Assert.Throws<ArgumentException>(() => StartupPlaces.Under(root, Path.GetTempPath()));

    // ---- the registry and the folders, on this test's stand-in ----

    private StartupPlaces Places => StartupPlaces.Under(_root, _folder);

    /// <summary>A value in the stand-in's Run key of a place.</summary>
    private void SeedValue(StartupPlace place, string name, string command)
    {
        var (hive, path) = Places.RunKeyOf(place);
        using var key = hive.CreateSubKey(path);
        key.SetValue(name, command);
    }

    private byte[]? ApprovedValue(StartupPlace place, string name)
    {
        var (hive, path) = Places.ApprovedKeyOf(place);
        using var key = hive.OpenSubKey(path);
        return key?.GetValue(name) as byte[];
    }

    private StartupEntry Entry(string name) => StartupList.Read(Places).Single(entry => entry.Name == name);

    [Fact]
    public void An_entry_of_the_users_Run_key_is_turned_off_and_on_again()
    {
        SeedValue(StartupPlace.UserRunKey, "Alpha", "C:\\Tools\\alpha.exe");
        SeedValue(StartupPlace.UserRunKey, "Beta", "C:\\Tools\\beta.exe");
        Assert.Equal(["Alpha, enabled, current user's Run key", "Beta, enabled, current user's Run key"], StartupList.Read(Places).Select(entry => entry.Label));

        Assert.Equal(StartupOutcome.Done, StartupList.Apply(Places, Entry("Alpha"), StartupAction.TurnOff).Outcome);
        Assert.Equal("Alpha, disabled, current user's Run key", Entry("Alpha").Label);
        Assert.Equal(0x03, ApprovedValue(StartupPlace.UserRunKey, "Alpha")![0]);
        Assert.True(Entry("Beta").Enabled);

        // The list's own record says it is off already: nothing is written a second time.
        Assert.Equal(StartupOutcome.Already, StartupList.Apply(Places, Entry("Alpha"), StartupAction.TurnOff).Outcome);

        Assert.Equal(StartupOutcome.Done, StartupList.Apply(Places, Entry("Alpha"), StartupAction.TurnOn).Outcome);
        Assert.True(Entry("Alpha").Enabled);
        Assert.Equal(new byte[] { 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, ApprovedValue(StartupPlace.UserRunKey, "Alpha"));

        // Turning off and on deleted nothing: the command is as it was.
        Assert.Equal("C:\\Tools\\alpha.exe", Entry("Alpha").Command);
    }

    [Fact]
    public void A_removed_entry_is_gone_with_its_switch_and_the_others_stay()
    {
        SeedValue(StartupPlace.UserRunKey, "Alpha", "C:\\Tools\\alpha.exe");
        SeedValue(StartupPlace.UserRunKey, "Beta", "C:\\Tools\\beta.exe");
        StartupList.Apply(Places, Entry("Alpha"), StartupAction.TurnOff);
        var alpha = Entry("Alpha");

        Assert.Equal(StartupOutcome.Done, StartupList.Apply(Places, alpha, StartupAction.Remove).Outcome);
        Assert.Equal(["Beta"], StartupList.Read(Places).Select(entry => entry.Name));
        Assert.Null(ApprovedValue(StartupPlace.UserRunKey, "Alpha"));

        // Asked again for the entry that is gone: said as such, nothing changed.
        Assert.Equal(StartupOutcome.Gone, StartupList.Apply(Places, alpha, StartupAction.Remove).Outcome);
        Assert.Equal(StartupOutcome.Gone, StartupList.Apply(Places, alpha, StartupAction.TurnOn).Outcome);
        Assert.Null(ApprovedValue(StartupPlace.UserRunKey, "Alpha"));
        Assert.Equal(["Beta"], StartupList.Read(Places).Select(entry => entry.Name));
    }

    [Fact]
    public void A_file_of_the_users_Startup_folder_is_turned_off_on_and_removed()
    {
        Directory.CreateDirectory(Places.UserFolder);
        var file = Path.Combine(Places.UserFolder, "Morning.lnk");
        File.WriteAllText(file, "a stand-in for a shortcut");
        Assert.Equal("Morning, enabled, your Startup folder", Entry("Morning").Label);

        Assert.Equal(StartupOutcome.Done, StartupList.Apply(Places, Entry("Morning"), StartupAction.TurnOff).Outcome);
        Assert.Equal("Morning, disabled, your Startup folder", Entry("Morning").Label);
        Assert.Equal(0x03, ApprovedValue(StartupPlace.UserFolder, "Morning.lnk")![0]);
        Assert.True(File.Exists(file));

        Assert.Equal(StartupOutcome.Done, StartupList.Apply(Places, Entry("Morning"), StartupAction.TurnOn).Outcome);
        Assert.True(Entry("Morning").Enabled);

        Assert.Equal(StartupOutcome.Done, StartupList.Apply(Places, Entry("Morning"), StartupAction.Remove).Outcome);
        Assert.False(File.Exists(file));
        Assert.Empty(StartupList.Read(Places));
        Assert.Null(ApprovedValue(StartupPlace.UserFolder, "Morning.lnk"));
    }

    [Fact]
    public void A_file_outside_the_Startup_folder_is_never_deleted()
    {
        Directory.CreateDirectory(Places.UserFolder);
        var elsewhere = Path.Combine(_folder, "Keep.lnk");
        File.WriteAllText(elsewhere, "not in the Startup folder");

        // An entry whose file is not in its place's folder is not that place's entry: gone, and the file stays.
        var entry = new StartupEntry("Keep", elsewhere, StartupPlace.UserFolder, true);
        Assert.Equal(StartupOutcome.Gone, StartupList.Apply(Places, entry, StartupAction.Remove).Outcome);
        Assert.True(File.Exists(elsewhere));
    }

    [Fact]
    public void An_entry_of_the_whole_computer_is_refused_and_left_as_it_was()
    {
        SeedValue(StartupPlace.MachineRunKey, "Updater", "C:\\Tools\\updater.exe");
        SeedValue(StartupPlace.MachineRunKey32, "Old", "C:\\Tools\\old.exe");
        Directory.CreateDirectory(Places.CommonFolder);
        var file = Path.Combine(Places.CommonFolder, "Everyone.lnk");
        File.WriteAllText(file, "a stand-in for a shortcut");

        foreach (var name in new[] { "Updater", "Old", "Everyone" })
        {
            foreach (var action in Enum.GetValues<StartupAction>())
            {
                Assert.Equal(StartupOutcome.NeedsAdministrator, StartupList.Apply(Places, Entry(name), action).Outcome);
            }
        }

        Assert.Equal(
            ["Everyone, enabled, all users' Startup folder", "Old, enabled, computer's Run key (32-bit)", "Updater, enabled, computer's Run key"],
            StartupList.Read(Places).Select(entry => entry.Label));
        Assert.Null(ApprovedValue(StartupPlace.MachineRunKey, "Updater"));
        Assert.True(File.Exists(file));
    }

    // ---- the safety rule itself ----

    /// <summary>
    /// The one way to the real places for a change is the property the window uses, <c>Current</c> of the places
    /// (PR-5.8). No test source names it, so no test can hand the real places to <see cref="StartupList.Apply"/>; the
    /// one read of the real list (<c>ServiceAndStartupTests</c>) takes no places and changes nothing.
    /// </summary>
    [Fact]
    public void No_test_names_the_real_places()
    {
        var folder = AppContext.BaseDirectory;
        while (folder is not null && !File.Exists(Path.Combine(folder, "AxKit.sln")))
        {
            folder = Path.GetDirectoryName(folder);
        }

        Assert.NotNull(folder);
        var forbidden = "StartupPlaces." + "Current";
        var files = Directory.GetFiles(Path.Combine(folder, "tests", "AxProcess.Tests"), "*.cs", SearchOption.TopDirectoryOnly);
        Assert.NotEmpty(files);
        Assert.All(files, file => Assert.False(File.ReadAllText(file).Contains(forbidden, StringComparison.Ordinal), $"{Path.GetFileName(file)} names the real startup places"));

        // And every Apply in the tests is given this test's stand-in.
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var apply = "StartupList." + "Apply(";
            for (var at = text.IndexOf(apply, StringComparison.Ordinal); at >= 0; at = text.IndexOf(apply, at + 1, StringComparison.Ordinal))
            {
                Assert.True(text.AsSpan(at + apply.Length).StartsWith("Places,"), $"{Path.GetFileName(file)}: an Apply that is not given the test's own places");
            }
        }
    }
}
