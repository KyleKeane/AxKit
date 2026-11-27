using System.Diagnostics;
using System.Text;

namespace AxKit.Core.Ai;

/// <summary>
/// One question to Claude Code in its print mode (docs/axKit/ai-help.md AH-2): the command line, the folder it runs
/// in, and the run itself with the prompt on standard input, never on the command line (it is long and holds the
/// user's words). One process per question; a follow-up resumes the session (AH-6.3).
/// </summary>
public static class AiRequest
{
    /// <summary>AH-2.4: a question that takes longer is ended and said.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);

    /// <summary>The three tools that read (AH-2.2); print mode refuses every other by itself.</summary>
    public const string Tools = "Read,Glob,Grep";

    /// <summary>
    /// The arguments: print mode with a JSON answer, the read tools, no server of the working folder's
    /// <c>.mcp.json</c> and no hook of its settings files (AH-2.6: print mode shows no trust question and would obey
    /// both; the words are <see cref="ClaudeCodeProgram"/>'s, <paramref name="noHooksFile"/> the file of
    /// <see cref="ClaudeCodeProgram.NoHooksSettings"/>), every allowed folder as <c>--add-dir</c> (but the working
    /// one, which Claude Code has already), then the model and the session to resume when given.
    /// </summary>
    public static IReadOnlyList<string> Arguments(IReadOnlyList<string> folders, string workingDirectory, string? model, string? resume, string noHooksFile)
    {
        var arguments = new List<string>
        {
            "-p", "--output-format", "json", "--allowedTools", Tools,
            ClaudeCodeProgram.StrictMcpConfigOption, ClaudeCodeProgram.SettingsOption, noHooksFile,
        };
        foreach (var folder in folders.Where(folder => !string.Equals(folder, workingDirectory, StringComparison.OrdinalIgnoreCase)))
        {
            arguments.Add("--add-dir");
            arguments.Add(folder);
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            arguments.Add("--model");
            arguments.Add(model);
        }

        if (!string.IsNullOrWhiteSpace(resume))
        {
            arguments.Add("--resume");
            arguments.Add(resume);
        }

        return arguments;
    }

    /// <summary>
    /// How Claude Code is started for a question (AH-2.1): <see cref="Arguments"/> on the bundle's one command line
    /// for Claude Code (<see cref="ClaudeCodeProgram.LineFor"/>), which runs npm's <c>claude.cmd</c> through cmd.exe
    /// with every argument quoted, or refuses a folder or a model cmd.exe would act on (a percent sign, an
    /// exclamation mark, a double quote), with the reason.
    /// </summary>
    public static ProgramLine Line(string claude, string workingFolder, IReadOnlyList<string> folders, string? model, string? resume, string noHooksFile) =>
        ClaudeCodeProgram.LineFor(claude, Arguments(folders, workingFolder, model, resume, noHooksFile));

    /// <summary>The process of a <see cref="Line"/> that was not refused, started in <paramref name="workingFolder"/> (AH-2.1, <see cref="AiSettings.EnsureWorkingFolder"/>).</summary>
    public static ProcessStartInfo StartInfo(ProgramLine line, string workingFolder) =>
        new(line.FileName, line.Arguments)
        {
            WorkingDirectory = workingFolder,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

    /// <summary>
    /// Claude Code run to its end with the prompt on its standard input; the answer, or an error answer after the time
    /// limit, or, without a run, when the command line was refused (<see cref="Line"/>). Throws as
    /// <see cref="Process.Start()"/> does when the program cannot be started; a cancelled wait ends the process and
    /// throws <see cref="OperationCanceledException"/>. The file that turns the hooks off (AH-2.6,
    /// <paramref name="noHooksFile"/>, the window's <see cref="AppPaths.ClaudeNoHooksFile"/>) is seen to first; when it cannot be written Claude Code is not run.
    /// </summary>
    public static async Task<AiAnswer> AskAsync(string claude, string prompt, string workingFolder, IReadOnlyList<string> folders, string? model, string? resume, string noHooksFile, CancellationToken cancellation)
    {
        try
        {
            ClaudeCodeProgram.WriteNoHooksSettings(noHooksFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new AiAnswer("Claude Code was not run. The file that turns off the hooks of Claude's working folder for a question could not be written: " + ex.Message, true, null);
        }

        var line = Line(claude, workingFolder, folders, model, resume, noHooksFile);
        if (line.Refusal is { } refusal)
        {
            return new AiAnswer("Claude Code was not run. " + refusal + " The folders Claude may read are in AI help's settings.", true, null);
        }

        var start = StartInfo(line, workingFolder);
        // Outside call, program, Claude Code: claude -p in its working folder with the folders the user allowed, with read-only tools, without the folder's servers and hooks, the prompt on its standard input (docs/axKit/ai-help.md AH-2.1); Claude Code talks to Anthropic under the user's account.
        var result = await ProcessRun.RunAsync(start, [prompt], Timeout, cancellation).ConfigureAwait(false);
        // Ended at the limit, what it printed until then is a part and no answer; an answer Claude Code finished while
        // a program it started held its output open is the answer.
        return result.Stopped
            ? new AiAnswer("No answer in two minutes: Claude Code was ended. Ask again, or a shorter question.", true, null)
            : AiAnswer.Parse(result.Output, result.Error, result.ExitCode);
    }
}
