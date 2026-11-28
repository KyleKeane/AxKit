using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// A modal loop ends "opened by the user" (the sixth review of 2026-11-27, F3; docs/axKit/SPEC.md AX-6.4). The window
/// base knows which key of the user's it handles (<see cref="UserKeyMoment"/>), so that a notice the key opens at once
/// may take its keys at once. A Windows dialog inside the key's command (Save as, a file picker) is a loop of its own:
/// the user's keys inside it are the dialog's, and the notice the command shows after it ("Save as UTF-8?", axFile's
/// name for a program) is no first result of the key any more. Until then it was, and the Enter that closed the
/// dialog, pressed twice, answered it unheard. Every modal loop of a window goes through the base's
/// <c>InModalLoop</c>, which this file reads the sources for.
/// </summary>
public sealed class ModalLoopTests
{
    /// <summary>The key of the moment is set for its handling, restored after it, and cleared by a loop inside it for the rest of the handling.</summary>
    [Fact]
    public void A_modal_loop_ends_the_users_key_for_the_rest_of_its_handling()
    {
        var moment = new UserKeyMoment();
        Assert.Null(moment.Current);
        Keys? insideLoop = Keys.None;
        Keys? afterLoop = Keys.None;
        moment.Handle(Keys.Control | Keys.Shift | Keys.S, () =>
        {
            Assert.Equal(Keys.Control | Keys.Shift | Keys.S, moment.Current);
            moment.ModalLoop(() =>
            {
                insideLoop = moment.Current;
                // A key the dialog's own loop hands the window (a nested dispatch): it restores what it found.
                moment.Handle(Keys.Enter, () => true);
                return true;
            });
            afterLoop = moment.Current;
            return true;
        });
        Assert.Null(insideLoop);
        Assert.Null(afterLoop);
        Assert.Null(moment.Current);
    }

    /// <summary>
    /// Save as, then "Save as UTF-8?": Ctrl+Shift+S runs Save as, whose Windows dialog is a modal loop; the question
    /// the write asks after it, made <see cref="Notice.ByUser"/>, holds its keys as the window base shows it. The same
    /// question asked without a dialog in between, at once from a Ctrl key, takes them at once.
    /// </summary>
    [Fact]
    public void Save_as_then_its_question_holds_its_keys()
    {
        var question = new Notice("Save as UTF-8?", "notes.txt is written in Windows-1252.",
            [new OverlayChoice("&Save as UTF-8", IsDefault: true), new OverlayChoice("&Cancel", IsCancel: true)]).ByUser();
        Assert.False(ShownAfter(Keys.Control | Keys.Shift | Keys.S, question, dialog: true).OpenedByUser);
        Assert.True(ShownAfter(Keys.Control | Keys.Shift | Keys.S, question, dialog: false).OpenedByUser);
        var editor = Read("src", "AxKit.Forms", "TextEditorForm.Files.cs");
        var saveAs = editor[editor.IndexOf("private void SaveAs(Action? then = null)", StringComparison.Ordinal)..];
        Assert.True(saveAs.IndexOf("ShowSystemDialog(dialog)", StringComparison.Ordinal) is var shown and > 0
            && saveAs.IndexOf("WriteFile(dialog.FileName", StringComparison.Ordinal) > shown, "Save as asks through ShowSystemDialog before it writes, and the write asks the question");
    }

    /// <summary>axFile's Add a program: the program picker, then the field for its name, which AskText shows as made by the user; it holds.</summary>
    [Fact]
    public void AxFiles_program_picker_then_its_name_field_holds_its_keys()
    {
        var name = Notice.Plain("Add a program", "The name the menu shows.").ByUser();
        Assert.False(ShownAfter(Keys.None, name, dialog: true).OpenedByUser);
        var programs = Read("src", "AxFile", "FileForm.Programs.cs");
        var add = programs[programs.IndexOf("private void AddProgram()", StringComparison.Ordinal)..];
        Assert.True(add.IndexOf("ShowSystemDialog(dialog)", StringComparison.Ordinal) is var shown and > 0
            && add.IndexOf("AskText(\"Add a program\"", StringComparison.Ordinal) > shown, "Add a program asks through ShowSystemDialog before its name field");
    }

