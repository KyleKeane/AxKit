using AxKit.Core;

namespace AxKit.Forms;

public abstract partial class AxKitForm
{
    // ---- updates (AX-3): the Help menu's two items and the window's members; the check, the offer in a notice, the
    // install in place and the restart are UpdateFlow's ----

    private readonly ToolStripMenuItem _checkItem = new($"Check for &updates (axKit {BundleInfo.Version})...");
    private readonly ToolStripMenuItem _updateItem = new() { Visible = false };

    /// <summary>What the last check found and what an install left to do; made in the constructor (AxKitForm.cs).</summary>
    private readonly UpdateFlow _updates;

    /// <summary>What the last check found, for About, Version: "not checked yet", "checking...", "axKit 3.5.0 (newer than this one)"; the Help menu reads it.</summary>
    protected internal string LatestReleaseState => _updates.LatestReleaseState;

    /// <summary>Update and restart was chosen and the update is installed: the new version starts when the window closes.</summary>
    protected bool RestartPending => _updates.RestartPending;

    /// <summary>The update is installed and the window is closing for it (AX-3.2): a window that otherwise hides on closing (axStart) closes.</summary>
    protected bool ClosingForUpdate => _updates.ClosingForUpdate;

    /// <summary>
    /// AX-3: asks GitHub for the latest release, every time. The state for About, Version is set first; then one
    /// thing is shown: a newer release in a notice (unprompted at startup, or said as "in the Help menu" while a notice
    /// is up), the newest version or a failure when the check was manual (axKit → Check for updates), nothing otherwise.
    /// </summary>
    protected void CheckForUpdates(bool manual) => _updates.CheckForUpdates(manual);

    /// <summary>axKit → Update to axKit n: the notice for the release the last check found.</summary>
    private void OfferKnownUpdate() => _updates.OfferKnownUpdate();

    /// <summary>On the way out after Update and restart (AX-3.2): the new version starts on what this window held as this process ends.</summary>
    private void StartInstalledVersion() => _updates.StartInstalledVersion();

    /// <summary>Keep working after Update and restart: the update is installed already; this window stays, and the new version comes with the next start.</summary>
    protected void KeepWorkingAfterUpdate() => _updates.KeepWorkingAfterUpdate();

    // ---- what UpdateFlow asks of the window: the two items, and the hooks an app fills in (AxKitForm.cs) ----

    /// <summary>While a check or an install runs, the two update items of the Help menu are disabled, so that neither starts twice.</summary>
    internal void EnableUpdateItems(bool enabled)
    {
        _checkItem.Enabled = enabled;
        _updateItem.Enabled = enabled;
    }

    /// <summary>The Help menu's Update to item with <paramref name="text"/>, or hidden for null: no newer release is known.</summary>
    internal void ShowUpdateItem(string? text)
    {
        if (text is not null)
        {
            _updateItem.Text = text;
        }

        _updateItem.Visible = text is not null;
    }

    /// <summary>The app's <see cref="UpdateNoted"/>.</summary>
    internal void NoteUpdate(string message) => UpdateNoted(message);

    /// <summary>The app's <see cref="UpdateRestartText"/>.</summary>
    internal string RestartText() => UpdateRestartText;

    /// <summary>The app's <see cref="RestartArguments"/>.</summary>
    internal IReadOnlyList<string> ArgumentsForRestart() => RestartArguments();
}
