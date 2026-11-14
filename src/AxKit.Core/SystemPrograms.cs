namespace AxKit.Core;

/// <summary>
/// The programs the apps start, found so that a file planted where axKit was started can never stand in for them
/// (docs/axKit/security-audit-2026-10-24.md S-1). Windows starts a bare name ("git", "cmd.exe") from the current folder
/// before System32 and the PATH, and an app started from Explorer or a console keeps the folder it was started in: a
/// downloaded repository, a folder of downloads. So Windows' own programs are named by their full path, and any other
/// is looked for on the PATH by axKit itself, in folders that are full paths only (a relative entry, "." included,
/// means the current folder). Every app starts its programs through here or by a full path it found itself.
/// </summary>
/// <summary>
/// How a program is started (<see cref="SystemPrograms.LineFor"/>): the file Windows runs (the program, or cmd.exe
/// for a batch file) and its arguments as one text; or, when <see cref="Refusal"/> is not null, the sentence that
/// says why it must not be started, the other two empty.
/// </summary>
public sealed record ProgramLine(string FileName, string Arguments, string? Refusal)
{
    /// <summary>The whole command line, for CreateProcess (the pseudo console).</summary>
    public string CommandLine => Arguments.Length > 0 ? $"\"{FileName}\" {Arguments}" : $"\"{FileName}\"";
}

public static class SystemPrograms
{
    /// <summary>The Windows folder, <c>C:\Windows</c>.</summary>
    public static string Windows => Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    public static string Explorer => Path.Combine(Windows, "explorer.exe");

    public static string Cmd => Path.Combine(Environment.SystemDirectory, "cmd.exe");

    /// <summary>Where Windows puts the aliases of packaged apps (<c>winget.exe</c>, the Store copy's <c>axKit.exe</c>), under the user's local application data.</summary>
    public static string WindowsApps => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps");

    /// <summary>Windows PowerShell 5.1, the one every Windows has.</summary>
    public static string PowerShell => Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");

    /// <summary>Windows' switch that keeps the current folder out of a program search by bare name: cmd.exe and Windows' own search read it.</summary>
    public const string NoCurrentFolderSearch = "NoDefaultCurrentDirectoryInExePath";

    /// <summary>
    /// Sets <see cref="NoCurrentFolderSearch"/> for this process, and so for every process it starts, which inherit
    /// the environment (S-1, the user's choice of 2026-11-09): in axTerminal's Command Prompt a program in the current
    /// folder is run as <c>.\name</c>, as PowerShell has always asked, and a <c>git.exe</c> planted in a downloaded
    /// repository never answers to <c>git</c>. The executable's <c>Main</c> calls it first.
    /// </summary>
    public static void KeepCurrentFolderOffSearches() => Environment.SetEnvironmentVariable(NoCurrentFolderSearch, "1");

    /// <summary>
    /// The characters cmd.exe acts on in an argument, quoted or not (the BatBadBut class of bug; S-3, from axFile's
    /// UserPrograms on its second use, 2026-10-25). The strict list, for a batch file of the user's own whose inside
    /// nobody knows (axFile's custom programs, axMCP's servers), where the arguments go as a list that is quoted only
    /// around spaces: such a batch file is refused every one of them. Claude Code's <c>claude.cmd</c> goes through
    /// <see cref="LineFor"/>, which quotes what quoting makes safe and refuses the rest.
    /// </summary>
    public static readonly char[] CmdSpecials = ['&', '|', '<', '>', '^', '%', '!', '(', ')', ',', ';', '=', '"'];

    /// <summary>Whether a text holds one of cmd.exe's special characters, so that it must not reach a batch program's command line.</summary>
    public static bool HasCmdSpecials(string text) => text.IndexOfAny(CmdSpecials) >= 0;

    /// <summary>Whether the program is a batch file (<c>.cmd</c>, <c>.bat</c>), which Windows runs through cmd.exe.</summary>
    public static bool IsBatch(string program) =>
        program.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || program.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The characters no quoting makes safe on cmd.exe's command line (docs/axKit/repairs.json R-24): <c>%</c> starts
    /// a variable inside quotes too; <c>!</c> does where delayed expansion is on (a registry setting, or the batch
    /// file's own); a double quote ends the quoted part as cmd reads it, whatever backslash stands before it, so that
    /// everything after it is read bare; a line break ends the command.
    /// </summary>
    public static readonly char[] CmdNeverSafe = ['%', '!', '"', '\r', '\n', '\0'];

    /// <summary>
    /// The characters cmd.exe acts on outside quotes and leaves alone inside them: the operators and the caret. A
    /// batch file's own path is refused them as well as <see cref="CmdNeverSafe"/>, because batch files read their own
    /// folder bare (npm's wrapper: <c>SET dp0=%~dp0</c>, where <c>R&amp;D</c> would run <c>D</c>).
    /// </summary>
    public static readonly char[] CmdOperators = ['&', '|', '<', '>', '^'];

