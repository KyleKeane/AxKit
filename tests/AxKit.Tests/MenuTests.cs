using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// A menu bar item's drop-down is the one WinForms makes (docs/axKit/SPEC.md AX-6.6): a hand-made drop-down assigned
/// to <c>DropDown</c> loses its owner item when it closes, after which the bar opens its neighbours' menus on Left and
/// Right (found in axFile's Item menu, 2026-10-21). Read off the sources, so that the construct fails here before it
/// is felt in a window.
/// </summary>
public sealed class MenuTests
{
    [Fact]
    public void No_menu_item_gets_a_hand_made_drop_down()
    {
        var root = LayoutTests.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (Regex.IsMatch(line, @"\bDropDown\s*=\s*new\b"))
                {
                    offenders.Add($"{Path.GetRelativePath(root, file)}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "A hand-made drop-down on a menu item: " + string.Join("; ", offenders));
    }

    /// <summary>
    /// A menu item names an app as the bundle spells it, the ax in lower case (docs/axKit/SPEC.md, the naming rule of
    /// 2026-10-23): "ax&amp;GIT", never "Ax&amp;Git", which the Show current folder in submenus kept until 2026-10-24.
    /// </summary>
    [Fact]
    public void No_menu_item_spells_an_app_the_old_way()
    {
        var root = LayoutTests.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (line.Contains("\"Ax&", StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetRelativePath(root, file)}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "An app named the old way in a menu: " + string.Join("; ", offenders));
    }

    /// <summary>
    /// Every window's bar is the same eight menus in one order, each with its own letter (docs/axKit/SPEC.md AX-9.1,
    /// B-19, the user's word of 2026-11-11): the names the window base gives them.
    /// </summary>
    [Fact]
    public void The_bar_is_eight_menus_in_one_order_with_letters_of_their_own()
    {
        Assert.Equal(
            ["&Session", "&Actions", "&Navigation", "&Context", "&Options", "&Window", "ax&Kit", "&Help"],
            AxKit.Forms.AxKitForm.BarMenus);
        var letters = AxKit.Forms.AxKitForm.BarMenus.Select(name => char.ToUpperInvariant(name[name.IndexOf('&') + 1])).ToList();
        Assert.Equal(letters.Count, letters.Distinct().Count());
        Assert.Equal('C', AxKit.Forms.ContextBarMenu.Letter);
    }

    /// <summary>
    /// Every window puts its bar together through the window base (AX-9.1, B-19): a project whose windows make a Help
    /// menu calls <c>PlaceMenuBar</c>, and no source adds menus to a bar of its own, so that no bar drifts from the eight.
    /// </summary>
    [Fact]
    public void Every_window_places_its_bar_through_the_base()
    {
        var root = LayoutTests.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var project in Directory.GetDirectories(Path.Combine(root, "src")))
        {
            var files = Directory.GetFiles(project, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")).ToList();
            var texts = files.Select(File.ReadAllText).ToList();
            var makesHelp = texts.Any(text => text.Contains("CreateHelpMenu(", StringComparison.Ordinal) && !text.Contains("protected ToolStripMenuItem CreateHelpMenu(", StringComparison.Ordinal));
            if (makesHelp && !texts.Any(text => text.Contains("PlaceMenuBar(", StringComparison.Ordinal)))
            {
                offenders.Add(Path.GetFileName(project) + ": a Help menu but no PlaceMenuBar");
            }

            for (var i = 0; i < files.Count; i++)
            {
                if (Regex.IsMatch(texts[i], @"\b(_menu|MenuBar|menu)\.Items\.(Add|AddRange)\(") && !files[i].EndsWith("AxKitForm.cs", StringComparison.Ordinal))
                {
                    offenders.Add(Path.GetRelativePath(root, files[i]) + ": adds to a menu bar itself");
                }
            }
        }

        Assert.True(offenders.Count == 0, "A bar not placed through AxKitForm.PlaceMenuBar: " + string.Join("; ", offenders));
    }

    /// <summary>
    /// A bar is never deeper than its menu, a submenu and an item (AX-9.1, 2026-11-11, the user's rule: Window, New
    /// window, axClaude is the deepest). <c>MenuShape.TooDeep</c> names every item past that, and the window base logs
    /// them as it places a bar; here the rule itself, on a bar of each shape.
    /// </summary>
    [Fact]
    public void A_bar_is_never_deeper_than_a_menu_a_submenu_and_an_item()
    {
        var bar = new MenuStrip();
        var window = new ToolStripMenuItem("&Window");
        var newWindow = new ToolStripMenuItem("&New window");
        newWindow.DropDownItems.Add(new ToolStripMenuItem("ax&Claude"));
        window.DropDownItems.Add(newWindow);
        bar.Items.Add(window);
        Assert.Empty(AxKit.Forms.MenuShape.TooDeep(bar.Items));

        var options = new ToolStripMenuItem("&Options");
        var folders = new ToolStripMenuItem("&Project folders");
        var remove = new ToolStripMenuItem("&Remove a project folder");
        remove.DropDownItems.Add(new ToolStripMenuItem("&1. Work"));
        folders.DropDownItems.Add(remove);
        options.DropDownItems.Add(folders);
        bar.Items.Add(options);
        Assert.Equal(["Options, Project folders, Remove a project folder, 1. Work"], AxKit.Forms.MenuShape.TooDeep(bar.Items));
    }
}