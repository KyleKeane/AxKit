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
# The runner is built into a folder of its own and started from there. Built and run in place (dotnet run), it could
# not be built while a window run started the same way was alive in this checkout: that run's AxAutomate.exe holds
# tools\AxAutomate\bin, and the build failed with MSB3027 after ten tries (found 2026-11-15, when several runs shared
# a night). The folder is under the temporary folder, by the checkout's name, so two checkouts do not share it.
$runnerFolder = Join-Path ([IO.Path]::GetTempPath()) ('axkit-reference-runner-' + (Split-Path -Leaf $root))
$runnerArguments = @('--reference', '--repository', $root)
if ($Check) { $runnerArguments += '--check' }

# A line the runner writes to stderr must not end this script under Stop when its output is redirected (Windows
# PowerShell 5.1); the exit code is the result.
$ErrorActionPreference = 'Continue'
$exitCode = 2
try {
    & dotnet build $runnerProject -c Release -v q -nologo -o $runnerFolder | Out-Null
    if ($LASTEXITCODE -eq 0) {
        & (Join-Path $runnerFolder 'AxAutomate.exe') @runnerArguments
        $exitCode = $LASTEXITCODE
    } else {
        Write-Host "The runner could not be built (dotnet build $runnerProject -c Release -o $runnerFolder)."
    }
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
