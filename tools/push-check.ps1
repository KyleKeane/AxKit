<#
.SYNOPSIS
  The checks a push must pass, run by the pre-push hook: the bundle's own tests, the test projects of what the pushed
  commits changed, and every page and script check of test.ps1. The push is refused when one fails.

.DESCRIPTION
  On 2026-11-24 two commits went in with a test of tests\AxKit.Tests failing, because only the test projects that
  seemed to matter had been run; and a page check found a stale page more than once after a push, on GitHub. A push
  now runs, before anything leaves the computer:
    - tests\AxKit.Tests always: it holds the source checks of the whole bundle (keys, menus, marks, layout, notes,
      privacy, the documents);
    - the test project of every app the pushed commits changed (src\Ax<Name>, src\Ax<Name>.Core or its tests), and
      every test project when a shared piece changed (src\AxKit.Core, src\AxKit.Forms, src\AxKit, Directory.Build.*);
    - the page checks, tools\upstream.ps1 -Check and tools\script-lint.ps1, as test.ps1 runs them;
    - first of all, where the priorities list is beside the repository: that every commit of the push that is no
      merge names its item on a line of its own (CLAUDE.md rule 9), with the passes of the commit-msg hook
      (tools\item-of-commit.ps1). The hook asks it at the commit, but only where it is installed and not past
      --no-verify; a commit without the line is named and the push refused.
  What was pushed is what lies between the remote's main and HEAD (git diff --name-only origin/main...HEAD); with no
  remote main, everything. Exit code 0 when all passed, 1 when one failed, with its lines. The run takes a minute or two,
  every project when shared code changed. The script stays ASCII (Windows PowerShell 5.1 reads it as ANSI).

  The checks are for the commits, so they run on a clean copy of HEAD (a worktree under the temporary folder, named
  by the checkout and this process, removed at the end) whenever the working tree holds something the commits do
  not: a change not committed, or a file never added under src, tests, tools or docs, which is named. Such a file
  would let the tests pass here and fail on GitHub, where it is not.

.EXAMPLE
  .\tools\push-check.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

# What the working tree holds that the commits do not: the changes to files git knows, and the files never added
# under src, tests, tools and docs (what git ignores, bin and obj, is not among them). A source or fixture file that
# was never added left the tree "clean" until 2026-11-27: the tests passed in place, with the file there, and the
# build on GitHub failed without it.
function Get-TreeChanges {
    $changes = @(& git -C $root status --porcelain --untracked-files=no)
    $never = @(& git -C $root status --porcelain --untracked-files=all -- src tests tools docs | Where-Object { "$_" -match '^\?\? ' } | ForEach-Object { "$_".Substring(3) })
    return [pscustomobject]@{ Changed = $changes; NeverAdded = $never; Count = $changes.Count + $never.Count }
}

# What is pushed is the commits, not the files on disk: with changes not committed (work in progress, or another
# session's) or files never added, the checks run on a clean copy of HEAD, a worktree in the temporary folder, and
# this script is run again there (2026-11-24: an edit made while a push was being checked broke the build the check
# was testing).
$tree = Get-TreeChanges
# In the clean copy itself (AXKIT_PUSH_COPY, set below) no second copy is made, whatever a check leaves there.
if ($tree.Count -gt 0 -and -not $env:AXKIT_PUSH_COPY) {
    # The copy is named by the checkout and by this process (tools\run-folder.ps1), so that two sessions pushing from
    # one checkout do not take each other's copy away; it is removed when the checks end. A copy left by a run that
    # was stopped is removed by the function, and git then forgets the worktree that was there.
    . (Join-Path $root 'tools\run-folder.ps1')
    $copy = Get-RunFolder $root 'axkit-push'
    & git -C $root worktree prune 2>&1 | Out-Null
    # Outside call, program, git: a worktree of HEAD in the temporary folder, for the checks alone.
    & git -C $root worktree add --detach $copy HEAD 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'Push check: the working tree has changes not committed or files never added, and no clean copy of HEAD could be made: commit or put them aside, then push again.'
        exit 1
    }

    if ($tree.Changed.Count -gt 0) { Write-Host "Push check: the working tree has $($tree.Changed.Count) change(s) not committed; the checks run on a clean copy of HEAD." }
    if ($tree.NeverAdded.Count -gt 0) {
        $more = if ($tree.NeverAdded.Count -gt 20) { ", and $($tree.NeverAdded.Count - 20) more" } else { '' }
        Write-Host "Push check: $($tree.NeverAdded.Count) file(s) under src, tests, tools or docs were never added to git, so the push does not hold them and the checks run on a clean copy of HEAD, without them: $((@($tree.NeverAdded | Select-Object -First 20)) -join ', ')$more. If the commits need one, add and commit it."
    }
    try {
        $env:AXKIT_PUSH_COPY = '1'
        # Outside call, program, powershell: this script, in the clean copy.
        & (Get-Process -Id $PID).Path -NoProfile -ExecutionPolicy Bypass -File (Join-Path $copy 'tools\push-check.ps1')
        $code = $LASTEXITCODE
    } finally {
        & git -C $root worktree remove --force $copy 2>&1 | Out-Null
    }

    exit $code
}

$all = @(Get-ChildItem -Path (Join-Path $root 'tests') -Filter '*.Tests.csproj' -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { $_.BaseName -replace '\.Tests$', '' })

& git -C $root rev-parse --verify -q origin/main 2>&1 | Out-Null
$remoteMain = $LASTEXITCODE -eq 0
$changed = if ($remoteMain) { @(& git -C $root diff --name-only origin/main...HEAD 2>$null) } else { @('src/AxKit.Core/') }

