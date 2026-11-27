using System.Text.Json.Nodes;
using AxKit.Core;
using AxPass.Core;

namespace AxPass.Tests;

/// <summary>The tool's output read into groups, entries and fields, as KeePassXC 2.8.0 printed it on a test database (docs/axPass/kdbx-notes.md section 8).</summary>
public class PassTests
{
    private const string Listing = "Top entry\r\nCafé, ünïcode\r\nEmail/\r\nEmail/Gmail\r\nEmail/Work/\r\nEmail/Work/[empty]\r\nRecycle Bin/\r\nRecycle Bin/Old\r\n";

    private const string Show = "Title: Gmail\r\nUserName: sam\r\nPassword: PROTECTED\r\nURL: https://mail.example.com\r\nNotes: line one\r\nline two: with a colon\r\nUuid: {6095b140-e977-4884-bcea-35d49c7adfaa}\r\nTags: \r\n";

    [Fact]
    public void ReadsTheGroupsAndEntries()
    {
        var tree = PassTree.Parse(Listing);
        Assert.Equal(["Email", "Recycle Bin", "Top entry", "Café, ünïcode"], tree.Level(string.Empty).Select(node => node.Name));
        Assert.Equal(["Work", "Gmail"], tree.Level("Email").Select(node => node.Name));
        Assert.Empty(tree.Level("Email/Work"));
        Assert.True(tree.HasGroup("Email/Work"));
        Assert.True(tree.HasEntry("Email/Gmail"));
        Assert.Equal(2, tree.CountIn("Email"));
        var gmail = tree.Nodes.Single(node => node.Name == "Gmail");
        Assert.Equal("Email", gmail.Group);
        Assert.False(gmail.IsGroup);
    }

    [Fact]
    public void TheRecycleBinComesLastAtTheTop()
    {
        var tree = PassTree.Parse("Recycle Bin/\r\nRecycle Bin/[empty]\r\nBanking/\r\nBanking/[empty]\r\n");
        Assert.Equal(["Banking", "Recycle Bin"], tree.Level(string.Empty, "Recycle Bin").Select(node => node.Name));
        // A group of that name that is not known to be the bin stands where the database has it.
        Assert.Equal(["Recycle Bin", "Banking"], tree.Level(string.Empty).Select(node => node.Name));
        Assert.True(PassBin.Holds("Recycle Bin", "Recycle Bin/Old"));
        Assert.True(PassBin.Holds("Recycle Bin", "Recycle Bin"));
        Assert.False(PassBin.Holds("Recycle Bin", "Recycle Bins/Old"));
        Assert.False(PassBin.Holds(null, "Recycle Bin/Old"));
    }

    // ---- The Recycle Bin as far as the tool's output says (PW-6.4; the review of 2026-11-27) ----

    private const string InfoOn = "UUID: {66123e79-c63b-417e-998d-e7e2fc34eaf3}\r\nName: \r\nCipher: AES 256-bit\r\nRecycle bin is enabled.\r\nLocation: t.kdbx\r\nNumber of groups: 1\r\n";

    [Fact]
    public void TheBinIsKnownOnlyByWhatTheToolSaysAndWhatWasSeen()
    {
        Assert.True(PassBin.IsOn(InfoOn));
        // Another line in its place (the bin off, or a tool that words it otherwise): not known to be on.
        Assert.False(PassBin.IsOn(InfoOn.Replace("Recycle bin is enabled.", "Recycle bin is not enabled.")));
        Assert.False(PassBin.IsOn("Note: Recycle bin is enabled. Really\r\n"));
        Assert.False(PassBin.IsOn(string.Empty));

        var withBin = PassTree.Parse(Listing);
        var without = PassTree.Parse("Top entry\r\nEmail/\r\nEmail/Gmail\r\nPapierkorb/\r\nPapierkorb/Old\r\n");
        // On, and the group of the tool's own name is there: that group.
        Assert.Equal("Recycle Bin", PassBin.Known(on: true, withBin, seen: null));
        // The same group in a database that is not known to have a bin is a group like any other.
        Assert.Null(PassBin.Known(on: false, withBin, seen: null));
        // On, but no group of that name: not known which group it is, whatever the groups are called.
        Assert.Null(PassBin.Known(on: true, without, seen: null));
        // A deleted entry was seen to arrive in a group: that group, while it is there.
        Assert.Equal("Papierkorb", PassBin.Known(on: false, without, seen: "Papierkorb"));
        Assert.Equal("Recycle Bin", PassBin.Known(on: true, withBin, seen: "Papierkorb"));
    }

