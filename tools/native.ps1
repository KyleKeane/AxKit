<#
.SYNOPSIS
  Runs a program (git, dotnet, gh) and ends with the program's own exit code, its error output given as plain lines
  among the others and redacted.

.DESCRIPTION
  The pitfall this script removes (the review of the sessions of 2026-11-25): in Windows PowerShell 5.1, a program
  that writes to its error output under 2>&1 has each of those lines turned into an error record, and the tool
  reported exit 1 although the program had succeeded ("main -> main", "Build succeeded"), 15 times; each was checked
  again by hand. Here the program's error output is read as text, and the last line printed is "exit <code>", the
  program's own. The output goes through tools\redact.ps1. The script stays ASCII.

.EXAMPLE
  .\tools\native.ps1 git push
  .\tools\native.ps1 dotnet build src\AxTask.Core -c Release -o C:\scratch\task
#>
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Program,
    [Parameter(Position = 1, ValueFromRemainingArguments = $true)][string[]]$Arguments = @()
)

$ErrorActionPreference = 'Continue'
$redact = Join-Path $PSScriptRoot 'redact.ps1'
# The error output as text: each error record printed as its message, never as a red record with a position.
& $Program @Arguments 2>&1 | ForEach-Object {
    if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { "$_" }
} | & $redact
$code = $LASTEXITCODE
"exit $code"
exit $code
