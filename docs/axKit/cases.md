# Cases

Written by `tools/cases.ps1` from `docs/axKit/cases.json`; never edit it by hand. How axKit handles each unexpected
situation: what it does, what it says, and the test or window script that proves it. A change to how a situation
is handled changes its row in the JSON in the same commit, then the script writes this page; `CasesTests` holds
every proof and every sentence named here to the code (`docs/axKit/SPEC.md` AX-5.10).

## Where it stands

- 64 cases, 55 with a proof, 9 without.
- A settings file: 19.
- A sealed setting: 12.
- A key: 4.
- A remembered path: 2.
- A data folder or file: 9.
- An outside program: 7.
- Claude's working folder: 5.
- A list of several windows: 2.
- A background app: 2.
- A report: 1.
- An update: 1.

## Not proved yet

- C-34, a key, held by another program. Owed: a window script in which another process holds the hotkey before the app starts; no script registers a key of its own yet.
- C-35, a key, held by another program. Owed: a window script in which another process holds the key that is chosen in the Hotkey notice.
- C-39, a data folder or file, changed outside axKit. Owed: a window script that activates the window after the change (a test window is never activated), or the step by ear of docs/axDown/nvda-test-plan.md.
- C-40, a data folder or file, gone while the window is open. Owed: a window script that deletes the open file, then saves, and finds the file written again.
- C-48, an outside program, missing. Owed: a window script that starts axGIT with git's program pointed at a file that is not there.
- C-49, an outside program, missing. Owed: a window script that starts axPass in a run where the tool is not found; the scripts of axPass need KeePassXC installed.
- C-50, an outside program, missing. Owed: a window script that asks for a shell whose program is not installed in the run.
- C-55, Claude's working folder, cannot be written. Owed: a test of AiRequest's run with a no-hooks file that cannot be written.
- C-62, an update, changed outside axKit. Owed: a run of a real update at the next release; no test reaches the extraction.

## A settings file

### A settings file: missing

**C-1, every app.**

- Does: Uses the defaults and writes the file at the first save.
- Says: nothing: a first start is no fault
- Proved by: `AxKit.Tests/SettingsFileTests.Missing_file_gives_defaults_without_an_error`.
- From: AX-5.3.

**C-12, every app.**

- Does: A missing recently closed list is an empty list; the Window menu offers nothing to open again.
- Says: nothing: an empty list is no fault
- The file: `AppPaths.RecentlyClosedFile`.
- Proved by: `AxKit.Tests/RecentlyClosedTests.The_file_round_trips_and_a_missing_file_is_an_empty_list`.
- From: AX-9.4.

**C-16, every app.**

- Does: Without a theme file every window is painted with the system's colours; Reload theme says how to make one.
- Says: No theme file: axKit menu, Theme, Edit theme file makes one
- The file: `AppPaths.ThemeFile`.
- Proved by: `tests/scripts/axDown/10-theme.txt: Reloading with no theme file says how to make one`.
- From: TH-3.4.

**C-19, every app.**

- Does: A missing reports settings file gets a new reporter id at the first report, kept from then on.
- Says: nothing: the id is made without a question
- The file: `AppPaths.ReportsFile`.
- Proved by: `AxKit.Tests/ReportSenderTests.The_reporter_id_is_made_once_and_kept`.
- From: RP-6.3.

**C-63, every app.**

- Does: Without the bundle's settings file the update check at start follows the apps' own settings of before: off when any app had turned it off, on otherwise; the first change in the axKit menu writes the file.
- Says: Nothing
- The file: `AppPaths.BundleSettingsFile`.
- Proved by: `AxKit.Tests/BundleSettingsTests.Without_the_file_the_switch_follows_the_apps_of_before`.
- From: AX-3.1.

### A settings file: cannot be opened

**C-2, every app.**

