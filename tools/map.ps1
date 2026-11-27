<#
.SYNOPSIS
Writes docs\axKit\map.md, the map of the windows for people, from docs\axKit\workflow-graph.json; or checks that the
page is current (-Check).

.DESCRIPTION
The source is the workflow graph (docs\axKit\workflow-graph.md WG-2.5), which the inventory run writes from the
windows on the hidden desktop (tools\AxAutomate --inventory write). The page is for reading: by app, each waypoint
(the places a user aims for), how it is reached in one action from the window's other places and from anything over
it, and what can be done from it, each key with the command it runs, where it lands and what is said; then the
window's other places the same way, and what stands over the places with its way back. The keys from anywhere (each
app's own key, which no bound counts) come first. What the graph does not cover yet is said at the top.

-Check writes nothing and exits with 1 when the page on disk is not what the file gives; then run the script without
-Check and commit the page. tools\pages.ps1 runs both, and test.ps1 the check. The script stays ASCII.
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
. (Join-Path $root 'tools\ordinal.ps1')
$sourcePath = Join-Path $root 'docs\axKit\workflow-graph.json'
$documentPath = Join-Path $root 'docs\axKit\map.md'
$componentsPath = Join-Path $root 'docs\axKit\components.json'

$graph = [IO.File]::ReadAllText($sourcePath, [Text.Encoding]::UTF8) | ConvertFrom-Json
$components = [IO.File]::ReadAllText($componentsPath, [Text.Encoding]::UTF8) | ConvertFrom-Json

# A chord as the bundle writes a key: ctrl+shift+f3 as Ctrl+Shift+F3, pageup as Page Up.
function Get-KeyWords([string]$chord) {
    $names = @{ 'ctrl' = 'Ctrl'; 'shift' = 'Shift'; 'alt' = 'Alt'; 'pageup' = 'Page Up'; 'pagedown' = 'Page Down'; 'esc' = 'Escape' }
    $parts = @($chord -split '\+' | ForEach-Object {
            if ($names.ContainsKey($_)) { $names[$_] }
            elseif ($_.Length -le 3 -and $_ -match '^f[0-9]+$') { $_.ToUpperInvariant() }
            elseif ($_.Length -eq 1) { $_.ToUpperInvariant() }
            else { $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1) }
        })
    return $parts -join '+'
}

# What stands over a place, in words.
$overWords = @{
    'palette' = 'the command palette'; 'form' = 'a form'; 'question' = 'a question'; 'error' = 'an error'
    'notice' = 'a notice'; 'choice' = 'a choice in its place'; 'dialog' = 'a Windows dialog'; 'menu' = 'the menu'
}

$nodes = @{}
foreach ($node in $graph.nodes) { $nodes[$node.id] = $node }

# A node in words: the place's words, with what is over it.
function Get-NodeWords([string]$id) {
    if ($id -eq 'outside') { return 'outside axKit' }
    if (-not $nodes.ContainsKey($id)) { return $id }
    $node = $nodes[$id]
    if ($node.over -eq 'none') { return $node.words }
    $over = if ($overWords.ContainsKey($node.over)) { $overWords[$node.over] } else { $node.over }
    return "$over over $($node.words)"
}

