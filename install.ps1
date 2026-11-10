<#
.SYNOPSIS
  Installs axKit for the current user, or removes it with -Uninstall. No administrator rights are needed.

.DESCRIPTION
  Run it from the folder that holds AxKit.exe: the extracted zip, or publish\win-x64 after publish.ps1.
  axKit is one program with several apps inside (docs/axKit/SPEC.md AX-2). It copies axKit to
  %LOCALAPPDATA%\Programs\AxKit and creates the ways to start each app, all from the list of apps that AxKit.exe apps
  prints (Dispatch.Apps; $apps below):
    - a Start menu entry per app, "axKit axClaude", "axKit axDown" and so on (type axKit in the Start menu to list them),
    - one submenu in File Explorer's right-click menu, "Open with axKit", on folders (and on a folder's background) and
      on files, listing the apps that open what was right-clicked: axClaude, axGIT, axTerminal and axFile on folders,
      axDown, axFile and axPreview on every file, axJSON on .json files, axMedia on sound and video files; and axDown
      under Windows' own "Open with" for .md, .markdown and .txt, axJSON for .json, axMedia for the sound and video
      kinds, axPreview for .txt, .md, .markdown, .log and .json, so that the user can make an app the default program
      for a kind in Settings, Default apps,
    - axStart, the program launcher, and axClip, the clipboard history, running in the background from sign-in (a value
      under the user's Run key that each writes itself): they are stopped before an install or a removal, and started
      again, hidden, after an install,
    - the console commands axClaude, axDown, axJSON, axGIT, axTerminal, axFile, axMedia, axPreview, axStart, axProcess, axTask, axPass, axCalc, axClip, axMCP, axHelp and axKit, each in the app's own case since 5.6.0, so that a screen reader says the name as it is (a folder
      opens axClaude, a file axDown, a .json file axJSON, a sound or video file axMedia): one-line shims in
      %USERPROFILE%\.local\bin, put on the user's PATH when it is not there yet (Claude Code's installer uses the same
      folder; a removal leaves the PATH, since the folder may hold other programs' commands).
  It also lists axKit in Settings, Apps (Add or remove programs), whose Uninstall runs install.cmd -Uninstall.
  -Uninstall removes all of that. The settings in %APPDATA%\AxKit and the logs in %LOCALAPPDATA%\AxKit stay.
  An installation of Axit, the bundle's name up to 4.0.0, or of axClaude 1.x, from before the bundle, is removed
  completely first (AX-2.6). What Axit kept in %APPDATA%\Axit and %LOCALAPPDATA%\Axit moves into the AxKit folders the
  first time an app starts (AX-5.4); axClaude 1.x's settings file stays in %APPDATA%\AxClaude, and axClaude copies it
  on its first start.

.EXAMPLE
  .\install.ps1                 # install or update
  .\install.ps1 -Uninstall      # remove
  .\install.ps1 -NoContextMenu  # install without the File Explorer entries
  .\install.ps1 -NoStartMenu    # install without the Start menu entries

  install.cmd, next to this script, runs it with the execution policy bypassed: double-click it, or install.cmd -Uninstall.
  Downloaded on its own from the releases page, install.cmd fetches the latest release and runs this script from it.

  The apps' own Update (the axKit menu) runs the downloaded version's copy of this script as
    install.ps1 -InPlace -LogFile <file>
  while the app keeps running (AX-2.8): the new files are written over the old ones, what earlier versions left goes
  (their File Explorer entries, Axit's and axClaude 1.x's folders and entries, files this version does not ship), and the
  running AxKit.exe is renamed AxKit.old.exe first, since a running program file can be renamed but not overwritten;
  the app then starts the new version itself, and the next start removes the old file. Copies before 2.3.0 call
    install.ps1 -WaitForProcess <pid> [-Start <folder> [-ContinueConversation]] [-StartFile <file>] -LogFile <file>
  after they have closed; those parameters stay (AX-2.7), which is also how axClaude 1.7.0 becomes axKit.
#>
[CmdletBinding()]
param(
    [switch]$Uninstall,
    [switch]$NoContextMenu,
    [switch]$NoStartMenu,
    [switch]$InPlace,
    [int]$WaitForProcess,
    [string]$Start,
    [switch]$ContinueConversation,
    [string]$StartFile,
    [string]$LogFile
)

$ErrorActionPreference = 'Stop'

