# axHelp specification

Status: first draft written by `tools/new-app.ps1` on 2026-11-09; nothing below is decided yet. Requirements are
numbered HE-n.m. The bundle's rules are `docs/axkit/SPEC.md` (AX-7 accessibility, AX-8 keys); "SPEC.md" in
axHelp's code means this file.

## 1. Summary

axHelp: a question from anywhere or about this window, answered by Claude. `AxKit.exe help `, Start entry `axKit axHelp`, command `axhelp`.

## 2. Goals and non-goals

## 3. Scenarios

## 4. Requirements

### HE-1 Start

- HE-1.1 `AxKit.exe help` starts axHelp (bundle AX-1.1).

### HE-2 The window

- HE-2.1 One list, named "Help", with the focus at start; its title ends in " - axHelp".

### HE-7 Keys

Every key is bundle-wide (AX-8) or window-specific, in `AxHelpKeys`. None of the window's own yet.

### HE-8 Notices and announcements

### HE-9 Settings

- HE-9.1 `%APPDATA%\AxKit\axhelp.json`: the window base's settings.

## 5. Architecture

The window is `src/AxHelp` on `AxKit.Forms/AxKitForm`; the pure code is `src/AxHelp.Core`, tested in
`tests/AxHelp.Tests`.

## 6. Decisions

## 7. Testing

The unit tests, and the NVDA test plan `docs/axhelp/nvda-test-plan.md`.
