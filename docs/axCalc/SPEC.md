# axCalc — Specification

axCalc is an app of the axKit bundle (`docs/axKit/SPEC.md`): a calculator as one field and a history, for a blind
developer who uses the NVDA screen reader. Built 2026-10-23 on the branch `axCalc` (the long run of that day,
`docs/axKit/plan.md`), not yet heard under NVDA. The document is kept in step with the code (a behaviour change
updates it in the same commit). Requirements are numbered `AC-n.m` and decisions `AC-Dn`; the bundle's rules, AX-7
(accessibility), AX-8 (keys) and AX-9 (the Window, axKit and Help menus), are cited by number and not repeated. In axCalc's code and
documents, "SPEC.md" means this file.

## 1. Summary

Windows' Calculator is a grid of buttons with a display that a screen reader reads one way and a keyboard user drives
another. A developer wants something simpler: type `1920*1080*4`, press Enter, hear the answer, and keep a history to
read back and copy from. axCalc is one window with an expression field at the bottom and the history above it, a text
of the lines `1920*1080*4 = 8294400`, read as the conversation in axClaude and the terminal in axTerminal are
(AC-2.1; a list until 2026-11-22). Enter answers; the answer is spoken once; nothing else is.

## 2. Goals and non-goals

- **G1 Type, Enter, hear.** The expression field takes the usual notation (`+ - * / % ^`, parentheses, functions,
  constants, variables) and answers on Enter in one announcement (AX-7.3).
- **G2 A history to read.** Every answer is a line of the history's text (the bundle's read-only text of a
  conversation window since 2026-11-22; a list box until then); arrows
  explore it, Enter puts a line's expression back in the field, a key copies a line's answer.
- **G3 For development.** Hexadecimal, binary and octal literals (`0xFF`, `0b1010`, `0o17`), the answer in those bases
  on a key, integer results without a trailing `.0`, variables for a value used twice.
- **G4 Local and plain.** No network, no library: the evaluator is a small parser in `AxCalc.Core`, tested (B-10).
  The history and the variables are in the settings file, plain JSON (B-15).
- **Non-goals.** Units, a programmer mode with fixed word sizes and bit operators, fractions, complex numbers,
  matrices, plotting, a symbolic algebra system, and buttons. Each waits for the ask (AC-D3).

## 3. Scenarios

1. Start axCalc (Start menu, `axCalc`, or the axKit menu's Apps): NVDA reads "axCalc window",
   then "Expression, edit". Type `1920*1080*4`, Enter: "8294400". The history's first line is `1920*1080*4 = 8294400`.
2. Type `*2`, Enter: "16588800": an expression that starts with an operator continues from the last answer.
3. F8: "16588800, hex FD 2000, binary 1111 1101 0010 0000 0000 0000, octal 77220000".
4. Type `rate = 1.25`, Enter: "rate = 1.25". Type `400 * rate`, Enter: "500".
5. Shift+Tab to the history, Up to `1920*1080*4 = 8294400`, Ctrl+Shift+C: "Copied 8294400". Enter: the field holds
   `1920*1080*4` with the focus in it, ready to change.
6. Type `sqrt(-1)`, Enter: "Error: not a real number"; the field keeps the text, so that it can be mended.

## 4. Functional requirements

### AC-1 Launch

- AC-1.1 `AxKit.exe calc`; the console command `axCalc`; the Start menu entry "axKit axCalc". No File Explorer entry: a
  calculator opens nothing. A path or any other argument is reported with the usage text (`AppHost`).
- AC-1.2 `AxCalcApp.Run(args)` is the entry point (AX-1.7): the host's log (`axCalc.log`), crash handling, `--help`
  and `--version`, the settings (`axCalc.json`), then `CalcForm`.
- AC-1.3 Several windows may be open; each keeps its own history while it runs, and the one closed last writes the
  history it had into the settings file (AC-D4).

### AC-2 The window

- AC-2.1 The window is the bundle's conversation window (`ConversationForm`, `docs/axKit/framework.md` section 4.1,
  step 9; since 2026-11-22, the maintainer: "axcalc i think should just be using the same as axclaude and
  axterminal"; a window of its own with the history as a list until then). From the top: the menu bar; the history, a
  read-only text named "History", one line an answer; the expression field, named "Expression", with its name in a
  band over it for the eyes; the status bar. Tab and Shift+Tab move between the history and the field, and so do
  Ctrl+Tab, Ctrl+Shift+Tab and F6 (AX-8.2). A notice takes the place of both while it shows (AX-6.4). The focus
  starts in the field. What a conversation window has is here too: find in the text, bookmarks, the Context menu on
  a line, a selection copied with Ctrl+C, and a line of the app's own ("System: …") for what the window must say
  about itself (a settings file that could not be read). The history's name no longer counts its entries; the
  question before Clear history does (AC-5.3).
- AC-2.2 The title is "axCalc", the app's name alone, since nothing is open in it (bundle AX-9.6, the maintainer's word of 2026-11-26; "Calculator - axCalc" until then), with the window's role (AX-7.1):
  NVDA says "axCalc window".
