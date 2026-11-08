<#
.SYNOPSIS
  Fetches the Windows SDK's packaging tools (makeappx.exe, makepri.exe, signtool.exe and their libraries) into
  tools\sdk\<version>\x64 for publish.ps1 -Msix on a computer without the Windows SDK, from Microsoft's own package
  Microsoft.Windows.SDK.BuildTools on nuget.org, and checks Microsoft's signature on every program and library before
  anything runs them (docs/axkit/store-distribution.md, docs/axkit/development-environment.md).

.DESCRIPTION
  The version is pinned here, as the workflows pin their actions; a newer version is a change of this file. Nothing is
  installed and no administrator rights are needed: the folder is the whole of it, git ignores tools\sdk, and deleting
  the folder removes the tools. A computer with the Windows SDK (the GitHub runners, a computer with Visual Studio)
  needs none of this: publish.ps1 looks in Windows Kits first.

  Prints the path of makeappx.exe as its one output line, so that a script can take it: $makeappx = & .\tools\get-sdk-tools.ps1

.EXAMPLE
  .\tools\get-sdk-tools.ps1
#>
[CmdletBinding()]
param(
    [string]$Version = '10.0.26100.9457'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
# $PSScriptRoot is not set while parameter defaults are evaluated in Windows PowerShell 5.1.
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$folder = Join-Path $root "tools\sdk\$Version"
$tools = Join-Path $folder 'x64'
$makeappx = Join-Path $tools 'makeappx.exe'
if (Test-Path $makeappx) {
    Write-Output $makeappx
    exit 0
}

# The package is a zip; only its bin\<SDK version>\x64 folder is taken, into a folder of its own.
$url = "https://api.nuget.org/v3-flatcontainer/microsoft.windows.sdk.buildtools/$Version/microsoft.windows.sdk.buildtools.$Version.nupkg"
$download = Join-Path $root "tools\sdk\download-$Version"
if (Test-Path $download) { Remove-Item $download -Recurse -Force }
New-Item -ItemType Directory -Force $download | Out-Null
$nupkg = Join-Path $download 'buildtools.zip'
Write-Host "Fetching Microsoft.Windows.SDK.BuildTools $Version from nuget.org..."
Invoke-WebRequest -Uri $url -OutFile $nupkg -UseBasicParsing

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($nupkg)
try {
    $entries = @($zip.Entries | Where-Object { $_.FullName -match '^bin/[^/]+/x64/.+' -and $_.Name })
    if ($entries.Count -eq 0) { throw "The package has no bin\<version>\x64 folder." }
    foreach ($entry in $entries) {
        $relative = ($entry.FullName -replace '^bin/[^/]+/x64/', '') -replace '/', '\'
        $target = Join-Path $tools $relative
        New-Item -ItemType Directory -Force (Split-Path -Parent $target) | Out-Null
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
    }
} finally {
    $zip.Dispose()
}
Remove-Item $download -Recurse -Force

# Microsoft's signature on every program and library, before anything runs them; one that fails removes the lot.
$checked = 0
foreach ($file in Get-ChildItem $tools -Include *.exe, *.dll -Recurse) {
    $signature = Get-AuthenticodeSignature $file.FullName
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
        Remove-Item $folder -Recurse -Force
        throw "$($file.Name) is not validly signed by Microsoft ($($signature.Status)); the tools were removed."
    }
    $checked++
}
if (-not (Test-Path $makeappx)) {
    Remove-Item $folder -Recurse -Force
    throw "The package had no makeappx.exe; the tools were removed."
}
Write-Host "Windows SDK tools $Version in $tools ($checked programs and libraries, Microsoft's signature checked on each)."
Write-Output $makeappx
