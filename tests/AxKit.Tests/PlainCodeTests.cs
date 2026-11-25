using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// Plain code (SPEC B-10, the root CLAUDE.md's Stack and constraints and rule 1), read off the sources: rules that held
/// when this test was written (2026-11-24, docs/axKit/repairs.json R-98) and that nothing kept, so that a smaller model
/// changing one app does not bring in what the bundle left out on purpose.
/// </summary>
public class PlainCodeTests
{
    private static IEnumerable<string> SourceFiles(string folder) =>
        Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(file => !Regex.IsMatch(file, @"[\\/](bin|obj)[\\/]"));

    [Fact]
    public void No_interface_is_declared_but_the_ones_Windows_asks_for()
    {
        // An interface with one implementation is what B-10 rules out; Windows' COM interfaces are declared as
        // interfaces with [ComImport] just above them, and are the only ones.
        var root = SourceTexts.RepositoryRoot();
        var declaration = new Regex(@"^\s*(public |internal |private |protected )*(partial )?interface \w+");
        var found = new List<string>();
        foreach (var file in SourceFiles(Path.Combine(root, "src")))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (declaration.IsMatch(lines[i]) && !lines.Skip(Math.Max(0, i - 4)).Take(Math.Min(i, 4)).Any(line => line.Contains("ComImport", StringComparison.Ordinal)))
                {
                    found.Add($"{Path.GetRelativePath(root, file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.True(found.Count == 0, "An interface that is not a COM interface of Windows (SPEC B-10: no interfaces with one implementation):\n" + string.Join("\n", found));
    }

    [Fact]
    public void A_core_project_has_no_window_code()
    {
        // Everything that does not need a window lives in a .Core project and is unit tested there: no Windows Forms.
        var root = SourceTexts.RepositoryRoot();
        var found = new List<string>();
        foreach (var core in Directory.GetDirectories(Path.Combine(root, "src"), "*.Core"))
        {
            foreach (var project in Directory.GetFiles(core, "*.csproj"))
            {
                if (File.ReadAllText(project).Contains("UseWindowsForms", StringComparison.Ordinal))
                {
                    found.Add(Path.GetRelativePath(root, project) + ": UseWindowsForms");
                }
            }

            foreach (var file in SourceFiles(core))
            {
                if (File.ReadAllText(file).Contains("System.Windows.Forms", StringComparison.Ordinal))
                {
                    found.Add(Path.GetRelativePath(root, file) + ": System.Windows.Forms");
                }
            }
        }

        Assert.True(found.Count == 0, "Window code in a core project (it belongs in the app's window project or AxKit.Forms):\n" + string.Join("\n", found));
    }

    [Fact]
    public void Ui_automation_notifications_are_raised_by_the_announcer_alone()
    {
        // Rule 1: results are announced through one UI Automation notification, raised in one place, so that every
        // window speaks the same way and the joining and holding of AX-7.3 apply to all of them.
        var root = SourceTexts.RepositoryRoot();
        var raising = SourceFiles(Path.Combine(root, "src"))
            .Where(file => File.ReadAllText(file).Contains("RaiseAutomationNotification", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .ToList();
        Assert.Equal(["src/AxKit.Forms/Announcer.cs"], raising);
    }
}