- AC-2.3 The status bar's left label is the last answer ("Answer 8294400", "No answer yet"); the right one the angle
  unit and the number of variables ("Degrees, 1 variable"). No AccessibleName, no Spring (`StatusLayout`).
- AC-2.4 The history has the newest entry last, as a paper roll does, and its caret is on the last line when the
  focus comes to it, so that Up from the end reads back. At most 500 entries; the oldest go first.

### AC-3 Expressions

- AC-3.1 Numbers: decimal with an optional fraction and exponent (`2.5`, `.5`, `6.02e23`), underscores between digits
  as separators (`1_000_000`), and `0x` hexadecimal, `0b` binary, `0o` octal integers, in either case. Everything is a
  double-precision number.
- AC-3.2 Operators, from the loosest: `+` and `-`; `*`, `/` (also `×` and `÷`), `%` and the word `mod` (the remainder,
  with the sign of the left side); a leading `-` or `+` (negation); `^` or `**` (power, right to left: `2^3^2` is 512;
  `-2^2` is -4); `!` after a value (the factorial of a whole number from 0 to 170). Parentheses group.
- AC-3.3 Functions, by name in any case, with their arguments in parentheses and separated by commas: `sqrt`, `cbrt`,
  `abs`, `exp`, `ln`, `log` (base 10), `log2`, `sin`, `cos`, `tan`, `asin`, `acos`, `atan`, `floor`, `ceil`, `trunc`,
  `round(x)` and `round(x, digits)` (digits 0 to 15, halves away from zero), `min` and `max` (one or more arguments),
  `hypot(x, y)`. The trigonometric ones take and give degrees or radians by Options, Angles (AC-6.1); degrees at
  first, as Windows' Calculator.
- AC-3.4 Names: the constants `pi` (also `π`) and `e`, `ans` (the last answer), and the variables. A name is a letter
  or `_`, then letters, digits and `_`, in any case.
- AC-3.5 An expression that starts with `+`, `*`, `/`, `×`, `÷`, `%`, `^` or `**` begins with the last answer: `*2`
  is `ans*2`. A leading `-` is a negative number, so `-3` is minus three; `ans-3` subtracts. With no answer yet the
  leading operator is the error "No answer yet to continue from".
