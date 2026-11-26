using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The first line and the bundle's section of every F1 text (docs/axKit/app-kit.md AK-2.3, AX-7.2).</summary>
public sealed class BundleHelpTests
{
    private static string[] Lines(string text) => text.Split("\r\n");

    /// <summary>The lines of an F1 text after the rules of the keys (AX-8.18), which every text starts with, and the blank line after them.</summary>
    private static string[] AfterRules(string text)
    {
        Assert.StartsWith(BundleHelp.RulesSection + "\r\n", text);
        return Lines(text)[Lines(BundleHelp.RulesSection).Length..];
    }

    [Fact]
    public void The_section_has_the_header_the_heading_and_the_bundle_lines_around_the_apps_own()
    {
        var lines = Lines(BundleHelp.Section("axThing", ["Tab: between the list and the field", "Ctrl+S: save the thing"]));
        Assert.Equal(
            [
                .. Lines(BundleHelp.RulesSection.TrimEnd('\r', '\n')),
                "",
                "In this text: h and Shift+H move by heading, 1 to 6 by level. Ctrl+F finds, F3 and Shift+F3 the next and the previous match",
                "",
                "## In every window of axKit",
                "  F1: this list",
                "  How to do a task: Help, User guide is axThing's guide; with AI help on, Ask about this window answers from the steps of axThing's tasks.",
                "  Ctrl+Shift+P: the command palette: type part of a command's name, Down and Up through the matches, Enter runs it",
                "  F4: report a bug, an idea or feedback to axKit's maintainer: type your words, Enter makes a new line; Tab goes on to the kind, a box for more detail and what is sent with your report; Ctrl+Enter or the Send button sends; Escape cancels and keeps nothing. The report is encrypted on your computer, and only the maintainer can read it",
                "  Ctrl+Enter: the main action of where you are: it sends a form from any of its fields, keeps a long text from inside its box, presses a notice's main button, and does a window's main action where it has one; anywhere else, the edge sound and nothing more",
                BundleHelp.ModeLine,
                "  Shift+F1 or Ctrl+Slash: ask Claude about this window: type your question and press Enter; the first time, a reminder asks you to turn Claude on; the answer is a notice, with a field under it to ask more",
                BundleHelp.ReadLine,
                "  F9: say the status, what the status bar at the bottom of the window holds, said by axKit itself; in a notice too",
                BundleHelp.AppKeysLine,
                "  Tab: between the list and the field",
                "  Ctrl+S: save the thing",
                "  Ctrl+Plus, Ctrl+Minus: bigger or smaller text",
                "  A line break typed into a field of several lines is said, new line; Options, Text, Say new line turns that off for every window",
                "  A key that lands on a heading, in a list or a text, plays a short rising sound before it is read; Options, Text, Sound on headings turns that off for every window",
                BundleHelp.NoticeLine,
                "  Shift+F10, the Applications key or Ctrl+Period: the Context menu, what acts on what you are on",
                "  A key that seems to do nothing: Help, Diagnostics, Test a key says each key you press and what it does here, and carries none of them out; Escape ends it. A key that is not said never reached axKit",
                "  Alt: the menu. Alt+F4: close axThing",
                "  " + BundleHelp.BundleMenusLine,
                "",
                "",
            ],
            lines);
    }

    [Fact]
    public void The_menus_line_names_each_bundle_menu_by_its_own_letter()
    {
        // The letters come from the menu bar's own names (AxKitForm.BarMenus), so that a menu given another letter
        // fails here: the line said Alt+A for the axKit menu from 2026-11-11, when its letter became K, to 2026-11-23.
        static string Alt(string menu) => "Alt+" + char.ToUpperInvariant(menu[menu.IndexOf('&') + 1]);
        var bar = AxKit.Forms.AxKitForm.BarMenus;
        Assert.Contains($"{Alt(bar[5])}: the Window menu", BundleHelp.BundleMenusLine);
        Assert.Contains($"{Alt(bar[6])}: the axKit menu: Apps and Access layers", BundleHelp.BundleMenusLine);
        Assert.Contains($"{Alt(bar[7])}: Help", BundleHelp.BundleMenusLine);
        Assert.Contains($"{Alt(bar[6])}, A lists the apps", BundleHelp.AppKeysLine);
    }

