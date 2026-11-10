# axList user guide

axList is planned, not yet part of axKit: this guide describes it as designed (`docs/axList/SPEC.md`) and is embedded
in the app once it is built.

axList keeps lists of things to remember, made for screen reader users. Each list is a plain Markdown file in a folder
of lists, so you can also read and change it in axDown, any editor, a script or an AI system, and axList reads it the
same way. Every change is written to the file at once: there is no Save. For things to do, with due dates and one view
of everything open, there is axTask.

## Start

- Start menu: `axKit axList`. Console: `axList`, or `axlist Shopping.md` for one list with its folder as the folder of
  lists. In File Explorer, Open with axKit on a `.md` file.
- The first time, axList uses the folder `Lists` in your Documents folder. Options, Lists folder chooses another.
- axList opens on the list you had open last.

## Three views

- **Lists** (Ctrl+1): the lists of the folder, "Shopping, 6 items". Enter opens one. Ctrl+N makes a new list after
  asking for its name.
- **Items** (Ctrl+2): the open list, one line per item: "Milk, task, not done", "Eggs, task, done", "1. Preheat the
  oven", "Butter", "Heading: Shopping". A nested item ends in "level 2". Escape or Backspace goes back to the lists.
- **Tasks** (Ctrl+3): every checkbox item not yet done, from every list: "Fix the tap, in Home". Space checks one off;
  Enter opens its list on it.

Pressing a view's key while it shows says its name again. Tab and Shift+Tab move to the next and the previous view. In
any view, type the start of an item to jump to it; the same letter again goes to the next one, and Shift with a letter
goes backwards. At the first or the last item, Up, Down, Home and End play a short low sound. F5 reads everything
again, for a list you changed in another program.

## Changing a list

In the Items view:

- **Space** checks a checkbox item off or back on: "Done: Milk".
- **Enter** changes the item's text in a notice; Enter there writes it.
- **Ctrl+N** adds a new item after the one you are on, of the same kind: a checkbox after a checkbox, the next number
  after a numbered item.
- **Delete** removes the item and anything nested under it.
- **Alt+Up** and **Alt+Down** move the item, with what is nested under it, past the one before or after it. At the top
  or the bottom you hear a short low sound and "Top of the list".
- **Alt+Right** nests the item under the one before it; **Alt+Left** takes it out a level.
- **Ctrl+T** gives an item a checkbox, or takes it away again.
- **Ctrl+Shift+D** hides the done checkbox items, and shows them again.
- **Ctrl+Z** undoes the last change, up to ten steps, as long as the window is open.
- **Ctrl+C** copies the item's text; **Ctrl+F** finds an item; F3 finds the next.

If another program changed the list since axList read it, axList reads it again and says so instead of changing the
wrong line.

## The file

A list file is Markdown:

```
# Shopping
- [ ] Bread
- [x] Eggs
  - [ ] Free range
- Butter
1. Preheat the oven
```

A line with `- [ ]` is a checkbox item, `- [x]` a done one, `-` (or `*`, `+`) a plain item, `1.` a numbered item, `#` a
heading; two spaces more nest an item under the one above. Any other line is kept as it is and shown as text. axList
adds nothing hidden to the file.

## Menus

- **Folder**: Lists folder, Refresh (F5), Exit.
- **List**: Open (Enter), New list (Ctrl+N), Show in axFile (Ctrl+E), Open in axDown, Copy name (Ctrl+C).
- **Item**: Check or uncheck (Space), Change (Enter), New item (Ctrl+N), Make a task or not (Ctrl+T), Remove (Delete),
  Move up, Move down, Indent, Outdent, Undo (Ctrl+Z), Copy text (Ctrl+C), Show in axFile, Edit in axDown.
- **Task**: Check off (Space), Go to its list (Enter), Undo, Copy text.
- **View**: Lists (Ctrl+1), Items (Ctrl+2), Tasks (Ctrl+3), Hide done tasks (Ctrl+Shift+D).
- **Options**: Text (font and size), General (the update check), Developer (the settings file, diagnostics).
- **Window**, **axKit** and **Help**: the bundle's menus, the same in every axKit window, with Options between Window and axKit. Window (Alt+W) starts a new window of any app, lists the open axKit windows (the one you were in last first: Alt+W, O, 1 takes you back to it), reopens a file or folder closed lately and closes all windows. axKit (Alt+A) holds what is the whole bundle's: Claude (the switch Use Claude in axKit, Ask about this window with Shift+F1, Folders Claude may read, Claude's working folder), the AI tools and the theme. Help (Alt+H) has the command palette (Ctrl+Shift+P), the keyboard shortcuts (F1), this guide, the app's resource pages, Send a report (F4), Your reports, Check for updates and About.

Only the menu of the view you are in can be opened, so an item's commands are never at hand among the lists. Shift+F10,
or the Applications key, opens it. To rename or delete a list, use axFile: Ctrl+E shows the list's file there.

## Settings

axList keeps the font, the text size, the update check, the window's place, the folder of lists, the list open last
and whether done items are hidden in `%APPDATA%\AxKit\axList.json`. Options, Developer, Open settings file opens it in
axJSON.

## If something goes wrong

- "The list changed on disk; it has been read again": another program changed the file. Look at it again and repeat
  the change.
- axList's log is `%LOCALAPPDATA%\AxKit\logs\axList.log`; it never holds the text of your lists. Options, Developer,
  Copy diagnostics puts what is needed for a bug report on the clipboard.

## About

axList is part of axKit by Dr. Kyle Keane, www.kylekeane.com. axKit, About has the licence and the version.

## Disclaimer

axList writes to your list files as you work. Keep a copy of a list that matters, as you would of any file; Ctrl+Z
undoes the last steps while the window is open.