    [Fact]
    public void WhatADeleteDoesIsSaidOnlyWhereItIsKnown()
    {
        var top = PassTree.NodeOf("Top entry", isGroup: false);
        var gmail = PassTree.NodeOf("Email/Gmail", isGroup: false);
        var email = PassTree.NodeOf("Email", isGroup: true);
        var old = PassTree.NodeOf("Recycle Bin/Old", isGroup: false);

        // The bin known: to the bin outside it, for good inside it.
        Assert.Equal(DeleteFate.Recycled, PassBin.Fate(true, "Recycle Bin", gmail));
        Assert.Equal(DeleteFate.Recycled, PassBin.Fate(true, "Recycle Bin", email));
        Assert.Equal(DeleteFate.ForGood, PassBin.Fate(true, "Recycle Bin", old));
        // On, and not known which group: an entry at the top cannot be in the bin; anything else may be.
        Assert.Equal(DeleteFate.Recycled, PassBin.Fate(true, null, top));
        Assert.Equal(DeleteFate.NotKnown, PassBin.Fate(true, null, gmail));
        Assert.Equal(DeleteFate.NotKnown, PassBin.Fate(true, null, email));
        // Not known to be on: nothing is known.
        Assert.Equal(DeleteFate.NotKnown, PassBin.Fate(false, null, top));

        var (title, text) = PassBin.Question(true, [gmail]);
        Assert.Equal("Delete Gmail?", title);
        Assert.Equal("This database has a Recycle Bin, but KeePassXC's tool does not say which group it is. If Gmail is in it already, it is deleted for good, and nothing brings it back.", text);
        (title, text) = PassBin.Question(false, [email], "2 items");
        Assert.Equal("Delete the group Email?", title);
        Assert.Equal("KeePassXC's tool does not say that this database has a Recycle Bin. It may be deleted for good, and nothing brings it back then. The group holds 2 items, which go with it.", text);
        (title, text) = PassBin.Question(false, [top, gmail]);
        Assert.Equal("Delete 2 entries?", title);
        Assert.Equal("KeePassXC's tool does not say that this database has a Recycle Bin. They may be deleted for good, and nothing brings them back then.\nTop entry\nGmail", text);
    }

    [Fact]
    public void WhatBecameOfADeletedEntryIsReadFromTheListingsBeforeAndAfter()
    {
        var before = PassTree.Parse("Top entry\r\nEmail/\r\nEmail/Gmail\r\nEmail/Old\r\nRecycle Bin/\r\nRecycle Bin/Old\r\n");
        var gmail = before.Nodes.Single(node => node.Name == "Gmail");
        var old = before.Nodes.Single(node => node.Path == "Email/Old");

        // Moved, and the one of its name in the bin: Ctrl+Z can bring it back.
        var moved = PassTree.Parse("Top entry\r\nEmail/\r\nEmail/Old\r\nRecycle Bin/\r\nRecycle Bin/Old\r\nRecycle Bin/Gmail\r\n");
        Assert.Equal([new Removed(gmail, "Recycle Bin", Sole: true)], PassBin.Outcome(before, moved, [gmail]));
        // Moved beside an older entry of its name: two lines of one path, and the tool reaches only the first.
        var twin = PassTree.Parse("Top entry\r\nEmail/\r\nEmail/Gmail\r\nRecycle Bin/\r\nRecycle Bin/Old\r\nRecycle Bin/Old\r\n");
        Assert.Equal(2, twin.Count("Recycle Bin/Old", isGroup: false));
        Assert.Equal([new Removed(old, "Recycle Bin", Sole: false)], PassBin.Outcome(before, twin, [old]));
        // In no group afterwards: deleted for good (the bin off, or it was in the bin).
        var gone = PassTree.Parse("Top entry\r\nEmail/\r\nEmail/Old\r\nRecycle Bin/\r\nRecycle Bin/Old\r\n");
        Assert.Equal([new Removed(gmail, null, Sole: false)], PassBin.Outcome(before, gone, [gmail]));
        // A bin under another name is found the same way: by what arrived in it.
        var other = PassTree.Parse("Top entry\r\nEmail/\r\nEmail/Old\r\nRecycle Bin/\r\nRecycle Bin/Old\r\nPapierkorb/\r\nPapierkorb/Gmail\r\n");
        Assert.Equal("Papierkorb", PassBin.MovedTo(before, other, gmail));
        // A group with a group of its own name inside: the nearest to the top is where it went.
        var nested = PassTree.Parse("A/\r\nA/A/\r\nA/A/[empty]\r\n");
        var after = PassTree.Parse("Recycle Bin/\r\nRecycle Bin/A/\r\nRecycle Bin/A/A/\r\nRecycle Bin/A/A/[empty]\r\n");
        Assert.Equal("Recycle Bin", PassBin.MovedTo(nested, after, nested.Nodes.First()));
    }

