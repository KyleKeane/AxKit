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

    /// <summary>
    /// The item window's fields (L3 of the review's fourth pass) that hold what a database showed: the rows and their
    /// marks, the detail shown, which level they were of and whether the detail was asked for; set back by the lock's
    /// filling of the empty level, and the selection's marks by <c>ForgetDatabase</c>.
    /// </summary>
    private static readonly string[] ItemFormOfTheDatabase = ["_rows", "_markRows", "_shown", "_lines", "_detailStale", "_detailLast", "_selectionLevel", "_filterLevel", "_unfiltered", "_selectionMade"];

    /// <summary>The item window's own fields, which outlive a database: its controls, its menus, the filter typed and the order chosen, the work running.</summary>
    private static readonly string[] ItemFormOfTheWindow = ["_split", "_status", "_nameLabel", "_placeLabel", "_listMenu", "_rowMenu", "_fieldMenu", "_context", "_menuOf", "_filter", "_sortItems", "_sortChoice", "_working", "_reads", "_reading"];

    private static List<string> FieldsOf(Type type) =>
        [.. type.GetFields(Own).Where(field => !field.Name.Contains('<', StringComparison.Ordinal)).Select(field => field.Name).Order(StringComparer.Ordinal)];

    /// <summary>Every field of the window, and of the item window under it (L3 of the fourth pass), is placed: of the database, or of the window. A new field fails here until it is placed, and one of the database is forgotten with the rest.</summary>
    [Fact]
    public void Every_field_of_the_window_is_placed_as_the_databases_or_the_windows()
    {
        Assert.Equal(OfTheDatabase.Concat(OfTheWindow).Order(StringComparer.Ordinal), FieldsOf(typeof(AxPass.PassForm)));
        Assert.Equal(ItemFormOfTheDatabase.Concat(ItemFormOfTheWindow).Order(StringComparer.Ordinal), FieldsOf(typeof(AxKit.Forms.ItemForm)));
    }

    /// <summary>A real window made on a thread of its own, as a window's thread is, and never shown; its failure is the test's.</summary>
    private static void OnWindowThread(Action<AxPass.PassForm> test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var window = new AxPass.PassForm(null, new PassSettings(), null);
                test(window);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    /// <summary>
    /// The lock forgets the database whole, run for real (L3 of the fourth pass; until then on an object that was
    /// never made, and the item window's state not at all): a window is made, given the state of an open database
    /// (its fields, a selected entry, the rows and an entry's detail shown), and locked; afterwards each field is
    /// what it is with none open, nothing is selected, and no row or detail of the database is held. Lock, opening
    /// another database and an unlock that is made all go through the one place.
    /// </summary>
    [Fact]
    public void Locking_a_window_forgets_every_part_of_the_database()
    {
        OnWindowThread(window =>
        {
            var type = typeof(AxPass.PassForm);
            var item = typeof(AxKit.Forms.ItemForm);
            void Set(Type owner, string name, object? value) => owner.GetField(name, Own)!.SetValue(window, value);
            object? Get(Type owner, string name) => owner.GetField(name, Own)!.GetValue(window);

            var undo = type.GetField("_undo", Own)!;
            var steps = Get(type, "_undo")!;
            var stepType = undo.FieldType.GetGenericArguments()[0];
            undo.FieldType.GetMethod("Add")!.Invoke(steps, [System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(stepType)]);
            Set(type, "_database", @"C:\nowhere\test.kdbx");
            Set(type, "_password", "test-master-1");
            Set(type, "_tree", PassTree.Parse("Email/\r\nEmail/Gmail\r\n"));
            Set(type, "_group", "Email");
            Set(type, "_backedUp", true);
            Set(type, "_binOn", true);
            Set(type, "_binSeen", "Recycle Bin");
            ((Dictionary<string, AxKit.Core.ItemDocument>)Get(type, "_entries")!)["Email/Gmail"] = AxKit.Core.ItemDocument.Of([]);
            ((HashSet<string>)Get(type, "_reading")!).Add("Email/Gmail");

            var gmail = new AxKit.Forms.ItemRow(IsList: false, "Email/Gmail", "Gmail", "Gmail");
            Set(item, "_rows", new List<AxKit.Forms.ItemRow> { gmail });
            Set(item, "_markRows", (IReadOnlyList<AxKit.Core.MarkRow>)[new AxKit.Core.MarkRow("Email/Gmail", "Gmail")]);
            var line = new AxKit.Forms.ItemDetailLine("User name: someone");
            Set(item, "_shown", new AxKit.Forms.ItemDetail("Gmail", "Email/Gmail", null, [line]));
            Set(item, "_lines", new List<(int Field, AxKit.Forms.ItemDetailLine Line)> { (-1, line) });
            var selection = (AxKit.Forms.ListSelection)item.GetProperty("Selection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(window)!;
            var marks = (AxKit.Core.Marks)typeof(AxKit.Forms.ListSelection).GetField("_marks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(selection)!;
            marks.All([new AxKit.Core.MarkRow("Email/Gmail", "Gmail")]);
            Assert.Equal(1, selection.Count);

            // The one place alone: the database's fields and the selection (without its Selection.Clear the
            // selection stays here, tried; the lock's filling below would clear it again, and hid that).
            type.GetMethod("ForgetDatabase", Own)!.Invoke(window, null);
            Assert.Equal(0, selection.Count);
            Assert.Null(Get(type, "_password"));

            // Then the lock as the user makes it, the window holding the database again.
            Set(type, "_password", "test-master-1");
            marks.All([new AxKit.Core.MarkRow("Email/Gmail", "Gmail")]);
            type.GetMethod("Lock", Own)!.Invoke(window, null);

            Assert.Null(Get(type, "_password"));
            Assert.Null(Get(type, "_tree"));
            Assert.Equal(string.Empty, Get(type, "_group"));
            Assert.Equal(false, Get(type, "_backedUp"));
            Assert.Equal(false, Get(type, "_binOn"));
            Assert.Null(Get(type, "_binSeen"));
            Assert.Empty((System.Collections.IEnumerable)Get(type, "_entries")!);
            Assert.Empty((System.Collections.IEnumerable)Get(type, "_reading")!);
            Assert.Empty((System.Collections.IEnumerable)Get(type, "_undo")!);
            Assert.Equal(0, selection.Count);
            Assert.DoesNotContain(gmail, (IEnumerable<AxKit.Forms.ItemRow>)Get(item, "_rows")!);
            Assert.Empty((IEnumerable<AxKit.Core.MarkRow>)Get(item, "_markRows")!);
            Assert.NotEqual("Gmail", ((AxKit.Forms.ItemDetail)Get(item, "_shown")!).Name);
            Assert.DoesNotContain((List<(int Field, AxKit.Forms.ItemDetailLine Line)>)Get(item, "_lines")!, one => one.Line == line);
        });
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