    /// <summary>The window base's dialog goes through the loop, and the loop through the moment.</summary>
    [Fact]
    public void The_window_bases_dialogs_go_through_the_loop()
    {
        var window = Read("src", "AxKit.Forms", "AxKitForm.cs");
        var dialog = window[window.IndexOf("protected DialogResult ShowSystemDialog(CommonDialog dialog)", StringComparison.Ordinal)..];
        Assert.Contains("return InModalLoop(() => dialog.ShowDialog(this));", dialog[..dialog.IndexOf("\n    }\n", StringComparison.Ordinal)], StringComparison.Ordinal);
        Assert.Contains("protected internal T InModalLoop<T>(Func<T> loop) => _userKey.ModalLoop(loop);", window, StringComparison.Ordinal);
    }

    /// <summary>The ways into a modal loop a window has: a Windows dialog, the shell's Open with and Properties, the shell's menu and a native popup menu, a message box.</summary>
    private static readonly string[] Loops = [".ShowDialog(", "WindowsOpenWith.Show(", "Shell.Properties(", "Shell.ShowMenu(", "NativeMenu.Choose(", "NativeMenu.Track(", "MessageBox.Show("];

    /// <summary>
    /// The places that may enter a loop outside <c>InModalLoop</c>, each with why: the host's message boxes come before
    /// any window exists (the usage, the version, a start that failed), and the shell's menu tracks its popup inside
    /// Shell.ShowMenu, which every caller runs inside the loop.
    /// </summary>
    private static readonly Dictionary<string, string> Outside = new(StringComparer.OrdinalIgnoreCase)
    {
        [Path.Combine("src", "AxKit.Forms", "AppHost.cs")] = "before any window exists",
        [Path.Combine("src", "AxKit", "Program.cs")] = "before any window exists",
        [Path.Combine("src", "AxFile", "Shell.cs")] = "inside Shell.ShowMenu, which its callers run in the loop",
        [Path.Combine("src", "AxKit.Forms", "NativeMenu.cs")] = "its own definition",
        [Path.Combine("src", "AxKit.Forms", "WindowsOpenWith.cs")] = "its own definition",
    };

    /// <summary>Every modal loop of a window, in every partial file of every app, runs inside <c>InModalLoop</c>, on the line that enters it.</summary>
    [Fact]
    public void Every_modal_loop_of_a_window_goes_through_the_base()
    {
        var root = SourceTexts.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || Outside.ContainsKey(relative))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("//", StringComparison.Ordinal) || !Loops.Any(loop => line.Contains(loop, StringComparison.Ordinal)))
                {
                    continue;
                }

                if (!line.Contains("InModalLoop(", StringComparison.Ordinal))
                {
                    offenders.Add($"{relative}:{i + 1}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "A modal loop outside InModalLoop keeps the user's key as the opener of what follows it: " + string.Join(", ", offenders));
    }

    /// <summary>A source file of the repository, its line ends as LF.</summary>
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([SourceTexts.RepositoryRoot(), .. parts])).ReplaceLineEndings("\n");

    /// <summary>The notice as the window base shows it after the key's command ran, with or without a dialog in between (the same steps as AxKitForm's ShowSystemDialog and ShowNotice).</summary>
    private static Notice ShownAfter(Keys key, Notice notice, bool dialog)
    {
        var moment = new UserKeyMoment();
        Notice? shown = null;
        moment.Handle(key, () =>
        {
            if (dialog)
            {
                moment.ModalLoop(() => moment.Handle(Keys.Enter, () => true));
            }

            shown = Notice.AsShown(notice, followsAnswer: false, forAiTool: false, userKey: moment.Current);
            return true;
        });
        return shown!;
    }
}
