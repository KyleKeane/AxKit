# axCalc: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence. The file names and the words
typed are the check's examples: use your own. Where a step names a command after its key, the key is that command's
in this version of axKit.

## Calculate and reuse earlier answers

Starts with: Open axCalc.

1. Type 1920*1080*4.
2. Press Enter.
3. Type 2+2.
4. Press Escape.
5. Press Escape.
6. Type /2.
7. Press Enter.
8. Press Ctrl+Q (Say the last answer).
9. Press Ctrl+I (Answer in other bases).
10. Press Tab.
11. Press Enter.
12. Press Tab.
13. Press Escape.
14. Press Tab.
15. Press Delete.
16. Press Ctrl+F (Find in the history).
17. Press Escape.
18. Press F1 (Keyboard shortcuts).
19. Press Escape.

## Use variables, earlier answers and the history in axCalc

Starts with: Open axCalc.

1. Press Shift+Tab.
2. Press Tab.
3. Type 1920*1080*4.
4. Press Enter.
5. Type *2.
6. Press Enter.
7. Type -3.
8. Press Enter.
9. Type ans*2.
10. Press Enter.
11. Type rate = 1.25.
12. Press Enter.
   You hear: rate = 1.25.
13. Type 400 * rate.
14. Press Enter.
15. Type sqrt(-1).
16. Press Enter.
   You hear: Error: not a real number.
17. Press Escape.
18. Type (2+3.
19. Press Enter.
   You hear: Error: missing closing parenthesis.
20. Press Escape.
21. Type foo.
22. Press Enter.
   You hear: Error: unknown name foo.
23. Press Escape.
24. Press Enter.
   You hear: Type an expression.
25. Type 7+.
26. Press Up.
27. Press Down.
28. Press Escape.
29. Type sin(30).
30. Press Enter.
31. Choose Radians in the Options menu, Angles.
32. Type sin(pi/2).
33. Press Enter.
34. Choose Degrees in the Options menu, Angles.
35. Type 2.5.
36. Press Enter.
37. Press Ctrl+I (Answer in other bases).
   You hear: 2.5 is not a whole number.
38. Press Shift+Tab.
39. Press Up.
40. Press Enter.
41. Press Backspace, Backspace, Backspace, Backspace, Backspace.
42. Type 30).
43. Press Enter.
44. Press Ctrl+F (Find in the history).
45. Type rate.
46. Press Enter.
47. Choose Variables in the Actions menu.
48. Press Escape.
49. Choose Clear variables in the Actions menu.
50. Press Enter.
   You hear: Variables cleared.
51. Press F6 (Switch between the history and the field).
52. Type 6*7.
53. Press Enter.
54. Close the window.
55. Open axCalc.
56. Press Ctrl+Q (Say the last answer).
57. Press Shift+Tab.
58. Press Ctrl+Shift+Delete (Clear history).
59. Press Escape.
60. Press Ctrl+Shift+Delete (Clear history).
61. Press Enter.
   You hear: History cleared.
62. Press Tab.
63. Type 1+1.
64. Press Enter.
65. Choose Keep history between runs in the Options menu.
66. Close the window.
67. Open axCalc.
68. Press Shift+Tab.

## Move through axCalc's history, find in it and remove an entry

Starts with: Open axCalc.

1. Type 255.
2. Press Enter.
3. Press Ctrl+I (Answer in other bases).
   You hear: 255, hex FF, binary 1111 1111, octal 377.
4. Type 10+1.
5. Press Enter.
6. Type 2.5*2.
7. Press Enter.
8. Type 10*3.
9. Press Enter.
10. Press Shift+Tab.
11. Press Up.
12. Press Up.
13. Press Up.
14. Press Up.
   You hear the sound Edge.
15. Press Ctrl+I (Answer in other bases).
   You hear: 255, hex FF, binary 1111 1111, octal 377.
16. Press Down.
17. Press Ctrl+I (Answer in other bases).
   You hear: 11, hex B, binary 1011, octal 13.
18. Press Ctrl+F (Find in the history).
19. Type 10.
20. Press Enter.
21. Press F3 (Find next).
22. Press F3 (Find next).
23. Press Shift+F3 (Find previous).
24. Press Ctrl+F (Find in the history).
25. Type zebra.
26. Press Enter.
   You hear: Not found.
27. Press Delete.
28. Press End.
29. Press Delete.
30. Type 255.
31. Press Ctrl+Shift+Delete (Clear history).
32. Press Escape.
33. Press Escape.
34. Press Ctrl+Plus (Larger text).
   You hear: Text size.
35. Press Ctrl+Minus (Smaller text).
   You hear: Text size.
36. Press Ctrl+Shift+P (Command palette).
37. Type bases.
38. Press Enter.
   You hear: 30, hex 1E, binary 1 1110, octal 36.