- Does: Reads again three times, a tenth of a second apart; a file still held gives the defaults for this run, and no save of this run writes over it.
- Says: The settings file could not be read, so the defaults are used. <path>: <the system's reason> Settings are not saved in this run, so that the file is not replaced. Close the window and start it again.
- The words in the code: `SettingsFile.NotReadHeading` and `SettingsFile.NotSaved`.
- Proved by: `AxKit.Tests/SettingsFileTests.A_file_that_cannot_be_opened_is_never_replaced_by_the_defaults`.
- From: R-51.

### A settings file: cannot be written

**C-3, every app.**

- Does: Keeps what was changed until the window closes, logs every failed save and says it once in a run: a notice in a window, a system line in a console window.
- Says: Settings could not be saved: <the system's reason>. What you change lasts until this window closes.
- Proved by: `AxKit.Tests/SettingsFileTests.A_file_that_cannot_be_written_is_said_once_in_a_run_with_the_reason`.
- From: R-63.

### A settings file: a value does not read

**C-4, every app.**

- Does: One value that does not read is put to its default alone and named, the rest of the file is loaded, and the file as it was is copied once beside it with .bad at the end of its name.
- Says: The settings file was read, except for the settings named here. <path>: these settings were not understood and are at their defaults: <the names>. A copy is kept as <path>.bad.
- The words in the code: `SettingsFile.PartlyReadHeading` and `SettingsFile.NotUnderstood`.
- Proved by: `AxKit.Tests/SettingsFileTests.One_value_that_does_not_read_costs_that_value_alone`.
- From: R-52.

**C-5, every app.**

- Does: A file whose text is no JSON object gives the defaults, and the file as it was is copied once beside it with .bad at the end of its name before the next save writes the defaults.
- Says: The settings file could not be read, so the defaults are used. <path>: <the reader's reason> A copy is kept as <path>.bad.
- The words in the code: `SettingsFile.NotReadHeading`.
- Proved by: `AxKit.Tests/SettingsFileTests.Broken_file_gives_defaults_and_names_the_file`.
- From: AX-5.3.

**C-6, every app.**

- Does: A stored window place that lies partly or wholly off the screens is fitted into the working area of the screen it shares the most with; one on no screen is dropped and Windows places the window.
- Says: nothing: the window opens where it can be seen
- Proved by: `AxKit.Tests/WindowPlaceTests.A_place_that_touches_a_screen_by_a_corner_is_moved_wholly_inside`.
- From: R-58.

**C-7, axHelp.**

- Does: A model name that is not letters, digits, dots, hyphens and brackets is replaced by the fast model at the load and again where the command line is made; the log holds its length.
- Says: nothing: the default model answers
- Proved by: `AxHelp.Tests/HelpTests.Only_a_models_name_is_kept_and_reaches_the_command_line`.
- From: R-71.

**C-11, every app.**

- Does: A consent file that is no JSON counts as nothing agreed, so each access layer asks again before it connects to its program.
- Says: nothing by itself: the reminder before the first connection is shown again
- The file: `AppPaths.ConsentFile`.
- Proved by: `AxKit.Tests/ConsentTests.A_damaged_file_means_nothing_was_agreed`.
- From: AX-11.5.

**C-13, every app.**

- Does: An entry of the recently closed list that is not a whole path is neither read nor written, so no entry can be taken as an option of a command line.
- Says: nothing: the entry is left out
- The file: `AppPaths.RecentlyClosedFile`.
- Proved by: `AxKit.Tests/RecentlyClosedTests.Only_whole_paths_are_read_from_the_file_and_written_to_it`.
- From: R-68.

**C-17, every app.**

- Does: A theme file that is no JSON or no theme file is not used: the windows keep the system's colours, the log says why once, and Reload theme says so.
- Says: No theme: the file is not a theme file; the log says why
- The file: `AppPaths.ThemeFile`.
- Proved by: `AxKit.Tests/ThemeTests.Enabled_false_is_read_and_a_file_that_is_not_a_theme_is_refused_with_the_reason`.
- From: TH-3.4.

