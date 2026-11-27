// ReportKey: the maintainer's key for the sealed reports (docs/axKit/reports.md RP-6.1a, docs/axKit/reports-setup.md
// section 5). A report is encrypted on the user's computer with the key's public half, which is a constant in
// AxKit.Core/Reports/ReportSeal; only the private half opens it. The private half lives on the maintainer's computer
// in %LOCALAPPDATA%\AxKit-release\reports-key.bin, protected for the Windows account that made it (DPAPI,
// AxKit.Core's UserSecret), so that a copy of the file is useless elsewhere. This tool never prints it.
//
// A LOST KEY MEANS THAT EVERY SEALED REPORT IS UNREADABLE, for good: the ones already on GitHub and the ones every
// released copy goes on sending. Its backup (export) lies beside its passphrase in the private repository
// KyleKeane/AxKit-keys, cloned as ..\axkit-keys (reports-setup.md section 5).
//
// One command per run:
//
//   ReportKey new              once: makes the key (RSA, 3072 bits), stores the private half, prints the public half
//                              and where to paste it. Refuses when a key exists: the released copies seal for it.
//   ReportKey public           the public half again with its fingerprint (the envelope's "key"), and whether it is
//                              the one built into this build of ReportSeal
//   ReportKey seal <file>      a text sealed as a copy of axKit seals a report, with the public half built into
//                              ReportSeal and an all-zero reporter id, for trying "open"; the file, or standard input
//   ReportKey open <file>      a sealed report's envelope, or a whole issue body that holds one, read from the file,
//                              or from standard input when the file is "-" or left out: the envelope's members in the
//                              clear, one a line, then the plain report
//   ReportKey issues           the open reports of KyleKeane/AxKit-reports, asked of the gh program: number, labels,
//                              state, title, the envelope's members in the clear and the plain report; an issue
//                              without an envelope (an earlier version's) is printed as it is and marked "not sealed"
//   ReportKey export <file>    a backup of the private half, encrypted with a passphrase read from standard input
//                              (twelve characters at least; PBKDF2-SHA256, 600000 rounds, then AES-256-GCM). Refuses
//                              to overwrite a file.
//   ReportKey import <file>    the private half back from such a backup, the passphrase read from standard input.
//                              Refuses when a key exists.
//
// AXKIT_REPORTS_KEY names another key file, for trying the tool without touching the real key.
// AXKIT_REPORTS_REPOSITORY names another repository for "issues".
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AxKit.Core;
using AxKit.Core.Reports;

namespace ReportKey;

internal static class Program
{
    private const string BackupFormat = "axkit-reports-key-backup 1";
    private const int Rounds = 600_000;
    private const int MinimumPassphrase = 12;

    private static readonly string KeyFile = Environment.GetEnvironmentVariable("AXKIT_REPORTS_KEY") is { Length: > 0 } named
        ? named
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AxKit-release", "reports-key.bin");

    private static readonly string Repository = Environment.GetEnvironmentVariable("AXKIT_REPORTS_REPOSITORY") is { Length: > 0 } other
        ? other
        : ReportSender.Repository;

    private static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        if (args.Length == 0)
        {
            Console.Error.WriteLine("ReportKey new | public | seal <file> | open <file> | issues | export <file> | import <file>; the commands are described at the top of Program.cs.");
            Console.Error.WriteLine("A lost key means that every sealed report is unreadable: keep a backup made with export.");
            return 2;
        }

