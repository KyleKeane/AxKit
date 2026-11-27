using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// The rule of a private copy (<c>AxKit.Forms/PrivateClipboard</c>; axPass PW-5.4, axTemplate TP-4.3, axClip CP-3.1):
/// the marks a password manager sets, by which axTemplate's launcher never puts such a text back as plain text and
/// axClip keeps no copy of it. Only the rule is tested here: the clipboard is the user's while the tests run.
/// </summary>
public sealed class PrivateClipboardTests
{
    [Fact]
    public void A_copy_is_private_by_an_excluding_format_or_a_value_of_zero()
    {
        Assert.False(PrivateClipboard.IsPrivate(excludingFormat: false, history: null, cloud: null));
        Assert.True(PrivateClipboard.IsPrivate(excludingFormat: true, history: null, cloud: null));
        Assert.True(PrivateClipboard.IsPrivate(excludingFormat: false, history: 0, cloud: null));
        Assert.True(PrivateClipboard.IsPrivate(excludingFormat: false, history: null, cloud: 0));
        // A program that says its copy may be kept is taken at its word.
        Assert.False(PrivateClipboard.IsPrivate(excludingFormat: false, history: 1, cloud: 1));
    }

    [Fact]
    public void What_Copy_marks_is_what_the_rule_reads()
    {
        Assert.Equal(["ExcludeClipboardContentFromMonitorProcessing", "Clipboard Viewer Ignore"], PrivateClipboard.ExcludingFormats);
        Assert.Equal("CanIncludeInClipboardHistory", PrivateClipboard.HistoryFormat);
        Assert.Equal("CanUploadToCloudClipboard", PrivateClipboard.CloudFormat);

        // Read off the source: a clipboard that cannot be opened to read the marks counts as private, and a value
        // that is there and cannot be read is 0, the value that keeps a copy out.
        var source = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "PrivateClipboard.cs")).ReplaceLineEndings("\n");
        Assert.Contains("catch (ExternalException)\n        {\n            return true;\n        }", source, StringComparison.Ordinal);
        Assert.Contains("if (pointer == 0)\n            {\n                return 0;\n            }", source, StringComparison.Ordinal);
    }
}
