using AxKit.Core;
using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// The app keys (docs/axKit/SPEC.md AX-8.18 rule 6; the maintainer's choices of 2026-11-23): Ctrl with a function key
/// opens a standalone app, Ctrl+Shift with one an access layer, each group of apps on its own block of four keys.
/// </summary>
public sealed class AppLaunchKeysTests
{
    [Fact]
    public void Each_group_has_its_block_of_four_function_keys()
    {
        Assert.Equal(
            [
                "axStart Ctrl+F1", "axFile Ctrl+F2", "axClip Ctrl+F3", "axTemplate Ctrl+F4",
                "axDown Ctrl+F5", "axJSON Ctrl+F6", "axPreview Ctrl+F7", "axMedia Ctrl+F8",
                "axTask Ctrl+F9", "axCalc Ctrl+F10", "axProcess Ctrl+F11", "axAutomate Ctrl+F12",
                "axTerminal Ctrl+Shift+F1", "axGIT Ctrl+Shift+F2", "axPass Ctrl+Shift+F3",
                "axClaude Ctrl+Shift+F5",
                "axHelp Ctrl+Shift+F9", "axMCP Ctrl+Shift+F10",
            ],
            AppLaunchKeys.Default.Select(key => $"{key.App.Name} {key.Text}"));
    }

    [Fact]
    public void An_app_past_the_fourth_of_its_group_and_a_group_past_the_third_block_have_none()
    {
        App Make(int n, AppGroup group) => new($"axApp{n}", $"app{n}", $"AxApp{n}", group, Opens.Nothing);
        var apps = new List<App>();
        apps.AddRange(Enumerable.Range(1, 5).Select(n => Make(n, AppGroup.GettingAround)));
        apps.Add(Make(6, AppGroup.Documents));
        apps.Add(Make(7, AppGroup.Tools));
        apps.Add(Make(8, AppGroup.Programs));
        var keys = AppLaunchKeys.For(apps);
        Assert.Equal(["axApp1 Ctrl+F1", "axApp2 Ctrl+F2", "axApp3 Ctrl+F3", "axApp4 Ctrl+F4", "axApp6 Ctrl+F5", "axApp7 Ctrl+F9"], keys.Select(key => $"{key.App.Name} {key.Text}"));
    }

    [Fact]
    public void No_group_holds_more_apps_than_a_block_has_keys()
    {
        Assert.All(Dispatch.Apps.GroupBy(app => app.Group), group => Assert.InRange(group.Count(), 1, AppLaunchKeys.Block));
        Assert.Equal(Dispatch.Apps.Count, AppLaunchKeys.Default.Count);
    }

