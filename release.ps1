<#
.SYNOPSIS
  Releases a new version of axKit, the bundle axClaude is one app of: sets the version, commits, tags and pushes. GitHub Actions then builds, tests,
  publishes the zip and creates the GitHub release that installed copies update themselves from.

.DESCRIPTION
  Before running it, add a "## <version>" section to CHANGELOG.md: its text becomes the release notes and
  is what the update notice in the app shows. Everything else must be committed. The script then
    1. fetches origin and stops if origin/main has commits that main here does not (skipped with -NoPush),
    2. checks the "## <version>" section: it is not empty, and every bullet starts with its app or apps ("- axFile: ",
       "- axDown, axJSON: ") or with "- For developers: ",
    3. writes docs/axKit/outside-calls.md from the sources (tools/outside-calls.ps1; a call without its explanation
       stops here, docs/axKit/SPEC.md B-17) and runs every test project in Release, as the workflows do (test.ps1),
    4. sets <Version> in src/AxKit/AxKit.csproj,
    5. commits CHANGELOG.md, the project file and docs/axKit/outside-calls.md as "Release <version>" and tags v<version>,
    6. pushes main, waits for its build to pass on GitHub (gh run watch), then pushes the tag: a tag never goes up on
       a commit whose build failed there; each push also syncs the reports clone and the management lists beside the
       repository through the pre-push hook (tools/sync-repos.ps1), which step 1 checks is installed,
    7. waits for the tag's release workflow, which leaves a draft release, then signs its zip with the release key
       on this machine and publishes it (tools/sign-release.ps1; docs/axKit/SPEC.md AX-3.4). The apps install only a
       signed zip, so a release is never published without this step.
  When it is done, the release is at https://github.com/KyleKeane/AxKit/releases and the apps offer it as an update.

  -Rehearse releases nothing. It makes the checks of steps 1 and 2 and asks git and GitHub every question the real
  run asks, through the same functions and so with the same command lines, about things that are already there: the
  build of main's newest commit, and the newest published release (tools/sign-release.ps1 -Rehearse). The parts that
  talk to git and GitHub otherwise run only during a real release, and 5.8.0 met two faults in them (2026-11-14).
  The rehearsal ends before the first statement that changes anything, in a few seconds; -WithTests adds test.ps1.
  Without a version, the changelog and version checks are made for the newest released section. It only fetches
  origin, as every session does. Exit code 0: every check passed and every question was answered sensibly. Files not
  yet committed are named as what the real run would refuse, and do not change the exit code: a rehearsal is run
  while the work is still open. Anything else the real run would refuse, and any question that fails, gives 1.

.EXAMPLE
  .\release.ps1 -Rehearse         # before a release: the checks and the questions to git and GitHub, nothing changed
  .\release.ps1 -Rehearse 1.0.1   # the same, with the changelog section and the tag of 1.0.1 checked
  .\release.ps1 1.0.1
  .\release.ps1 1.0.1 -NoPush     # commit and tag only; push later with: git push origin main; git push origin v1.0.1
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Version,

    [switch]$NoPush,

    [switch]$Rehearse,

    [switch]$WithTests
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\AxKit\AxKit.csproj'
$changelog = Join-Path $root 'CHANGELOG.md'

# ---- The checks and the questions, each one a function that the release and the rehearsal both call. A check
# ---- returns what is wrong, or nothing. None of them changes anything but origin's refs (the fetch).

function Get-VersionProblem([string]$ofVersion) {
    if ($ofVersion -notmatch '^\d+\.\d+\.\d+$') { return "The version must be three numbers, like 1.0.1 (got '$ofVersion')." }
}

function Get-BranchProblem {
    $branch = "$(git -C $root rev-parse --abbrev-ref HEAD)".Trim()
    if ($branch -ne 'main') { return "Release from main (you are on '$branch')." }
}

# Nothing is released over work someone else has pushed: main here must contain everything origin/main has.
function Get-BehindProblem {
    git -C $root fetch -q origin
    if ($LASTEXITCODE -ne 0) { return "Could not fetch origin. Nothing was changed." }
    git -C $root merge-base --is-ancestor origin/main main
    if ($LASTEXITCODE -ne 0) {
        return "origin/main has commits that main here does not. Bring them in first (git pull --ff-only, or a merge), then run this again."
    }
}

