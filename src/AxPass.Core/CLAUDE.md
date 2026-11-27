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
  `Generate`, `CreateDatabase`, `Info`), the options (`Quiet`, `ListOptions`, `TitleOption` and the other `edit` options,
  `PasswordPromptOption`, `ShowValueOptions`, `GenerateArguments`), the marks of its paths (`PathSeparator`, `Root`,
  `EmptyMark`, `RecycleBin`) and the names `show` prints. Every class here and the window name a command or an option
  by its constant; `KeePassCliTextsTests` fails when one is written as a string anywhere else, or when something
  searches the tool's error text. No prompt and no error text of the tool is matched anywhere.
- `KeePassCli` and `CliResult` (PW-5): `Find` (KeePassXC's install folder, then the path set and the PATH, those two
  only for a file `SignedByKeePassXC`: signed, the signature holding, the signer one of `KeePassCliTexts.Publishers`), `Run` (one command,
  the arguments in `ArgumentList`, the input lines on standard input, a timeout; through `AxKit.Core/ProcessRun`),
  `On` (the command, `-q`, the options, the database, the paths); `CliResult.Why` (the tool's own words for a
  failure) and `SaidNothing` (a failure without words, which with `-q` is a wrong master password).
- `PassTree` and `PassNode`: `ls -R -f` read into groups and entries (`Parse`), a group's `Level` (groups first, the
  group given as the Recycle Bin last at the top), `CountIn`, `HasGroup`, `HasEntry`, `Count` (how many lines the
  listing had for a path: two entries of one name in a group are one node and two lines), `NodeOf`, `Join`,
  `RefusedName` (a slash), `DatabaseName`.
- `PassBin`, `DeleteFate` and `Removed` (PW-6.4b, 2026-11-27): what axPass knows of the Recycle Bin from the tool's
  own output and no more, since the tool never says which group it is: `IsOn` (`db-info`'s line,
  `KeePassCliTexts.RecycleBinOn`, with the command `Info`), `Known` (the group a deleted entry was seen to arrive
  in, else the tool's own name's top group when the bin is on), `Holds`, `Fate` (to the bin, for good, or not known,
  for which the delete asks first, `Question`), `MovedTo` and `Outcome` (what became of what was removed, from the
  listing before and after: the group that gained it, and whether it is the one of its name there, which is what
  lets `mv` reach it), `Said` (the sentence after a delete), `StepSaid` and `NotSole` (Ctrl+Z's words). Never take
  a group for the bin by its name alone, and never move or delete in the bin by a name two entries have.
  `KeePassCli.Result` turns a run into a `CliResult`: ended at its limit, it failed.
- `EntryFields` (PW-6): `show` read into an `ItemDocument` of KeePass's fields (the password's value never read, its
  type `password`), other attributes after them; `EditOptions` (the `edit` options that make one document into
  another); `Emptied` and `CannotEmpty` (the fields a change would empty, which the tool's `edit` cannot do: it takes
  an empty value as not given, so the window refuses the change, and an undo that would need it, in its own words,
  PW-6.1); `IsStandard`, `ValueOf`.
- `Backups` (PW-4.5): `FolderFor` ("mine backups" beside the database), `Make` (the file copied, named with the time, the newest `Keep` kept), `List` (newest first, by name), `Changes` (the tool's commands that change the database).
- `PassSettings`: the database open last, the tool's path when set, the minutes before the lock.

## Gotchas

- `show` prints a note of several lines over several lines; a new field starts only at a name `show` prints itself.
- The test data uses `sam` as a user name and `test-master-1` as a password, never a real name or secret.
