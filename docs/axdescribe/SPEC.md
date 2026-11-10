# axDescribe specification

Status: first draft written by `tools/new-app.ps1` on 2026-11-09; nothing below is decided yet. Requirements are
numbered DE-n.m. The bundle's rules are `docs/axkit/SPEC.md` (AX-7 accessibility, AX-8 keys); "SPEC.md" in
axDescribe's code means this file.

## 1. Summary

axDescribe: the window in front described by Claude. `AxKit.exe describe `, Start entry `axKit axDescribe`, command `axdescribe`.

## 2. Goals and non-goals

## 3. Scenarios

## 4. Requirements

### DE-1 Start

- DE-1.1 `AxKit.exe describe` starts axDescribe (bundle AX-1.1).

### DE-2 The window

- DE-2.1 One list, named "Describe", with the focus at start; its title ends in " - axDescribe".

### DE-7 Keys

Every key is bundle-wide (AX-8) or window-specific, in `AxDescribeKeys`. None of the window's own yet.

### DE-8 Notices and announcements

### DE-9 Settings

- DE-9.1 `%APPDATA%\AxKit\axdescribe.json`: the window base's settings.

## 5. Architecture

The window is `src/AxDescribe` on `AxKit.Forms/AxKitForm`; the pure code is `src/AxDescribe.Core`, tested in
`tests/AxDescribe.Tests`.

## 6. Decisions

## 7. Testing

The unit tests, and the NVDA test plan `docs/axdescribe/nvda-test-plan.md`.
