namespace AxAutomate.Core;

/// <summary>One named value of a step (SPEC.md AU-3.3): <c>keys</c> with <c>ctrl+shift+p</c>, <c>text</c> with <c>Command palette</c>.</summary>
public sealed record StepValue(string Key, string Value);

/// <summary>
/// One step of a workflow (SPEC.md AU-3.2): what it does (<see cref="Do"/>, empty for a note alone), its values, what
/// happens when it fails, the sentence it reports then, the steps that run only when it passed or failed, a note for
/// the reader, and the line of the file it came from (0 when the file is JSON).
/// </summary>
public sealed record WorkflowStep(
    string Do,
    IReadOnlyList<StepValue> Values,
    string OnFailure,
    string Report,
    string Note,
    IReadOnlyList<WorkflowStep> Then,
    IReadOnlyList<WorkflowStep> Otherwise,
    int Line)
{
    public const string Stop = "stop";
    public const string Continue = "continue";

    public bool IsNote => Do.Length == 0;

    /// <summary>The value of a key, or an empty text.</summary>
    public string Value(string key) => Values.FirstOrDefault(value => value.Key == key)?.Value ?? string.Empty;

    public static WorkflowStep NoteOnly(string note, int line) => new(string.Empty, [], Stop, string.Empty, note, [], [], line);
}

/// <summary>
/// A section of a script (docs/axKit/user-workflows.md WF-3, SPEC.md AU-5.2b): a goal, what a user gets done by the
/// steps under a <c># Goal: &lt;title&gt;</c> line, or a check (<see cref="Check"/>), steps under a
/// <c># Check: &lt;title&gt;</c> line that only test. It runs from its own line to the line before the next goal or check
/// line, or to the script's end, lines numbered from 1. <see cref="Stable"/>: a <c># Stable</c> line marks it (WF-3.5).
/// <see cref="Named"/> is false for the one goal of a script with neither line, titled by the workflow's label and
/// covering the whole script (WF-3.3).
/// </summary>
public sealed record Goal(string Title, int FirstLine, int LastLine, bool Stable, bool Named, bool Check);

/// <summary>
/// A workflow (SPEC.md AU-3.1): its title, what it covers, its steps in order, the file it was read from, and what in
/// the file was not understood (null when everything was).
/// </summary>
public sealed record Workflow(string Title, string Covers, IReadOnlyList<WorkflowStep> Steps, string Path, string? Problem)
{
    /// <summary>A script's sections, goals and checks, in the script's order (<see cref="ScriptReader.Goals"/>); none for a workflow file.</summary>
    public IReadOnlyList<Goal> Goals { get; init; } = [];

    /// <summary>What the header's <c># Needs:</c> line says the script depends on outside axKit (WF-3.6); empty without one.</summary>
    public string Needs { get; init; } = "";

    /// <summary>The steps that run, nested ones included; notes left out.</summary>
    public int StepCount => Count(Steps);

    private static int Count(IReadOnlyList<WorkflowStep> steps) =>
        steps.Sum(step => (step.IsNote ? 0 : 1) + Count(step.Then) + Count(step.Otherwise));
}
