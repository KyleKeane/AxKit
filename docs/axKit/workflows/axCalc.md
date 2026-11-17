# axCalc: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Calculate and reuse earlier answers

Starts with: Open axCalc.

### Calculate an expression

1. Type: 1920*1080*4
2. Press Enter.
    You hear: 8294400.

### Empty the field

1. Type: 2+2
2. Press Escape.
    You hear: Cleared.
3. Press Escape.
    You hear: Empty.

### Go on from the last answer

1. Type: /2
2. Press Enter.
    You hear: 4147200.

### Hear the last answer in other bases

1. Press Ctrl+Q (Say the last answer; Actions menu).
    You hear: 4147200.
2. Press Ctrl+I (Answer in other bases; Actions menu).
    You hear: hex.

### Put an earlier expression back in the field

1. Press Tab.
2. Press Enter.

### Remove an entry from the history

1. Press Tab.
2. Press Escape.
3. Press Tab.
4. Press Delete.
    You hear: Removed.

### Read the keyboard shortcuts

1. Press F1 (Keyboard shortcuts; Help menu).
2. Press Escape.

## Use variables, earlier answers and the history in axCalc

Starts with: Open axCalc.

### Go on from the last answer and use ans

1. Type: 1920*1080*4
2. Press Enter.
    You hear: 8294400.
3. Type: *2
4. Press Enter.
    You hear: 16588800.
5. Type: -3
6. Press Enter.
    You hear: -3.
7. Type: ans*2
8. Press Enter.
    You hear: -6.

### Set a variable and use it

1. Type: rate = 1.25
2. Press Enter.
    You hear: rate = 1.25.
3. Type: 400 * rate
4. Press Enter.
    You hear: 500.

### Switch angles between degrees and radians

1. Type: sin(30)
2. Press Enter.
    You hear: 0.5.
3. Choose Radians in the Options menu, Angles.
4. Type: sin(pi/2)
5. Press Enter.
    You hear: 1.
6. Choose Degrees in the Options menu, Angles.

### Change an entry of the history and answer it again

1. Press Shift+Tab.
2. Press Up.
3. Press Enter.
4. Press Backspace 5 times.
5. Type: 30)
6. Press Enter.
    You hear: 0.5.

### Find an entry in the history

1. Press Ctrl+F (Find in the history).
2. Type: rate
3. Press Enter.

### See the variables and clear them

1. Choose Variables in the Actions menu.
2. Press Escape.
3. Choose Clear variables in the Actions menu.
4. Press Enter.
    You hear: Variables cleared.

### Clear the history

1. Press Ctrl+Shift+Delete (Clear history; Actions menu).
2. Press Escape.
3. Press Ctrl+Shift+Delete (Clear history; Actions menu).
4. Press Enter.
    You hear: History cleared.

### Stop keeping the history between runs

1. Press Tab.
2. Type: 1+1
3. Press Enter.
    You hear: 2.
4. Choose Keep history between runs in the Options menu.
5. Close the window.
6. Open axCalc.
7. Press Shift+Tab.

## Move through axCalc's history, find in it and remove an entry

Starts with: Open axCalc.

### Hear the last answer in other bases

1. Press Ctrl+I (Answer in other bases; Actions menu).
    You hear: 255, hex FF, binary 1111 1111, octal 377.
2. Type: 10+1
3. Press Enter.
    You hear: 11.
4. Type: 2.5*2
5. Press Enter.
6. Type: 10*3
7. Press Enter.
    You hear: 30.

### Move through the history one entry at a time

1. Press Shift+Tab.
2. Press Up 3 times.

### Hear the answer of an entry in other bases

1. Press Ctrl+I (Answer in other bases; Actions menu).
    You hear: 255, hex FF, binary 1111 1111, octal 377.
2. Press Down.
3. Press Ctrl+I (Answer in other bases; Actions menu).
    You hear: 11, hex B, binary 1011, octal 13.

### Find the next and the previous match in the history

1. Press Ctrl+F (Find in the history).
2. Type: 10
3. Press Enter.
4. Press F3 2 times (Find next).
5. Press Shift+F3 (Find previous).

### Remove entries from the history

1. Press Delete.
    You hear: Removed.
2. Press End.
3. Press Delete.
    You hear: Removed.

### Find an entry by typing its first characters

1. Type: 255

### Go back to the field from the history

1. Press Escape.

### Change the text size

1. Press Ctrl+Plus (Larger text; Options menu, Text).
    You hear: Text size.
2. Press Ctrl+Minus (Smaller text; Options menu, Text).
    You hear: Text size.

### Hear the bases from the command palette

1. Press Ctrl+Shift+P (Command palette; Help menu).
2. Type: bases
3. Press Enter.
    You hear: 30, hex 1E, binary 1 1110, octal 36.

## Use powers, factorials, remainders and other number forms

Starts with: Open axCalc.

### Raise a number to a power

1. Type: 2^10
2. Press Enter.
    You hear: 1024.
3. Type: 2**3
4. Press Enter.
    You hear: 8.

### Take a factorial

1. Type: 5!
2. Press Enter.
    You hear: 120.

### Take a remainder

1. Type: 17 mod 5
2. Press Enter.
3. Type: 17%5
4. Press Enter.
    You hear: 2.

### Round to a number of places

1. Type: round(3.14159, 2)
2. Press Enter.
    You hear: 3.14.

### Type numbers with separators, in hexadecimal and in binary

1. Type: 1_000_000+1
2. Press Enter.
    You hear: 1000001.
3. Type: 0xFF+0b1010
4. Press Enter.
    You hear: 265.