function Say([string]$text) {
    Write-Host $text
    if ($LogFile) {
        try { Add-Content -Path $LogFile -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $text" -Encoding utf8 } catch { }
    }
}

# Any error that nothing below handles is said and logged: the updater runs this script without a window, so the
# log is the only place it would show.
trap {
    # Where it failed, so that the log and the app's notice say which step it was (until 3.7.2, the message alone).
    $where = if ($_.InvocationInfo -and $_.InvocationInfo.ScriptLineNumber) { " (install.ps1 line $($_.InvocationInfo.ScriptLineNumber): $($_.InvocationInfo.Line.Trim()))" } else { '' }
    Say "axKit was not installed: $($_.Exception.Message)$where"
    exit 1
}

if ($WaitForProcess) {
    Say "Waiting for the app (process $WaitForProcess) to close..."
    try { Wait-Process -Id $WaitForProcess -Timeout 120 -ErrorAction Stop } catch { }
    if (Get-Process -Id $WaitForProcess -ErrorAction SilentlyContinue) {
        Say "The app is still running after two minutes. The update was not installed; start it from the axKit menu again."
        exit 1
    }
}

$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $env:LOCALAPPDATA 'Programs\AxKit'
$exe = Join-Path $target 'AxKit.exe'
$bin = Join-Path $env:USERPROFILE '.local\bin'
$programs = [Environment]::GetFolderPath('Programs')
$classes = 'HKCU:\Software\Classes'
# The entry in Settings, Apps (Add or remove programs) for this user; its Uninstall runs install.cmd -Uninstall.
$appsKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AxKit'

# The apps (AX-2), from the program itself: AxKit.exe apps prints Dispatch.Apps (src/AxKit.Core/Dispatch.cs) as JSON,
# so a new app of the bundle needs nothing here (docs/axKit/app-kit.md AK-4.1). The installation keeps that listing as
# apps.json, which an uninstall reads when the program next to it cannot be run. Per app: Name, which gives the Start
# menu entry "axKit <Name>", the console command <name> and the ProgID AxKit.<Name>. Verb: what AxKit.exe takes. Icon:
# the file the Start menu entry and the File Explorer entries show, <Project>.ico. Opens: what the app opens from
# File Explorer's "Open with axKit" submenu (AX-2.3): 'folders' (a folder, and a folder's background), 'files', or
# both. Types: the file types a files entry is shown on; every file when absent. OpenWith: the file types the app is
# registered as a program for, under Windows' own "Open with" and in Settings, Default apps (B-9), with the TypeName
# that Windows shows for them.
$listingFile = 'apps.json'

# The first line of every installation's log, before any step that can fail: what is installed on what, for a report
# when something goes wrong.
if (-not $Uninstall) {
    $newExe = Join-Path $source 'AxKit.exe'
    $newVersion = if (Test-Path -LiteralPath $newExe) { (Get-Item -LiteralPath $newExe).VersionInfo.FileVersion } else { 'with no AxKit.exe beside install.ps1' }
    Say "Installing axKit $newVersion$(if ($InPlace) { ' in place, the app running' }) on Windows $([Environment]::OSVersion.Version), PowerShell $($PSVersionTable.PSVersion)"
    # What the installation changes and what axKit sends, said before anything is changed (docs/axKit/code-signing.md: the
    # signing service's rule that system changes come with a warning and a privacy statement is shown at installation).
    Say "It installs for your Windows account only, without administrator rights: the program in $target, Start menu entries, console commands in $bin (added to your PATH), File Explorer's Open with axKit and Open with entries, axStart and axClip at sign-in, and an entry in Settings, Apps that uninstalls all of it."
    Say "Privacy: axKit collects nothing and has no telemetry. On its own it only asks GitHub for the latest release when a window starts; Options, General, Check for updates at start turns that off. PRIVACY.md beside the program says everything axKit contacts and when."
    Say "axKit is a set of personal tools, shared as they are under the GNU AGPL 3: not a product or a service, with no support and no warranty. Keep backups of what matters. NOTICE.md beside the program has the licence and the reminders."
}

function Get-AppListing {
    $program = Join-Path $source 'AxKit.exe'
    $json = $null
    $reason = $null
    if (Test-Path -LiteralPath $program) {
        # Through a process with its output redirected: the program has no console of its own, so & would neither
        # wait for it nor catch what it writes. What went wrong is kept for the message: until 3.7.2 it was dropped,
        # and a failed update said only that the list could not be read.
        try {
            $start = New-Object System.Diagnostics.ProcessStartInfo $program
            $start.Arguments = 'apps'
            $start.UseShellExecute = $false
            $start.RedirectStandardOutput = $true
            $start.RedirectStandardError = $true
            $start.CreateNoWindow = $true
            $start.WorkingDirectory = $source
            $process = [System.Diagnostics.Process]::Start($start)
            $errors = $process.StandardError.ReadToEndAsync()
            $json = $process.StandardOutput.ReadToEnd()
            if (-not $process.WaitForExit(30000)) {
                $reason = 'it did not end within 30 seconds'
                $json = $null
            } elseif ($process.ExitCode -ne 0) {
                $reason = "it ended with exit code $($process.ExitCode)"
                if ($errors.Result.Trim()) { $reason += ": $($errors.Result.Trim())" }
                $json = $null
            } elseif (-not $json -or -not $json.Trim().StartsWith('[')) {
                $reason = "it ended without the list (it wrote $(if ($json) { $json.Length } else { 0 }) characters)"
                $json = $null
            }
        } catch {
            # Windows refused to start the new program (3.7.4 on one machine: "An Application Control policy has blocked
            # this file", Smart App Control): installing it would leave a program that cannot start, so nothing is
            # changed and the running version stays. An uninstall goes on with apps.json, which it only reads.
            if (-not $Uninstall) {
                throw "Windows will not run the new AxKit.exe ($($_.Exception.Message)), so nothing was changed and this version stays. On Windows 11 this is most often Smart App Control, which blocks a program that is not code-signed"
            }
            $reason = "it could not be run: $($_.Exception.Message)"
            $json = $null
        }
    } else {
        $reason = 'it is not there'
    }
    $saved = Join-Path $source $listingFile
    if (-not $json -and (Test-Path -LiteralPath $saved)) {
        Say "AxKit.exe apps did not answer ($reason); the list of apps is read from $listingFile instead."
        $json = Get-Content -LiteralPath $saved -Raw
    }
    if (-not $json) { throw "the list of apps could not be read from $program (AxKit.exe apps): $reason, and there is no $listingFile beside it" }
    return $json
}
$listingJson = Get-AppListing
$apps = @()
foreach ($entry in ($listingJson | ConvertFrom-Json)) {
    $iconFile = "$($entry.project).ico"
    # Title is the name as the user meets it, "axClaude, access layer for Claude Code" (AX-11.2); AccessLayer puts the app
    # after the standalone apps in File Explorer's submenu (AX-11.3).
    $app = @{ Name = $entry.name; Project = $entry.project; Title = $(if ($entry.title) { $entry.title } else { $entry.name }); AccessLayer = [bool]$entry.accessLayer
        Verb = $entry.verb; Description = $entry.description; Opens = @($entry.explorer)
        Icon = if (Test-Path -LiteralPath (Join-Path $source $iconFile)) { Join-Path $target $iconFile } else { $exe } }
    if (@($entry.types).Count -gt 0) { $app.Types = @($entry.types) }
    if (@($entry.openWith).Count -gt 0) { $app.OpenWith = @($entry.openWith); $app.TypeName = $entry.typeName }
    $apps += $app
}
# The places that follow from the name.
foreach ($app in $apps) {
    $app.Start = Join-Path $programs "axKit $($app.Title).lnk"
    # In the app's own case (axDown.cmd): what the user hears in a folder listing; Windows finds it typed any way.
    $app.Shim = Join-Path $bin ($app.Name + '.cmd')
    $app.ProgId = "AxKit.$($app.Name)"
    # Windows' Open with lists one program per program file, so an app with file types has a hard link of AxKit.exe
    # under its own name for its ProgID; started so, the program hands its arguments to AxKit.exe (src/AxKit/Program.cs).
    $app.Link = Join-Path $target "$($app.Name).exe"
}
$linkNames = @($apps | Where-Object { $_.OpenWith } | ForEach-Object { Split-Path -Leaf $_.Link })

# File Explorer's "Open with axKit" submenu (AX-2.3): on folders, on a folder's background and on files, each listing
# the apps whose Opens says so, in the order of their names. The registry shows an app only where it belongs, so
# no app has to check the path it is given.
$menus = @(
    @{ Key = "$classes\Directory\shell\AxKit"; Opens = 'folders'; Argument = '%1' },
    @{ Key = "$classes\Directory\Background\shell\AxKit"; Opens = 'folders'; Argument = '%V' },
    @{ Key = "$classes\*\shell\AxKit"; Opens = 'files'; Argument = '%1' }
)

# What the bundle's earlier names put on the machine (AX-2.6), all of it removed by an install or an uninstall: Axit,
# the bundle's name up to 4.0.0 (axKit since 2026-11-05, B-16), whose places are this version's under the old name,
# with the Start menu entries by title (4.0.0), by name (up to 3.7.6) and by project (up to 2.x, "Axit AxClaude");
# and axClaude 1.x, the first app alone, with one entry per app in File Explorer's menus (up to 2.13.0). A ProgID's
# Open with values live inside the file types' keys (Remove-OldOpenWith); Axit's axPreview had its own file types up
# to 3.7.1 and every file since.
$earlier = @(
    @{ Name = 'Axit'; Target = Join-Path $env:LOCALAPPDATA 'Programs\Axit'; Exe = 'Axit.exe'
        AppsKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Axit'
        StartMenu = @($apps | ForEach-Object { "Axit $($_.Title).lnk"; "Axit $($_.Name).lnk"; "Axit $($_.Project).lnk" } | Select-Object -Unique | ForEach-Object { Join-Path $programs $_ })
        MenuKeys = @("$classes\Directory\shell\Axit", "$classes\Directory\Background\shell\Axit", "$classes\*\shell\Axit")
        ProgIds = @($apps | Where-Object { $_.OpenWith } | ForEach-Object { "Axit.$($_.Name)" })
        Shims = @(Join-Path $bin 'axit.cmd'); RunValues = @('Axit axStart', 'Axit axClip') },
    @{ Name = 'axClaude'; Target = Join-Path $env:LOCALAPPDATA 'Programs\AxClaude'; Exe = 'AxClaude.exe'
        AppsKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AxClaude'
        StartMenu = @(Join-Path $programs 'AxClaude.lnk')
        MenuKeys = @("$classes\Directory\shell\AxClaude", "$classes\Directory\Background\shell\AxClaude",
            "$classes\Directory\shell\AxGit", "$classes\Directory\Background\shell\AxGit",
            "$classes\Directory\shell\AxTerminal", "$classes\Directory\Background\shell\AxTerminal", "$classes\*\shell\AxDown")
        ProgIds = @(); Shims = @(); RunValues = @() }
)
$oldPreviewTypes = @('.txt', '.md', '.markdown', '.log', '.json')
# Everything but the folder that an earlier name left: its Apps entry, Start menu entries, File Explorer keys, ProgIDs
# and console commands, as paths and registry paths.
$earlierPlaces = @($earlier | ForEach-Object { $_.AppsKey; $_.StartMenu; $_.MenuKeys; $_.Shims; @($_.ProgIds | ForEach-Object { "$classes\$_" }) })

# A ProgID's values taken out of the file types' Open with keys.
function Remove-OpenWithValues([string]$progId, [string[]]$extensions) {
    foreach ($extension in $extensions) {
        $key = "$classes\$extension\OpenWithProgids"
        if ((Test-Path -LiteralPath $key) -and (Get-ItemProperty -LiteralPath $key).PSObject.Properties[$progId]) {
            Remove-ItemProperty -LiteralPath $key -Name $progId
        }
    }
}
function Remove-OldOpenWith {
    foreach ($app in ($apps | Where-Object { $_.OpenWith })) {
        $types = @($app.OpenWith) + $(if ($app.Name -eq 'axPreview') { $oldPreviewTypes } else { @() })
        Remove-OpenWithValues "Axit.$($app.Name)" $types
    }
}

# Everything an installation puts on the machine, this version's and the earlier names', so that one removal clears
# all of them. The Open with values live inside the file types' own keys and are removed one by one
# (Remove-Installation).
$places = @($target, $appsKey) + @($menus | ForEach-Object { $_.Key }) +
    @($apps | Where-Object { $_.OpenWith } | ForEach-Object { "$classes\$($_.ProgId)" }) +
    @($apps | ForEach-Object { $_.Start }) + @(Join-Path $bin 'axKit.cmd') + @($apps | ForEach-Object { $_.Shim }) +
    @($earlier | ForEach-Object { $_.Target }) + $earlierPlaces

function Get-Version([string]$path) {
    (Get-Item $path).VersionInfo.FileVersion -replace '^(\d+\.\d+\.\d+).*', '$1'
}

# axStart and axClip run in the background from sign-in (docs/axStart/SPEC.md AS-1.3, AS-1.5; docs/axClip/SPEC.md
# CP-1.2): they are the apps that would always be running here, so they are stopped before anything is changed and
# started again, hidden, afterwards. Their processes are told by their verbs; axStart holds nothing unsaved, axClip
# writes a kept history as it ends. One table, the verb, the name, the settings file and what is said.
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$background = @(
    @{ Verb = 'start'; Name = 'axStart'; Settings = 'axStart.json'; What = 'the launcher'; Started = 'axStart, the launcher, runs in the background: Ctrl+Shift+Space, or the hotkey you chose, brings up its search field' },
    @{ Verb = 'clip'; Name = 'axClip'; Settings = 'axClip.json'; What = 'the clipboard history'; Started = 'axClip, the clipboard history, runs in the background: Windows+Alt+V, or the hotkey you chose, brings it up' },
    @{ Verb = 'help'; Name = 'axHelp'; Settings = 'axHelp.json'; What = 'the conversation with Claude'; Started = 'axHelp runs in the background: Windows+Alt+H brings up your conversation with Claude, Windows+Alt+S puts a picture of the screen into it' },
    # axDescribe merged into axHelp in 5.7.0: an older one is stopped for the installation and not started again.
    @{ Verb = 'describe'; Name = 'axDescribe'; Settings = 'axDescribe.json'; What = 'the former screen description'; Started = ''; Former = $true }
)
function Stop-Background {
    foreach ($app in $background) {
        # Under the old name too: an Axit.exe from sign-in holds its folder and its hotkey.
        $running = @(Get-CimInstance Win32_Process -Filter "Name = 'AxKit.exe' OR Name = 'Axit.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -match ('^"?[^"]*\\(AxKit|Axit)[^"\\]*\.exe"?\s+' + $app.Verb + '(\s|$)') })
        foreach ($process in $running) {
            try { Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop; Wait-Process -Id $process.ProcessId -Timeout 10 -ErrorAction SilentlyContinue } catch { }
        }
        if ($running) { Say "Stopped $($app.Name), $($app.What), for the installation" }
    }
}

function Start-Background {
    foreach ($app in $background) {
        if ($app.Former) { continue }
        # Not when the user turned Start with Windows off in the app's settings.
        $settingsFile = Join-Path $env:APPDATA "AxKit\$($app.Settings)"
        if (Test-Path -LiteralPath $settingsFile) {
            try { if ((Get-Content -LiteralPath $settingsFile -Raw | ConvertFrom-Json).startWithWindows -eq $false) { continue } } catch { }
        }
        # A background app that does not start is said, not a failed installation: the files are in place by now.
        try {
            Start-Process -FilePath $exe -ArgumentList @($app.Verb, '--hidden') -WorkingDirectory $target -ErrorAction Stop
            Say $app.Started
        } catch {
            Say "$($app.Name) could not be started again: $($_.Exception.Message)"
        }
    }
}

function Test-Running {
    $names = @('AxKit') + @($earlier | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_.Exe) }) + @($linkNames | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) })
    $exes = @($exe) + @($earlier | ForEach-Object { Join-Path $_.Target $_.Exe })
    [bool](Get-Process -Name $names -ErrorAction SilentlyContinue | Where-Object { $exes -contains $_.Path -or ($_.Path -and (Split-Path -Parent $_.Path) -eq $target) })
}