    [Fact]
    public void TheSentenceAfterADeleteSaysWhatTheListingShows()
    {
        var gmail = PassTree.NodeOf("Email/Gmail", isGroup: false);
        var bank = PassTree.NodeOf("Bank", isGroup: false);
        var work = PassTree.NodeOf("Work", isGroup: true);
        const string undo = "Ctrl+Z";

        Assert.Equal("Moved Gmail to the Recycle Bin; Ctrl+Z brings it back", PassBin.Said([new Removed(gmail, "Recycle Bin", true)], 1, false, undo));
        Assert.Equal("Moved Gmail to the Recycle Bin, the group Papierkorb; Ctrl+Z brings it back", PassBin.Said([new Removed(gmail, "Papierkorb", true)], 1, false, undo));
        Assert.Equal("Moved Gmail to the Recycle Bin. Ctrl+Z cannot bring it back: another entry there is called Gmail, and only KeePassXC's own window tells them apart",
            PassBin.Said([new Removed(gmail, "Recycle Bin", false)], 1, false, undo));
        Assert.Equal("Moved Work to the Recycle Bin", PassBin.Said([new Removed(work, "Recycle Bin", true)], 1, false, undo));
        // Asked for in the Recycle Bin, for good is what was asked; anywhere else it is said as what it is.
        Assert.Equal("Deleted Gmail", PassBin.Said([new Removed(gmail, null, false)], 1, true, undo));
        Assert.Equal("Deleted Gmail for good: it went to no Recycle Bin", PassBin.Said([new Removed(gmail, null, false)], 1, false, undo));

        Assert.Equal("Moved 2 entries to the Recycle Bin; Ctrl+Z brings them back",
            PassBin.Said([new Removed(gmail, "Recycle Bin", true), new Removed(bank, "Recycle Bin", true)], 2, false, undo));
        // The tool refused the third: what was moved until then.
        Assert.Equal("Moved 2 of 3 entries to the Recycle Bin; Ctrl+Z brings them back",
            PassBin.Said([new Removed(gmail, "Recycle Bin", true), new Removed(bank, "Recycle Bin", true)], 3, false, undo));
        Assert.Equal("Deleted 2 entries for good", PassBin.Said([new Removed(gmail, null, false), new Removed(bank, null, false)], 2, false, undo));
        Assert.Equal("Moved 1 entry to the Recycle Bin, deleted 1 entry for good; Ctrl+Z brings the moved ones back",
            PassBin.Said([new Removed(gmail, "Recycle Bin", true), new Removed(bank, null, false)], 2, false, undo));
        Assert.Equal("Moved 2 entries to the Recycle Bin; Ctrl+Z brings 1 of the moved ones back, the others share a name with another entry there",
            PassBin.Said([new Removed(gmail, "Recycle Bin", true), new Removed(bank, "Recycle Bin", false)], 2, false, undo));
        Assert.Equal("Moved 2 entries to the Recycle Bin. Ctrl+Z cannot bring them back: each shares its name with another entry there",
            PassBin.Said([new Removed(gmail, "Recycle Bin", false), new Removed(bank, "Recycle Bin", false)], 2, false, undo));

        Assert.Equal("Moved Gmail to the Recycle Bin", PassBin.StepSaid([new Removed(gmail, "Recycle Bin", true)]));
        Assert.Equal("Moved 2 entries to the Recycle Bin", PassBin.StepSaid([new Removed(gmail, "Recycle Bin", true), new Removed(bank, "Recycle Bin", true)]));
    }

