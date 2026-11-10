using System.Text.RegularExpressions;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The theme file (docs/axKit/theme.md TH-1, TH-5): what is read, what is kept, and that no app paints a colour of its own.</summary>
public sealed class ThemeTests
{
    [Fact]
    public void The_template_is_a_theme_with_every_class_at_system()
    {
        var theme = Theme.Read(Theme.Template(), out var problem);
        Assert.Null(problem);
        Assert.NotNull(theme);
        Assert.True(theme!.Enabled);
        Assert.Empty(theme.NotUnderstood);
        Assert.All(Theme.Keys, key => Assert.True(theme.Color(key).IsSystem));
        Assert.Equal(22, Theme.Keys.Count);
        Assert.Contains("field.background", Theme.Keys);
        Assert.Contains("titleBar.foreground", Theme.Keys);
        Assert.True(theme.WordWrap);
        Assert.Contains("\"display\": {", Theme.Template());
    }

    /// <summary>docs/axKit/theme.md TH-6: the display settings, read whatever enabled says, written into a file with or without them.</summary>
    [Fact]
    public void Word_wrap_is_read_from_the_display_settings_and_written_beside_what_is_there()
    {
        var off = Theme.Read("""{ "format": "axkit-theme 1", "enabled": false, "display": { "wordWrap": false } }""", out var problem);
        Assert.Null(problem);
        Assert.False(off!.WordWrap);
        Assert.False(off.Enabled);
        Assert.True(Theme.Read("""{ "format": "axkit-theme 1" }""", out _)!.WordWrap);
        var odd = Theme.Read("""{ "format": "axkit-theme 1", "colors": { "text.foreground": "red" }, "display": { "wordWrap": "no" } }""", out _);
        Assert.True(odd!.WordWrap);
        Assert.Equal(["text.foreground", "display.wordWrap"], odd.NotUnderstood);

        var path = Path.Combine(Path.GetTempPath(), "axkit-tests", Guid.NewGuid().ToString("N"), "theme.json");
        // No file: the template with the value as asked.
        Assert.Null(Theme.SetWordWrap(path, false));
        Assert.False(Theme.Load(path, out _)!.WordWrap);
        Assert.True(Theme.Load(path, out _)!.Enabled);
        // A file without a display object gets one; its colours and its own keys stay.
        File.WriteAllText(path, """{ "format": "axkit-theme 1", "note": "mine", "colors": { "text.background": "#102030" } }""");
        Assert.Null(Theme.SetWordWrap(path, false));
        var written = File.ReadAllText(path);
        Assert.Contains("\"note\": \"mine\"", written);
        Assert.Contains("\"wordWrap\": false", written);
        Assert.Equal(0x102030, Theme.Load(path, out _)!.Color("text.background").Rgb);
        Assert.Null(Theme.SetWordWrap(path, true));
        Assert.True(Theme.Load(path, out _)!.WordWrap);
        Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    }

    [Fact]
    public void A_hex_colour_is_read_system_in_any_case_and_a_bad_value_is_system_and_counted()
    {
        var theme = Theme.Read("""
            {
              "format": "axkit-theme 1",
              "name": "Mine",
              "colors": {
                "text.background": "#1a2B3c",
                "text.foreground": " SYSTEM ",
                "list.background": "#GGGGGG",
                "list.foreground": "blue",
                "window.background": 12
              }
            }
            """, out var problem);
        Assert.Null(problem);
        Assert.NotNull(theme);
        Assert.Equal("Mine", theme!.Name);
        Assert.True(theme.Enabled);
        Assert.Equal(0x1A2B3C, theme.Color("text.background").Rgb);
        Assert.True(theme.Color("text.foreground").IsSystem);
        Assert.True(theme.Color("list.background").IsSystem);
        Assert.True(theme.Color("menu.background").IsSystem);
        Assert.Equal(["list.background", "list.foreground", "window.background"], theme.NotUnderstood);
    }

