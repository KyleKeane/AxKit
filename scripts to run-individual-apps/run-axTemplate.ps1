<#
.SYNOPSIS
  Builds axKit and starts axTemplate: run.ps1 with -App template. Every other argument goes through unchanged.

.EXAMPLE
  & '.\scripts to run-individual-apps\run-axTemplate.ps1'                          # build and start axTemplate
  & '.\scripts to run-individual-apps\run-axTemplate.ps1' -NoBuild                 # without building
#>
& "$PSScriptRoot\..\run.ps1" -App template @args
exit $LASTEXITCODE