    /// <summary>How a refusal names a character: a screen reader may say nothing for the sign alone.</summary>
    private static string CharacterName(char c) => c switch
    {
        '%' => "a percent sign (%)",
        '!' => "an exclamation mark (!)",
        '"' => "a double quote (\")",
        '&' => "an ampersand (&)",
        '|' => "a vertical bar (|)",
        '<' => "a less-than sign (<)",
        '>' => "a greater-than sign (>)",
        '^' => "a caret (^)",
        _ => "a line break",
    };

    /// <summary>
    /// Why a batch program cannot be started safely with these arguments, in a sentence that names the character and
    /// the path or argument that holds it; null when it can, and always null for a program that is no batch file.
    /// </summary>
    public static string? BatchRefusal(string program, IEnumerable<string> arguments)
    {
        if (!IsBatch(program))
        {
            return null;
        }

        var name = Path.GetFileName(program);
        var at = program.IndexOfAny([.. CmdNeverSafe, .. CmdOperators]);
        if (at >= 0)
        {
            return $"{name} is a batch program, which Windows runs through cmd.exe, and its own path, {program}, holds {CharacterName(program[at])}, which cmd.exe would act on: it is not started.";
        }

        foreach (var argument in arguments)
        {
            at = argument.IndexOfAny(CmdNeverSafe);
            if (at >= 0)
            {
                return $"{name} is a batch program, which Windows runs through cmd.exe, and \"{argument.ReplaceLineEndings(" ")}\" holds {CharacterName(argument[at])}, which cmd.exe would act on even inside quotes: it is not started with it.";
            }
        }

        return null;
    }

    /// <summary>
    /// The one place a command line is made for a program that may be a batch file (R-24; Claude Code's session, its
    /// version, AI help and the AI tools' registration): a program as it is, each argument quoted where the C runtime
    /// needs it; a batch file through cmd.exe by its full path as <c>/d /s /c ""path" "arg" ..."</c>, every argument
    /// in quotes, inside which <c>&amp; | &lt; &gt; ^ ( ) , ; =</c> and spaces are plain text to cmd.exe, or the
    /// <see cref="ProgramLine.Refusal"/> of <see cref="BatchRefusal"/>, with nothing to run.
    /// </summary>
    public static ProgramLine LineFor(string program, IEnumerable<string> arguments)
    {
        var list = arguments.ToList();
        if (!IsBatch(program))
        {
            return new ProgramLine(program, string.Join(' ', list.Select(QuoteArgument)), null);
        }

        if (BatchRefusal(program, list) is { } refusal)
        {
            return new ProgramLine(string.Empty, string.Empty, refusal);
        }

        // /s: cmd takes the first and the last quote off and reads the rest as typed, so the path and every argument
        // keep their own quotes. No argument holds a quote here, so QuoteAlways adds only the pair and doubles the
        // backslashes at the end, for the program the batch file hands the arguments to.
        var inner = string.Concat(list.Select(argument => " " + QuoteAlways(argument)));
        return new ProgramLine(Cmd, $"/d /s /c \"\"{program}\"{inner}\"", null);
    }

    /// <summary>
    /// The argument as the child's CommandLineToArgvW reads it back (from axClaude's ClaudeLauncher on its second
    /// use, 2026-11-14): quoted when it is empty or has a space, a tab or a quote; inside, backslashes are literal
    /// except in front of a quote, the closing one included, where the run is doubled, and a quote gets one more
    /// backslash. An argument ending in a backslash used to swallow the closing quote and every argument after it.
    /// </summary>
    public static string QuoteArgument(string argument) =>
        argument.Length > 0 && argument.IndexOfAny([' ', '\t', '"']) < 0 ? argument : QuoteAlways(argument);

    private static string QuoteAlways(string argument)
    {
        var quoted = new System.Text.StringBuilder("\"");
        var backslashes = 0;
        foreach (var c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }

            quoted.Append('\\', c == '"' ? backslashes * 2 + 1 : backslashes).Append(c);
            backslashes = 0;
        }

        return quoted.Append('\\', backslashes * 2).Append('"').ToString();
    }

    /// <summary>The first <paramref name="fileName"/> in the PATH's folders that are full paths; null when there is none.</summary>
    public static string? OnPath(string fileName) => OnPath(fileName, Environment.GetEnvironmentVariable("PATH"));

    /// <summary>The same over a given PATH, for the tests.</summary>
    public static string? OnPath(string fileName, string? path)
    {
        foreach (var folder in PathFolders(path))
        {
            var candidate = Path.Combine(folder, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>The PATH's folders that are full paths, in order: where a program may be looked for.</summary>
    public static IReadOnlyList<string> PathFolders() => PathFolders(Environment.GetEnvironmentVariable("PATH"));

    /// <summary>The same over a given PATH: its entries, quotes off, the ones that are full paths (an entry with characters a path cannot hold is skipped).</summary>
    public static IReadOnlyList<string> PathFolders(string? path) =>
        (path ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Trim('"'))
            .Where(folder => { try { return Path.IsPathFullyQualified(folder); } catch (ArgumentException) { return false; } })
            .ToList();
}
