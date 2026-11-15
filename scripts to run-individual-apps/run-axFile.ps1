<#
.SYNOPSIS
  Builds axKit and starts axFile: run.ps1 with -App file. Every other argument goes through unchanged. Without a path it
  opens the last folder axFile had.

.EXAMPLE
  & '.\scripts to run-individual-apps\run-axFile.ps1'                          # the last folder
  & '.\scripts to run-individual-apps\run-axFile.ps1' C:\Music\Dune            # that folder (or a file, selected in its folder)
  & '.\scripts to run-individual-apps\run-axFile.ps1' -NoBuild                 # without building
#>
& "$PSScriptRoot\..\run.ps1" -App file @args
exit $LASTEXITCODE
