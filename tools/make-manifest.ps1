<#
.SYNOPSIS
  Writes the MSIX package manifest, AppxManifest.xml, for publish.ps1 -Msix from the apps' listing (AxKit.exe apps,
  apps.json), so that the package declares the apps from the same list as everything else (docs/axkit/SPEC.md B-10,
  AX-13.2; docs/axkit/store-distribution.md section 3).

.DESCRIPTION
  The Store allows eight <Application> entries per package and no hidden one (its validation of 2026-11-09: "more than
  the limit of 8", and AppListEntry="none" needs a waiver), so the entries are the apps marked storeEntry in the
  listing (Dispatch.Apps), eight at most. The axClaude entry comes first, has the id AxKit and no parameters, so that
  it starts axClaude as AxKit.exe does with nothing (AX-1.3) and carries what the whole bundle needs: every app's
  console command (axkit.exe, axclaude.exe, axdown.exe ...; the program tells the app by the command's own name,
  Dispatch.VerbOfProgramName) and the file types of the apps that have no entry (the program tells the app by the
  file's kind, AX-1.2). Every other entry runs AxKit.exe with its verb as parameters (uap10:Parameters, which Windows
  passes), with its own name, images and file types; an app that starts at sign-in carries its startup task with
  "<verb> --hidden" as the task's parameters. Windows lists the entries in the Start menu as "axKit <title>" and the
  tasks under Settings, Apps, Startup.

  The identity values are the Store's for axKit (Partner Center, Product identity, 2026-11-08) and are not secrets.
  The version is the bundle's with .0 appended, the fourth part being the Store's. The minimum Windows is 10.0.19041
  (version 2004), the first with the uap10 manifest namespace.

.EXAMPLE
  .\tools\make-manifest.ps1 -Apps publish\msix\apps.json -Out publish\msix\package\AppxManifest.xml -Version 5.2.1
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Apps,
    [Parameter(Mandatory)][string]$Out,
    [Parameter(Mandatory)][string]$Version,
    [string]$Name = 'axKit.axKit',
    [string]$Publisher = 'CN=F1B55F68-5EAE-4338-B94B-73044EAE40DA',
    [string]$PublisherDisplayName = 'axKit',
    [string]$Description = 'Accessible Windows apps for screen reader users: a front end for Claude Code, editors for text, Markdown and JSON, a git manager, a terminal, a file manager, a player, a viewer, a launcher, a process list, tasks, a password database, a calculator and a clipboard history, in one program.',
    [string]$MinVersion = '10.0.19041.0',
    [string]$MaxVersionTested = '10.0.26100.0'
)

$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "The version must be major.minor.patch ('$Version' was given); the fourth part is the Store's and is written as 0."
}
# ForEach-Object unrolls the array: Windows PowerShell 5.1 hands a JSON array back as one object.
$listing = @(Get-Content -LiteralPath $Apps -Raw -Encoding UTF8 | ConvertFrom-Json | ForEach-Object { $_ })
if ($listing.Count -eq 0) { throw "$Apps lists no apps." }

$entries = @($listing | Where-Object { $_.storeEntry })
if ($entries.Count -eq 0) { throw 'No app is marked storeEntry; the package needs at least one entry.' }
if ($entries.Count -gt 8) { throw "The Store allows eight entries per package; $($entries.Count) apps are marked storeEntry." }
$firstEntry = $entries | Where-Object { $_.verb -eq 'claude' } | Select-Object -First 1
if (-not $firstEntry) { $firstEntry = $entries[0] }
$otherEntries = @($entries | Where-Object { $_.verb -ne $firstEntry.verb })
# A startup task hangs on an entry (SPEC AX-13.2): an app that starts at sign-in without one, the eight being taken,
# has no sign-in start in the Store's copy and is started from another window there (axHelp, axDescribe, 2026-11-09).
foreach ($app in $listing | Where-Object { $_.atSignIn -and -not $_.storeEntry }) {
    Write-Host "$($app.name) starts at sign-in but has no entry: no startup task in the package; it is started from another window."
}

