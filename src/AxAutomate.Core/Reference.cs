using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>A script as the reference reads it: its path from the repository's root with forward slashes ("tests/scripts/axDown/04-find-and-go-to-line.txt") and its lines.</summary>
public sealed record ReferenceScript(string Path, IReadOnlyList<string> Lines);

/// <summary>A page of the reference: its path from the repository's root with forward slashes, and its text, every line ended by a line feed.</summary>
public sealed record ReferencePage(string Path, string Text);

/// <summary>
/// The reference for people (docs/axKit/user-workflows.md WF-7, formats 5.6), pure: the pages a user, a screen reader
/// and Claude in axHelp read, written from two inputs and nothing else, the scripts and the command inventory
/// (<see cref="CommandInventory"/>). The index names each group's workflows and under each its goals; a group's page
/// holds each goal's steps as numbered sentences, the key named as it is pressed, with what is heard under them; the
/// commands page holds one line a command. Each script is read by the engine's own reader
/// (<see cref="ScriptReader.Parse"/>, <see cref="ScriptApps.ByLine"/>), so a page and a run cannot read a script
/// differently. Nothing of the checks stands on a page (WF-7.5): no check, no script path, no needs line, nothing of
/// a run. The pages are heard, so every sentence is one a person can follow by ear (WF-7.9, WF-D-31): a window is
/// named by its app, a key as a keyboard names it, a text cut short says so. The words here are the pages' own:
/// axAutomate's window keeps <see cref="StepText"/>'s. The command line writes the pages and compares them
/// (<c>tools/workflows.ps1</c>); tested in <c>tests/AxAutomate.Tests/ReferenceTests</c>.
/// </summary>
public static class Reference
{
    /// <summary>The index, by group each workflow's label and under it its goals' titles.</summary>
    public const string IndexPath = "docs/axKit/workflows.md";

    /// <summary>The folder of the groups' pages, <c>&lt;page&gt;.md</c> each (<see cref="PageName"/>); a file of it that is no page any more is a page of a group that is gone.</summary>
    public const string PagesFolder = "docs/axKit/workflows";

    /// <summary>The page of every command of the inventory.</summary>
    public const string CommandsPath = "docs/axKit/window-commands.md";

    /// <summary>The group of <c>tests/scripts/mcp</c>.</summary>
    public const string AiTools = "AI tools";

    /// <summary>The group of <c>tests/nvda-scripts</c>.</summary>
    public const string NvdaOnly = "NVDA only";

    /// <summary>The heading of the commands page under which the rows of every app stand once (WF-7.6).</summary>
    public const string EveryWindow = "In every window";

    /// <summary>What a <c>{temp}</c> that stands alone is written as (WF-7.7): no path of a run is on a page.</summary>
    private const string TestFolder = "the test folder";

    /// <summary>The run's folder as a script writes it before a path in it.</summary>
    private const string TempFolder = @"{temp}\";

    /// <summary>
    /// What a line under a numbered step starts with: four spaces, so that it stays inside its item in Markdown also
    /// from item 10 on, whose text starts four characters in. A line before step 1 has none: four spaces there would
    /// make it a code block.
    /// </summary>
    private const string Under = "    ";

    /// <summary>What is dropped from the ends of an expectation's text (WF-7.4): the scripts match parts of announcements.</summary>
    private static readonly char[] Edges = [',', ':', ';', '|', '.', ' '];

    /// <summary>The words an announcement never ends with (WF-7.4): a text that ends with one was cut short in the script.</summary>
    private static readonly string[] Joining = ["of", "to", "the", "and", "a", "an", "or", "at"];

    // The three preambles of 5.6, word for word and line for line.
    private static readonly string[] IndexPreamble =
    [
        "Written by `tools/workflows.ps1` from the scripts of `tests/scripts` and `tests/nvda-scripts` and the command",
        "inventory (`docs/axKit/window-commands.json`); never edit it by hand. What a user can do in axKit, by app: each",
        "workflow is one script of the automated checks, found by its label, a task in the user's words, and under it are its",
        "goals. The steps of every goal, with the keys as they are now, are in the app's page. The checks run on the",
        "developer's computer (`tools/AxAutomate`, `nvda-pass.ps1`), never in CI (`docs/axKit/automation.md`,",
        "`docs/axKit/user-workflows.md`).",
    ];

    private static readonly string[] GroupPreamble =
    [
        "Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it",
        "is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A",
        "line \"You hear\" gives words of what axKit says there, not always the whole sentence; after \"words such as\" they are",
        "cut short. After \"Type:\" comes the text to type, to the end of the line, with no full stop added. The file names and",
        "the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that",
        "command's in this version of axKit.",
    ];

    private static readonly string[] CommandsPreamble =
    [
        "Written by `tools/workflows.ps1` from `docs/axKit/window-commands.json`, which the automated checks write from the",
        "windows themselves; never edit it by hand. One line a command: its name, its menu, its key, and its mark, what it",
        "does beyond reading (read, open, change, act). A command that all or nearly all windows have stands once, under \"In",
        "every window\" or under a heading that names the windows without it; under an app's name are the commands of its own.",
    ];

    /// <summary>A goal as a page shows it: its title and its lines (<see cref="GoalLines"/>).</summary>
    private sealed record ShownGoal(string Title, IReadOnlyList<string> Lines);

    /// <summary>
    /// A workflow as the pages show it: its label, how it starts (empty for nothing), the steps of a script with no
    /// goal line (none for a script with sections), and its goals, the checks and the goals without an input step
    /// left out.
    /// </summary>
    private sealed record ShownWorkflow(string Label, string Starts, IReadOnlyList<string> Steps, IReadOnlyList<ShownGoal> Goals);

