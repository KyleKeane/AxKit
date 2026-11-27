<#
.SYNOPSIS
  The spoken warning and the countdown of the scripts that take the keyboard, the mouse or the foreground of the
  user's own desktop: nvda-pass.ps1 and tools\store-screenshots.ps1.

.DESCRIPTION
  Dot-source it. Such a script runs only at the user's word, after he has stepped away (docs/axKit/automation.md
  section 3), and he must hear that it is coming before it takes anything (his ask of 2026-11-21: he touched the
  keyboard not knowing a pass was about to start; until then nvda-pass.ps1 waited in silence for 20 seconds without
  input, and the first spoken word came with the keyboard already taken). So Windows' own voice speaks, and right
  before the script takes anything it says one short sentence and counts from ten to one aloud. A key or a mouse move
  after that sentence began means someone is at the computer: the script stops for good and says so. Windows keeps
  the time of the last input of the session (GetLastInputInfo). The voice is SAPI's: NVDA never speaks it, and no
  window takes the focus.

    Write-Spoken <sentence> [-Whole]   one sentence in Windows' voice, also written as a line. -Whole waits until it
                                       has been spoken to its end; without it the script goes on while the voice
                                       speaks. A new sentence takes the place of one still being spoken. A computer
                                       without the voice gets a tone, so that something is heard.
    Stop-WhenSomeoneIsThere -Coming <sentence> -Stopped <sentence> -Nothing <sentence> -Again <words>
                                       the countdown. -Coming is the sentence before the numbers ("NVDA test pass.
                                       Press any key to stop it."), -Stopped what is said when someone was there
                                       ("Stopped. No test pass."), -Nothing what is said when Windows cannot tell
                                       ("No test pass."), -Again the written advice after "someone is at the
                                       computer". It ends the calling script with exit code 2 when someone was there
                                       or Windows could not say; otherwise it returns after the last number.

  Until 2026-11-27 the countdown was nvda-pass.ps1's alone, and tools\store-screenshots.ps1 opened its windows in
  front with nothing said. The runner itself (tools\AxAutomate --nvda) has no countdown: it is started through
  nvda-pass.ps1. The script stays ASCII (Windows PowerShell 5.1 reads it as ANSI).
#>

Add-Type -Namespace AxKitNvdaPass -Name LastInput -MemberDefinition @'
[StructLayout(LayoutKind.Sequential)]
public struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
[DllImport("user32.dll")]
public static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
public static long MillisecondsAgo() {
    LASTINPUTINFO info = new LASTINPUTINFO();
    info.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
    if (!GetLastInputInfo(ref info)) { return -1; }
    return (long)unchecked((uint)Environment.TickCount - info.dwTime);
}
'@
$spoken = $null
try { $spoken = New-Object -ComObject SAPI.SpVoice } catch { $spoken = $null }

function Write-Spoken([string]$Sentence, [switch]$Whole) {
    Write-Host "(Windows' voice: $Sentence)"
    if ($null -eq $spoken) {
        [Console]::Beep(880, 400)
        return
    }
    # SAPI's flags: 1 speaks without waiting, 2 ends what is still being spoken.
    $flags = 3
    if ($Whole) { $flags = 2 }
    try { [void]$spoken.Speak($Sentence, $flags) } catch { [Console]::Beep(880, 400) }
}

function Stop-WhenSomeoneIsThere([string]$Coming, [string]$Stopped, [string]$Nothing, [string]$Again) {
    if ([AxKitNvdaPass.LastInput]::MillisecondsAgo() -lt 0) {
        Write-Spoken $Nothing -Whole
        Write-Host 'Not started: Windows did not say when the last key or mouse input was, so nobody is known to have stepped away.'
        exit 2
    }
    # One short sentence, then every second from ten to one (the user's words on hearing the first form, 2026-11-21:
    # "shorter sentence and just count down from ten").
    $began = [Diagnostics.Stopwatch]::StartNew()
    Write-Spoken $Coming -Whole
    for ($left = 10; $left -ge 0; $left--) {
        $ago = [AxKitNvdaPass.LastInput]::MillisecondsAgo()
        if ($ago -ge 0 -and $ago -lt $began.ElapsedMilliseconds) {
            Write-Spoken $Stopped -Whole
            Write-Host "Not started: a key or the mouse was used during the spoken countdown, so someone is at the computer. $Again"
            exit 2
        }
        if ($left -gt 0) {
            Write-Spoken "$left"
            Start-Sleep -Seconds 1
        }
    }
}
