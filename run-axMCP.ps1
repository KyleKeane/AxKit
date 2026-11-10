<#
.SYNOPSIS
  Builds axKit and starts axMCP: run.ps1 with -App mcpclient. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axMCP.ps1                          # build and start axMCP
  .\run-axMCP.ps1 C:\some\path            # build and start axMCP on a folder
  .\run-axMCP.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App mcpclient @args
exit $LASTEXITCODE
