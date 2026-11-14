# AxProcess.Core, the pure code

Everything of axProcess that needs no window. Unit tested in `tests/AxProcess.Tests`: the labels, the order, the arithmetic and the parsing from data, and one read each of this computer's processes, services and startup list. The spec is `docs/axProcess/SPEC.md` ("SPEC.md" below); the window is `src/AxProcess`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxProcess
```

## Where things are

- `ProcessTable`, `ProcessSample`, `ProcessRow`, `ProcessSort` (SPEC.md PR-3): `Sample` reads every process (name, working set, processor time, which a protected process does not give), `Rows` turns two samples into rows with each process's share of all processors over the time between (an ID that came back under another name has none), `Sorted`, `ListName`, `Summary` (PR-2.5).
- `SizeText`: gigabytes with one decimal for the summary; a process's size is `AxKit.Core/FileSize` since 2026-10-25 (one format for every app, the user's choice), "900 KB", "350.4 MB", "1.2 GB".
- `SystemInfo` and `SystemTimes`: the physical memory (`GlobalMemoryStatusEx`), the processors' idle, kernel and user times (`GetSystemTimes`; `Busy` between two, the kernel time including the idle time), a process's program file (`QueryFullProcessImageName` with the limited query right, which works for most processes a user cannot open fully) and the lines of its details notice.
- `ServiceManager`, `ServiceRow`, `ServiceResult` (PR-4): the Service Control Manager by P/Invoke on advapi32. `List` (`EnumServicesStatusEx` with the process information, looping on more data, then each service's start type from `QueryServiceConfig` and the delayed flag from `QueryServiceConfig2`), `Start` and `Stop` (the right asked for, the command, then a wait of up to 30 seconds for the state; already running or not active counts as done; access denied is `Denied`). `ServiceRow` maps the manager's numbers to states and start types.
- `StartupList`, `StartupEntry`, `StartupPlace`, `CommandLine` (PR-5): the three Run keys and the two Startup folders; `IsEnabled` reads Task Manager's `StartupApproved` bytes (odd first byte: turned off); `CommandLine.ProgramOf` finds the program of a command (the quoted path, the text through `.exe`, or a rooted path). `StartupList.Apply(places, entry, action)` turns an entry off or on (the `StartupApproved` value written) or removes it (the Run key's value or the folder's file deleted, then its `StartupApproved` value), and answers a `StartupResult`: done, already so, needs administrator rights, gone, denied, or failed with Windows' message (PR-5.4 to PR-5.7).
- `StartupPlaces` (PR-5.8): where the five places are, as data. `Current` is Windows' own for the user's window and a stand-in for a window of a test run (`AxKit.Core/AppPaths.RunMark`): the key `HKEY_CURRENT_USER\Software\AxKit\Runs\<mark>\Startup` with the same key paths under `User` and `Computer`, and the folders `AxKit\startup\yours` and `everyone` of the run's folder. `Under(root, folder)` makes a stand-in, refused outside `Software\AxKit`, never with administrator rights. `RunKeyOf`, `ApprovedKeyOf` and `FolderOf` give a place's key or folder; every read and write goes through them.
- `StartupChange`, `StartupAction`, `StartupOutcome`, `StartupResult` (PR-5.4 to PR-5.7), all pure: `NeedsAdministrator` (the computer's Run keys and all users' Startup folder), `ApprovedBytes` (12 bytes: 02 and zeros for on; 03, three zeros and a FILETIME for off), `ApprovedName`, `Refusal` (what is refused before anything is tried), `RemoveQuestion` and `RemoveText`, `Said` (the one sentence of each outcome).
- `ProcessSettings` (PR-7.1): the settings file with the order of the Processes view.

## Gotchas

- `Process.TotalProcessorTime` throws for protected processes (System, the antivirus); their memory is still readable, so they are listed without a processor share.
- The working set is what Task Manager's Details tab calls memory; its Processes tab shows the private working set, a little smaller. The spec says working set.
- SAFETY (SPEC.md PR-5.8): no test and no check script may change a real start-up entry of the computer, not even one it made itself. Tests give `StartupList.Apply` a stand-in from `StartupPlaces.Under` and never name `StartupPlaces.Current`; `StartupChangeTests.No_test_names_the_real_places` reads the test sources for both. The tests' stand-in is `HKEY_CURRENT_USER\Software\AxKit\Tests\Startup-<random>` and a temporary folder, deleted when the test ends.
- A `StartupApproved` value is in the hive of its entry: under HKEY_CURRENT_USER for the user's Run key and Startup folder, under HKEY_LOCAL_MACHINE for the computer's, which a user may not write. So the computer's entries are refused before anything is tried (`StartupChange.Refusal`), unless the process has administrator rights.
- `ControlService` writes a `SERVICE_STATUS`, smaller than the `SERVICE_STATUS_PROCESS` it is given here, which is safe; the wait reads the full status with `QueryServiceStatusEx`.
