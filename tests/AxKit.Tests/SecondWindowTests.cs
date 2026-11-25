namespace AxKit.Tests;

/// <summary>
/// No second window of an app's own (docs/axKit/SPEC.md AX-7.2; the root CLAUDE.md, rule 2): questions, errors and
/// help texts are notices inside the window (<c>OverlayPanel</c>), and what an app waits on goes in place of its input
/// field (<c>ControlArea</c>); never a <c>Form</c> or a <c>MessageBox</c> once a window exists. Read off the sources, as
/// <see cref="ListTests"/> reads them: a message box, a dialog shown or a bare form made anywhere under <c>src</c>
/// fails here, unless its file is one of those below, each with its reason. The shell's own dialogs and menu (axFile's
/// Open with, Properties and Windows menu) are Windows' calls, not these, and are not looked for.
/// </summary>
public sealed class SecondWindowTests
{
    /// <summary>The files allowed a message box or a dialog, with why.</summary>
    private static readonly IReadOnlyDictionary<string, string> Allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // The executable before any app is chosen: a bad command line, --help and --version of the bundle itself.
        [Path.Combine("src", "AxKit", "Program.cs")] = "no window exists yet",
        // An app's start before its window: --help, --version, a bad command line, the question about a background
        // app that does not answer; and a crash report only when there is no window to put it in.
        [Path.Combine("src", "AxKit.Forms", "AppHost.cs")] = "no window exists yet, or none is left",
        // ShowSystemDialog, the one place a window shows a dialog of Windows: the folder, file and font pickers.
        [Path.Combine("src", "AxKit.Forms", "AxKitForm.cs")] = "the Windows pickers",
    };

    /// <summary>What makes a second window: a message box, a dialog shown modally, a form that is no window of the bundle's.</summary>
    private static readonly string[] Makers = ["MessageBox.Show", ".ShowDialog(", "new Form("];

    [Fact]
    public void No_message_box_dialog_or_bare_form_outside_the_allowed_files()
    {
        var root = SourceTexts.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || Allowed.ContainsKey(relative))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                // A comment may name them to say what is not done.
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                if (Makers.Any(maker => line.Contains(maker, StringComparison.Ordinal)))
                {
                    offenders.Add($"{relative}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "A second window where a notice inside the window belongs (AX-7.2: OverlayPanel, ControlArea; a Windows picker goes through AxKitForm.ShowSystemDialog): " + string.Join("; ", offenders));
    }

    /// <summary>The list above stays true: a file that no longer shows any of them leaves the list.</summary>
    [Fact]
    public void Every_allowed_file_still_needs_its_place_in_the_list()
    {
        var root = SourceTexts.RepositoryRoot();
        var idle = Allowed.Keys
            .Where(relative => !File.Exists(Path.Combine(root, relative)) || !Makers.Any(maker => File.ReadAllText(Path.Combine(root, relative)).Contains(maker, StringComparison.Ordinal)))
            .ToList();
        Assert.True(idle.Count == 0, "Allowed a second window and showing none: take it out of SecondWindowTests.Allowed: " + string.Join(", ", idle));
    }
}