# One edge as a line: the key, the command, where it lands and what is said.
function Get-EdgeLine($edge, [string]$from) {
    $line = "- **$(Get-KeyWords $edge.action)**"
    if ($edge.PSObject.Properties['command'] -and $edge.command) { $line += ", $($edge.command)" }
    $line += if ($edge.to -eq $from) { ': stays here' } else { ": to $(Get-NodeWords $edge.to)" }
    if ($edge.said) { $line += "; says `"$($edge.said)`"" }
    return $line + '.'
}

# The ways into a node in one action from the window's other nodes, grouped by where they start.
function Get-WaysIn($node, $appNodes) {
    $ways = New-Object System.Collections.Generic.List[string]
    foreach ($other in $appNodes) {
        if ($other.id -eq $node.id) { continue }
        $keys = @($other.edges | Where-Object { $_.to -eq $node.id } | ForEach-Object { Get-KeyWords $_.action })
        if ($keys.Count -gt 0) { $ways.Add("from $(Get-NodeWords $other.id): $($keys -join ', ')") }
    }
    return $ways
}

$apps = @(Sort-Ordinal @($graph.apps) { $_.app })
$covered = @($apps | ForEach-Object { $_.app })
$notCovered = @(Sort-Ordinal @($components.apps | Where-Object { $covered -notcontains $_.app } | ForEach-Object { $_.app }))
$outside = $nodes['outside']

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# The map of the windows')
$lines.Add('')
$lines.Add('Written by `tools/map.ps1` from `docs/axKit/workflow-graph.json`, which the inventory run writes from the windows')
$lines.Add('themselves (`tools/AxAutomate --inventory write`); never edit it by hand. The design is `docs/axKit/workflow-graph.md`.')
$lines.Add('Each window has a few places where the focus can be; a waypoint is a place you aim for. Under each place: how it is')
$lines.Add('reached in one action, and every key that does something there, with the command it runs, where you land and what')
$lines.Add('you hear. Moving inside a place (the arrows, Home, End, the lines of a text) is not on the map.')
$lines.Add('')
$lines.Add("The map covers $($apps.Count) of $($components.apps.Count) apps: $($covered -join ', ').")
if ($notCovered.Count -gt 0) { $lines.Add("Not on it yet: $($notCovered -join ', ').") }
$lines.Add('')
$lines.Add('## From anywhere')
$lines.Add('')
$lines.Add('Each app''s own key opens its window on its first waypoint. It is free: no count of actions includes it.')
$lines.Add('')
if ($outside) {
    foreach ($edge in @(Sort-Ordinal @($outside.edges) { $_.action })) {
        $lines.Add("- **$(Get-KeyWords $edge.action)**: $($edge.command), on $(Get-NodeWords $edge.to).")
    }
}

foreach ($app in $apps) {
    $appNodes = @(Sort-Ordinal @($graph.nodes | Where-Object { $_.app -eq $app.app }) { $_.id })
    $type = @($components.'window types' | Where-Object { $_.id -eq $app.type })
    $lines.Add('')
    $lines.Add("## $($app.app)")
    $lines.Add('')
    $kind = if ($type.Count -gt 0) { $type[0].title.ToLowerInvariant() } else { 'a window' }
    $lines.Add("On $kind. It opens on $(Get-NodeWords $app.start).")
    $places = @($appNodes | Where-Object { $_.over -eq 'none' })
    $waypoints = @($places | Where-Object { $_.waypoint })
    $others = @($places | Where-Object { -not $_.waypoint })
    foreach ($group in @(@{ Title = 'waypoint'; Nodes = $waypoints }, @{ Title = 'place'; Nodes = $others })) {
        foreach ($node in $group.Nodes) {
            $lines.Add('')
            $title = $node.words.Substring(0, 1).ToUpperInvariant() + $node.words.Substring(1)
            $lines.Add("### $title, a $($group.Title)")
            $lines.Add('')
            $reached = New-Object System.Collections.Generic.List[string]
            if ($node.id -eq $app.start -and $outside) {
                $own = @($outside.edges | Where-Object { $_.to -eq $node.id } | ForEach-Object { Get-KeyWords $_.action })
                if ($own.Count -gt 0) { $reached.Add("from anywhere: $($own -join ', '), free") }
            }
            foreach ($way in (Get-WaysIn $node $appNodes)) { $reached.Add($way) }
            $lines.Add('Reached ' + $(if ($reached.Count -gt 0) { ($reached -join '; ') + '.' } else { 'by no action the crawl found.' }))
            $lines.Add('')
            $edges = @(Sort-Ordinal @($node.edges) { $_.action })
            if ($edges.Count -eq 0) { $lines.Add('No action found here.'); continue }
            $lines.Add('What can be done here:')
            $lines.Add('')
            foreach ($edge in $edges) { $lines.Add((Get-EdgeLine $edge $node.id)) }
        }
    }

    $overNodes = @($appNodes | Where-Object { $_.over -ne 'none' })
    if ($overNodes.Count -gt 0) {
        $lines.Add('')
        $lines.Add('### What stands over the places')
        $lines.Add('')
        foreach ($node in $overNodes) {
            $back = @(Sort-Ordinal @($node.edges) { $_.action } | ForEach-Object { "$(Get-KeyWords $_.action) to $(Get-NodeWords $_.to)" })
            $way = if ($back.Count -gt 0) { $back -join '; ' } else { 'no way back found' }
            $first = (Get-NodeWords $node.id)
            $lines.Add("- $($first.Substring(0, 1).ToUpperInvariant() + $first.Substring(1)): $way.")
        }
    }
}

$text = ($lines -join "`r`n") + "`r`n"
$edgeCount = 0
foreach ($node in $graph.nodes) { $edgeCount += @($node.edges).Count }
$summary = "$($apps.Count) app(s), $(@($graph.nodes).Count) node(s), $edgeCount edge(s)"
if ($Check) {
    $current = if (Test-Path $documentPath) { [IO.File]::ReadAllText($documentPath) } else { '' }
    # Line endings aside: the checkout's are native, LF on CI.
    if (($current -replace "`r`n", "`n") -cne ($text -replace "`r`n", "`n")) {
        Write-Host 'docs/axKit/map.md is not what docs/axKit/workflow-graph.json gives: run tools\map.ps1 and commit the page.'
        exit 1
    }

    Write-Host "docs/axKit/map.md is current: $summary."
    exit 0
}

[IO.File]::WriteAllText($documentPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Wrote docs/axKit/map.md: $summary."
# An exit code of its own: without it the caller reads the code of whatever ran before (tools\pages.ps1).
exit 0
