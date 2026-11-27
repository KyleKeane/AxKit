using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// One source for every key (the maintainer, 2026-11-23: "please make sure that there is only one source of tructh for
/// the hotkeys and that everything is filling its code and hints from those. we should not maintaining things by
/// hand"). A key is defined in its table (BundleKeys, the window's Ax&lt;Name&gt;Keys, AppLaunchKeys) and its name in
/// a text, an F1 line, a hint or a notice, is made from the table (<c>BundleKeys.Text</c>), never typed. This test is
/// a ratchet: it counts, per source file, the key names typed into the code outside comments and the key tables, and
/// fails when a file has more than <see cref="StillByHand"/> allows (a new key typed by hand) or fewer (a file
/// converted: lower its number, or take it off the list, in the same commit), so that the list only shrinks.
/// </summary>
public sealed partial class HandKeyTests
{
    /// <summary>The files that still type key names, with how many; empty when every text is made from the tables.</summary>
    private static readonly Dictionary<string, int> StillByHand = new()
    {
        ["src/AxAutomate/HelpText.cs"] = 1,
        ["src/AxCalc/HelpText.cs"] = 3,
        ["src/AxClaude.Core/StartupOptions.cs"] = 1,
        ["src/AxClaude.Core/Transcript/SessionModel.cs"] = 1,
        ["src/AxClaude/HelpText.cs"] = 25,
        ["src/AxClaude/MainForm.Model.cs"] = 1,
        ["src/AxClaude/MainForm.NewSession.cs"] = 1,
        ["src/AxClaude/MainForm.PutAway.cs"] = 1,
        ["src/AxClaude/MainForm.Ui.cs"] = 2,
        ["src/AxClip/HelpText.cs"] = 1,
        ["src/AxDown/HelpText.cs"] = 5,
        ["src/AxGit/HelpText.cs"] = 7,
        ["src/AxHelp.Core/WorkingFolderFiles.cs"] = 1,
        ["src/AxHelp/HelpText.cs"] = 4,
        ["src/AxJSON/HelpText.cs"] = 4,
        ["src/AxKit.Core/Ai/AiPrompts.cs"] = 1,
        ["src/AxKit.Core/Ai/AiToolsRule.cs"] = 1,
        // One more on 2026-11-26 (Alt+O in the bundle's menus line, AX-9.1, the Options menu with the bundle's settings). Three more the same day: the rules of the keys (AX-8.18) name Ctrl+Delete, Ctrl+Space and Ctrl+Enter, and
        // AxKit.Core cannot read the tables of AxKit.Forms; the rules' patterns are written in words (Ctrl and Shift).
        ["src/AxKit.Core/BundleHelp.cs"] = 30,
        ["src/AxKit.Core/ListOptions.cs"] = 2,
        ["src/AxKit.Core/Mcp/McpOffer.cs"] = 2,
        ["src/AxKit.Core/Mcp/McpTools.cs"] = 1,
        ["src/AxKit.Forms/AxKitForm.cs"] = 1,
        ["src/AxKit.Forms/ConversationForm.cs"] = 1,
        ["src/AxKit.Forms/TextEditorForm.Viewer.cs"] = 1,
        ["src/AxKit.Forms/TextEditorForm.cs"] = 4,
        ["src/AxMCP.Core/MCPSettings.cs"] = 1,
        ["src/AxPass/HelpText.cs"] = 1,
        ["src/AxPreview.Core/PictureText.cs"] = 2,
        ["src/AxPreview.Core/Renderers.cs"] = 1,
        ["src/AxPreview/HelpText.cs"] = 5,
        ["src/AxProcess/HelpText.cs"] = 3,
        ["src/AxProcess/ProcessForm.cs"] = 2,
        ["src/AxStart/HelpText.cs"] = 1,
        ["src/AxTask.Core/ListActions.cs"] = 5,
        ["src/AxTask/HelpText.cs"] = 2,
        ["src/AxTask/TaskForm.Commands.cs"] = 1,
        ["src/AxTemplate.Core/TemplateMatch.cs"] = 1,
        ["src/AxTemplate/HelpText.cs"] = 3,
        ["src/AxTerminal/HelpText.cs"] = 15,
        ["src/AxTerminal/TerminalForm.NewShell.cs"] = 1,
        ["src/AxTerminal/TerminalForm.Ui.cs"] = 2,
    };

    /// <summary>The key tables and the code that writes a key's name from its value: the one place a key is typed.</summary>
    private static bool IsKeyTable(string relative) =>
        relative.EndsWith("Keys.cs", StringComparison.Ordinal)
        || relative is "src/AxKit.Core/KeyChord.cs" or "src/AxKit.Core/Hotkey.cs" or "src/AxKit.Core/KeyTest.cs";

    [GeneratedRegex(@"\b(Ctrl|Alt|Shift|Windows)\+([A-Z][A-Za-z]*|F[0-9]+|[0-9])\b")]
    private static partial Regex KeyName();

    /// <summary>The key names typed in one file's code: comment lines and the comment after " //" left out.</summary>
    public static int Count(string text)
    {
        var count = 0;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimStart();
            if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('*') || line.StartsWith("/*", StringComparison.Ordinal))
            {
                continue;
            }

            var comment = line.IndexOf(" //", StringComparison.Ordinal);
            count += KeyName().Count(comment < 0 ? line : line[..comment]);
        }

        return count;
    }

    [Fact]
    public void A_key_name_is_typed_only_in_the_key_tables()
    {
        var root = SourceTexts.RepositoryRoot();
        var found = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal) || IsKeyTable(relative))
            {
                continue;
            }

            var count = Count(File.ReadAllText(file));
            if (count > 0)
            {
                found[relative] = count;
            }
        }

        var wrong = new List<string>();
        foreach (var (file, count) in found)
        {
            var allowed = StillByHand.GetValueOrDefault(file);
            if (count > allowed)
            {
                wrong.Add($"{file}: {count} key names typed by hand, {allowed} allowed: make the new ones from the key table (BundleKeys.Text)");
            }
            else if (count < allowed)
            {
                wrong.Add($"{file}: {count} key names typed by hand, the list says {allowed}: lower its number in HandKeyTests.StillByHand");
            }
        }

        wrong.AddRange(StillByHand.Keys.Where(file => !found.ContainsKey(file)).Select(file => $"{file}: no key name typed by hand any more: take it off HandKeyTests.StillByHand"));
        Assert.True(wrong.Count == 0, string.Join("\n", wrong) + "\n\nThe list as the sources are now:\n" + string.Join("\n", found.Select(entry => $"        [\"{entry.Key}\"] = {entry.Value},")));
    }

    [Fact]
    public void Key_names_in_comments_are_not_counted()
    {
        Assert.Equal(2, Count("var a = \"Ctrl+S: save\"; // Ctrl+Q here\n/// Alt+F4 closes\n * Shift+Tab\nText(\"F1 and Ctrl+Shift+P\")"));
    }
}
