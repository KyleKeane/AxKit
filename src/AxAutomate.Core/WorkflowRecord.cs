using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxAutomate.Core;

/// <summary>One result of a section in one tier: "passed", "failed" or "not run", with the version, the commit and the date (yyyy-MM-dd).</summary>
public sealed record RecordedRun(string Result, string Version, string Commit, string Date);

/// <summary>A section's entry for one tier: its last result and its last pass (null when it never passed there).</summary>
public sealed record RecordedTier(RecordedRun Last, RecordedRun? Passed);

/// <summary>One goal or check of a script: known by its workflow's label and its title (WF-D-17), with the script's path from the repository's root.</summary>
public sealed record RecordedGoal(string Workflow, string Goal, string Script, IReadOnlyDictionary<string, RecordedTier> Tiers);

/// <summary>
/// The workflow record, <c>docs/axKit/workflow-record.json</c> (docs/axKit/user-workflows.md WF-6, format 5.5): for each
/// goal and check and each tier it ran in, the last result and the last pass. Pure: the command line reads the file,
/// enters a run's sections with <see cref="With"/>, drops what no script has any more with <see cref="Pruned"/> and
/// writes it with <see cref="Write"/>. It holds labels, titles, script paths, versions, commits and dates, nothing else (WF-6.4).
/// </summary>
public static class WorkflowRecord
{
    public const string Format = "axkit-workflow-record 1";
    public const string Desktop = "desktop", Nvda = "nvda", AiTools = "ai-tools";

    private const string PassedResult = "passed";

    // The tiers in the order a line writes them.
    private static readonly string[] TierOrder = [Desktop, Nvda, AiTools];

    private static readonly HashSet<string> Results = new(StringComparer.Ordinal) { PassedResult, "failed", "not run" };

    private static readonly JsonSerializerOptions Text = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>The record's entries in the file's order. An unreadable text, or one of another format, gives none; an entry or a tier that is not whole is left out, and a second entry of one label and title too.</summary>
    public static IReadOnlyList<RecordedGoal> Read(string json)
    {
        JsonObject? root;
        try
        {
            root = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return [];
        }
        if (root is null || StringOf(root, "format") != Format || root["goals"] is not JsonArray entries)
        {
            return [];
        }
        var goals = new List<RecordedGoal>();
        foreach (var node in entries)
        {
            if (node is not JsonObject entry)
            {
                continue;
            }
            var workflow = StringOf(entry, "workflow");
            var goal = StringOf(entry, "goal");
            var script = StringOf(entry, "script");
            if (workflow is null || goal is null || script is null || entry["tiers"] is not JsonObject tierNodes)
            {
                continue;
            }
            if (goals.Any(known => known.Workflow == workflow && known.Goal == goal))
            {
                continue;
            }
            var tiers = new Dictionary<string, RecordedTier>(StringComparer.Ordinal);
            foreach (var (name, value) in tierNodes)
            {
                if (!TierOrder.Contains(name) || value is not JsonObject tier)
                {
                    continue;
                }
                var last = RunOf(tier["last"], result: null);
                if (last is null)
                {
                    continue;
                }
                var passed = tier["passed"] is null ? null : RunOf(tier["passed"], PassedResult);
                tiers[name] = new RecordedTier(last, passed);
            }
            goals.Add(new RecordedGoal(workflow, goal, script, tiers));
        }
        return goals;
    }

    /// <summary>The file of 5.5: the entries in the order of their script's path (ordinal, lower case), a script's own in the order given, one a line, ending with a line feed.</summary>
    public static string Write(IReadOnlyList<RecordedGoal> goals)
    {
        var text = new StringBuilder();
        text.Append("{\n  \"format\": ").Append(Quoted(Format)).Append(",\n");
        var sorted = goals.OrderBy(goal => goal.Script.ToLowerInvariant(), StringComparer.Ordinal).ToList();
        if (sorted.Count == 0)
        {
            text.Append("  \"goals\": []\n}\n");
            return text.ToString();
        }
        text.Append("  \"goals\": [\n");
        for (var i = 0; i < sorted.Count; i++)
        {
            text.Append("    ").Append(Line(sorted[i])).Append(i < sorted.Count - 1 ? ",\n" : "\n");
        }
        text.Append("  ]\n}\n");
        return text.ToString();
    }

