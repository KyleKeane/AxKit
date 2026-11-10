using AxKit.Core;

namespace AxHelp.Core;

/// <summary>One question and its answer (SPEC.md HE-3.1), kept for the list; the session is what a follow-up resumes.</summary>
public sealed record HelpExchange(string Question, string Answer, string? SessionId, DateTime When);

/// <summary>
/// Everything axHelp remembers between runs (SPEC.md HE-5), in <c>%APPDATA%\AxKit\axHelp.json</c> through the
/// bundle's <see cref="SettingsFile"/>: the hotkey, Start with Windows and Say the first match from
/// <see cref="HotkeySettings"/>, the font and the update check from <see cref="WindowSettings"/>, and the last ten
/// questions with their answers. The switch, the folders and the model of AI help itself are the bundle's
/// (<c>AxKit.Core.Ai.AiSettings</c>), never here.
/// </summary>
public sealed class HelpSettings : HotkeySettings
{
    /// <summary>How many exchanges the list keeps (HE-3.1).</summary>
    public const int Kept = 10;

    /// <summary>Nothing is said while a question is typed (HE-3.3; 2026-11-09: the user heard "No questions yet" in the middle of typing one); the file's <c>sayFirstMatch</c> turns it on.</summary>
    public HelpSettings() => SayFirstMatch = false;

    /// <summary>Windows+Alt+H (HE-4.1).</summary>
    protected override Hotkey DefaultHotkey => new(Hotkey.Windows | Hotkey.Alt, 'H');

    /// <summary>The last questions with their answers, newest first.</summary>
    public List<HelpExchange> Exchanges { get; set; } = [];

    public static string DefaultPath => AppPaths.SettingsFile("axHelp");

    public static HelpSettings Load(string path, out string? error)
    {
        var settings = SettingsFile.Load<HelpSettings>(path, out error);
        settings.Sanitize();
        return settings;
    }

    public override void Save(string path) => SettingsFile.Save(path, this);

    /// <summary>A new exchange at the front; the same question asked again moves there with its new answer; ten kept.</summary>
    public void Remember(string question, string answer, string? sessionId, DateTime when)
    {
        Exchanges.RemoveAll(exchange => string.Equals(exchange.Question, question, StringComparison.OrdinalIgnoreCase));
        Exchanges.Insert(0, new HelpExchange(question, answer, sessionId, when));
        if (Exchanges.Count > Kept)
        {
            Exchanges.RemoveRange(Kept, Exchanges.Count - Kept);
        }
    }

    private void Sanitize()
    {
        SanitizeHotkey();
        Exchanges = (Exchanges ?? []).Where(exchange => exchange is { Question.Length: > 0, Answer: not null }).Take(Kept).ToList();
    }
}
