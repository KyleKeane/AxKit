# Outside calls

Everything axKit reaches outside its own process: the network requests its own code makes, the programs it starts and the pipes it opens, by what is reached. For each, what it is, where it is (the program file or the endpoint), how axKit reaches it, what it may reach in turn, its documentation, and every call under it with its app, file and line, the function it stands in, the function it calls and what the call does for the user. The second section is the functions themselves, where each is defined and where it is used, and what is watched for.

`tools/outside-calls.ps1` writes this page from the sources, so it is a list, not a promise: each call in the code carries a comment that starts `Outside call, <kind>, <what is reached>:` on the line above it or at its end, and this page is their list, found by the functions of the second section. A call without that comment fails the script, which `build.yml` runs on every push (with the check that this page is current) and `release.ps1` before every release; `release.ps1` writes the page again and commits it with the release, so it is exact at every tag (bundle SPEC B-17). Do not edit this page by hand: change the comment in the code, or the tables in the script, and run it.

The three kinds: **network**, a request over the network from axKit's own code; **program**, another program started on this computer, Windows' own, one the user installed, or axKit itself; **pipe**, a channel on this computer that another process connects to or that axKit connects to. The access layers (axClaude, axGIT, axTerminal, axPass) meet the network only through the programs listed here, which use it as they would without axKit (bundle SPEC AX-11.11). `PRIVACY.md` says the same for the user in plain words; `SECURITY.md` says what matters for security. The set of apps grows as the author finds another thing worth doing by ear, and every new call enters this page the same way.

Today: 11 network requests, 28 program starts and 4 pipes, in 28 files, under the 11 headings below.

## What axKit reaches

### Network

#### GitHub

- **What**: GitHub, where this repository, its releases and the reports repository `KyleKeane/AxKit-reports` live. The one place axKit's own code reaches over the network: the updater and the report sender (docs/axKit/reports.md).
- **Where**: The updater: `GET https://api.github.com/repos/KyleKeane/AxKit/releases/latest` (the latest release as JSON) and `GET https://github.com/KyleKeane/AxKit/releases/download/<tag>/<file>` (the release's zip and its `.sig`, accepted from that one address only). The reports: `GET https://raw.githubusercontent.com/KyleKeane/AxKit-reports/main/blocklist.json` (the blocklist) and `POST https://api.github.com/repos/KyleKeane/AxKit-reports/issues` (a report as an issue, as axKit's own reporter account). axTask's GitHub lists: `GET https://api.github.com/repos/<repository>/issues` (the issues, no account), and as the user's own account `PATCH .../issues/<n>`, `POST .../issues/<n>/comments` and `GET` and `PUT .../contents/blocklist.json`.
- **How**: .NET's HttpClient over HTTPS with the headers `User-Agent: AxKit/<version>` and `Accept: application/vnd.github+json`; the request carries the version and, as every web request does, the network address, nothing else about the user. A download is installed only when its SHA-256 matches GitHub's digest of the file and the release key's ECDSA P-256 signature (AX-3.4). A report carries what the user read and edited in the report notice, redacted, and the install's random reporter id, never a name or an account (reports.md RP-4, RP-6.3); the reporter account's token ships in the release build and can reach that repository's issues alone (RP-6.2).
- **When**: the update check as a window starts (Options, General, Check for updates at start, a setting that can be off) or on Help, Check for updates; the two downloads when the user chooses Update. A report when the user presses Enter on one (F4), the unsent ones with it and when Your reports opens; never at a window's start (RP-6.7). The lists and the actions when the user opens, refreshes or acts on a GitHub list in axTask (RP-7).
- **Documentation**: https://docs.github.com/en/rest/releases/releases#get-the-latest-release; https://docs.github.com/en/rest/issues/issues; https://docs.github.com/en/rest/repos/contents; GitHub's privacy statement, https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement.
- **Apps**: every window (the updater and the report); axTask (the GitHub lists).

Calls (11):

- **shared by every app**, `src/AxKit.Core/AxKitHttp.cs:19`, in `AxKitHttp.Create`, by `new HttpClient`: the one HTTP client of axKit, for the update check, the two downloads and the reports; no other code makes a network request (AX-11.11).
  Code: `var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };`