# An app's Open with link (AX-2.3), made again on every install so that it is the AxKit.exe just written: one still
# running is the old program file and is renamed aside as the program file is, for the next start to remove.
function Set-OpenWithLink($app) {
    if (Test-Path -LiteralPath $app.Link) {
        try { Remove-Item -LiteralPath $app.Link -Force -ErrorAction Stop }
        catch {
            $aside = Join-Path $target ("$($app.Name).old." + (Get-Date -Format 'HHmmss') + '.exe')
            Move-Item -LiteralPath $app.Link -Destination $aside -Force
        }
    }
    try {
        New-Item -ItemType HardLink -Path $app.Link -Target $exe -ErrorAction Stop | Out-Null
        return $app.Link
    } catch {
        # A drive without hard links: the ProgID names AxKit.exe, and Windows' Open with lists only one of these apps.
        return $exe
    }
}

# Removes everything an installation put on the machine, whatever version made it. The settings and the logs stay.
# Windows can hold the executable for a moment after the app exits, so a folder is tried again for a few seconds.
# Registry paths go through -LiteralPath: the file keys have a * in them, which PowerShell would otherwise expand.
function Remove-Installation {
    foreach ($folder in @($target) + @($earlier | ForEach-Object { $_.Target })) {
        for ($attempt = 1; Test-Path -LiteralPath $folder; $attempt++) {
            try { Remove-Item -LiteralPath $folder -Recurse -Force }
            catch {
                if ($attempt -ge 10) { throw }
                Start-Sleep -Seconds 1
            }
        }
    }
    foreach ($item in $places) {
        if (Test-Path -LiteralPath $item) { Remove-Item -LiteralPath $item -Recurse -Force }
    }
    foreach ($value in @($background | ForEach-Object { "AxKit $($_.Name)" }) + @($earlier | ForEach-Object { $_.RunValues })) {
        if ((Test-Path -LiteralPath $runKey) -and (Get-ItemProperty -LiteralPath $runKey).PSObject.Properties[$value]) {
            Remove-ItemProperty -LiteralPath $runKey -Name $value
        }
    }
    foreach ($app in ($apps | Where-Object { $_.OpenWith })) {
        Remove-OpenWithValues $app.ProgId $app.OpenWith
    }
    Remove-OldOpenWith
}

