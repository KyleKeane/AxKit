using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The table of cases, <c>docs/axKit/cases.json</c> (docs/axKit/SPEC.md AX-5.10): how each unexpected situation is
/// handled, one row per case, read by people as <c>docs/axKit/cases.md</c> (tools/cases.ps1) and held here to what the
/// repository has. The file is written as <see cref="Write"/> writes it; every thing and situation is of the file's
/// two closed lists; every proof names a test method or a window script's goal or check that exists, and a row without
/// a proof says what is owed; every constant named under <c>words</c> exists and its text is in what the row says; and
/// every settings file of <see cref="AppPaths"/> and every settings class has a row. Read off the sources, as
/// <see cref="NotesTests"/> is: a removed test or a reworded sentence fails here with the row's id.
/// </summary>
public sealed class CasesTests
{
    private const string Format = "axkit-cases 1";
    private const string SettingsFileThing = "a settings file";
    private const string SealedSettingThing = "a sealed setting";
    private const string All = "all";

    /// <summary>One row; <see cref="Words"/>, <see cref="Owed"/> and <see cref="File"/> are empty when the row has none.</summary>
    private sealed record Case(string Id, string Thing, IReadOnlyList<string> Apps, string Situation, string Does, string Says,
        IReadOnlyList<string> Words, string Proof, string Owed, string Source, string File);

    private sealed record Table(string Format, IReadOnlyList<string> Things, IReadOnlyList<string> Situations, IReadOnlyList<Case> Rows);

    [Fact]
    public void The_file_is_well_formed_and_written_as_its_writer_writes_it()
    {
        var text = File.ReadAllText(CasesPath()).Replace("\r\n", "\n", StringComparison.Ordinal);
        var table = Read(text);

        Assert.Equal(Format, table.Format);
        Assert.NotEmpty(table.Rows);

        // The ids count up from C-1 without a gap, so a row is found by its number and none is used twice.
        Assert.Equal(Enumerable.Range(1, table.Rows.Count).Select(number => "C-" + number), table.Rows.Select(row => row.Id));

        // One order of the members, one row a line: the file is exactly what the writer gives, line ends aside.
        Assert.Equal(Write(table), text);
    }

