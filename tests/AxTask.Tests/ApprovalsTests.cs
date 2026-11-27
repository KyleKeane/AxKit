using AxTask.Core;

namespace AxTask.Tests;

/// <summary>
/// The maintainer's signed approval of an outside report (docs/axKit/outside-reports.md sections 5 and 6): the text it
/// signs, a key made and opened only with the passphrase, and a signature that a changed entry, another key or a
/// changed pinned key does not pass. The keys are made here with few rounds, so that the tests stay quick.
/// </summary>
public sealed class ApprovalsTests
{
    private const string Passphrase = "correct horse battery";
    private const int TestRounds = 1000;

    private static readonly ApprovalEntry Entry = new(
        Approvals.Approved, "KyleKeane/AxKit-reports issue 15", "axStart: says nothing at its first launch", "New item",
        "axStart: on its first launch NVDA does not speak its list until it is started again", "High",
        "Reproduce a fresh start;\r\nmake the first showing take the foreground.\r\n", "Nothing found.\r\n", "2026-11-21");

    [Fact]
    public void The_text_holds_each_field_on_its_line_with_the_review_as_its_hash()
    {
        var text = Approvals.Text(Entry);

        Assert.StartsWith("axkit-approval 1\ndecision: approved\nreport: KyleKeane/AxKit-reports issue 15\n", text);
        Assert.Contains("\nproposed: Reproduce a fresh start;\\nmake the first showing take the foreground.\n", text);
        Assert.DoesNotContain("Nothing found", text);
        Assert.Matches("\nreview: [0-9a-f]{64}\n", text);
        Assert.EndsWith("date: 2026-11-21\n", text);
        // Line ends of the long fields do not change the text: the file may hold either.
        Assert.Equal(text, Approvals.Text(Entry with { Proposed = Entry.Proposed.Replace("\r\n", "\n"), Review = "Nothing found." }));
    }

    [Fact]
    public void A_line_end_and_the_two_characters_backslash_n_make_different_texts()
    {
        // The 2026-11-21 audit: before the backslash was doubled, a line end in the scope and the characters \ and n
        // made the same signed line, so one could be turned into the other under a standing signature.
        var lineEnd = Approvals.Text(Entry with { Proposed = "run tools\nnow" });
        var characters = Approvals.Text(Entry with { Proposed = "run tools\\nnow" });

        Assert.NotEqual(lineEnd, characters);
        Assert.Contains("\nproposed: run tools\\nnow\n", lineEnd);
        Assert.Contains("\nproposed: run tools\\\\nnow\n", characters);
    }

    [Theory]
    [InlineData("Proposed", "Make the list speak.\u200B And add a download.", "U+200B")]
    [InlineData("Proposed", "Make the list speak.\u202E", "U+202E")]
    [InlineData("Harm review", "Nothing found.\u0007", "U+0007")]
    [InlineData("Item", "axStart: a title\u2028with a separator", "U+2028")]
    [InlineData("Proposed", "Hidden tag \U000E0041 here", "U+E0041")]
    public void An_entry_with_a_character_that_is_not_spoken_is_not_signed(string field, string value, string named)
    {
        // What is signed is what was heard (OR-5.6).
        var refused = OutsideReports.Refusal(EntryWith(field, value));

        Assert.NotNull(refused);
        Assert.Contains($"The field {field} holds a character that is not spoken ({named}", refused, StringComparison.Ordinal);
    }

    [Fact]
    public void Half_a_pair_of_surrogates_is_not_spoken_either()
    {
        // Put into the document as a value: neither an attribute's text nor written JSON can hold half a pair.
        var entry = EntryWith("Proposed", "x");
        entry = entry.With("Proposed", System.Text.Json.Nodes.JsonValue.Create("Half a pair " + (char)0xD83D + " here"));
        var refused = OutsideReports.Refusal(entry);

        Assert.Contains("(U+D83D, character 13)", refused, StringComparison.Ordinal);
    }

    [Fact]
    public void Line_ends_tabs_and_letters_of_any_language_are_spoken()
    {
        Assert.Null(OutsideReports.Refusal(EntryWith("Proposed", "First line.\r\n\tSecond: caf\u00E9, \u65E5\u672C\u8A9E, \U0001F600.")));
    }