    private sealed record ShownGroup(string Name, IReadOnlyList<ShownWorkflow> Workflows);

    /// <summary>
    /// A step that presses keys, as its line is written: the keys, the command named after them (" (Find; Navigation
    /// menu)", or nothing) and how many times in a row, "Press Tab 7 times." (WF-7.3). <c>One</c> says that it is one
    /// key: only such presses are counted together, since "Press Down, Enter 2 times" would not say what is pressed twice.
    /// </summary>
    private sealed record Press(string Keys, string Hint, int Count, bool One)
    {
        public string Text => $"Press {Keys}{(Count > 1 ? $" {Count} times" : string.Empty)}{Hint}.";
    }

    /// <summary>
    /// The pages (WF-7.2), each line ended by a line feed: the index first, then one page per group in the groups'
    /// order (WF-7.8), then the commands page. A script without a <c># Workflow:</c> line is refused: an
    /// <see cref="InvalidOperationException"/> names every such script. An empty inventory gives a commands page of
    /// its preamble alone, and steps that name no command.
    /// </summary>
    public static IReadOnlyList<ReferencePage> Pages(IReadOnlyList<ReferenceScript> scripts, IReadOnlyList<InventoryApp> inventory)
    {
        var groups = Groups(scripts, inventory);
        var pages = new List<ReferencePage> { new(IndexPath, Index(groups)) };
        pages.AddRange(groups.Select(group => new ReferencePage($"{PagesFolder}/{PageName(group.Name)}.md", GroupPage(group))));
        pages.Add(new ReferencePage(CommandsPath, CommandsPage(inventory)));
        return pages;
    }

    /// <summary>
    /// How many workflows and how many how-tos the pages hold, for the line the command line prints: a workflow is a
    /// script; a how-to is a goal that stands on a page, or the steps of a script with no goal line. A script without
    /// a label is refused as <see cref="Pages"/> refuses it.
    /// </summary>
    public static (int Workflows, int Goals) Counts(IReadOnlyList<ReferenceScript> scripts)
    {
        var workflows = Groups(scripts, []).SelectMany(group => group.Workflows).ToList();
        return (workflows.Count, workflows.Sum(workflow => workflow.Goals.Count + (workflow.Steps.Count > 0 ? 1 : 0)));
    }

    /// <summary>A script's group (section 3), by the folder it is in: <see cref="NvdaOnly"/> for <c>tests/nvda-scripts</c>, <see cref="AiTools"/> for <c>tests/scripts/mcp</c>, else the folder's name, an app's ("axDown").</summary>
    public static string Group(string scriptPath)
    {
        var parts = Slashed(scriptPath).Split('/', StringSplitOptions.RemoveEmptyEntries);
        var folder = parts.Length >= 2 ? parts[^2] : string.Empty;
        return Same(folder, "nvda-scripts") ? NvdaOnly : Same(folder, "mcp") ? AiTools : folder;
    }

    /// <summary>The name of a group's page without its extension (5.6): the group's name for an app ("axDown"), "ai-tools" and "nvda-only" for the two others.</summary>
    public static string PageName(string group) => group switch
    {
        AiTools => "ai-tools",
        NvdaOnly => "nvda-only",
        _ => group,
    };

    /// <summary>
    /// A goal's lines (WF-7.3, WF-7.4, format 5.6) from its steps, a <c>try</c> with its <c>iffound</c> steps in it
    /// (<see cref="WorkflowStep.Then"/>): each input step (<see cref="ScriptReader.IsInput"/>) a numbered sentence
    /// ended by a full stop (typed text ends the line itself, with nothing added), an <c>iffound</c> one after "If
    /// that came: "; presses that are equal and follow each other with nothing heard between them are one line,
    /// "Press Tab 7 times."; each expectation of speech a line of four spaces and what is heard, under the input step
    /// before it, or without the spaces before step 1 when none came yet. Every other step is left out: the set-up,
    /// the waits, the checks of state, the refusals. <paramref name="apps"/> is the script's
    /// <see cref="ScriptApps.ByLine"/>, by which a step's command is found among its app's rows of the inventory and a
    /// window is named. None when the steps hold no input step: a how-to with nothing to do is no how-to (WF-D-28).
    /// </summary>
    public static IReadOnlyList<string> GoalLines(IReadOnlyList<WorkflowStep> steps, IReadOnlyList<string?> apps, IReadOnlyList<InventoryApp> inventory)
    {
        var lines = new List<string>();
        var number = 0;

        // The press the last line is, while nothing stands after it: an equal press that follows joins it.
        Press? last = null;
        foreach (var step in steps.Where(step => !step.IsNote))
        {
            var conditional = step.Then.Where(then => !then.IsNote).ToList();
            if (ScriptReader.IsInput(step.Do))
            {
                var sentence = Sentence(step, apps, inventory);
                var press = AsPress(sentence);
                if (press is { One: true } && last is { One: true } && last.Keys == press.Keys && last.Hint == press.Hint)
                {
                    last = last with { Count = last.Count + press.Count };
                    lines[^1] = $"{number}. {last.Text}";
                }
                else
                {
                    number++;
                    last = press;
                    lines.Add($"{number}. {press?.Text ?? Ended(step, sentence)}");
                }
            }
            else if (HeardLine(step, step.OnFailure == WorkflowStep.Continue, conditional.Any(then => ScriptReader.IsInput(then.Do)), Before(lines)) is { } heard)
            {
                lines.Add((number == 0 ? string.Empty : Under) + heard);
                last = null;
            }

            // The steps that run only when the try's words came: each input says so; what is heard before the first
            // of them may come or not, as the try's own words, and after it stands under that input.
            var acted = false;
            foreach (var then in conditional)
            {
                if (ScriptReader.IsInput(then.Do))
                {
                    number++;
                    acted = true;
                    last = null;
                    lines.Add($"{number}. If that came: {Ended(then, Sentence(then, apps, inventory))}");
                }
                else if (HeardLine(then, !acted || then.OnFailure == WorkflowStep.Continue, keepShort: false, Before(lines)) is { } heard)
                {
                    lines.Add((number == 0 ? string.Empty : Under) + heard);
                    last = null;
                }
            }
        }

        return number == 0 ? [] : lines;
    }

