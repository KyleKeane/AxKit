# NVDA only: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Hear an offered command as NVDA reads it

Starts with: Open axClaude with the folder heard.

You hear: trust.
1. Press Enter.
    You hear: Ready.
2. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
3. Press Enter.
    You hear: AI tools on.
    You may hear: Not in a test run.
4. Call the tool axkit_windows.
5. Press Tab.
6. Press Space.
    You hear: checked.
7. Press Tab.
8. Press Enter.
    You hear: axAutomate may read axKit.
    The tool answers with app: axClaude.
9. Call the tool axkit_offer_command.
    The tool answers with offered: true.
    You hear the sound Offer.
    NVDA says: Command offer: it runs on your computer, outside axKit.
    NVDA says: Explain this command.
10. Press Down.
    NVDA says: Yes, run it.
11. Press Down.
    NVDA says: No, do not run it.
12. Press Down.
    NVDA says: No, copy it to the clipboard instead.
13. Press Down.
    NVDA says: No, and report a concern about it.
14. Press Enter.
    NVDA says: Type what concerns you about this command.
15. Press Escape.
    NVDA says: offers a command for you to run: echo heard-by-ear.
16. Press Home.
    NVDA says: Explain this command.
17. Press Enter.
    NVDA says: Command offer: about this command.
18. Press Escape.
    NVDA says: offers a command for you to run: echo heard-by-ear.
19. Press Ctrl+2 (Conversation; Navigation menu).
20. Press Ctrl+1 (Message field; Navigation menu).
    NVDA says: offers a command for you to run: echo heard-by-ear.
21. Press Ctrl+Q (Claude's question again; Navigation menu).
    NVDA says: Its reason: A check by ear.
22. Press Down.
    NVDA says: Yes, run it.
23. Press Enter.
    You hear: Sent.
24. Press Ctrl+2 (Conversation; Navigation menu).

## Read a long reply with Say all while Claude writes

Starts with: Open axClaude with the folder read.

You hear: trust.
1. Press Enter.
    You hear: Ready.
2. Type: Write a numbered list of 40 lines, each naming a different vegetable with one full sentence about it, numbered 1 to 40, nothing else.
3. Press Enter.
    You hear: Done.
4. Type: Now a numbered list of 120 lines, each naming a different fruit with one full sentence about it, numbered 1 to 120, nothing else.
5. Press Enter.
6. Press Ctrl+2 (Conversation; Navigation menu).
7. Press Ctrl+Home.
8. Press NVDA+A.
    NVDA says: Input 1.
9. Press Ctrl+End.

## Come back to the right place after a Windows dialog

Starts with: Open axDown with the file notes.md.

NVDA says: notes.md.
1. Press Ctrl+O (Open; Session menu).
2. Press Escape.

## Hear axDown's notices and palette with NVDA

Starts with: Open axDown with the file notes.md.

NVDA says: notes.md.
1. Press F1 (Keyboard shortcuts; Help menu).
    NVDA says: Keyboard shortcuts.
2. Press Escape.
    NVDA says: notes.md.
3. Press Ctrl+Shift+P (Command palette; Help menu).
4. Type: save as
    NVDA says: Save as.
5. Press Escape.
    NVDA says: notes.md.

## Find a name backwards with Shift and a letter

Starts with: Open axFile with the folder f.

### Go back to the name before with Shift and its letter

1. Press End.
2. Press Shift+C 2 times.

## Put a template into another window from axTemplate's launcher, with NVDA

Starts with: Open axDown with the file notes.md.

### Put a template in where the caret was

1. Press Shift+End.
2. Press Ctrl+C.
3. Press End.
4. Press Enter.
5. Start axTemplate again with --launcher.
6. A new window opens: go to it.
    NVDA says: Search templates.
7. Type: thanks
8. Press Down.
    NVDA says: Thanks, 1 of 1.
9. Press Enter.
10. Go to the axDown window.
11. Type: friend

### Put a template in around what is selected

1. Press Enter.
2. Type: word
3. Press Shift+Home.
4. Start axTemplate again with --launcher.
5. Go to the other window.
    NVDA says: Search templates.
6. Type: link
7. Press Down.
    NVDA says: Link, 1 of 1.
8. Press Ctrl+Enter.
9. Go to the axDown window.
10. Type: address

## Hear the caution of a command another program put into the command field

Starts with: Open axTerminal with the folder myproject --command Write-Output "two words".

### Hear that another program put the command there, before the command, and again on coming back to the field

NVDA says: Command, put here by another program and not sent. axKit checked nothing of it: read it, and press Enter only if you want to run it.
You hear the sound Ready.
1. Press Alt+2 (Terminal; Navigation menu).
2. Press Alt+1 (Command field; Navigation menu).
    NVDA says: Command, put here by another program and not sent.
