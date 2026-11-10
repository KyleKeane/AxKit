namespace AxHelp.Core;

/// <summary>
/// The two files axHelp writes into Claude's working folder before the conversation's first start, each only when it
/// is missing, so that the user's own changes stay (SPEC.md HE-2.4): <c>CLAUDE.md</c>, Claude Code's standing
/// instructions for every message there (short spoken replies, how to read a screenshot, when to hand work on), and
/// <c>.claude\agents\agent.md</c>, a Claude Code subagent on the strongest model, so that the fast conversation hands
/// work of several steps to it and answers the rest itself (the user's ask of 2026-11-10: "a simple fast chat option
/// that only spins up a better model if needed for agentic work").
/// </summary>
public static class WorkingFolderFiles
{
    public const string InstructionsName = "CLAUDE.md";

    public static readonly string AgentPath = Path.Combine(".claude", "agents", "agent.md");

    public const string Instructions = """
        # axHelp

        This folder is the conversation of axHelp, part of axKit, for a blind user of Windows who uses the NVDA screen
        reader. Every reply is read aloud.

        - Answer briefly, in plain prose: short paragraphs, no tables, no decoration, keys named as they are pressed
          (Ctrl+Shift+W). Say when you are not sure.
        - A message that starts with "Screenshot of" or "Picture" names a picture file: read it with your Read tool
          first, then answer the user's question about it; with no question, say what the window or the picture is,
          what it shows and the controls a keyboard user can reach.
        - Work on the computer of more than a step or two (changing or organising files, running commands, installing,
          settings) goes to the subagent named agent, which runs on a stronger model; tell the user in a sentence that
          you are handing it on, and report its result briefly. Answer questions and small tasks yourself.

        This file is yours: change it, and Claude reads your version from the next message on.
        """;

    public const string Agent = """
        ---
        name: agent
        description: Does work on the user's Windows computer that takes several steps or real care (changing or organising files, running commands, installing programs, changing settings). Use it for any such task; answer simple questions without it.
        model: opus
        ---

        You do tasks on a Windows computer for a blind user who uses the NVDA screen reader. Work carefully, one step at a
        time, and ask before anything that deletes, overwrites or cannot be undone. When you are done, say in two or
        three plain sentences what you did and where the result is.
        """;

    /// <summary>The two files written into <paramref name="folder"/> where missing; the names of those written.</summary>
    public static IReadOnlyList<string> WriteMissing(string folder)
    {
        var written = new List<string>();
        foreach (var (name, text) in new[] { (InstructionsName, Instructions), (AgentPath, Agent) })
        {
            var path = Path.Combine(folder, name);
            if (File.Exists(path))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text.ReplaceLineEndings("\r\n") + "\r\n");
            written.Add(name);
        }

        return written;
    }
}
