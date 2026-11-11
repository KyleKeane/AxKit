# Themes: every colour of every window from one file

Asked by the user on 2026-11-09 ("every display parameter very easy to manage, every colour of every class of object
adjustable, theme support through a simple theme file that an axKit app edits"). Specification first, then the build;
requirements are numbered TH-n.m, decisions TH-Dn. It amends the bundle's colour rule (`docs/axKit/SPEC.md` AX-7.7):
without a theme file nothing there changes.

## 1. Summary

A theme is one small JSON file, `%APPDATA%\AxKit\theme.json`, that names every class of object the windows draw and
gives each a colour, or the word `system` for Windows' own. Every window of the bundle reads it as it opens and
whenever the file changes, and paints its controls from it. Without the file, or with every value `system`, the
windows are exactly what they are today: Windows' colours, light or dark as Windows is, high contrast as Windows has
it. The file is edited in axJSON (Options, Theme, Edit theme file in every window), so the user's own tools are the
theme editor; there is no colour picker window. Nothing NVDA hears changes: a colour is drawing only. Since 5.5.0 the
same file holds the display settings every window shares (section TH-6): the editors' word wrap, one value for axDown,
axJSON and axPreview, written by Ctrl+Shift+W in any of them.

Who it is for: the sighted helpers who read over a blind user's shoulder (the 2026-11-08 field tint came from one
taking the white message field for more of the white conversation), and users with low vision who need their own
contrast. The blind user keeps Windows' colours and loses nothing.

## 2. Goals and non-goals

- G1. Every colour the bundle draws is in one place, named by the class of object it paints, never by the control or
  the window. A class is what a helper would name: the text, the list, the field, the notice, the status bar.
- G2. The default is unchanged. No file, no change (AX-7.7 as written). A window with the theme off sets no colour at
  all.
- G3. Edited with the bundle's own tools: a JSON file axJSON opens, written as a complete template the first time, so
  the whole list of classes is read by ear and nothing has to be guessed.
- G4. Reached through the window base alone. An app never sets a colour (axMedia's black video surface stays the one
  exception, AM-2.4); a test fails on a colour in an app's code.
- Non-goal: a theme per app. One pair of eyes, one theme.
- Non-goal: fonts. Each app has Options, Text (the font, its size) already, kept per app since the texts differ.
- Non-goal: images, gradients, animations, a gallery of themes, a picker window (AX-7.2).

## 3. Scenarios

- A helper finds the white message field hard to tell from the conversation. In any window: Options, Theme, Edit theme
  file. axJSON opens `theme.json` with every class listed and `system` everywhere. They set `"field.background":
  "#FFF4C2"`, save, and switch back to axClaude: the field is yellow. Every other window takes it the next time it is
  activated or opened.
- A user with low vision sets `text.background` to `#000000` and `text.foreground` to `#FFFF00` for every editor,
  transcript and notice text, and leaves the rest to Windows.
- The blind user never opens the file. Nothing changes for them.
- A value is mistyped (`"#GGG"`). The window logs it, treats it as `system`, and Reload says "Theme reloaded: 1 value not
  understood, text.background".

## 4. Requirements

### TH-1 The file

- TH-1.1 One file for the bundle, `%APPDATA%\AxKit\theme.json` (`AppPaths`, beside `consent.json` and
  `recently-closed.json`); never one per app.
- TH-1.2 Its shape, written whole by Edit theme file when the file does not exist:

```json
{
  "format": "axkit-theme 1",
  "enabled": true,
  "name": "My theme",
  "colors": {
    "window.background": "system",
    "window.text": "system",
    "text.background": "system",
    "text.foreground": "system",
    "transcript.background": "system",
    "transcript.foreground": "system",
    "list.background": "system",
    "list.foreground": "system",
    "field.background": "system",
    "field.foreground": "system",
    "band.background": "system",
    "band.foreground": "system",
    "notice.background": "system",
    "notice.foreground": "system",
    "status.background": "system",
    "status.foreground": "system",
    "menu.background": "system",
    "menu.foreground": "system",
    "button.background": "system",
    "button.foreground": "system",
    "titleBar.background": "system",
    "titleBar.foreground": "system"
  },
  "display": {
    "wordWrap": true
  }
}
```

