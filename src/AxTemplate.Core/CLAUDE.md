# AxTemplate.Core, the pure code

Everything of axTemplate that needs no window. Unit tested in `tests/AxTemplate.Tests`. The spec is `docs/axTemplate/SPEC.md` ("SPEC.md" below); the windows are `src/AxTemplate`, with their own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxTemplate
```

## Where things are

- The store of templates is `AxKit.Core`'s since 2026-11-23, on its second use, the editors' Insert template (TP-5, step 4): `TemplateStore` with `Template`, `TemplateFile`, `TemplateList`, `TemplateFolder`, described in `src/AxKit.Core/notes/text-and-editors.md`. Its tests stay here, in `tests/AxTemplate.Tests`.
- `TemplateMatch` (TP-3.2, TP-3.3): the finding by words in the name or the text, names first; the order of an empty field, the ones used last first; what is said of a match and of none.
- `TemplateSettings` (TP-7.2): one settings file for both windows, on `AxKit.Core/HotkeySettings`: the hotkey (Ctrl+Shift+Semicolon at first), the hotkey windows' settings, the names used last, the browser's place.
- `PasteTarget` with `LineBreaks` (TP-4.8): what the window in front does with a paste's line breaks, from its class names and style, and the question before several lines go into a terminal; and `GivesBack` (TP-4.3, 2026-11-27): whether the clipboard's text of before is put back after the paste, never a text that was copied as private. Tested in `PasteTargetTests`.
- The marks of a template's text are not here: `AxKit.Core/TextTemplate`, which the editors use too.

## Gotchas

- An item names its configuration under the key `schema` (`FlatItem.SchemaKey`), and a file without it is no item, so no template.
- A new app with a field to write in is added to `AppsWithField`, in the bundle's order of apps (a test holds the order); every folder gains its check the next time it is opened.
- A configuration that does not read is left as it is on disk, and the templates are read through the fields for no app, so the Title and the Text still show.