    /// <summary>
    /// An expectation's text as words to read (WF-7.4): without its mark ("! " an interrupting announcement, the marks
    /// of a sound, a focus move and a value, as <see cref="Event.Spoken"/> writes them), without a comma, colon,
    /// semicolon, bar, full stop or space at either end, without a bracket that opens and never closes and what
    /// follows it, and without a joining word at its end ("of", "to", "the", "and", "a", "an", "or", "at") when two
    /// words stay without it. Empty when no letter and no digit is left.
    /// </summary>
    public static string Heard(string text) => Read(Unmarked(text)).Words;

    /// <summary>
    /// Whether an expectation's text reads as a whole announcement (WF-7.4): the script wrote it without a comma or
    /// colon at either end, with every bracket closed and with no joining word last. A text that is not whole was cut
    /// short, and its line says "words such as".
    /// </summary>
    public static bool IsWhole(string text) => Read(Unmarked(text)).Whole;

    /// <summary>
    /// The groups with their workflows as the pages show them, in the pages' order (WF-7.8): the apps by name without
    /// regard to case, then <see cref="AiTools"/>, then <see cref="NvdaOnly"/>; in a group the scripts by their path,
    /// ordinal, lower case.
    /// </summary>
    private static List<ShownGroup> Groups(IReadOnlyList<ReferenceScript> scripts, IReadOnlyList<InventoryApp> inventory)
    {
        // The label is read with an empty title in its place: a script whose title is still empty has no label line.
        var read = scripts
            .OrderBy(script => Slashed(script.Path).ToLowerInvariant(), StringComparer.Ordinal)
            .Select(script => (Script: script, Workflow: ScriptReader.Parse(script.Lines, string.Empty, script.Path)))
            .ToList();
        var unlabelled = read.Where(one => one.Workflow.Title.Length == 0).Select(one => Slashed(one.Script.Path)).ToList();
        if (unlabelled.Count > 0)
        {
            throw new InvalidOperationException("No '# Workflow:' line: " + string.Join(", ", unlabelled));
        }

        return [.. read
            .GroupBy(one => Group(one.Script.Path))
            .OrderBy(group => group.Key == NvdaOnly ? 2 : group.Key == AiTools ? 1 : 0)
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ShownGroup(group.Key, [.. group.Select(one => Shown(one.Script, one.Workflow, inventory))]))];
    }

    /// <summary>
    /// One script as the pages show it (WF-7.5). With goal or check lines: "Starts with" is the setup's window steps,
    /// and each goal that holds an input step has its lines; a check has none. With neither line the script is one
    /// goal (WF-3.3): its window steps before the first other input step are how it starts, and its steps follow; a
    /// script whose only input steps are window steps has them as its steps, since opening the window is what it shows.
    /// </summary>
    private static ShownWorkflow Shown(ReferenceScript script, Workflow workflow, IReadOnlyList<InventoryApp> inventory)
    {
        var apps = ScriptApps.ByLine(script.Lines);
        var steps = workflow.Steps.Where(step => !step.IsNote).ToList();
        if (!workflow.Goals.Any(goal => goal.Named))
        {
            var first = steps.FindIndex(step => ScriptReader.IsInput(step.Do) && !IsWindowStep(step));
            var opening = first < 0 ? [] : steps.Take(first).Where(IsWindowStep).ToList();
            var rest = first < 0 ? steps : [.. steps.Where((step, index) => index >= first || !IsWindowStep(step))];
            return new ShownWorkflow(workflow.Title, Starts(opening, apps, inventory), GoalLines(rest, apps, inventory), []);
        }

        var firstSection = workflow.Goals[0].FirstLine;
        var setup = steps.Where(step => step.Line < firstSection && IsWindowStep(step)).ToList();
        var goals = workflow.Goals
            .Where(goal => !goal.Check)
            .Select(goal => new ShownGoal(goal.Title, GoalLines([.. steps.Where(step => goal.FirstLine <= step.Line && step.Line <= goal.LastLine)], apps, inventory)))
            .Where(goal => goal.Lines.Count > 0)
            .ToList();
        return new ShownWorkflow(workflow.Title, Starts(setup, apps, inventory), [], goals);
    }

    /// <summary>A step that opens, chooses or closes a window (section 3): what a workflow starts with.</summary>
    private static bool IsWindowStep(WorkflowStep step) => step.Do is "start" or "launch" or "adopt" or "use" or "close";

    /// <summary>The window steps a workflow starts with, as sentences: "Open axDown with the file find.md."; empty for none.</summary>
    private static string Starts(IReadOnlyList<WorkflowStep> windowSteps, IReadOnlyList<string?> apps, IReadOnlyList<InventoryApp> inventory) =>
        string.Join(" ", windowSteps.Select(step => Stopped(Sentence(step, apps, inventory))));

    /// <summary>
    /// The index (5.6): its preamble, then for each group where its steps are, and each workflow's label, numbered,
    /// with its goals' titles under it, four spaces in, so that they stay under a label numbered 10 or more.
    /// </summary>
    private static string Index(IReadOnlyList<ShownGroup> groups)
    {
        var blocks = new List<IReadOnlyList<string>> { new[] { "# Workflows" }, IndexPreamble };
        foreach (var group in groups)
        {
            var page = PageName(group.Name);
            blocks.Add(["## " + group.Name]);
            blocks.Add([$"Steps and keys: `{PagesFolder}/{page}.md` (in Claude's working folder: `workflows-{page}.md`)."]);
            var list = new List<string>();
            for (var i = 0; i < group.Workflows.Count; i++)
            {
                list.Add($"{i + 1}. **{group.Workflows[i].Label}**");
                list.AddRange(group.Workflows[i].Goals.Select(goal => Under + "- " + goal.Title));
            }

            blocks.Add(list);
        }

        return Text(blocks);
    }

    /// <summary>A group's page (5.6): its preamble, then each workflow with how it starts, its steps when it has no goal line, and each goal under its title.</summary>
    private static string GroupPage(ShownGroup group)
    {
        var blocks = new List<IReadOnlyList<string>> { new[] { $"# {group.Name}: workflows" }, GroupPreamble };
        foreach (var workflow in group.Workflows)
        {
            blocks.Add(["## " + workflow.Label]);
            if (workflow.Starts.Length > 0)
            {
                blocks.Add(["Starts with: " + workflow.Starts]);
            }

            if (workflow.Steps.Count > 0)
            {
                blocks.Add(workflow.Steps);
            }

            foreach (var goal in workflow.Goals)
            {
                blocks.Add(["### " + goal.Title]);
                blocks.Add(goal.Lines);
            }
        }

        return Text(blocks);
    }

    /// <summary>
    /// The commands page (WF-7.6, 5.6): its preamble, then the rows that all or nearly all apps have, each once: under
    /// <see cref="EveryWindow"/> the rows of every app, then under "In every window but axClip and axStart" the rows
    /// of three apps in four or more, by the apps without them; then each app of the inventory, by name, with the
    /// rows that are its own, in the inventory's order. A row is the same in two apps when its whole line is. With
    /// one app nothing is shared.
    /// </summary>
    private static string CommandsPage(IReadOnlyList<InventoryApp> inventory)
    {
        var apps = inventory.Where(app => app.Commands.Count > 0).OrderBy(app => app.App, StringComparer.OrdinalIgnoreCase).ToList();
        var rows = apps.Select(app => (app.App, Lines: app.Commands.Select(CommandLine).ToList())).ToList();

        // Each line once, in the order the apps give it, with the apps that have it.
        var order = rows.SelectMany(app => app.Lines).Distinct().ToList();
        var holders = order.ToDictionary(line => line, line => rows.Where(app => app.Lines.Contains(line)).Select(app => app.App).ToList());
        var shared = order.Where(line => holders[line].Count >= 2 && holders[line].Count * 4 >= apps.Count * 3).ToHashSet();

        var blocks = new List<IReadOnlyList<string>> { new[] { "# The commands of every window" }, CommandsPreamble };
        var sections = order.Where(shared.Contains)
            .GroupBy(line => string.Join(", ", apps.Select(app => app.App).Where(app => !holders[line].Contains(app))))
            .OrderBy(section => apps.Count - holders[section.First()].Count)
            .ThenBy(section => section.Key, StringComparer.OrdinalIgnoreCase);
        foreach (var section in sections)
        {
            blocks.Add(["## " + EveryWindow + (section.Key.Length > 0 ? " but " + Listed(section.Key.Split(", ")) : string.Empty)]);
            blocks.Add([.. section]);
        }

        foreach (var (app, lines) in rows)
        {
            var own = lines.Where(line => !shared.Contains(line)).ToList();
            if (own.Count > 0)
            {
                blocks.Add(["## " + app]);
                blocks.Add(own);
            }
        }

        return Text(blocks);
    }

    /// <summary>Names as a sentence lists them: "axClip", "axClip and axStart", "axClip, axStart and axTerminal".</summary>
    private static string Listed(IReadOnlyList<string> names) =>
        names.Count == 1 ? names[0] : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1];

    /// <summary>
    /// A row's line of the commands page: "- Redo. Menu: Actions. Key: Ctrl+Y. Also: Ctrl+Shift+Z. Mark: change." The
    /// key is the row's key text, the further chords are in key words (<see cref="Keys"/>), a key with no menu item
    /// says so, and a row the AI tools never run ends with that.
    /// </summary>
    private static string CommandLine(ListedCommand row)
    {
        var parts = new List<string>
        {
            Stopped(CommandNames.Plain(row.Label)),
            $"Menu: {(row.KeyOnly ? "none, a key alone" : row.Menu.Length > 0 ? row.Menu : "none")}.",
            $"Key: {(row.KeyText.Length > 0 ? row.KeyText : "none")}.",
        };
        if (row.Also.Count > 0)
        {
            parts.Add($"Also: {Keys(string.Join(' ', row.Also))}.");
        }

        if (row.Mark.Length > 0)
        {
            parts.Add($"Mark: {row.Mark}.");
        }

        if (!row.Ai)
        {
            parts.Add("Never run by AI tools.");
        }

        return "- " + string.Join(" ", parts);
    }

    /// <summary>A page's text: its blocks, each a run of lines, with one blank line between two, every line ended by a line feed.</summary>
    private static string Text(IReadOnlyList<IReadOnlyList<string>> blocks) =>
        string.Join("\n\n", blocks.Select(block => string.Join("\n", block))) + "\n";

    /// <summary>
    /// An input step's sentence without its end (WF-7.3), in the page's own words: what a person does, never the
    /// runner's mechanics. <c>do</c>, <c>keys</c> and <c>menu</c> with the command's key and menus; <c>start</c> and
    /// <c>launch</c> as opening the app; <c>adopt</c> and <c>use</c> as going to a window, named by its app where the
    /// script's lines say it; <c>type</c> as "Type: " and the text; the rest (<c>close</c>, <c>mcp call</c>) in
    /// <see cref="StepText.Sentence"/>'s words. Then without the run's folder and with the escapes filled in
    /// (<see cref="Filled"/>).
    /// </summary>
    private static string Sentence(WorkflowStep step, IReadOnlyList<string?> apps, IReadOnlyList<InventoryApp> inventory)
    {
        var app = AppAt(apps, step.Line);
        return Filled(step.Do switch
        {
            "do" => DoSentence(step, app, inventory),
            "keys" => KeysSentence(step, app, inventory),
            "menu" => MenuSentence(step, app, inventory),
            "start" => StartSentence(step, again: false),
            "launch" => StartSentence(step, again: true),
            "adopt" => "A new window opens: go to it",
            "use" => UseSentence(app, AppAt(apps, step.Line - 1)),
            "type" => "Type: " + step.Value("text"),
            _ => StepText.Sentence(step),
        });
    }

    /// <summary>
    /// A step's sentence with its end: a full stop, except after typed text, where the line ends with the text, so
    /// that a listener knows that no full stop is typed.
    /// </summary>
    private static string Ended(WorkflowStep step, string sentence) => step.Do == "type" ? sentence : Stopped(sentence);

    /// <summary>
    /// A sentence that presses keys, as a <see cref="Press"/>: its keys, the command named after them, and how many
    /// times when it is one key several times ("Press Down, Down, Down" is Down 3 times); null for any other sentence.
    /// </summary>
    private static Press? AsPress(string sentence)
    {
        const string Opening = "Press ";
        if (!sentence.StartsWith(Opening, StringComparison.Ordinal))
        {
            return null;
        }

        var open = sentence.EndsWith(')') ? sentence.IndexOf(" (", StringComparison.Ordinal) : -1;
        var keys = (open < 0 ? sentence : sentence[..open])[Opening.Length..];
        var parts = keys.Split(", ");
        var same = parts.All(part => part == parts[0]);
        return new Press(same ? parts[0] : keys, open < 0 ? string.Empty : sentence[open..], same ? parts.Length : 1, same);
    }

    /// <summary>
    /// The step <c>do</c> with the command's key and menus from the inventory: "Press Ctrl+H (Heading; Navigation menu,
    /// Next)"; for a key with no menu item "Press Ctrl+Tab (Between the text and the details)"; for a menu item with
    /// no chord, which the step chooses, "Choose Send message in the Actions menu"; for a name the inventory does not
    /// resolve, the step's own words, "Do the command Find". Where the script's lines do not say whose window the step
    /// reaches (<c>adopt</c>, a start by a file alone), every app of the inventory is asked, and the command is read
    /// with its key only when all that have it read the same: a key of another app's command of that name is never
    /// written.
    /// </summary>
    private static string DoSentence(WorkflowStep step, string? app, IReadOnlyList<InventoryApp> inventory)
    {
        IEnumerable<string> asked = app is null ? inventory.Select(known => known.App) : [app];
        var sentences = new List<string>();
        foreach (var known in asked)
        {
            CommandInventory.Resolve(inventory, known, step.Value("command"), out var row);
            if (row is not null && RowSentence(row) is { } sentence && !sentences.Contains(sentence))
            {
                sentences.Add(sentence);
            }
        }

        return sentences.Count == 1 ? sentences[0] : StepText.Sentence(step);
    }

    /// <summary>A row as a <c>do</c> step reads: pressed by its key, with its label and its menus after it, or chosen in its menus when it has no chord; null for a row with neither.</summary>
    private static string? RowSentence(ListedCommand row)
    {
        var key = PressedKey(row);
        if (key.Length > 0)
        {
            return $"Press {key} ({Named(row)})";
        }

        var menus = Menus(row.Menu);
        return menus.Length > 0 ? $"Choose {CommandNames.Plain(row.Label)} in the {menus}" : null;
    }

    /// <summary>
    /// A command as it is named after its key, the same after a <c>do</c> step and after a <c>keys</c> step, so that
    /// one key reads one way on a page: its label and its menus from the bar down, "Heading; Navigation menu, Next",
    /// or its label alone for a key with no menu item.
    /// </summary>
    private static string Named(ListedCommand row)
    {
        var label = CommandNames.Plain(row.Label);
        var menus = Menus(row.Menu);
        return menus.Length > 0 ? $"{label}; {menus}" : label;
    }

    /// <summary>
    /// The step <c>keys</c>: "Press Ctrl+F", the chords in key words (<see cref="Keys"/>). A line of one chord that
    /// carries Ctrl or Alt or is a function key (<see cref="KeyChord.IsCommandChord"/>) and that exactly one row of the
    /// app holds names that command as a <c>do</c> step would, "Press Ctrl+G (Go to line; Navigation menu)": a hint
    /// from the inventory, since the script's state is not known here. The key is the one the script presses: the
    /// row's key as its menu shows it when that is the chord, else the chord in key words. A plain key names none:
    /// Enter, Backspace or a letter is a field's or a list's own key far more often than the command the inventory
    /// has for it, and "Press Backspace (Up one level)" in a field would be wrong.
    /// </summary>
    private static string KeysSentence(WorkflowStep step, string? app, IReadOnlyList<InventoryApp> inventory)
    {
        var presses = step.Value("keys").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (presses.Length != 1 || !KeyChord.IsCommandChord(presses[0]) || CommandInventory.OfChord(inventory, app, presses[0]) is not { } row)
        {
            return "Press " + Keys(step.Value("keys"));
        }

        var own = row.Key.Length > 0 && KeyChord.Normal(row.Key) == KeyChord.Normal(presses[0]);
        return $"Press {(own ? PressedKey(row) : Keys(presses[0]))} ({Named(row)})";
    }

    /// <summary>
    /// The step <c>menu</c>: a text that exactly one row of the app answers (<see cref="CommandInventory.OfMenuText"/>)
    /// reads with that row's whole label and menus, and its key when it has one, "Choose Larger text in the Options
    /// menu, Text (key: Ctrl+Plus)": as the choice it is, since the check chose the item and did not press the key.
    /// Otherwise the text as the script has it, "Choose the menu item &lt;text&gt;", without a bracket that opens
    /// and never closes: a script may name an item by its first words ("Check for updates (axKit").
    /// </summary>
    private static string MenuSentence(WorkflowStep step, string? app, IReadOnlyList<InventoryApp> inventory)
    {
        if (CommandInventory.OfMenuText(inventory, app, step.Value("name")) is not { } row || Menus(row.Menu) is not { Length: > 0 } menus)
        {
            return "Choose the menu item " + Closed(step.Value("name")).TrimEnd();
        }

        var key = ShownKey(row);
        return $"Choose {CommandNames.Plain(row.Label)} in the {menus}" + (key.Length > 0 ? $" (key: {key})" : string.Empty);
    }

    /// <summary>
    /// The steps <c>start</c> and <c>launch</c> (the second hands over to a window that is open, which for a person is
    /// starting the app again): "Open axDown with the file find.md" for a start whose first word is a verb of
    /// <see cref="Dispatch.Apps"/>, "Open axDown" without arguments, "Open the file settings.json" for a start by a
    /// file alone; "Start axClip again", "Start axMedia again with --add the folder ch", "Open the file a.md again".
    /// The arguments are the ones a person gives (<see cref="Given"/>).
    /// </summary>
    private static string StartSentence(WorkflowStep step, bool again)
    {
        var words = Words(step.Value("arguments"));
        var named = words.Count > 0 ? Dispatch.Apps.FirstOrDefault(app => Same(app.Verb, words[0])) : null;
        var what = string.Join(" ", Given(named is null ? words : [.. words.Skip(1)], named?.Name));
        if (named is null)
        {
            return what.Length == 0 ? "Open axKit" + (again ? " again" : string.Empty) : $"Open {what}{(again ? " again" : string.Empty)}";
        }

        var opening = again ? $"Start {named.Name} again" : $"Open {named.Name}";
        return what.Length > 0 ? $"{opening} with {what}" : opening;
    }

    /// <summary>
    /// A start's arguments as a person gives them (WF-7.3). What only serves the check is left out: in a start of
    /// axClaude or axHelp everything after a bare <c>--</c> (Claude Code's own options, by which a check picks its
    /// model), <c>--record</c> with its file, <c>--model</c> with its value, and <c>--claude</c> with a program in the
    /// run's folder (the check's stand-in for Claude Code). A path in the run's folder is named by what it is, "the
    /// folder notes" or "the file find.md" (a name with an extension is a file), and the run's folder itself is "the
    /// test folder": <c>{temp}</c> is never on a page.
    /// </summary>
    private static List<string> Given(IReadOnlyList<string> words, string? app)
    {
        var given = new List<string>();
        for (var i = 0; i < words.Count; i++)
        {
            var word = words[i];
            if (word == "--" && (app is "axClaude" or "axHelp"))
            {
                break;
            }

            var value = i + 1 < words.Count ? words[i + 1].Trim('"') : string.Empty;
            if (word is "--record" or "--model" || (word == "--claude" && value.StartsWith(TempFolder, StringComparison.Ordinal)))
            {
                i++;
                continue;
            }

            var path = word.Trim('"');
            given.Add(path == "{temp}" ? TestFolder
                : path.StartsWith(TempFolder, StringComparison.Ordinal) ? (IsFileName(path) ? "the file " : "the folder ") + path[TempFolder.Length..]
                : word);
        }

        return given;
    }

    /// <summary>Whether a path's last part has an extension, as a file's name has and a folder's seldom.</summary>
    private static bool IsFileName(string path) => path[(path.LastIndexOfAny(['\\', '/']) + 1)..].IndexOf('.') > 0;

    /// <summary>A command line's words: split at spaces, a part in double quotes kept as one word with its quotes.</summary>
    private static List<string> Words(string arguments)
    {
        var words = new List<string>();
        var word = new System.Text.StringBuilder();
        var quoted = false;
        foreach (var character in arguments)
        {
            if (character == ' ' && !quoted)
            {
                if (word.Length > 0)
                {
                    words.Add(word.ToString());
                    word.Clear();
                }

                continue;
            }

            quoted ^= character == '"';
            word.Append(character);
        }

        if (word.Length > 0)
        {
            words.Add(word.ToString());
        }

        return words;
    }

    /// <summary>
    /// The step <c>use</c>, as going to a window: "Go to the axFile window" where the script's lines name its app
    /// (<see cref="ScriptApps.ByLine"/>), "Go to the other axDown window" when the window left is of the same app, "Go
    /// to the other window" where the app is not known. A window's number is the run's, and no person counts windows.
    /// </summary>
    private static string UseSentence(string? app, string? left) =>
        app is null ? "Go to the other window"
        : app == left ? $"Go to the other {app} window"
        : $"Go to the {app} window";

    /// <summary>A row's menus from the bar down, the word "menu" after the first: "Navigation menu", "Navigation menu, Next"; empty for a row without menus.</summary>
    private static string Menus(string menu)
    {
        var plain = CommandNames.Plain(menu);
        var comma = plain.IndexOf(", ", StringComparison.Ordinal);
        return plain.Length == 0 ? string.Empty : comma < 0 ? plain + " menu" : plain[..comma] + " menu" + plain[comma..];
    }

    /// <summary>
    /// Keys as a keyboard names them (WF-7.3): "ctrl+shift+p pagedown" is "Ctrl+Shift+P, Page Down", "nvda+a" is
    /// "NVDA+A". Each key's name with a capital, and the few that are two words or an abbreviation by name. A row's
    /// own key text, which is what its menu shows, never passes through here.
    /// </summary>
    private static string Keys(string keys) =>
        string.Join(", ", keys.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(chord => string.Join("+", chord.Split('+').Select(KeyWord))));

    private static string KeyWord(string key) => key.ToLowerInvariant() switch
    {
        "pagedown" => "Page Down",
        "pageup" => "Page Up",
        "nvda" => "NVDA",
        "esc" => "Escape",
        _ => key.Length == 0 ? key : char.ToUpperInvariant(key[0]) + key[1..],
    };

    /// <summary>
    /// The key a <c>do</c> step is read with: the row's key as its menu shows it (the window's own spelling, where it
    /// differs from the chord's words), else the first of its further chords in key words, else, for a key with no
    /// menu item, its key text. Empty for a menu item with no chord, which is chosen and not pressed.
    /// </summary>
    private static string PressedKey(ListedCommand row) =>
        row.Key.Length > 0 ? (row.KeyText.Length > 0 ? row.KeyText : Keys(row.Key))
        : row.Also.Count > 0 ? Keys(row.Also[0])
        : row.KeyOnly ? row.KeyText
        : string.Empty;

    /// <summary>The key a menu item is named with after a <c>menu</c> step: its key text as the menu shows it, else its first chord in key words; empty for an item without a key.</summary>
    private static string ShownKey(ListedCommand row) =>
        row.KeyText.Length > 0 ? row.KeyText
        : row.Key.Length > 0 ? Keys(row.Key)
        : row.Also.Count > 0 ? Keys(row.Also[0])
        : string.Empty;

    /// <summary>
    /// The line of an expectation of speech (WF-7.4), ended by a full stop, or null for a step that is none or leaves
    /// nothing to read: <c>expect said</c> "You hear: ...", a sound "You hear the sound Edge.", a focus move "The focus
    /// moves to ...", a value "It reads ..."; <c>expect spoken</c> "NVDA says: ..."; <c>expect mcp</c> "The tool
    /// answers: ..." (<see cref="ToolLine"/>). With <paramref name="may"/> (a <c>try said</c>, which may not come)
    /// "You may hear: ...". Words that were cut short in the script (<see cref="IsWhole"/>) say so: "You hear words
    /// such as: ...". One word is kept, since one word or one number can be the whole point ("8294400" after a sum),
    /// unless the line before it (<paramref name="before"/>) already holds that word: then it only repeats it. With
    /// <paramref name="keepShort"/> it always stays: the words of a try that steps hang on, since "If that came"
    /// under it must have what came.
    /// </summary>
    private static string? HeardLine(WorkflowStep step, bool may, bool keepShort, string before)
    {
        var text = Filled(step.Value("text"));
        switch (step.Do)
        {
            case "expect spoken":
                return Announced("NVDA says", Read(text), keepShort: false, before);
            case "expect mcp":
                return ToolLine(text, before);
            case "expect said":
                break;
            default:
                return null;
        }

        var read = Read(Unmarked(text));
        if (text.StartsWith('♪'))
        {
            return Line(may ? "You may hear the sound " : "You hear the sound ", read.Words);
        }

        if (text.StartsWith("→ ", StringComparison.Ordinal))
        {
            return Line(may ? "The focus may move to " : "The focus moves to ", read.Words);
        }

        if (text.StartsWith("· ", StringComparison.Ordinal))
        {
            return Line(may ? "It may read " : "It reads ", read.Words);
        }

        return Announced(may ? "You may hear" : "You hear", read, keepShort, before);
    }

    /// <summary>Words of an announcement after who says them: "You hear: Line 5 of 7." for a whole text, "You hear words such as: Line 5." for one cut short; null for no words, and for one word that the line before already holds.</summary>
    private static string? Announced(string opening, (string Words, bool Whole) read, bool keepShort, string before) =>
        !keepShort && Repeats(read.Words, before) ? null : Line(opening + (read.Whole ? ": " : " words such as: "), read.Words);

    /// <summary>
    /// The line of a tool's answer (WF-7.4): a text the check wrote as a piece of JSON, <c>"app": "axDown"</c>, reads as
    /// a sentence, "The tool answers with app: axDown.", the name in plain words ("notDone" is "not done"); any other
    /// text reads "The tool answers: ...". Null for no words, and for one word that the line before already holds.
    /// </summary>
    private static string? ToolLine(string text, string before)
    {
        var trimmed = text.Trim();
        var end = trimmed.StartsWith('"') ? trimmed.IndexOf("\":", 1, StringComparison.Ordinal) : -1;
        if (end > 1)
        {
            var value = trimmed[(end + 2)..].Trim().Trim('"').Trim(Edges);
            return value.Length > 0 ? Stopped($"The tool answers with {Spaced(trimmed[1..end])}: {value}") : null;
        }

        var words = Read(text).Words;
        return Repeats(words, before) ? null : Line("The tool answers: ", words);
    }

    /// <summary>A name of data in plain words: "notDone" is "not done", "doneLeftOut" is "done left out".</summary>
    private static string Spaced(string name) =>
        string.Concat(name.Select((character, index) => char.IsUpper(character) && index > 0 ? " " + char.ToLowerInvariant(character) : character.ToString()));

    /// <summary>What is heard after its opening words, ended by a full stop; null when no words are left.</summary>
    private static string? Line(string opening, string words) => words.Length > 0 ? Stopped(opening + words) : null;

    /// <summary>Whether what is heard is one word that the line before it already holds, without regard to case: "You hear: apple." under "Type: apple" tells nothing.</summary>
    private static bool Repeats(string words, string before) =>
        words.Length > 0 && !words.Contains(' ')
        && before.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(word => Same(word.Trim('.', ',', ':', ';', '(', ')', '"'), words));

    /// <summary>The line before the one being written; empty at a goal's start.</summary>
    private static string Before(List<string> lines) => lines.Count > 0 ? lines[^1] : string.Empty;

    /// <summary>
    /// An expectation's text, without its mark, as words to read and whether it reads as a whole announcement
    /// (WF-7.4). The edges are dropped (<see cref="Edges"/>); a bracket that opens and never closes goes with what
    /// follows it (<see cref="Closed"/>); a joining word at the end (<see cref="Joining"/>) goes when two words stay
    /// without it. The text is whole when nothing but a full stop at its end went. No words when no letter and no
    /// digit is left ("@@"), and for a joining word alone.
    /// </summary>
    private static (string Words, bool Whole) Read(string text)
    {
        var given = text.Trim().TrimEnd('.', ' ');

        // A backslash first is the middle of a path ("\tasks"): the name after it is what is read.
        var words = Closed(given).Trim(Edges).TrimStart('\\');
        var last = words.LastIndexOf(' ');
        var joining = Joining.Any(word => Same(word, words[(last + 1)..]));
        if (joining && words.IndexOf(' ') < last)
        {
            words = words[..last].Trim(Edges);
        }

        // A joining word alone ("of") tells nothing.
        var nothing = !words.Any(char.IsLetterOrDigit) || (joining && last < 0);
        return nothing ? (string.Empty, false) : (words, !joining && words == given);
    }

    /// <summary>A text without a round or square bracket that opens and never closes, and without what follows it: "2. orchard (" is "2. orchard ".</summary>
    private static string Closed(string text)
    {
        var cut = text.Length;
        foreach (var (open, close) in new[] { ('(', ')'), ('[', ']') })
        {
            var at = text.LastIndexOf(open);
            if (at >= 0 && text.IndexOf(close, at) < 0)
            {
                cut = Math.Min(cut, at);
            }
        }

        return text[..cut];
    }

    /// <summary>An expectation's text without its mark, as <see cref="Window.Wanted"/> reads the marks: "!" alone asks for any interrupting announcement and leaves no words.</summary>
    private static string Unmarked(string text) =>
        text == "!" ? string.Empty
        : text.StartsWith('♪') ? text.TrimStart('♪', ' ')
        : text.StartsWith("! ", StringComparison.Ordinal) || text.StartsWith("→ ", StringComparison.Ordinal) || text.StartsWith("· ", StringComparison.Ordinal) ? text[2..]
        : text;

    /// <summary>
    /// A text without the run's folder and with its escapes filled in (WF-7.7): <c>{temp}\</c> is dropped, a
    /// <c>{temp}</c> left is "the test folder", and each <c>\uXXXX</c> is its character, as the engine fills a line
    /// (<see cref="ScriptLines.Filled"/>).
    /// </summary>
    private static string Filled(string text) => ScriptLines.Filled(text.Replace(TempFolder, string.Empty, StringComparison.Ordinal), TestFolder);

    /// <summary>A sentence ended by a full stop; one that ends with a full stop, a question mark, an exclamation mark or an ellipsis already has its end.</summary>
    private static string Stopped(string sentence)
    {
        var text = sentence.TrimEnd();
        return text.Length > 0 && text[^1] is '.' or '?' or '!' or '…' ? text : text + ".";
    }

    /// <summary>The app a line of the script reaches (lines from 1), or null where <see cref="ScriptApps.ByLine"/> could not read it.</summary>
    private static string? AppAt(IReadOnlyList<string?> apps, int line) => line >= 1 && line <= apps.Count ? apps[line - 1] : null;

    private static string Slashed(string path) => path.Replace('\\', '/');

    private static bool Same(string one, string other) => string.Equals(one, other, StringComparison.OrdinalIgnoreCase);
}