- TH-1.3 A colour is `system` (Windows' own colour for that class, which follows the light or dark app theme) or a hex
  colour `#RRGGBB`, case not mattering. Nothing else: not names, not alpha, not `rgb()`; one way (B-10).
- TH-1.4 A key the bundle does not know is kept as it is when the file is written again (as item files keep theirs,
  item-apps IA-4.1), so that a later version's classes and a user's notes survive. A known key that is missing means
  `system`. A value that is not understood means `system` and is counted for Reload's sentence and the log.
- TH-1.5 `enabled: false` keeps the file and paints nothing: Options, Theme, Theme on toggles it and writes the file.
- TH-1.6 No file, a file that is not JSON, or a file whose `format` is not `axkit-theme 1`: no theme, logged once per
  window, never spoken unprompted (AX-7.3).

### TH-2 The classes

What each class paints, in every window, by the control's kind, so that a new app gets the theme with nothing written:

| Class | Paints |
| --- | --- |
| `window` | the form and every panel behind the controls, and the labels on them (the band over a question's list, the control area's labels) |
| `text` | every edit control the user reads or writes in: the editors (axDown, axJSON, axPreview), a notice's text, the fields inside notices, axCalc's field, axStart's and axClip's search field |
| `transcript` | the console views of axClaude and axTerminal (`TranscriptView`), apart from the editors so that a conversation and a document can differ |
| `list` | every `AxKitList` and the lists in the control area (a question's answers), the item panes, axFile's folder list; the selected row stays Windows' highlight in this version (TH-D5) |
| `field` | the console windows' message or command field, tinted since 5.2.2 (today's tints become this class's defaults when the theme says `system`, so that nothing changes) |
| `band` | the header band over that field with its name |
| `notice` | the notice panel behind a notice's text and buttons (`OverlayPanel`) |
| `status` | the status bar and its labels |
| `menu` | the menu bar and its drop-downs (one `ProfessionalColorTable` with the two colours; `system` keeps WinForms' renderer as it is) |
| `button` | the buttons of notices and of the control area |
| `titleBar` | the window's caption, through `DwmSetWindowAttribute` (Windows 11; on Windows 10 the two keys do nothing, logged once) |

- TH-2.1 `.background` and `.foreground` for each; a class with no text (the window) still has `.foreground` for the
  labels on it, so that every class reads the same way.
- TH-2.2 axMedia's video area stays black whatever the theme says (AM-2.4).
- TH-2.3 A control's disabled state keeps Windows' greyed text; the theme never paints a disabled control.

### TH-3 When it is applied

- TH-3.1 By the window base (`AxKitForm`), once the window's controls exist and before its first paint (`OnLoad`), over the whole control tree;
  again when a notice opens (its controls are new) and when the control area changes (the console windows'); and on
  Options, Theme, Reload.
- TH-3.2 When the window is activated and the file's write time changed since it was read, the file is read and
  applied again, silently: edit in axJSON, Alt+Tab back, see it. The check is one file stamp, as the editors' changed
  on disk check is (AD-3.6).
- TH-3.3 With Windows' high contrast on (`SystemInformation.HighContrast`) the theme is not applied: high contrast is
  Windows' own accessibility choice, and a theme over it would undo it (TH-D4).
- TH-3.4 Applying a theme moves no focus, selects nothing and announces nothing (AX-7.3). Reload alone speaks: "Theme
  reloaded", or "Theme reloaded: N values not understood, …", or "No theme file: axKit menu, Theme, Edit theme file
  makes one", or "No theme: the file is not a theme file; the log says why".

### TH-4 The menu

axKit menu, Theme ▸ in every window, from the base (the one Options submenu every window builds from the base, so
that no window has a line of its own for it):

- **Edit theme file**: writes the complete template (TH-1.2) when the file does not exist, then opens it in a new axJSON
  window, as Options, Developer, Open settings file does.
- **Reload theme**: TH-3.4.
- **Theme on**: a checked item, `enabled`; toggling writes the file (and makes it from the template when there is none)
  and says "Theme on" or "Theme off".

No key of its own: three menu items, reached by the menu, by the palette (AX-10) or by Alt, O, T, H. F1 names nothing new.

### TH-5 Tests

- TH-5.1 `AxKit.Core/Theme` is tested: the template round trip, `system` and hex values, a bad value counted and
  treated as `system`, an unknown key kept, a missing key as `system`, `enabled` false, a file that is not a theme; the
  display settings read, written into a file that has none and kept beside a user's own keys (TH-6).
- TH-5.2 A repository test fails on a colour in an app's or the shared pieces' code outside the theme's own files:
  `Color.FromArgb`, `Color.<Name>`, `SystemColors.` and `BackColor =` or `ForeColor =` are allowed in
  `AxKit.Forms/Theme*.cs` and axMedia's video area alone (G4; the console field's tints move into the theme's defaults).
- TH-5.3 A step in the automation scripts (axDown): Options, Theme, Edit theme file opens axJSON on `theme.json`
  (`adopt`), Reload says "Theme reloaded"; Ctrl+Shift+W says "Word wrap off" and the file then holds
  `"wordWrap": false` (TH-6). Nothing more for NVDA: no focus or announcement changes beyond those sentences.

### TH-6 Display settings

The `display` object of the same file (2026-11-09, the user: one JSON with the adjustable display properties, "so that
things like word wrap can be tweaked"): what every window shares of how it shows its text, apart from colours. Read
whatever `enabled` says and under high contrast too, since none of it is a colour.

- TH-6.1 `display.wordWrap` (true by default): whether the editors, axDown, axJSON and axPreview, wrap long lines at
  the window's edge. Every other text of the bundle always wraps (a notice's text, a field, a transcript), since a line
  past the edge there would be unreachable; the editors alone have the long lines, a table or a line of code, that a
  user may want whole. One value for the three: it was a `wordWrap` key in each editor's own settings file until
  5.5.0, and a toggle left off in axDown's read as lines cut at the window's edge (the reports repository's issue 4).
- TH-6.2 Options, Text, Word wrap (Ctrl+Shift+W) in an editor writes the value into the file (the template first when
  there is none) and applies it to its own text with the caret kept (axDown AD-2.4); every other editor window takes
  it when next activated (TH-3.2), Reload theme at once, through the base's `OnThemeChanged`. Edited in axJSON like
  any other value of the file.
- TH-6.3 A value that is not `true` or `false` is on and counted for Reload's sentence, as a colour not understood is
  (TH-1.4).
- TH-6.4 Another display setting joins this object when a use comes, one that a helper or the user wants the same in
  every window: one key, read by the base and taken by the windows it concerns through `OnThemeChanged`, never a
  setting per app. A per-window setting (the font, each app's own since the texts differ) stays in the app's file.
- TH-6.5 `display.sayNewLine` (true by default; 2026-11-11, bundle AX-7.8, B-18): whether a line break typed into a
  field is said, "new line", in every window. Options, Text, Say new line in any window writes it the same way as word
  wrap (`Theme.SetSayNewLine`), says "Say new line on" or "Say new line off", and every other window takes it when next
  activated (TH-3.2), its Text menu checked as it opens.
  The template carries it beside `wordWrap`; a value that is not `true` or `false` is on and counted (TH-6.3).

## 5. Architecture

- `AxKit.Core/Theme.cs`: the record (`Enabled`, `Name`, `WordWrap`, the colours as a map from class key to
  `ThemeColor`, which is `System` or an ARGB integer), `Load(path)` returning the theme and the keys not understood,
  `Template()`, `SetEnabled` and `SetWordWrap` writing one value and keeping every other key, `Classes` (the list of
  keys, one place), `ParseColor`. No WinForms type: the colour is an integer, so that the project stays `net10.0` and
  tested.
- `AxKit.Forms/ThemePainter.cs`: `Apply(Control root, Theme theme)`, one walk over the tree deciding each control's
  class by its type (`TranscriptView` before `TextBox`, the console field by a tag the console window sets, a notice's
  panel by type, `MenuStrip`, `StatusStrip`, `Button`, `AxKitList`, `ListBox`, `TextBox`, the rest `window`), and the
  caption through `DwmSetWindowAttribute`. A `system` class gets the class's default, which is what the control had
  before themes existed (the Window colour for texts and lists, the console tints for the field and its band, the
  ambient colour for surfaces and labels), so that a value turned back to `system` is seen at the next reload and a
  Windows app theme change repaints the defaults (`SystemColorsChanged`); `Mark` names a control's class when its type
  does not (the console field, its band, axMedia's video surface as `fixed`).
- `AxKitForm`: the file read in the constructor (so that a window's own constructor can take a display setting from
  it) and painted in `OnLoad`, the Options, Theme items, the file stamp check on `Activated`, the calls to the painter
  (TH-3.1); `WordWrapPreference`, `SetWordWrapPreference` and the virtual `OnThemeChanged` for TH-6, which
  `TextEditorForm` overrides to follow the file.
- `ConsoleForm.ApplyFieldColors`: today's two tints become the `field` and `band` defaults the painter uses for
  `system`, so that the console windows look as they do in 5.2.2 without a theme.

## 6. Plan

Each step its own commit, the gate (`test.ps1`) green on each; the spec changes in the same commit as the behaviour.

1. This document; `SPEC.md` AX-7.7 amended with one sentence; `plan.md` and the root `CLAUDE.md` map.
2. `AxKit.Core/Theme` with its tests (TH-5.1).
3. `AxKit.Forms/ThemePainter`, the base's hooks and the menu (TH-3, TH-4); the console tints as defaults.
4. The colour test (TH-5.2); the F1 texts unchanged; the automation step (TH-5.3); the user guides' one paragraph
   (the bundle README's Settings section).
5. Later, when heard in use: the selected row of lists (TH-D5), a second file beside the first that a helper can switch
   to (`theme.json` stays the one that is read).
6. The display settings (TH-6), 2026-11-09: word wrap moved out of the three editors' settings files into the one
   `display` object, with the shared edit control (bundle AX-8.9) in the same change.

## 7. Decisions

- **TH-D1 One file for the bundle.** A theme is the eyes reading the screen, the same in every window; a per-app
  theme would be fourteen files to keep alike.
- **TH-D2 `system` written out, not omitted.** The template lists every class with `system`, so that axJSON reads the
  whole vocabulary to a user who cannot see the result, and a value is changed in place rather than invented.
- **TH-D3 No picker window.** The bundle's rule (AX-7.2), and the user who asked cannot see a colour swatch; a helper
  types six hex digits. axJSON's Format and its navigation keys are the editor.
- **TH-D4 Windows' high contrast wins.** Someone who turned high contrast on chose their colours in Windows; the theme
  stands aside rather than argue with it.
- **TH-D5 The list's selected row follows Windows in this version.** Painting it needs the list to draw its own rows
  (owner draw), which touches every `AxKitList`; it waits for a need and an NVDA pass of its own, since the rows'
  texts are what NVDA reads.
- **TH-D6 Colours are integers in Core.** `System.Drawing.Color` would tie the pure project to the Windows desktop
  assemblies; `#RRGGBB` to an `int` and back is six lines and tested.
- **TH-D7 Display settings in the theme file, not a file of their own and not per app.** The user asked for one JSON
  of the adjustable display properties; the theme file is that file already, edited the same way, re-read on the same
  activation check, and a second file would be a second place to look. No refresh key is added for it: the activation
  check and Reload theme are the refresh, and Ctrl+R is axTerminal's recent commands.