    [Fact]
    public void Every_thing_situation_and_app_is_of_its_closed_list_and_no_sentence_is_empty()
    {
        var table = Read(File.ReadAllText(CasesPath()));
        var apps = Dispatch.Apps.Select(app => app.Name).ToHashSet(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (var row in table.Rows)
        {
            if (!table.Things.Contains(row.Thing))
            {
                problems.Add($"{row.Id}: the thing '{row.Thing}' is not in the file's list of things");
            }

            if (!table.Situations.Contains(row.Situation))
            {
                problems.Add($"{row.Id}: the situation '{row.Situation}' is not in the file's list of situations");
            }

            var everyApp = row.Apps.Count == 1 && row.Apps[0] == All;
            if (row.Apps.Count == 0 || (!everyApp && row.Apps.Any(app => !apps.Contains(app))))
            {
                problems.Add($"{row.Id}: apps is neither [\"{All}\"] nor a list of app names as Dispatch.Apps has them");
            }

            if (row.Does.Length == 0 || row.Says.Length == 0 || row.Source.Length == 0)
            {
                problems.Add($"{row.Id}: does, says and source each hold a text (says is \"nothing\" with the reason when nothing is said)");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void Every_proof_names_a_test_or_a_script_that_exists_and_a_row_without_one_says_what_is_owed()
    {
        var root = SourceTexts.RepositoryRoot();
        var problems = new List<string>();
        foreach (var row in Read(File.ReadAllText(CasesPath())).Rows)
        {
            if (row.Proof.Length == 0)
            {
                if (row.Owed.Length == 0)
                {
                    problems.Add($"{row.Id}: no proof and no \"owed\": name the test or the script, or say what would prove it");
                }
            }
            else if (row.Owed.Length > 0)
            {
                problems.Add($"{row.Id}: a row with a proof owes nothing: take \"owed\" away");
            }
            else if (ProofProblem(root, row.Proof) is { } problem)
            {
                problems.Add($"{row.Id}: {problem}");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void Every_constant_named_under_words_exists_and_its_text_is_in_what_the_row_says()
    {
        var sources = Sources(Path.Combine(SourceTexts.RepositoryRoot(), "src"));
        var problems = new List<string>();
        foreach (var row in Read(File.ReadAllText(CasesPath())).Rows)
        {
            foreach (var name in row.Words)
            {
                var parts = name.Split('.');
                var text = parts.Length == 2 ? ConstantText(sources, parts[0], parts[1]) : null;
                if (text is null)
                {
                    problems.Add($"{row.Id}: words names {name}, and no class of src has a public const string of that name");
                }
                else if (!row.Says.Contains(text, StringComparison.Ordinal))
                {
                    problems.Add($"{row.Id}: says does not hold the text of {name}, which is now: {text}");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void Every_settings_file_and_every_settings_class_has_a_row()
    {
        var root = SourceTexts.RepositoryRoot();
        var rows = Read(File.ReadAllText(CasesPath())).Rows;
        var problems = new List<string>();

        // The bundle's own files in the settings folder: each member of AppPaths that is one file under
        // %APPDATA%\AxKit has a row that names it, about the file or about its sealed settings.
        var paths = File.ReadAllText(Path.Combine(root, "src", "AxKit.Core", "AppPaths.cs"));
        var files = Regex.Matches(paths, @"public static string (\w+File) \{ get; \} =\s*Path\.Combine\(Home\(Environment\.SpecialFolder\.ApplicationData\)")
            .Select(match => "AppPaths." + match.Groups[1].Value)
            .ToList();
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            if (!rows.Any(row => row.File == file && row.Thing is SettingsFileThing or SealedSettingThing))
            {
                problems.Add($"{file}: no row of a settings file or a sealed setting names it under \"file\"");
            }
        }

        foreach (var row in rows.Where(row => row.File.Length > 0 && !files.Contains(row.File)))
        {
            problems.Add($"{row.Id}: file names {row.File}, which is no settings file of AppPaths");
        }

        // The apps' settings classes, WindowSettings and what derives from it: each concrete one has a row of a
        // settings file for its app, the app read from its AppPaths.SettingsFile("...") line; a class without that
        // line serves several apps (the editors') and needs a row for all.
        var classes = SettingsClasses(Sources(Path.Combine(root, "src")));
        Assert.NotEmpty(classes);
        var apps = Dispatch.Apps.Select(app => app.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var (name, app) in classes)
        {
            if (app is not null && !apps.Contains(app))
            {
                problems.Add($"{name}: its settings file is named after {app}, which is no app of Dispatch.Apps");
            }

            if (!rows.Any(row => row.Thing == SettingsFileThing && row.File.Length == 0 && (row.Apps.Contains(All) || (app is not null && row.Apps.Contains(app)))))
            {
                problems.Add($"{name}: no row of a settings file is for {app ?? "every app"}");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    /// <summary>The concrete classes that derive from WindowSettings, each with the app its settings file is named after, or null.</summary>
    private static List<(string Name, string? App)> SettingsClasses(IReadOnlyList<string> sources)
    {
        var found = new List<(string Name, string Base, bool IsAbstract, string? App)>();
        foreach (var source in sources)
        {
            foreach (Match match in Regex.Matches(source, @"public (sealed |abstract )?class (\w+)\s*(?:\([^)]*\))?\s*:\s*(\w+)"))
            {
                var app = Regex.Match(source, @"AppPaths\.SettingsFile\(""(\w+)""\)");
                found.Add((match.Groups[2].Value, match.Groups[3].Value, match.Groups[1].Value == "abstract ", app.Success ? app.Groups[1].Value : null));
            }
        }

        var bases = new HashSet<string>(StringComparer.Ordinal) { "WindowSettings" };
        var result = new List<(string Name, string? App)>();
        for (var grew = true; grew;)
        {
            grew = false;
            foreach (var item in found.Where(item => bases.Contains(item.Base) && !bases.Contains(item.Name)))
            {
                bases.Add(item.Name);
                grew = true;
                if (!item.IsAbstract)
                {
                    result.Add((item.Name, item.App));
                }
            }
        }

        return result;
    }

    /// <summary>Why a proof names nothing that exists, or null: <c>Project/Class.Method</c>, or <c>tests/scripts/app/file: the goal's or check's title</c>.</summary>
    private static string? ProofProblem(string root, string proof)
    {
        if (proof.StartsWith("tests/scripts/", StringComparison.Ordinal))
        {
            var colon = proof.IndexOf(": ", StringComparison.Ordinal);
            if (colon < 0)
            {
                return $"the proof '{proof}' names a script without \": \" and the title of its goal or check";
            }

            var script = Path.Combine(root, proof[..colon].Replace('/', Path.DirectorySeparatorChar));
            var title = proof[(colon + 2)..];
            if (!File.Exists(script))
            {
                return $"the script {proof[..colon]} does not exist";
            }

            return File.ReadLines(script).Any(line => line.TrimEnd() == "# Goal: " + title || line.TrimEnd() == "# Check: " + title)
                ? null
                : $"the script {proof[..colon]} has no line '# Goal: {title}' or '# Check: {title}'";
        }

        var match = Regex.Match(proof, @"^([\w.]+)/(\w+)\.(\w+)$");
        if (!match.Success)
        {
            return $"the proof '{proof}' is neither Project/Class.Method nor tests/scripts/<app>/<file>: <title>";
        }

        var project = Path.Combine(root, "tests", match.Groups[1].Value);
        if (!Directory.Exists(project))
        {
            return $"the test project tests/{match.Groups[1].Value} does not exist";
        }

        var ofClass = new Regex(@"\bclass " + match.Groups[2].Value + @"\b");
        var method = new Regex(@"\b(void|Task) " + match.Groups[3].Value + @"\s*\(");
        return Sources(project).Any(source => ofClass.IsMatch(source) && method.IsMatch(source))
            ? null
            : $"no class {match.Groups[2].Value} of tests/{match.Groups[1].Value} has a method {match.Groups[3].Value}";
    }

    /// <summary>The text of a <c>public const string</c> of a class of the sources, or null when there is none.</summary>
    private static string? ConstantText(IReadOnlyList<string> sources, string type, string member)
    {
        var ofClass = new Regex(@"\bclass " + type + @"\b");
        var constant = new Regex(@"public const string " + member + @" = ""((?:[^""\\]|\\.)*)"";");
        foreach (var source in sources.Where(source => ofClass.IsMatch(source)))
        {
            if (constant.Match(source) is { Success: true } match)
            {
                return Regex.Unescape(match.Groups[1].Value);
            }
        }

        return null;
    }

    /// <summary>The text of every C# file under a folder, the build's folders left out.</summary>
    private static List<string> Sources(string folder) =>
        Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .ToList();

    private static Table Read(string text)
    {
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        var rows = new List<Case>();
        foreach (var row in root.GetProperty("rows").EnumerateArray())
        {
            rows.Add(new Case(
                row.GetProperty("id").GetString()!,
                row.GetProperty("thing").GetString()!,
                Strings(row.GetProperty("apps")),
                row.GetProperty("situation").GetString()!,
                row.GetProperty("does").GetString()!,
                row.GetProperty("says").GetString()!,
                row.TryGetProperty("words", out var words) ? Strings(words) : [],
                row.GetProperty("proof").GetString()!,
                row.TryGetProperty("owed", out var owed) ? owed.GetString()! : string.Empty,
                row.GetProperty("source").GetString()!,
                row.TryGetProperty("file", out var file) ? file.GetString()! : string.Empty));
        }

        return new Table(root.GetProperty("format").GetString()!, Strings(root.GetProperty("things")), Strings(root.GetProperty("situations")), rows);
    }

    private static List<string> Strings(JsonElement list) => list.EnumerateArray().Select(item => item.GetString()!).ToList();

    /// <summary>
    /// The file as it is written: the format, the two closed lists, then one row a line with its members in one
    /// order; words, owed and file only where the row has them. Line ends are LF here; the file's own are the
    /// checkout's.
    /// </summary>
    private static string Write(Table table)
    {
        var text = new StringBuilder();
        text.Append("{\n");
        text.Append("  \"format\": ").Append(Quoted(table.Format)).Append(",\n");
        text.Append("  \"things\": ").Append(Quoted(table.Things)).Append(",\n");
        text.Append("  \"situations\": ").Append(Quoted(table.Situations)).Append(",\n");
        text.Append("  \"rows\": [\n");
        for (var index = 0; index < table.Rows.Count; index++)
        {
            var row = table.Rows[index];
            text.Append("    {\"id\": ").Append(Quoted(row.Id));
            text.Append(", \"thing\": ").Append(Quoted(row.Thing));
            text.Append(", \"apps\": ").Append(Quoted(row.Apps));
            text.Append(", \"situation\": ").Append(Quoted(row.Situation));
            text.Append(", \"does\": ").Append(Quoted(row.Does));
            text.Append(", \"says\": ").Append(Quoted(row.Says));
            if (row.Words.Count > 0)
            {
                text.Append(", \"words\": ").Append(Quoted(row.Words));
            }

            text.Append(", \"proof\": ").Append(Quoted(row.Proof));
            if (row.Owed.Length > 0)
            {
                text.Append(", \"owed\": ").Append(Quoted(row.Owed));
            }

            text.Append(", \"source\": ").Append(Quoted(row.Source));
            if (row.File.Length > 0)
            {
                text.Append(", \"file\": ").Append(Quoted(row.File));
            }

            text.Append(index == table.Rows.Count - 1 ? "}\n" : "},\n");
        }

        text.Append("  ]\n}\n");
        return text.ToString();
    }

    private static readonly JsonSerializerOptions Plain = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static string Quoted(string text) => JsonSerializer.Serialize(text, Plain);

    private static string Quoted(IReadOnlyList<string> texts) => "[" + string.Join(", ", texts.Select(Quoted)) + "]";

    private static string CasesPath() => Path.Combine(SourceTexts.RepositoryRoot(), "docs", "axKit", "cases.json");
}