        try
        {
            switch (args[0])
            {
                case "new":
                    return New();
                case "public":
                {
                    var privateKey = PrivateKey();
                    try
                    {
                        var publicHalf = PublicHalf(privateKey);
                        Console.WriteLine(publicHalf);
                        Console.Error.WriteLine("Fingerprint: " + ReportSeal.Fingerprint(publicHalf));
                        Console.Error.WriteLine(publicHalf == ReportSeal.PublicKey
                            ? "It is the key this build of axKit seals reports for (ReportSeal.PublicKey)."
                            : "It is NOT the key this build of axKit seals reports for (ReportSeal.PublicKey): reports sealed by this build do not open with it.");
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(privateKey);
                    }

                    return 0;
                }

                case "seal":
                    Console.Write(ReportSeal.Seal(ReadText(args.Length > 1 ? args[1] : "-"), Guid.Empty, BundleInfo.Version, "feedback", ReportSeal.PublicKey));
                    return 0;

                case "open":
                {
                    var sealedText = ReadText(args.Length > 1 ? args[1] : "-");
                    var privateKey = PrivateKey();
                    try
                    {
                        var plain = ReportSeal.Open(sealedText, privateKey);
                        Console.WriteLine(Metadata(ReportSeal.Read(sealedText)!));
                        Console.WriteLine();
                        Console.Write(plain);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(privateKey);
                    }

                    return 0;
                }

                case "issues":
                    return Issues();
                case "export":
                    return Export(Argument(args, 1));
                case "import":
                    return Import(Argument(args, 1));
                default:
                    Console.Error.WriteLine($"Unknown command: {args[0]}");
                    return 2;
            }
        }
        catch (Exception ex) when (ex is ReportSealException or InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException or FormatException or System.ComponentModel.Win32Exception)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int New()
    {
        RefuseAnExistingKey();
        using var rsa = RSA.Create(ReportSeal.KeyBits);
        var privateKey = rsa.ExportPkcs8PrivateKey();
        try
        {
            Store(privateKey);
            Console.WriteLine($"Made the reports key: {KeyFile} (readable only by this Windows account).");
            Console.WriteLine("Back it up now: ReportKey export <file>. A lost key means that every sealed report is unreadable.");
            Console.WriteLine(@"Its public half, for PublicKey in src\AxKit.Core\Reports\ReportSeal.cs:");
            Console.WriteLine(PublicHalf(privateKey));
            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    private static int Issues()
    {
        var gh = SystemPrograms.OnPath("gh.exe") ?? throw new InvalidOperationException("The gh program (GitHub CLI) is not on the PATH.");
        var start = new ProcessStartInfo(gh) { StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        foreach (var argument in new[] { "issue", "list", "--repo", Repository, "--state", "open", "--limit", "500", "--json", "number,state,title,labels,body" })
        {
            start.ArgumentList.Add(argument);
        }

        // Outside call, program, gh: reads the open issues of the reports repository from GitHub, with their bodies,
        // under the maintainer's own sign-in to gh; the sealed bodies are opened on this computer.
        var result = ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
        if (result.Stopped || result.TimedOut || result.ExitCode != 0)
        {
            throw new InvalidOperationException("GitHub could not be asked through gh: " + (result.TimedOut ? "it took too long" : result.Error.Trim()));
        }

        var privateKey = PrivateKey();
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var issues = json.RootElement.EnumerateArray().OrderBy(issue => issue.GetProperty("number").GetInt32()).ToList();
            Console.WriteLine($"{issues.Count} open {(issues.Count == 1 ? "report" : "reports")} in {Repository}.");
            foreach (var issue in issues)
            {
                var body = issue.TryGetProperty("body", out var text) ? text.GetString() ?? string.Empty : string.Empty;
                var labels = issue.TryGetProperty("labels", out var list) && list.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", list.EnumerateArray().Select(label => label.GetProperty("name").GetString()))
                    : string.Empty;
                Console.WriteLine();
                Console.WriteLine($"===== Report {issue.GetProperty("number").GetInt32()}, {(labels.Length == 0 ? "no label" : labels)}, {issue.GetProperty("state").GetString()}: {issue.GetProperty("title").GetString()}");
                if (ReportSeal.Read(body) is not { } envelope)
                {
                    Console.WriteLine("(not sealed: the issue's body as it is on GitHub)");
                    Console.WriteLine(body.ReplaceLineEndings("\n").TrimEnd());
                    continue;
                }

                Console.WriteLine(Metadata(envelope));
                Console.WriteLine();
                try
                {
                    Console.WriteLine(ReportSeal.Open(body, privateKey).TrimEnd());
                }
                catch (ReportSealException ex)
                {
                    Console.WriteLine("(sealed, and not opened: " + ex.Message + ")");
                }
            }

            return 0;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("gh answered with something that is not the issues' JSON: " + ex.Message);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    private static int Export(string file)
    {
        if (File.Exists(file) || Directory.Exists(file))
        {
            throw new InvalidOperationException($"{file} exists already; a backup never replaces a file.");
        }

        var privateKey = PrivateKey();
        try
        {
            var passphrase = ReadPassphrase("Passphrase for the backup, twelve characters at least: ");
            if (passphrase.Length < MinimumPassphrase)
            {
                throw new InvalidOperationException($"The passphrase must have {MinimumPassphrase} characters at least; nothing was written.");
            }

            var salt = RandomNumberGenerator.GetBytes(16);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var tag = new byte[16];
            var cipher = new byte[privateKey.Length];
            var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, Rounds, HashAlgorithmName.SHA256, 32);
            try
            {
                using var aes = new AesGcm(key, tag.Length);
                aes.Encrypt(nonce, privateKey, cipher, tag, Encoding.ASCII.GetBytes(BackupFormat));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }

            File.WriteAllText(file, BackupFormat + "\n" + Convert.ToBase64String([.. salt, .. nonce, .. tag, .. cipher], Base64FormattingOptions.InsertLineBreaks).ReplaceLineEndings("\n") + "\n");
            Console.WriteLine($"Wrote the backup: {Path.GetFullPath(file)}");
            // The maintainer's decision of 2026-11-15: the backups lie in the private repository, not in a password manager.
            Console.WriteLine("It opens with the passphrase alone, on any computer (ReportKey import). It is kept beside its passphrase in the private repository KyleKeane/AxKit-keys, cloned as ..\\axkit-keys: write it there, or move it there, then commit and push that repository (docs/axKit/reports-setup.md section 5).");
            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    private static int Import(string file)
    {
        RefuseAnExistingKey();
        var lines = File.ReadAllText(file).ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2 || lines[0] != BackupFormat)
        {
            throw new InvalidOperationException($"{file} is not a backup made by ReportKey export (its first line is not \"{BackupFormat}\").");
        }

        byte[] all;
        try
        {
            all = Convert.FromBase64String(string.Concat(lines.Skip(1)));
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("The backup was changed or cut off: its lines are not base64.");
        }

        if (all.Length <= 16 + 12 + 16)
        {
            throw new InvalidOperationException("The backup was changed or cut off: it is too short.");
        }

        var passphrase = ReadPassphrase("Passphrase of the backup: ");
        var privateKey = new byte[all.Length - 44];
        var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, all.AsSpan(0, 16), Rounds, HashAlgorithmName.SHA256, 32);
        try
        {
            using (var aes = new AesGcm(key, 16))
            {
                aes.Decrypt(all.AsSpan(16, 12), all.AsSpan(44), all.AsSpan(28, 16), privateKey, Encoding.ASCII.GetBytes(BackupFormat));
            }

            Store(privateKey);
            Console.WriteLine($"Took the reports key from the backup: {KeyFile} (readable only by this Windows account).");
            Console.WriteLine("Its public half, which must be PublicKey in src\\AxKit.Core\\Reports\\ReportSeal.cs:");
            Console.WriteLine(PublicHalf(privateKey));
            return 0;
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("The backup does not open: the passphrase is wrong, or the file was changed.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    private static void RefuseAnExistingKey()
    {
        if (File.Exists(KeyFile))
        {
            throw new InvalidOperationException($"A reports key already exists at {KeyFile}. It is never replaced by this tool: the released copies of axKit seal their reports for it.");
        }
    }

    /// <summary>The private half (PKCS#8) from the key file. The caller clears it.</summary>
    private static byte[] PrivateKey()
    {
        if (!File.Exists(KeyFile))
        {
            throw new InvalidOperationException($"No reports key at {KeyFile}. Make one with: ReportKey new; or bring it back from a backup with: ReportKey import <file>.");
        }

        var text = UserSecret.Unprotect(File.ReadAllText(KeyFile).Trim())
            ?? throw new InvalidOperationException($"The reports key at {KeyFile} does not open for this Windows account: it was made by another account or on another computer. Bring it back from a backup with: ReportKey import <file>.");
        return Convert.FromBase64String(text);
    }

    private static void Store(byte[] privateKey)
    {
        // Checked before it is kept: a key that is no RSA key would only be found out with the first report.
        using (var rsa = RSA.Create())
        {
            try
            {
                rsa.ImportPkcs8PrivateKey(privateKey, out _);
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("That is not an RSA private key; nothing was stored.");
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(KeyFile))!);
        File.WriteAllText(KeyFile, UserSecret.Protect(Convert.ToBase64String(privateKey)));
    }

    /// <summary>The public half as ReportSeal holds it: SubjectPublicKeyInfo in base64.</summary>
    private static string PublicHalf(byte[] privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKey, out _);
        return Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
    }

    /// <summary>What an envelope says in the clear, one member a line, before the plain report.</summary>
    private static string Metadata(ReportEnvelope envelope) =>
        $"format: {envelope.Format}\nreporter: {envelope.Reporter:D}\nversion: {envelope.Version}\nkind: {envelope.Kind}\nkey: {envelope.Key}\nmethod: {envelope.Method}";

    private static string ReadText(string file) => file == "-" ? Console.In.ReadToEnd() : File.ReadAllText(file);

    /// <summary>One line from standard input; typed at a console, it is not shown.</summary>
    private static string ReadPassphrase(string prompt)
    {
        if (Console.IsInputRedirected)
        {
            return Console.In.ReadLine() ?? string.Empty;
        }

        Console.Error.Write(prompt);
        var text = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.Error.WriteLine();
                return text.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (text.Length > 0)
                {
                    text.Length--;
                }
            }
            else if (key.KeyChar != '\0')
            {
                text.Append(key.KeyChar);
            }
        }
    }

    private static string Argument(string[] args, int index) =>
        index < args.Length ? args[index] : throw new ArgumentException($"{args[0]} needs a file: ReportKey {args[0]} <file>.");
}
