using AxKit.Core;
using AxPreview.Core;

namespace AxPreview.Tests;

/// <summary>A picture's text through Windows' reader (docs/axPreview/SPEC.md AP-7.2 item 4, AP-D11): the script and its run as pure code, and the reader itself on the fixture when this Windows has a language for it.</summary>
public sealed class PictureTextTests
{
    [Fact]
    public void The_script_asks_Windows_reader_and_runs_as_Windows_PowerShell()
    {
        Assert.Contains("Windows.Media.Ocr.OcrEngine", PictureText.Script);
        Assert.Contains("exit 2", PictureText.Script);
        Assert.Contains("MaxImageDimension", PictureText.Script);
        var start = PictureText.Start(@"C:\scripts\ocr.ps1", @"C:\pictures\shot.png");
        Assert.EndsWith(@"\WindowsPowerShell\v1.0\powershell.exe", start.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", @"C:\scripts\ocr.ps1", @"C:\pictures\shot.png"], start.ArgumentList);
    }

    [Fact]
    public void A_run_becomes_lines_or_a_problem()
    {
        var (lines, problem) = PictureText.Read(new ProcessResult(0, "Hello\r\nSecond line\r\n\r\n", string.Empty, TimedOut: false, Finished: true));
        Assert.Null(problem);
        Assert.Equal(["Hello", "Second line"], lines);

        (lines, problem) = PictureText.Read(new ProcessResult(0, string.Empty, string.Empty, TimedOut: false, Finished: true));
        Assert.Null(problem);
        Assert.Empty(lines!);

        (lines, problem) = PictureText.Read(new ProcessResult(2, string.Empty, string.Empty, TimedOut: false, Finished: true));
        Assert.Null(lines);
        Assert.Equal(PictureText.NoLanguage, problem);

        (lines, problem) = PictureText.Read(new ProcessResult(1, string.Empty, "The file is not a picture\r\nmore", TimedOut: false, Finished: true));
        Assert.Null(lines);
        Assert.Equal("The file is not a picture", problem);

        (lines, problem) = PictureText.Read(new ProcessResult(-1, string.Empty, string.Empty, TimedOut: true, Finished: false));
        Assert.Null(lines);
        Assert.Contains("no answer in 30 seconds", problem);
    }

    [Fact]
    public void The_first_page_names_the_picture_and_the_finished_page_holds_its_text()
    {
        var first = PictureText.FirstPage(@"C:\pictures\shot.png");
        Assert.StartsWith("shot.png is a picture (PNG). Reading its text", first.Document.Text);
        Assert.NotNull(first.Later);
        Assert.Null(first.Said);
        Assert.Equal(WayOut.Windows, first.WayOut);

        var text = PictureText.Finished(@"C:\pictures\shot.png", ["Hello", "World"], null);
        var lines = text.Document.Text.Split("\r\n");
        Assert.StartsWith("shot.png, picture (PNG): the text Windows' reader found follows.", lines[0]);
        Assert.Equal(string.Empty, lines[1]);
        Assert.Equal(["Hello", "World"], lines[2..]);
        Assert.Equal(["Text lines: 2"], text.Contents);
        Assert.Equal("Text read: 2 lines", text.Said);
        Assert.Equal("Text read: 1 line", PictureText.Finished("a.png", ["one"], null).Said);

        var none = PictureText.Finished(@"C:\pictures\blank.png", [], null);
        Assert.StartsWith("No text was found in blank.png.", none.Document.Text);
        Assert.Equal("No text found in the picture", none.Said);

        var failed = PictureText.Finished(@"C:\pictures\shot.png", null, "boom");
        Assert.StartsWith("The text of shot.png could not be read: boom", failed.Document.Text);
        Assert.Equal("Text not read: boom", failed.Said);
    }

    [Fact]
    public async Task Windows_reader_reads_the_fixture_when_this_Windows_has_a_language()
    {
        var script = Path.Combine(Path.GetTempPath(), "axpreview-tests", Guid.NewGuid().ToString("N"), "ocr.ps1");
        var picture = Path.Combine(RepositoryRoot(), "tests", "fixtures", "picture-text.png");
        Assert.True(File.Exists(picture), picture);
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var (lines, problem) = await PictureText.RecognizeAsync(picture, limit.Token, script);
        Assert.True(File.Exists(script));
        Directory.Delete(Path.GetDirectoryName(script)!, recursive: true);
        if (problem == PictureText.NoLanguage)
        {
            // A Windows without a recognition language (a build server): the script ran and said so; nothing more to check here.
            return;
        }

        Assert.Null(problem);
        Assert.Contains(lines!, line => line.Contains("Hello from axKit", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines!, line => line.Contains("2026", StringComparison.Ordinal));
    }

    private static string RepositoryRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "AxKit.sln")))
        {
            folder = folder.Parent;
        }

        return folder?.FullName ?? throw new InvalidOperationException("The repository root was not found above " + AppContext.BaseDirectory);
    }
}
