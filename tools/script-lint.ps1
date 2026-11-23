<#
.SYNOPSIS
Checks every PowerShell script of the repository for the four faults that have cost a release, a file or a build: a character
that is not ASCII, a script that does not parse, a local variable named like one of the script's parameters, and a
culture's sort in a script that writes a page.

.DESCRIPTION
The scripts are every *.ps1 under the repository, outside bin, obj, publish, tools\sdk, .git and .claude\worktrees.
Each of them must hold four rules, and each rule is here because the fault happened:

  1. ASCII only, and so no byte order mark. Windows PowerShell 5.1 reads a script without a mark as ANSI, so one
     character outside ASCII becomes several: an ellipsis made the Store's release notes three characters too long
     (2026-11-09). Write "..." and [char]0x2026, never the character.
  2. It parses: PowerShell's own parser reports no error.
  3. No assignment to a variable whose name is one of the script's parameters in other letters' case. Variable names
     are case-insensitive and a parameter is a variable, so a local $msix is the -Msix switch and a local $apps is the
     [string]$Apps parameter, which turns the array assigned to it into one string (publish.ps1 and
     tools\make-manifest.ps1, 2026-11-08). An assignment to the parameter in its own spelling is meant and passes; so
     does one inside a function, which has its own variables. A foreach loop's variable counts as an assignment.
  4. A script that takes -Check (the scripts that write a page of docs\axKit, and the checks) holds no Sort-Object.
     Sort-Object orders text by the culture's rules, and Windows PowerShell 5.1 and PowerShell 7 do not agree on a
     hyphen and a full stop: the apps page written on the development computer was stale in every build on GitHub
     for an afternoon (2026-11-17). Such a script sorts with Sort-Ordinal of tools\ordinal.ps1.

test.ps1 and build.yml run it; it writes nothing. Exit code 0 when every script holds the rules, otherwise 1 with one
line per fault: the file, the line, what is wrong. -Check is accepted so that it is called like the page checks; the
script does the same without it.

.EXAMPLE
  .\tools\script-lint.ps1 -Check
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
. (Join-Path $root 'tools\ordinal.ps1')

$scripts = @(Sort-Ordinal @(Get-ChildItem -LiteralPath $root -Filter '*.ps1' -Recurse -File -Force |
    Where-Object { $_.FullName.Substring($root.Length) -notmatch '\\(bin|obj|publish|\.git|tools\\sdk|\.claude\\worktrees)\\' }) { $_.FullName })

# Whether an assignment stands inside a function: there the variable is the function's own.
function Test-InFunction($ast) {
    for ($parent = $ast.Parent; $parent; $parent = $parent.Parent) {
        if ($parent -is [System.Management.Automation.Language.FunctionDefinitionAst]) { return $true }
    }
    return $false
}

$faults = New-Object System.Collections.Generic.List[string]
foreach ($script in $scripts) {
    $name = $script.FullName.Substring($root.Length + 1)

    # Rule 1: ASCII only.
    $bytes = [IO.File]::ReadAllBytes($script.FullName)
    $line = 1
    foreach ($byte in $bytes) {
        if ($byte -eq 10) { $line++ }
        if ($byte -gt 127) {
            $faults.Add("${name}:${line}: a character that is not ASCII (write it as [char]0x..., or in ASCII)")
            break
        }
    }

    # Rule 2: it parses.
    $tokens = $null
    $errors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$errors)
    foreach ($problem in @($errors)) {
        $faults.Add("${name}:$($problem.Extent.StartLineNumber): does not parse: $($problem.Message)")
    }

    if (-not $ast.ParamBlock) { continue }
    $parameters = @($ast.ParamBlock.Parameters | ForEach-Object { $_.Name.VariablePath.UserPath })

    # Rule 4: a script that takes -Check writes or checks a page, and sorts by the characters' own order.
    if ($parameters -contains 'Check') {
        $sorts = @($ast.FindAll({
            param($node)
            $node -is [System.Management.Automation.Language.CommandAst] -and $node.GetCommandName() -in @('Sort-Object', 'sort')
        }, $true))
        foreach ($sort in $sorts) {
            $faults.Add("${name}:$($sort.Extent.StartLineNumber): Sort-Object orders text by the culture's rules, which Windows PowerShell and PowerShell 7 do not share: use Sort-Ordinal (tools\ordinal.ps1)")
        }
    }

    # Rule 3: no local named like a parameter of the script.
    $assigned = @($ast.FindAll({
        param($node)
        $node -is [System.Management.Automation.Language.AssignmentStatementAst] -or $node -is [System.Management.Automation.Language.ForEachStatementAst]
    }, $true))
    foreach ($statement in $assigned) {
        if (Test-InFunction $statement) { continue }
        $target = if ($statement -is [System.Management.Automation.Language.ForEachStatementAst]) { $statement.Variable } else { $statement.Left }
        # "[string]$text = ..." is a conversion around the variable.
        while ($target -is [System.Management.Automation.Language.ConvertExpressionAst]) { $target = $target.Child }
        if ($target -isnot [System.Management.Automation.Language.VariableExpressionAst]) { continue }
        $variable = $target.VariablePath.UserPath
        foreach ($parameter in $parameters) {
            if ($variable -ieq $parameter -and $variable -cne $parameter) {
                $faults.Add("${name}:$($target.Extent.StartLineNumber): the local `$$variable is the parameter `$$parameter (names are case-insensitive): give the local another name")
            }
        }
    }
}

if ($faults.Count -gt 0) {
    $faults | ForEach-Object { Write-Host $_ }
    Write-Host "tools/script-lint.ps1: $($faults.Count) fault(s) in the scripts; the rules are at the top of the script."
    exit 1
}

Write-Host "The scripts hold their rules: $($scripts.Count) scripts, ASCII, parsing, no local named like a parameter, no culture sort in a page script."
exit 0
