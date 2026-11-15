<#
.SYNOPSIS
  Builds axKit and starts axPreview: run.ps1 with -App preview. Every other argument goes through unchanged. Without a
  path it opens an empty window.

.EXAMPLE
  & '.\scripts to run-individual-apps\run-axPreview.ps1' C:\notes\README.md    # the file, read-only
  & '.\scripts to run-individual-apps\run-axPreview.ps1'                       # an empty window
  & '.\scripts to run-individual-apps\run-axPreview.ps1' -NoBuild              # without building
#>
& "$PSScriptRoot\..\run.ps1" -App preview @args
exit $LASTEXITCODE
