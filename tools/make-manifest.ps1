<#
.SYNOPSIS
  Writes the MSIX package manifest, AppxManifest.xml, for publish.ps1 -Msix from the apps' listing (AxKit.exe apps,
  apps.json), so that the package declares the apps from the same list as everything else (docs/axkit/SPEC.md B-10,
  docs/axkit/store-distribution.md section 3).

.DESCRIPTION
  One <Application> per app, all running AxKit.exe: its Start menu name "axKit <title>" as install.ps1 gives it, its
  icon from Assets\<Project>\, its verb as the command line Windows passes when the entry is started
  (uap10:Parameters), a console command <name>.exe (axdown.exe) as an app execution alias, and, for an app with file
  types, their association so that Windows' Open with lists the app on them. Two kinds of hidden entries follow, out
  of the apps list: "AxKit", the bundle's own command axkit.exe, which takes a verb as AxKit.exe does; and
  "<Project>SignIn" for every app that starts at sign-in (atSignIn in the listing), carrying its startup task, which
  Windows lists under Settings, Apps, Startup. The program tells the entry it was started from by its application id
  (AxKit.Core.PackageIdentity, Dispatch.WithApplicationVerb), so the verb is known even where Windows passes no
  parameters.

  The identity values are the Store's for axKit (Partner Center, Product identity, 2026-11-08) and are not secrets:
  they are in every copy of the package. The version is the bundle's with .0 appended, the fourth part being the
  Store's. The minimum Windows is 10.0.19041 (version 2004), the first with the uap10 manifest namespace.

.EXAMPLE
  .\tools\make-manifest.ps1 -Apps publish\msix\package\apps.json -Out publish\msix\package\AppxManifest.xml -Version 5.2.0
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

function Esc([string]$text) { return [System.Security.SecurityElement]::Escape($text) }

$lines = New-Object System.Collections.Generic.List[string]
function Line([string]$text) { $lines.Add($text) }

# One <Application>: the entry's id, what the Start menu shows, its assets folder, the command line Windows passes
# (empty for none), whether the app list shows it, and its extensions as lines already indented by eight spaces.
function Add-Application([string]$id, [string]$displayName, [string]$description, [string]$assets, [string]$parameters, [bool]$listed, [string[]]$extensions) {
    $open = "    <Application Id=`"$(Esc $id)`" Executable=`"AxKit.exe`" uap10:RuntimeBehavior=`"packagedClassicApp`" uap10:TrustLevel=`"mediumIL`""
    if ($parameters) { $open += " uap10:Parameters=`"$(Esc $parameters)`"" }
    Line "$open>"
    $visual = "      <uap:VisualElements DisplayName=`"$(Esc $displayName)`" Description=`"$(Esc $description)`" BackgroundColor=`"transparent`"" +
        " Square150x150Logo=`"Assets\$assets\Square150x150Logo.png`" Square44x44Logo=`"Assets\$assets\Square44x44Logo.png`""
    if (-not $listed) { $visual += ' AppListEntry="none"' }
    Line "$visual />"
    if ($extensions.Count -gt 0) {
        Line '      <Extensions>'
        foreach ($extension in $extensions) { Line $extension }
        Line '      </Extensions>'
    }
    Line '    </Application>'
}

function Alias-Extension([string]$alias) {
    return @(
        '        <uap5:Extension Category="windows.appExecutionAlias" Executable="AxKit.exe" EntryPoint="Windows.FullTrustApplication">',
        '          <uap5:AppExecutionAlias>',
        "            <uap5:ExecutionAlias Alias=`"$(Esc $alias)`" />",
        '          </uap5:AppExecutionAlias>',
        '        </uap5:Extension>'
    )
}

function FileTypes-Extension([string]$ftaName, [string]$typeName, [string[]]$types) {
    $result = @(
        '        <uap:Extension Category="windows.fileTypeAssociation">',
        "          <uap3:FileTypeAssociation Name=`"$(Esc $ftaName)`">",
        "            <uap:DisplayName>$(Esc $typeName)</uap:DisplayName>",
        '            <uap:SupportedFileTypes>'
    )
    foreach ($type in $types) { $result += "              <uap:FileType>$(Esc $type)</uap:FileType>" }
    $result += @(
        '            </uap:SupportedFileTypes>',
        '          </uap3:FileTypeAssociation>',
        '        </uap:Extension>'
    )
    return $result
}

function StartupTask-Extension([string]$taskId, [string]$displayName) {
    return @(
        '        <desktop:Extension Category="windows.startupTask" Executable="AxKit.exe" EntryPoint="Windows.FullTrustApplication">',
        "          <desktop:StartupTask TaskId=`"$(Esc $taskId)`" Enabled=`"true`" DisplayName=`"$(Esc $displayName)`" />",
        '        </desktop:Extension>'
    )
}

# A package declares a file type once (makeappx refuses a second), so the first app of the list keeps a type two apps
# share, and an app whose Open with says every file ("*", axPreview) declares none: a package cannot claim every file.
$declared = New-Object 'System.Collections.Generic.HashSet[string]'

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

foreach ($app in $listing) {
    $extensions = @()
    $extensions += Alias-Extension ("$($app.name)".ToLowerInvariant() + '.exe')
    $types = @($app.openWith | Where-Object { $_ -and $_ -ne '*' } | ForEach-Object { $_.ToLowerInvariant() } | Sort-Object -Unique | Where-Object { -not $declared.Contains($_) })
    foreach ($type in $types) { [void]$declared.Add($type) }
    if ($types.Count -gt 0) {
        $typeName = if ($app.typeName) { $app.typeName } else { "File ($($app.name))" }
        $extensions += FileTypes-Extension ("$($app.name)".ToLowerInvariant() + '-files') $typeName $types
    }
    Add-Application $app.project "axKit $($app.title)" $app.description $app.project $app.verb $true $extensions
}

# The bundle's own command, axkit.exe, with no verb of its own: AxKit.exe reads the verb from the command line as it
# does outside the package. Hidden from the app list; it carries the bundle's icon.
$bundle = $listing | Where-Object { $_.verb -eq 'claude' } | Select-Object -First 1
if (-not $bundle) { $bundle = $listing[0] }
Add-Application 'AxKit' 'axKit' 'axKit, the command that starts any app by its verb' $bundle.project '' $false (Alias-Extension 'axkit.exe')

# The apps that start at sign-in, each as a hidden entry with its startup task: started from it, the program knows
# the verb and --hidden from the entry's id when Windows passes no parameters.
foreach ($app in $listing | Where-Object { $_.atSignIn }) {
    Add-Application "$($app.project)SignIn" "axKit $($app.name) at sign-in" "$($app.name), started hidden at sign-in" $app.project "$($app.verb) --hidden" $false (StartupTask-Extension $app.project "axKit $($app.name)")
}

Line '  </Applications>'
Line '</Package>'

$folder = Split-Path -Parent ([System.IO.Path]::GetFullPath($Out))
if ($folder -and -not (Test-Path $folder)) { New-Item -ItemType Directory -Force $folder | Out-Null }
[System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($Out), (($lines -join "`r`n") + "`r`n"), (New-Object System.Text.UTF8Encoding $false))
Write-Host "Wrote $Out for $Name $Version.0: $($listing.Count) apps, $(@($listing | Where-Object { $_.atSignIn }).Count) at sign-in."
