using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxKit.Core.Reports;

/// <summary>A sealed report could not be read or opened: it is no envelope, of a format or method not known, changed or cut off, or sealed for another key.</summary>
public sealed class ReportSealException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// The envelope of a sealed report (docs/axKit/reports.md RP-6.1a), the JSON object an issue's body holds. In the
/// clear: the format, a note, the install's random reporter id, axKit's version, the report's kind (its label), the
/// fingerprint of the public key it was sealed for and the method. Sealed: the report itself, as the wrapped key, the
/// nonce, the tag and the body, each base64. Nothing of the user's is in the clear.
/// </summary>
public sealed record ReportEnvelope(string Format, Guid Reporter, string Version, string Kind, string Key, string Method, string WrappedKey, string Nonce, string Tag, string Body);

/// <summary>
/// A report sealed on the user's computer for the maintainer alone (docs/axKit/reports.md RP-6.1a; the user's decisions
/// of 2026-11-14). The reporter token ships inside every copy of axKit and can read the reports repository's issues,
/// so whoever takes the token out of the program could read every report; a report's content is therefore encrypted
/// with the maintainer's public key before any network contact, and only the private key, which never leaves the
/// maintainer's computer (<c>tools/ReportKey</c>), opens it.
///
/// Hybrid encryption with what .NET has built in: a fresh random AES-256-GCM key and nonce per report, the AES key
/// wrapped with RSA (3072 bits) OAEP-SHA256. The output is one JSON object, <see cref="ReportEnvelope"/>, written
/// indented with its members in a fixed order. The members in the clear are GCM's associated data, so a sealed body
/// moved under another reporter id, version, kind, key or method does not open. What an envelope still tells beside
/// its members: about how long the report is.
/// </summary>
public static class ReportSeal
{
    public const string Format = "axkit-report 1";
    public const string Method = "RSA-OAEP-SHA256+AES-256-GCM";
    public const string Note = "Encrypted for axKit's maintainer; only the maintainer's key opens it.";
    public const int KeyBits = 3072;

    /// <summary>
    /// The public half of the maintainer's reports key, made 2026-11-14 with <c>tools/ReportKey new</c>:
    /// SubjectPublicKeyInfo in base64. The private half is on the maintainer's computer alone
    /// (docs/axKit/reports-setup.md section 5); without it no sealed report can be read.
    /// </summary>
    public const string PublicKey =
        "MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAvB4SOHg9Cbd7716C+AdXdDxqfzvOfbGtAE24jqcYLWHb36DGOSmM3QO4WWIlTPdKqeHN"
        + "LCGy1ULIEGNcXtuMVS2Qf57FrGDgrvGWoxnvKWFuEFEvzOso2tJ1lVrDapOnIf11IlLBXaXwnh9QMgRjy82qpPS5ZRrHoilV2HreAZKKBLq41OJB"
        + "Z4pse7RMSDJl4LnBL9mfc0Gt5hoyCatVKNvLoCWz1PBhhmG0CDTInp7ngm3ibrGv522ySS8DBhpTPQ8RT6eLF/x5gxuIH+hrb+DWZPd9+4ZPA3VJ"
        + "+RKgDMuJ7Pqmub5Gkb/NZzuROkMQSnbn/JUCi4SPpWwREgyi7LW5+YSb8EKj5bLzk1sIRh0Gl9yAyz/y2NcjbISq9RJ3fF5jiBTPRKRTNrKxae6F"
        + "MNjISSRe9OD3jczzKuChDGz6ul2CRDHe9/RFLLh7LRMhvTd7i/ZlihDqeppC1UYJdbIeQBxv815PmLQ3d6tz88KbzBlVUlCWhW9OAUqeYgvdAgMB"
        + "AAE=";

    private const int NonceLength = 12;
    private const int TagLength = 16;
    private const int AesKeyLength = 32;

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Which key a report was sealed for: the first sixteen hexadecimal characters of the SHA-256 of the public key's
    /// SubjectPublicKeyInfo. Not the key, and nothing of the user's; it tells the reader which private key opens a
    /// report, so that the maintainer's key can be replaced one day.
    /// </summary>
    public static string Fingerprint(byte[] subjectPublicKeyInfo) =>
        Convert.ToHexString(SHA256.HashData(subjectPublicKeyInfo))[..16].ToLowerInvariant();

    /// <summary>The fingerprint of a public key as the code holds it (SubjectPublicKeyInfo, base64).</summary>
    public static string Fingerprint(string publicKey) => Fingerprint(Convert.FromBase64String(publicKey));

    /// <summary>
    /// The report's text sealed for a public key (SubjectPublicKeyInfo, base64): the envelope as JSON, with the
    /// reporter id, the version and the kind's label in the clear beside it. Throws <see cref="ReportSealException"/>
    /// for a key that is none.
    /// </summary>
    public static string Seal(string plainText, Guid reporter, string version, string kind, string publicKey)
    {
        var plain = Encoding.UTF8.GetBytes(plainText);
        var key = RandomNumberGenerator.GetBytes(AesKeyLength);
        try
        {
            byte[] wrapped;
            string fingerprint;
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
                wrapped = rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256);
                fingerprint = Fingerprint(rsa.ExportSubjectPublicKeyInfo());
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException)
            {
                throw new ReportSealException("The report could not be encrypted: the public key is not a usable RSA key.", ex);
            }

            var nonce = RandomNumberGenerator.GetBytes(NonceLength);
            var tag = new byte[TagLength];
            var cipher = new byte[plain.Length];
            var envelope = new ReportEnvelope(Format, reporter, version.Trim(), kind.Trim(), fingerprint, Method, Convert.ToBase64String(wrapped), Convert.ToBase64String(nonce), string.Empty, string.Empty);
            using (var aes = new AesGcm(key, TagLength))
            {
                aes.Encrypt(nonce, plain, cipher, tag, Associated(envelope));
            }

