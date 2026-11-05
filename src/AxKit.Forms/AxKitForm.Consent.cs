using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The reminder before an access layer first connects to a program or service (docs/axkit/SPEC.md AX-11.4, AX-11.5):
/// a notice in the window with one check box and two buttons, once per app and program, kept in
/// <see cref="AppPaths.ConsentFile"/>. Install it now (AX-2.9) and Add axKit to Claude Code ask the same way.
/// </summary>
public abstract partial class AxKitForm
{
    /// <summary>
    /// The programs agreed to in this window since it opened: an agreement counts for the session even when the file
    /// could not be written (a read-only profile, a full disk), where the same notice came back for ever (review,
    /// 2026-11-02); the user is asked again next time, as <see cref="Consent"/> says.
    /// </summary>
    private readonly HashSet<string> _agreed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// True when the user has agreed to <paramref name="target"/> for this app, so that the caller goes on. Otherwise the
    /// reminder is shown and false comes back: Connect (Enter, once the box is checked) records the agreement and runs
    /// <paramref name="connect"/>, the caller itself, which then comes through here and goes on; Not now (Escape) runs
    /// <paramref name="notNow"/>, which says how to connect later. One check, here, for a connection and for Install it
    /// now alike (the overload takes the title and the button); a caller that checked first as well had made this one
    /// dead (review, 2026-11-01).
    /// </summary>
    protected bool HasConsent(ConsentTarget target, Action connect, Action notNow) =>
        HasConsent(target, $"Before {AppName} connects to {target.Program}", "&Connect", connect, notNow);

    protected bool HasConsent(ConsentTarget target, string title, string button, Action connect, Action notNow)
    {
        if (_agreed.Contains(target.Program) || Consent.Given(AppPaths.ConsentFile, AppName, target.Program))
        {
            return true;
        }

        ShowNotice(ConsentNotice(target, title, button, connect, notNow));
        return false;
    }

    /// <summary>The check box's sentence (AX-11.4), the same under the AI tools' pairing notice, where axKit as a whole is the access layer.</summary>
    internal static string ConsentBox(string who, string program) =>
        $"I understand that {who} is only an access layer, and that following the terms of {program} is up to me";

    /// <summary>
    /// The notice of AX-11.4, a plain reminder: what the access layer does, whose program does the work and under whose
    /// terms, with their address when one is known, the check box, and two buttons. <paramref name="action"/> names the
    /// default button (Connect, Install), which records the agreement before it runs <paramref name="go"/>.
    /// </summary>
    private Notice ConsentNotice(ConsentTarget target, string title, string action, Action go, Action notNow)
    {
        var text =
            $"A reminder: {AppName} is only an access layer. It shows what {target.Program} does and passes your commands to it; {target.Program} is not part of axKit.\n" +
            $"{target.Work}\n" +
            $"Its terms are {target.Terms}, between you and its maker{(target.TermsUrl.Length > 0 ? ": " + target.TermsUrl : "; no address is known for them.")}\n" +
            "axKit is a set of personal tools shared as they are, not a product or a service (axKit menu, About, Licence).";
        return new Notice(title, text,
        [
            new OverlayChoice(action, () =>
            {
                _agreed.Add(target.Program);
                Consent.Record(AppPaths.ConsentFile, AppName, target.Program, BundleInfo.Version, DateTime.Now);
                go();
            }, IsDefault: true),
            new OverlayChoice("&Not now", notNow, IsCancel: true),
        ])
        {
            Consent = ConsentBox(AppTitle, target.Program),
        };
    }
}
