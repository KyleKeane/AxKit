# axJSON: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence. The file names and the words
typed are the check's examples: use your own. Where a step names a command after its key, the key is that command's
in this version of axKit.

## Open a JSON file in axJSON

Starts with: Open axJSON. Open axJSON with settings.json. Open settings.json. Use window 2.

1. Press Down.
2. Type X.
3. Press Ctrl+S (Save).
4. Press Tab, Tab.
5. Press Enter.

## Move through a JSON file by its structure

Starts with: Open axJSON with settings.json.

1. Press Ctrl+K (Key, Navigation, Next).
   You hear: model: opus.
2. Press Ctrl+K (Key, Navigation, Next).
   You hear: permissions, object with 2 keys.
3. Press Ctrl+K (Key, Navigation, Next).
   You hear: allow, array with 2 items.
4. Press Ctrl+K (Key, Navigation, Next).
   You hear: deny, empty array.
5. Press Ctrl+K (Key, Navigation, Next).
   You hear: count: 3.
6. Press Ctrl+K (Key, Navigation, Next).
   You hear: on: true.
7. Press Ctrl+K (Key, Navigation, Next).
   You hear: No next key.
8. Press Ctrl+Shift+K (Key, Navigation, Previous).
   You hear: count: 3.
9. Press Ctrl+Shift+K (Key, Navigation, Previous).
   You hear: deny, empty array.
10. Press Ctrl+Shift+K (Key, Navigation, Previous).
   You hear: allow, array with 2 items.
11. Press Ctrl+Shift+K (Key, Navigation, Previous).
   You hear: permissions, object with 2 keys.
12. Press Ctrl+Shift+K (Key, Navigation, Previous).
   You hear: model: opus.
13. Press Ctrl+Shift+K (Key, Navigation, Previous).
   You hear: No previous key.
14. Press Ctrl+Home, Ctrl+J.
   You hear: permissions, object with 2 keys.
15. Press Ctrl+J (Object, Navigation, Next).
   You hear: No next object.
16. Press Ctrl+Shift+J (Object, Navigation, Previous).
   You hear: object with 4 keys.
17. Press Ctrl+Home, Ctrl+L.
   You hear: allow, array with 2 items.
18. Press Ctrl+L (Array, Navigation, Next).
   You hear: deny, empty array.
19. Press Ctrl+Home, Ctrl+I.
   You hear: Item 1 of 2, Bash.
20. Press Ctrl+I (Item, Navigation, Next).
   You hear: Item 2 of 2, Read.
21. Press Ctrl+I (Item, Navigation, Next).
   You hear: No next item.
22. Press Ctrl+Home, Ctrl+E.
   You hear: No next error.
23. Press Down, Down, Right, Right, Left, Up, End, Home.
24. Press Ctrl+Home.
25. Choose Key in the Navigation menu, Next (key: Ctrl+K).
   You hear: model: opus.
26. Choose Object in the Navigation menu, Next (key: Ctrl+J).
   You hear: permissions, object with 2 keys.
27. Choose Array in the Navigation menu, Next (key: Ctrl+L).
   You hear: allow, array with 2 items.
28. Choose Item in the Navigation menu, Next (key: Ctrl+I).
   You hear: Item 1 of 2, Bash.
29. Choose Item in the Navigation menu, Previous (key: Ctrl+Shift+I).
   You hear: No previous item.
30. Choose Array in the Navigation menu, Previous (key: Ctrl+Shift+L).
   You hear: allow, array with 2 items.
31. Choose Object in the Navigation menu, Previous (key: Ctrl+Shift+J).
   You hear: permissions, object with 2 keys.
32. Choose Object in the Navigation menu, Previous (key: Ctrl+Shift+J).
   You hear: object with 4 keys.
33. Choose Key in the Navigation menu, Previous (key: Ctrl+Shift+K).
   You hear: No previous key.
34. Choose Error in the Navigation menu, Next (key: Ctrl+E).
   You hear: No next error.
35. Choose Error in the Navigation menu, Previous (key: Ctrl+Shift+E).
   You hear: No previous error.
36. Choose User guide in the Help menu.
37. Press Ctrl+K (Key, Navigation, Next).
   You hear: No next key.
38. Press Escape.
39. Open axJSON with broken.json.
40. Press Ctrl+Home, Ctrl+E.
   You hear: No next error.
41. Open axJSON with missing-value.json.
42. Press Ctrl+Home, Ctrl+E.
   You hear: Error, line 4: expected a value, found ','.
43. Press Ctrl+End, Ctrl+Shift+E.
   You hear: Error, line 4: expected a value, found ','.
44. Use window 1.
45. Press Ctrl+Home, Ctrl+J.
   You hear: permissions, object with 2 keys.
46. Press Ctrl+Shift+J (Object, Navigation, Previous).
   You hear: object with 4 keys.

## Format a JSON file

Starts with: Open axJSON with one-line.json.

1. Press Ctrl+Shift+F (Format).
2. Press Ctrl+Home.
3. Press Down.
4. Open axJSON with broken.json.
5. Press Ctrl+Shift+F (Format).
   You hear: Formatted, repaired: 1 trailing comma, 1 missing comma.
6. Press Ctrl+E (Error, Navigation, Next).
   You hear: No next error.
7. Press Ctrl+Home, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K, Ctrl+K.
   You hear: on: true.
8. Press End.
9. Type , "q": 'x'.
10. Press Ctrl+Shift+F (Format).
   You hear: Formatted, repaired: 1 single-quoted string.
11. Open axJSON with missing-value.json.
12. Press Ctrl+Shift+F (Format).
   You hear: Not valid JSON: line 4, expected a value, found ','.
13. Choose Format in the Actions menu (key: Ctrl+Shift+F).
   You hear: Not valid JSON: line 4, expected a value, found ','.
14. Open axJSON with formatted.json.
15. Press Ctrl+Shift+F (Format).
   You hear: Already formatted.
16. Press Ctrl+Z (Undo).
   You hear: Nothing to undo.
17. Use window 1.
18. Press Ctrl+Z (Undo).
19. Press Ctrl+Y (Redo).
20. Press Ctrl+End.
21. Type abc.
22. Type def.
23. Press Ctrl+Z (Undo).
24. Press Ctrl+Z (Undo).
25. Press Ctrl+Z (Undo).
26. Press Ctrl+Z (Undo).
   You hear: Nothing to undo.
27. Press Ctrl+Y, Ctrl+Y, Ctrl+Y.
28. Press Ctrl+Y (Redo).
   You hear: Nothing to redo.

## Use axJSON's axKit menu and About

Starts with: Open axJSON with settings.json.

1. Choose Version in the Help menu, About.
2. Press Escape.

## Use axJSON's Context menu

Starts with: Open axJSON with settings.json.

1. Choose Select all in the Context menu (key: Ctrl+A).
2. Press Right.
3. Press Ctrl+Shift+P (Command palette).
4. Type select all, context.
   You hear: Select all, Context, Ctrl+A.
5. Press Enter.
6. Press Left.