**C-18, every app.**

- Does: A colour of the theme file that does not read is the system's colour, and Reload theme counts and names the values not understood.
- Says: Theme reloaded: 1 value not understood, <the value's name>
- The file: `AppPaths.ThemeFile`.
- Proved by: `AxKit.Tests/ThemeTests.A_hex_colour_is_read_system_in_any_case_and_a_bad_value_is_system_and_counted`.
- From: TH-3.4.

**C-64, every app.**

- Does: An entry of the app keys file that names no app, names one a second time, is no key, is not Ctrl with a function key for a standalone app or Ctrl+Shift with one for an access layer, is a bundle key or is another app's key is skipped, and its app keeps its default key; a file that is no JSON object gives every default. Each is a line of axKit.log with its reason; nothing breaks.
- Says: nothing in the window: the log holds the reason
- The file: `AppPaths.AppKeysFile`.
- Proved by: `AxKit.Tests/AppLaunchKeysTests.Each_refused_entry_is_skipped_with_its_reason_and_its_app_keeps_its_default`.
- From: AX-8.18.

### A settings file: written by two windows

**C-8, every app.**

- Does: A save keeps what the file gained since this window loaded it: each top-level setting keeps the file's value unless this window changed it, and a list both windows changed is this window's whole.
- Says: nothing: nothing is lost that needs an answer
- Proved by: `AxKit.Tests/SettingsFileTests.A_save_keeps_what_another_window_saved_since_this_one_loaded_the_file`.
- From: R-62.

### A settings file: from a newer version

**C-9, every app.**

- Does: Members this version does not know are kept and written back as they were, under no seal.
- Says: nothing: the newer version finds its settings again
- Proved by: `AxTerminal.Tests/TerminalSealTests.A_member_this_version_does_not_know_is_kept_and_is_under_no_seal`.
- From: R-67.

**C-10, every app.**

- Does: The consent file and the recently closed list keep the members this version does not know and write them back.
- Says: nothing: the newer version finds them again
- The file: `AppPaths.ConsentFile`.
- Proved by: `AxKit.Tests/ConsentTests.Members_this_version_does_not_know_are_kept_in_the_consent_file_and_in_the_recently_closed_list`.
- From: R-67.

## A sealed setting

### A sealed setting: changed outside axKit

**C-20, axTerminal.**

- Does: Starts the default shell, uses none of the shells of the file, keeps the other settings, and keeps the file as it was found beside it with .changed at the end of its name; said once, as a system line.
- Says: Your shells in axTerminal's settings were changed outside axKit and are not used. Add them again from Session, Add a shell. The file as it was found is kept beside it, with .changed at the end of its name.
- The words in the code: `TerminalSettings.ShellsNotUsed` and `SettingsSeal.CopyKept`.
- Proved by: `AxTerminal.Tests/TerminalSealTests.A_command_line_changed_by_hand_is_not_used_and_the_other_settings_stay`.
- From: R-53.

**C-21, axGIT.**

- Does: Trusts no repository, so git runs none of their own programs, keeps the other settings, and keeps the file as it was found beside it with .changed at the end of its name.
- Says: The repositories you trusted in axGIT's settings were changed outside axKit and are not used: git runs none of their own programs. To trust one again, open it and choose Trust this repository. The file as it was found is kept beside it, with .changed at the end of its name.
- The words in the code: `GitSettings.TrustNotUsed` and `SettingsSeal.CopyKept`.
- Proved by: `AxGit.Tests/GitSealTests.A_repository_added_by_hand_is_not_trusted_and_the_other_settings_stay`.
- From: R-53.

**C-22, axGIT.**

