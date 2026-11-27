using System.Diagnostics;
using System.Text;
using AxKit.Core;

namespace AxPass.Core;

/// <summary>What one run of the tool gave: its exit code and what it printed.</summary>
public sealed record CliResult(int ExitCode, string Output, string Error)
{
    public bool Succeeded => ExitCode == 0;

    /// <summary>
    /// The tool complained in no words. With <c>-q</c> a wrong master password ends it so, which is how the window
    /// tells a password that does not open the database from a failure the tool explains: no text of the tool's is matched.
    /// </summary>
    public bool SaidNothing => Error.Trim().Length == 0;

    /// <summary>The tool's own words for a failure ("Could not find entry with path …"), or a plain line when it said nothing (a wrong password, with -q).</summary>
    public string Why => Error.Trim() is { Length: > 0 } error ? error.ReplaceLineEndings(" ") : $"KeePassXC's tool ended with code {ExitCode}";
}

/// <summary>
/// KeePassXC's command-line tool (docs/axPass/SPEC.md PW-5, PW-D1; what it does was tried and is in
/// docs/axPass/kdbx-notes.md section 8): one process per command, no window, the database's and the entry's paths as
/// arguments, the master password (and a new entry password, twice) as lines of standard input, never on the command
/// line, in a file or in the environment. Blocking: the window runs it off the UI thread. The tool's commands and
/// options are written in <see cref="KeePassCliTexts"/> and nowhere else (root CLAUDE.md rule 12).
/// </summary>
public static class KeePassCli
{
    public const string ExeName = "keepassxc-cli.exe";

    /// <summary>How long one command may take before it is ended: Argon2 with a large memory setting takes seconds.</summary>
    public const int TimeoutMs = 60_000;

    /// <summary>
    /// The tool the master password is handed to (security audit S-19): KeePassXC's install folder first, which only an
    /// administrator can write; then the path the user set, then the PATH, each only when Windows finds the file
    /// signed by a publisher it trusts, since a folder the user can write could hold a planted program that would
    /// receive the password. Null when there is none.
    /// </summary>
    public static string? Find(string? setPath) => Find(setPath, SignedByKeePassXC);

    /// <summary>
    /// Whether the file is signed, the signature holds, and the signer is KeePassXC's publisher
    /// (<see cref="KeePassCliTexts.Publishers"/>; repairs.md R-55): a program signed by anyone else, however validly,
    /// is not the tool and never gets the master password. A refusal for the signer is logged with the signer's name.
    /// </summary>
    public static bool SignedByKeePassXC(string file)
    {
        if (!Signatures.IsTrusted(file))
        {
            return false;
        }

        var signer = Signatures.SignerName(file);
        if (IsPublisher(signer))
        {
            return true;
        }

        Log.Error($"{ExeName} at {file} is signed by {signer ?? "an unnamed publisher"}, not by KeePassXC's publisher: the master password is not given to it.");
        return false;
    }

    /// <summary>Whether the name is one of KeePassXC's publishers, without regard to case or the spaces around it.</summary>
    public static bool IsPublisher(string? signer) =>
        signer is not null && KeePassCliTexts.Publishers.Contains(signer.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>The same with the signature check given, for the tests.</summary>
    public static string? Find(string? setPath, Func<string, bool> trusted)
    {
        foreach (var folder in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
        {
            var path = Path.Combine(Environment.GetFolderPath(folder), "KeePassXC", ExeName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        if (!string.IsNullOrWhiteSpace(setPath) && File.Exists(setPath) && trusted(setPath))
        {
            return setPath;
        }

        return SystemPrograms.OnPath(ExeName) is { } onPath && trusted(onPath) ? onPath : null;
    }

    /// <summary>
    /// Runs one command: <paramref name="arguments"/> as they are, <paramref name="input"/> the lines of standard input
    /// (the master password first), each ended by a line feed. A run past <see cref="TimeoutMs"/> is ended and reported.
    /// </summary>
    public static CliResult Run(string cli, IReadOnlyList<string> arguments, IReadOnlyList<string> input)
    {
        var info = new ProcessStartInfo(cli)
        {
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            StandardInputEncoding = new UTF8Encoding(false),
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        // The bundle's run (AxKit.Core/ProcessRun, 2026-10-25): the password lines on standard input, each ended by a
        // line feed, the outputs read meanwhile, the tool ended with what it started after the timeout.
        // Outside call, program, KeePassXC: runs the user's installed keepassxc-cli.exe for one command on the
        // database, the master password on its standard input, and reads what it prints; the tool has no network
        // feature.
        return Result(ProcessRun.Run(info, input, TimeSpan.FromMilliseconds(TimeoutMs)));
    }

    /// <summary>
    /// A run as the tool's result (pure, tested): ended at the time limit before it had finished, it failed, with
    /// nothing of what it printed until then (a part of a list is no list); a run the tool finished by itself is its
    /// own exit code and output, whatever held its output open after.
    /// </summary>
    public static CliResult Result(ProcessResult result) =>
        result.Stopped
            ? new CliResult(-1, string.Empty, $"KeePassXC's tool took over {TimeoutMs / 1000} seconds and was ended")
            : new CliResult(result.ExitCode, result.Output, result.Error);

    /// <summary>The arguments of a command on a database: the command, -q (so that the password prompt goes nowhere), the options, the database, the rest.</summary>
    public static IReadOnlyList<string> On(string command, string database, IEnumerable<string>? options = null, params string[] rest) =>
        [command, KeePassCliTexts.Quiet, .. options ?? [], database, .. rest.Select(AsPath)];

    /// <summary>
    /// An entry's or a group's path as the tool reads it: one that starts with "-" (a name in a shared database such
    /// as <c>--key-file=\\server\k</c>) gets the root's "/" in front, so that the tool never reads it as an option
    /// (security audit S-6).
    /// </summary>
    public static string AsPath(string path) => path.StartsWith('-') ? KeePassCliTexts.Root + path : path;
}