    [Fact]
    public void What_is_signed_is_the_entrys_own_values_whatever_the_lists_configuration_names()
    {
        // docs/axTask/forms-framework.md AF-11.3: a value the configuration does not name is kept and not shown. An
        // approval reads the file itself, so a configuration that names no field hides nothing from what is signed.
        var entry = EntryWith("Proposed", "Make the list speak.");
        var shown = AxKit.Core.ItemView.Document(AxKit.Core.ItemView.Of(entry, AxKit.Core.ListConfig.New([])));
        Assert.Equal(["Title"], shown.Fields.Select(field => field.Title));

        var signed = OutsideReports.EntryOf(entry, Approvals.Approved, "2026-11-21");
        Assert.Equal("Make the list speak.", signed.Proposed);
        Assert.Equal("Nothing found.", signed.Review);
        Assert.Equal("axStart: said nothing", signed.Title);
        Assert.Contains("Make the list speak.", OutsideReports.NoticeText(entry, approve: true), StringComparison.Ordinal);

        // Signed, the three values are written where the list has their fields, and nothing else changes.
        var config = AxKit.Core.ListConfig.New(
        [
            new("State", AxKit.Core.FieldTypes.Text, null, []),
            new("Approved on", AxKit.Core.FieldTypes.Date, null, []),
            new("Report", AxKit.Core.FieldTypes.Text, null, []),
            new("Approval", AxKit.Core.FieldTypes.Text, null, []),
        ]);
        var after = OutsideReports.Signed(entry, config, approve: true, "signature", "2026-11-21");
        Assert.Equal(OutsideReports.ApprovedState, after.Text("State"));
        Assert.Equal("2026-11-21", after.Text("Approved on"));
        Assert.Equal("signature", after.Text("Approval"));
        Assert.Equal("Make the list speak.", after.Text("Proposed"));
        // Approved on, which the entry lacked, goes right after State, the field the configuration has before it;
        // Approval, the configuration's last, after every field of it the entry holds.
        var names = after.Values.Select(pair => pair.Key).ToList();
        Assert.Equal(names.IndexOf("State") + 1, names.IndexOf("Approved on"));
        Assert.True(names.IndexOf("Approval") > names.IndexOf("Approved on"));
        Assert.Equal(OutsideReports.DeclinedState, OutsideReports.Signed(entry, config, approve: false, "s", "2026-11-21").Text("State"));
    }

    private static AxKit.Core.FlatItem EntryWith(string field, string value)
    {
        var fields = new Dictionary<string, string>
        {
            ["Report"] = "KyleKeane/AxKit-reports issue 15",
            ["State"] = OutsideReports.AwaitingApproval,
            ["Harm review"] = "Nothing found.",
            ["Category"] = "New item",
            ["Item"] = "axStart: a title",
            ["Proposed"] = "Make the list speak.",
            ["Proposed priority"] = "High",
        };
        fields[field] = value;
        var json = new System.Text.Json.Nodes.JsonObject { ["schema"] = "list.json", ["Title"] = "axStart: said nothing" };
        foreach (var (name, text) in fields)
        {
            json[name] = text;
        }

        return AxKit.Core.FlatItem.Parse(json.ToJsonString());
    }

    [Fact]
    public void A_signature_made_with_the_passphrase_verifies_with_the_public_half()
    {
        var (file, publicKey) = Approvals.MakeKey(Passphrase, TestRounds);
        var text = Approvals.Text(Entry);

        var signature = Approvals.Sign(text, file, Passphrase);

        Assert.True(Approvals.Verify(text, signature, publicKey));
        Assert.Equal(publicKey.Fingerprint, Approvals.PublicKeyOf(file).Fingerprint);
    }

    [Fact]
    public void Another_passphrase_opens_nothing_and_signs_nothing()
    {
        var (file, _) = Approvals.MakeKey(Passphrase, TestRounds);

        var refused = Assert.Throws<ApprovalException>(() => Approvals.Sign("x", file, "correct horse battery!"));
        Assert.Equal("That is not the approval passphrase.", refused.Message);
    }

    [Fact]
    public void A_short_passphrase_makes_no_key() =>
        Assert.Throws<ApprovalException>(() => Approvals.MakeKey("short", TestRounds));

    [Fact]
    public void A_changed_entry_does_not_verify()
    {
        var (file, publicKey) = Approvals.MakeKey(Passphrase, TestRounds);
        var signature = Approvals.Sign(Approvals.Text(Entry), file, Passphrase);

        foreach (var changed in new[]
        {
            Entry with { Proposed = Entry.Proposed + " And add a download." },
            Entry with { Review = "Something else." },
            Entry with { Category = "Same as an item" },
            Entry with { Item = "axKit: anything" },
            Entry with { Decision = Approvals.Declined },
            Entry with { Report = "KyleKeane/AxKit-reports issue 16" },
            Entry with { Priority = "Low" },
            Entry with { Date = "2026-11-22" },
        })
        {
            Assert.False(Approvals.Verify(Approvals.Text(changed), signature, publicKey));
        }
    }

    [Fact]
    public void Another_key_does_not_verify_and_nothing_unreadable_throws()
    {
        var (file, _) = Approvals.MakeKey(Passphrase, TestRounds);
        var (_, otherKey) = Approvals.MakeKey(Passphrase, TestRounds);
        var text = Approvals.Text(Entry);
        var signature = Approvals.Sign(text, file, Passphrase);

        Assert.False(Approvals.Verify(text, signature, otherKey));
        Assert.False(Approvals.Verify(text, "not base64 !", otherKey));
        Assert.False(Approvals.Verify(text, string.Empty, otherKey));
    }