    /// <summary>
    /// The apps page (written by tools/apps-page.ps1, which works the keys out by the same rule) gives every app the
    /// key the code gives it, under the heading of its kind and group: no document holds the keys by hand (the
    /// maintainer, 2026-11-23: "make the documentation fuly automatically made from the underlyfing data").
    /// </summary>
    [Fact]
    public void The_apps_page_gives_every_app_its_key_kind_and_group()
    {
        var page = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "docs", "axKit", "apps.md")).Replace("\r\n", "\n");
        var keys = AppLaunchKeys.Default;
        foreach (var app in Dispatch.Apps)
        {
            var at = page.IndexOf($"\n### {app.Name}\n", StringComparison.Ordinal);
            Assert.True(at > 0, $"{app.Name} has no heading in apps.md: run tools/apps-page.ps1");
            var heading = page[..at].Split('\n').Last(line => line.StartsWith("## ", StringComparison.Ordinal));
            Assert.Equal($"## {(app.IsAccessLayer ? "Access layers" : "Apps")}: {Dispatch.GroupName(app.Group)}", heading);
            var next = page.IndexOf("\n### ", at + 1, StringComparison.Ordinal);
            var section = next < 0 ? page[at..] : page[at..next];
            var key = keys.First(k => k.App == app);
            Assert.Contains($"- key {key.Text} in every axKit window;", section);
        }
    }

    /// <summary>
    /// The command inventory (docs/axKit/window-commands.json, written by the window runs, which the key map is
    /// written from) holds the app keys as the code gives them and no submenu of the Window menu the code no longer
    /// has: a run with --inventory write never removes a row, and the New window rows with the keys of the first
    /// assignment stayed in it unseen on 2026-11-23 (the cure is a run with --inventory rewrite).
    /// </summary>
    [Fact]
    public void The_command_inventory_holds_the_app_keys_as_the_code_gives_them()
    {
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "docs", "axKit", "window-commands.json")));
        string[] windowSubmenus = ["Window, Open windows", "Window, Recently closed"];
        var wrong = new List<string>();
        foreach (var app in document.RootElement.GetProperty("apps").EnumerateArray())
        {
            foreach (var row in app.GetProperty("commands").EnumerateArray())
            {
                var menu = row.GetProperty("menu").GetString()!;
                var label = row.GetProperty("label").GetString()!;
                if (menu.StartsWith("Window, ", StringComparison.Ordinal) && !windowSubmenus.Contains(menu))
                {
                    wrong.Add($"{app.GetProperty("app").GetString()}: {label} under {menu}, a submenu the Window menu no longer has");
                }

                if (menu is "axKit, Apps" or "axKit, Access layers")
                {
                    var key = AppLaunchKeys.Default.FirstOrDefault(k => k.App.Title == label);
                    var expected = key?.Text ?? string.Empty;
                    if (row.GetProperty("keyText").GetString() != expected)
                    {
                        wrong.Add($"{app.GetProperty("app").GetString()}: {label} with {row.GetProperty("keyText").GetString()}, the code gives {expected}");
                    }
                }
            }
        }

        Assert.True(wrong.Count == 0, "The command inventory is stale; run the window scripts with --inventory rewrite:\n" + string.Join("\n", wrong.Take(10)));
    }

    [Fact]
    public void The_bundle_keys_hold_every_app_key_and_each_finds_its_app()
    {
        foreach (var key in AppLaunchKeys.Default)
        {
            var keys = BundleKeys.LaunchKey(key);
            Assert.Contains(BundleKeys.All, entry => entry.Key == keys);
            Assert.Equal(key.App, BundleKeys.AppOfLaunchKey(keys));
        }

        Assert.Equal(Keys.Control | Keys.F2, BundleKeys.LaunchKey(AppLaunchKeys.Of("axFile")!));
        Assert.Null(BundleKeys.AppOfLaunchKey(Keys.F1));
        Assert.Null(BundleKeys.AppOfLaunchKey(Keys.Control | Keys.Shift | Keys.F4));
    }

    // The user's own table, app-keys.json (AX-8.18 rule 6; the maintainer's decision of 2026-11-25).

    private static readonly IReadOnlyList<(string Name, string Chord)> Bundle = [.. BundleKeys.Fixed.Select(key => (key.Name, BundleKeys.Chord(key.Key)))];

    private static string KeyOf(AppKeysResolved resolved, string app) =>
        resolved.Keys.FirstOrDefault(key => key.App.Name == app)?.Text ?? "none";

    [Fact]
    public void The_tests_and_a_process_without_the_file_have_the_defaults()
    {
        Assert.Same(AppLaunchKeys.Default, AppLaunchKeys.Current);
        Assert.Equal(string.Empty, AppLaunchKeys.Changes(AppLaunchKeys.Default));
    }

    [Fact]
    public void An_entry_gives_its_app_a_key_none_takes_it_away_and_an_app_left_out_keeps_its_default()
    {
        var resolved = AppLaunchKeys.Resolve("""{ "about": "anything", "axfile": "ctrl+f9", "axTask": "Ctrl + F2", "axCalc": "None", "axGIT": "Ctrl+Shift+F12" }""", Bundle);
        Assert.Empty(resolved.Refused);
        Assert.Equal("Ctrl+F9", KeyOf(resolved, "axFile"));
        Assert.Equal("Ctrl+F2", KeyOf(resolved, "axTask"));
        Assert.Equal("none", KeyOf(resolved, "axCalc"));
        Assert.Equal("Ctrl+Shift+F12", KeyOf(resolved, "axGIT"));
        Assert.Equal("Ctrl+F1", KeyOf(resolved, "axStart"));
        Assert.Equal(AppLaunchKeys.Default.Count - 1, resolved.Keys.Count);
        // Each app at its default's place, so the menus and the F1 text keep the bundle's order.
        Assert.Equal(AppLaunchKeys.Default.Where(key => key.App.Name != "axCalc").Select(key => key.App), resolved.Keys.Select(key => key.App));
        Assert.Equal("axFile Ctrl+F9, axTask Ctrl+F2, axCalc no key, axGIT Ctrl+Shift+F12", AppLaunchKeys.Changes(resolved.Keys));
    }

    [Fact]
    public void Two_apps_may_swap_their_keys_in_one_file()
    {
        var resolved = AppLaunchKeys.Resolve("""{ "axStart": "Ctrl+F2", "axFile": "Ctrl+F1" }""", Bundle);
        Assert.Empty(resolved.Refused);
        Assert.Equal("Ctrl+F2", KeyOf(resolved, "axStart"));
        Assert.Equal("Ctrl+F1", KeyOf(resolved, "axFile"));
    }

    [Fact]
    public void Each_refused_entry_is_skipped_with_its_reason_and_its_app_keeps_its_default()
    {
        var resolved = AppLaunchKeys.Resolve(
            """
            {
              "axNothing": "Ctrl+F4",
              "axFile": "Ctrl+Banana",
              "axDown": 7,
              "axJSON": "Alt+F6",
              "axMedia": "Ctrl+Shift+F8",
              "axGIT": "Ctrl+F2",
              "axPreview": "Ctrl+A",
              "axCalc": "Ctrl+F7",
              "axTask": "Ctrl+F1",
              "axClip": "none",
              "axClip": "Ctrl+F3"
            }
            """,
            [("Thing", "ctrl+f7")]);
        Assert.Equal(
            [
                "\"axNothing\" names no app of axKit; the entry is skipped",
                "axFile: \"Ctrl+Banana\" is no key axKit can read, such as Ctrl+F2; axFile keeps its default key",
                "axDown: \"7\" is no key axKit can read, such as Ctrl+F2; axDown keeps its default key",
                "axJSON: Alt+F6 is not Ctrl with a function key, F1 to F12, a standalone app's kind of key; axJSON keeps its default key",
                "axMedia: Ctrl+Shift+F8 is not Ctrl with a function key, F1 to F12, a standalone app's kind of key; axMedia keeps its default key",
                "axGIT: Ctrl+F2 is not Ctrl+Shift with a function key, F1 to F12, an access layer's kind of key; axGIT keeps its default key",
                "axPreview: Ctrl+A is not Ctrl with a function key, F1 to F12, a standalone app's kind of key; axPreview keeps its default key",
                "axCalc: Ctrl+F7 is the bundle's key for Thing; axCalc keeps its default key",
                "axClip is named a second time; the second entry is skipped",
                "axTask: Ctrl+F1 is axStart's key; give axStart another key or none in the same file; axTask keeps its default key",
            ],
            resolved.Refused);
        // axClip's first entry holds: no key; every other app keeps its default.
        Assert.Equal(AppLaunchKeys.Default.Where(key => key.App.Name != "axClip"), resolved.Keys);
        Assert.Equal("none", KeyOf(resolved, "axClip"));
        Assert.Equal("Ctrl+F2", KeyOf(resolved, "axFile"));
        Assert.Equal("Ctrl+F9", KeyOf(resolved, "axTask"));
    }

    [Fact]
    public void A_key_given_twice_holds_for_the_first_and_a_refusal_that_keeps_a_default_refuses_what_took_it()
    {
        // axDown's entry is refused for Ctrl+F2, so axDown keeps Ctrl+F5, which axFile's entry asked for: refused too.
        var resolved = AppLaunchKeys.Resolve("""{ "axStart": "Ctrl+F12", "axFile": "Ctrl+F5", "axDown": "Ctrl+F12" }""", Bundle);
        Assert.Equal(
            [
                "axDown: Ctrl+F12 is given to axStart before it in the file; axDown keeps its default key",
                "axStart: Ctrl+F12 is axAutomate's key; give axAutomate another key or none in the same file; axStart keeps its default key",
                "axFile: Ctrl+F5 is axDown's key; give axDown another key or none in the same file; axFile keeps its default key",
            ],
            resolved.Refused);
        Assert.Equal(AppLaunchKeys.Default.Select(key => key.Text), resolved.Keys.Select(key => key.Text));
    }

    [Fact]
    public void A_file_that_is_no_JSON_object_gives_the_defaults_with_one_reason()
    {
        var broken = AppLaunchKeys.Resolve("{ \"axFile\": ", Bundle);
        Assert.Same(AppLaunchKeys.Default, broken.Keys);
        Assert.Single(broken.Refused);
        Assert.StartsWith("the file is no JSON (", broken.Refused[0]);
        var list = AppLaunchKeys.Resolve("[\"axFile\"]", Bundle);
        Assert.Same(AppLaunchKeys.Default, list.Keys);
        Assert.Equal(["the file is no JSON object of app names and keys; the default app keys hold"], list.Refused);
    }

    [Fact]
    public void The_template_writes_every_app_with_its_key_and_reads_back_as_the_defaults()
    {
        var template = AppLaunchKeys.Template(AppLaunchKeys.Default);
        Assert.Contains($"  \"about\": \"{AppLaunchKeys.AboutText}\",\r\n", template);
        Assert.Contains("  \"axFile\": \"Ctrl+F2\",\r\n", template);
        Assert.Contains("  \"axMCP\": \"Ctrl+Shift+F10\"\r\n}", template);
        var resolved = AppLaunchKeys.Resolve(template, Bundle);
        Assert.Empty(resolved.Refused);
        Assert.Equal(AppLaunchKeys.Default, resolved.Keys);
        var none = AppLaunchKeys.Template(AppLaunchKeys.Resolve("""{ "axCalc": "none" }""", Bundle).Keys);
        Assert.Contains("  \"axCalc\": \"none\",\r\n", none);
    }

    [Fact]
    public void The_F1_line_names_the_keys_the_file_changed()
    {
        Assert.DoesNotContain("your own keys:", BundleHelp.AppKeysLine);
        Assert.Contains("axKit, Edit app keys file", BundleHelp.AppKeysLine);
        var line = BundleHelp.AppKeysLineFor(AppLaunchKeys.Resolve("""{ "axFile": "Ctrl+F11", "axProcess": "none" }""", Bundle).Keys);
        Assert.Contains(". In place of these, your own keys: axFile Ctrl+F11, axProcess no key. Alt+K, A", line);
    }
}
