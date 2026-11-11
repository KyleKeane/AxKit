<#
.SYNOPSIS
Writes docs\axKit\outside-calls.md, the page of everything axKit reaches outside its own process: the network
requests, the programs started and the pipes opened, by what is reached, with every call's place and purpose; or
checks that the page is current (-Check).

.DESCRIPTION
Scans every .cs file under src (bin and obj left out) for the functions that reach outside the process, the table
$functions below: the framework's HTTP client, the process starts of .NET and of Windows, the pipes. Each call carries
a comment in the code, on the line above it or at its end, that starts

    // Outside call, <kind>, <what is reached>: <what the call does for the user and why>

with <kind> one of network, program or pipe, and <what is reached> one or more keys of the table $targets below
(several separated by ", ": a shared piece that starts Claude Code and the shells names both); more comment lines
directly under it continue the text. The page lists, for each target, what it is, where it is (the program file or
the endpoint), how axKit reaches it, what it may reach in turn, and its documentation, then every call under it with
its app, file and line, the function it stands in, the function it calls and its purpose. A second section gives the
functions themselves: where each is defined (.NET, Windows and its P/Invoke declaration, or the file and line in this
repository) and every place it is used, and what is watched for and not used.

A call without that comment, with the wrong kind or with an unknown target is an error: the script says where and
exits with 1. A new way of reaching outside (a new API) is added to $functions; a new program or service to $targets.

-Check writes nothing and exits with 1 when a call has no comment or the page on disk is not what the sources give;
then run the script without -Check and commit the page. build.yml runs the check on every push; release.ps1 runs the
script without -Check before the tests and commits the page with the release (docs/axKit/SPEC.md B-17).
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $root 'src'
$documentPath = Join-Path $root 'docs\axKit\outside-calls.md'

