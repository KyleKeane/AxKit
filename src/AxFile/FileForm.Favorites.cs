using AxFile.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxFile;

/// <summary>
/// The Favorites list (SPEC.md AF-9.1, AF-3.9): the window's second pane, under the folder list, with the folders the
/// user pinned (Ctrl+D, the settings' <c>favorites</c>), one line each, "Music (C:\Music)", in the order added; "No
/// favorites yet" until the first. Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the folder list and
/// it (the item window's pane keys), Escape in it goes back; Enter on a line shows that folder in the folder list,
/// Delete takes it out of the favorites. Navigation → Favorites ▸ lists the same folders as a submenu.
/// </summary>
public sealed partial class FileForm
{
    /// <summary>The one line of the Favorites list before the first favorite (AF-9.1), so that the list is never empty (AX-9.5).</summary>
    private const string NoFavoritesText = "No favorites yet";

    /// <summary>The Favorites list: the item window's second pane (<see cref="ItemForm.SecondPane.Log"/>, a list the app fills itself), under the folder list with a third of the window (AF-9.1).</summary>
    private AxKitList _pane => DetailPane;

    private void SetUpFavorites()
    {
        _pane.AccessibleName = "Favorites";
        _pane.DoubleClick += (_, _) => OpenFavorite();
    }

    /// <summary>
    /// The list's lines made again from the settings' favorites, after they changed: one line a favorite, "No favorites
    /// yet" while there is none. <paramref name="select"/>, a favorite just added, is selected, so that Tab lands on
    /// it; otherwise the line the user was on stays, or the one that took its place. Nothing is said: what changed
    /// the favorites says so itself.
    /// </summary>
    private void FillFavoritesPane(string? select = null)
    {
        var favorites = _settings.Favorites;
        IReadOnlyList<string> lines = favorites.Count == 0 ? [NoFavoritesText] : [.. favorites.Select(FileSettings.FavoriteLine)];
        var keep = _pane.SelectedIndex;
        _pane.SetLines(lines);
        var chosen = select is null ? -1 : favorites.FindIndex(folder => string.Equals(folder, select, StringComparison.OrdinalIgnoreCase));
        var index = chosen >= 0 ? chosen : Math.Clamp(keep, 0, lines.Count - 1);
        if (_pane.SelectedIndex != index)
        {
            _pane.SelectedIndex = index;
        }
    }

    /// <summary>Navigation → The Favorites list: the focus in it (AF-9.1).</summary>
    private void ShowFavorites() => _pane.Focus();

    /// <summary>The favorite under the selection; null on "No favorites yet".</summary>
    private string? SelectedFavorite()
    {
        var index = _pane.SelectedIndex;
        return index >= 0 && index < _settings.Favorites.Count ? _settings.Favorites[index] : null;
    }

    /// <summary>
    /// Enter in the Favorites list: the folder in the folder list, its first entry selected, the focus there and the
    /// folder said (AF-3.1); it joins the recent folders, as a favorite chosen from the submenu does. A folder that is
    /// gone gives the "Folder not found" notice and its nearest parent (AF-3.5). On "No favorites yet", that line again.
    /// </summary>
    private void OpenFavorite()
    {
        if (SelectedFavorite() is not { } folder)
        {
            Announce(NoFavoritesText, true);
            return;
        }

        ShowInList(folder, null, remember: true);
    }

    /// <summary>Ctrl+I in the Favorites list: the favorite's full path and its place, "C:\Music, 2 of 3"; on "No favorites yet", that line again.</summary>
    private void SayFavorite()
    {
        if (SelectedFavorite() is not { } folder)
        {
            Announce(NoFavoritesText, true);
            return;
        }

        Announce($"{folder}, {_pane.SelectedIndex + 1} of {_settings.Favorites.Count}", true);
    }

    /// <summary>Delete in the Favorites list: the favorite taken out, "Removed from Favorites"; the folder itself is not touched. On "No favorites yet", that line again.</summary>
    private void RemoveSelectedFavorite()
    {
        if (SelectedFavorite() is not { } folder)
        {
            Announce(NoFavoritesText, true);
            return;
        }

        _settings.RemoveFavorite(folder);
        SaveSettings();
        FillFavoritesPane();
        Announce("Removed from Favorites", true);
    }

    // ---- Navigation → Favorites ▸ and Ctrl+D (AF-3.9) ----

    /// <summary>Navigation → Favorites ▸: the pinned folders that still exist as "1. name (path)", or "No favorites yet", then the key's item.</summary>
    private void FillFavorites()
    {
        RecentMenu.FillFolders(_favoritesItem, _settings.Favorites, "&No favorites yet", CommandMark.Open, target => Do(() => ShowFolderAsync(target, null, remember: true)));
        _favoritesItem.DropDownItems.Add(new ToolStripSeparator());
        _favoritesItem.DropDownItems.Add(Item("&Add or remove this folder", ToggleFavorite, AxFileKeys.Favorite, CommandMark.Change));
    }

    /// <summary>Ctrl+D: the folder shown pinned, "Added to Favorites", and selected in the Favorites list; or unpinned again, "Removed from Favorites".</summary>
    private void ToggleFavorite()
    {
        if (!TryFolder(out var folder))
        {
            return;
        }

        var added = _settings.ToggleFavorite(folder);
        SaveSettings();
        FillFavoritesPane(added ? folder : null);
        Announce(added ? "Added to Favorites" : "Removed from Favorites", true);
    }
}
