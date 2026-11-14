using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core.Reports;

namespace AxKit.Tests;

/// <summary>
/// A report sealed for the maintainer's key and its JSON envelope (docs/axKit/reports.md RP-6.1a). The keys here are
/// throwaway pairs made in memory for the run: the maintainer's private key is on his computer alone and no test
/// reads it.
/// </summary>
public sealed class ReportSealTests
{
    /// <summary>A pair made for the tests: the public half as the code holds one, the private half as PKCS#8.</summary>
    internal sealed record Pair(string Public, byte[] Private)
    {
        public static Pair New()
        {
            using var rsa = RSA.Create(ReportSeal.KeyBits);
            return new Pair(Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), rsa.ExportPkcs8PrivateKey());
        }
    }

    internal static readonly Pair Keys = Pair.New();
    private static readonly Pair Other = Pair.New();
    private static readonly Guid Reporter = Guid.Parse("6f1c2d3e-4a5b-4c6d-8e7f-901234567890");
    private const string Words = "The heron list forgets my marmalade folder after Ctrl+F.";

    private static string Seal(string text = Words) => ReportSeal.Seal(text, Reporter, "5.9.0", "bug", Keys.Public);

    private static string With(string envelope, string member, string value)
    {
        var node = JsonNode.Parse(envelope)!.AsObject();
        node[member] = value;
        return node.ToJsonString();
    }

    private static string WithChangedByte(string envelope, string member)
    {
        var bytes = Convert.FromBase64String(JsonNode.Parse(envelope)![member]!.GetValue<string>());
        bytes[bytes.Length / 2] ^= 0x01;
        return With(envelope, member, Convert.ToBase64String(bytes));
    }

    [Fact]
    public void The_envelope_is_one_json_object_with_these_members_in_this_order()
    {
        var envelope = Seal();
        using var json = JsonDocument.Parse(envelope);
        Assert.Equal(JsonValueKind.Object, json.RootElement.ValueKind);
        Assert.Equal(
            ["format", "note", "reporter", "version", "kind", "key", "method", "wrappedKey", "nonce", "tag", "body"],
            json.RootElement.EnumerateObject().Select(member => member.Name));
        Assert.All(json.RootElement.EnumerateObject(), member => Assert.Equal(JsonValueKind.String, member.Value.ValueKind));
        Assert.Equal("axkit-report 1", json.RootElement.GetProperty("format").GetString());
        Assert.Equal("Encrypted for axKit's maintainer; only the maintainer's key opens it.", json.RootElement.GetProperty("note").GetString());
        Assert.Equal("6f1c2d3e-4a5b-4c6d-8e7f-901234567890", json.RootElement.GetProperty("reporter").GetString());
        Assert.Equal("5.9.0", json.RootElement.GetProperty("version").GetString());
        Assert.Equal("bug", json.RootElement.GetProperty("kind").GetString());
        Assert.Equal("RSA-OAEP-SHA256+AES-256-GCM", json.RootElement.GetProperty("method").GetString());
        Assert.Matches("^[0-9a-f]{16}$", json.RootElement.GetProperty("key").GetString()!);
        Assert.Equal(ReportSeal.KeyBits / 8, Convert.FromBase64String(json.RootElement.GetProperty("wrappedKey").GetString()!).Length);
        Assert.Equal(12, Convert.FromBase64String(json.RootElement.GetProperty("nonce").GetString()!).Length);
        Assert.Equal(16, Convert.FromBase64String(json.RootElement.GetProperty("tag").GetString()!).Length);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(Words), Convert.FromBase64String(json.RootElement.GetProperty("body").GetString()!).Length);
        // Indented, one member a line, line feeds alone, and readable as it is: no + for a plus sign.
        Assert.StartsWith("{\n  \"format\": \"axkit-report 1\",\n  \"note\": ", envelope);
        Assert.EndsWith("\"\n}\n", envelope);
        Assert.DoesNotContain("\r", envelope);
        Assert.DoesNotContain("\\u", envelope);
    }

    [Fact]
    public void The_key_member_is_a_fingerprint_of_the_public_key_and_not_the_key()
    {
        var envelope = ReportSeal.Read(Seal())!;
        var spki = Convert.FromBase64String(Keys.Public);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(spki))[..16].ToLowerInvariant(), envelope.Key);
        Assert.Equal(ReportSeal.Fingerprint(Keys.Public), envelope.Key);
        Assert.NotEqual(ReportSeal.Fingerprint(Other.Public), envelope.Key);
        Assert.DoesNotContain(Keys.Public[..40], Seal());
    }

    [Fact]
    public void The_reporter_the_version_and_the_kind_are_read_without_any_key()
    {
        var envelope = ReportSeal.Read(Seal());
        Assert.NotNull(envelope);
        Assert.Equal(Reporter, envelope.Reporter);
        Assert.Equal("5.9.0", envelope.Version);
        Assert.Equal("bug", envelope.Kind);
        Assert.Equal(ReportSeal.Format, envelope.Format);
        Assert.Equal(ReportSeal.Method, envelope.Method);

        // Inside an issue's body, with its code fence; and none in a body that has no envelope.
        Assert.Equal(Reporter, ReportSeal.Read("```json\n" + Seal() + "```\n")?.Reporter);
        Assert.Null(ReportSeal.Read("A report typed in the browser.\n\nSent from axKit 5.8.0 with F4; reporter 6f1c2d3e-4a5b-4c6d-8e7f-901234567890\n"));
        Assert.Null(ReportSeal.Read("{\"format\": \"something else 1\"}"));
        Assert.Null(ReportSeal.Read("{ not json }"));
        Assert.Null(ReportSeal.Read(string.Empty));
    }

    [Fact]
    public void No_word_of_the_report_is_in_the_envelope()
    {
        var envelope = Seal();
        foreach (var word in Words.Split(' ', '.', '+'))
        {
            // Words of six letters and more: a shorter one can turn up in base64 by chance.
            if (word.Length > 5)
            {
                Assert.DoesNotContain(word, envelope, StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.DoesNotContain(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Words))[..24], envelope);
    }

    [Theory]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("Line one\r\nLine two\n\n```text\n12 s ago: Find, Ctrl+F\n```\n")]
    [InlineData("Grüße aus Zürich: façade, naïve, 日本語, עברית, 🎧 and a \"quote\" with a \\ backslash")]
    public void A_text_comes_back_as_it_went_in(string text)
    {
        Assert.Equal(text, ReportSeal.Open(Seal(text), Keys.Private));
    }

    [Fact]
    public void A_large_report_comes_back_and_two_seals_of_one_text_differ()
    {
        var large = string.Join('\n', Enumerable.Range(0, 20000).Select(i => $"line {i}: the quick brown fox, ß and é"));
        Assert.True(large.Length > 500_000);
        Assert.Equal(large, ReportSeal.Open(Seal(large), Keys.Private));

        var first = ReportSeal.Read(Seal())!;
        var second = ReportSeal.Read(Seal())!;
        Assert.NotEqual(first.Nonce, second.Nonce);
        Assert.NotEqual(first.WrappedKey, second.WrappedKey);
        Assert.NotEqual(first.Body, second.Body);
    }

    [Fact]
    public void Another_key_is_refused_by_its_fingerprint_before_anything_is_decrypted()
    {
        var ex = Assert.Throws<ReportSealException>(() => ReportSeal.Open(Seal(), Other.Private));
        Assert.Contains("another private key opens it", ex.Message);
        Assert.Contains(ReportSeal.Fingerprint(Keys.Public), ex.Message);
        Assert.Contains(ReportSeal.Fingerprint(Other.Public), ex.Message);

        // An envelope that claims the other key's fingerprint still does not open with it.
        var claimed = With(Seal(), "key", ReportSeal.Fingerprint(Other.Public));
        Assert.Throws<ReportSealException>(() => ReportSeal.Open(claimed, Other.Private));
        // And a fingerprint that is not this key's is refused even where the key would open the rest.
        var wrong = Assert.Throws<ReportSealException>(() => ReportSeal.Open(With(Seal(), "key", "0123456789abcdef"), Keys.Private));
        Assert.Contains("0123456789abcdef", wrong.Message);

        Assert.Throws<ReportSealException>(() => ReportSeal.Open(Seal(), [1, 2, 3]));
    }

    [Theory]
    [InlineData("wrappedKey")]
    [InlineData("nonce")]
    [InlineData("tag")]
    [InlineData("body")]
    public void A_changed_byte_in_a_sealed_member_is_refused(string member)
    {
        var changed = WithChangedByte(Seal(), member);
        var ex = Assert.Throws<ReportSealException>(() => ReportSeal.Open(changed, Keys.Private));
        Assert.Matches("changed|does not open", ex.Message);
    }

    [Theory]
    [InlineData("reporter", "00000000-0000-4000-8000-000000000001")]
    [InlineData("version", "5.9.1")]
    [InlineData("kind", "feedback")]
    public void A_changed_member_in_the_clear_is_refused(string member, string value)
    {
        var ex = Assert.Throws<ReportSealException>(() => ReportSeal.Open(With(Seal(), member, value), Keys.Private));
        Assert.Contains("its check does not hold", ex.Message);
    }

    [Fact]
    public void An_unknown_format_or_method_is_refused_with_its_name()
    {
        var format = Assert.Throws<ReportSealException>(() => ReportSeal.Open(With(Seal(), "format", "axkit-report 2"), Keys.Private));
        Assert.Contains("format is \"axkit-report 2\"", format.Message);
        var method = Assert.Throws<ReportSealException>(() => ReportSeal.Open(With(Seal(), "method", "ROT13"), Keys.Private));
        Assert.Contains("method is \"ROT13\"", method.Message);
    }

    [Fact]
    public void A_cut_or_broken_envelope_is_refused()
    {
        var envelope = Seal();
        // Cut in the middle: no whole object is left.
        var none = Assert.Throws<ReportSealException>(() => ReportSeal.Open(envelope[..(envelope.Length / 2)], Keys.Private));
        Assert.Contains("No sealed report here", none.Message);
        Assert.Throws<ReportSealException>(() => ReportSeal.Open("Plain words, no envelope.", Keys.Private));

        // A body cut short, a member that is not base64, a member missing, a nonce of the wrong length.
        var body = JsonNode.Parse(envelope)!["body"]!.GetValue<string>();
        Assert.Throws<ReportSealException>(() => ReportSeal.Open(With(envelope, "body", Convert.ToBase64String(Convert.FromBase64String(body)[..^5])), Keys.Private));
        Assert.Throws<ReportSealException>(() => ReportSeal.Open(With(envelope, "body", "not base64!"), Keys.Private));
        Assert.Throws<ReportSealException>(() => ReportSeal.Open(With(envelope, "nonce", "AAAA"), Keys.Private));
        var missing = JsonNode.Parse(envelope)!.AsObject();
        missing.Remove("tag");
        Assert.Throws<ReportSealException>(() => ReportSeal.Open(missing.ToJsonString(), Keys.Private));
    }

    [Fact]
    public void A_public_key_that_is_none_cannot_seal()
    {
        Assert.Throws<ReportSealException>(() => ReportSeal.Seal(Words, Reporter, "5.9.0", "bug", "not a key"));
        Assert.Throws<ReportSealException>(() => ReportSeal.Seal(Words, Reporter, "5.9.0", "bug", Convert.ToBase64String([1, 2, 3])));
    }

    [Fact]
    public void The_key_built_into_the_code_is_an_rsa_key_of_the_stated_size_and_seals()
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(ReportSeal.PublicKey), out var read);
        Assert.Equal(Convert.FromBase64String(ReportSeal.PublicKey).Length, read);
        Assert.Equal(ReportSeal.KeyBits, rsa.KeySize);

        // Sealed for it, and not opened: its private half is nowhere near the tests.
        var envelope = ReportSeal.Read(ReportSeal.Seal(Words, Reporter, "5.9.0", "bug", ReportSeal.PublicKey));
        Assert.Equal(ReportSeal.Fingerprint(ReportSeal.PublicKey), envelope?.Key);
        Assert.Throws<ReportSealException>(() => ReportSeal.Open(ReportSeal.ToJson(envelope!), Keys.Private));
    }
}
