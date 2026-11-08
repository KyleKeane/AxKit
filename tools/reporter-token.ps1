<#
.SYNOPSIS
  Writes the reporter token's file for a release build (docs/axkit/reports.md RP-6.2).

.DESCRIPTION
  src\AxKit.Core\Reports\reporter.token holds the reporter account's token mixed with a fixed key and written as
  base64, the form ReporterToken.Decode in AxKit.Core reads; AxKit.Core.csproj embeds the file when it exists.
  publish.ps1 calls this with the secret AXKIT_REPORTER_TOKEN and removes the file after the build; the file is
  ignored by git and must never be committed. -Remove deletes it.

.EXAMPLE
  .\tools\reporter-token.ps1 -Token $env:AXKIT_REPORTER_TOKEN
  .\tools\reporter-token.ps1 -Remove
#>
param(
    [string]$Token,
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$file = Join-Path $root 'src\AxKit.Core\Reports\reporter.token'

if ($Remove) {
    if (Test-Path $file) { Remove-Item $file -Force; Write-Host "Removed $file." }
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    Write-Host "No token given: nothing written. Pass -Token, or -Remove to delete the file."
    exit 1
}

# The same mixing as ReporterToken.Mix: each byte of the token against the key's bytes, round and round.
$key = [System.Text.Encoding]::ASCII.GetBytes('axKit reports: a key that hides the token from a glance, not from a reader')
$bytes = [System.Text.Encoding]::UTF8.GetBytes($Token.Trim())
$mixed = New-Object byte[] $bytes.Length
for ($i = 0; $i -lt $bytes.Length; $i++) { $mixed[$i] = $bytes[$i] -bxor $key[$i % $key.Length] }
[System.IO.File]::WriteAllText($file, [Convert]::ToBase64String($mixed), [System.Text.Encoding]::ASCII)
Write-Host "Wrote the reporter token's file ($($bytes.Length) bytes mixed)."