function Esc([string]$text) { return [System.Security.SecurityElement]::Escape($text) }

$lines = New-Object System.Collections.Generic.List[string]
function Line([string]$text) { $lines.Add($text) }

# One <Application>: the entry's id, what the Start menu shows, its assets folder, the command line Windows passes
# (empty for none), and its extensions as lines already indented by eight spaces.
function Add-Application([string]$id, [string]$displayName, [string]$description, [string]$assets, [string]$parameters, [string[]]$extensions) {
    $open = "    <Application Id=`"$(Esc $id)`" Executable=`"AxKit.exe`" uap10:RuntimeBehavior=`"packagedClassicApp`" uap10:TrustLevel=`"mediumIL`""
    if ($parameters) { $open += " uap10:Parameters=`"$(Esc $parameters)`"" }
    Line "$open>"
    Line ("      <uap:VisualElements DisplayName=`"$(Esc $displayName)`" Description=`"$(Esc $description)`" BackgroundColor=`"transparent`"" +
        " Square150x150Logo=`"Assets\$assets\Square150x150Logo.png`" Square44x44Logo=`"Assets\$assets\Square44x44Logo.png`" />")
    if ($extensions.Count -gt 0) {
        Line '      <Extensions>'
        foreach ($extension in $extensions) { Line $extension }
        Line '      </Extensions>'
    }
    Line '    </Application>'
}

# The console commands of one entry: one extension, one alias each.
function Alias-Extension([string[]]$aliases) {
    $result = @('        <uap5:Extension Category="windows.appExecutionAlias" Executable="AxKit.exe" EntryPoint="Windows.FullTrustApplication">', '          <uap5:AppExecutionAlias>')
    foreach ($alias in $aliases) { $result += "            <uap5:ExecutionAlias Alias=`"$(Esc $alias)`" />" }
    $result += @('          </uap5:AppExecutionAlias>', '        </uap5:Extension>')
    return $result
}

function FileTypes-Extension([string]$ftaName, [string]$typeName, [string[]]$types) {
    $result = @(
        '        <uap:Extension Category="windows.fileTypeAssociation">',
        "          <uap3:FileTypeAssociation Name=`"$(Esc $ftaName)`">",
        "            <uap:DisplayName>$(Esc $typeName)</uap:DisplayName>",
        '            <uap:SupportedFileTypes>'
    )
    foreach ($type in $types) { $result += "              <uap:FileType>$(Esc $type)</uap:FileType>" }
    $result += @('            </uap:SupportedFileTypes>', '          </uap3:FileTypeAssociation>', '        </uap:Extension>')
    return $result
}

# The startup task of an app that runs from sign-in: its own parameters, since the task starts the app with none of
# the entry's (a hidden entry per task was the first design; the Store allows no hidden entry).
function StartupTask-Extension([string]$taskId, [string]$displayName, [string]$parameters) {
    return @(
        "        <desktop:Extension Category=`"windows.startupTask`" Executable=`"AxKit.exe`" EntryPoint=`"Windows.FullTrustApplication`" uap10:Parameters=`"$(Esc $parameters)`">",
        "          <desktop:StartupTask TaskId=`"$(Esc $taskId)`" Enabled=`"true`" DisplayName=`"$(Esc $displayName)`" />",
        '        </desktop:Extension>'
    )
}

# A package declares a file type once (makeappx refuses a second), so the first app of the list keeps a type two apps
# share, and an app whose Open with says every file ("*", axPreview) declares none: a package cannot claim every file.
$declared = New-Object 'System.Collections.Generic.HashSet[string]'
function Types-Of($app) {
    $types = @($app.openWith | Where-Object { $_ -and $_ -ne '*' } | ForEach-Object { $_.ToLowerInvariant() } | Sort-Object -Unique | Where-Object { -not $declared.Contains($_) })
    foreach ($type in $types) { [void]$declared.Add($type) }
    return $types
}
function FileTypes-Of($app) {
    $types = Types-Of $app
    if ($types.Count -eq 0) { return @() }
    $typeName = if ($app.typeName) { $app.typeName } else { "File ($($app.name))" }
    return FileTypes-Extension ("$($app.name)".ToLowerInvariant() + '-files') $typeName $types
}

Line '<?xml version="1.0" encoding="utf-8"?>'
Line '<!-- Written by tools/make-manifest.ps1 from the apps listing (AxKit.exe apps); a change is made there, never here. -->'
Line '<Package'
Line '  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"'
Line '  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"'
Line '  xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3"'
Line '  xmlns:uap5="http://schemas.microsoft.com/appx/manifest/uap/windows10/5"'
Line '  xmlns:uap10="http://schemas.microsoft.com/appx/manifest/uap/windows10/10"'
Line '  xmlns:desktop="http://schemas.microsoft.com/appx/manifest/desktop/windows10"'
Line '  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"'
Line '  IgnorableNamespaces="uap uap3 uap5 uap10 desktop rescap">'
Line "  <Identity Name=`"$(Esc $Name)`" Publisher=`"$(Esc $Publisher)`" Version=`"$Version.0`" ProcessorArchitecture=`"x64`" />"
Line '  <Properties>'
Line '    <DisplayName>axKit</DisplayName>'
Line "    <PublisherDisplayName>$(Esc $PublisherDisplayName)</PublisherDisplayName>"
Line '    <Logo>Assets\StoreLogo.png</Logo>'
Line "    <Description>$(Esc $Description)</Description>"
Line '  </Properties>'
Line '  <Resources>'
Line '    <Resource Language="en-us" />'
Line '  </Resources>'
Line '  <Dependencies>'
Line "    <TargetDeviceFamily Name=`"Windows.Desktop`" MinVersion=`"$(Esc $MinVersion)`" MaxVersionTested=`"$(Esc $MaxVersionTested)`" />"
Line '  </Dependencies>'
Line '  <Capabilities>'
Line '    <rescap:Capability Name="runFullTrust" />'
Line '  </Capabilities>'
Line '  <Applications>'

# The other entries first in the walk, so that their file types are declared under them; the first entry is written
# first all the same, carrying every command and the types of the apps without an entry.
$otherBlocks = @{}
foreach ($app in $otherEntries) {
    $extensions = @()
    $extensions += FileTypes-Of $app
    if ($app.atSignIn) { $extensions += StartupTask-Extension $app.project "axKit $($app.name)" "$($app.verb) --hidden" }
    $otherBlocks[$app.verb] = $extensions
}
$firstExtensions = @()
$firstExtensions += Alias-Extension (@('axkit.exe') + @($listing | ForEach-Object { "$($_.name)".ToLowerInvariant() + '.exe' }))
$firstExtensions += FileTypes-Of $firstEntry
foreach ($app in $listing | Where-Object { -not $_.storeEntry }) { $firstExtensions += FileTypes-Of $app }
if ($firstEntry.atSignIn) { $firstExtensions += StartupTask-Extension $firstEntry.project "axKit $($firstEntry.name)" "$($firstEntry.verb) --hidden" }
Add-Application 'AxKit' "axKit $($firstEntry.title)" $firstEntry.description $firstEntry.project '' $firstExtensions
foreach ($app in $otherEntries) {
    Add-Application $app.project "axKit $($app.title)" $app.description $app.project $app.verb $otherBlocks[$app.verb]
}

Line '  </Applications>'
Line '</Package>'

$folder = Split-Path -Parent ([System.IO.Path]::GetFullPath($Out))
if ($folder -and -not (Test-Path $folder)) { New-Item -ItemType Directory -Force $folder | Out-Null }
[System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($Out), (($lines -join "`r`n") + "`r`n"), (New-Object System.Text.UTF8Encoding $false))
Write-Host "Wrote $Out for $Name $Version.0: $($entries.Count) entries for $($listing.Count) apps, $(@($listing | Where-Object { $_.atSignIn }).Count) startup tasks, $($declared.Count) file types."