# What axKit reaches, in the order the page lists it. Each has the key the comments name, its kind, and its lines, each
# a label and a plain-language text; every address in them is where a reader checks the facts.
$targets = @(
    @{ Key = 'GitHub'; Kind = 'network'; Lines = @(
        @('What', 'GitHub, where this repository, its releases and the reports repository `KyleKeane/AxKit-reports` live. The one place axKit''s own code reaches over the network: the updater and the report sender (docs/axKit/reports.md).'),
        @('Where', 'The updater: `GET https://api.github.com/repos/KyleKeane/AxKit/releases/latest` (the latest release as JSON) and `GET https://github.com/KyleKeane/AxKit/releases/download/<tag>/<file>` (the release''s zip and its `.sig`, accepted from that one address only). The reports: `GET https://raw.githubusercontent.com/KyleKeane/AxKit-reports/main/blocklist.json` (the blocklist) and `POST https://api.github.com/repos/KyleKeane/AxKit-reports/issues` (a report as an issue, as axKit''s own reporter account). axTask''s GitHub lists: `GET https://api.github.com/repos/<repository>/issues` (the issues, no account), and as the user''s own account `PATCH .../issues/<n>`, `POST .../issues/<n>/comments` and `GET` and `PUT .../contents/blocklist.json`.'),
        @('How', '.NET''s HttpClient over HTTPS with the headers `User-Agent: AxKit/<version>` and `Accept: application/vnd.github+json`; the request carries the version and, as every web request does, the network address, nothing else about the user. A download is installed only when its SHA-256 matches GitHub''s digest of the file and the release key''s ECDSA P-256 signature (AX-3.4). A report carries what the user read and edited in the report notice, redacted, and the install''s random reporter id, never a name or an account (reports.md RP-4, RP-6.3); the reporter account''s token ships in the release build and can reach that repository''s issues alone (RP-6.2).'),
        @('When', 'the update check as a window starts (Options, General, Check for updates at start, a setting that can be off) or on Help, Check for updates; the two downloads when the user chooses Update. A report when the user presses Enter on one (F4), the unsent ones with it and when Your reports opens; never at a window''s start (RP-6.7). The lists and the actions when the user opens, refreshes or acts on a GitHub list in axTask (RP-7).'),
        @('Documentation', 'https://docs.github.com/en/rest/releases/releases#get-the-latest-release; https://docs.github.com/en/rest/issues/issues; https://docs.github.com/en/rest/repos/contents; GitHub''s privacy statement, https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement.'),
        @('Apps', 'every window (the updater and the report); axTask (the GitHub lists).')) }
    @{ Key = 'Claude Code'; Kind = 'program'; Lines = @(
        @('What', 'Claude Code, Anthropic''s command-line program, which the user installs: `claude.exe` of its native installer (`%USERPROFILE%\.local\bin`) or npm''s `claude.cmd`, found on the PATH or in those folders, or where the user pointed axClaude.'),
        @('How', 'for a session, `claude --ax-screen-reader` in a ConPTY pseudo console (PtyHost): what it prints is read from the console and the user''s keys are written to it. For its version and for `claude mcp add` and `claude mcp remove`, a process without a window whose output is read. For AI help (`docs/axKit/ai-help.md`), `claude -p --output-format json --allowedTools Read,Glob,Grep` once per question, the prompt on its standard input, the folders the user allowed as `--add-dir`, a follow-up with `--resume`; read-only tools, so it changes no file and runs no command.'),
        @('What it reaches', 'Anthropic''s service with the user''s account, and whatever else Claude Code does from a terminal, its own update check included, under Anthropic''s terms; axKit neither sees nor changes that traffic.'),
        @('Documentation', 'https://code.claude.com/docs/en/cli-reference and https://code.claude.com/docs/en/setup; Anthropic''s privacy policy, https://www.anthropic.com/legal/privacy.'),
        @('Apps', 'axClaude, access layer for Claude Code; Add axKit to Claude Code (axKit menu, AI tools) in every window; AI help in every window (Shift+F1, the axKit menu, Claude); axHelp, access layer for Claude Code, through axClaude.')) }
    @{ Key = 'Git'; Kind = 'program'; Lines = @(
        @('What', 'Git for Windows, the user''s `git.exe` on the PATH.'),
        @('How', 'one process per command, `git -C <repository> -c core.quotepath=false -c color.ui=never <command>`, with `GIT_TERMINAL_PROMPT=0`, `LC_ALL=C.UTF-8` and `GIT_PAGER=cat`; both outputs are read under a time limit.'),
        @('What it reaches', 'the repository''s remotes on fetch, pull and push alone, after the user''s consent for that server (AX-11.5); git''s credential helper may ask for a sign-in in its own window. Every other command works on the repository on disk.'),
        @('Documentation', 'https://git-scm.com/docs.'),
        @('Apps', 'axGIT, access layer for Git.')) }
    @{ Key = 'Shells'; Kind = 'program'; Lines = @(
        @('What', 'the user''s shell for axTerminal: Windows PowerShell (`powershell.exe`) and Command Prompt (`cmd.exe`), Windows'' own by their full paths; Git Bash (`Git\bin\bash.exe` of Git for Windows, never a bash.exe on the PATH); PowerShell 7 (`pwsh.exe` on the PATH or under Program Files).'),
        @('How', 'in a ConPTY pseudo console (PtyHost), as a terminal runs it, with axTerminal''s prompt marks handed in through the environment.'),
        @('What it reaches', 'whatever the commands the user types reach, with the user''s rights.'),
        @('Documentation', 'https://learn.microsoft.com/windows/console/pseudoconsoles; docs/axTerminal/SPEC.md.'),
        @('Apps', 'axTerminal, access layer for your shell.')) }
    @{ Key = 'KeePassXC'; Kind = 'program'; Lines = @(
        @('What', 'KeePassXC''s command-line tool, `keepassxc-cli.exe`, from the user''s installation of KeePassXC, found in its folder or on the PATH.'),
        @('How', 'one process per command with the database''s path, the master password on its standard input, both outputs read under a time limit.'),
        @('What it reaches', 'nothing on the network: the tool has no network feature; it reads and writes the database file.'),
        @('Documentation', 'https://keepassxc.org/docs/KeePassXC_UserGuide#_command_line_tool; docs/axPass/kdbx-notes.md.'),
        @('Apps', 'axPass, access layer for KeePassXC.')) }
    @{ Key = 'MCP servers'; Kind = 'program'; Lines = @(
        @('What', 'the MCP server the user starts in axMCP: a program defined in Claude Code''s configuration (`%USERPROFILE%\.claude.json`, a folder''s `.mcp.json`, read and never written) or in axMCP''s own list, by the command line that definition gives.'),
        @('How', 'a child process without a window, the Model Context Protocol''s JSON-RPC one line per message on its standard input and output, its standard error to axMCP''s log, the environment variables of its definition added; stopped by closing its input, then ending its process tree after two seconds.'),
        @('What it reaches', 'whatever the server''s maker built it to reach, on this computer and on any network, with the user''s rights, after the user''s consent for that server (AX-11.4). Remote servers (http, sse) are listed and never started by this version (docs/axMCP/SPEC.md MP-D2).'),
        @('Documentation', 'https://modelcontextprotocol.io/specification; https://code.claude.com/docs/en/mcp; docs/axMCP/SPEC.md.'),
        @('Apps', 'axMCP, access layer for MCP servers.')) }
    @{ Key = 'Installers'; Kind = 'program'; Lines = @(
        @('What', 'the installers of Install it now (AX-2.9), run only when the user chooses to install a program an app needs and Windows lacks: Windows'' own winget (`%LOCALAPPDATA%\Microsoft\WindowsApps\winget.exe`, never one on the PATH) for Git (`Git.Git`), KeePassXC (`KeePassXCTeam.KeePassXC`) and PowerShell 7 (`Microsoft.PowerShell`); Windows PowerShell running Anthropic''s installer script for Claude Code.'),
        @('How', '`winget install --id <id> --exact --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity`, without a window, its output read; `powershell.exe` running `irm https://claude.ai/install.ps1 | iex`.'),
        @('What it reaches', 'winget''s own source and the maker''s download server for the installer; `claude.ai` for Claude Code''s installer. The installer then runs under its maker''s terms; one that needs administrator rights asks Windows, and Windows asks the user.'),
        @('Documentation', 'https://learn.microsoft.com/windows/package-manager/winget/install; https://code.claude.com/docs/en/setup.'),
        @('Apps', 'the access layer whose program is missing; axTerminal for PowerShell 7 and Git Bash.')) }
    @{ Key = 'Windows'; Kind = 'program'; Lines = @(
        @('What', 'Windows itself: the shell (File Explorer''s parts) and Windows'' own programs by their full paths.'),
        @('How', 'ShellExecute with a file (the shell opens it with the program Windows has for it, or shows its Open with dialog), a web address (the default browser) or the verb `properties`; the shell''s context menu interface IContextMenu for the Windows menu and the shell''s own commands; the Open with dialog `SHOpenWithDialog`; `explorer.exe` for File Explorer; `ms-windows-store:` for axKit''s page in the Microsoft Store app and `ms-settings:` for Windows'' Startup apps page in the Settings app (the Store''s copy, AX-13); `powershell.exe` for the installer''s `install.ps1` and for axPreview''s own script that asks Windows'' text reader (`Windows.Media.Ocr`) for the text in a picture (docs/axPreview/SPEC.md AP-D11); the Service Control Manager to start a service.'),
        @('What it reaches', 'nothing on the network by itself; the browser, the program Windows picks for a file, a shell extension or a service then act on their own.'),
        @('Documentation', 'https://learn.microsoft.com/windows/win32/shell/launch; https://learn.microsoft.com/windows/win32/api/shobjidl_core/nn-shobjidl_core-icontextmenu; https://learn.microsoft.com/windows/win32/services/starting-a-service.'),
        @('Apps', 'every window (links, Show in File Explorer, updates); axFile; axPreview; axProcess.')) }
    @{ Key = 'User programs'; Kind = 'program'; Lines = @(
        @('What', 'programs the user chose: one added to axFile''s Open with list, by the path the user gave (a script runs through Windows PowerShell); one picked in axStart, by its name in the shell''s Applications folder or its target file.'),
        @('How', 'started through the shell with the selected entries'' paths as arguments (axFile), or as the Start menu starts it, as administrator when the user asks (axStart).'),
        @('What it reaches', 'whatever the program does, with the user''s rights.'),
        @('Documentation', 'docs/axFile/SPEC.md (Open with); docs/axStart/SPEC.md.'),
        @('Apps', 'axFile; axStart.')) }
    @{ Key = 'axKit'; Kind = 'program'; Lines = @(
        @('What', 'axKit itself, this very program file: a new window of an app, the restart under its own name when started through an Open with hard link, the restart after an update, a window the AI tool asked for, the windows and the AI tools'' server of a workflow''s run (axAutomate, on the run''s own desktop).'),
        @('How', 'a new process of this program file (or the installed `AxKit.exe` after an update) with the app''s verb and path; a test run''s variables go only to a window of the run (S-11).'),
        @('What it reaches', 'nothing new: the same program.'),
        @('Documentation', 'docs/axKit/SPEC.md AX-1, AX-2.3, AX-3, AX-9.'),
        @('Apps', 'every window.')) }
    @{ Key = 'AI tools'; Kind = 'pipe'; Lines = @(
        @('What', 'the paired AI tool, Claude Code as an MCP client, which starts `AxKit.exe mcp` as its server once the user has turned the AI tools on and paired it (axKit menu, AI tools).'),
        @('How', 'MCP''s JSON-RPC, one line per message, on the server process''s standard input and output; the server asks the open windows over their named pipes (`AxKit-mcp-<user>-<random>`, this Windows account only, each connection answering a challenge with the paired key, HMAC-SHA256); every call is written to `mcp-audit.log` with its arguments hashed.'),
        @('What it reaches', 'nothing on the network: pipes on this computer. What the AI tool does with what it read is under its own terms. axPass and axClip are never read; axClaude and axTerminal are never opened.'),
        @('Documentation', 'https://modelcontextprotocol.io/specification; docs/axKit/mcp.md.'),
        @('Apps', 'every window.')) }
    @{ Key = 'Workflow commands'; Kind = 'program'; Lines = @(
        @('What', 'the command line of a workflow''s `run` step (a test''s `git init`, `findstr`, `copy`), through Windows'' own `cmd.exe`, when a workflow runs: in axAutomate''s window (F5) or with tools/AxAutomate.'),
        @('How', '`cmd.exe /d /s /c "<command line>"` without a window, in the run''s own folder, both outputs read, waited for up to 60 seconds and ended with its process tree after that.'),
        @('What it reaches', 'whatever the command reaches, with the user''s rights; the workflows of `tests/scripts` make and read files in the run''s folder.'),
        @('Documentation', 'docs/axAutomate/SPEC.md AU-3.3, AU-4.4; docs/axKit/automation.md.'),
        @('Apps', 'axAutomate; tools/AxAutomate.')) }
    @{ Key = 'Test automation'; Kind = 'pipe'; Lines = @(
        @('What', 'the automation channel of a window of a workflow''s run, in a run only: axAutomate''s window (F5) or tools/AxAutomate, the bundle''s own test driver, starts the run''s windows on a desktop of the run''s own with the run''s secret (`AXKIT_AUTOMATION`), and each opens the pipe `axkit-auto-<process id>` for it. Never in normal use; the user''s own windows never open it.'),
        @('How', 'a named pipe this Windows account alone may open; the run''s secret and folder are handed to the run''s windows alone in their own environment, and go from it to the window''s memory at its start (S-11).'),
        @('What it reaches', 'nothing on the network.'),
        @('Documentation', 'docs/axKit/automation.md.'),
        @('Apps', 'every window, in a test run.')) }
)

