using AxAutomate.Core;
using AxKit.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The repository's own workflow files, read without a window (docs/axKit/user-workflows.md WF-2.11, WF-11.1): the
/// command inventory, <c>docs/axKit/window-commands.json</c>, is well formed and written as its writer writes it, holds
/// nothing that never enters it, and names every command a script's <c>do</c> step runs. That it is current is the
/// window runs' check (WF-2.10), not this one. And the kept traces, <c>tests/traces</c> (WF-5.9): every
/// <c># Stable</c> section has its kept section, no other trace is kept, and no script that may keep none is marked.
/// That a kept trace is what a window says today is the window runs' check too (WF-5.5). And the workflow
/// record, <c>docs/axKit/workflow-record.json</c> (WF-6): well formed, written as its writer writes it, no account path.
/// </summary>
public sealed class RepositoryWorkflowTests
{
    [Fact]
    public void The_inventory_is_well_formed_and_written_as_the_writer_writes_it()
    {
        var text = Lf(File.ReadAllText(InventoryPath()));
        var apps = CommandInventory.Read(text);

        // The format line, and apps to read: an unreadable file reads as none.
        Assert.StartsWith("{\n  \"format\": \"" + CommandInventory.Format + "\",\n", text, StringComparison.Ordinal);
        Assert.NotEmpty(apps);

        // One order, one row a line: the file is exactly what the writer gives for what it holds, line ends aside.
        Assert.Equal(text, CommandInventory.Write(apps));
    }

    [Fact]
    public void No_script_runs_a_command_by_name_inside_a_key_test()
    {
        // AX-8.12, R-60: in the key test a key is named and nothing is run, so a press there is a keys step. A do
        // step would wait at the window, and through the AI tools it would ask for the command itself.
        var inside = new List<string>();
        foreach (var script in RepositoryScripts())
        {
            var testing = false;
            var number = 0;
            foreach (var line in File.ReadLines(script))
            {
                number++;
                var step = line.Trim();
                while (step.StartsWith("iffound ", StringComparison.Ordinal))
                {
                    step = step[8..].TrimStart();
                }

                if (step == "menu Test a key" || step == "do Test a key")
                {
                    testing = true;
                }
                else if (testing && step.StartsWith("keys ", StringComparison.Ordinal) && step[5..].Split(' ').Contains("escape"))
                {
                    testing = false;
                }
                else if (testing && step.StartsWith("do ", StringComparison.Ordinal))
                {
                    inside.Add($"{Relative(script)} line {number}: {step}");
                }
            }
        }

        Assert.Empty(inside);
    }