# Every push of this repository also syncs the repositories beside it, the reports clone and the management lists
# (tools/sync-repos.ps1), through the pre-push hook, so that a release never leaves them behind. The hook is a
# setting of the clone, made once (docs/axKit/development-environment.md section 4).
# The setting may hold the folder as written there, tools/hooks, or its full path: both name the same folder.
function Get-HooksProblem {
    $hooks = "$(git -C $root config --get core.hooksPath)".Trim()
    $hooksFolder = ''
    if ($hooks) {
        $hooksFolder = if ([IO.Path]::IsPathRooted($hooks)) { $hooks } else { Join-Path $root $hooks }
        $hooksFolder = [IO.Path]::GetFullPath($hooksFolder).TrimEnd('\')
    }
    if ($hooksFolder -ine [IO.Path]::GetFullPath((Join-Path $root 'tools\hooks')).TrimEnd('\')) {
        return "The pre-push hook is not installed in this clone. Run once: git config core.hooksPath tools/hooks (docs/axKit/development-environment.md section 4), then run this again."
    }
}

# The files the real run refuses to release over: everything uncommitted but the two it commits itself.
function Get-UncommittedFiles {
    $generated = @('CHANGELOG.md', 'docs/axKit/outside-calls.md')
    return @(git -C $root status --porcelain) | Where-Object { $_ -and ($generated -notcontains $_.Substring(3)) }
}

function Get-TagProblem([string]$ofTag) {
    if (git -C $root tag --list $ofTag) { return "The tag $ofTag already exists. Pick the next version." }
}

# The section is the release notes and the update notice: it says something, and every bullet names its app or apps,
# or is for developers, so that a reader of the notes knows where a change is (docs/axKit/longevity.md 3.11). The
# apps are the folders of docs/ that hold a SPEC.md (axkit among them, for the bundle; never a private or planning
# folder), matched without regard to case; the notes come from tools/release-notes.ps1, the script the release
# workflow runs, which fails on an empty section.
function Get-ChangelogProblem([string]$ofVersion) {
    if (-not (Select-String -Path $changelog -Pattern ('^## ' + [regex]::Escape($ofVersion) + '(\s|$)') -Quiet)) {
        return "CHANGELOG.md has no '## $ofVersion' section. Add one (its text becomes the release notes), then run this again."
    }
    try {
        $notes = & (Join-Path $root 'tools\release-notes.ps1') -Version $ofVersion -Path $changelog
    } catch {
        return $_.Exception.Message
    }
    $appNames = @(Get-ChildItem (Join-Path $root 'docs') -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'SPEC.md') } | ForEach-Object { $_.Name }) -join '|'
    $prefix = "^- (($appNames)(, ($appNames))*|For developers): "
    $unnamed = @($notes -split "`n" | Where-Object { $_ -match '^- ' -and $_ -notmatch $prefix })
    if ($unnamed.Count -gt 0) {
        return (@("Every bullet of the '## $ofVersion' section starts with its app or apps ('- axFile: ', '- axDown, axJSON: ') or with '- For developers: '. These do not:") + ($unnamed | ForEach-Object { "  $_" })) -join "`n"
    }
}

# The project file's text with the version set, or nothing when the file has no <Version> element.
function Get-ProjectWithVersion([string]$ofVersion) {
    $xml = Get-Content $project -Raw
    $updated = $xml -replace '<Version>[^<]*</Version>', "<Version>$ofVersion</Version>"
    if ($updated -eq $xml -and $xml -notmatch "<Version>$([regex]::Escape($ofVersion))</Version>") { return $null }
    return $updated
}

# One question to GitHub: gh with these words, each word one argument. The answer is what gh wrote, trimmed; the
# command as text and gh's exit code stay in $script:asked and $script:askedExit, for the rehearsal's report. What gh
# writes as an error goes to the console as it comes.
# Every --jq filter is one plain word, with no space and no double quote in it: a filter with a quoted space reaches
# gh as two arguments from Windows PowerShell 5.1, and gh refused it at every try of the 5.8.0 release, which waited
# without end (2026-11-14). ReleaseScriptTests holds the scripts to it.
function Get-GitHubAnswer([string[]]$words) {
    $script:asked = 'gh ' + ($words -join ' ')
    $lines = @(gh @words)
    $script:askedExit = $LASTEXITCODE
    return ($lines -join "`n").Trim()
}