# Name: as the page names the function. Kind: network, program or pipe. Pattern: a case-sensitive regular expression
# that finds a use on one line of code (comment lines and extern declarations are skipped). Defined: a sentence for a
# function of .NET, or a hashtable with Find, a regular expression for its declaration line in src (a function of this
# repository, or a Windows function declared for P/Invoke, whose DllImport attribute above names the DLL), and an
# optional Note that opens the sentence.
$http = '.NET, System.Net.Http: the framework''s HTTP client'
$sockets = '.NET, System.Net.Sockets'
$functions = @(
    @{ Name = 'HttpClient.SendAsync';                   Kind = 'network'; Pattern = '\.SendAsync\(';                 Defined = $http }
    @{ Name = 'HttpClient.GetAsync';                    Kind = 'network'; Pattern = '\.GetAsync\(';                  Defined = $http }
    @{ Name = 'HttpClient.GetStringAsync';              Kind = 'network'; Pattern = '\.GetStringAsync\(';            Defined = $http }
    @{ Name = 'HttpClient.GetStreamAsync';              Kind = 'network'; Pattern = '\.GetStreamAsync\(';            Defined = $http }
    @{ Name = 'HttpClient.GetByteArrayAsync';           Kind = 'network'; Pattern = '\.GetByteArrayAsync\(';         Defined = $http }
    @{ Name = 'HttpClient.PostAsync';                   Kind = 'network'; Pattern = '\.PostAsync\(';                 Defined = $http }
    @{ Name = 'new HttpClient';                         Kind = 'network'; Pattern = 'new HttpClient\b';              Defined = $http }
    @{ Name = 'WebRequest.Create';                      Kind = 'network'; Pattern = 'WebRequest\.Create\w*\(';       Defined = '.NET, System.Net: the older request classes' }
    @{ Name = 'new WebClient';                          Kind = 'network'; Pattern = 'new WebClient\b';               Defined = '.NET, System.Net: the older request classes' }
    @{ Name = 'new Socket';                             Kind = 'network'; Pattern = 'new Socket\(';                  Defined = $sockets }
    @{ Name = 'new TcpClient';                          Kind = 'network'; Pattern = 'new TcpClient\b';               Defined = $sockets }
    @{ Name = 'new TcpListener';                        Kind = 'network'; Pattern = 'new TcpListener\b';             Defined = $sockets }
    @{ Name = 'new UdpClient';                          Kind = 'network'; Pattern = 'new UdpClient\b';               Defined = $sockets }
    @{ Name = 'new HttpListener';                       Kind = 'network'; Pattern = 'new HttpListener\b';            Defined = '.NET, System.Net' }
    @{ Name = 'Dns.GetHostEntry, Dns.GetHostAddresses'; Kind = 'network'; Pattern = '\bDns\.GetHost\w*\(';           Defined = '.NET, System.Net' }
    @{ Name = 'Process.Start';                          Kind = 'program'; Pattern = '\bProcess\.Start\(';            Defined = '.NET, System.Diagnostics: starts a program, through the Windows shell when UseShellExecute is set' }
    @{ Name = 'ProcessRun.Run';                         Kind = 'program'; Pattern = '\bProcessRun\.Run\(';           Defined = @{ Find = 'public static .*\bRun\(ProcessStartInfo' } }
    @{ Name = 'ProcessRun.RunAsync';                    Kind = 'program'; Pattern = '\bProcessRun\.RunAsync\(';      Defined = @{ Find = 'public static .*\bRunAsync\(ProcessStartInfo' } }
    @{ Name = 'PtyHost.Start';                          Kind = 'program'; Pattern = '\bPtyHost\.Start\(';            Defined = @{ Find = 'public static PtyHost Start\(' } }
    @{ Name = 'CreateProcess (Windows)';                Kind = 'program'; Pattern = '\bCreateProcess\w*\(';          Defined = @{ Find = 'extern .*\bCreateProcess\w*\(' } }
    @{ Name = 'ShellExecuteEx (Windows)';               Kind = 'program'; Pattern = '\bShellExecuteEx\w*\(';         Defined = @{ Find = 'extern .*\bShellExecuteEx\w*\(' } }
    @{ Name = 'IContextMenu.InvokeCommand (Windows)';   Kind = 'program'; Pattern = '\.InvokeCommand\(';             Defined = @{ Find = '^\s*int InvokeCommand\('; Note = 'the Windows shell''s COM interface IContextMenu, as a method of it, declared for axKit in' } }
    @{ Name = 'SHOpenWithDialog (Windows)';             Kind = 'program'; Pattern = '\bSHOpenWithDialog\(';          Defined = @{ Find = 'extern .*\bSHOpenWithDialog\(' } }
    @{ Name = 'StartService (Windows)';                 Kind = 'program'; Pattern = '\bStartService\w*\(';           Defined = @{ Find = 'extern .*\bStartService\w*\(' } }
    @{ Name = 'new NamedPipeClientStream';              Kind = 'pipe';    Pattern = 'new NamedPipeClientStream\(';   Defined = '.NET, System.IO.Pipes: connects to a named pipe on this computer' }
    @{ Name = 'NamedPipeServerStreamAcl.Create';        Kind = 'pipe';    Pattern = 'NamedPipeServerStreamAcl\.Create\('; Defined = '.NET, System.IO.Pipes: opens a named pipe on this computer with an access list' }
    @{ Name = 'Console.OpenStandardInput';              Kind = 'pipe';    Pattern = 'Console\.OpenStandardInput\(';  Defined = '.NET, System: the standard input, which the process that started this one writes to' }
)