    [Fact]
    public void The_app_names_what_Alt_F4_does_the_heading_and_the_Navigation_menu()
    {
        var lines = AfterRules(BundleHelp.Section("axThing", [], altF4: "hide axThing", heading: "Anywhere", navigateMenu: true));
        Assert.Equal("In this text: h and Shift+H move by heading, 1 to 6 by level; the Next and Previous keys of the Navigation menu work here too. Ctrl+F finds, F3 and Shift+F3 the next and the previous match", lines[0]);
        Assert.Equal("## Anywhere", lines[2]);
        Assert.Contains("  Alt: the menu. Alt+F4: hide axThing", lines);
        Assert.DoesNotContain(lines, line => line.Contains("close axThing", StringComparison.Ordinal));
    }

    /// <summary>Every app's F1 text starts with the builder's part, so that the bundle's lines are written once.</summary>
    [Fact]
    public void Every_F1_text_starts_with_the_bundle_section()
    {
        string[] texts =
        [
            AxCalc.HelpText.Shortcuts, AxClaude.HelpText.Shortcuts, AxClip.HelpText.Shortcuts, AxDown.HelpText.Shortcuts,
            AxFile.HelpText.Shortcuts, AxGit.HelpText.Shortcuts, AxJSON.HelpText.Shortcuts, AxMedia.HelpText.Shortcuts,
            AxPass.HelpText.Shortcuts, AxPreview.HelpText.Shortcuts, AxProcess.HelpText.Shortcuts, AxStart.HelpText.Shortcuts,
            AxTask.HelpText.Shortcuts, AxTerminal.HelpText.Shortcuts,
        ];
        foreach (var text in texts)
        {
            // The rules of the keys come first (AX-8.18), the same in every text, then a blank line.
            var all = AfterRules(text);
            Assert.StartsWith("In this text: h and Shift+H move by heading, 1 to 6 by level", all[0]);
            // An access layer's second line says what it is (AX-11.2); the rest is the same for every app.
            var layer = Dispatch.Apps.FirstOrDefault(app => app.IsAccessLayer && all[1].StartsWith(app.Title + ": ", StringComparison.Ordinal));
            var lines = layer is null ? all : [all[0], .. all[2..]];
            Assert.StartsWith("## ", lines[2]);
            Assert.Equal("  F1: this list", lines[3]);
            var axkitMenu = Array.IndexOf(lines, "  " + BundleHelp.BundleMenusLine);
            Assert.True(axkitMenu > 3, "No axKit menu line in the bundle section: " + lines[2]);
            Assert.StartsWith("  Alt: the menu. Alt+F4: ", lines[axkitMenu - 1]);
            Assert.Equal("", lines[axkitMenu + 1]);
            Assert.StartsWith("## ", lines[axkitMenu + 2]);
        }

        Assert.Equal(4, texts.Count(text => Dispatch.Apps.Any(app => app.IsAccessLayer && AfterRules(text)[1].StartsWith(app.Title + ": ", StringComparison.Ordinal))));

        // Every text ends with the bundle's Notices section, the same words in every window (AX-7.2, 2026-11-09).
        foreach (var text in texts)
        {
            var lines = Lines(text);
            Assert.Equal("## Notices", lines.Last(line => line.StartsWith("## ", StringComparison.Ordinal)));
            Assert.Equal("", lines[Array.IndexOf(lines, "## Notices") - 1]);
            Assert.Contains("'s questions, errors and help texts appear inside the window, in place of ", lines[^2]);
            Assert.StartsWith("  Tab moves between the text, a field and the buttons. Enter chooses the main button. Escape closes the notice. In a notice's text, h, 1 to 6", lines[^1]);
            Assert.EndsWith(" and Ctrl+F work as in this one", lines[^1]);
        }
    }
}