            return ToJson(envelope with { Tag = Convert.ToBase64String(tag), Body = Convert.ToBase64String(cipher) });
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    /// <summary>The envelope as the issue's body holds it: one JSON object, indented, the members in this order.</summary>
    public static string ToJson(ReportEnvelope envelope) => new JsonObject
    {
        ["format"] = envelope.Format,
        ["note"] = Note,
        ["reporter"] = envelope.Reporter.ToString("D"),
        ["version"] = envelope.Version,
        ["kind"] = envelope.Kind,
        ["key"] = envelope.Key,
        ["method"] = envelope.Method,
        ["wrappedKey"] = envelope.WrappedKey,
        ["nonce"] = envelope.Nonce,
        ["tag"] = envelope.Tag,
        ["body"] = envelope.Body,
    }.ToJsonString(Indented).ReplaceLineEndings("\n") + "\n";

    /// <summary>
    /// The envelope a text holds, read without any key: the JSON object alone, or inside a longer text such as an
    /// issue's body with its code fence (from the first opening brace to the last closing one). Null when the text
    /// holds no such object, as an issue of an earlier version or one typed in the browser.
    /// </summary>
    public static ReportEnvelope? Read(string text)
    {
        var from = text.IndexOf('{');
        var to = text.LastIndexOf('}');
        if (from < 0 || to < from)
        {
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(text[from..(to + 1)]);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || Member(root, "format") is not { } format || !format.StartsWith("axkit-report ", StringComparison.Ordinal))
            {
                return null;
            }

            return new ReportEnvelope(
                format,
                Guid.TryParse(Member(root, "reporter"), out var reporter) ? reporter : Guid.Empty,
                Member(root, "version") ?? string.Empty,
                Member(root, "kind") ?? string.Empty,
                Member(root, "key") ?? string.Empty,
                Member(root, "method") ?? string.Empty,
                Member(root, "wrappedKey") ?? string.Empty,
                Member(root, "nonce") ?? string.Empty,
                Member(root, "tag") ?? string.Empty,
                Member(root, "body") ?? string.Empty);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// The plain text of a sealed report, the envelope alone or inside an issue's body, opened with the private key
    /// (PKCS#8). Throws <see cref="ReportSealException"/> with the reason: no envelope, a format or a method not
    /// known, a key fingerprint that is not this key's (each before anything is decrypted), a member changed or cut
    /// off, or another key.
    /// </summary>
    public static string Open(string sealedText, byte[] privateKey)
    {
        var envelope = Read(sealedText)
            ?? throw new ReportSealException("No sealed report here: the text holds no JSON object whose \"format\" starts with \"axkit-report\", or the object was cut off.");
        if (envelope.Format != Format)
        {
            throw new ReportSealException($"The sealed report's format is \"{envelope.Format}\", and this program reads \"{Format}\": a newer axKit made it, or it was changed.");
        }

        if (envelope.Method != Method)
        {
            throw new ReportSealException($"The sealed report's method is \"{envelope.Method}\", and this program reads \"{Method}\": a newer axKit made it, or it was changed.");
        }

        var key = Array.Empty<byte>();
        try
        {
            using var rsa = RSA.Create();
            try
            {
                rsa.ImportPkcs8PrivateKey(privateKey, out _);
            }
            catch (CryptographicException ex)
            {
                throw new ReportSealException("The private key is not a usable RSA key.", ex);
            }

            var mine = Fingerprint(rsa.ExportSubjectPublicKeyInfo());
            if (!string.Equals(envelope.Key, mine, StringComparison.OrdinalIgnoreCase))
            {
                throw new ReportSealException($"The report was sealed for the key {envelope.Key}, and this key is {mine}: another private key opens it.");
            }

            byte[] wrapped, nonce, tag, cipher;
            try
            {
                wrapped = Convert.FromBase64String(envelope.WrappedKey);
                nonce = Convert.FromBase64String(envelope.Nonce);
                tag = Convert.FromBase64String(envelope.Tag);
                cipher = Convert.FromBase64String(envelope.Body);
            }
            catch (FormatException ex)
            {
                throw new ReportSealException("The sealed report was changed or cut off: a member is not base64.", ex);
            }

            if (nonce.Length != NonceLength || tag.Length != TagLength || wrapped.Length == 0)
            {
                throw new ReportSealException("The sealed report was changed or cut off: the wrapped key, the nonce or the tag has the wrong length.");
            }

            try
            {
                key = rsa.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA256);
            }
            catch (CryptographicException ex)
            {
                throw new ReportSealException("The sealed report does not open: its wrapped key was changed, or it was sealed for another key.", ex);
            }

            if (key.Length != AesKeyLength)
            {
                throw new ReportSealException("The sealed report does not open: its wrapped key was changed, or it was sealed for another key.");
            }

            var plain = new byte[cipher.Length];
            try
            {
                using var aes = new AesGcm(key, TagLength);
                aes.Decrypt(nonce, cipher, tag, plain, Associated(envelope));
            }
            catch (CryptographicException ex)
            {
                throw new ReportSealException("The sealed report was changed or cut off: its check does not hold.", ex);
            }

            return Encoding.UTF8.GetString(plain);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>What the check covers beside the body: every member in the clear that says something about the report, one per line.</summary>
    private static byte[] Associated(ReportEnvelope envelope) =>
        Encoding.UTF8.GetBytes($"{envelope.Format}\n{envelope.Reporter:D}\n{envelope.Version}\n{envelope.Kind}\n{envelope.Key}\n{envelope.Method}");

    private static string? Member(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