$kindNames = [ordered]@{ network = 'Network'; program = 'Programs'; pipe = 'Pipes' }

# The app a source folder belongs to, for the calls' lines.
$apps = @{
    AxClaude = 'axClaude'; AxDown = 'axDown'; AxJSON = 'axJSON'; AxGit = 'axGIT'; AxTerminal = 'axTerminal'; AxFile = 'axFile'
    AxMedia = 'axMedia'; AxPreview = 'axPreview'; AxStart = 'axStart'; AxProcess = 'axProcess'; AxTask = 'axTask'
    AxPass = 'axPass'; AxCalc = 'axCalc'; AxClip = 'axClip'; AxAutomate = 'axAutomate'; AxKit = 'shared by every app'
}

$marker = [regex]'//\s*Outside call, (\w+), ([^:]+):\s*(.*)$'
$plainComment = [regex]'^\s*//(?!/)'
$commentStart = [regex]'^\s*//\s?'
$declaration = [regex]'^\s*(?:(?:public|private|protected|internal|static|async|override|virtual|sealed|partial|unsafe|extern)\s+)+(?!class\b|record\b|struct\b|interface\b|enum\b|readonly\b|const\b|event\b|delegate\b)[\w<>\[\],.?()\s]*?\b(\w+)\s*(?:\(|=>|\{|$)'
$typeDeclaration = [regex]'^\s*(?:\w+\s+)*(?:class|record|struct|interface)\s+(\w+)'
$import = [regex]'(?:DllImport|LibraryImport)\("([^"]+)"'