- **shared by every app**, `src/AxKit.Core/Reports/GitHubIssues.cs:81`, in `GitHubIssues.CreateAsync`, by `HttpClient.SendAsync`: posts a report as an issue of the reports repository, as axKit's own reporter account, with what the user read and edited in the report notice and the install's random reporter id; only when the user sends a report (docs/axKit/reports.md RP-6.1, PRIVACY.md).
  Code: `using var response = await http.SendAsync(request, cancellation).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Reports/GitHubIssues.cs:93`, in `GitHubIssues.ReadBlocklistAsync`, by `HttpClient.SendAsync`: reads the reports repository's blocklist before a report is sent, so that a blocked install or version sends nothing; one GET with the version in the User-Agent and nothing else about the user (docs/axKit/reports.md RP-6.4).
  Code: `using var response = await http.SendAsync(request, cancellation).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Reports/GitHubIssues.cs:112`, in `GitHubIssues.ListAsync`, by `HttpClient.SendAsync`: lists the issues of a repository for a GitHub list of axTask, page by page, when the user opens or refreshes the list (docs/axKit/reports.md RP-7.1).
  Code: `using var response = await http.SendAsync(request, cancellation).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Reports/GitHubIssues.cs:152`, in `GitHubIssues.PatchAsync`, by `HttpClient.SendAsync`: changes an issue's labels or state as the user's own account, from an action on an item of a GitHub list in axTask (docs/axKit/reports.md RP-7.2).
  Code: `using var response = await http.SendAsync(request, cancellation).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Reports/GitHubIssues.cs:162`, in `GitHubIssues.CommentAsync`, by `HttpClient.SendAsync`: adds the user's comment to an issue as the user's own account, from an action on an item of a GitHub list in axTask (docs/axKit/reports.md RP-7.2).
  Code: `using var response = await http.SendAsync(request, cancellation).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Reports/GitHubIssues.cs:174`, in `GitHubIssues.WriteBlocklistAsync`, by `HttpClient.SendAsync`: reads the blocklist file's version before writing it, as the user's own account, from Block this reporter or Block this version in axTask (docs/axKit/reports.md RP-7.2).
  Code: `using var found = await http.SendAsync(read, cancellation).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Reports/GitHubIssues.cs:195`, in `GitHubIssues.WriteBlocklistAsync`, by `HttpClient.SendAsync`: writes the reports repository's blocklist as the user's own account, from Block this reporter or Block this version in axTask (docs/axKit/reports.md RP-6.4, RP-7.2).
  Code: `using var response = await http.SendAsync(request, cancellation).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Updates/UpdateCheck.cs:130`, in `UpdateCheck.FetchLatestAsync`, by `HttpClient.SendAsync`: asks GitHub for the latest release of this repository, so that the window can say when a newer axKit exists; one GET with the version in the User-Agent header and nothing else about the user; off when the user turns the update check off, run by hand from Check for updates (AX-3, PRIVACY.md).
  Code: `using var response = await http.SendAsync(request, cancellation).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Updates/Updater.cs:65`, in `Updater.DownloadAsync`, by `HttpClient.GetStringAsync`: downloads the release's signature file from this repository's release page, the only address the updater accepts; it is checked against the zip before anything is installed (AX-3.4).
  Code: `var signature = await Http.GetStringAsync(release.SignatureUrl, cancellation).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Updates/Updater.cs:89`, in `Updater.DownloadAsync`, by `HttpClient.GetAsync`: downloads the release's zip from this repository's release page, hashed as it arrives; it is installed only if the hash matches GitHub's digest of the file and the release key's signature (AX-3.4).
  Code: `using (var response = await Http.GetAsync(release.ZipUrl, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))`

### Programs

#### Claude Code

- **What**: Claude Code, Anthropic's command-line program, which the user installs: `claude.exe` of its native installer (`%USERPROFILE%\.local\bin`) or npm's `claude.cmd`, found on the PATH or in those folders, or where the user pointed axClaude.
- **How**: for a session, `claude --ax-screen-reader` in a ConPTY pseudo console (PtyHost): what it prints is read from the console and the user's keys are written to it. For its version and for `claude mcp add` and `claude mcp remove`, a process without a window whose output is read. For AI help (`docs/axKit/ai-help.md`), `claude -p --output-format json --allowedTools Read,Glob,Grep` once per question, the prompt on its standard input, the folders the user allowed as `--add-dir`, a follow-up with `--resume`; read-only tools, so it changes no file and runs no command.
- **What it reaches**: Anthropic's service with the user's account, and whatever else Claude Code does from a terminal, its own update check included, under Anthropic's terms; axKit neither sees nor changes that traffic.
- **Documentation**: https://code.claude.com/docs/en/cli-reference and https://code.claude.com/docs/en/setup; Anthropic's privacy policy, https://www.anthropic.com/legal/privacy.
- **Apps**: axClaude, access layer for Claude Code; Add axKit to Claude Code (axKit menu, AI tools) in every window; AI help in every window (Shift+F1, the axKit menu, Claude); axHelp, access layer for Claude Code, through axClaude.

Calls (5):