- AC-3.6 `name = expression` sets a variable to the expression's value and answers "name = value"; the value is also
  the new last answer. The names of the constants, the functions and `ans` cannot be set ("Error: pi cannot be
  changed"). Session, Variables lists them (AC-5.4).
- AC-3.7 An answer is written in plain digits when it is a whole number under 10^15 (`8294400`, never `8294400.0`
  or `8.2944E+06`), otherwise with up to 15 significant digits (`0.333333333333333`, `6.02E+23`); `-0` is `0`.
- AC-3.8 Errors, each spoken as "Error: …" with the field left as it was and nothing added to the history: "empty"
  (Enter on a blank field says "Type an expression" instead), "unknown name foo", "unexpected …" naming the
  character or the word, "missing closing parenthesis", "division by zero", "not a real number" (the square root of
  a negative, the logarithm of zero or less, `asin(2)`), "too large", "factorial takes a whole number from 0 to 170",
  "sqrt takes 1 argument" (and the like for each function).

### AC-4 The answer in other bases

- AC-4.1 F8 (Ctrl+I until 2026-11-26) says the last answer, or the selected entry's answer while the history has the focus, in hexadecimal,
  binary and octal: "255, hex FF, binary 1111 1111, octal 377". The binary digits are grouped by four from the right
  and the hexadecimal by four too, so that the reading has pauses. A negative whole number is the magnitude with a
  minus ("hex -FF"); an answer that is not a whole number, or is over 2^63, says "255.5 is not a whole number" or "too
  large for other bases".
- AC-4.2 Session, Copy answer in ▸ Hexadecimal, Binary, Octal copies that form without spaces, with its prefix
  (`0xFF`, `0b11111111`, `0o377`), so that it pastes into code.

### AC-5 The history

- AC-5.1 Each answer adds the entry "<expression> = <answer>", the expression as typed without the spaces around it
  (`1920*1080*4 = 8294400`); an assignment's entry is "<name> = <value>" (`rate = 1.25`), and its whole expression is
  what Enter puts back (AC-5.2).
- AC-5.2 In the history: Enter on a line puts its expression into the field, the caret at its end, and moves the
  focus there (the Context menu's "Take this line's expression into the field"); on a line that is no answer (a line
  of the app's own, a bookmark) it says "No answer on this line". Ctrl+Shift+C copies the answer of the line you are
  on ("Copied 8294400"), and F8 says it in other bases; Escape goes back to the field. Delete no longer removes
  one entry (2026-11-22, the framework's decision 2): the history is a text, and Clear history empties it.
- AC-5.3 Session, Clear history (Ctrl+Shift+Delete) asks "Clear the history? The N entries cannot be brought back."
  with Clear and Cancel; Clear empties the history, its bookmarks with it, and says "History cleared". The variables stay; Session, Clear
  variables removes them after the same kind of question.
- AC-5.4 Session, Variables shows a notice listing each variable as "name = value", one per line, or "No
  variables. Set one with name = value".
- AC-5.5 Ctrl+S saves the history as a text file, one entry per line, through the Windows save dialog (AX-7.2), and
  says "Saved <file name>".
- AC-5.6 Ctrl+F finds text in the history from the line you are on (the conversation window's find), F3 and Shift+F3
  the next and the previous, round the ends; "Not found" otherwise.
- AC-5.7 The history's keys are a conversation window's, the same letters as in axClaude's conversation and
  axTerminal's terminal (`AxCalcKeys.Transcript`): **a** and Shift+A the next and the previous answer, **s** and
  Shift+S a line of the app's own, **m** a bookmark on the line and **k** and Shift+K from bookmark to bookmark
  (Ctrl+K bookmarks from the field too), **l** the line's number ("Line 2 of 4"), Backspace back to the line
  before a jump, and Ctrl+Alt with a, s or k every line of that kind from the line you are on to the end. Until
  2026-11-22 the history was the bundle's list, where a typed character found an entry by its start.

### AC-6 The field

- AC-6.1 Enter answers (AC-3); the answer is announced alone ("8294400"), interrupting, and the field is emptied for the
  next expression. Ctrl+Delete empties a field with text in it ("Cleared") and says "Empty" on an empty one (Escape
  until 2026-11-26; Escape now only goes back). Escape in the field has nothing to go back to: the edge sound, and the
  field keeps its text (silent from the move of 2026-11-26 until the fix of the same day: never silence). F7 says the last answer again ("Answer 8294400"; Ctrl+Q until
  2026-11-26). Ctrl+Shift+C in the field copies the last answer.
- AC-6.2 Up and Down in the field are the edit control's own: they never put a history entry into the field
  (the bundle's rule that arrow keys never pull text into a field); the way back to an expression is the history and
  Enter.

### AC-7 Menus, keys, texts

- AC-7.1 Menus (AX-9.1, the bundle's eight since 2026-11-11): **Session (S):** Save history… Ctrl+S | ─ | Exit Alt+F4.
  **Actions (A):** Answer Enter | Copy answer Ctrl+Shift+C | Copy answer in ▸ (Hexadecimal | Binary | Octal) | Answer in
  other bases F8 | Say the last answer F7 | ─ | Variables | Clear variables… | Clear history… Ctrl+Shift+Delete.
  **Navigation (N):** Find… Ctrl+F | Find next F3 | Find previous Shift+F3 | ─ | Next bookmark k in the history |
  Previous bookmark Shift+K in the history. **Context (C)**, what acts on the history's line: Take this line's
  expression into the field Enter in the history | Bookmark this line Ctrl+K | Copy the selection Ctrl+C in the
  history. **Options (O):** Angles ▸ (Degrees ✓ |
  Radians) | Keep history between runs ✓ | Text ▸ | General ▸ | Developer ▸. **Window (W)**, **axKit (K)** (AX-9.1). **Help (H):**
  Command palette… F1 | Keyboard shortcuts | User guide | ─ | Calculator resources ▸ (Floating point explained). Every
  item has a mnemonic (AX-7.4).
- AC-7.2 Keys (AX-8). Bundle-wide: F1 and Ctrl+Comma, the command palette with the keys (Ctrl+Shift+P until
  2026-11-26), the control keys, Ctrl+F, F3, Shift+F3, F7 (the last answer),
  Ctrl+S (the history as a
  text file), Ctrl+Plus, Ctrl+Minus, Alt+F4. Window-specific (`AxCalcKeys`): Enter (answer in the field, the line's
  expression into the field in the history), Escape (back to the field from the history; in the field the edge sound), Ctrl+Delete (clear the
  field), Ctrl+Shift+C, F8, Ctrl+K, Ctrl+Shift+Delete, and the history's keys of AC-5.7 with Ctrl+C for a selection.
  The collision test covers the table.
- AC-7.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axCalc/user-guide.md` is embedded (Help, User
  guide); About is the bundle's (AX-9.9).

### AC-8 Announcements

One UI Automation notification each (AX-7.3), on the control that has the focus or on the notice: the answer itself;
"name = value" for an assignment; "Error: <what>"; "Type an expression"; "Cleared"; "Empty"; "Answer <value>" and "No
answer yet" (F7); the bases line (AC-4.1); "Copied <value>"; "Removed"; "History cleared"; "Variables cleared";
"History, empty" and "No entry selected" (a history command with nothing to act on); "No variables" (Clear
variables with none);
"Saved <file name>"; "Degrees" and "Radians" (the Angles items); "Not found" (find); the bundle's text size and
diagnostics announcements. Nothing is spoken on an arrow key.

### AC-9 Settings

- AC-9.1 `%APPDATA%\AxKit\axCalc.json` (AX-5.3; `CalcSettings`): `fontFamily`, `fontSize`, `fontBold`,
  `checkForUpdates`, `window`, `degrees` (true), `keepHistory` (true), `history` (the entries, oldest first, at most
  500: `expression` and `answer`), `variables` (name to value). With Keep history off the file holds no history and no
  variables, and the window starts empty.
- AC-9.2 The log is `%LOCALAPPDATA%\AxKit\logs\axCalc.log`: the start, the exit and errors; never an expression or an
  answer.

## 5. Architecture

```
src/AxCalc.Core/    class library (net10.0): Calculator (the evaluator: a tokenizer and a recursive descent
                    parser over doubles, the variables, the last answer, the angle unit), NumberText (an answer's
                    text and its other bases), CalcEntry (one history line), CalcSettings; references AxKit.Core
src/AxCalc/         class library (net10.0-windows): AxCalcApp (Run, Usage), CalcForm (the window), AxCalcKeys,
                    HelpText (F1 text, the embedded guide); references AxCalc.Core, AxKit.Core, AxKit.Forms
tests/AxCalc.Tests/ CalculatorTests, NumberTextTests, CalcSettingsTests
docs/axCalc/        this spec, user-guide.md (embedded), nvda-test-plan.md
```

`CalcForm` derives from the bundle's window base, `AxKit.Forms/AxKitForm` (AX-6.5), which holds the notices, the
fonts, the window's place, find and the update flow.

## 6. Design decisions

- **AC-D1 A field and a list, no buttons.** The keyboard is the calculator; a grid of buttons would be a second way to
  type the same thing and a long Tab order. The list box is the control NVDA reads best (axGIT AG-D3).
  **Changed on 2026-11-22**: a field and a text. The bundle is consolidated onto a few window types
  (`docs/axKit/framework.md`), and a field with what was answered above it is the conversation window, the one
  axClaude and axTerminal have, so that one set of habits serves all three. What it costs: an entry is no longer
  removed alone, a typed character no longer finds an entry (Ctrl+F does), and the history's name no longer counts.
- **AC-D2 The answer once, the history newest last.** Enter speaks the answer and nothing else; the history grows at
  the bottom, like a terminal's transcript, so that Up from the end reads the latest first.
- **AC-D3 Small first.** Units, bit operators and a programmer mode are what a developer might ask for next; they are
  left until asked, so that the first version is a parser of a few hundred lines that a smaller model can change
  (B-10). Hexadecimal, binary and octal come in as literals and as a reading key, which covers most of the need.
- **AC-D4 One history in the settings file.** A calculator's history is small and personal; it lives in the settings
  file so that there is one file to read or delete, in the plain JSON every axKit file is (B-15). Two windows open at
  once each keep their own while they run, and the last one closed wins, as with every other setting.
- **AC-D5 The arrow keys stay the field's.** Up in the field recalling the last expression is what many calculators
  do; the bundle's rule is that arrows never pull text into a field (the user's, 2026-10-20), so the way back is the
  history list and Enter.

## 7. Open questions

- Units and conversions (`1.5 GB in MB`, `90 min in h`), the next likely ask.
- Bit operators (`&`, `|`, `xor`, shifts) and a word size for a programmer mode.
- Whether the digits of a long answer should be grouped for reading ("8,294,400").

## 8. Status

- 2026-10-23: built on the branch `axCalc` (AC-1 to AC-9) with its tests; not yet run under NVDA. The test plan is
  `docs/axCalc/nvda-test-plan.md`.
