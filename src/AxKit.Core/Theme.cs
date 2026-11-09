using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AxKit.Core;

/// <summary>One colour of a theme (docs/axkit/theme.md TH-1.3): Windows' own colour for the class, or an RGB value.</summary>
public readonly record struct ThemeColor(int? Rgb)
{
    public static readonly ThemeColor System = new(null);

    public bool IsSystem => Rgb is null;
}

/// <summary>
/// The theme file (docs/axkit/theme.md): <c>%APPDATA%\AxKit\theme.json</c>, one for the bundle, with a colour or
/// <c>system</c> for every class of object the windows draw (TH-2). Read here, painted by <c>AxKit.Forms.ThemePainter</c>.
/// No file, or <c>system</c> everywhere, is no change at all (AX-7.7). Keys the bundle does not know are kept when the
/// file is written again (TH-1.4); a value not understood is <c>system</c> and counted, for Reload's sentence.
/// </summary>
public sealed class Theme
{
    public const string Format = "axkit-theme 1";

    /// <summary>The classes of object (TH-2), in the file's order; each has <c>.background</c> and <c>.foreground</c>.</summary>
    public static readonly IReadOnlyList<string> Classes =
        ["window", "text", "transcript", "list", "field", "band", "notice", "status", "menu", "button", "titleBar"];

    /// <summary>Every key of the file's <c>colors</c> object: the classes with their two sides.</summary>
    public static readonly IReadOnlyList<string> Keys =
        [.. Classes.SelectMany(name => new[] { name + ".background", name + ".foreground" })];

    private const string SystemWord = "system";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly Dictionary<string, ThemeColor> _colors;

    private Theme(bool enabled, string name, Dictionary<string, ThemeColor> colors, IReadOnlyList<string> notUnderstood)
    {
        Enabled = enabled;
        Name = name;
        _colors = colors;
        NotUnderstood = notUnderstood;
    }

    /// <summary>TH-1.5: false keeps the file and paints nothing.</summary>
    public bool Enabled { get; }

    public string Name { get; }

    /// <summary>The keys whose value was not understood, in the file's order (TH-1.4).</summary>
    public IReadOnlyList<string> NotUnderstood { get; }

    /// <summary>The colour of a key, <c>window.background</c>; <see cref="ThemeColor.System"/> for a key missing or not understood.</summary>
    public ThemeColor Color(string key) => _colors.TryGetValue(key, out var colour) ? colour : ThemeColor.System;

    /// <summary>
    /// The theme in <paramref name="path"/>, or null with <paramref name="problem"/> null when there is no file, and
    /// null with the reason when the file is not a theme (TH-1.6).
    /// </summary>
    public static Theme? Load(string path, out string? problem)
    {
        if (!File.Exists(path))
        {
            problem = null;
            return null;
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            problem = ex.Message;
            return null;
        }

        return Read(text, out problem);
    }

    /// <summary>The theme in a file's text; null with the reason when it is not one.</summary>
    public static Theme? Read(string text, out string? problem)
    {
        JsonObject root;
        try
        {
            root = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) as JsonObject
                ?? throw new JsonException("not a JSON object");
        }
        catch (JsonException ex)
        {
            problem = "not JSON: " + ex.Message;
            return null;
        }

        if (TextOf(root["format"]) is not Format)
        {
            problem = $"not a theme file: its format is not \"{Format}\"";
            return null;
        }

        problem = null;
        var enabled = root["enabled"] is not JsonValue flag || !flag.TryGetValue<bool>(out var value) || value;
        var colors = new Dictionary<string, ThemeColor>(StringComparer.Ordinal);
        var notUnderstood = new List<string>();
        if (root["colors"] is JsonObject given)
        {
            // In the file's order (TH-1.4), so that Reload names the values not understood as the user reads them.
            foreach (var (key, node) in given)
            {
                if (!Keys.Contains(key) || node is null)
                {
                    continue;
                }

                if (TextOf(node) is { } word && string.Equals(word.Trim(), SystemWord, StringComparison.OrdinalIgnoreCase))
                {
                    colors[key] = ThemeColor.System;
                }
                else if (TextOf(node) is { } hex && ParseColor(hex, out var rgb))
                {
                    colors[key] = new ThemeColor(rgb);
                }
                else
                {
                    notUnderstood.Add(key);
                }
            }
        }

        return new Theme(enabled, TextOf(root["name"]) ?? string.Empty, colors, notUnderstood);
    }

    /// <summary>A whole file with every class at <c>system</c> (TH-1.2), for Edit theme file when there is none.</summary>
    public static string Template()
    {
        var colors = new JsonObject();
        foreach (var key in Keys)
        {
            colors[key] = SystemWord;
        }

        var root = new JsonObject
        {
            ["format"] = Format,
            ["enabled"] = true,
            ["name"] = "My theme",
            ["colors"] = colors,
        };
        return ToText(root);
    }

    /// <summary>
    /// Writes <c>enabled</c> into the file, every other key kept as it is (TH-1.4, TH-1.5); the template first when
    /// there is no file. The reason when the file could not be changed (not JSON, not writable), null when it was.
    /// </summary>
    public static string? SetEnabled(string path, bool enabled)
    {
        try
        {
            JsonObject root;
            if (File.Exists(path))
            {
                if (JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }) is not JsonObject existing)
                {
                    return "the theme file is not a JSON object";
                }

                root = existing;
            }
            else
            {
                root = (JsonObject)JsonNode.Parse(Template())!;
            }

            root["enabled"] = enabled;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, ToText(root));
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return ex.Message;
        }
    }

    /// <summary>A hex colour <c>#RRGGBB</c> to its RGB value (TH-1.3); false for anything else.</summary>
    public static bool ParseColor(string text, out int rgb)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 7 && trimmed[0] == '#' && int.TryParse(trimmed.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
        {
            return true;
        }

        rgb = 0;
        return false;
    }

    /// <summary>An RGB value as the file writes it, <c>#1A2B3C</c>.</summary>
    public static string ColorText(int rgb) => "#" + (rgb & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture);

    private static string ToText(JsonObject root) => root.ToJsonString(Indented).ReplaceLineEndings("\r\n") + "\r\n";

    private static string? TextOf(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
