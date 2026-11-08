using System.Text;

namespace AxKit.Core.Reports;

/// <summary>
/// The reporter account's token (docs/axkit/reports.md RP-6.2): never in the source, but embedded into a release
/// build from the file <c>Reports/reporter.token</c>, which <c>publish.ps1</c> writes from the secret
/// <c>AXKIT_REPORTER_TOKEN</c> through <c>tools/reporter-token.ps1</c> and removes again. The file holds the token
/// mixed with a fixed key and written as base64: that keeps it from a glance at the program file and from nobody who
/// looks for it, as the page says. A build without the file has no token, and its sender says so.
/// </summary>
public static class ReporterToken
{
    public const string ResourceName = "AxKit.Core.ReporterToken";

    private static readonly byte[] Key = Encoding.ASCII.GetBytes("axKit reports: a key that hides the token from a glance, not from a reader");

    /// <summary>The token this build carries, or null for a build made without the secret.</summary>
    public static string? Token { get; } = Load();

    /// <summary>The file's text for a token: the same mixing <c>tools/reporter-token.ps1</c> does.</summary>
    public static string Encode(string token) => Convert.ToBase64String(Mix(Encoding.UTF8.GetBytes(token.Trim())));

    /// <summary>The token a file's text holds; null for an empty or unreadable one.</summary>
    public static string? Decode(string encoded)
    {
        try
        {
            var token = Encoding.UTF8.GetString(Mix(Convert.FromBase64String(encoded.Trim()))).Trim();
            return token.Length == 0 ? null : token;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? Load()
    {
        using var stream = typeof(ReporterToken).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return Decode(reader.ReadToEnd());
    }

    private static byte[] Mix(byte[] bytes)
    {
        var mixed = new byte[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
        {
            mixed[i] = (byte)(bytes[i] ^ Key[i % Key.Length]);
        }

        return mixed;
    }
}
