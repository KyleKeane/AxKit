# AxHelp.Core, the pure code

Everything of axHelp that needs no window: the settings, the conversation's command line, the files axHelp writes into Claude's working folder, and what counts as a picture file. Unit tested in `tests/AxHelp.Tests`. The spec is `docs/axHelp/SPEC.md` ("SPEC.md" below); the window is `src/AxHelp`, with its own `CLAUDE.md`. AI help's own pure code (the working folder among it) is the bundle's, `src/AxKit.Core/Ai`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxHelp
```

## Where things are

- `HelpSettings` (SPEC.md HE-2.3, HE-4, HE-5): on the bundle's `HotkeySettings` (the conversation's hotkey, Ctrl+Shift+Slash by default, Start with Windows); `ScreenshotHotkeyText` with `ScreenshotKey` (Ctrl+Shift+Period since 2026-11-24, when Ctrl+Shift+Comma went to axClip); the defaults of before, Windows+Alt+H (`RetiredDefaults`), and Windows+Alt+S and Ctrl+Shift+Comma (`RetiredScreenshotHotkeys`), taken over to the new ones in `Load` (HE-4.2), `Model` (`FastModel`, haiku, by default) with the choices `Models`, kept only when it is a model's name (`IsModelName`, `ModelOrDefault`: letters, digits, dots, hyphens, underscores and square brackets after a first letter or digit, since it goes onto Claude Code's command line, bundle repairs R-71; `ConversationArguments` checks it again), `ScreenshotsKept`; `ConversationArguments` (the axClaude command line: the folder, `--title axHelp`, `--model`, `--hide-on-close`, `--ai-tools`, `--source screen` (docs/axClaude/stream-mode.md SM-9.5: always Claude Code's screen, whatever axClaude's setting `claudeSource` says), `--hidden` for the start ahead, `--message`; `--ai-tools` is always given, SPEC.md HE-2.8: axClaude decides at each start of Claude Code by the user's AI tools setting, and axHelp reads no setting for it and never turns the tools on); `KeepConversationReady` (off at first, HE-D9: never change that default without the user's word) and `StartsConversationHidden`, the four conditions of the start ahead (SPEC.md HE-2.7).
- `WorkingFolderFiles` (SPEC.md HE-2.4): `CLAUDE.md` and `.claude\agents\agent.md` (a subagent on opus), written into the working folder only where missing. They are the user's from then on. What axKit keeps current there, the guides in `documentation` and the rule file that tells Claude to read them (SPEC.md HE-2.6), and the rule file of the AI tools (`.claude\rules\axkit-tools.md`, SPEC.md HE-2.8, `AxKit.Core/Ai/AiToolsRule`: made from `McpTools.All`, a new tool needs its guidance there), is not here: `AxKit.Core/Ai/AiDocumentation`, since Shift+F1 uses the same folder.
- `PictureFile` (SPEC.md HE-3.3; axDescribe's until 5.7.0): `Kinds`, `Filter` for the Windows file picker, `IsPicture`, `Refusal`.

## Gotchas

- A settings file of 5.6.0 holds `exchanges`; System.Text.Json leaves an unknown property alone, so it still reads.

- `Conversation` (SPEC.md HE-2.1, HE-1.3): the two pure decisions that keep one conversation at a time, `Next` (hand over to the window, wait for the process axHelp started, start only when there is neither) and `AtQuit` (ask the window to end, end a started process that has no window yet, nothing). The window keeps the started process; never go back to a wait by the clock.
- `WorkingFolderFiles.Obeyed` (SPEC.md HE-2.9, 2026-11-16): the agent file's path and its text as written, for `AxKit.Core/Ai/AiFolderCheck`: while `agent.md` holds exactly that text it is not asked about. Change `Agent` and an agent file written by an earlier version is asked about once.