- **axClaude**, `src/AxClaude.Core/Pty/ClaudeLauncher.cs:46`, in `ClaudeLauncher.VersionAsync`, by `ProcessRun.RunAsync`: runs the user's Claude Code once with --version, without a window, so that axClaude knows which Claude Code it found and that it runs.
  Code: `var result = await ProcessRun.RunAsync(start, [], timeout).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Ai/AiRequest.cs:74`, in `AiRequest.AskAsync`, by `ProcessRun.RunAsync`: claude -p in its working folder with the folders the user allowed, with read-only tools, the prompt on its standard input (docs/axKit/ai-help.md AH-2.1); Claude Code talks to Anthropic under the user's account.
  Code: `var result = await ProcessRun.RunAsync(start, [prompt], Timeout, cancellation).ConfigureAwait(false);`
- **shared by every app**, `src/AxKit.Core/Pty/PtyHost.cs:107`, in `PtyHost.Start`, by `CreateProcess (Windows)`: the one place a program is started in a ConPTY pseudo console: Claude Code for axClaude, the user's shell for axTerminal; what the program prints is read from the console and the user's keys are written to it.
  Code: `created = Native.CreateProcess(null, options.CommandLine, IntPtr.Zero, IntPtr.Zero, false,`
- **shared by every app**, `src/AxKit.Forms/AxKitForm.Mcp.cs:206`, in `AxKitForm.RunClaude`, by `Process.Start`: runs the user's Claude Code without a window for "claude mcp add" and "claude mcp remove", so that Add axKit to Claude Code registers axKit's AI tools there (docs/axKit/mcp.md MC-6.8).
  Code: `using var process = System.Diagnostics.Process.Start(start);`
- **shared by every app**, `src/AxKit.Forms/ConsoleForm.Process.cs:51`, in `ConsoleForm.StartProcess`, by `PtyHost.Start`: starts the window's program in a pseudo console: "claude --ax-screen-reader" for axClaude, the chosen shell for axTerminal.
  Code: `host = PtyHost.Start(options, OnData, OnExited);`

#### Git

- **What**: Git for Windows, the user's `git.exe` on the PATH.
- **How**: one process per command, `git -C <repository> -c core.quotepath=false -c color.ui=never <command>`, with `GIT_TERMINAL_PROMPT=0`, `LC_ALL=C.UTF-8` and `GIT_PAGER=cat`; both outputs are read under a time limit.
- **What it reaches**: the repository's remotes on fetch, pull and push alone, after the user's consent for that server (AX-11.5); git's credential helper may ask for a sign-in in its own window. Every other command works on the repository on disk.
- **Documentation**: https://git-scm.com/docs.
- **Apps**: axGIT, access layer for Git.

Calls (1):

- **axGIT**, `src/AxGit.Core/GitRunner.cs:111`, in `GitRunner.RunAsync`, by `ProcessRun.RunAsync`: runs the user's installed git on the repository for one command and reads what it prints, one process per command, its terminal prompts off; fetch, pull and push reach the repository's remotes, every other command works on disk (AX-11.11).
  Code: `result = await ProcessRun.RunAsync(info, [], timeout, cancellation);`

#### Shells

- **What**: the user's shell for axTerminal: Windows PowerShell (`powershell.exe`) and Command Prompt (`cmd.exe`), Windows' own by their full paths; Git Bash (`Git\bin\bash.exe` of Git for Windows, never a bash.exe on the PATH); PowerShell 7 (`pwsh.exe` on the PATH or under Program Files).
- **How**: in a ConPTY pseudo console (PtyHost), as a terminal runs it, with axTerminal's prompt marks handed in through the environment.
- **What it reaches**: whatever the commands the user types reach, with the user's rights.
- **Documentation**: https://learn.microsoft.com/windows/console/pseudoconsoles; docs/axTerminal/SPEC.md.
- **Apps**: axTerminal, access layer for your shell.

Calls (2):

- **shared by every app**, `src/AxKit.Core/Pty/PtyHost.cs:107`, in `PtyHost.Start`, by `CreateProcess (Windows)`: the one place a program is started in a ConPTY pseudo console: Claude Code for axClaude, the user's shell for axTerminal; what the program prints is read from the console and the user's keys are written to it.
  Code: `created = Native.CreateProcess(null, options.CommandLine, IntPtr.Zero, IntPtr.Zero, false,`
- **shared by every app**, `src/AxKit.Forms/ConsoleForm.Process.cs:51`, in `ConsoleForm.StartProcess`, by `PtyHost.Start`: starts the window's program in a pseudo console: "claude --ax-screen-reader" for axClaude, the chosen shell for axTerminal.
  Code: `host = PtyHost.Start(options, OnData, OnExited);`

#### KeePassXC

- **What**: KeePassXC's command-line tool, `keepassxc-cli.exe`, from the user's installation of KeePassXC, found in its folder or on the PATH.
- **How**: one process per command with the database's path, the master password on its standard input, both outputs read under a time limit.
- **What it reaches**: nothing on the network: the tool has no network feature; it reads and writes the database file.
- **Documentation**: https://keepassxc.org/docs/KeePassXC_UserGuide#_command_line_tool; docs/axPass/kdbx-notes.md.
- **Apps**: axPass, access layer for KeePassXC.

