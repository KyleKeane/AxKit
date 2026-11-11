using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxAutomate.Core;

/// <summary>
/// A workflow file, <c>&lt;name&gt;.workflow.json</c> (SPEC.md AU-3): <c>format</c>, <c>title</c>, <c>covers</c> and
/// <c>steps</c>; a step's <c>do</c>, <c>on failure</c>, <c>report</c>, <c>note</c>, <c>then</c> and <c>otherwise</c>,
/// and every other key a value of the operation. What is not understood is said in <see cref="Workflow.Problem"/>
/// (AU-3.1.1), and the rest still read.
/// </summary>
public static class WorkflowFile
{
    public const string Extension = ".workflow.json";
    public const string Format = "axkit-workflow 1";

    private static readonly HashSet<string> StepKeys = new(StringComparer.Ordinal) { "do", "on failure", "report", "note", "then", "otherwise" };

    public static Workflow Read(string path)
    {
        var name = System.IO.Path.GetFileName(path);
        var title = name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) ? name[..^Extension.Length] : name;
        try
        {
            return Parse(File.ReadAllText(path), title, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new Workflow(title, string.Empty, [], path, "the file could not be read");
        }
    }

    public static Workflow Parse(string text, string title, string path)
    {
        JsonObject? root;
        try
        {
            root = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) as JsonObject;
        }
        catch (JsonException ex)
        {
            return new Workflow(title, string.Empty, [], path, $"not JSON: {ex.Message}");
        }

        if (root is null)
        {
            return new Workflow(title, string.Empty, [], path, "not a workflow: the file is not one object");
        }

        var problems = new List<string>();
        if (Text(root["format"]) != Format)
        {
            problems.Add($"the format is not {Format}");
        }

        var steps = root["steps"] is JsonArray array ? Steps(array, problems) : [];
        if (root["steps"] is not JsonArray)
        {
            problems.Add("no list of steps");
        }

        return new Workflow(Text(root["title"]) is { Length: > 0 } own ? own : title, Text(root["covers"]), steps, path, problems.Count == 0 ? null : string.Join("; ", problems));
    }

    private static List<WorkflowStep> Steps(JsonArray array, List<string> problems)
    {
        var steps = new List<WorkflowStep>();
        foreach (var node in array)
        {
            if (node is not JsonObject step)
            {
                problems.Add($"step {steps.Count + 1} is not an object");
                continue;
            }

            var onFailure = Text(step["on failure"]) is { Length: > 0 } given ? given : WorkflowStep.Stop;
            if (onFailure is not (WorkflowStep.Stop or WorkflowStep.Continue))
            {
                problems.Add($"step {steps.Count + 1}: on failure is {onFailure}, not stop or continue");
            }

            var values = step.Where(pair => !StepKeys.Contains(pair.Key)).Select(pair => new StepValue(pair.Key, Text(pair.Value))).ToList();
            steps.Add(new WorkflowStep(
                Text(step["do"]),
                values,
                onFailure,
                Text(step["report"]),
                Text(step["note"]),
                step["then"] is JsonArray then ? Steps(then, problems) : [],
                step["otherwise"] is JsonArray otherwise ? Steps(otherwise, problems) : [],
                0));
        }

        return steps;
    }

    /// <summary>A value as text: a string as it is, anything else as its JSON.</summary>
    private static string Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : node?.ToJsonString() ?? string.Empty;
}
