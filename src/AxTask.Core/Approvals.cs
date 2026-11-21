using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace AxTask.Core;

/// <summary>The approval key or a signature could not be used: a file that is none, a wrong passphrase, a key that does not fit.</summary>
public sealed class ApprovalException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// One triage entry of the outside reports list as its approval signs it (docs/axKit/outside-reports.md OR-3, OR-5.3):
/// the decision and the fields the maintainer read. <see cref="Review"/> is the Harm review whole; the approval text
/// holds its hash.
/// </summary>
public sealed record ApprovalEntry(string Decision, string Report, string Title, string Category, string Item, string Priority, string Proposed, string Review, string Date);

/// <summary>The public half of the approval key as the code repository pins it (OR-6.2): the modulus and the exponent, so that Windows PowerShell 5.1 can verify too.</summary>
public sealed record ApprovalPublicKey(byte[] Modulus, byte[] Exponent)
{
    /// <summary>The first sixteen hexadecimal characters of the SHA-256 of the modulus and the exponent: which key it is, said to the maintainer.</summary>
    public string Fingerprint => Convert.ToHexString(SHA256.HashData([.. Modulus, .. Exponent]))[..16].ToLowerInvariant();
}

/// <summary>
/// The maintainer's approval of an outside report (docs/axKit/outside-reports.md sections 5 and 6): an RSA key whose
/// private half is encrypted with a key derived from a passphrase only he knows, and a signature with it over the
/// entry's approval text, which the scripts and axTask verify with the public half pinned in the code repository. Pure:
/// the files are read and written by the callers. The passphrase is never kept; the private key lives in memory only
/// while a signature is made, and is cleared after.
///
/// Signatures are RSA PKCS#1 v1.5 with SHA-256 over the UTF-8 bytes of the text, which .NET Framework's
/// RSACryptoServiceProvider verifies as well (tools/approvals.ps1).
/// </summary>
public static class Approvals
{
    /// <summary>The first line of every approval text.</summary>
    public const string TextFormat = "axkit-approval 1";

    /// <summary>The format of the key file and of the pinned public key.</summary>
    public const string KeyFormat = "axkit-approval-key 1";

    /// <summary>The first line of the text a key change signs (OR-6.3).</summary>
    public const string ChangeFormat = "axkit-approval-key-change 1";

    public const string Approved = "approved";
    public const string Declined = "declined";

    public const int KeyBits = 3072;
    public const int DefaultRounds = 600_000;
    public const int MinimumPassphrase = 12;

    private const int SaltLength = 16;
    private const int NonceLength = 12;
    private const int TagLength = 16;

