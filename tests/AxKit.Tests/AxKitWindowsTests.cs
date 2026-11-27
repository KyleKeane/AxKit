using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The window lists of the axKit menu (docs/axKit/SPEC.md AX-9): the app from a title, Open windows and each app's windows from the Z order, and the labels.</summary>
public sealed class AxKitWindowsTests
{
    /// <summary>In Z order, the most recently used first; handle 3 is the bundle's own message box, no app's window.</summary>
    private static readonly IReadOnlyList<AxKitWindow> Windows =
    [
        new(1, "notes.md, axDown, *C:\\work\\notes.md"),
        new(2, "my - project, axClaude, C:\\my - project"),
        new(3, "axKit"),
        new(4, "axClaude"),
        new(5, "Untitled, axDown"),
    ];

    [Theory]
    [InlineData("notes.md, axDown, C:\\work\\notes.md", "axDown")]
    [InlineData("notes.md, axDown, *C:\\work\\notes.md", "axDown")]
    [InlineData("music, axFile, C:\\work\\music", "axFile")]
    [InlineData("axClaude", "axClaude")]
    [InlineData("myproject, axClaude, C:\\src\\myproject", "axClaude")]
    [InlineData("Smith, John.docx, axDown, C:\\letters\\Smith, John.docx", "axDown")]
    [InlineData("axDown, axFile, C:\\src\\axDown", "axFile")]
    [InlineData("work, axTerminal, C:\\work, PowerShell", "axTerminal")]
    [InlineData("This PC, axFile", "axFile")]
    // What is open holds an app's name as a part of its own: the app is the last one named before the path.
    [InlineData("ideas, axTask, axFile, C:\\stuff\\ideas, axTask", "axFile")]
    [InlineData("ideas, axTask.md, axDown, *C:\\stuff\\ideas, axTask.md", "axDown")]
    [InlineData("for axDown, axTask, axDown, \\\\server\\share\\for axDown, axTask", "axDown")]
    [InlineData("ideas, axTask, axDown", "axDown")]
    [InlineData("C:\\, axFile", "axFile")]
    public void The_app_is_the_part_after_what_is_open(string title, string app) => Assert.Equal(app, AxKitWindows.AppOf(title));

    [Theory]
    [InlineData("axDown", null, null, "axDown")]
    [InlineData("axDown", "Untitled", null, "Untitled, axDown")]
    [InlineData("axDown", "notes.md", "*C:\\work\\notes.md", "notes.md, axDown, *C:\\work\\notes.md")]
    [InlineData("axClaude", "myproject", "C:\\src\\myproject", "myproject, axClaude, C:\\src\\myproject")]
    [InlineData("axFile", "C:\\", "C:\\", "C:\\, axFile")]
    public void The_title_is_what_is_open_then_the_app_then_the_path(string app, string? name, string? path, string title) =>
        Assert.Equal(title, AxKitWindows.Title(app, name is null ? null : new Opened(name, path)));

    [Fact]
    public void A_path_gives_its_name_and_itself()
    {
        Assert.Equal(new Opened("myproject", "C:\\src\\myproject"), Opened.At("C:\\src\\myproject"));
        Assert.Equal("music, axFile, C:\\work\\music\\", AxKitWindows.Title("axFile", Opened.At("C:\\work\\music\\")));
    }

    [Fact]
    public void Open_windows_is_every_other_app_window_in_the_order_given()
    {
        Assert.Equal([2, 4, 5], AxKitWindows.Others(Windows, 1).Select(w => (int)w.Handle));
        Assert.Equal([1, 2, 4, 5], AxKitWindows.Others(Windows, 0).Select(w => (int)w.Handle));
    }

    [Fact]
    public void An_apps_list_keeps_the_current_window()
    {
        Assert.Equal("axClaude", Dispatch.Of(Dispatch.AxClaude).Name);
        Assert.Equal([2, 4], AxKitWindows.Of(Windows, Dispatch.Of(Dispatch.AxClaude)).Select(w => (int)w.Handle));
        Assert.Equal([1, 5], AxKitWindows.Of(Windows, Dispatch.Of(Dispatch.AxDown)).Select(w => (int)w.Handle));
    }

    [Fact]
    public void Labels_number_the_items_and_name_the_apps_with_the_letter_after_Ax()
    {
        Assert.Equal("&1. notes.md, axDown", AxKitWindows.Label(0, "notes.md, axDown"));
        Assert.Equal("&9. a", AxKitWindows.Label(8, "a"));
        Assert.Equal("&a. a", AxKitWindows.Label(9, "a"));
        Assert.Equal("&2. Q && A, axDown", AxKitWindows.Label(1, "Q & A, axDown"));
        Assert.Equal("ax&Claude, access layer for Claude Code", AxKitWindows.AppLabel(Dispatch.Of(Dispatch.AxClaude)));
        Assert.Equal("ax&Down", AxKitWindows.AppLabel(Dispatch.Of(Dispatch.AxDown)));
    }
}
