# AxKit.Core notes: Background apps

- `SignInStart` (docs/axStart/SPEC.md AS-1.3; from axStart, axClip, 2026-10-23): a background app's value under the
  user's Run key, "axKit <app>" with `"…\AxKit.exe" <verb> --hidden`, written only by the installed program, and not at all by the Store's copy, where `OpenWindowsStartupSettings` opens Windows' Startup apps page instead (AX-13.4, 2026-11-08). Named so
  since axProcess has a `StartupEntry` of its own, an entry of the startup list.
- `Hotkey` (docs/axStart/SPEC.md AS-4; from AxStart.Core, axClip, 2026-10-23): a system-wide key as `RegisterHotKey`
  takes it, its text both ways ("Ctrl+Shift+Space", any order and case on reading), `Refusal` (Ctrl, Alt or the
  Windows key, and a key of `Names`, Slash and Comma among them, never Period, AX-8.11), axStart's `Default`; each app names its own default.