    [Fact]
    public void A_private_half_put_beside_another_public_key_does_not_open()
    {
        var (file, _) = Approvals.MakeKey(Passphrase, TestRounds);
        var (otherFile, _) = Approvals.MakeKey(Passphrase, TestRounds);
        var other = System.Text.Json.Nodes.JsonNode.Parse(otherFile)!.AsObject();
        var swapped = System.Text.Json.Nodes.JsonNode.Parse(file)!.AsObject();
        swapped["modulus"] = (string?)other["modulus"];

        Assert.Throws<ApprovalException>(() => Approvals.Sign("x", swapped.ToJsonString(), Passphrase));
    }

    [Fact]
    public void The_pinned_key_reads_back_and_a_fingerprint_that_does_not_fit_is_none()
    {
        var (_, publicKey) = Approvals.MakeKey(Passphrase, TestRounds);
        var pinned = Approvals.PinnedText(publicKey);

        Assert.Equal(publicKey.Fingerprint, Approvals.ReadPinned(pinned)!.Fingerprint);
        Assert.Equal(publicKey.Fingerprint, Approvals.ReadPinned(pinned.Replace("\n", "\r\n"))!.Fingerprint);
        Assert.Null(Approvals.ReadPinned(pinned.Replace(publicKey.Fingerprint, "0000000000000000")));
        Assert.Null(Approvals.ReadPinned(null));
        Assert.Null(Approvals.ReadPinned("something else"));
        Assert.Matches("^[0-9a-f]{16}$", publicKey.Fingerprint);
    }

    [Fact]
    public void A_change_of_the_pinned_key_is_signed_by_the_old_key()
    {
        var (oldFile, oldKey) = Approvals.MakeKey(Passphrase, TestRounds);
        var (_, newKey) = Approvals.MakeKey("another long passphrase", TestRounds);
        var change = Approvals.ChangeText(newKey);

        var signature = Approvals.Sign(change, oldFile, Passphrase);

        Assert.Equal($"axkit-approval-key-change 1\nnew: {newKey.Fingerprint}\n", change);
        Assert.True(Approvals.Verify(change, signature, oldKey));
        Assert.False(Approvals.Verify(change, signature, newKey));
    }

    /// <summary>
    /// OR-6.3: a change of the key that fails on its way leaves nothing of itself: no line of a change that never
    /// happened (which would refuse every later change as "not pinned yet"), the public half and the key as before.
    /// </summary>
    [Fact]
    public void A_change_of_key_that_fails_midway_leaves_the_old_key_and_no_line_of_the_change()
    {
        using var temp = new TempFolder("approval-key-tests");
        var keyFile = Path.Combine(temp.Path, OutsideReports.KeyFileName);
        var pinned = Path.Combine(temp.Path, OutsideReports.PinnedFileName);
        var changeFile = Path.Combine(temp.Path, OutsideReports.ChangeFileName);
        var (oldFile, oldKey) = Approvals.MakeKey(Passphrase, TestRounds);
        OutsideReports.WriteKey(keyFile, oldFile, oldKey, change: null);
        Assert.Equal(oldFile, File.ReadAllText(keyFile));
        Assert.Equal(Approvals.PinnedText(oldKey), File.ReadAllText(pinned));
        Assert.False(File.Exists(changeFile));

        var (newFile, newKey) = Approvals.MakeKey("another long passphrase", TestRounds);
        var signature = Approvals.Sign(Approvals.ChangeText(newKey), oldFile, Passphrase);
        // The new key's file cannot be written: a folder has its name.
        Directory.CreateDirectory(keyFile + ".new");
        Assert.ThrowsAny<Exception>(() => OutsideReports.WriteKey(keyFile, newFile, newKey, signature));
        Assert.False(File.Exists(changeFile));
        Assert.Equal(Approvals.PinnedText(oldKey), File.ReadAllText(pinned));
        Assert.Equal(oldFile, File.ReadAllText(keyFile));

        // With the way free, the change is written whole.
        Directory.Delete(keyFile + ".new");
        OutsideReports.WriteKey(keyFile, newFile, newKey, signature);
        Assert.Equal($"Approval key changed: {signature}\r\n", File.ReadAllText(changeFile));
        Assert.Equal(Approvals.PinnedText(newKey), File.ReadAllText(pinned));
        Assert.Equal(newFile, File.ReadAllText(keyFile));
    }

    [Fact]
    public void A_file_that_is_no_key_file_is_said_and_not_thrown_through()
    {
        foreach (var file in new[] { "", "not json", "{}", "{\"format\":\"axkit-approval-key 1\"}", "[1]" })
        {
            Assert.Throws<ApprovalException>(() => Approvals.Sign("x", file, Passphrase));
        }
    }
}
