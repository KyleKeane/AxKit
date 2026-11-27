# AxKit.Core notes: Background apps

- `SignInStart` (docs/axStart/SPEC.md AS-1.3; from axStart, axClip, 2026-10-23): a background app's value under the
  user's Run key, "axKit <app>" with `"…\AxKit.exe" <verb> --hidden`, written only by the installed program, and not at all by the Store's copy, where `OpenWindowsStartupSettings` opens Windows' Startup apps page instead (AX-13.4, 2026-11-08). `Applies` is the rule of who touches the Run key at all: never the Store's copy and never a window of a test run (2026-11-27: a run's window with the setting off removed the user's own value). Named so
  since axProcess has a `StartupEntry` of its own, an entry of the startup list.
- `Hotkey` (docs/axStart/SPEC.md AS-4; from AxStart.Core, axClip, 2026-10-23): a system-wide key as `RegisterHotKey`
  takes it, its text both ways ("Ctrl+Shift+Space", any order and case on reading), `Refusal` (Ctrl, Alt or the
  Windows key, and a key of `Names`, Slash and Comma among them, never Period, AX-8.11), axStart's `Default`; each app names its own default.
- `BackgroundStart` with `SecondStart` (bundle AX-1.9, repairs R-65, 2026-11-16): what a second start of axStart, axClip or axHelp does when the first copy does not answer: `Decide` (window found and answered, found and silent, not found while the mutex is held; never asks with `--hidden` or in a test run), the words `Question` and `NotEnded`, and `IsCopyOf`, whether a command line names the app's verb as its first argument. The window side is `AxKit.Forms/AppHost.OneCopy`. Tested in `BackgroundStartTests`.