    /// <summary>
    /// The record with one run entered (WF-6.1): the goal's entry for the tier gets the run as its last, and a run that
    /// passed is its last pass too; another result keeps the pass it had. The entry's script is set to the one given.
    /// A goal not yet in the record is put after the last entry of its script, or at the end.
    /// </summary>
    public static IReadOnlyList<RecordedGoal> With(IReadOnlyList<RecordedGoal> goals, string workflow, string goal, string script, string tier, RecordedRun run)
    {
        var result = goals.ToList();
        var index = result.FindIndex(known => known.Workflow == workflow && known.Goal == goal);
        var tiers = new Dictionary<string, RecordedTier>(StringComparer.Ordinal);
        if (index >= 0)
        {
            foreach (var (name, value) in result[index].Tiers)
            {
                tiers[name] = value;
            }
        }
        tiers.TryGetValue(tier, out var before);
        tiers[tier] = new RecordedTier(run, run.Result == PassedResult ? run : before?.Passed);
        var entry = new RecordedGoal(workflow, goal, script, tiers);
        if (index >= 0)
        {
            result[index] = entry;
        }
        else
        {
            var lastOfScript = result.FindLastIndex(known => known.Script == script);
            result.Insert(lastOfScript >= 0 ? lastOfScript + 1 : result.Count, entry);
        }
        return result;
    }

    /// <summary>
    /// The record without the goals no script has any more (WF-6.2): an entry stays when its label and title are among
    /// the existing ones; it takes their script's path, and the entries are put in the order of the existing ones.
    /// </summary>
    public static IReadOnlyList<RecordedGoal> Pruned(IReadOnlyList<RecordedGoal> goals, IReadOnlyList<(string Workflow, string Goal, string Script)> existing)
    {
        var kept = new List<RecordedGoal>();
        foreach (var (workflow, goal, script) in existing)
        {
            var entry = goals.FirstOrDefault(known => known.Workflow == workflow && known.Goal == goal);
            if (entry is not null && !kept.Any(known => known.Workflow == workflow && known.Goal == goal))
            {
                kept.Add(entry with { Script = script });
            }
        }
        return kept;
    }

    private static string Line(RecordedGoal goal)
    {
        var tiers = string.Join(", ", TierOrder.Where(goal.Tiers.ContainsKey).Select(name =>
        {
            var tier = goal.Tiers[name];
            var passed = tier.Passed is null ? "null" : Pass(tier.Passed);
            return Quoted(name) + ": {\"last\": " + Run(tier.Last) + ", \"passed\": " + passed + "}";
        }));
        return "{\"workflow\": " + Quoted(goal.Workflow) + ", \"goal\": " + Quoted(goal.Goal) + ", \"script\": " + Quoted(goal.Script)
            + ", \"tiers\": {" + tiers + "}}";
    }

    private static string Run(RecordedRun run) =>
        "{\"result\": " + Quoted(run.Result) + ", \"version\": " + Quoted(run.Version) + ", \"commit\": " + Quoted(run.Commit) + ", \"date\": " + Quoted(run.Date) + "}";

    private static string Pass(RecordedRun run) =>
        "{\"version\": " + Quoted(run.Version) + ", \"commit\": " + Quoted(run.Commit) + ", \"date\": " + Quoted(run.Date) + "}";

    private static string Quoted(string value) => JsonSerializer.Serialize(value, Text);

    // A run of the file; result null reads it from the object (a last run), else it is given (a pass, which has none).
    private static RecordedRun? RunOf(JsonNode? node, string? result)
    {
        if (node is not JsonObject run)
        {
            return null;
        }
        var read = result ?? StringOf(run, "result");
        var version = StringOf(run, "version");
        var commit = StringOf(run, "commit");
        var date = StringOf(run, "date");
        if (read is null || !Results.Contains(read) || version is null || commit is null || date is null)
        {
            return null;
        }
        return new RecordedRun(read, version, commit, date);
    }

    private static string? StringOf(JsonObject node, string name) =>
        node[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