$files = @(Get-ChildItem -Path $sourceRoot -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Sort-Object FullName)

function Relative([string]$path) {
    return $path.Substring($root.Length + 1).Replace('\', '/')
}

function AppOf([string]$relative) {
    $project = ($relative -split '/')[1] -replace '\.(Core|Forms)$', ''
    if ($apps.ContainsKey($project)) { return $apps[$project] }
    return $project
}

# The nearest line above that declares something, the member the call stands in, and its type: the least indented
# type declaration above it, so that a nested record declared earlier in the class does not pass for the class.
function EnclosingMember([string[]]$lines, [int]$index) {
    $member = $null
    $type = $null
    $typeIndent = [int]::MaxValue
    for ($j = $index - 1; $j -ge 0; $j--) {
        if ($null -eq $member) {
            $m = $declaration.Match($lines[$j])
            if ($m.Success) { $member = $m.Groups[1].Value }
        }
        $t = $typeDeclaration.Match($lines[$j])
        if ($t.Success) {
            $indent = $lines[$j].Length - $lines[$j].TrimStart().Length
            if ($indent -lt $typeIndent) { $type = $t.Groups[1].Value; $typeIndent = $indent }
        }
    }
    if ($null -eq $member) { return $type }
    if ($null -eq $type) { return $member }
    return "$type.$member"
}

# The explanation of the call on line $index: the marker at the line's end, or the last marker in the comment block
# right above it with the block's lines after it. Null when there is none.
function Explanation([string[]]$lines, [int]$index) {
    $m = $marker.Match($lines[$index])
    if ($m.Success) {
        return @{ Kind = $m.Groups[1].Value; Targets = @($m.Groups[2].Value -split ',\s*'); Text = $m.Groups[3].Value.Trim() }
    }
    $block = New-Object System.Collections.Generic.List[string]
    for ($j = $index - 1; $j -ge 0 -and $plainComment.IsMatch($lines[$j]); $j--) {
        $block.Insert(0, $lines[$j])
    }
    for ($k = $block.Count - 1; $k -ge 0; $k--) {
        $m = $marker.Match($block[$k])
        if ($m.Success) {
            $parts = New-Object System.Collections.Generic.List[string]
            $parts.Add($m.Groups[3].Value.Trim())
            for ($l = $k + 1; $l -lt $block.Count; $l++) {
                $parts.Add($commentStart.Replace($block[$l], '', 1).Trim())
            }
            return @{ Kind = $m.Groups[1].Value; Targets = @($m.Groups[2].Value -split ',\s*'); Text = (($parts | Where-Object { $_ }) -join ' ') }
        }
    }
    return $null
}

$sources = @{}
foreach ($file in $files) {
    $sources[$file.FullName] = [IO.File]::ReadAllLines($file.FullName)
}

# Where a function of this repository or of Windows is declared: the first line in src that matches Find.
function Declaration([hashtable]$defined) {
    $find = [regex]$defined.Find
    foreach ($file in $files) {
        $lines = $sources[$file.FullName]
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if (-not $find.IsMatch($lines[$i])) { continue }
            $place = "``$(Relative $file.FullName):$($i + 1)``"
            $text = $lines[$i].Trim()
            if ($defined.ContainsKey('Note')) {
                return "$($defined.Note) $place (``$text``)."
            }
            if ($text -match '\bextern\b') {
                $dll = $null
                for ($j = [Math]::Max(0, $i - 4); $j -lt $i; $j++) {
                    $d = $import.Match($lines[$j])
                    if ($d.Success) { $dll = $d.Groups[1].Value }
                }
                $library = if ($dll) { "Windows' ``$dll``" } else { 'Windows' }
                return "$library, declared for axKit in $place (``$text``)."
            }
            return "this repository, $place (``$text``)."
        }
    }
    return 'not found in src: the Find pattern of tools/outside-calls.ps1 is out of date.'
}