    /// <summary>
    /// A run ended at its time limit failed, with nothing of what it had printed; one the tool finished is its own
    /// (the review of 2026-11-27: every run past the limit was taken as ended, also one that had done its change).
    /// </summary>
    [Fact]
    public void ARunEndedAtItsLimitFailedAndOneTheToolFinishedIsItsOwn()
    {
        var stopped = KeePassCli.Result(new ProcessResult(-1, "Email/\r\nEmail/Gm", string.Empty, TimedOut: true, Finished: false));
        Assert.False(stopped.Succeeded);
        Assert.Equal(string.Empty, stopped.Output);
        Assert.Contains("60 seconds", stopped.Why);
        var finished = KeePassCli.Result(new ProcessResult(0, "Email/\r\n", string.Empty, TimedOut: true, Finished: true));
        Assert.True(finished.Succeeded);
        Assert.Equal("Email/\r\n", finished.Output);
    }

    [Fact]
    public void ReadsAnEntryWithoutItsPassword()
    {
        var entry = EntryFields.Parse(Show);
        Assert.Equal(["Title", "UserName", "Password", "URL", "Notes"], entry.Fields.Select(field => field.Title));
        Assert.Equal("Gmail", entry.Title);
        Assert.Equal(FieldTypes.UserName, entry.Fields[1].Type);
        Assert.Equal(FieldTypes.Password, entry.Fields[2].Type);
        Assert.Equal(string.Empty, FieldTypes.TextOf(entry.Fields[2].Value));
        Assert.Equal("hidden", FieldTypes.Spoken(entry.Fields[2]));
        Assert.Equal("line one\nline two: with a colon", FieldTypes.TextOf(entry.Fields[4].Value));
    }

    [Fact]
    public void AChangeBecomesTheToolsOptions()
    {
        var before = EntryFields.Parse(Show);
        var after = before.WithValue(1, JsonValue.Create("sam2")).WithValue(3, JsonValue.Create(string.Empty));
        Assert.Equal(["-u", "sam2", "--url", ""], EntryFields.EditOptions(before, after));
        Assert.Empty(EntryFields.EditOptions(before, before));
        Assert.Equal(["-t", "Gmail personal"], EntryFields.EditOptions(before, before.WithTitle("Gmail personal")));
    }

    /// <summary>
    /// PW-6.1: KeePassXC's tool takes an option with an empty value as not given (asked of the tool, 2.8.0,
    /// 2026-11-21: alone it answers "Not changing any field", beside another change it drops it without a word), so a
    /// field that would lose its value is named before the tool is started, in axPass's own words.
    /// </summary>
    [Fact]
    public void AFieldThatWouldBeEmptiedIsNamedBeforeTheToolIsAsked()
    {
        var before = EntryFields.Parse(Show);
        var urlGone = before.WithValue(3, JsonValue.Create(string.Empty));
        Assert.Equal([EntryFields.Url], EntryFields.Emptied(before, urlGone));
        Assert.Equal("URL cannot be emptied here: KeePassXC's tool takes an empty value as no change. Empty it in KeePassXC's own window",
            EntryFields.CannotEmpty(EntryFields.Emptied(before, urlGone)));

        // Beside another change the emptied field is still named: the tool would drop it without a word.
        var both = urlGone.WithValue(1, JsonValue.Create(string.Empty));
        Assert.Equal([EntryFields.UserName, EntryFields.Url], EntryFields.Emptied(before, both));
        Assert.StartsWith("UserName and URL cannot be emptied here", EntryFields.CannotEmpty(EntryFields.Emptied(before, both)));
        Assert.EndsWith("Empty them in KeePassXC's own window", EntryFields.CannotEmpty(EntryFields.Emptied(before, both)));

        // A value changed, or written into an empty field, is no emptying; the way back from the second is.
        Assert.Empty(EntryFields.Emptied(before, before.WithValue(3, JsonValue.Create("https://example.org"))));
        Assert.Empty(EntryFields.Emptied(urlGone, before));
        Assert.Equal([EntryFields.Url], EntryFields.Emptied(before, urlGone));
        Assert.Empty(EntryFields.Emptied(before, before));
    }

    [Theory]
    [InlineData("Gmail", null)]
    [InlineData("a/b", "A name cannot hold a slash: KeePassXC's tool reads it as a group")]
    [InlineData("  ", "Type a name")]
    public void ASlashIsRefused(string name, string? why) => Assert.Equal(why, PassTree.RefusedName(name));

