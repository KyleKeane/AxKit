# The components and the window types

Written by `tools/components.ps1` from `docs/axKit/components.json`; never edit it by hand. The design is
`docs/axKit/framework.md`: every window of axKit is one of a few types, and each type is made only of the
pieces below. Learn a piece once and it works the same wherever it appears.

## The components

### The list

- What: Rows you move through with the arrow keys. Every list of every window is this one.
- Built into it: Type the start of a name to jump to it; the edge sound at the first and the last row; rename in place with F2 where rows have names.
- Selecting: With src/AxKit.Forms/ListSelection.cs: Space selects the row you are on, Shift with an arrow selects as it moves, Ctrl+A all, Escape none (SPEC AX-8.16). On in axFile's folder list, axTask's navigator and axMedia's queue so far.
- Its sound: three quick beeps up, then four quick beeps going down (the kind of event `list-entered`, SPEC AX-7.11). It plays whenever the focus enters it from outside; moving inside it is silent. Then: NVDA reads the row; a selected row ends in ', selected'.
- Its keys, by family (`docs/axKit/key-groups.md`): moves, letter, space, shift-select, enter, delete, escape.
- The code: `AxKitList`, `src/AxKit.Forms/AxKitList.cs`.

### The text to read

- What: Text you read and do not change: a conversation, a notice's text, a file shown read-only. The conversation's text is this class; a notice's text and a read-only file are the text box below, set read-only.
- Built into it: The caret moves only on your own key. Letters jump by heading and the like; Ctrl+F finds.
- Its sound: three quick beeps up, then one low long tone (the kind of event `text-entered`, SPEC AX-7.11). It plays whenever the focus enters it from outside; moving inside it is silent. Then: NVDA reads from the caret.
- Its keys, by family (`docs/axKit/key-groups.md`): moves, letter, ctrl-arrows.
- The code: `TranscriptView`, `src/AxKit.Forms/TranscriptView.cs`.

### The field

- What: Text you type, on one line or several: a message, a command, a document, a name.
- Built into it: Ctrl+Space switches between editing and navigating; a line break typed is said, 'new line'.
- Its sound: three quick beeps up, then one long tone (the kind of event `field-entered`, SPEC AX-7.11). It plays whenever the focus enters it from outside; moving inside it is silent. Then: NVDA reads the field's name and what it holds.
- Its keys, by family (`docs/axKit/key-groups.md`): moves, space, ctrl-arrows, enter.
- The code: `AxKitTextBox`, `src/AxKit.Forms/AxKitTextBox.cs`.

### The choice in the field's place

- What: What a window waits on, shown where its field is: a list of answers, or a small field. Never a second window.
- Built into it: The number keys choose an answer; Enter and Escape answer.
- Its sound: three quick beeps up, then four quick beeps going down (the kind of event `list-entered`, SPEC AX-7.11). It plays whenever the focus enters it from outside; moving inside it is silent. Then: The question, then NVDA reads the answer you are on.
- Its keys, by family (`docs/axKit/key-groups.md`): moves, enter, escape.
- The code: `ControlArea`, `src/AxKit.Forms/ControlArea.cs`.

### The form

- What: Several standard controls for one action, in Tab order, each named with an instruction.
- Built into it: Tab between the controls; Ctrl+Enter or Enter does it; Escape leaves and nothing is changed. Ctrl+Q reads its explanation.
- Its sound: three quick beeps up, then beeps and tones together (the kind of event `form-entered`, SPEC AX-7.11). It plays whenever the focus enters it from outside; moving inside it is silent. Then: After NVDA has read the first control: the title and the word 'form'.
- Its keys, by family (`docs/axKit/key-groups.md`): ctrl-tab, enter, enter-forms, escape.
- The code: `FormControls`, `src/AxKit.Forms/FormControls.cs`.

### The notice

- What: A text with its buttons over the window: a help text, a question, an error. It holds one of the pieces above when it asks for more.
- Built into it: Enter and Escape press its buttons from anywhere in it; Ctrl+Q reads its text again.
- Its sound: three quick beeps up, then one low long tone (the kind of event `text-entered`, SPEC AX-7.11). It plays whenever the focus enters it from outside; moving inside it is silent. Then: NVDA reads the text.
- Its keys, by family (`docs/axKit/key-groups.md`): enter, escape, letter.
- The code: `OverlayPanel`, `src/AxKit.Forms/OverlayPanel.cs`.

## The window types

### The conversation window

- What: For a program you talk to in turns: what was said above, and under it the field, or the choice that stands in its place.
- Made of: the text to read; the field; the choice in the field's place.
- Moving: Tab and the other pane keys switch between the text and the field. Escape in the text goes to the field. Enter on a line of the text takes it into the field.
- Apps on it: axTerminal, axClaude.
- To move onto it: axCalc.
- The code: `ConsoleForm`, `src/AxKit.Forms/ConsoleForm.cs`.

### The editor

- What: For one text you write or read: the text, with a choice under it for Go to line and for the options of a place; read-only, a list of details under it.
- Made of: the field; the choice in the field's place; the list.
- Moving: Ctrl+Tab and F6 switch panes; Tab types a tab while editing.
- Apps on it: axDown, axJSON, axPreview.
- The code: `TextEditorForm`, `src/AxKit.Forms/TextEditorForm.cs`.

### The navigator

- What: For things in levels: a list of rows you go into and out of, and beside it more of the row you are on.
- Made of: the list; the list.
- Moving: Right goes into a row that holds a level; Left and Backspace go up. Enter opens the row. Tab and the other pane keys switch to the second pane; Escape comes back.
- Apps on it: axTask, axAutomate, axPass, axMCP.
- To move onto it: axFile, axMedia, axProcess, axGIT.
- The code: `ItemForm`, `src/AxKit.Forms/ItemForm.cs`.

### The hotkey window

- What: For something called from anywhere in Windows with a key: a field that always holds the caret, and under it a list the arrow keys move through from the field.
- Made of: the field; the list.
- Moving: Down and Up move through the list while you type. Enter does the app's thing with the row. Escape hides the window.
- Apps on it: axStart, axClip, axHelp.
- The code: `HotkeyForm`, `src/AxKit.Forms/HotkeyForm.cs`.

## The apps

17 apps: 12 on a type, 5 with a window of their own still to move.

| App | Type today | To be | Note |
|---|---|---|---|
| axFile | a window of its own | navigator | A window of its own today. The largest move, and the last. |
| axDown | editor | editor |  |
| axJSON | editor | editor |  |
| axPreview | editor | editor | Read-only, with the list of details. |
| axMedia | a window of its own | navigator | A window of its own today. To be a queue and its controls. |
| axTask | navigator | navigator |  |
| axStart | hotkey | hotkey |  |
| axClip | hotkey | hotkey |  |
| axCalc | a window of its own | conversation | A window of its own today. |
| axProcess | a window of its own | navigator | A window of its own today. Its three views to be three levels. |
| axAutomate | navigator | navigator |  |
| axTerminal | conversation | conversation |  |
| axGIT | a window of its own | navigator | A window of its own today. Its three views to be three levels. |
| axPass | navigator | navigator |  |
| axClaude | conversation | conversation |  |
| axMCP | navigator | navigator |  |
| axHelp | hotkey | hotkey | A text and no list. |
