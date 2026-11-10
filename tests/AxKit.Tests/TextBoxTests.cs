using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// Every edit control of the bundle is <c>AxKit.Forms/AxKitTextBox</c> (docs/axkit/SPEC.md AX-8.9), which has no
/// length cap and the bundle's page and paragraph keys built in, so that a new field gets them by being made and
/// nothing else. Read off the sources: a plain <c>TextBox</c> made in an app or the shared pieces fails here, and so
/// does a second subscription of <c>EditKeys.Handle</c>, which would move the caret twice.
/// </summary>
public sealed class TextBoxTests
{
    [Fact]
    public void Every_edit_control_is_the_bundles_text_box()
    {
        var root = LayoutTests.RepositoryRoot();
        var made = new List<string>();
        var subscribed = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var isTheComponent = relative.EndsWith(Path.Combine("AxKit.Forms", "AxKitTextBox.cs"), StringComparison.OrdinalIgnoreCase);
            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                // "TextBox _field = new()", "TextBox Input { get; } = new()", "new TextBox {", "class X : TextBox":
                // a plain text box made or derived from. "AxKitTextBox" has no word boundary before "TextBox".
                if (!isTheComponent && Regex.IsMatch(line, @"\bTextBox\s+\w+\s*(?:\{\s*get;\s*\}\s*)?=\s*new\b|\bnew\s+TextBox\s*[({]|:\s*TextBox\b"))
                {
                    made.Add($"{relative}:{number}: {line.Trim()}");
                }

                if (!isTheComponent && line.Contains("EditKeys.Handle"))
                {
                    subscribed.Add($"{relative}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(made.Count == 0, "A plain TextBox where the bundle's AxKitTextBox belongs (AX-8.9): " + string.Join("; ", made));
        Assert.True(subscribed.Count == 0, "EditKeys.Handle subscribed outside AxKitTextBox, which handles the keys itself; the caret would move twice: " + string.Join("; ", subscribed));
    }
}
