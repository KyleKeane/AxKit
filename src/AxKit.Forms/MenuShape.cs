using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The shape rules of a window's bar (docs/axKit/SPEC.md AX-9.1, 2026-11-11): never deeper than bar menu, submenu,
/// item, as Window, New window, axClaude is. <see cref="AxKitForm.PlaceMenuBar"/> logs every item past that depth;
/// <c>MenuTests</c> holds the rule.
/// </summary>
public static class MenuShape
{
    /// <summary>The deepest an item may stand: the bar's menu is 1, a submenu 2, an item in it 3.</summary>
    public const int MaxDepth = 3;

    /// <summary>The paths of the items deeper than <see cref="MaxDepth"/> ("Options, Project folders, Remove a project folder, 1. x"), none on a bar that keeps the rule.</summary>
    public static List<string> TooDeep(ToolStripItemCollection bar)
    {
        var found = new List<string>();
        Walk(bar, [], found);
        return found;
    }

    private static void Walk(ToolStripItemCollection items, List<string> path, List<string> found)
    {
        foreach (ToolStripItem item in items)
        {
            if (item is not ToolStripMenuItem menuItem)
            {
                continue;
            }

            path.Add(MenuText.Plain(menuItem.Text ?? string.Empty));
            if (path.Count > MaxDepth)
            {
                found.Add(string.Join(", ", path));
            }

            if (menuItem.HasDropDownItems)
            {
                Walk(menuItem.DropDownItems, path, found);
            }

            path.RemoveAt(path.Count - 1);
        }
    }
}
