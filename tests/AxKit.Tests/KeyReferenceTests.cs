using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The F1 text's part made from the window's commands (docs/axKit/SPEC.md AX-7.2a): by menu, then the keys alone.</summary>
public sealed class KeyReferenceTests
{
    private static ListedCommand Row(string label, string menu, string keyText, bool keyOnly = false) =>
        new(label, menu, keyText.Length > 0 ? keyText.ToLowerInvariant() : string.Empty, [], keyText, "read", null, keyOnly, true, string.Empty);

    [Fact]
    public void Every_command_with_a_key_is_listed_under_its_top_menu_then_the_keys_alone()
    {
        var text = KeyReference.Section(
        [
            Row("Open", "File", "Ctrl+O"),
            Row("Close", "File", string.Empty),
            Row("Heading", "Navigation, Next", "H"),
            Row("Open", "File", "Ctrl+O"),
            Row("Next pane", string.Empty, "F6", keyOnly: true),
        ]);
        Assert.Equal(
            KeyReference.Heading + "\n" + KeyReference.Intro + "\n"
            + "### File menu\n  Ctrl+O: Open\n"
            + "### Navigation menu\n  H: Heading (Next)\n"
            + KeyReference.KeysAlone + "\n  F6: Next pane\n",
            text);
    }

    [Fact]
    public void A_window_with_no_key_alone_has_no_such_heading()
    {
        Assert.DoesNotContain(KeyReference.KeysAlone, KeyReference.Section([Row("Open", "File", "Ctrl+O")]), StringComparison.Ordinal);
    }
}
