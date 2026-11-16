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
2. Choose the menu item Let AI tools read and open axKit.
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
19. Press Ctrl+2.
    NVDA says: Conversation.
20. Press Ctrl+1.
    NVDA says: offers a command for you to run: echo heard-by-ear.
21. Press Ctrl+Q.
    NVDA says: Its reason: A check by ear.
22. Press Down.
    NVDA says: Yes, run it.
23. Press Enter.
    You hear: Sent.
24. Press Ctrl+2.

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
6. Press Ctrl+2.
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
