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

    [Fact]
    public void A_file_that_is_no_key_file_is_said_and_not_thrown_through()
    {
        foreach (var file in new[] { "", "not json", "{}", "{\"format\":\"axkit-approval-key 1\"}", "[1]" })
        {
            Assert.Throws<ApprovalException>(() => Approvals.Sign("x", file, Passphrase));
        }
    }
}
