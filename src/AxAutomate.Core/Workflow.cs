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
/// A workflow (SPEC.md AU-3.1): its title, what it covers, its steps in order, the file it was read from, and what in
/// the file was not understood (null when everything was).
/// </summary>
public sealed record Workflow(string Title, string Covers, IReadOnlyList<WorkflowStep> Steps, string Path, string? Problem)
{
    /// <summary>The steps that run, nested ones included; notes left out.</summary>
    public int StepCount => Count(Steps);

    private static int Count(IReadOnlyList<WorkflowStep> steps) =>
        steps.Sum(step => (step.IsNote ? 0 : 1) + Count(step.Then) + Count(step.Otherwise));
}