    /// <summary>
    /// The text an approval signs (OR-5.3): one "name: value" line for each field, in a fixed order, LF line ends; the
    /// long fields with their line ends made LF and their ends trimmed, the review as its SHA-256 in hexadecimal.
    /// </summary>
    public static string Text(ApprovalEntry entry)
    {
        var lines = new StringBuilder();
        lines.Append(TextFormat).Append('\n');
        Line(lines, "decision", entry.Decision);
        Line(lines, "report", entry.Report);
        Line(lines, "title", entry.Title);
        Line(lines, "category", entry.Category);
        Line(lines, "item", entry.Item);
        Line(lines, "priority", entry.Priority);
        Line(lines, "proposed", Long(entry.Proposed));
        Line(lines, "review", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Long(entry.Review)))).ToLowerInvariant());
        Line(lines, "date", entry.Date);
        return lines.ToString();
    }

    /// <summary>
    /// A new approval key (OR-6.1): the key file's JSON, with the private half encrypted under the passphrase, and the
    /// public half. Throws <see cref="ApprovalException"/> for a passphrase shorter than <see cref="MinimumPassphrase"/>.
    /// </summary>
    public static (string KeyFile, ApprovalPublicKey PublicKey) MakeKey(string passphrase, int rounds = DefaultRounds)
    {
        if (passphrase.Length < MinimumPassphrase)
        {
            throw new ApprovalException($"The approval passphrase must have {MinimumPassphrase} characters at least.");
        }

        using var rsa = RSA.Create(KeyBits);
        var parameters = rsa.ExportParameters(false);
        var publicKey = new ApprovalPublicKey(parameters.Modulus!, parameters.Exponent!);
        var privateKey = rsa.ExportPkcs8PrivateKey();
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var tag = new byte[TagLength];
        var cipher = new byte[privateKey.Length];
        var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, rounds, HashAlgorithmName.SHA256, 32);
        try
        {
            using var aes = new AesGcm(key, TagLength);
            aes.Encrypt(nonce, privateKey, cipher, tag, Associated(publicKey));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(privateKey);
        }

        var file = new JsonObject
        {
            ["format"] = KeyFormat,
            ["note"] = "The maintainer's approval key for outside reports (docs/axKit/outside-reports.md section 6). The private half opens only with his passphrase.",
            ["rounds"] = rounds,
            ["salt"] = Convert.ToBase64String(salt),
            ["nonce"] = Convert.ToBase64String(nonce),
            ["tag"] = Convert.ToBase64String(tag),
            ["private"] = Convert.ToBase64String(cipher),
            ["modulus"] = Convert.ToBase64String(publicKey.Modulus),
            ["exponent"] = Convert.ToBase64String(publicKey.Exponent),
        };
        return (file.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }), publicKey);
    }

    /// <summary>The public half of a key file. Throws <see cref="ApprovalException"/> for a file that is none.</summary>
    public static ApprovalPublicKey PublicKeyOf(string keyFile) => ReadKeyFile(keyFile).PublicKey;

    /// <summary>
    /// The signature, base64, of a text with the key file's private half, opened with the passphrase. Throws
    /// <see cref="ApprovalException"/> when the passphrase does not open it ("That is not the approval passphrase").
    /// </summary>
    public static string Sign(string text, string keyFile, string passphrase)
    {
        var read = ReadKeyFile(keyFile);
        var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, read.Salt, read.Rounds, HashAlgorithmName.SHA256, 32);
        var privateKey = new byte[read.Cipher.Length];
        try
        {
            try
            {
                using var aes = new AesGcm(key, TagLength);
                aes.Decrypt(read.Nonce, read.Cipher, read.Tag, privateKey, Associated(read.PublicKey));
            }
            catch (CryptographicException ex)
            {
                throw new ApprovalException("That is not the approval passphrase.", ex);
            }

            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKey, out _);
            var signature = rsa.SignData(Encoding.UTF8.GetBytes(text), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    /// <summary>Whether a signature, base64, is the public key's over the text. Never throws: anything unreadable is no signature.</summary>
    public static bool Verify(string text, string signature, ApprovalPublicKey publicKey)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters { Modulus = publicKey.Modulus, Exponent = publicKey.Exponent });
            return rsa.VerifyData(Encoding.UTF8.GetBytes(text), Convert.FromBase64String(signature.Trim()), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>The text a change of the pinned key signs with the old key (OR-6.3): the new key's fingerprint.</summary>
    public static string ChangeText(ApprovalPublicKey newKey) => $"{ChangeFormat}\nnew: {newKey.Fingerprint}\n";

    /// <summary>
    /// The pinned public key as the code repository holds it, <c>docs/axKit/approval-key.pub</c> (OR-6.2): the format,
    /// the fingerprint, the modulus and the exponent, one "name: value" line each, LF.
    /// </summary>
    public static string PinnedText(ApprovalPublicKey key) =>
        $"{KeyFormat}\nfingerprint: {key.Fingerprint}\nmodulus: {Convert.ToBase64String(key.Modulus)}\nexponent: {Convert.ToBase64String(key.Exponent)}\n";

    /// <summary>The pinned public key read; null for a text that is none (a missing line, a fingerprint that does not fit the key).</summary>
    public static ApprovalPublicKey? ReadPinned(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != KeyFormat)
        {
            return null;
        }

        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon > 0)
            {
                values[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            }
        }

        try
        {
            if (!values.TryGetValue("modulus", out var modulus) || !values.TryGetValue("exponent", out var exponent) || !values.TryGetValue("fingerprint", out var fingerprint))
            {
                return null;
            }

            var key = new ApprovalPublicKey(Convert.FromBase64String(modulus), Convert.FromBase64String(exponent));
            return key.Fingerprint == fingerprint ? key : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private sealed record KeyFile(int Rounds, byte[] Salt, byte[] Nonce, byte[] Tag, byte[] Cipher, ApprovalPublicKey PublicKey);

    private static KeyFile ReadKeyFile(string keyFile)
    {
        try
        {
            if (JsonNode.Parse(keyFile) is not JsonObject json || (string?)json["format"] != KeyFormat)
            {
                throw new ApprovalException("The approval key file is not one of this format.");
            }

            string Member(string name) => (string?)json[name] ?? throw new ApprovalException($"The approval key file has no {name}.");
            var rounds = (int?)json["rounds"] ?? 0;
            if (rounds < 1)
            {
                throw new ApprovalException("The approval key file has no rounds.");
            }

            return new KeyFile(rounds, Convert.FromBase64String(Member("salt")), Convert.FromBase64String(Member("nonce")),
                Convert.FromBase64String(Member("tag")), Convert.FromBase64String(Member("private")),
                new ApprovalPublicKey(Convert.FromBase64String(Member("modulus")), Convert.FromBase64String(Member("exponent"))));
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or FormatException or InvalidOperationException)
        {
            throw new ApprovalException("The approval key file cannot be read.", ex);
        }
    }

    /// <summary>The associated data of the encrypted private half: the format and the public half, so that a private half put beside another public key does not open.</summary>
    private static byte[] Associated(ApprovalPublicKey key) => [.. Encoding.UTF8.GetBytes(KeyFormat), .. key.Modulus, .. key.Exponent];

    private static void Line(StringBuilder lines, string name, string value) =>
        lines.Append(name).Append(": ").Append(value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal).Trim()).Append('\n');

    private static string Long(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
}
