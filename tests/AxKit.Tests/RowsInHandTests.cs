using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// What a command of a navigator acts on is decided in one place, the item window's <c>RowsInHand</c>
/// (docs/axKit/SPEC.md AX-8.16, "What the commands act on"): the selected rows whenever any is selected, the row you
/// are on alone otherwise. Read off the sources, as <see cref="ListTests"/> reads them. The review of 2026-11-27
/// found three windows that asked for the selected rows themselves and took them only when there were two or more,
/// so that Delete with one row selected and the arrow keys moved on acted on the row under the arrow keys. Since
/// then an app cannot reach the selected rows but through that method: the item window's own list of them is
/// private, which this test holds, with the names a second way would most likely have.
/// </summary>
public sealed partial class RowsInHandTests
{
    /// <summary>The selected rows, asked for outside the item window: its own method, or the marks' rows taken by hand.</summary>
    [GeneratedRegex(@"\bSelectedRows\s*\(|\bSelection\s*\.\s*(Contains|Keys)\b[^;]*\.\s*(Where|Select)\b|\bWhere\s*\([^;]*\bSelection\s*\.\s*Contains\b|" + TwoOrMore)]
    private static partial Regex AnotherWay();

    /// <summary>
    /// The original fault's own shape (the review's second pass of 2026-11-27: the pattern above did not hold
    /// it): the selection taken only when two or more rows are selected, which is a count of the selected rows
    /// compared with a number other than none, written as a comparison or as a pattern. Whether any row is
    /// selected (a comparison with 0) is the one question a window has reason to ask.
    /// </summary>
    private const string TwoOrMore = @"\bSelection\s*\.\s*Count\s*(?:[<>]=?|[=!]=)\s*[1-9]|\b(Selection|SelectedRows\s*\(\s*\))\s+is\s*(not\s*)?\{\s*Count\s*:\s*(?:[<>]=?\s*)?[1-9]";

    /// <summary>The check is itself checked: the ways the fault was and could be written are found, and what the windows rightly write is not.</summary>
    [Theory]
    [InlineData("var rows = SelectedRows() is { Count: > 1 } selected ? selected : [row];", true)]
    [InlineData("var rows = Selection.Count > 1 ? RowsInHand() : [row];", true)]
    [InlineData("if (Selection.Count >= 2)", true)]
    [InlineData("return Selection.Count==1 ? [current] : RowsInHand();", true)]
    [InlineData("var several = Selection is { Count: >= 2 };", true)]
    [InlineData("var mine = rows.Where(row => Selection.Contains(row.Key)).ToList();", true)]
    [InlineData("var mine = Selection.Keys.Select(key => Find(key)).ToList();", true)]
    [InlineData("if (Selection.Count == 0)", false)]
    [InlineData("only = Selection.Count > 0 ? [.. RowsInHand().Select(row => row.Key)] : [];", false)]
    [InlineData("text += $\", {Selection.Count} selected\";", false)]
    [InlineData("var rows = RowsInHand();", false)]
    public void The_check_finds_the_faults_shape(string line, bool found) => Assert.Equal(found, AnotherWay().IsMatch(line));

    [Fact]
    public void The_item_window_keeps_its_list_of_the_selected_rows_to_itself()
    {
        var source = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "ItemForm.cs"));
        Assert.Contains("private IReadOnlyList<ItemRow> SelectedRows()", source, StringComparison.Ordinal);
        Assert.Contains("protected IReadOnlyList<ItemRow> RowsInHand()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void No_window_takes_the_selected_rows_another_way()
    {
        var root = SourceTexts.RepositoryRoot();
        var separator = Path.DirectorySeparatorChar;
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{separator}obj{separator}", StringComparison.Ordinal) || relative.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
                || relative.StartsWith(Path.Combine("src", "AxKit.Forms") + separator, StringComparison.Ordinal))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (!line.TrimStart().StartsWith("//", StringComparison.Ordinal) && AnotherWay().IsMatch(line))
                {
                    offenders.Add($"{relative}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "The selected rows taken in a window itself; what a command acts on is ItemForm.RowsInHand (AX-8.16): " + string.Join("; ", offenders));
    }
}
