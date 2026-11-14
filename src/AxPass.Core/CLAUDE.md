# AxPass.Core, the pure code

Everything of axPass that needs no window, unit tested in `tests/AxPass.Tests` (with one test that runs KeePassXC's
tool on a throwaway database when KeePassXC is installed, and passes without running where it is not, as on the build
servers). The spec is `docs/axPass/SPEC.md` (PW-n.m); what the tool prints and takes is `docs/axPass/kdbx-notes.md`
section 8. The window is `src/AxPass`, with its own `CLAUDE.md`. The item format and the field types are `AxKit.Core`'s.

## Commands

```
.\test.ps1 AxPass
```

## Where things are

- `KeePassCliTexts` (root `CLAUDE.md` rule 12, 2026-11-14; `docs/axKit/access-layers.md`): the one table of the tool's
  words, as constants: the commands (`List`, `Show`, `Edit`, `Add`, `Remove`, `RemoveGroup`, `MakeGroup`, `Move`,
  `Generate`, `CreateDatabase`), the options (`Quiet`, `ListOptions`, `TitleOption` and the other `edit` options,
  `PasswordPromptOption`, `ShowValueOptions`, `GenerateArguments`), the marks of its paths (`PathSeparator`, `Root`,
  `EmptyMark`, `RecycleBin`) and the names `show` prints. Every class here and the window name a command or an option
  by its constant; `KeePassCliTextsTests` fails when one is written as a string anywhere else, or when something
  searches the tool's error text. No prompt and no error text of the tool is matched anywhere.
- `KeePassCli` and `CliResult` (PW-5): `Find` (the path set, KeePassXC's install folder, the PATH), `Run` (one command,
  the arguments in `ArgumentList`, the input lines on standard input, a timeout; through `AxKit.Core/ProcessRun`),
  `On` (the command, `-q`, the options, the database, the paths); `CliResult.Why` (the tool's own words for a
  failure) and `SaidNothing` (a failure without words, which with `-q` is a wrong master password).
- `PassTree` and `PassNode`: `ls -R -f` read into groups and entries (`Parse`), a group's `Level` (groups first, the
  Recycle Bin last at the top), `CountIn`, `HasGroup`, `HasEntry`, `InRecycleBin`, `Join`, `RefusedName` (a slash),
  `DatabaseName`.
- `EntryFields` (PW-6): `show` read into an `ItemDocument` of KeePass's fields (the password's value never read, its
  type `password`), other attributes after them; `EditOptions` (the `edit` options that make one document into
  another); `IsStandard`, `ValueOf`.
- `Backups` (PW-4.5): `FolderFor` ("mine backups" beside the database), `Make` (the file copied, named with the time, the newest `Keep` kept), `List` (newest first, by name), `Changes` (the tool's commands that change the database).
- `PassSettings`: the database open last, the tool's path when set, the minutes before the lock.

## Gotchas

- `show` prints a note of several lines over several lines; a new field starts only at a name `show` prints itself.
- The test data uses `sam` as a user name and `test-master-1` as a password, never a real name or secret.
