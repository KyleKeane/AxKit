<#
.SYNOPSIS
  Builds axKit and starts axAutomate: run.ps1 with -App automate. Every other argument goes through unchanged.

.EXAMPLE
  & '.\scripts to run-individual-apps\run-axAutomate.ps1'                          # build and start axAutomate
  & '.\scripts to run-individual-apps\run-axAutomate.ps1' C:\some\path            # build and start axAutomate on a folder
  & '.\scripts to run-individual-apps\run-axAutomate.ps1' -NoBuild                 # without building
#>
& "$PSScriptRoot\..\run.ps1" -App automate @args
exit $LASTEXITCODE
