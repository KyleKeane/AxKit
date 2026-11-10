using AxKit.Core;

namespace AxDescribe.Core;

/// <summary>
/// One description (SPEC.md DE-3.1): the window's title and program, Claude's text, the session a follow-up resumes,
/// when; and for a picture file of the user's (DE-2.7) its path, which a follow-up's call gets as its folder, null for
/// the one picture axDescribe takes of a window (a file written before 5.6.0 has no <c>picture</c> and reads as null).
/// </summary>
public sealed record Description(string Title, string? Program, string Answer, string? SessionId, DateTime When, string? Picture = null);

/// <summary>
/// Everything axDescribe remembers between runs (SPEC.md DE-5), in <c>%APPDATA%\AxKit\axDescribe.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the hotkey, Start with Windows and Say the first match from
/// <see cref="HotkeySettings"/>, the font and the update check from <see cref="WindowSettings"/>, and the last ten
/// descriptions. The switch, the folders and the model of AI help itself are the bundle's (<c>AxKit.Core.Ai.AiSettings</c>).
/// </summary>
public sealed class DescribeSettings : HotkeySettings
{
    /// <summary>How many descriptions the list keeps (DE-3.1).</summary>
    public const int Kept = 10;

    /// <summary>Windows+Alt+S (DE-4.1).</summary>
    protected override Hotkey DefaultHotkey => new(Hotkey.Windows | Hotkey.Alt, 'S');

    /// <summary>The last descriptions, newest first.</summary>
    public List<Description> Descriptions { get; set; } = [];

    public static string DefaultPath => AppPaths.SettingsFile("axDescribe");

    public static DescribeSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<DescribeSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>A new description at the front; ten kept. <paramref name="picture"/> is a described file's path (DE-2.7), null for a window.</summary>
    public void Remember(string title, string? program, string answer, string? sessionId, DateTime when, string? picture = null)
    {
        Descriptions.Insert(0, new Description(title, program, answer, sessionId, when, picture));
        if (Descriptions.Count > Kept)
        {
            Descriptions.RemoveRange(Kept, Descriptions.Count - Kept);
        }
    }

    private void Sanitize()
    {
        SanitizeHotkey();
        Descriptions = (Descriptions ?? []).Where(description => description is { Title: not null, Answer.Length: > 0 }).Take(Kept).ToList();
    }
}
