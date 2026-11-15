<#
.SYNOPSIS
  Builds axKit and starts axClip: run.ps1 with -App clip. Every other argument goes through unchanged.

.EXAMPLE
  & '.\scripts to run-individual-apps\run-axClip.ps1'                          # build and start axClip
  & '.\scripts to run-individual-apps\run-axClip.ps1' -NoBuild                 # without building
#>
& "$PSScriptRoot\..\run.ps1" -App clip @args
exit $LASTEXITCODE
