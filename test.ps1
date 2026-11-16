<#
.SYNOPSIS
  Runs every test project of axKit in the Release configuration, the configuration GitHub Actions builds.

.DESCRIPTION
  The test projects are found under tests\ (every *.Tests.csproj), so a new app's tests are included without an edit
  here. They run one by one, never through the solution: the solution would build src\AxKit into the bin\Debug that a
  running axKit holds open. Release is what the workflows build, and only Release treats warnings as errors
  (Directory.Build.props), so a test that passes in Debug can still stop a release; 3.3.0 was lost that way.
  release.ps1 and run.ps1 -Test call this script. Exit code 0 when every project passed, otherwise the first failure's.
  A run of every project ends with the checks build.yml also makes: the generated pages (outside calls, workflows,
  repairs, apps, cases, the key map), the outside programs' record (tools\upstream.ps1 -Check) and the scripts' rules
  (tools\script-lint.ps1). CI does two things this script does not: it builds and tests the whole solution in one go,
  and it builds the Store package (publish.ps1 -Msix).

  Before anything is built, the script refuses a clone whose git hooks are not installed, on the computer that has
  the management lists beside the repository: see the comment at the check.

.EXAMPLE
  .\test.ps1                 # every test project
  .\test.ps1 axFile          # one project: tests\AxFile.Tests
#>
[CmdletBinding()]
param(
    # One app's tests alone, by the folder name without ".Tests" (axFile, axKit, axClaude, ...).
    [Parameter(Position = 0)]
    [string]$Only = ''
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$projects = @(Get-ChildItem -Path (Join-Path $root 'tests') -Filter '*.Tests.csproj' -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Sort-Object Name)
if ($Only -ne '') {
    $projects = @($projects | Where-Object { $_.BaseName -eq "$Only.Tests" })
    if ($projects.Count -eq 0) {
        Write-Host "No test project tests\$Only.Tests."
        exit 1
    }
}

# The hooks are installed (root CLAUDE.md rules 7 and 9): the commit-msg hook marks each commit's item in the priorities
# list and the pre-push hook syncs the repositories beside this one, and both do nothing in a clone where the setting
# was never made. Only on a computer that has the lists (..\axkit-management): a contributor's clone and CI have no
# lists to keep. The setting may hold tools/hooks or the folder's full path, as release.ps1's check allows.
if (Test-Path -LiteralPath (Join-Path $root '..\axkit-management')) {
    $hooks = "$(git -C $root config --get core.hooksPath)".Trim()
    $hooksFolder = ''
    if ($hooks) {
        $hooksFolder = if ([IO.Path]::IsPathRooted($hooks)) { $hooks } else { Join-Path $root $hooks }
        $hooksFolder = [IO.Path]::GetFullPath($hooksFolder).TrimEnd('\')
    }
    if ($hooksFolder -ine [IO.Path]::GetFullPath((Join-Path $root 'tools\hooks')).TrimEnd('\')) {
        Write-Host 'The git hooks are not installed in this clone, so no commit marks its item and no push syncs the lists. Run once: git config core.hooksPath tools/hooks'
        exit 1
    }
}

try {
    # The runner writes a failing test to stderr; when this script's output is redirected (a log, a gate run), Windows
    # PowerShell would turn that line into a terminating error under Stop and end the run at the first failure. The
    # exit code below is the check.
    $ErrorActionPreference = 'Continue'
    foreach ($project in $projects) {
        Write-Host "Testing $($project.BaseName)..."
        dotnet test $project.FullName --configuration Release --nologo -v q
        if ($LASTEXITCODE -ne 0) {
            Write-Host "$($project.BaseName) failed."
            exit $LASTEXITCODE
        }
    }
} finally {
    # The build servers (the compiler server, any MSBuild node) end with the run instead of idling for minutes on
    # several hundred megabytes, whichever way the run ended (Directory.Build.rsp; 2026-11-08).
    dotnet build-server shutdown | Out-Null
}

Write-Host "All $($projects.Count) test projects passed."

if ($Only -eq '') {
    # The pages a script writes from the sources (docs/axKit/repairs.json R-30): build.yml checks them, and until
    # 2026-11-13 this script did not, so a stale page was found only by CI, six times in three days. Each check says
    # which script writes its page again.
    foreach ($page in 'outside-calls', 'workflows', 'repairs', 'apps-page', 'cases', 'key-map') {
        & (Join-Path $root "tools\$page.ps1") -Check
        if ($LASTEXITCODE -ne 0) {
            Write-Host "The page check tools\$page.ps1 -Check failed: run tools\$page.ps1 (without -Check) and commit the page it writes."
            exit $LASTEXITCODE
        }
    }

    # The record of the outside programs' verified versions (docs/axKit/automation.md, When an outside program
    # changes): only that it is well formed. The installed versions are compared by tools\upstream.ps1 alone, never
    # here, since CI's computer has other versions.
    & (Join-Path $root 'tools\upstream.ps1') -Check
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'The check tools\upstream.ps1 -Check failed: correct docs\axKit\upstream.json as its lines say.'
        exit $LASTEXITCODE
    }

    # The scripts' rules (ASCII, parsing, no local named like a parameter): each was a gotcha of the root CLAUDE.md
    # until 2026-11-16 and is now this check, whose lines say what to correct.
    & (Join-Path $root 'tools\script-lint.ps1') -Check
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'The check tools\script-lint.ps1 -Check failed: correct the scripts as its lines say.'
        exit $LASTEXITCODE
    }
}

exit 0
