using System.Reflection;
using AxKit.Core;
using AxKit.Forms;

namespace AxMedia;

/// <summary>The texts axMedia shows as notices: the keyboard shortcuts (F1) and the user guide; About is the bundle's (<c>AxKit.Core.BundleAbout</c>).</summary>
public static class HelpText
{
    /// <summary>The F1 text (SPEC.md AM-5.3): a first line naming the text's own keys, then one Markdown heading per group (bundle AX-7.2): the bundle-wide keys, then axMedia's own. The first line and the bundle's section come from <c>BundleHelp</c>.</summary>
    public static readonly string Shortcuts = BundleHelp.Section(
        "axMedia",
        [
            $"{BundleKeys.Text(BundleKeys.Find)}: find a file whose name contains the text. {BundleKeys.Text(BundleKeys.FindNext)} and {BundleKeys.Text(BundleKeys.FindPrevious)}: the next or the previous match",
        ]) + $"""
        ## The queue
          Up, Down, Home, End, Page Up, Page Down: move through the files; type the start of a name to jump to it, Shift with a letter backwards. The one playing reads "(playing)" after its name
          Right, Left, Backspace: the edge sound; a queue has no list to go into and no level above
          Enter: play this file; the rest of the queue follows in order
          Delete: take this file out of the queue; the one playing goes on to the next. With files selected, all of them: "Removed 3 files"
          Space: select the file you are on, or deselect it again; a selected file reads ", selected". {BundleKeys.Text(SelectionKeys.SelectDown)}, {BundleKeys.Text(SelectionKeys.SelectUp)}, {BundleKeys.Text(SelectionKeys.SelectToStart)}, {BundleKeys.Text(SelectionKeys.SelectToEnd)}: select as you move. {BundleKeys.Text(SelectionKeys.SelectTo)}: select from the nearest selected file to this one. {BundleKeys.Text(SelectionKeys.SelectAll)}: select every file. {BundleKeys.Text(SelectionKeys.SelectNone)}, or Escape: deselect all
          {BundleKeys.Text(AxMediaKeys.Information)}: the file's place, "3 of 12", its position while it plays, and how many are selected
          {BundleKeys.Text(AxMediaKeys.OpenFiles)}: add files with the Windows file picker, several at once. {BundleKeys.Text(AxMediaKeys.OpenFolder)}: add a folder's sound and video files. Session, Open recent lists the files and folders added lately by number
          Session, Clear queue: the queue emptied and the sound stopped
          {BundleKeys.Text(AxMediaKeys.NewWindow)}: a new axMedia window
          {BundleKeys.Text(AxMediaKeys.ShowInExplorer)}: show the file in File Explorer. Session, Show current file in, axFile: the file in its folder in axFile
          {BundleKeys.Text(BundleKeys.Save)}: nothing to save here; axMedia says so

        ## Playing
          {BundleKeys.Text(BundleKeys.MainAction)}, from anywhere in the window: play the file you are on when nothing plays; pause; resume
          {BundleKeys.Text(AxMediaKeys.Forward)}, {BundleKeys.Text(AxMediaKeys.Back)}: 15 seconds forward or back. {BundleKeys.Text(AxMediaKeys.GoToTime)}: go to a time, 12:40 or 1:02:10 or seconds
          {BundleKeys.Text(AxMediaKeys.NextFile)}, {BundleKeys.Text(AxMediaKeys.PreviousFile)}: the next or the previous file of the queue
          {BundleKeys.Text(AxMediaKeys.Faster)}, {BundleKeys.Text(AxMediaKeys.Slower)}: faster or slower by a quarter, the voice's pitch kept; Actions, Normal speed goes back to 1
          {BundleKeys.Text(AxMediaKeys.WhatIsPlaying)}: what is playing, where it is, and the speed
          {BundleKeys.Text(ItemKeys.ItemMenu)}, the Applications key or {BundleKeys.Text(BundleKeys.ContextMenu)}: the Context menu, what acts on the file you are on in the queue
          Options, Say each file as it starts: the next file's name said when the queue moves on, the one thing axMedia says on its own
          The volume is Windows' own: your keyboard's volume keys
        """ + BundleHelp.FilterAndSortSection("Name puts the queue itself in the order of the names, so that the files play in that order; The order added puts it back") + BundleHelp.NoticesSection("axMedia", "the list");

    /// <summary>docs/axMedia/user-guide.md, embedded by AxMedia.csproj.</summary>
    public static string UserGuide() => EmbeddedText.Read(Assembly.GetExecutingAssembly(), "UserGuide.md", "docs/axMedia/user-guide.md");
}
