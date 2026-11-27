using AxPass.Core;

namespace AxPass.Tests;

/// <summary>
/// Nothing of a database reaches the window after the lock (docs/axPass/SPEC.md PW-4.3). The window waits for
/// KeePassXC's tool and for a backup's copy in one way, <c>PassForm.Tool</c>, which gives the answer only while
/// the window is as it was when the work was asked for: not locked since, not unlocked again, not pointed at
/// another database. The review's second pass of 2026-11-27 found the comparison written by hand in three places
/// and missing in two (the unlock itself, the new database). Read off the sources, as
/// <see cref="KeePassCliTextsTests"/> reads them: no wait of the window goes around that one way.
/// </summary>
public sealed class PassWindowTests
{
    private static IEnumerable<(string File, int Number, string Line)> Lines()
    {
        var folder = Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxPass");
        foreach (var file in Directory.GetFiles(folder, "*.cs"))
        {
            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (!line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    yield return (Path.GetFileName(file), number, line);
                }
            }
        }
    }

    [Fact]
    public void The_window_starts_work_off_its_thread_in_one_place()
    {
        var starts = Lines().Where(line => line.Line.Contains("Task.Run(", StringComparison.Ordinal)).ToList();
        var one = Assert.Single(starts);
        Assert.Equal("PassForm.cs", one.File);
        Assert.Equal("var result = await Task.Run(work);", one.Line.Trim());

        string[] otherWays = ["ThreadPool.", "new Thread(", "Task.Factory", ".ContinueWith(", "BackgroundWorker", ".ConfigureAwait(false)"];
        var offenders = Lines().Where(line => otherWays.Any(way => line.Line.Contains(way, StringComparison.Ordinal))).Select(line => $"{line.File}:{line.Number}").ToList();
        Assert.True(offenders.Count == 0, "Work off the window's thread started another way than PassForm.Tool, which is what drops an answer that comes after a lock (PW-4.3): " + string.Join(", ", offenders));
    }

    [Fact]
    public void Every_run_of_the_tool_and_every_backup_is_waited_for_through_the_one_way()
    {
        var offenders = Lines()
            .Where(line => line.Line.Contains("KeePassCli.Run(", StringComparison.Ordinal) || line.Line.Contains("Backups.Make(", StringComparison.Ordinal))
            .Where(line => !line.Line.Contains("await Tool(() => ", StringComparison.Ordinal))
            .Select(line => $"{line.File}:{line.Number}: {line.Line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0, "KeePassXC's tool run, or a backup made, outside PassForm.Tool: its answer could reach the window after a lock (PW-4.3): " + string.Join("; ", offenders));
    }

    // ---- M6 (the review's third pass of 2026-11-27): what belongs to a database is forgotten in one place ----

    /// <summary>The window's fields that belong to the database open: each is set back by <c>PassForm.ForgetDatabaseState</c>.</summary>
    private static readonly string[] OfTheDatabase = ["_password", "_tree", "_group", "_backedUp", "_binOn", "_binSeen", "_entries", "_reading", "_undo"];

    /// <summary>The window's own fields, which outlive a database: the settings, the menus, the timers, which database, the counts, the work running.</summary>
    private static readonly string[] OfTheWindow = ["_settings", "_menu", "_groupMenu", "_entryMenu", "_fieldMenu", "_lockTimer", "_clipboardTimer", "_clipboardSequence", "_database", "_busy", "_unlock", "_unlocksAsked", "_locks"];

    private const System.Reflection.BindingFlags Own = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly;

    /// <summary>Every field of the window is placed: of the database, or of the window. A new field fails here until it is placed, and one of the database is forgotten with the rest.</summary>
    [Fact]
    public void Every_field_of_the_window_is_placed_as_the_databases_or_the_windows()
    {
        var fields = typeof(AxPass.PassForm).GetFields(Own)
            .Where(field => !field.Name.Contains('<', StringComparison.Ordinal))
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.Equal(OfTheDatabase.Concat(OfTheWindow).Order(StringComparer.Ordinal), fields);
    }

    /// <summary>
    /// The one place sets every field of the database back, run for real on a window that was never made (no
    /// control is created, and nothing of it is finalized): each field is given a value of an open database, and
    /// after the forgetting each is what it is with none open. Lock, opening another database and an unlock that is
    /// made all go through it (until then a new database made from an unlocked window kept the other's undo steps).
    /// </summary>
    [Fact]
    public void Forgetting_the_database_sets_every_one_of_its_fields_back()
    {
        var type = typeof(AxPass.PassForm);
        var window = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
        GC.SuppressFinalize(window);
        void Set(string name, object? value) => type.GetField(name, Own)!.SetValue(window, value);
        object? Get(string name) => type.GetField(name, Own)!.GetValue(window);

        var undo = type.GetField("_undo", Own)!;
        var steps = Activator.CreateInstance(undo.FieldType)!;
        var stepType = undo.FieldType.GetGenericArguments()[0];
        undo.FieldType.GetMethod("Add")!.Invoke(steps, [System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(stepType)]);
        Set("_undo", steps);
        Set("_password", "test-master-1");
        Set("_tree", PassTree.Parse("Email/\r\nEmail/Gmail\r\n"));
        Set("_group", "Email");
        Set("_backedUp", true);
        Set("_binOn", true);
        Set("_binSeen", "Recycle Bin");
        Set("_entries", new Dictionary<string, AxKit.Core.ItemDocument>(StringComparer.Ordinal) { ["Email/Gmail"] = AxKit.Core.ItemDocument.Of([]) });
        Set("_reading", new HashSet<string>(StringComparer.Ordinal) { "Email/Gmail" });

        type.GetMethod("ForgetDatabaseState", Own)!.Invoke(window, null);

        Assert.Null(Get("_password"));
        Assert.Null(Get("_tree"));
        Assert.Equal(string.Empty, Get("_group"));
        Assert.Equal(false, Get("_backedUp"));
        Assert.Equal(false, Get("_binOn"));
        Assert.Null(Get("_binSeen"));
        Assert.Empty((System.Collections.IEnumerable)Get("_entries")!);
        Assert.Empty((System.Collections.IEnumerable)Get("_reading")!);
        Assert.Empty((System.Collections.IEnumerable)Get("_undo")!);
    }

    /// <summary>The three ways a database goes from the window each forget it whole, through the one place; the selection and a copied password with it.</summary>
    [Fact]
    public void Lock_another_database_and_an_unlock_forget_the_database_in_the_one_place()
    {
        var text = string.Join("\n", Lines().Select(line => line.Line));
        Assert.True(Lines().Count(line => line.Line.Trim() == "ForgetDatabase();") >= 3, "Lock, OpenDatabase and the made unlock each call ForgetDatabase.");
        Assert.Contains("ForgetDatabaseState();\n        Selection.Clear();\n        ClearCopiedPassword();", text, StringComparison.Ordinal);
        Assert.DoesNotContain(Lines(), line => line.Line.Trim() == "_undo.Clear();" && !line.File.Equals("PassForm.cs", StringComparison.Ordinal));
    }

    /// <summary>The count the one way compares is moved wherever the window's state changes hands: asked for and made unlocks, the locks, Windows' own lock, another database, the close.</summary>
    [Fact]
    public void The_count_moves_at_every_change_of_the_windows_state()
    {
        var moves = Lines().Count(line => line.Line.Trim() == "_unlock++;");
        Assert.True(moves >= 7, $"The unlock count is moved in {moves} places; an unlock asked for, an unlock made, Lock twice (locked and not), Windows' lock, another database and the close are seven.");
    }
}
