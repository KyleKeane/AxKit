using System.Text.Json;
using System.Text.RegularExpressions;
using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Tests;

/// <summary>
/// The framework's file against the sources (docs/axKit/framework.md FW-6.1, FW-7.1, FW-7.3): every app of the bundle
/// is in docs/axKit/components.json once; an app's window is the class the file names and derives from its type's
/// class; only the apps the file says are still to move have a window of their own on the bare window base, so a new
/// window of an app's own fails here; and what the file names (kinds of event, families of keys) exists.
/// </summary>
public sealed class ComponentsTests
{
    /// <summary>
    /// An app's row. <see cref="SecondWindow"/> and <see cref="SecondType"/> are empty but for an app that has a
    /// second whole window of another bundle type, each a start of its own (axTemplate's launcher beside its
    /// browser, docs/axTemplate/SPEC.md TP-D8, the maintainer's answers of 2026-11-23): the file names it, so that a
    /// second window class is still never there by accident.
    /// </summary>
    private sealed record AppRow(string App, string Window, string Type, string ToBe, string SecondWindow, string SecondType);

    private static JsonDocument File() =>
        JsonDocument.Parse(System.IO.File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "docs", "axKit", "components.json")));

    private static List<AppRow> Apps(JsonDocument document) =>
        [.. document.RootElement.GetProperty("apps").EnumerateArray().Select(app => new AppRow(
            app.GetProperty("app").GetString()!, app.GetProperty("window").GetString()!,
            app.GetProperty("type").GetString()!, app.GetProperty("to be").GetString()!,
            app.TryGetProperty("second window", out var second) ? second.GetString()! : string.Empty,
            app.TryGetProperty("second type", out var secondType) ? secondType.GetString()! : string.Empty))];

    /// <summary>
    /// The classes an app's window of a type may derive from: the type's class, and for a type whose window can stand
    /// over a program in a console the class that adds the program to it ("with a program": the conversation window's
    /// <c>ConsoleForm</c> over <c>ConversationForm</c>, framework step 9).
    /// </summary>
    private static Dictionary<string, string[]> TypeClasses(JsonDocument document) =>
        document.RootElement.GetProperty("window types").EnumerateArray()
            .ToDictionary(type => type.GetProperty("id").GetString()!, type => type.TryGetProperty("with a program", out var program)
                ? new[] { type.GetProperty("class").GetString()!, program.GetString()! }
                : [type.GetProperty("class").GetString()!]);

    /// <summary>Every "class X : Base" of an app's project, by class name; the shared projects (AxKit, AxKit.Core, AxKit.Forms) are not apps.</summary>
    private static Dictionary<string, string> WindowClassesOf(string project)
    {
        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        var folder = Path.Combine(SourceTexts.RepositoryRoot(), "src", project);
        foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
        {
            foreach (Match match in Regex.Matches(System.IO.File.ReadAllText(file), @"class (\w+) : (AxKitForm|ConversationForm|ConsoleForm|TextEditorForm|ItemForm|HotkeyForm|Form)\b"))
            {
                found[match.Groups[1].Value] = match.Groups[2].Value;
            }
        }

        return found;
    }

    [Fact]
    public void Every_app_of_the_bundle_is_in_the_file_once()
    {
        using var document = File();
        var file = Apps(document).Select(app => app.App).ToList();
        Assert.Equal(file.Count, file.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(Dispatch.Apps.Select(app => app.Name).Order(StringComparer.Ordinal), file.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void An_apps_window_is_the_class_the_file_names_and_derives_from_its_types_class()
    {
        using var document = File();
        var types = TypeClasses(document);
        var problems = new List<string>();
        foreach (var row in Apps(document))
        {
            var project = Dispatch.Apps.First(app => app.Name == row.App).Project;
            var windows = WindowClassesOf(project);
            if (row.Window.Length == 0)
            {
                // An app with no window class of its own (the editors): it must have none.
                if (windows.Count > 0)
                {
                    problems.Add($"{row.App}: the file names no window, and {project} declares {string.Join(", ", windows.Keys)}.");
                }

                continue;
            }

            if (!windows.TryGetValue(row.Window, out var derivesFrom))
            {
                problems.Add($"{row.App}: {project} declares no window class {row.Window}.");
                continue;
            }

            string[] wanted = row.Type.Length == 0 ? ["AxKitForm"] : types[row.Type];
            if (!wanted.Contains(derivesFrom))
            {
                problems.Add($"{row.App}: {row.Window} derives from {derivesFrom}; the file says {(row.Type.Length == 0 ? "a window of its own, on AxKitForm" : $"the type {row.Type}, {string.Join(" or ", wanted)}")}.");
            }

            // A second window the file names: a class of the app's, on its own type's class, never on the bare base.
            if (row.SecondWindow.Length > 0)
            {
                if (!windows.TryGetValue(row.SecondWindow, out var secondFrom))
                {
                    problems.Add($"{row.App}: {project} declares no window class {row.SecondWindow}, the file's second window.");
                }
                else if (!types.TryGetValue(row.SecondType, out var secondWanted) || !secondWanted.Contains(secondFrom))
                {
                    problems.Add($"{row.App}: the second window {row.SecondWindow} derives from {secondFrom}; the file says the type {row.SecondType}.");
                }
            }

            foreach (var other in windows.Where(window => window.Key != row.Window && window.Key != row.SecondWindow))
            {
                problems.Add($"{row.App}: a window class the file does not name, {other.Key} : {other.Value}; an app has one window (AX-7.2), or a second one of a bundle type that the file names.");
            }
        }

        Assert.True(problems.Count == 0, "docs/axKit/components.json and the sources differ: " + string.Join(" ", problems));
    }

    /// <summary>
    /// FW-7.1: no app has a window of its own, on the bare window base; the last, axFile, moved onto the navigator
    /// on 2026-11-22. A new app's window derives from a type; a name here is a decision to take with the
    /// maintainer, not a line to add.
    /// </summary>
    [Fact]
    public void No_app_has_a_window_of_its_own()
    {
        using var document = File();
        var owed = Apps(document).Where(app => app.Type.Length == 0).Select(app => app.App).Order(StringComparer.Ordinal).ToList();
        Assert.Empty(owed);
        Assert.All(Apps(document), app => Assert.True(app.ToBe.Length > 0, $"{app.App} does not say which type it is to be."));
    }

    /// <summary>
    /// FW-7.4, AX-7.11: an entry is raised by the component the focus entered, never by a window. So the four kinds
    /// are named only where they are defined, in the components, and in the one place of the window base that
    /// plays them. An app's file that names one fails here: the app gets the sound by using the component.
    /// </summary>
    [Fact]
    public void Only_the_components_raise_an_entry()
    {
        string[] allowed =
        [
            "AxKit.Core/Events/EventKind.cs", "AxKit.Core/Events/EventPresenter.cs",
            "AxKit.Forms/AxKitList.cs", "AxKit.Forms/AxKitTextBox.cs", "AxKit.Forms/FormControls.cs", "AxKit.Forms/ControlArea.cs",
            "AxKit.Forms/AxKitForm.Theme.cs",
        ];
        var source = Path.Combine(SourceTexts.RepositoryRoot(), "src");
        var elsewhere = Directory.GetFiles(source, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(file => Regex.IsMatch(System.IO.File.ReadAllText(file), @"EventKind\.(Form|List|Field|Text)Entered\b"))
            .Select(file => Path.GetRelativePath(source, file).Replace('\\', '/'))
            .Where(file => !allowed.Contains(file))
            .ToList();
        Assert.True(elsewhere.Count == 0, "An entry event is named outside the components: " + string.Join(", ", elsewhere));
    }

    [Fact]
    public void What_the_file_names_exists()
    {
        using var document = File();
        var root = SourceTexts.RepositoryRoot();
        using var keys = JsonDocument.Parse(System.IO.File.ReadAllText(Path.Combine(root, "docs", "axKit", "key-families.json")));
        var families = keys.RootElement.GetProperty("families").EnumerateArray().Select(family => family.GetProperty("id").GetString()).ToHashSet();
        foreach (var component in document.RootElement.GetProperty("components").EnumerateArray())
        {
            var id = component.GetProperty("id").GetString();
            var entry = component.GetProperty("entry event").GetString()!;
            Assert.True(EventPresenter.Of(entry) is not null, $"The component '{id}' names the kind of event '{entry}', which EventKind does not have.");
            Assert.True(EventPresenter.SoundsOf(EventPresenter.Of(entry)!.Value).Count == 1, $"The component '{id}': its entry event '{entry}' has no one sound.");
            foreach (var family in component.GetProperty("keys").EnumerateArray().Select(key => key.GetString()))
            {
                Assert.True(families.Contains(family), $"The component '{id}' names the family of keys '{family}', which docs/axKit/key-families.json does not have.");
            }
        }
    }
}
