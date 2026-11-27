<#
.SYNOPSIS
Writes docs\axKit\over-the-bounds.md and the baseline docs\axKit\goal-counts.json, the bounds of the workflow graph;
or checks them (-Check); -Write takes a goal that grew into the baseline, at the maintainer's word.

.DESCRIPTION
The bounds are docs\axKit\workflow-graph.md WG-3 (SPEC.md G7): a goal takes four actions at most, ideally three, and
every waypoint of a window is two actions or fewer from any node of it. Every goal of the scripts of tests\scripts is
counted as WG-3.3 says (the app's own key free, the steps that prepare or look not counted, the example's text a goal
types before its first command not counted), and the waypoint bound is found by breadth-first search over
docs\axKit\workflow-graph.json. The page lists by app the goals over four with their count and their steps, those at
four, and the nodes over the waypoint bound. The counting is C# (AxAutomate.Core's GoalCounts and GraphBounds), run by
the command line of the automated checks, "AxAutomate --bounds" (tools\AxAutomate\BoundsRun.cs), which plays nothing
and needs no window.

The baseline holds each goal's count: a goal may get shorter, never longer (WG-3.4, the maintainer's decision of
2026-11-26). Without a switch both files are written, but a count that grew keeps the baseline's count and a new goal
over four stays out of it, so that -Check goes on naming them. -Write takes every count in as it is: run it only at the
maintainer's word, and the commit shows the change. -Check writes nothing and exits with 1 when the page or the
baseline is not what the scripts give, or a goal is longer than the baseline holds (named, one line each); then run
the script and commit what it writes. tools\pages.ps1 runs it with the other pages, and test.ps1 the check. The script
stays ASCII.
#>
[CmdletBinding()]
param([switch]$Check, [switch]$Write)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
if ($Check -and $Write) {
    Write-Host 'Give -Check or -Write, not both.'
    exit 2
}

$runnerProject = Join-Path $root 'tools\AxAutomate\AxAutomate.Runner.csproj'
# The runner's folder is the one tools\workflows.ps1 builds into, by the checkout's name, so that the second build of
# a run of tools\pages.ps1 finds it current (workflows.ps1 says why the runner is not run in place).
$runnerFolder = Join-Path ([IO.Path]::GetTempPath()) ('axkit-reference-runner-' + (Split-Path -Leaf $root))
$runnerArguments = @('--bounds', '--repository', $root)
if ($Check) { $runnerArguments += '--check' }
if ($Write) { $runnerArguments += '--write' }

# A line the runner writes to stderr must not end this script under Stop (Windows PowerShell 5.1); the exit code is
# the result.
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
    dotnet build-server shutdown | Out-Null
}

exit $exitCode
