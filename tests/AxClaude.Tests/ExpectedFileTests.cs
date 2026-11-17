namespace AxClaude.Tests;

/// <summary>
/// The writer of expected files in a test's update mode (tests/Shared/Fixtures.cs, AXCLAUDE_UPDATE_EXPECTED=1 and
/// AXTERMINAL_UPDATE_EXPECTED=1): it leaves a file whose text is the same as it is, whatever its line ends, so that
/// git lists only the expected files that changed.
/// </summary>
public sealed class ExpectedFileTests
{
    [Fact]
    public void A_file_with_the_same_text_keeps_its_bytes_whatever_its_line_ends()
    {
        using var temp = new TempFolder("axclaude-expected");
        var path = temp.File("a.expected.txt", "one\r\ntwo\r\n"u8.ToArray());

        Assert.False(Fixtures.WriteExpected(path, "one\ntwo\n"));
        Assert.Equal("one\r\ntwo\r\n"u8.ToArray(), File.ReadAllBytes(path));
    }

    [Fact]
    public void A_file_whose_text_differs_and_a_file_that_is_missing_are_written()
    {
        using var temp = new TempFolder("axclaude-expected");
        var path = temp.File("a.expected.txt", "one\r\ntwo\r\n"u8.ToArray());

        Assert.True(Fixtures.WriteExpected(path, "one\nthree\n"));
        Assert.Equal("one\nthree\n"u8.ToArray(), File.ReadAllBytes(path));

        var missing = Path.Combine(temp.Path, "b.expected.txt");
        Assert.True(Fixtures.WriteExpected(missing, "new\n"));
        Assert.Equal("new\n"u8.ToArray(), File.ReadAllBytes(missing));
    }
}