# The run of build.yml for a commit: its number, or nothing (or 'null') while GitHub has none yet.
function Find-BuildRun([string]$ofCommit) {
    return Get-GitHubAnswer @('run', 'list', '--workflow', 'build.yml', '--commit', $ofCommit, '--limit', '1', '--json', 'databaseId', '--jq', '.[0].databaseId')
}

# Two plain questions, the status and then the conclusion, where one filter joining them was the fault of 5.8.0.
function Get-RunStatus([string]$ofRun) {
    return Get-GitHubAnswer @('run', 'view', $ofRun, '--json', 'status', '--jq', '.status')
}

function Get-RunConclusion([string]$ofRun) {
    return Get-GitHubAnswer @('run', 'view', $ofRun, '--json', 'conclusion', '--jq', '.conclusion')
}

# ---- The rehearsal: the checks and the questions above, and nothing else. It always ends in an exit, above the
# ---- first statement of this script that changes anything; ReleaseScriptTests reads the script for that.
if ($Rehearse) {
    $failed = 0        # what gives exit code 1: a refusal other than uncommitted files, or a question that failed
    $checked = 0
    function Write-Check([string]$what, $problem) {
        $script:checked++
        if ($problem) {
            $script:failed++
            Write-Host "The real run would stop here: ${what}:"
            Write-Host "  $problem"
        } else {
            Write-Host "Passed: $what."
        }
    }
    function Write-Answer([string]$what, [string]$answer, [bool]$sensible) {
        $script:checked++
        if ($script:askedExit -eq 0 -and $sensible) {
            Write-Host "Answered: ${what}: $answer"
            Write-Host "  $script:asked"
        } else {
            $script:failed++
            Write-Host "FAILED: ${what}."
            Write-Host "  Command: $script:asked"
            Write-Host "  Exit code: $script:askedExit"
            Write-Host "  Output: '$answer'"
        }
    }

    try {
        Write-Host "Rehearsal of a release: checks and questions only."
        $rehearsed = $Version
        if (-not $rehearsed) {
            $heading = Select-String -Path $changelog -Pattern '^## (\d+\.\d+\.\d+)(\s|$)' | Select-Object -First 1
            if ($heading) { $rehearsed = $heading.Matches[0].Groups[1].Value }
            Write-Host "No version given: the changelog and version checks are made for the newest released section, $rehearsed."
        }
        Write-Check "the version's format ($rehearsed)" (Get-VersionProblem $rehearsed)
        Write-Check 'the branch is main' (Get-BranchProblem)
        Write-Check 'main has everything origin/main has (origin fetched)' (Get-BehindProblem)
        Write-Check 'the hooks setting of the clone' (Get-HooksProblem)

        $uncommitted = @(Get-UncommittedFiles)
        $checked++
        if ($uncommitted.Count -gt 0) {
            Write-Host "The real run would stop here: $($uncommitted.Count) uncommitted file(s). Commit or stash them before the release; the rehearsal goes on:"
            $uncommitted | ForEach-Object { Write-Host "  $_" }
        } else {
            Write-Host 'Passed: nothing uncommitted but what the release commits.'
        }

        if ($Version) {
            Write-Check "the tag v$rehearsed is free" (Get-TagProblem "v$rehearsed")
        } else {
            Write-Host "Not checked: that the tag is free (v$rehearsed is released; give the next version to check its tag)."
        }
        Write-Check "the changelog section of $rehearsed" (Get-ChangelogProblem $rehearsed)
        $withVersion = Get-ProjectWithVersion $rehearsed
        Write-Check 'the project file has its <Version> element' $(if (-not $withVersion) { "No <Version> element was found in $project." })

        if ($WithTests) {
            Write-Host 'Running the tests...'
            & (Join-Path $root 'test.ps1')
            Write-Check 'the tests and the generated pages (test.ps1)' $(if ($LASTEXITCODE -ne 0) { "test.ps1 ended with exit code $LASTEXITCODE." })
        } else {
            Write-Host 'Not run: the tests and the generated pages (add -WithTests).'
        }

        if (Get-Command gh -ErrorAction SilentlyContinue) {
            # The build of main's newest commit; of origin/main's when the newest one here has not been pushed.
            $sha = "$(git -C $root rev-parse HEAD)".Trim()
            $id = Find-BuildRun $sha
            if ($script:askedExit -eq 0 -and (-not $id -or $id -eq 'null')) {
                $pushed = "$(git -C $root rev-parse origin/main)".Trim()
                if ($pushed -ne $sha) {
                    Write-Host "GitHub has no build of $sha (not pushed yet): asking about origin/main's commit."
                    $sha = $pushed
                    $id = Find-BuildRun $sha
                }
            }
            Write-Answer "the build run of main's commit $sha" $id ($id -match '^\d+$')
            if ($id -match '^\d+$') {
                $status = Get-RunStatus $id
                Write-Answer "the status of run $id" $status ($status -match '^[a-z_]+$')
                if ($status -eq 'completed') {
                    $conclusion = Get-RunConclusion $id
                    Write-Answer "the conclusion of run $id" $conclusion ($conclusion -match '^[a-z_]+$')
                    if ($conclusion -match '^[a-z_]+$' -and $conclusion -ne 'success') {
                        Write-Host "  Note: with this conclusion on the release's commit, the real run would not push the tag."
                    }
                } else {
                    Write-Host "  The run has not ended, so the conclusion was not asked, as in the real run's wait."
                }
            }

            # What tools/sign-release.ps1 asks, about the newest published release.
            & (Join-Path $root 'tools\sign-release.ps1') -Rehearse
            Write-Check 'the questions of tools/sign-release.ps1' $(if ($LASTEXITCODE -ne 0) { "sign-release.ps1 -Rehearse ended with exit code $LASTEXITCODE; its lines are above." })
        } else {
            $failed++
            Write-Host 'FAILED: gh is not installed, so no question to GitHub could be asked (the real run would push the tag without waiting for the build, and sign nothing).'
        }
    } catch {
        $failed++
        Write-Host "FAILED: the rehearsal itself stopped: $($_.Exception.Message)"
        Write-Host "  $($_.InvocationInfo.PositionMessage)"
    }

    Write-Host "Checked: $checked checks and questions, $failed of them failed."
    Write-Host 'Not rehearsed: the commit, the tag, the pushes, the download and the signing of the zip, the upload, the publishing.'
    Write-Host 'Nothing was changed: no commit, no tag, no push, nothing signed, uploaded or published.'
    if ($failed -gt 0) { exit 1 }
    exit 0
}
# ---- End of the rehearsal. Everything below is the release.