    [Fact]
    public void TheArgumentsPutTheDatabaseAfterTheOptions()
    {
        Assert.Equal(["edit", "-q", "-u", "x", "db.kdbx", "Email/Gmail"], KeePassCli.On("edit", "db.kdbx", ["-u", "x"], "Email/Gmail"));
        Assert.Equal("Email/Gmail", PassTree.Join("Email", "Gmail"));
        Assert.Equal("Gmail", PassTree.Join(string.Empty, "Gmail"));
    }

    [Fact]
    public void BackupsGoBesideTheDatabaseAndTheOldestGo()
    {
        using var temp = new TempFolder("axpass-backups");
        var folder = temp.Path;
        var db = Path.Combine(folder, "mine.kdbx");
        File.WriteAllBytes(db, [1, 2, 3]);
        var start = new DateTime(2026, 10, 24, 15, 30, 0);
        for (var i = 0; i < Backups.Keep + 3; i++)
        {
            Backups.Make(db, start.AddMinutes(i));
        }

        var kept = Backups.List(db);
        Assert.Equal(Backups.Keep, kept.Count);
        Assert.Equal(Path.Combine(folder, "mine backups", "mine 2026-10-24 15-52-00.kdbx"), kept[0]);
        Assert.EndsWith("mine 2026-10-24 15-33-00.kdbx", kept[^1]);
        Assert.Equal([1, 2, 3], File.ReadAllBytes(kept[0]));
        Assert.EndsWith(" - 2.kdbx", Backups.Make(db, start.AddMinutes(Backups.Keep + 2)));
        Assert.True(Backups.Changes("edit"));
        Assert.False(Backups.Changes("show"));
    }

