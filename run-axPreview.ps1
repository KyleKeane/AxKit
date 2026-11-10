<#
.SYNOPSIS
  Builds axKit and starts axPreview: run.ps1 with -App preview. Every other argument goes through unchanged. Without a
  path it opens an empty window.

.EXAMPLE
  .\run-axPreview.ps1 C:\notes\README.md    # the file, read-only
  .\run-axPreview.ps1                       # an empty window
  .\run-axPreview.ps1 -NoBuild              # without building
#>
& "$PSScriptRoot\run.ps1" -App preview @args
exit $LASTEXITCODE
