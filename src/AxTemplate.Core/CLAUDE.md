# AxTemplate.Core, the pure code

Everything of axTemplate that needs no window. Unit tested in `tests/AxTemplate.Tests`. The spec is `docs/axTemplate/SPEC.md` ("SPEC.md" below); the windows are `src/AxTemplate`, with their own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxTemplate
```

## Where things are

- `TemplateStore` (TP-2): the templates' folder as a list of the bundle's item format (`AxKit.Core`'s `ListConfig`, `FlatItem`, `ItemView`). `Fields` and `AppsWithField` are a template's fields; `ConfigurationToWrite` makes the folder's `list.json` or adds the fields a newer version brings, never taking one away; `Open` makes the folder ready and reads it (disk, off the UI thread); `Files` gives every item file for the browser, `Read` the templates for the launcher; `After` is the files after a change of the browser's own, or its undo, without the disk; `NameRefused`; `LeftKeys`, the Left keys that set the caret after a paste (TP-2.3).
- `Template`, `TemplateFile`, `TemplateList`, `TemplateFolder`: a template as it is used (name, text, where it shows), a file as the browser holds it (its text and its item, or none), and the two shapes a read gives.
- `TemplateMatch` (TP-3.2, TP-3.3): the finding by words in the name or the text, names first; the order of an empty field, the ones used last first; what is said of a match and of none.
- `TemplateSettings` (TP-7.2): one settings file for both windows, on `AxKit.Core/HotkeySettings`: the hotkey (Ctrl+Shift+Apostrophe at first), the hotkey windows' settings, the names used last, the browser's place.
- The marks of a template's text are not here: `AxKit.Core/TextTemplate`, which the editors use too.

## Gotchas

- An item names its configuration under the key `schema` (`FlatItem.SchemaKey`), and a file without it is no item, so no template.
- A new app with a field to write in is added to `AppsWithField`, in the bundle's order of apps (a test holds the order); every folder gains its check the next time it is opened.
- A configuration that does not read is left as it is on disk, and the templates are read through the fields for no app, so the Title and the Text still show.