- Does: A trusted repository whose own programs are not the ones it named when it was trusted is not trusted for them: its programs stay off and the notice asks again.
- Says: This repository runs programs of its own (the notice's title, then the programs by name)
- Proved by: `AxGit.Tests/GitSealTests.A_trusted_repository_that_gains_a_hook_is_asked_about_again`.
- From: R-53.

**C-23, axFile.**

- Does: Offers none of the custom programs of Open with, keeps the other settings, and keeps the file as it was found beside it with .changed at the end of its name.
- Says: Your programs of Open with in axFile's settings were changed outside axKit and are not used. Add them again from Options, Add a program to Open with. The file as it was found is kept beside it, with .changed at the end of its name.
- The words in the code: `FileSettings.ProgramsNotUsed` and `SettingsSeal.CopyKept`.
- Proved by: `AxFile.Tests/FileSealTests.A_program_changed_by_hand_is_not_used_and_the_other_settings_stay`.
- From: R-53.

**C-24, axMCP.**

- Does: Lists none of the user's own servers, keeps the other settings, and keeps the file as it was found beside it with .changed at the end of its name.
- Says: Your servers in axMCP's settings were changed outside axKit and are not used. Add them again with Ctrl+N at the top level. The file as it was found is kept beside it, with .changed at the end of its name.
- The words in the code: `MCPSettings.ServersNotUsed` and `SettingsSeal.CopyKept`.
- Proved by: `AxMCP.Tests/MCPSealTests.A_file_from_before_the_seal_is_accepted_sealed_by_the_next_save_and_then_must_keep_its_seal`.
- From: R-53.

**C-25, axClaude.**

- Does: Does not use the place of Claude Code named in the file and looks for Claude Code in the usual places; the other settings stay.
- Says: The place of Claude Code in axClaude's settings, claudePath, was changed outside axKit and is not used: axClaude looks for Claude Code in the usual places. If it is not found, choose it again with Locate claude.exe in the notice that says so. The file as it was found is kept beside it, with .changed at the end of its name.
- The words in the code: `AppSettings.ClaudePathNotUsed` and `SettingsSeal.CopyKept`.
- Proved by: `AxClaude.Tests/SettingsSealTests.A_path_changed_by_hand_is_not_used_and_the_other_settings_stay`.
- From: R-53.

**C-26, every app.**

- Does: AI help reads as off, with no folder Claude may read, the usual search for Claude Code and the default working folder; the model is kept. The next thing that needs the settings shows the notice AI help and does not go on with what was asked.
- Says: AI help's settings were changed outside axKit and are not used: Claude is off, it may read no folder of yours, and Claude Code and its working folder are the usual ones. Turn Claude on and add the folders again from the axKit menu, Claude. The file as it was found is kept beside it, with .changed at the end of its name.
- The words in the code: `AiSettings.NotUsed` and `SettingsSeal.CopyKept`.
- The file: `AppPaths.AiHelpFile`.
- Proved by: `AxKit.Tests/SettingsSealTests.AI_help_from_before_the_seal_is_accepted_sealed_by_the_next_save_and_then_must_keep_its_seal`.
- From: R-53.

**C-27, every app.**

- Does: An agreement that something other than axKit wrote into the consent file is no agreement: nothing counts as agreed, and each access layer asks again.
- Says: The record of what you agreed to was changed outside axKit and is not used, so axKit asks again.
- The words in the code: `Consent.NotUsed`.
- The file: `AppPaths.ConsentFile`.
- Proved by: `AxKit.Tests/SettingsSealTests.A_consent_another_program_added_is_not_the_users_agreement`.
- From: R-53.

**C-28, every app.**

- Does: The AI tools read as off with no paired tool; the axKit menu's AI tools submenu says why at every opening until the tools are turned on again or forgotten, and the log holds it once in a process.
- Says: The AI tools' setting was changed outside axKit, or its key is gone: AI tools are off, and every AI tool must be allowed again.
- The words in the code: `McpAccess.ChangedOutside`.
- The file: `AppPaths.McpFile`.
- Proved by: `AxKit.Tests/McpTests.A_file_counts_only_with_its_seal`.
- From: R-56.

**C-29, axHelp.**

- Does: The list of the files accepted in Claude's working folder is not used when it was changed by hand or by another program: every obeyed file is asked about again.
- Says: The list of files accepted in Claude's working folder was changed outside axKit and is not used: every such file is asked about again. (in the log; the window shows the notice of the files)
- The words in the code: `AiFolderCheck.ListNotUsed`.
- The file: `AppPaths.AiFolderFilesFile`.
- Proved by: `AxKit.Tests/AiFolderCheckTests.The_accepted_list_is_for_one_folder_and_refuses_a_hand_edit`.
- From: R-54.

**C-30, every app.**

- Does: The list of the reports the report form wrote is not used when it was changed outside axKit, and taking the file away does not bring back the time before it: no report saved before then is sent.
- Says: The list of the reports axKit's report form wrote was changed outside axKit and is not used: reports saved before now are not sent. (in the log; the window says how many files were not sent)
- The words in the code: `ReportWritten.ListNotUsed`.
- The file: `AppPaths.ReportsWrittenFile`.
- Proved by: `AxKit.Tests/ReportSenderTests.The_list_refuses_a_hand_edit_and_taking_it_away_does_not_bring_back_the_time_before_it`.
- From: R-57.

### A sealed setting: written by two windows

**C-31, axTerminal.**

- Does: A save takes another window's shells from the file only when the file's seal checks; from a file whose seal does not check it takes nothing.
- Says: nothing: the merge needs no answer
- Proved by: `AxTerminal.Tests/TerminalSealTests.A_save_takes_nothing_from_a_file_whose_seal_does_not_check`.
- From: R-62.

## A key

### A key: missing

**C-33, every app.**

- Does: A sealed file whose key file is gone is refused: its sealed settings are at their defaults until they are set again through axKit.
- Says: the refusal sentence of each sealed file, as in its own row
- Proved by: `AxKit.Tests/SettingsSealTests.A_sealed_file_without_its_key_file_is_refused`.
- From: AX-5.6.

### A key: cannot be opened

**C-32, every app.**

- Does: A key file of the settings' seal that is there and cannot be read refuses every sealed setting until a save makes a new key.
- Says: the refusal sentence of each sealed file, as in its own row
- Proved by: `AxKit.Tests/SettingsSealTests.A_key_file_that_cannot_be_read_refuses_everything_until_a_save_makes_a_new_one`.
- From: AX-5.6.

### A key: held by another program

**C-34, axStart, axClip and axHelp.**

- Does: A hotkey that cannot be registered at a start shows the window once with a notice, so that the app can still be reached and another key chosen.
- Says: The hotkey <key> is taken by another program. Options, Hotkey, to choose another.
- Not proved yet. Owed: a window script in which another process holds the hotkey before the app starts; no script registers a key of its own yet.
- From: AS-4.3.

**C-35, axStart, axClip and axHelp.**

- Does: A new hotkey chosen in the Hotkey notice that another program holds is not taken, and the old key stays registered.
- Says: <key> is taken by another program
- Not proved yet. Owed: a window script in which another process holds the key that is chosen in the Hotkey notice.
- From: AS-4.2.

## A remembered path

### A remembered path: on the network and away

**C-36, axFile, axGIT, axTerminal, axClaude, axPass, axTask and axAutomate.**

- Does: A remembered path that is not on a drive of this computer is not looked at during a start: the window opens on its fallback and says the path was not opened.
- Says: <path> is on the network and was not opened at start; Open recent opens it.
- Proved by: `AxKit.Tests/LocalPathsTests.A_remembered_path_on_the_network_is_not_asked_about_at_a_start_and_is_said`.
- From: R-64.

**C-37, every app.**

- Does: No window asks whether a remembered path exists except through LocalPaths, which asks only of a path on this computer; a path elsewhere is listed as it is and found missing when it is chosen.
- Says: nothing while listing
- Proved by: `AxKit.Tests/LocalPathsTests.No_window_asks_whether_a_remembered_path_exists_except_through_LocalPaths`.
- From: R-64.

## A data folder or file

### A data folder or file: cannot be opened

**C-46, axClip.**

- Does: A kept clipboard history that Windows can no longer open for this account starts empty; the settings file as it was is copied once beside it with .bad at the end of its name, and the window says it once, at its first showing.
- Says: The kept clipboard history could not be read on this computer and starts empty.
- The words in the code: `ClipSettings.HistoryNotReadText`.
- Proved by: `AxClip.Tests/ClipHistoryTests.A_kept_history_that_cannot_be_opened_is_kept_once_as_bad_and_is_said`.
- From: R-69.

### A data folder or file: a value does not read

**C-42, axTask.**

- Does: An item file that does not read as an item is a row of its list, named by its file, which Enter opens in axJSON; Rename and Delete leave it alone.
- Says: <file name>, not readable as an item, Enter opens it in axJSON
- Proved by: `tests/scripts/axTask/10-damaged-item.txt: Hear a damaged item in its list`.
- From: R-66.

**C-43, axTask.**

- Does: A list that gathers items (All tasks, a date list, a tag, a filter) says how many item files it could not read and in which lists.
- Says: 2 items not readable, in Home, Work
- Proved by: `AxTask.Tests/FolderTests.A_damaged_item_is_named_in_its_list_and_counted_with_its_lists_by_what_gathers_items`.
- From: R-66.

### A data folder or file: changed outside axKit

**C-38, axDown and axJSON.**

- Does: Save of a file another program changed since it was opened asks first: Write over, Reload and lose mine, or Cancel.
- Says: The file changed on disk (the notice's title). <name> was changed by another program since you opened it. Write over it, or reload it and lose your changes?
- Proved by: `tests/scripts/axDown/03-files.txt: Saving a file that another program changed asks first`.
- From: AD-3.6.

**C-39, axDown, axJSON and axPreview.**

- Does: On coming back to the window, a file changed on disk is read again with the caret kept on its line when nothing is unsaved; with unsaved changes it is said once and Save will ask.
- Says: Reloaded, <name> changed on disk; or, with unsaved changes: <name> changed on disk; Save will ask
- Not proved yet. Owed: a window script that activates the window after the change (a test window is never activated), or the step by ear of docs/axDown/nvda-test-plan.md.
- From: AD-3.6.

**C-44, axTask.**

- Does: An item whose file changed on disk since it was read is read again before a change is written, never written over.
- Says: The item changed on disk; it has been read again
- Proved by: `tests/scripts/axTask/02-lists-links-smart.txt: An item changed on disk is read again, not written over`.
- From: none found in docs/axTask/SPEC.md.

**C-45, axTask.**

- Does: An action key of a list that moves or opens, and that this install neither wrote nor accepted, says what it now does at the first press and acts at the second; a key that sets fields acts at once.
- Says: Ctrl+<digit> in this list now <what it does>. Press it again to do it.
- Proved by: `AxTask.Tests/ListActionsTests.An_action_that_moves_or_opens_asks_first_and_one_that_sets_fields_does_not`.
- From: R-72.

### A data folder or file: gone while the window is open

**C-40, axDown, axJSON and axPreview.**

- Does: A file deleted from outside while it is open changes nothing in the window: the text stays, nothing is reloaded, and Save writes the file again without a question.
- Says: nothing: the text in the window is whole
- Not proved yet. Owed: a window script that deletes the open file, then saves, and finds the file written again.
- From: AD-3.6.

**C-41, axFile.**

- Does: A folder deleted from outside while it is shown gives a notice at the next reading, and the window shows the nearest folder above that still exists.
- Says: Folder not found (the notice's title). <folder> does not exist any more.
- Proved by: `tests/scripts/axFile/01-browse.txt: A folder deleted from outside is said as not found`.
- From: AF-3.5.

## An outside program

### An outside program: missing

**C-47, axClaude.**

- Does: Without Claude Code the window shows a notice that lists the places tried and offers Install Claude Code now first; a restart without it shows the notice again.
- Says: Claude Code was not found (the notice's title, then the places tried)
- Proved by: `tests/scripts/axClaude/10-missing.txt: Hear the ways to install Claude Code`.
- From: FR-1.9.

**C-48, axGIT.**

- Does: Without git the window shows a notice that offers Install Git now and git-scm.com, and stays with every git command disabled.
- Says: Git was not found (the notice's title). axGIT runs the git program, and it is not on this computer: neither on the PATH nor in Program Files.
- Not proved yet. Owed: a window script that starts axGIT with git's program pointed at a file that is not there.
- From: AG-1.5.

**C-49, axPass.**

- Does: Without KeePassXC's command-line tool the window shows a notice that offers Install KeePassXC now and its download page; once installed, the unlock notice follows.
- Says: KeePassXC is not installed (the notice's title)
- Not proved yet. Owed: a window script that starts axPass in a run where the tool is not found; the scripts of axPass need KeePassXC installed.
- From: PW-5.1.

**C-50, axTerminal.**

- Does: A shell that is not found on this computer starts nothing; the window says so and Ctrl+N offers the other shells.
- Says: PowerShell 7 was not found on this computer. Press Ctrl+N and choose another shell.
- Not proved yet. Owed: a window script that asks for a shell whose program is not installed in the run.
- From: none found in docs/axTerminal/SPEC.md.

### An outside program: changed outside axKit

**C-53, axMCP.**

- Does: A server whose program, arguments, environment or folder are not the ones agreed to asks again before it starts, and the notice reads the whole command line, the folder and the variables' names, never a value.
- Says: Before axMCP starts <server> (the notice's title, then the command line)
- Proved by: `AxMCP.Tests/ServerConsentTests.An_agreement_counts_for_the_same_definition_and_for_no_other`.
- From: R-61.

### An outside program: planted

**C-51, axPass.**

- Does: A keepassxc-cli.exe found outside KeePassXC's install folder is the tool only when KeePassXC's publisher signed it; one another publisher signed is passed over, the log holds the signer's name, and the master password is never given to it.
- Says: KeePassXC is not installed (the notice's title), when no other tool is found
- Proved by: `AxPass.Tests/PassTests.OnlyKeePassXCsPublisherIsTheTool`.
- From: R-55.

**C-52, axGIT and axTerminal.**

- Does: git and PowerShell 7 are looked for in their install folders under Program Files, which only an administrator can write, before the PATH; a PATH folder that is not on a drive of this computer is passed over.
- Says: nothing: the installed program runs
- Proved by: `AxKit.Tests/SystemProgramsTests.An_installed_program_is_found_in_its_install_folder_before_the_path_and_never_in_a_path_folder_off_this_computer`.
- From: R-70.

## Claude's working folder

### Claude's working folder: missing

**C-58, every app.**

- Does: A working folder that was deleted is made again when first needed, and every file of axKit's is written into it.
- Says: nothing: the folder is there again
- Proved by: `AxKit.Tests/AiDocumentationTests.The_files_are_written_where_missing_and_left_alone_while_the_version_is_current`.
- From: AH-7.7.

### Claude's working folder: cannot be written

**C-55, every app.**

- Does: When the file that turns the hooks off for a question cannot be written, Claude Code is not run and the answer says so.
- Says: Claude Code was not run. The file that turns off the hooks of Claude's working folder for a question could not be written: <the system's reason>
- Not proved yet. Owed: a test of AiRequest's run with a no-hooks file that cannot be written.
- From: AH-2.6.

### Claude's working folder: changed outside axKit

**C-57, every app.**

- Does: A guide or rule file of axKit's in the folder that was changed, emptied or deleted is written again at the next use; a file of the user's is left as it is.
- Says: nothing: the repair is code alone
- Proved by: `AxKit.Tests/AiDocumentationTests.A_file_of_axKits_that_was_changed_is_put_right_and_a_file_of_the_users_is_left`.
- From: AH-7.7.

### Claude's working folder: not written by axKit

**C-54, every app.**

- Does: A question of Shift+F1 starts Claude Code with the servers of the folder's .mcp.json left out and its hooks off for that run, so no file of the folder starts a program when a question is asked.
- Says: nothing: the question is answered as always
- Proved by: `AxKit.Tests/AiTests.The_command_line_is_print_mode_with_the_read_tools_the_other_folders_the_model_and_the_session`.
- From: R-54.

**C-56, axHelp.**

- Does: Before axHelp starts its conversation, a file Claude Code would obey that axKit did not write and the user has not accepted, or that changed since, is named in a notice whose default is Cancel; nothing starts in the background in such a folder.
- Says: Claude's working folder holds files that can run commands (the notice's title, then the files one a line)
- The words in the code: `AiFolderCheck.NoticeTitle`.
- Proved by: `AxKit.Tests/AiFolderCheckTests.The_servers_file_a_third_rule_a_skill_a_command_and_a_changed_agent_are_named`.
- From: R-54.

## A list of several windows

### A list of several windows: written by two windows

**C-14, every app.**

- Does: The recently closed list is read, added to and written by one window at a time, under a mutex named after the file; a window that waits more than half a second writes nothing, and the log holds a line.
- Says: nothing: every window's entry ends up on the list
- Proved by: `AxKit.Tests/RecentlyClosedTests.Writers_at_the_same_time_all_end_up_on_the_list`.
- From: R-68.

### A list of several windows: does not answer

**C-15, every app.**

- Does: A writer of the recently closed list that cannot get its turn within half a second gives up without hanging the window that closes.
- Says: nothing: the log holds a line
- Proved by: `AxKit.Tests/RecentlyClosedTests.A_writer_that_waits_too_long_writes_nothing_and_does_not_hang`.
- From: R-68.

## A background app

### A background app: does not answer

**C-59, axStart, axClip and axHelp.**

- Does: A second start that finds the first copy not answering asks, in a message box of Windows since no window exists yet, whether to end it and start again; No is the default, and only this program's own process of that app is ended. A start at sign-in and a start in a test run never ask.
- Says: <app> is running but does not answer. End it and start again?
- Proved by: `AxKit.Tests/BackgroundStartTests.A_copy_that_answered_ends_the_second_start_and_one_that_did_not_is_asked_about`.
- From: R-65.

**C-60, axStart, axClip and axHelp.**

- Does: When Yes finds nothing it may end, or the copy would not go, nothing is started and the user is told what to do.
- Says: <app> could not be ended, so it was not started again. Sign out of Windows and in again, or end its AxKit.exe in Task Manager, then start <app>.
- Proved by: `AxKit.Tests/BackgroundStartTests.The_question_and_the_failure_name_the_app`.
- From: R-65.

## A report

### A report: planted

**C-61, every app.**

- Does: The sender sends only the waiting reports whose files axKit's report form wrote; another file in the reports folder is neither sent nor marked, and what the window says after a send ends with how many were not sent.
- Says: 1 file in the reports folder was not written by axKit's report form and was not sent.
- Proved by: `AxKit.Tests/ReportSenderTests.A_planted_item_is_not_sent_and_is_counted_and_the_forms_own_report_is_sent`.
- From: R-57.

## An update

### An update: changed outside axKit

**C-62, every app.**

- Does: The bytes of a downloaded update that were hashed and checked are the bytes that are extracted, so a file changed between the check and the extraction is not installed.
- Says: nothing
- Not proved yet. Owed: a run of a real update at the next release; no test reaches the extraction.
- From: R-59.
