<#
.SYNOPSIS
  Builds axKit and starts axJSON: run.ps1 with -App json. Every other argument goes through unchanged. Without a file
  it opens an empty document.

.EXAMPLE
  & '.\scripts to run-individual-apps\run-axJSON.ps1'                          # an empty document
  & '.\scripts to run-individual-apps\run-axJSON.ps1' C:\x\settings.json       # that file
  & '.\scripts to run-individual-apps\run-axJSON.ps1' -NoBuild                 # without building
#>
& "$PSScriptRoot\..\run.ps1" -App json @args
exit $LASTEXITCODE