    [Fact]
    public void OnlyKeePassXCsPublisherIsTheTool()
    {
        // repairs.md R-55: a valid signature is not enough, the signer must be KeePassXC's publisher.
        Assert.True(KeePassCli.IsPublisher("DroidMonkey Apps, LLC"));
        Assert.True(KeePassCli.IsPublisher(" droidmonkey apps, llc "));
        Assert.False(KeePassCli.IsPublisher("Microsoft Corporation"));
        Assert.False(KeePassCli.IsPublisher("DroidMonkey Apps"));
        Assert.False(KeePassCli.IsPublisher(string.Empty));
        Assert.False(KeePassCli.IsPublisher(null));

        // A file nobody signed.
        using var temp = new TempFolder("axpass-tests");
        var planted = Path.Combine(temp.Path, KeePassCli.ExeName);
        File.WriteAllText(planted, "not a program");
        Assert.Null(AxKit.Core.Signatures.SignerName(planted));
        Assert.False(KeePassCli.SignedByKeePassXC(planted));

        // A program another publisher signed validly: the one running these tests, where it is signed.
        if (Environment.ProcessPath is { } running && AxKit.Core.Signatures.IsTrusted(running))
        {
            Assert.NotNull(AxKit.Core.Signatures.SignerName(running));
            Assert.False(KeePassCli.SignedByKeePassXC(running));
        }

        // KeePassXC's own tool, where it is installed (not on the build servers).
        var installed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "KeePassXC", KeePassCli.ExeName);
        if (File.Exists(installed))
        {
            Assert.True(KeePassCli.SignedByKeePassXC(installed));
        }
    }

    /// <summary>The tool itself on a throwaway database, when KeePassXC is installed (not on the build servers, where it passes without running).</summary>
    [Fact]
    public void TheToolTakesThePasswordOnAPipe()
    {
        if (KeePassCli.Find(null) is not { } cli)
        {
            return;
        }

        using var temp = new TempFolder("axpass-tests");
        var folder = temp.Path;
        var db = Path.Combine(folder, "test.kdbx");
        const string pw = "test-master-1";
        Assert.True(KeePassCli.Run(cli, KeePassCli.On("db-create", db, ["-p"]), [pw, pw]).Succeeded);
        Assert.True(KeePassCli.Run(cli, KeePassCli.On("mkdir", db, null, "Email"), [pw]).Succeeded);
        Assert.True(KeePassCli.Run(cli, KeePassCli.On("add", db, ["-u", "sam", "-p"], "Email/Gmail"), [pw, "entry-1", "entry-1"]).Succeeded);
        var tree = PassTree.Parse(KeePassCli.Run(cli, KeePassCli.On("ls", db, ["-R", "-f"]), [pw]).Output);
        Assert.True(tree.HasEntry("Email/Gmail"));
        var entry = EntryFields.Parse(KeePassCli.Run(cli, KeePassCli.On("show", db, null, "Email/Gmail"), [pw]).Output);
        Assert.Equal("sam", EntryFields.ValueOf(entry, EntryFields.UserName));
        Assert.False(KeePassCli.Run(cli, KeePassCli.On("ls", db), ["wrong"]).Succeeded);
    }

    /// <summary>
    /// What axPass reads of the Recycle Bin, against the tool itself (PW-6.4; when KeePassXC is installed): the tool
    /// says the database has one; an entry removed arrives in a group, found by the listings; a second of its name
    /// makes two lines of one path, of which the tool reaches the first, the older one, which is why Ctrl+Z moves
    /// back only the one entry of a name; and an entry removed in the bin is in no group after.
    /// </summary>
    [Fact]
    public void TheToolsOwnOutputSaysWhatBecameOfADeletedEntry()
    {
        if (KeePassCli.Find(null) is not { } cli)
        {
            return;
        }

        using var temp = new TempFolder("axpass-bin");
        var db = Path.Combine(temp.Path, "test.kdbx");
        const string pw = "test-master-1";
        CliResult Run(string command, IEnumerable<string>? options, params string[] rest) => KeePassCli.Run(cli, KeePassCli.On(command, db, options, rest), [pw]);
        PassTree List() => PassTree.Parse(Run(KeePassCliTexts.List, KeePassCliTexts.ListOptions).Output);

        Assert.True(KeePassCli.Run(cli, KeePassCli.On(KeePassCliTexts.CreateDatabase, db, [KeePassCliTexts.PasswordPromptOption]), [pw, pw]).Succeeded);
        Assert.True(PassBin.IsOn(Run(KeePassCliTexts.Info, null).Output));
        Assert.True(Run(KeePassCliTexts.MakeGroup, null, "Email").Succeeded);
        Assert.True(Run(KeePassCliTexts.Add, [KeePassCliTexts.UserNameOption, "first"], "Gmail").Succeeded);
        Assert.True(Run(KeePassCliTexts.Add, [KeePassCliTexts.UserNameOption, "second"], "Email/Gmail").Succeeded);

        // A new database has no group for the bin yet: on, and not known which group.
        var start = List();
        Assert.Null(PassBin.Known(on: true, start, seen: null));
        var first = start.Nodes.Single(node => node.Path == "Gmail");
        Assert.Equal(DeleteFate.Recycled, PassBin.Fate(true, null, first));
        Assert.Equal(DeleteFate.NotKnown, PassBin.Fate(true, null, start.Nodes.Single(node => node.Path == "Email/Gmail")));

        Assert.True(Run(KeePassCliTexts.Remove, null, "Gmail").Succeeded);
        var one = List();
        var moved = Assert.Single(PassBin.Outcome(start, one, [first]));
        Assert.Equal(new Removed(first, KeePassCliTexts.RecycleBin, Sole: true), moved);
        Assert.Equal(KeePassCliTexts.RecycleBin, PassBin.Known(on: true, one, seen: null));

        // A second entry of the same name: two in the bin, one path.
        var second = one.Nodes.Single(node => node.Path == "Email/Gmail");
        Assert.True(Run(KeePassCliTexts.Remove, null, "Email/Gmail").Succeeded);
        var two = List();
        Assert.Equal(new Removed(second, KeePassCliTexts.RecycleBin, Sole: false), Assert.Single(PassBin.Outcome(one, two, [second])));
        // The path reaches the older one, which is why the newer one is never moved back by it.
        var reached = EntryFields.Parse(Run(KeePassCliTexts.Show, null, PassTree.Join(KeePassCliTexts.RecycleBin, "Gmail")).Output);
        Assert.Equal("first", EntryFields.ValueOf(reached, EntryFields.UserName));

        // Removed in the bin: in no group afterwards, deleted for good.
        var inBin = two.Nodes.Single(node => node.Path == PassTree.Join(KeePassCliTexts.RecycleBin, "Gmail"));
        Assert.Equal(DeleteFate.ForGood, PassBin.Fate(true, KeePassCliTexts.RecycleBin, inBin));
        Assert.True(Run(KeePassCliTexts.Remove, null, inBin.Path).Succeeded);
        var three = List();
        Assert.Equal(new Removed(inBin, null, Sole: false), Assert.Single(PassBin.Outcome(two, three, [inBin])));
        Assert.Equal(1, three.Count(inBin.Path, isGroup: false));
    }
}