if ($Uninstall) {
    # A running app holds its folder: then nothing is removed, so the Apps entry and install.cmd stay to try again.
    Stop-Background
    if (Test-Running) {
        Say "axKit is still running, so nothing was removed. Close it and uninstall again."
        exit 1
    }

    Remove-Installation
    Say "axKit was removed. Settings in $env:APPDATA\AxKit and the logs in $env:LOCALAPPDATA\AxKit were kept."
    exit 0
}

$sourceExe = Join-Path $source 'AxKit.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) {
    Say "AxKit.exe was not found next to this script in $source."
    exit 1
}
Stop-Background
$sameFolder = [string]::Equals((Resolve-Path $source).Path.TrimEnd('\'), $target.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)
if (-not $sameFolder) {
    # A previous installation, of axKit, of Axit or of axClaude 1.x, is removed completely first, so nothing of it
    # stays behind; then this one is installed fresh. The older updaters come here too, after the running app has closed.
    # In place (-InPlace, the apps' own update) the app keeps running: the files are written over the old ones, the
    # running program file is renamed, since it can be renamed but not overwritten or deleted, and what earlier
    # versions left goes afterwards, so that an update leaves the machine as a fresh install would.
    $found = @($places | Where-Object { Test-Path -LiteralPath $_ })
    if ($found -and -not $InPlace) {
        if (Test-Running) {
            Say "axKit is still running, so nothing was changed. Close it and run this again."
            exit 1
        }

        $what = 'what is left of an earlier installation'
        foreach ($program in @(@{ Name = 'axKit'; Path = $exe }) + @($earlier | ForEach-Object { @{ Name = $_.Name; Path = Join-Path $_.Target $_.Exe } })) {
            if (Test-Path -LiteralPath $program.Path) { $what = "$($program.Name) " + (Get-Version $program.Path); break }
        }
        Say "Removing $what before installing..."
        Remove-Installation
    }

    New-Item -ItemType Directory -Force $target | Out-Null
    $files = @('AxKit.exe', 'README.md', 'LICENSE', 'NOTICE.md', 'PRIVACY.md', 'THIRD-PARTY-NOTICES.md', 'install.ps1', 'install.cmd', $listingFile) + @($apps | ForEach-Object { Split-Path -Leaf $_.Icon }) | Select-Object -Unique
    foreach ($name in $files) {
        $file = Join-Path $source $name
        if (-not (Test-Path -LiteralPath $file)) { continue }
        $destination = Join-Path $target $name
        if ($InPlace -and $name -eq 'AxKit.exe' -and (Test-Path -LiteralPath $destination)) {
            $old = Join-Path $target 'AxKit.old.exe'
            if (Test-Path -LiteralPath $old) { try { Remove-Item -LiteralPath $old -Force } catch { } }
            if (Test-Path -LiteralPath $old) { $old = Join-Path $target ('AxKit.old.' + (Get-Date -Format 'HHmmss') + '.exe') }
            Move-Item -LiteralPath $destination -Destination $old -Force
            Say "Moved the running program file aside as $(Split-Path -Leaf $old); the next start removes it."
        }
        Copy-Item -LiteralPath $file -Destination $target -Force
    }

    if ($InPlace) {
        # What earlier names and versions left and this one does not write again: their folders, Apps entries, Start
        # menu entries, console commands, ProgIDs, File Explorer keys and sign-in values, and files in the folder this
        # version does not ship (the program file moved aside goes at the next start).
        foreach ($item in @($earlier | ForEach-Object { $_.Target }) + $earlierPlaces) {
            if (Test-Path -LiteralPath $item) { try { Remove-Item -LiteralPath $item -Recurse -Force } catch { } }
        }
        foreach ($value in @($earlier | ForEach-Object { $_.RunValues })) {
            if ((Test-Path -LiteralPath $runKey) -and (Get-ItemProperty -LiteralPath $runKey).PSObject.Properties[$value]) { Remove-ItemProperty -LiteralPath $runKey -Name $value }
        }
        Remove-OldOpenWith
        foreach ($stale in Get-ChildItem -LiteralPath $target -File | Where-Object { $files -notcontains $_.Name -and $linkNames -notcontains $_.Name -and $_.Name -notlike '*.old*.exe' }) {
            try { Remove-Item -LiteralPath $stale.FullName -Force } catch { }
        }
    }
}

# The list of apps this installation was made from, for an uninstall that cannot run the program (Get-AppListing).
[System.IO.File]::WriteAllText((Join-Path $target $listingFile), $listingJson)

# Files extracted from a downloaded zip carry the "downloaded from the internet" mark, which makes SmartScreen warn
# about an unknown publisher on every start. The installed copies lose the mark, so the warning does not come back.
try { Get-ChildItem $target -File | Unblock-File -ErrorAction Stop } catch { }

New-Item -ItemType Directory -Force $bin | Out-Null
Set-Content -Path (Join-Path $bin 'axKit.cmd') -Value @('@echo off', "`"$exe`" %*") -Encoding ascii
foreach ($app in $apps) {
    Set-Content -Path $app.Shim -Value @('@echo off', "`"$exe`" $($app.Verb) %*") -Encoding ascii
}

# The commands work in a console only when their folder is on the PATH. Claude Code's installer puts it there; without
# Claude Code (docs/axKit/SPEC.md G6) this install does, in the user's PATH, which needs no rights. The registry value
# is read as it is stored (unexpanded) and written back as an expandable string, so that an entry like
# %USERPROFILE%\... stays one; [Environment]::SetEnvironmentVariable would expand them all into a plain string. The
# removal leaves the PATH as it is: the folder may hold other programs' commands.
$environment = Get-Item -LiteralPath 'HKCU:\Environment'
$userPath = [string]$environment.GetValue('Path', '', 'DoNotExpandEnvironmentNames')
$onPath = @([Environment]::ExpandEnvironmentVariables($userPath) -split ';' | ForEach-Object { $_.Trim().TrimEnd('\') })
if ($onPath -notcontains $bin.TrimEnd('\')) {
    $entry = '%USERPROFILE%\.local\bin'
    $newPath = if ($userPath.Trim()) { $userPath.TrimEnd(';') + ';' + $entry } else { $entry }
    Set-ItemProperty -LiteralPath 'HKCU:\Environment' -Name 'Path' -Value $newPath -Type ExpandString
    # Running programs (Explorer, so that a console opened from the Start menu from now on has the folder) learn of
    # the change from the settings-change message, which [Environment]::SetEnvironmentVariable sends after any write,
    # the removal of a value that does not exist included: that is this line's whole effect.
    [Environment]::SetEnvironmentVariable('AXKIT_PATH_CHANGED', $null, 'User')
    Say "Added $entry to your PATH: a console opened from now on finds the axKit commands."
}

if (-not $NoStartMenu) {
    $shell = New-Object -ComObject WScript.Shell
    foreach ($app in $apps) {
        # Removed first: a file written over one of the same name keeps the old one's capitals, and the shortcut's
        # name is what the Start menu shows and NVDA reads ("axKit axClaude", once "axKit AxClaude").
        Remove-Item -LiteralPath $app.Start -ErrorAction SilentlyContinue
        $link = $shell.CreateShortcut($app.Start)
        $link.TargetPath = $exe
        $link.Arguments = $app.Verb
        $link.WorkingDirectory = $target
        $link.Description = $app.Description
        $link.IconLocation = "$($app.Icon),0"
        $link.Save()
    }
}

if (-not $NoContextMenu) {
    # A static cascading menu (Windows 7 and later): the submenu's key has its name in MUIVerb and an empty
    # SubCommands, and the entries are verbs under its shell subkey. AppliesTo keeps an entry to the file types it
    # names (Advanced Query Syntax, as Windows' own verbs do). The entries of earlier versions go first, on every
    # path through here, the in-place update's included.
    foreach ($key in @($earlier | ForEach-Object { $_.MenuKeys })) {
        if (Test-Path -LiteralPath $key) { Remove-Item -LiteralPath $key -Recurse -Force }
    }

    foreach ($menu in $menus) {
        New-Item -Path $menu.Key -Force | Out-Null
        # The entries made again from nothing: New-Item -Force keeps an existing key's children, and the in-place update,
        # which removes no installation, would otherwise leave an earlier version's entries beside the new ones (their
        # names gained an order number in 4.0.0, so every app would have shown twice; found in review, 2026-11-01).
        Remove-Item -LiteralPath (Join-Path $menu.Key 'shell') -Recurse -Force -ErrorAction SilentlyContinue
        Set-ItemProperty -LiteralPath $menu.Key -Name 'MUIVerb' -Value 'Open with axKit'
        Set-ItemProperty -LiteralPath $menu.Key -Name 'Icon' -Value "`"$exe`",0"
        Set-ItemProperty -LiteralPath $menu.Key -Name 'SubCommands' -Value ''
        # File Explorer orders the entries by their keys' names: "1 axFile" for a standalone app, "2 axClaude" for an
        # access layer, so the access layers come last, after a separator (AX-11.3; ECF_SEPARATORBEFORE, 0x20).
        $here = @($apps | Where-Object { $_.Opens -contains $menu.Opens })
        $firstLayer = @($here | Where-Object { $_.AccessLayer } | Sort-Object { $_.Name }) | Select-Object -First 1
        foreach ($app in $here) {
            $key = Join-Path $menu.Key "shell\$(if ($app.AccessLayer) { '2' } else { '1' }) $($app.Name)"
            New-Item -Path (Join-Path $key 'command') -Force | Out-Null
            Set-ItemProperty -LiteralPath $key -Name 'MUIVerb' -Value $app.Title
            if ($firstLayer -and $app.Name -eq $firstLayer.Name) {
                New-ItemProperty -LiteralPath $key -Name 'CommandFlags' -Value 0x20 -PropertyType DWord -Force | Out-Null
            }
            Set-ItemProperty -LiteralPath $key -Name 'Icon' -Value "`"$($app.Icon)`",0"
            if ($app.Types) {
                Set-ItemProperty -LiteralPath $key -Name 'AppliesTo' -Value (($app.Types | ForEach-Object { "System.FileExtension:=`"$_`"" }) -join ' OR ')
            }
            Set-ItemProperty -LiteralPath (Join-Path $key 'command') -Name '(default)' -Value "`"$exe`" $($app.Verb) `"$($menu.Argument)`""
        }
    }

    # The ProgID that Open with and Default apps know an app by (AX-2.3, B-9): the user makes it the default, never
    # this script. The type * is every file (HKCU\Software\Classes\*\OpenWithProgids).
    Remove-OldOpenWith
    foreach ($app in ($apps | Where-Object { $_.OpenWith })) {
        $program = Set-OpenWithLink $app
        $progIdKey = "$classes\$($app.ProgId)"
        New-Item -Path $progIdKey -Force | Out-Null
        Set-ItemProperty -LiteralPath $progIdKey -Name '(default)' -Value $app.TypeName
        Set-ItemProperty -LiteralPath $progIdKey -Name 'FriendlyTypeName' -Value $app.TypeName
        New-Item -Path (Join-Path $progIdKey 'DefaultIcon') -Force | Out-Null
        Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'DefaultIcon') -Name '(default)' -Value $app.Icon
        New-Item -Path (Join-Path $progIdKey 'Application') -Force | Out-Null
        Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'Application') -Name 'ApplicationName' -Value $app.Title
        Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'Application') -Name 'ApplicationIcon' -Value $app.Icon
        New-Item -Path (Join-Path $progIdKey 'shell\open\command') -Force | Out-Null
        Set-ItemProperty -LiteralPath (Join-Path $progIdKey 'shell\open\command') -Name '(default)' -Value "`"$program`" $($app.Verb) `"%1`""
        foreach ($extension in $app.OpenWith) {
            $openWith = "$classes\$extension\OpenWithProgids"
            # Made only when missing: New-Item -Force on an existing key empties it, which took the other programs'
            # values of the type with it, and axDown's when axPreview came next (up to 3.7.1; measured 2026-10-24).
            if (-not (Test-Path -LiteralPath $openWith)) { New-Item -Path $openWith -Force | Out-Null }
            New-ItemProperty -LiteralPath $openWith -Name $app.ProgId -Value ([byte[]]@()) -PropertyType None -Force | Out-Null
        }
    }
}

New-Item -Path $appsKey -Force | Out-Null
$uninstallCmd = Join-Path $target 'install.cmd'
$appsUninstall = if (Test-Path -LiteralPath $uninstallCmd) { "`"$uninstallCmd`" -Uninstall" }
    else { "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$(Join-Path $target 'install.ps1')`" -Uninstall" }
$entries = [ordered]@{
    DisplayName = 'axKit'
    DisplayVersion = (Get-Version $exe)
    Publisher = 'Dr. Kyle Keane'
    DisplayIcon = "`"$exe`",0"
    InstallLocation = $target
    UninstallString = $appsUninstall
    URLInfoAbout = 'https://github.com/KyleKeane/AxKit'
}
foreach ($name in $entries.Keys) { Set-ItemProperty -LiteralPath $appsKey -Name $name -Value $entries[$name] }
foreach ($name in 'NoModify', 'NoRepair') { New-ItemProperty -LiteralPath $appsKey -Name $name -Value 1 -PropertyType DWord -Force | Out-Null }
New-ItemProperty -LiteralPath $appsKey -Name 'EstimatedSize' -Value ([int]((Get-Item $exe).Length / 1KB)) -PropertyType DWord -Force | Out-Null

$names = @($apps | ForEach-Object { $_.Name })
Say "Installed axKit to $target"
Say "Settings, Apps: axKit, with Uninstall"
if (-not $NoStartMenu) { Say "Start menu: $(($names | ForEach-Object { "axKit $_" }) -join ', ') (press the Windows key and type axKit to list them)" }
if (-not $NoContextMenu) {
    $onFolders = @($apps | Where-Object { $_.Opens -contains 'folders' } | ForEach-Object { $_.Name }) -join ', '
    $onFiles = @($apps | Where-Object { $_.Opens -contains 'files' } | ForEach-Object { if ($_.Types) { "$($_.Name) on $($_.Types -join ', ')" } else { "$($_.Name) on every file" } }) -join ', '
    $openWith = @($apps | Where-Object { $_.OpenWith } | ForEach-Object { "$($_.Name) for $(if ($_.OpenWith -contains '*') { 'every file' } else { $_.OpenWith -join ', ' })" }) -join '; '
    Say "File Explorer: Open with axKit in the right-click menu: $onFolders on folders; $onFiles. Under Open with: $openWith"
}
Say "Console: $($names -join ', '), each taking a folder or file, or axKit <folder or file> (shims in $bin)"
if (-not (($env:Path -split ';') -contains $bin)) {
    Say "Note: $bin is not on PATH in this console. Open a new console, or add it, before using the commands."
}

Start-Background

if ($Start) {
    $arguments = @('claude', "`"$Start`"")
    if ($ContinueConversation) { $arguments += @('--', '--continue') }
    Say "Starting axClaude on $Start"
    Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $target
}

if ($StartFile) {
    Say "Starting axDown on $StartFile"
    Start-Process -FilePath $exe -ArgumentList @('down', "`"$StartFile`"") -WorkingDirectory $target
}
