<#
.SYNOPSIS
Writes the workflow pages from the scripts of the automated checks and the command inventory, or checks that they are
current (-Check).

.DESCRIPTION
The pages for people (docs/axKit/user-workflows.md WF-7): docs\axKit\workflows.md, the index, by app each workflow's
label and under it its goals; docs\axKit\workflows\<app>.md, one page per group, each goal's steps with their keys
and what is heard; docs\axKit\window-commands.md, every command of every window. They come from two inputs and
nothing else: the scripts of tests\scripts and tests\nvda-scripts, and the command inventory
docs\axKit\window-commands.json. The generator is C# (AxAutomate.Core's Reference), run by the command line of the
automated checks, "AxAutomate --reference" (tools\AxAutomate\ReferenceRun.cs), which plays nothing and needs no
window. Every script starts with its label, "# Workflow: <a task in the user's words>" (docs/axAutomate/SPEC.md
AU-5.2a): the generator refuses a script without one and names it.

-Check writes nothing and exits with 1 when a page on disk is not what the inputs give (line ends aside), or is
missing, or is left over; then run the script without -Check and commit the pages with the change that made them
differ (WF-11.4). test.ps1 and build.yml run the check.
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
# The repository from this script's own path: test.ps1 starts it by its full path from whatever folder it was in.
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
$runnerProject = Join-Path $root 'tools\AxAutomate\AxAutomate.Runner.csproj'
# No --nologo: dotnet run does not take it and would hand it to the program as its first word.
$runArguments = @('run', '--project', $runnerProject, '-c', 'Release', '-v', 'q', '--', '--reference', '--repository', $root)
if ($Check) { $runArguments += '--check' }

# A line the runner writes to stderr must not end this script under Stop when its output is redirected (Windows
# PowerShell 5.1); the exit code is the result.
$ErrorActionPreference = 'Continue'
$exitCode = 2
try {
    & dotnet @runArguments
    $exitCode = $LASTEXITCODE
} finally {
    # The build servers end with the run instead of idling on several hundred megabytes (Directory.Build.rsp).
    dotnet build-server shutdown | Out-Null
}

# The key map is written from the same inventory, so it is written with these pages: whoever runs this script after
# an inventory change (the runner's own message says to) leaves no page behind. On 2026-11-16 five new rows of
# axProcess left the key map stale until test.ps1 named it. A check (-Check) writes nothing; test.ps1 checks the key
# map itself.
if (-not $Check -and $exitCode -eq 0) {
    & (Join-Path $root 'tools\key-map.ps1')
    if ($LASTEXITCODE -ne 0) { $exitCode = $LASTEXITCODE }
    # The families page counts the inventory's commands by menu, so it follows too.
    & (Join-Path $root 'tools\families.ps1')
    if ($LASTEXITCODE -ne 0) { $exitCode = $LASTEXITCODE }
}

exit $exitCode
