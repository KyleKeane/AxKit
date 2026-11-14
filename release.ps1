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

.EXAMPLE
  .\release.ps1 1.0.1
  .\release.ps1 1.0.1 -NoPush     # commit and tag only; push later with: git push origin main; git push origin v1.0.1
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Version,

    [switch]$NoPush
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\AxKit\AxKit.csproj'
$changelog = Join-Path $root 'CHANGELOG.md'
$tag = "v$Version"

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "The version must be three numbers, like 1.0.1 (got '$Version')."
    exit 1
}

$branch = (git -C $root rev-parse --abbrev-ref HEAD).Trim()
if ($branch -ne 'main') {
    Write-Host "Release from main (you are on '$branch')."
    exit 1
}

# Nothing is released over work someone else has pushed: main here must contain everything origin/main has. The
# check belongs to the push, so -NoPush (commit and tag offline, push later) does not need origin to be reachable.
if (-not $NoPush) {
    git -C $root fetch -q origin
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Could not fetch origin. Nothing was changed."
        exit $LASTEXITCODE
    }

    git -C $root merge-base --is-ancestor origin/main main
    if ($LASTEXITCODE -ne 0) {
        Write-Host "origin/main has commits that main here does not. Bring them in first (git pull --ff-only, or a merge), then run this again."
        exit 1
    }

    # Every push of this repository also syncs the repositories beside it, the reports clone and the management lists
    # (tools/sync-repos.ps1), through the pre-push hook, so that a release never leaves them behind. The hook is a
    # setting of the clone, made once (docs/axKit/development-environment.md section 4).
    # The setting may hold the folder as written there, tools/hooks, or its full path: both name the same folder.
    $hooks = "$(git -C $root config --get core.hooksPath)".Trim()
    $hooksFolder = ''
    if ($hooks) {
        $hooksFolder = if ([IO.Path]::IsPathRooted($hooks)) { $hooks } else { Join-Path $root $hooks }
        $hooksFolder = [IO.Path]::GetFullPath($hooksFolder).TrimEnd('\')
    }
    if ($hooksFolder -ine [IO.Path]::GetFullPath((Join-Path $root 'tools\hooks')).TrimEnd('\')) {
        Write-Host "The pre-push hook is not installed in this clone. Run once: git config core.hooksPath tools/hooks (docs/axKit/development-environment.md section 4), then run this again."
        exit 1
    }
}

$generated = @('CHANGELOG.md', 'docs/axKit/outside-calls.md')
$dirty = @(git -C $root status --porcelain) | Where-Object { $_ -and ($generated -notcontains $_.Substring(3)) }
if ($dirty.Count -gt 0) {
    Write-Host "Commit or stash these first; only CHANGELOG.md and docs/axKit/outside-calls.md may be uncommitted:"
    $dirty | ForEach-Object { Write-Host "  $_" }
    exit 1
}

if (git -C $root tag --list $tag) {
    Write-Host "The tag $tag already exists. Pick the next version."
    exit 1
}

if (-not (Select-String -Path $changelog -Pattern ('^## ' + [regex]::Escape($Version) + '(\s|$)') -Quiet)) {
    Write-Host "CHANGELOG.md has no '## $Version' section. Add one (its text becomes the release notes), then run this again."
    exit 1
}

# The section is the release notes and the update notice: it says something, and every bullet names its app or apps,
# or is for developers, so that a reader of the notes knows where a change is (docs/axKit/longevity.md 3.11). The
# apps are the folders of docs/ that hold a SPEC.md (axkit among them, for the bundle; never a private or planning
# folder), matched without regard to case; the notes come from tools/release-notes.ps1, the script the release
# workflow runs, which fails on an empty section.
try {
    $notes = & (Join-Path $root 'tools\release-notes.ps1') -Version $Version -Path $changelog
} catch {
    Write-Host $_.Exception.Message
    exit 1
}
$apps = @(Get-ChildItem (Join-Path $root 'docs') -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'SPEC.md') } | ForEach-Object { $_.Name }) -join '|'
$prefix = "^- (($apps)(, ($apps))*|For developers): "
$unnamed = @($notes -split "`n" | Where-Object { $_ -match '^- ' -and $_ -notmatch $prefix })
if ($unnamed.Count -gt 0) {
    Write-Host "Every bullet of the '## $Version' section starts with its app or apps ('- axFile: ', '- axDown, axJSON: ') or with '- For developers: '. These do not:"
    $unnamed | ForEach-Object { Write-Host "  $_" }
    exit 1
}

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

$xml = Get-Content $project -Raw
$updated = $xml -replace '<Version>[^<]*</Version>', "<Version>$Version</Version>"
if ($updated -eq $xml -and $xml -notmatch "<Version>$([regex]::Escape($Version))</Version>") {
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
        $id = gh run list --workflow build.yml --commit $sha --limit 1 --json databaseId --jq '.[0].databaseId'
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
        # Two plain questions: a filter with a quoted space in it reaches gh as two arguments from Windows PowerShell 5.1,
        # and gh refused it at every try of the 5.8.0 release, which waited without end (2026-11-14).
        $status = "$(gh run view $run --json status --jq .status)".Trim()
        if ($status -eq 'completed') { $conclusion = "$(gh run view $run --json conclusion --jq .conclusion)".Trim() }
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
