<#
.SYNOPSIS
  Builds axKit and starts axStart, the program launcher: run.ps1 with -App start. Its search field shows, and it keeps
  running in the background with its hotkey (Ctrl+Shift+Space unless changed) until axStart, Quit axStart. An installed
  axStart already running takes the start instead: quit it first to try a development build.

.EXAMPLE
  .\run-axStart.ps1               # build, then the search field
  .\run-axStart.ps1 -NoBuild      # without building
#>
& "$PSScriptRoot\run.ps1" -App start @args
exit $LASTEXITCODE
