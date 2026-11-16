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
/// a run. The command line writes the pages and compares them (<c>tools/workflows.ps1</c>); tested in
/// <c>tests/AxAutomate.Tests/ReferenceTests</c>.
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

    /// <summary>What a <c>{temp}</c> that stands alone is written as (WF-7.7): no path of a run is on a page.</summary>
    private const string TestFolder = "the test folder";

    /// <summary>What is dropped from the ends of an expectation's text (WF-7.4): the scripts match parts of announcements.</summary>
    private static readonly char[] Edges = [',', ':', '.', ' '];

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
        "line \"You hear\" gives words of what axKit says there, not always the whole sentence. The file names and the words",
        "typed are the check's examples: use your own. Where a step names a command after its key, the key is that command's",
        "in this version of axKit.",
    ];

    private static readonly string[] CommandsPreamble =
    [
        "Written by `tools/workflows.ps1` from `docs/axKit/window-commands.json`, which the automated checks write from the",
        "windows themselves; never edit it by hand. One line a command: its name, its menu, its key, and its mark, what it",
        "does beyond reading (read, open, change, act).",
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
    /// ended by a full stop, an <c>iffound</c> one after "If that came: "; each expectation of speech a line of three
    /// spaces and what is heard, under the input step before it, or before step 1 when none came yet. Every other step
    /// is left out: the set-up, the waits, the checks of state, the refusals. <paramref name="apps"/> is the script's
    /// <see cref="ScriptApps.ByLine"/>, by which a step's command is found among its app's rows of the inventory.
    /// None when the steps hold no input step: a how-to with nothing to do is no how-to (WF-D-28).
    /// </summary>
    public static IReadOnlyList<string> GoalLines(IReadOnlyList<WorkflowStep> steps, IReadOnlyList<string?> apps, IReadOnlyList<InventoryApp> inventory)
    {
        var lines = new List<string>();
        var number = 0;
        foreach (var step in steps.Where(step => !step.IsNote))
        {
            var conditional = step.Then.Where(then => !then.IsNote).ToList();
            if (ScriptReader.IsInput(step.Do))
            {
                number++;
                lines.Add($"{number}. {Stopped(Sentence(step, AppAt(apps, step.Line), inventory))}");
            }
            else if (HeardLine(step, step.OnFailure == WorkflowStep.Continue, conditional.Any(then => ScriptReader.IsInput(then.Do))) is { } heard)
            {
                lines.Add("   " + heard);
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
                    lines.Add($"{number}. If that came: {Stopped(Sentence(then, AppAt(apps, then.Line), inventory))}");
                }
                else if (HeardLine(then, !acted || then.OnFailure == WorkflowStep.Continue, keepShort: false) is { } heard)
                {
                    lines.Add("   " + heard);
                }
            }
        }

        return number == 0 ? [] : lines;
    }

    /// <summary>
    /// An expectation's text as words to read (WF-7.4): without its mark ("! " an interrupting announcement, the marks
    /// of a sound, a focus move and a value, as <see cref="Event.Spoken"/> writes them) and without a leading or
    /// trailing comma, colon, full stop or space; empty when fewer than two words are left, since one word of an
    /// announcement tells a reader nothing ("trust", "checked").
    /// </summary>
    public static string Heard(string text) => Enough(Unmarked(text).Trim(Edges));

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

    /// <summary>The window steps a workflow starts with, as sentences: "Open axDown with find.md."; empty for none.</summary>
    private static string Starts(IReadOnlyList<WorkflowStep> windowSteps, IReadOnlyList<string?> apps, IReadOnlyList<InventoryApp> inventory) =>
        string.Join(" ", windowSteps.Select(step => Stopped(Sentence(step, AppAt(apps, step.Line), inventory))));

    /// <summary>The index (5.6): its preamble, then for each group where its steps are, and each workflow's label, numbered, with its goals' titles under it.</summary>
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
                list.AddRange(group.Workflows[i].Goals.Select(goal => "   - " + goal.Title));
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

    /// <summary>The commands page (WF-7.6, 5.6): its preamble, then each app of the inventory that has rows, by name, and one line a row in the inventory's order.</summary>
    private static string CommandsPage(IReadOnlyList<InventoryApp> inventory)
    {
        var blocks = new List<IReadOnlyList<string>> { new[] { "# The commands of every window" }, CommandsPreamble };
        foreach (var app in inventory.Where(app => app.Commands.Count > 0).OrderBy(app => app.App, StringComparer.OrdinalIgnoreCase))
        {
            blocks.Add(["## " + app.App]);
            blocks.Add([.. app.Commands.Select(CommandLine)]);
        }

        return Text(blocks);
    }

    /// <summary>
    /// A row's line of the commands page: "- Redo. Menu: Actions. Key: Ctrl+Y. Also: Ctrl+Shift+Z. Mark: change." The
    /// key is the row's key text, the further chords are in key words (<see cref="StepText.Keys"/>), a key with no
    /// menu item says so, and a row the AI tools never run ends with that.
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
            parts.Add($"Also: {StepText.Keys(string.Join(' ', row.Also))}.");
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
    /// An input step's sentence without its full stop (WF-7.3): <c>do</c>, <c>keys</c>, <c>menu</c> and <c>start</c> in
    /// the page's own words, every other step in <see cref="StepText.Sentence"/>'s; then without the run's folder and
    /// with the escapes filled in (<see cref="Filled"/>).
    /// </summary>
    private static string Sentence(WorkflowStep step, string? app, IReadOnlyList<InventoryApp> inventory) => Filled(step.Do switch
    {
        "do" => DoSentence(step, app, inventory),
        "keys" => KeysSentence(step, app, inventory),
        "menu" => MenuSentence(step, app, inventory),
        "start" => StartSentence(step),
        _ => StepText.Sentence(step),
    });

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
        var label = CommandNames.Plain(row.Label);
        var menus = Menus(row.Menu);
        var key = PressedKey(row);
        if (key.Length > 0)
        {
            return menus.Length > 0 ? $"Press {key} ({label}; {menus})" : $"Press {key} ({label})";
        }

        return menus.Length > 0 ? $"Choose {label} in the {menus}" : null;
    }

    /// <summary>
    /// The step <c>keys</c>: "Press Ctrl+F", the chords in key words. A line of one chord that carries Ctrl or Alt or
    /// is a function key (<see cref="KeyChord.IsCommandChord"/>) and that exactly one row of the app holds names that
    /// command, "Press Ctrl+G (Go to line)": a hint from the inventory, since the script's state is not known here.
    /// A plain key names none: Enter, Backspace or a letter is a field's or a list's own key far more often than the
    /// command the inventory has for it, and "Press Backspace (Up one level)" in a field would be wrong.
    /// </summary>
    private static string KeysSentence(WorkflowStep step, string? app, IReadOnlyList<InventoryApp> inventory)
    {
        var sentence = StepText.Sentence(step);
        var presses = step.Value("keys").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return presses.Length == 1 && KeyChord.IsCommandChord(presses[0]) && CommandInventory.OfChord(inventory, app, presses[0]) is { } row
            ? $"{sentence} ({NameAmong(CommandInventory.Of(inventory, app ?? string.Empty), row)})"
            : sentence;
    }

    /// <summary>
    /// The step <c>menu</c>: a text that exactly one row of the app answers (<see cref="CommandInventory.OfMenuText"/>)
    /// reads with that row's menus, and its key when it has one, "Choose Larger text in the Options menu, Text (key:
    /// Ctrl+Plus)": as the choice it is, since the check chose the item and did not press the key. Otherwise the
    /// step's own words, "Choose the menu item &lt;text&gt;".
    /// </summary>
    private static string MenuSentence(WorkflowStep step, string? app, IReadOnlyList<InventoryApp> inventory)
    {
        if (CommandInventory.OfMenuText(inventory, app, step.Value("name")) is not { } row || Menus(row.Menu) is not { Length: > 0 } menus)
        {
            return StepText.Sentence(step);
        }

        var key = ShownKey(row);
        return $"Choose {CommandNames.Plain(row.Label)} in the {menus}" + (key.Length > 0 ? $" (key: {key})" : string.Empty);
    }

    /// <summary>
    /// The step <c>start</c>: "Open axDown with find.md" for a start whose first word is a verb of
    /// <see cref="Dispatch.Apps"/>, "Open axDown" without arguments, "Open settings.json" for a start by a file alone.
    /// </summary>
    private static string StartSentence(WorkflowStep step)
    {
        var arguments = step.Value("arguments").Trim();
        var space = arguments.IndexOf(' ');
        var first = space < 0 ? arguments : arguments[..space];
        var rest = space < 0 ? string.Empty : arguments[(space + 1)..].Trim();
        if (Dispatch.Apps.FirstOrDefault(app => Same(app.Verb, first)) is not { } named)
        {
            return arguments.Length > 0 ? "Open " + arguments : "Open axKit";
        }

        return rest.Length > 0 ? $"Open {named.Name} with {rest}" : $"Open {named.Name}";
    }

    /// <summary>A row's menus from the bar down, the word "menu" after the first: "Navigation menu", "Navigation menu, Next"; empty for a row without menus.</summary>
    private static string Menus(string menu)
    {
        var plain = CommandNames.Plain(menu);
        var comma = plain.IndexOf(", ", StringComparison.Ordinal);
        return plain.Length == 0 ? string.Empty : comma < 0 ? plain + " menu" : plain[..comma] + " menu" + plain[comma..];
    }

    /// <summary>
    /// The key a <c>do</c> step is read with: the row's key as its menu shows it (the window's own spelling, where it
    /// differs from the chord's words), else the first of its further chords in key words, else, for a key with no
    /// menu item, its key text. Empty for a menu item with no chord, which is chosen and not pressed.
    /// </summary>
    private static string PressedKey(ListedCommand row) =>
        row.Key.Length > 0 ? (row.KeyText.Length > 0 ? row.KeyText : StepText.Keys(row.Key))
        : row.Also.Count > 0 ? StepText.Keys(row.Also[0])
        : row.KeyOnly ? row.KeyText
        : string.Empty;

    /// <summary>The key a menu item is named with after a <c>menu</c> step: its key text as the menu shows it, else its first chord in key words; empty for an item without a key.</summary>
    private static string ShownKey(ListedCommand row) =>
        row.KeyText.Length > 0 ? row.KeyText
        : row.Key.Length > 0 ? StepText.Keys(row.Key)
        : row.Also.Count > 0 ? StepText.Keys(row.Also[0])
        : string.Empty;

    /// <summary>A command's canonical name among its app's rows (WF-1.1): its label, and where several menu items of the app share the label, its whole line, "Heading, Navigation, Next".</summary>
    private static string NameAmong(IReadOnlyList<ListedCommand> rows, ListedCommand row)
    {
        var label = CommandNames.Plain(row.Label);
        var shared = rows.Count(other => !other.KeyOnly && Same(CommandNames.Plain(other.Label), label)) > 1;
        return shared ? CommandNames.Line(new CommandName(row.Label, row.Menu)) : label;
    }

    /// <summary>
    /// The line of an expectation of speech (WF-7.4), ended by a full stop, or null for a step that is none or leaves
    /// nothing to read: <c>expect said</c> "You hear: ...", a sound "You hear the sound Edge.", a focus move "The focus
    /// moves to ...", a value "It reads ..."; <c>expect spoken</c> "NVDA says: ..."; <c>expect mcp</c> "The tool
    /// answers: ...". With <paramref name="may"/> (a <c>try said</c>, which may not come) "You may hear: ...". The
    /// name of a sound, a control or a value is whole in one word; words of an announcement need two
    /// (<see cref="Heard"/>), except with <paramref name="keepShort"/>: the words of a try that steps hang on stay,
    /// since "If that came" under it must have what came.
    /// </summary>
    private static string? HeardLine(WorkflowStep step, bool may, bool keepShort)
    {
        var text = Filled(step.Value("text"));
        switch (step.Do)
        {
            case "expect spoken":
                return Line("NVDA says: ", Enough(text.Trim(Edges)));
            case "expect mcp":
                return Line("The tool answers: ", Enough(text.Trim(Edges)));
            case "expect said":
                break;
            default:
                return null;
        }

        var words = Unmarked(text).Trim(Edges);
        if (text.StartsWith('♪'))
        {
            return Line(may ? "You may hear the sound " : "You hear the sound ", words);
        }

        if (text.StartsWith("→ ", StringComparison.Ordinal))
        {
            return Line(may ? "The focus may move to " : "The focus moves to ", words);
        }

        if (text.StartsWith("· ", StringComparison.Ordinal))
        {
            return Line(may ? "It may read " : "It reads ", words);
        }

        return Line(may ? "You may hear: " : "You hear: ", keepShort ? words : Enough(words));
    }

    /// <summary>What is heard after its opening words, ended by a full stop; null when no words are left.</summary>
    private static string? Line(string opening, string words) => words.Length > 0 ? Stopped(opening + words) : null;

    /// <summary>An expectation's text without its mark, as <see cref="Window.Wanted"/> reads the marks: "!" alone asks for any interrupting announcement and leaves no words.</summary>
    private static string Unmarked(string text) =>
        text == "!" ? string.Empty
        : text.StartsWith('♪') ? text.TrimStart('♪', ' ')
        : text.StartsWith("! ", StringComparison.Ordinal) || text.StartsWith("→ ", StringComparison.Ordinal) || text.StartsWith("· ", StringComparison.Ordinal) ? text[2..]
        : text;

    /// <summary>The words when there are two or more of them, else nothing (WF-7.4).</summary>
    private static string Enough(string words) => words.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2 ? string.Empty : words;

    /// <summary>
    /// A text without the run's folder and with its escapes filled in (WF-7.7): <c>{temp}\</c> is dropped, a
    /// <c>{temp}</c> left is "the test folder", and each <c>\uXXXX</c> is its character, as the engine fills a line
    /// (<see cref="ScriptLines.Filled"/>).
    /// </summary>
    private static string Filled(string text) => ScriptLines.Filled(text.Replace(@"{temp}\", string.Empty, StringComparison.Ordinal), TestFolder);

    /// <summary>A sentence ended by a full stop; one that ends with a full stop, a question mark or an exclamation mark already has its end.</summary>
    private static string Stopped(string sentence)
    {
        var text = sentence.TrimEnd();
        return text.Length > 0 && text[^1] is '.' or '?' or '!' ? text : text + ".";
    }

    /// <summary>The app a line of the script reaches (lines from 1), or null where <see cref="ScriptApps.ByLine"/> could not read it.</summary>
    private static string? AppAt(IReadOnlyList<string?> apps, int line) => line >= 1 && line <= apps.Count ? apps[line - 1] : null;

    private static string Slashed(string path) => path.Replace('\\', '/');

    private static bool Same(string one, string other) => string.Equals(one, other, StringComparison.OrdinalIgnoreCase);
}