    [Fact]
    public void The_record_is_well_formed()
    {
        // WF-6, format 5.5. The file is made by the first recorded run; an entry no script has any more fails
        // nothing here: the next recorded run drops it (WF-6.2).
        var path = Path.Combine(Root(), "docs", "axKit", "workflow-record.json");
        if (!File.Exists(path))
        {
            return;
        }

        var text = Lf(File.ReadAllText(path));
        var goals = WorkflowRecord.Read(text);
        Assert.StartsWith("{\n  \"format\": \"" + WorkflowRecord.Format + "\",\n", text, StringComparison.Ordinal);
        Assert.Equal(text, WorkflowRecord.Write(goals));

        // Labels, titles, script paths, versions, commits and dates, nothing else (WF-6.4): no account path, no
        // backslash, and every script a path of the repository with forward slashes.
        Assert.Empty(Redaction.Find(text));
        Assert.DoesNotContain('\\', text);
        Assert.All(goals, goal => Assert.StartsWith("tests/", goal.Script, StringComparison.Ordinal));
        var dates = goals.SelectMany(goal => goal.Tiers.Values)
            .SelectMany(tier => tier.Passed is null ? new[] { tier.Last.Date } : new[] { tier.Last.Date, tier.Passed.Date });
        Assert.All(dates, date => Assert.True(
            DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _),
            date));
    }

    [Fact]
    public void The_inventory_holds_no_numbered_entry_and_no_account_path()
    {
        var text = File.ReadAllText(InventoryPath());
        Assert.Empty(Redaction.Find(text));

        // Every row is one the inventory keeps (WF-2.8): no numbered entry of a list, and none that a named rule of
        // CommandInventory.Kept leaves out as the name of something of the computer the run was made on.
        var left = CommandInventory.Read(text)
            .SelectMany(app => app.Commands.Where(row => !CommandInventory.Kept(row, string.Empty, string.Empty))
                .Select(row => $"{app.App}: {CommandNames.Line(new CommandName(row.Label, row.Menu))}"))
            .ToList();
        Assert.Empty(left);
    }

    [Fact]
    public void Every_do_step_of_every_script_names_a_command_of_the_inventory()
    {
        // WF-2.11: under the app of the window the step reaches (ScriptApps.ByLine); where that cannot be read, in at
        // least one app, each app's rows by themselves. A step whose app the inventory holds no row for is passed over,
        // as the live check passes that app over.
        var apps = CommandInventory.Read(File.ReadAllText(InventoryPath()));
        var unknown = new List<string>();
        var steps = 0;
        foreach (var path in RepositoryScripts())
        {
            var lines = File.ReadAllLines(path);
            var reached = ScriptApps.ByLine(lines);
            for (var i = 0; i < lines.Length; i++)
            {
                if (DoName(lines[i]) is not { } name)
                {
                    continue;
                }

                var app = reached[i];
                if (app is not null && !CommandInventory.Holds(apps, app))
                {
                    continue;
                }

                steps++;
                var resolution = CommandInventory.Resolve(apps, app, name, out _);
                if (resolution.Match != CommandMatch.Found)
                {
                    var why = app is null
                        ? "no app's commands name it once (the window's app cannot be read off the script)"
                        : CommandNames.Refusal(resolution, [.. CommandInventory.Of(apps, app).Select(row => new CommandName(row.Label, row.Menu))]);
                    unknown.Add($"{Relative(path)}, line {i + 1}: do {name} under {app ?? "an app not known"}: {why}");
                }
            }
        }

        Assert.Empty(unknown);
        Assert.True(steps > 0, "No do step of the repository's scripts was checked: the inventory holds none of their apps.");
    }

    [Fact]
    public void A_do_step_is_read_after_iffound_and_never_from_a_comment()
    {
        Assert.Equal("Find", DoName("do Find"));
        Assert.Equal("Heading, Navigation, Next", DoName("  iffound do Heading, Navigation, Next "));
        Assert.Null(DoName("# do Find"));
        Assert.Null(DoName("done"));
        Assert.Null(DoName("expect said do Find"));
    }

    [Fact]
    public void Every_stable_section_has_its_kept_trace_and_no_other_trace_is_kept()
    {
        // WF-5.9, without a window. The files a script's stable sections give, by the kept file's path.
        var problems = new List<string>();
        var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in RepositoryScripts())
        {
            var script = Relative(path);
            var lines = File.ReadAllLines(path);
            var workflow = ScriptReader.Parse(lines, Path.GetFileNameWithoutExtension(path), path);
            if (!Traces.MayKeep(script, workflow.Needs))
            {
                // WF-5.6: a window that reads the computer, the user's session or a model: no mark at all, not even
                // one that marks nothing.
                if (lines.Any(IsStableLine))
                {
                    problems.Add($"{script} holds a # Stable line and may keep no trace (WF-5.6): take the line away");
                }

                continue;
            }

            var stable = workflow.Goals.Where(goal => goal.Stable).Select(Traces.Heading).ToList();
            if (stable.Count == 0)
            {
                continue;
            }

            var kept = Traces.PathFor(script);
            expected.Add(kept);
            var file = Path.Combine(Root(), kept.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(file))
            {
                problems.Add($"{script} has {stable.Count} stable section(s) and no kept trace {kept}: run it with --traces write");
                continue;
            }

            var text = Lf(File.ReadAllText(file));
            if (!text.StartsWith("# Trace of " + script + "\n", StringComparison.Ordinal))
            {
                problems.Add($"{kept} does not begin with the line \"# Trace of {script}\": it is written by --traces write, never by hand");
            }

            var sections = Traces.Read(text).Select(section => section.Heading).ToList();
            problems.AddRange(stable.Except(sections).Select(heading => $"{script}: the stable section \"{heading}\" has no kept section in {kept}: run it with --traces write"));
            problems.AddRange(sections.Except(stable).Select(heading => $"{kept}: the kept section \"{heading}\" belongs to no # Stable section of {script}: run it with --traces write"));
            if (sections.Count != sections.Distinct().Count())
            {
                problems.Add($"{kept} holds a section twice");
            }
        }

        // No other file: every file of tests/traces is the kept trace of a script that exists and has a stable section.
        var folder = Path.Combine(Root(), "tests", "traces");
        if (Directory.Exists(folder))
        {
            problems.AddRange(Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
                .Select(Relative)
                .Where(file => !expected.Contains(file))
                .Select(file => $"{file} is the kept trace of no script with a # Stable section: delete it, or run the script with --traces write"));
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void A_stable_line_is_a_comment_whose_whole_text_is_the_word()
    {
        Assert.True(IsStableLine("# Stable"));
        Assert.True(IsStableLine("  #stable "));
        Assert.False(IsStableLine("# Stable goals come later"));
        Assert.False(IsStableLine("type # Stable"));
        Assert.False(IsStableLine("Stable"));
    }

    /// <summary>Whether a script's line is a <c># Stable</c> line (WF-3.5), wherever it stands: a comment whose whole text is the word, in any case.</summary>
    private static bool IsStableLine(string line)
    {
        var text = line.Trim();
        return text.StartsWith('#') && string.Equals(text.TrimStart('#').Trim(), ScriptReader.StableWord, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The name a <c>do</c> step runs, after any <c>iffound</c>, as the runner reads it; null for any other line.</summary>
    private static string? DoName(string line)
    {
        var text = line.Trim();
        while (text.StartsWith("iffound ", StringComparison.Ordinal))
        {
            text = text[8..].TrimStart();
        }

        return text.StartsWith("do ", StringComparison.Ordinal) ? text[3..].Trim() : null;
    }

    private static string Root()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "AxKit.sln")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("No repository above the tests.");
        }

        return root;
    }

    private static string InventoryPath() => Path.Combine(Root(), "docs", "axKit", "window-commands.json");

    private static string Relative(string path) => Path.GetRelativePath(Root(), path).Replace('\\', '/');

    /// <summary>Every script of tests/scripts and tests/nvda-scripts.</summary>
    private static List<string> RepositoryScripts()
    {
        var root = Root();
        var scripts = Directory.GetFiles(Path.Combine(root, "tests", "scripts"), "*.txt", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(root, "tests", "nvda-scripts"), "*.txt", SearchOption.AllDirectories))
            .ToList();
        Assert.NotEmpty(scripts);
        return scripts;
    }

    private static string Lf(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
