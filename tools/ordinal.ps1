<#
.SYNOPSIS
Sort-Ordinal, for the scripts that write a page or check one: items in the order of their keys' own characters.

.DESCRIPTION
Dot-source it: . (Join-Path $PSScriptRoot 'ordinal.ps1')

Sort-Object orders text by the culture's rules, and Windows PowerShell 5.1 and PowerShell 7 do not agree on them:
a hyphen and a full stop change places. A page written on the development computer was then stale in the build on
GitHub, which runs the other PowerShell (2026-11-17, every build of an afternoon). So a script that takes -Check
sorts with this function and never with Sort-Object; tools\script-lint.ps1 holds that (its rule 4).

The order is the characters' own, case aside (StringComparer.OrdinalIgnoreCase). For a key of several parts, join
them with a tab, which sorts before every printable character, so that a shorter first part comes first; write a
number at a fixed width: "{0}`t{1:D6}" -f $_.File, $_.Line.

.EXAMPLE
  $files = @(Sort-Ordinal $files { $_.FullName })
  $names = @(Sort-Ordinal $names)
#>

# The items in the order of their keys; the key is the item's own text unless a script block over $_ gives one.
# The items come back one by one, as from any pipeline: wrap the call in @( ) where an array is needed.
function Sort-Ordinal($items, [scriptblock]$key) {
    # No items given as $null (an empty pipeline in parentheses) are no items, not one empty item.
    $list = [object[]]@($items | Where-Object { $null -ne $_ })
    if ($list.Count -lt 2) { return $list }
    $keys = if ($key) { [string[]]@($list | ForEach-Object $key) } else { [string[]]@($list | ForEach-Object { [string]$_ }) }
    [Array]::Sort($keys, $list, [StringComparer]::OrdinalIgnoreCase)
    return $list
}
