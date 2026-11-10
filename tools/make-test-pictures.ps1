<#
.SYNOPSIS
  Draws the test pictures of axHelp's screenshots (docs/axHelp/SPEC.md HE-3) into tests\fixtures\pictures: three PNG files
  that look like what a screen shows, made in code so that nothing personal is in them and anyone can make them again.

.DESCRIPTION
  dialog.png   a dialog "Save changes to report.docx?" with Yes, No and Cancel buttons
  editor.png   an axDown-like window: a menu bar, a few lines of text, a status bar
  chart.png    a bar chart "Sales by quarter" with four labelled bars
  The same files are committed; run this only to change them, then commit the result.

.EXAMPLE
  .\tools\make-test-pictures.ps1
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).ProviderPath
$folder = Join-Path $root 'tests\fixtures\pictures'
New-Item -ItemType Directory -Force $folder | Out-Null

function New-Picture([int]$width, [int]$height, [scriptblock]$draw, [string]$name) {
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    & $draw $g
    $g.Dispose()
    $path = Join-Path $folder $name
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    Write-Host "Wrote $path"
}

$font = New-Object System.Drawing.Font 'Segoe UI', 11
$bold = New-Object System.Drawing.Font 'Segoe UI', 11, ([System.Drawing.FontStyle]::Bold)
$black = [System.Drawing.Brushes]::Black
$white = [System.Drawing.Brushes]::White
$grey = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(240, 240, 240))
$blue = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(0, 120, 215))
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(160, 160, 160))

function Button([System.Drawing.Graphics]$g, [int]$x, [int]$y, [string]$text, [bool]$default) {
    $g.FillRectangle($grey, $x, $y, 90, 30)
    $g.DrawRectangle($(if ($default) { [System.Drawing.Pens]::Blue } else { $pen }), $x, $y, 90, 30)
    $size = $g.MeasureString($text, $font)
    $g.DrawString($text, $font, $black, $x + (90 - $size.Width) / 2, $y + (30 - $size.Height) / 2)
}

New-Picture 520 220 {
    param($g)
    $g.Clear([System.Drawing.Color]::White)
    $g.FillRectangle($blue, 0, 0, 520, 32)
    $g.DrawString('Microsoft Word', $bold, $white, 10, 6)
    $g.DrawString('Save changes to report.docx?', $bold, $black, 24, 60)
    $g.DrawString('If you click No, your changes since the last save are lost.', $font, $black, 24, 92)
    Button $g 190 160 'Yes' $true
    Button $g 300 160 'No' $false
    Button $g 410 160 'Cancel' $false
} 'dialog.png'

New-Picture 760 440 {
    param($g)
    $g.Clear([System.Drawing.Color]::White)
    $g.FillRectangle($grey, 0, 0, 760, 30)
    $g.DrawString('notes.md - axDown', $bold, $black, 10, 5)
    $g.FillRectangle($grey, 0, 30, 760, 26)
    $x = 10
    foreach ($menu in 'File', 'Edit', 'Navigate', 'Options', 'axKit', 'Help') {
        $g.DrawString($menu, $font, $black, $x, 33)
        $x += [int]$g.MeasureString($menu, $font).Width + 18
    }
    $lines = @('# Shopping', '', '- Bread', '- Milk', '- Two apples', '', 'Call the plumber on Monday about the tap in the kitchen.')
    $y = 70
    foreach ($line in $lines) { $g.DrawString($line, $font, $black, 12, $y); $y += 26 }
    $g.FillRectangle($grey, 0, 410, 760, 30)
    $g.DrawString('notes.md', $font, $black, 10, 415)
    $g.DrawString('Line 3, Column 1 · UTF-8 · CRLF', $font, $black, 500, 415)
} 'editor.png'

New-Picture 640 420 {
    param($g)
    $g.Clear([System.Drawing.Color]::White)
    $g.DrawString('Sales by quarter, 2026 (thousands)', $bold, $black, 20, 14)
    $g.DrawLine([System.Drawing.Pens]::Black, 60, 360, 600, 360)
    $g.DrawLine([System.Drawing.Pens]::Black, 60, 60, 60, 360)
    $values = @(@('Q1', 120), @('Q2', 180), @('Q3', 95), @('Q4', 210))
    $x = 110
    foreach ($bar in $values) {
        $height = [int]($bar[1] * 1.2)
        $g.FillRectangle($blue, $x, 360 - $height, 70, $height)
        $g.DrawString($bar[0], $font, $black, $x + 22, 368)
        $g.DrawString([string]$bar[1], $font, $black, $x + 18, 340 - $height)
        $x += 120
    }
} 'chart.png'
