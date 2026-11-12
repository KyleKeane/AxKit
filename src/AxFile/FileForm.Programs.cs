using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AxFile.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxFile;

/// <summary>axFile's window, the entry handed to other programs: the user's own programs (AF-4.11) and Windows' own ways with the entry (AF-4.7).</summary>
public sealed partial class FileForm
{
    // ---- the user's own programs (AF-4.11): a list in the settings, each run on the entries as its arguments ----

    /// <summary>
    /// Open with ▸ (AF-4.7, AF-4.11): Windows default and Choose a program…, then the user's own programs as
    /// "1. name (path)" with Add a program… and Remove a program ▸ under them; refilled each time it opens.
    /// </summary>
    private void FillOpenWith(ToolStripMenuItem menu)
    {
        menu.DropDownItems.Clear();
        menu.DropDownItems.Add(new ToolStripMenuItem("Windows &default", null, (_, _) => OpenSelectedWithWindows()));
        menu.DropDownItems.Add(new ToolStripMenuItem("&Choose a program...", null, (_, _) => OpenWith()));
        menu.DropDownItems.Add(new ToolStripSeparator());
        var programs = _settings.Programs;
        if (programs.Count == 0)
        {
            menu.DropDownItems.Add(new ToolStripMenuItem("&No programs of your own yet"));
        }

        for (var i = 0; i < programs.Count; i++)
        {
            var program = programs[i];
            menu.DropDownItems.Add(new ToolStripMenuItem(AxKitWindows.Label(i, $"{program.Name} ({program.Path})"), null, (_, _) => RunProgram(program)));
        }

        menu.DropDownItems.Add(new ToolStripMenuItem("&Add a program...", null, (_, _) => AddProgram()));
        var remove = new ToolStripMenuItem("&Remove a program");
        if (programs.Count == 0)
        {
            remove.DropDownItems.Add(new ToolStripMenuItem("&No programs of your own yet"));
        }

        for (var i = 0; i < programs.Count; i++)
        {
            var program = programs[i];
            remove.DropDownItems.Add(new ToolStripMenuItem(AxKitWindows.Label(i, program.Name), null, (_, _) => RemoveProgram(program)));
        }

        menu.DropDownItems.Add(remove);
    }

    /// <summary>Open with a custom program ▸ one program: run on the selected entries, or the current one, as its arguments; "Started <name>".</summary>
    private void RunProgram(UserProgram program)
    {
        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        var paths = targets.Select(target => target.Path).ToList();
        if (UserPrograms.RefusedTarget(program, paths) is { } refused)
        {
            // S-3: cmd.exe would read the name's & or | as a command; the name is the danger, not the program.
            Failed($"{program.Name} not started", $"{System.IO.Path.GetFileName(refused)} has a character a batch file reads as a command (& | < > ^ % ! ( ) , ; = or a quote). Rename it, or use a program that is not a batch file.");
            return;
        }

        try
        {
            // Outside call, program, User programs: starts a program the user added to Open with on the selected
            // entries; the user's own program, by the path they gave, a script through Windows PowerShell.
            Process.Start(UserPrograms.StartInfo(program, paths))?.Dispose();
            Announce($"Started {program.Name}", true);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            Failed($"{program.Name} failed", ex.Message);
        }
    }

    /// <summary>
    /// Add a program…: the Windows file picker on programs and scripts, then the notice "Add a program" with the field
    /// "Name" holding the file's name; "Added <name>". A blank name is said and the notice stays, as every one-field
    /// notice does (the base's <c>AskText</c>; until 2026-11-09 the notice closed first).
    /// </summary>
    private void AddProgram()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose a program or script",
            Filter = "Programs and scripts (*.exe;*.cmd;*.bat;*.ps1;*.py)|*.exe;*.cmd;*.bat;*.ps1;*.py|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        var chosen = ShowSystemDialog(dialog) == DialogResult.OK ? dialog.FileName : null;
        if (chosen is null)
        {
            return;
        }

        AskText("Add a program", $"The name the menu shows for {chosen}.", "Name:", "Name", UserPrograms.DefaultName(chosen), "&Add",
            typed =>
            {
                var name = typed.Trim();
                _settings.Programs.Add(new UserProgram(name, chosen));
                SaveSettings();
                Announce($"Added {name}", true);
            },
            typed => typed.Trim().Length == 0 ? "Type a name" : null, emptyMessage: "Type a name");
    }

    /// <summary>Remove a program ▸ one program: taken off the list; "Removed <name>".</summary>
    private void RemoveProgram(UserProgram program)
    {
        _settings.Programs.Remove(program);
        SaveSettings();
        Announce($"Removed {program.Name}", true);
    }

    // ---- Windows' own ways with the entry (AF-4.7): the system's dialogs and menu, like the folder picker (AX-7.2) ----

    /// <summary>Item → Open with → Choose a program…: the Windows dialog that picks a program for the file, and runs it.</summary>
    private void OpenWith()
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        if (entry.IsFolder)
        {
            Announce("Not a file", true);
            return;
        }

        if (WindowsOpenWith.Show(Handle, entry.Path) is { } reason)
        {
            Failed("Open with failed", reason);
        }
    }

    /// <summary>Item → Properties (Alt+Enter): the Windows Properties sheet of the entry, a file or a folder.</summary>
    private void ShowProperties()
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        if (Shell.Properties(Handle, entry.Path) is { } reason)
        {
            Failed("Properties failed", reason);
        }
    }

    /// <summary>Shift+F10, the Applications key and a right click in the folder list (AF-4.6, AF-D13): the bar's Context menu with the entry's commands, opened as Alt and C open it (AX-9.11; the window base's <c>OpenBarMenu</c>).</summary>
    private void ShowItemMenu()
    {
        _context.Fill();
        OpenBarMenu(ContextBarMenu.Letter);
    }

    /// <summary>Context → Windows menu…: the shell's own menu for the entry, under it in the list, once Context has closed.</summary>
    private void ShowWindowsMenu()
    {
        if (!TryEntry(out var entry))
        {
            return;
        }

        var bounds = _list.GetItemRectangle(_list.SelectedIndex);
        var at = _list.PointToScreen(new Point(bounds.Left, bounds.Bottom));
        // Posted, so that Context has closed and the list has the focus back before the shell's menu takes over.
        BeginInvoke(() =>
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                if (Shell.ShowMenu(Handle, entry.Path, at) is { } reason)
                {
                    Failed("Windows menu failed", reason);
                }
            }
            catch (Exception ex) when (ex is COMException or InvalidCastException or Win32Exception)
            {
                Log.Error("The Windows menu failed", ex);
                Failed("Windows menu failed", ex.Message);
            }
        });
    }
}