function Stop-OnProblem($problem) {
    if ($problem) {
        Write-Host $problem
        exit 1
    }
}

if (-not $Version) {
    Write-Host "Give the version, like .\release.ps1 1.0.1 (or -Rehearse to rehearse a release without making one)."
    exit 1
}
$tag = "v$Version"

Stop-OnProblem (Get-VersionProblem $Version)
Stop-OnProblem (Get-BranchProblem)

# The check belongs to the push, so -NoPush (commit and tag offline, push later) does not need origin to be reachable.
if (-not $NoPush) {
    Stop-OnProblem (Get-BehindProblem)
    Stop-OnProblem (Get-HooksProblem)
}

$dirty = @(Get-UncommittedFiles)
if ($dirty.Count -gt 0) {
    Write-Host "Commit or stash these first; only CHANGELOG.md and docs/axKit/outside-calls.md may be uncommitted:"
    $dirty | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Stop-OnProblem (Get-TagProblem $tag)
Stop-OnProblem (Get-ChangelogProblem $Version)

# The page of everything axKit reaches outside its process, written from the sources and committed with the release
# (docs/axKit/SPEC.md B-17): a call without its explanation stops the release here.
& (Join-Path $root 'tools\outside-calls.ps1')
if ($LASTEXITCODE -ne 0) {
    Write-Host "Every outside call needs its comment first. Nothing was changed."
    exit $LASTEXITCODE
}

# The tests as GitHub Actions runs them: every test project, in Release (test.ps1).
Write-Host "Running the tests..."
& (Join-Path $root 'test.ps1')
if ($LASTEXITCODE -ne 0) {
    Write-Host "The tests failed. Nothing was changed."
    exit $LASTEXITCODE
}

$updated = Get-ProjectWithVersion $Version
if (-not $updated) {
    Write-Host "No <Version> element was found in $project."
    exit 1
}
# Not Set-Content -Encoding utf8: in Windows PowerShell 5.1 that writes a byte order mark.
[IO.File]::WriteAllText($project, $updated, (New-Object System.Text.UTF8Encoding($false)))

git -C $root add -- CHANGELOG.md src/AxKit/AxKit.csproj docs/axKit/outside-calls.md
git -C $root commit -q -m "Release $Version"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
git -C $root tag -a $tag -m "axKit $Version"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Committed and tagged $tag."

if ($NoPush) {
    Write-Host "Not pushed. When ready: git push origin main; git push origin $tag"
    exit 0
}

# main goes first and the tag only once main's build has passed on GitHub (build.yml, followed with gh): the tag
# starts the release build, and a tag on a commit whose build fails on the runner (an image or an action moved under
# the workflow) would be a dangling tag to delete by hand. Without gh the tag follows main at once.
# -q: git's progress goes to stderr, which Windows PowerShell turns into an error under Stop when the script's output is
# redirected (the 3.5.0 release stopped there after a successful push); a failure still sets the exit code.
git -C $root push -q origin main
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if (Get-Command gh -ErrorAction SilentlyContinue) {
    Write-Host "Pushed main. Waiting for its build before pushing the tag..."
    $sha = (git -C $root rev-parse HEAD).Trim()
    $run = $null
    for ($try = 0; -not $run -and $try -lt 24; $try++) {
        Start-Sleep -Seconds 5
        $id = Find-BuildRun $sha
        if ($id -and $id -ne 'null') { $run = $id }
    }
    if (-not $run) {
        Write-Host "No build of main's $sha appeared on GitHub within two minutes. The tag was not pushed; once the build has passed: git push origin $tag"
        exit 1
    }
    # Asked every 20 seconds, 40 minutes at most (docs/axKit/repairs.json R-38): gh run watch has no limit of its own,
    # and a build that hangs on GitHub would hold the release for ever.
    $conclusion = $null
    for ($try = 0; -not $conclusion -and $try -lt 120; $try++) {
        Start-Sleep -Seconds 20
        $status = Get-RunStatus $run
        if ($status -eq 'completed') { $conclusion = Get-RunConclusion $run }
    }
    if (-not $conclusion) {
        Write-Host "The build of main (run $run) had not ended after 40 minutes. The tag $tag was not pushed; look at it with: gh run view $run. Once it has passed: git push origin $tag"
        exit 1
    }
    if ($conclusion -ne 'success') {
        Write-Host "The build of main failed; the tag $tag was not pushed. Fix main, delete the tag here (git tag -d $tag) and release the next patch version."
        exit 1
    }
} else {
    Write-Host "Pushed main. gh is not installed, so the tag follows without waiting for main's build."
}

git -C $root push -q origin $tag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Pushed the tag. GitHub Actions is building the draft release: https://github.com/KyleKeane/AxKit/actions"
# AX-13.6: the release's package reaches the Microsoft Store by an upload in Partner Center, by hand.
Write-Host "Once published, the release's AxKit-<version>.msix is uploaded to the Microsoft Store in Partner Center by hand (docs/axKit/store-setup.md section 5)."

# AX-3.4 (security audit S-27): the draft is signed here, with the key that never leaves this machine, and only then
# published; the apps install nothing unsigned.
if (Get-Command gh -ErrorAction SilentlyContinue) {
    & (Join-Path $root 'tools\sign-release.ps1') $Version
    exit $LASTEXITCODE
}

Write-Host "gh is not installed: once the workflow is done, sign and publish with: .\tools\sign-release.ps1 $Version"