Calls (1):

- **axPass**, `src/AxPass.Core/KeePassCli.cs:79`, in `KeePassCli.Run`, by `ProcessRun.Run`: runs the user's installed keepassxc-cli.exe for one command on the database, the master password on its standard input, and reads what it prints; the tool has no network feature.
  Code: `var result = ProcessRun.Run(info, input, TimeSpan.FromMilliseconds(TimeoutMs));`

#### Installers

- **What**: the installers of Install it now (AX-2.9), run only when the user chooses to install a program an app needs and Windows lacks: Windows' own winget (`%LOCALAPPDATA%\Microsoft\WindowsApps\winget.exe`, never one on the PATH) for Git (`Git.Git`), KeePassXC (`KeePassXCTeam.KeePassXC`) and PowerShell 7 (`Microsoft.PowerShell`); Windows PowerShell running Anthropic's installer script for Claude Code.
- **How**: `winget install --id <id> --exact --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity`, without a window, its output read; `powershell.exe` running `irm https://claude.ai/install.ps1 | iex`.
- **What it reaches**: winget's own source and the maker's download server for the installer; `claude.ai` for Claude Code's installer. The installer then runs under its maker's terms; one that needs administrator rights asks Windows, and Windows asks the user.
- **Documentation**: https://learn.microsoft.com/windows/package-manager/winget/install; https://code.claude.com/docs/en/setup.
- **Apps**: the access layer whose program is missing; axTerminal for PowerShell 7 and Git Bash.

Calls (1):

- **shared by every app**, `src/AxKit.Forms/AxKitForm.Install.cs:122`, in `AxKitForm.RunInstallerAsync`, by `Process.Start`: runs the installer of a missing program the user chose in Install it now (AX-2.9): Windows' own winget from its own source for Git, KeePassXC and PowerShell 7, or Anthropic's installer script for Claude Code in Windows PowerShell; each downloads the maker's installer.
  Code: `process = Process.Start(info);`

#### Windows