$targetKeys = @($targets | ForEach-Object { $_.Key })
$hits = New-Object System.Collections.Generic.List[object]
$errors = New-Object System.Collections.Generic.List[string]
foreach ($file in $files) {
    $lines = $sources[$file.FullName]
    $relative = Relative $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($plainComment.IsMatch($line) -or $line -match '^\s*///' -or $line -match '\bextern\b') { continue }
        foreach ($function in $functions) {
            if (-not [regex]::IsMatch($line, $function.Pattern)) { continue }
            $explanation = Explanation $lines $i
            $code = $line.Trim()
            $where = "$relative`:$($i + 1)"
            if ($null -eq $explanation) {
                $errors.Add("$where`: $($function.Name) has no 'Outside call, $($function.Kind), <what is reached>:' comment above it or at its end: $code")
                continue
            }
            if ($explanation.Kind -cne $function.Kind) {
                $errors.Add("$where`: $($function.Name) is a $($function.Kind) call; its comment says '$($explanation.Kind)'.")
                continue
            }
            $unknown = @($explanation.Targets | Where-Object { $targetKeys -cnotcontains $_ })
            if ($unknown.Count -gt 0) {
                $errors.Add("$where`: the comment names '$($unknown -join ''', ''')', not a key of the targets table in tools/outside-calls.ps1 ($($targetKeys -join ', ')).")
                continue
            }
            $hits.Add([pscustomobject]@{
                File = $relative
                Line = $i + 1
                App = (AppOf $relative)
                Member = (EnclosingMember $lines $i)
                Function = $function
                Targets = $explanation.Targets
                Code = $code
                Text = $explanation.Text
            })
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Outside calls without a usable explanation (docs/axKit/outside-calls.md, tools/outside-calls.ps1):"
    $errors | ForEach-Object { Write-Host "  $_" }
    exit 1
}

$out = New-Object System.Collections.Generic.List[string]
$out.Add('# Outside calls')
$out.Add('')
$out.Add('Everything axKit reaches outside its own process: the network requests its own code makes, the programs it starts and the pipes it opens, by what is reached. For each, what it is, where it is (the program file or the endpoint), how axKit reaches it, what it may reach in turn, its documentation, and every call under it with its app, file and line, the function it stands in, the function it calls and what the call does for the user. The second section is the functions themselves, where each is defined and where it is used, and what is watched for.')
$out.Add('')
$out.Add('`tools/outside-calls.ps1` writes this page from the sources, so it is a list, not a promise: each call in the code carries a comment that starts `Outside call, <kind>, <what is reached>:` on the line above it or at its end, and this page is their list, found by the functions of the second section. A call without that comment fails the script, which `build.yml` runs on every push (with the check that this page is current) and `release.ps1` before every release; `release.ps1` writes the page again and commits it with the release, so it is exact at every tag (bundle SPEC B-17). Do not edit this page by hand: change the comment in the code, or the tables in the script, and run it.')
$out.Add('')
$out.Add('The three kinds: **network**, a request over the network from axKit''s own code; **program**, another program started on this computer, Windows'' own, one the user installed, or axKit itself; **pipe**, a channel on this computer that another process connects to or that axKit connects to. The access layers (axClaude, axGIT, axTerminal, axPass) meet the network only through the programs listed here, which use it as they would without axKit (bundle SPEC AX-11.11). `PRIVACY.md` says the same for the user in plain words; `SECURITY.md` says what matters for security. The set of apps grows as the author finds another thing worth doing by ear, and every new call enters this page the same way.')
$out.Add('')
$counts = @{}
foreach ($kind in $kindNames.Keys) { $counts[$kind] = @($hits | Where-Object { $_.Function.Kind -eq $kind }).Count }
$fileCount = @($hits | ForEach-Object { $_.File } | Sort-Object -Unique).Count
$reached = @($targets | Where-Object { $key = $_.Key; @($hits | Where-Object { $_.Targets -ccontains $key }).Count -gt 0 }).Count
$out.Add("Today: $($counts['network']) network requests, $($counts['program']) program starts and $($counts['pipe']) pipes, in $fileCount files, under the $reached headings below.")
$out.Add('')
$out.Add('## What axKit reaches')
$out.Add('')
foreach ($kind in $kindNames.Keys) {
    $out.Add("### $($kindNames[$kind])")
    $out.Add('')
    foreach ($target in @($targets | Where-Object { $_.Kind -eq $kind })) {
        $key = $target.Key
        $calls = @($hits | Where-Object { $_.Targets -ccontains $key } | Sort-Object { $_.File }, { $_.Line })
        if ($calls.Count -eq 0) { continue }
        $out.Add("#### $key")
        $out.Add('')
        foreach ($pair in $target.Lines) {
            $out.Add("- **$($pair[0])**: $($pair[1])")
        }
        $out.Add('')
        $out.Add("Calls ($($calls.Count)):")
        $out.Add('')
        foreach ($call in $calls) {
            $out.Add("- **$($call.App)**, ``$($call.File):$($call.Line)``, in ``$($call.Member)``, by ``$($call.Function.Name)``: $($call.Text)")
            $out.Add("  Code: ``$($call.Code)``")
        }
        $out.Add('')
    }
}
$out.Add('## The functions that make these calls')
$out.Add('')
$out.Add('The script finds every call by these functions, by name, in every `.cs` file under `src` (bin and obj left out; comment lines and the P/Invoke declarations themselves skipped). A new way of reaching outside is added to the table in the script.')
$out.Add('')
$unused = New-Object System.Collections.Generic.List[string]
foreach ($function in $functions) {
    $uses = @($hits | Where-Object { $_.Function.Name -eq $function.Name })
    if ($uses.Count -eq 0) { $unused.Add("$($function.Name) ($($function.Kind))"); continue }
    $out.Add("### $($function.Name), $($function.Kind)")
    $out.Add('')
    $defined = if ($function.Defined -is [hashtable]) { Declaration $function.Defined } else { "$($function.Defined)." }
    $out.Add("Defined in $defined")
    $out.Add('')
    $out.Add("Used in $($uses.Count) place$(if ($uses.Count -ne 1) { 's' }):")
    $out.Add('')
    foreach ($use in $uses) {
        $out.Add("- ``$($use.File):$($use.Line)``, in ``$($use.Member)``, for $($use.Targets -join ' and ').")
    }
    $out.Add('')
}
$out.Add("Watched for and not used today: $($unused -join '; ').")

$text = ($out -join "`r`n") + "`r`n"
$utf8 = New-Object System.Text.UTF8Encoding($false)

if ($Check) {
    $existing = if (Test-Path -LiteralPath $documentPath) { [IO.File]::ReadAllText($documentPath) } else { '' }
    $pageLines = ($existing -replace "`r`n", "`n") -split "`n"
    $sourceLines = ($text -replace "`r`n", "`n") -split "`n"
    if (($existing -replace "`r`n", "`n") -cne ($text -replace "`r`n", "`n")) {
        Write-Host "docs/axKit/outside-calls.md is not what the sources give. Run tools/outside-calls.ps1 and commit the page."
        # The first line that differs, so that a run elsewhere (CI's PowerShell 7 against a Windows PowerShell page) says what it saw.
        $count = [Math]::Max($pageLines.Count, $sourceLines.Count)
        for ($i = 0; $i -lt $count; $i++) {
            $page = if ($i -lt $pageLines.Count) { $pageLines[$i] } else { '(no line)' }
            $source = if ($i -lt $sourceLines.Count) { $sourceLines[$i] } else { '(no line)' }
            if ($page -cne $source) {
                Write-Host "First difference at line $($i + 1):"
                Write-Host "  page:    $page"
                Write-Host "  sources: $source"
                break
            }
        }
        exit 1
    }
    Write-Host "docs/axKit/outside-calls.md is current: $($hits.Count) outside calls, each with its explanation."
    exit 0
}

[IO.File]::WriteAllText($documentPath, $text, $utf8)
Write-Host "Wrote docs/axKit/outside-calls.md: $($hits.Count) outside calls in $fileCount files."
exit 0
