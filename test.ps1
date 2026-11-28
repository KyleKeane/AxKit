<#
.SYNOPSIS
  Runs every test project of axKit in the Release configuration, the configuration GitHub Actions builds.

.DESCRIPTION
  The test projects are found under tests\ (every *.Tests.csproj), so a new app's tests are included without an edit
  here. They run in one dotnet test over a solution filter of the test projects alone, written for the run (no test
  project references src\AxKit, so the bin\Debug a running axKit holds open is never touched; one run was 17 per cent
  faster than one project at a time on the development computer, with the same free memory, 2026-11-25, repairs
  R-100); one app's tests run alone. Never through the whole solution: it would build src\AxKit into that bin\Debug. Release is what the workflows build, and only Release treats warnings as errors
  (Directory.Build.props), so a test that passes in Debug can still stop a release; 3.3.0 was lost that way.
  release.ps1 and run.ps1 -Test call this script. Exit code 0 when every project passed, otherwise the first failure's.
  A run of every project starts with the quick checks, before anything is built: tools\pages.ps1 -Check, the one
  list of them, which build.yml and the push check run as well (every generated page, the menu paths the documents
  name, the outside programs' record, the scripts' rules; the list is that script's and is not repeated here). With
  one app named they are left out. CI does two things this script does not: it builds and tests the whole solution
  in one go, src\AxKit and the tools among it, which nothing here compiles (release.ps1 therefore refuses while the
  newest build of origin/main on GitHub is not a success), and it builds the Store package (publish.ps1 -Msix).

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
# lists to keep. The setting may hold tools/hooks or the folder's full path, as release.ps1's check allows. In a
# worktree beside the checkout (a branch's own folder) the setting is the checkout's, with the full path of its
# hooks, and those are the hooks that run there: that passes too.
. (Join-Path $root 'tools\siblings.ps1')
if (Test-Path -LiteralPath (Get-BesideFolder 'axkit-management')) {
    if (-not (Test-HooksInstalled $root)) {
        Write-Host 'The git hooks are not installed in this clone, so no commit marks its item and no push syncs the lists. Run once: git config core.hooksPath tools/hooks'
        exit 1
    }
}

# The quick checks first, before the tests (docs/axKit/repairs.json R-81): every page a script writes, the menu paths the
# documents name, the record of the outside programs and the scripts' rules, all named at once by tools\pages.ps1 -Check.
# A stale page was found only by CI six times in three days until 2026-11-13, and then one at a time after the tests.
if ($Only -eq '') {
    & (Join-Path $root 'tools\pages.ps1') -Check
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

# The tests' own folders in the temporary folder (the review's third pass of 2026-11-27, L7: 1,027 folders of one test
# class were found left there, unnoticed for a week). Every test's TempFolder starts with axkit-test-; the others are
# the few tests that make a folder of their own, and axGIT's hooks folder for a run when its own cannot be emptied.
# Each name has this run's mark right after its beginning (AXKIT_TEST_RUN, tests\Shared\TempFolder.cs, the fourth
# pass, L4), and after the run none of this run's may be left: a leak shows at once, and another run beside this one
# (the push check on a clean copy) is never counted. TempFolderTests holds this table against the tests' sources.
$testFolderPrefixes = @('axkit-test-', 'axAutomate-tests-', 'axkit-failedlogs-test-', 'axhelp-tests-', 'axclaude-nowhere-', 'axmcp-tests-', 'axmedia-settings-', 'axkit-startup-', 'axTemplate-tests-', 'axterminal-tests-', 'axGIT-no-hooks-')
$testRun = 'r' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$env:AXKIT_TEST_RUN = $testRun
function Get-TestFolderCount {
    $temp = [IO.Path]::GetTempPath()
    @(Get-ChildItem -LiteralPath $temp -Directory -ErrorAction SilentlyContinue | Where-Object {
        $name = $_.Name
        @($testFolderPrefixes | Where-Object { $name.StartsWith($_ + $testRun + '-', [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
    }).Count
}

try {
    # The runner writes a failing test to stderr; when this script's output is redirected (a log, a gate run), Windows
    # PowerShell would turn that line into a terminating error under Stop and end the run at the first failure. The
    # exit code below is the check.
    $ErrorActionPreference = 'Continue'
    if ($Only -eq '') {
        # Every project in one run: a solution filter of the test projects, written to a temporary folder for this run
        # from the projects found, so that a new app's tests need no edit anywhere.
        $filter = Join-Path ([IO.Path]::GetTempPath()) ("axkit-tests-" + [Guid]::NewGuid().ToString('N') + ".slnf")
        $names = @($projects | ForEach-Object { $_.FullName.Substring($root.Length + 1) })
        $json = [ordered]@{ solution = [ordered]@{ path = (Join-Path $root 'AxKit.sln'); projects = $names } } | ConvertTo-Json -Depth 4
        [IO.File]::WriteAllText($filter, $json, (New-Object System.Text.UTF8Encoding($false)))
        Write-Host "Testing $($projects.Count) test projects in one run..."
        try {
            dotnet test $filter --configuration Release --nologo -v q
            $code = $LASTEXITCODE
        } finally {
            Remove-Item -LiteralPath $filter -ErrorAction SilentlyContinue
        }
        if ($code -ne 0) {
            Write-Host "A test project failed (the lines above name it)."
            exit $code
        }
    } else {
        foreach ($project in $projects) {
            Write-Host "Testing $($project.BaseName)..."
            dotnet test $project.FullName --configuration Release --nologo -v q
            if ($LASTEXITCODE -ne 0) {
                Write-Host "$($project.BaseName) failed."
                exit $LASTEXITCODE
            }
        }
    }
} finally {
    # The build servers (the compiler server, any MSBuild node) end with the run instead of idling for minutes on
    # several hundred megabytes, whichever way the run ended (Directory.Build.rsp; 2026-11-08).
    dotnet build-server shutdown | Out-Null
    Remove-Item Env:AXKIT_TEST_RUN -ErrorAction SilentlyContinue
}

$foldersLeft = Get-TestFolderCount
if ($foldersLeft -gt 0) {
    Write-Host "The tests passed, but left $foldersLeft folders of their own in $([IO.Path]::GetTempPath()) (names starting with one of $($testFolderPrefixes -join ', ') and then $testRun-): a test does not clean up after itself."
    exit 1
}

Write-Host "All $($projects.Count) test projects passed."

exit 0