    [Fact]
    public void Enabled_false_is_read_and_a_file_that_is_not_a_theme_is_refused_with_the_reason()
    {
        var off = Theme.Read("""{ "format": "axkit-theme 1", "enabled": false }""", out var problem);
        Assert.Null(problem);
        Assert.False(off!.Enabled);

        Assert.Null(Theme.Read("not json", out problem));
        Assert.StartsWith("not JSON", problem);
        Assert.Null(Theme.Read("""{ "format": "axkit-item 1" }""", out problem));
        Assert.Contains("not a theme file", problem);
        Assert.Null(Theme.Read("[1, 2]", out problem));
        Assert.StartsWith("not JSON", problem);
    }

    [Fact]
    public void Colours_parse_both_ways()
    {
        Assert.True(Theme.ParseColor("#FFFFFF", out var white));
        Assert.Equal(0xFFFFFF, white);
        Assert.True(Theme.ParseColor("#000000", out var black));
        Assert.Equal(0, black);
        Assert.False(Theme.ParseColor("FFFFFF", out _));
        Assert.False(Theme.ParseColor("#FFF", out _));
        Assert.False(Theme.ParseColor("#FFFFFFFF", out _));
        Assert.False(Theme.ParseColor("rgb(1,2,3)", out _));
        Assert.Equal("#1A2B3C", Theme.ColorText(0x1A2B3C));
        Assert.Equal("#000007", Theme.ColorText(7));
    }

    [Fact]
    public void Load_gives_no_theme_for_a_missing_file_and_SetEnabled_keeps_what_it_does_not_know()
    {
        var path = Path.Combine(Path.GetTempPath(), "axkit-tests", Guid.NewGuid().ToString("N"), "theme.json");
        Assert.Null(Theme.Load(path, out var problem));
        Assert.Null(problem);

        // No file: the template is written with the switch as asked.
        Assert.Null(Theme.SetEnabled(path, false));
        var theme = Theme.Load(path, out problem);
        Assert.Null(problem);
        Assert.False(theme!.Enabled);
        Assert.All(Theme.Keys, key => Assert.True(theme.Color(key).IsSystem));

        // A user's own key and a colour survive the switch being written.
        File.WriteAllText(path, """
            {
              "format": "axkit-theme 1",
              "enabled": false,
              "note": "mine",
              "colors": { "text.background": "#102030", "text.extra": "kept" }
            }
            """);
        Assert.Null(Theme.SetEnabled(path, true));
        var written = File.ReadAllText(path);
        Assert.Contains("\"note\": \"mine\"", written);
        Assert.Contains("\"text.extra\": \"kept\"", written);
        Assert.Contains("\"enabled\": true", written);
        Assert.Equal(0x102030, Theme.Load(path, out _)!.Color("text.background").Rgb);

        // A file that is not JSON is left alone, with the reason.
        File.WriteAllText(path, "broken");
        Assert.NotNull(Theme.SetEnabled(path, true));
        Assert.Equal("broken", File.ReadAllText(path));
        Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    }

    /// <summary>The files that may name a colour: the painter, which holds every class's default, and axMedia's black video surface (AM-2.4).</summary>
    private static readonly IReadOnlySet<string> AllowedToColour = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Path.Combine("src", "AxKit.Forms", "ThemePainter.cs"),
        Path.Combine("src", "AxMedia", "MediaForm.cs"),
    };

    [Fact]
    public void No_app_paints_a_colour_of_its_own()
    {
        // docs/axKit/theme.md TH-5.2, bundle AX-7.7: a colour comes from the theme's classes, never from a window's code.
        var root = LayoutTests.RepositoryRoot();
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || AllowedToColour.Contains(relative))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (Regex.IsMatch(line, @"\bColor\.(?:FromArgb|FromName|FromKnownColor|[A-Z][a-zA-Z]+)\b"))
                {
                    offenders.Add($"{relative}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "A colour set outside the theme's classes (docs/axKit/theme.md TH-5.2): " + string.Join("; ", offenders));
    }
}