- **What**: Windows itself: the shell (File Explorer's parts) and Windows' own programs by their full paths.
- **How**: ShellExecute with a file (the shell opens it with the program Windows has for it, or shows its Open with dialog), a web address (the default browser) or the verb `properties`; the shell's context menu interface IContextMenu for the Windows menu and the shell's own commands; the Open with dialog `SHOpenWithDialog`; `explorer.exe` for File Explorer; `ms-windows-store:` for axKit's page in the Microsoft Store app and `ms-settings:` for Windows' Startup apps page in the Settings app (the Store's copy, AX-13); `powershell.exe` for the installer's `install.ps1` and for axPreview's own script that asks Windows' text reader (`Windows.Media.Ocr`) for the text in a picture (docs/axPreview/SPEC.md AP-D11); the Service Control Manager to start a service.
- **What it reaches**: nothing on the network by itself; the browser, the program Windows picks for a file, a shell extension or a service then act on their own.
- **Documentation**: https://learn.microsoft.com/windows/win32/shell/launch; https://learn.microsoft.com/windows/win32/api/shobjidl_core/nn-shobjidl_core-icontextmenu; https://learn.microsoft.com/windows/win32/services/starting-a-service.
- **Apps**: every window (links, Show in File Explorer, updates); axFile; axPreview; axProcess.

Calls (14):

- **axFile**, `src/AxFile/FileForm.Operations.cs:698`, in `FileForm.ShowFolderInExplorer`, by `Process.Start`: opens File Explorer on This PC when the user asks for the current folder in File Explorer while at the drives level; Windows' own program by its full path.
  Code: `Process.Start(new ProcessStartInfo(SystemPrograms.Explorer, "shell:MyComputerFolder") { UseShellExecute = false })?.Dispose();`
- **axFile**, `src/AxFile/FileForm.Operations.cs:782`, in `FileForm.OpenWithWindows`, by `Process.Start`: opens the selected file or folder with the program Windows has for it, as Enter in File Explorer does (AF-3.2), through the Windows shell; when Windows has none, its Open with dialog follows.
  Code: `Process.Start(new ProcessStartInfo(entry.Path) { UseShellExecute = true })?.Dispose();`
- **axFile**, `src/AxFile/Shell.cs:68`, in `Shell.Properties`, by `ShellExecuteEx (Windows)`: shows Windows' Properties sheet for the entry (Alt+Enter), the shell's own window, through the shell's verb "properties".
  Code: `if (ShellExecuteExW(ref info))`
- **axFile**, `src/AxFile/Shell.cs:116`, in `Shell.ShowMenu`, by `IContextMenu.InvokeCommand (Windows)`: runs the item the user picked in the Windows menu, the shell's own context menu for the entry (More from Windows); what it does is the shell's or an installed shell extension's.
  Code: `var hr = ((IContextMenu)parts.Handler).InvokeCommand(ref info);`
- **axFile**, `src/AxFile/Shell.cs:326`, in `Shell.Invoke`, by `IContextMenu.InvokeCommand (Windows)`: runs one command of the shell's own menu for an entry or a drive without showing the menu: the Windows operations axFile's items stand for (the shell's cut, copy and paste; Eject for a drive).
  Code: `var hr = contextMenu.InvokeCommand(ref info);`
- **shared by every app**, `src/AxKit.Core/SignInStart.cs:57`, in `SignInStart.OpenWindowsStartupSettings`, by `Process.Start`: opens Windows' Startup apps page in the Settings app through the Windows shell (the ms-settings: address), from the Store's copy's Start with Windows item (AX-13.4).
  Code: `Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true })?.Dispose();`
- **shared by every app**, `src/AxKit.Core/Updates/Updater.cs:183`, in `Updater.InstallInPlaceAsync`, by `Process.Start`: runs the downloaded version's install.ps1 in Windows PowerShell, by its full path, to install the update for this user while the app keeps running (AX-2.8).
  Code: `using var process = Process.Start(start) ?? throw new InvalidOperationException("PowerShell could not be started.");`
- **shared by every app**, `src/AxKit.Forms/AxKitForm.cs:815`, in `AxKitForm.OpenUrl`, by `Process.Start`: opens a web page in the user's default browser through the Windows shell, for the Help menu's links, the release page and links the user presses Enter on; axKit itself fetches nothing.
  Code: `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });`
- **shared by every app**, `src/AxKit.Forms/AxKitForm.cs:840`, in `AxKitForm.OpenInExplorer`, by `Process.Start`: opens File Explorer on a folder, or with a file selected, for Show in File Explorer; Windows' own program by its full path.
  Code: `Process.Start(new ProcessStartInfo(SystemPrograms.Explorer, arguments) { UseShellExecute = false })?.Dispose();`
- **shared by every app**, `src/AxKit.Forms/AxKitForm.Updates.cs:156`, in `AxKitForm.OpenStorePage`, by `Process.Start`: opens axKit's page in the Microsoft Store app through the Windows shell (the ms-windows-store: address), from the Store's copy's Check for updates (AX-13.3); axKit downloads nothing.
  Code: `System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(StoreListing.PageUri) { UseShellExecute = true });`
- **shared by every app**, `src/AxKit.Forms/TextEditorForm.Viewer.cs:274`, in `TextEditorForm.OpenInWindowsProgram`, by `Process.Start`: opens the viewed file with the program Windows has for it (axPreview's Open with Windows default, AP-7.3), through the Windows shell; never a file that would run (S-46).
  Code: `Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();`
- **shared by every app**, `src/AxKit.Forms/WindowsOpenWith.cs:23`, in `WindowsOpenWith.Show`, by `SHOpenWithDialog (Windows)`: shows Windows' own Open with dialog for a file, and Windows runs the program the user picks in it (axFile's and axPreview's Open with, Choose another program).
  Code: `var hr = SHOpenWithDialog(owner, in info);`
- **axPreview**, `src/AxPreview.Core/PictureText.cs:112`, in `PictureText.RecognizeAsync`, by `ProcessRun.RunAsync`: Windows PowerShell by its full path, running axPreview's own script beside its logs, which asks Windows' text reader (Windows.Media.Ocr) for the text in one picture; on this computer, no network.
  Code: `return Read(await ProcessRun.RunAsync(Start(script, picturePath), [], Timeout, cancellation).ConfigureAwait(false));`
- **axProcess**, `src/AxProcess.Core/ServiceManager.cs:171`, in `ServiceManager.Start`, by `StartService (Windows)`: starts a Windows service the user chose in axProcess (Services view, Start), through the Service Control Manager; Windows runs the service's own program.
  Code: `StartService(service, 0, null) ? 0 : Marshal.GetLastWin32Error(), ErrorAlreadyRunning);`

#### User programs

- **What**: programs the user chose: one added to axFile's Open with list, by the path the user gave (a script runs through Windows PowerShell); one picked in axStart, by its name in the shell's Applications folder or its target file.
- **How**: started through the shell with the selected entries' paths as arguments (axFile), or as the Start menu starts it, as administrator when the user asks (axStart).
- **What it reaches**: whatever the program does, with the user's rights.
- **Documentation**: docs/axFile/SPEC.md (Open with); docs/axStart/SPEC.md.
- **Apps**: axFile; axStart.

Calls (2):

- **axFile**, `src/AxFile/FileForm.Programs.cs:74`, in `FileForm.RunProgram`, by `Process.Start`: starts a program the user added to Open with on the selected entries; the user's own program, by the path they gave, a script through Windows PowerShell.
  Code: `Process.Start(UserPrograms.StartInfo(program, paths))?.Dispose();`
- **axStart**, `src/AxStart/AppsFolder.cs:116`, in `AppsFolder.Start`, by `Process.Start`: starts the program the user picked in axStart, by its name in the shell's Applications folder as the Start menu does, or its target file as administrator when asked.
  Code: `Process.Start(info)?.Dispose();`

#### axKit

- **What**: axKit itself, this very program file: a new window of an app, the restart under its own name when started through an Open with hard link, the restart after an update, a window the AI tool asked for.
- **How**: a new process of this program file (or the installed `AxKit.exe` after an update) with the app's verb and path; a test run's variables go only to a window of the run (S-11).
- **What it reaches**: nothing new: the same program.
- **Documentation**: docs/axKit/SPEC.md AX-1, AX-2.3, AX-3, AX-9.
- **Apps**: every window.

Calls (4):

- **shared by every app**, `src/AxKit.Core/Updates/Updater.cs:236`, in `Updater.StartInstalled`, by `Process.Start`: starts the installed AxKit.exe again after an update, with the window's own arguments, so that the user comes back to the same app (Update and restart).
  Code: `Process.Start(start)?.Dispose();`
- **shared by every app**, `src/AxKit.Forms/McpBroker.cs:300`, in `McpBroker.Open`, by `Process.Start`: starts an axKit app for the AI tool axkit_open, this very program file with a checked verb and path, through the shell so that the new window inherits none of the broker's pipes (docs/axKit/mcp.md MC-6).
  Code: `using var process = Process.Start(start);`
- **shared by every app**, `src/AxKit.Forms/WindowMenu.cs:183`, in `WindowMenu.StartWith`, by `Process.Start`: starts another window of axKit from the Window menu (New window, Show current folder in, Open in axKit): this very program file with the app's verb and path.
  Code: `Process.Start(info)?.Dispose();`
- **shared by every app**, `src/AxKit/Program.cs:143`, in `Program.StartedAsOpenWithLink`, by `Process.Start`: starts AxKit.exe itself with the same arguments when this process was started under an app's hard-linked name from Windows' Open with (axDown.exe), so that every window runs as AxKit.exe (AX-2.3).
  Code: `using var started = System.Diagnostics.Process.Start(start);`

### Pipes

#### AI tools

- **What**: the paired AI tool, Claude Code as an MCP client, which starts `AxKit.exe mcp` as its server once the user has turned the AI tools on and paired it (axKit menu, AI tools).
- **How**: MCP's JSON-RPC, one line per message, on the server process's standard input and output; the server asks the open windows over their named pipes (`AxKit-mcp-<user>-<random>`, this Windows account only, each connection answering a challenge with the paired key, HMAC-SHA256); every call is written to `mcp-audit.log` with its arguments hashed.
- **What it reaches**: nothing on the network: pipes on this computer. What the AI tool does with what it read is under its own terms. axPass and axClip are never read; axClaude and axTerminal are never opened.
- **Documentation**: https://modelcontextprotocol.io/specification; docs/axKit/mcp.md.
- **Apps**: every window.

Calls (3):

- **shared by every app**, `src/AxKit.Forms/McpBroker.cs:56`, in `McpBroker.Run`, by `Console.OpenStandardInput`: the standard input of the MCP server process ("AxKit.exe mcp"), on which the paired AI tool, Claude Code, sends its requests; the answers go back on standard output; on this computer, never over the network.
  Code: `using var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));`
- **shared by every app**, `src/AxKit.Forms/McpBroker.cs:234`, in `McpBroker.Exchange`, by `new NamedPipeClientStream`: connects to one axKit window's pipe to ask it what it shows, for the AI tool's request; the broker first checks that the pipe's server is this very program file (docs/axKit/mcp.md MC-6.3).
  Code: `using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, System.Security.Principal.TokenImpersonationLevel.Identification);`
- **shared by every app**, `src/AxKit.Forms/McpChannel.cs:88`, in `McpChannel.Serve`, by `NamedPipeServerStreamAcl.Create`: the window's own named pipe, which only this Windows account may open, where the MCP server asks the window what it shows while the AI tools are on; each connection must answer the challenge with the paired key.
  Code: `using var pipe = NamedPipeServerStreamAcl.Create(_name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, UserPipe.Security());`

#### Test automation

- **What**: tools/AxAutomate, the bundle's own test driver, in an automated run only: a window started with the run's secret (`AXKIT_AUTOMATION`) opens the pipe `axkit-auto-<process id>` for it. Never in normal use.
- **How**: a named pipe this Windows account alone may open; the run's secret and folder go from the environment to the window's memory at its start (S-11).
- **What it reaches**: nothing on the network.
- **Documentation**: docs/axKit/automation.md.
- **Apps**: every window, in a test run.

Calls (1):

- **shared by every app**, `src/AxKit.Forms/AutomationChannel.cs:123`, in `AutomationChannel.Serve`, by `NamedPipeServerStreamAcl.Create`: the window's pipe of an automated test run only (AXKIT_AUTOMATION, set by tools/AxAutomate), which only this account may open, so that the test tool can drive the window; never opened in normal use (docs/axKit/automation.md).
  Code: `using var pipe = NamedPipeServerStreamAcl.Create($"axkit-auto-{Environment.ProcessId}", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, UserPipe.Security());`

## The functions that make these calls

The script finds every call by these functions, by name, in every `.cs` file under `src` (bin and obj left out; comment lines and the P/Invoke declarations themselves skipped). A new way of reaching outside is added to the table in the script.

### HttpClient.SendAsync, network

Defined in .NET, System.Net.Http: the framework's HTTP client.

Used in 8 places:

- `src/AxKit.Core/Reports/GitHubIssues.cs:81`, in `GitHubIssues.CreateAsync`, for GitHub.
- `src/AxKit.Core/Reports/GitHubIssues.cs:93`, in `GitHubIssues.ReadBlocklistAsync`, for GitHub.
- `src/AxKit.Core/Reports/GitHubIssues.cs:112`, in `GitHubIssues.ListAsync`, for GitHub.
- `src/AxKit.Core/Reports/GitHubIssues.cs:152`, in `GitHubIssues.PatchAsync`, for GitHub.
- `src/AxKit.Core/Reports/GitHubIssues.cs:162`, in `GitHubIssues.CommentAsync`, for GitHub.
- `src/AxKit.Core/Reports/GitHubIssues.cs:174`, in `GitHubIssues.WriteBlocklistAsync`, for GitHub.
- `src/AxKit.Core/Reports/GitHubIssues.cs:195`, in `GitHubIssues.WriteBlocklistAsync`, for GitHub.
- `src/AxKit.Core/Updates/UpdateCheck.cs:130`, in `UpdateCheck.FetchLatestAsync`, for GitHub.

### HttpClient.GetAsync, network

Defined in .NET, System.Net.Http: the framework's HTTP client.

Used in 1 place:

- `src/AxKit.Core/Updates/Updater.cs:89`, in `Updater.DownloadAsync`, for GitHub.

### HttpClient.GetStringAsync, network

Defined in .NET, System.Net.Http: the framework's HTTP client.

Used in 1 place:

- `src/AxKit.Core/Updates/Updater.cs:65`, in `Updater.DownloadAsync`, for GitHub.

### new HttpClient, network

Defined in .NET, System.Net.Http: the framework's HTTP client.

Used in 1 place:

- `src/AxKit.Core/AxKitHttp.cs:19`, in `AxKitHttp.Create`, for GitHub.

### Process.Start, program

Defined in .NET, System.Diagnostics: starts a program, through the Windows shell when UseShellExecute is set.

Used in 16 places:

- `src/AxFile/FileForm.Operations.cs:698`, in `FileForm.ShowFolderInExplorer`, for Windows.
- `src/AxFile/FileForm.Operations.cs:782`, in `FileForm.OpenWithWindows`, for Windows.
- `src/AxFile/FileForm.Programs.cs:74`, in `FileForm.RunProgram`, for User programs.
- `src/AxKit.Core/SignInStart.cs:57`, in `SignInStart.OpenWindowsStartupSettings`, for Windows.
- `src/AxKit.Core/Updates/Updater.cs:183`, in `Updater.InstallInPlaceAsync`, for Windows.
- `src/AxKit.Core/Updates/Updater.cs:236`, in `Updater.StartInstalled`, for axKit.
- `src/AxKit.Forms/AxKitForm.cs:815`, in `AxKitForm.OpenUrl`, for Windows.
- `src/AxKit.Forms/AxKitForm.cs:840`, in `AxKitForm.OpenInExplorer`, for Windows.
- `src/AxKit.Forms/AxKitForm.Install.cs:122`, in `AxKitForm.RunInstallerAsync`, for Installers.
- `src/AxKit.Forms/AxKitForm.Mcp.cs:206`, in `AxKitForm.RunClaude`, for Claude Code.
- `src/AxKit.Forms/AxKitForm.Updates.cs:156`, in `AxKitForm.OpenStorePage`, for Windows.
- `src/AxKit.Forms/McpBroker.cs:300`, in `McpBroker.Open`, for axKit.
- `src/AxKit.Forms/TextEditorForm.Viewer.cs:274`, in `TextEditorForm.OpenInWindowsProgram`, for Windows.
- `src/AxKit.Forms/WindowMenu.cs:183`, in `WindowMenu.StartWith`, for axKit.
- `src/AxKit/Program.cs:143`, in `Program.StartedAsOpenWithLink`, for axKit.
- `src/AxStart/AppsFolder.cs:116`, in `AppsFolder.Start`, for User programs.

### ProcessRun.Run, program

Defined in this repository, `src/AxKit.Core/ProcessRun.cs:69` (`public static ProcessResult Run(ProcessStartInfo start, IReadOnlyList<string> input, TimeSpan timeout) =>`).

Used in 1 place:

- `src/AxPass.Core/KeePassCli.cs:79`, in `KeePassCli.Run`, for KeePassXC.

### ProcessRun.RunAsync, program

Defined in this repository, `src/AxKit.Core/ProcessRun.cs:23` (`public static async Task<ProcessResult> RunAsync(ProcessStartInfo start, IReadOnlyList<string> input, TimeSpan timeout, CancellationToken cancellation = default)`).

Used in 4 places:

- `src/AxClaude.Core/Pty/ClaudeLauncher.cs:46`, in `ClaudeLauncher.VersionAsync`, for Claude Code.
- `src/AxGit.Core/GitRunner.cs:111`, in `GitRunner.RunAsync`, for Git.
- `src/AxKit.Core/Ai/AiRequest.cs:74`, in `AiRequest.AskAsync`, for Claude Code.
- `src/AxPreview.Core/PictureText.cs:112`, in `PictureText.RecognizeAsync`, for Windows.

### PtyHost.Start, program

Defined in this repository, `src/AxKit.Core/Pty/PtyHost.cs:46` (`public static PtyHost Start(PtyOptions options, Action<byte[]> dataReceived, Action<int> exited)`).

Used in 1 place:

- `src/AxKit.Forms/ConsoleForm.Process.cs:51`, in `ConsoleForm.StartProcess`, for Claude Code and Shells.

### CreateProcess (Windows), program

Defined in Windows' `kernel32.dll`, declared for axKit in `src/AxKit.Core/Pty/PtyHost.cs:345` (`public static extern bool CreateProcess(string? lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes,`).

Used in 1 place:

- `src/AxKit.Core/Pty/PtyHost.cs:107`, in `PtyHost.Start`, for Claude Code and Shells.

### ShellExecuteEx (Windows), program

Defined in Windows' `shell32.dll`, declared for axKit in `src/AxFile/Shell.cs:455` (`private static extern bool ShellExecuteExW(ref ShellExecuteInfo info);`).

Used in 1 place:

- `src/AxFile/Shell.cs:68`, in `Shell.Properties`, for Windows.

### IContextMenu.InvokeCommand (Windows), program

Defined in the Windows shell's COM interface IContextMenu, as a method of it, declared for axKit in `src/AxFile/Shell.cs:593` (`int InvokeCommand(ref InvokeInfo info);`).

Used in 2 places:

- `src/AxFile/Shell.cs:116`, in `Shell.ShowMenu`, for Windows.
- `src/AxFile/Shell.cs:326`, in `Shell.Invoke`, for Windows.

### SHOpenWithDialog (Windows), program

Defined in Windows' `shell32.dll`, declared for axKit in `src/AxKit.Forms/WindowsOpenWith.cs:28` (`private static extern int SHOpenWithDialog(nint owner, in OpenAsInfo info);`).

Used in 1 place:

- `src/AxKit.Forms/WindowsOpenWith.cs:23`, in `WindowsOpenWith.Show`, for Windows.

### StartService (Windows), program

Defined in Windows' `advapi32.dll`, declared for axKit in `src/AxProcess.Core/ServiceManager.cs:335` (`private static extern bool StartService(IntPtr service, uint arguments, string[]? vectors);`).

Used in 1 place:

- `src/AxProcess.Core/ServiceManager.cs:171`, in `ServiceManager.Start`, for Windows.

### new NamedPipeClientStream, pipe

Defined in .NET, System.IO.Pipes: connects to a named pipe on this computer.

Used in 1 place:

- `src/AxKit.Forms/McpBroker.cs:234`, in `McpBroker.Exchange`, for AI tools.

### NamedPipeServerStreamAcl.Create, pipe

Defined in .NET, System.IO.Pipes: opens a named pipe on this computer with an access list.

Used in 2 places:

- `src/AxKit.Forms/AutomationChannel.cs:123`, in `AutomationChannel.Serve`, for Test automation.
- `src/AxKit.Forms/McpChannel.cs:88`, in `McpChannel.Serve`, for AI tools.

### Console.OpenStandardInput, pipe

Defined in .NET, System: the standard input, which the process that started this one writes to.

Used in 1 place:

- `src/AxKit.Forms/McpBroker.cs:56`, in `McpBroker.Run`, for AI tools.

Watched for and not used today: HttpClient.GetStreamAsync (network); HttpClient.GetByteArrayAsync (network); HttpClient.PostAsync (network); WebRequest.Create (network); new WebClient (network); new Socket (network); new TcpClient (network); new TcpListener (network); new UdpClient (network); new HttpListener (network); Dns.GetHostEntry, Dns.GetHostAddresses (network).