# Rule 9 once more, for what the commit-msg hook did not see: a commit made where the hook is not installed (another
# clone, a worktree) or past it (--no-verify). Every commit of the push that is no merge names its item on a line of
# its own; the passes are the hook's (tools\item-of-commit.ps1): what git writes itself (a revert, a fixup) and the
# release script's commit. A merge is known by its two parents here, not by its words. Asked only where the lists are
# beside the repository, as the hook asks; whether the words name an item is the hook's to say, at the commit.
. (Join-Path $root 'tools\siblings.ps1')
if ($remoteMain -and (Test-Path -LiteralPath (Join-Path (Get-BesideFolder 'axkit-management') 'priorities'))) {
    # One record per commit: a record separator, the short name, then the message as it is.
    $log = @(& git -C $root log origin/main..HEAD --no-merges '--format=%x1e%h%n%B' 2>$null) -join "`n"
    $unnamed = New-Object System.Collections.Generic.List[string]
    foreach ($record in $log.Split([char]0x1e)) {
        $lines = @($record -split "`n" | Where-Object { "$_".Trim() })
        if ($lines.Count -lt 2) { continue }
        $subject = "$($lines[1])".Trim()
        if ($subject -match '^(Revert "|fixup! |squash! |amend! )' -or $subject -match '^Release \d+\.\d+\.\d+$') { continue }
        if (@($lines | Where-Object { $_ -match '^\s*Item( done)?:\s*\S' }).Count -eq 0) { $unnamed.Add("$("$($lines[0])".Trim()) $subject") }
    }
    if ($unnamed.Count -gt 0) {
        Write-Host "Push check: $($unnamed.Count) commit(s) of this push name no item of the priorities list (CLAUDE.md rule 9):"
        $unnamed | ForEach-Object { Write-Host "  $_" }
        Write-Host "Give each its line, 'Item: <words of the item's title>', 'Item done: <words>' or 'Item: none, <the reason>': the newest commit with git commit --amend, an older one with a rebase. Nothing was pushed."
        exit 1
    }
}

$projects = New-Object System.Collections.Generic.List[string]
$projects.Add('AxKit')
$shared = $false
foreach ($file in $changed) {
    # What every test project compiles or reads (docs/axKit/repairs.json R-82): the shared code, the build's settings,
    # the shared test helpers and their props, the fixtures, the SDK's version. src/AxKit itself, the executable, is
    # referenced by no test project and runs AxKit.Tests alone, which always runs.
    if ($file -match '^src/AxKit\.(Core|Forms)/' -or $file -match '^Directory\.Build\.' -or $file -match '^global\.json$' -or $file -match '^tests/(Shared/|fixtures/|Directory\.Build\.)') { $shared = $true; continue }
    if ($file -match '^(src|tests)/(Ax[A-Za-z0-9]+?)(\.Core|\.Tests)?/') {
        $name = $Matches[2]
        if ($all -contains $name -and -not $projects.Contains($name)) { $projects.Add($name) }
    }
    if ($file -match '^tools/AxAutomate/' -and -not $projects.Contains('AxAutomate')) { $projects.Add('AxAutomate') }
}
if ($shared) {
    foreach ($name in $all) { if (-not $projects.Contains($name)) { $projects.Add($name) } }
}

Write-Host "Push check: the quick checks, then the test projects $($projects -join ', ')."
# The quick checks first, every one of them, so that a push learns of all its stale pages at once and in seconds,
# before the tests' minutes (2026-11-24: two pushes in a row were refused one page at a time, after the tests); the
# list of them is tools\pages.ps1 -Check's alone (docs/axKit/repairs.json R-81).
& (Join-Path $root 'tools\pages.ps1') -Check
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Push check: the quick checks above failed; nothing was pushed.'
    exit 1
}

try {
    foreach ($project in $projects) {
        $path = Join-Path $root "tests\$project.Tests\$project.Tests.csproj"
        if (-not (Test-Path -LiteralPath $path)) { continue }
        # Outside call, program, dotnet: runs one test project of this repository in Release, as test.ps1 does.
        $lines = @(& dotnet test $path --configuration Release --nologo -v q 2>&1 | ForEach-Object { "$_" })
        if ($LASTEXITCODE -ne 0) {
            # Files edited while the check ran (2026-11-24: a push refused on a half-made change that was never
            # committed): the failure says nothing of the commits, so the whole check runs again on a clean copy.
            if (-not $env:AXKIT_PUSH_COPY -and (Get-TreeChanges).Count -gt 0) {
                Write-Host "Push check: $project.Tests failed, but files changed or appeared while the check ran; it runs again on a clean copy of HEAD."
                & dotnet build-server shutdown 2>&1 | Out-Null
                # Outside call, program, powershell: this script again, which now finds the tree changed and uses a clean copy.
                & (Get-Process -Id $PID).Path -NoProfile -ExecutionPolicy Bypass -File $MyInvocation.MyCommand.Path
                exit $LASTEXITCODE
            }

            $lines | Where-Object { $_ -match 'error|Assert|Expected|Actual|Failed |\[FAIL\]' } | Select-Object -First 40 | ForEach-Object { Write-Host "  $_" }
            Write-Host "Push check: $project.Tests failed."
            exit 1
        }
        Write-Host "  $project.Tests passed"
    }
} finally {
    & dotnet build-server shutdown 2>&1 | Out-Null
}

Write-Host 'Push check: every check passed.'
exit 0
