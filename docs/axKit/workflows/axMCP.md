# axMCP: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence. The file names and the words
typed are the check's examples: use your own. Where a step names a command after its key, the key is that command's
in this version of axKit.

## Start an MCP server and use its tools

Starts with: Open axMCP with the test folder.

1. Type fake.
2. Press Tab.
3. Press Down.
4. Press Escape.
5. Press Right.
   You hear: fake, starting.
   You hear: fake, running, Tools 1, Resources 1, Prompts 0.
6. Press Right.
   You hear: Tools, 1 tool, echo, tool, Says the text back.
7. Press Tab.
8. Press Down.
9. Press Ctrl+Enter.
   You hear: text is required.
10. Press Enter.
11. Type hello.
12. Press Enter.
   You hear: text (required): hello.
13. Press Ctrl+Enter.
   You hear: Calling echo.
14. Press Escape.
15. Press Ctrl+J.
16. Take the window the app opened.
17. Close the window.
18. Use window 1.
19. Press Escape.
20. Press Left.
21. Press Down.
22. Press Right.
   You hear: Resources, 1 resource, readme, resource, fake://readme.
23. Press Enter.
   You hear: Reading readme.
24. Press Escape.
25. Press Ctrl+Shift+S.
   You hear: Stopped fake.
26. Press Left.
27. Press Left.

## Add, rename and remove a server of your own in axMCP

Starts with: Open axMCP with the test folder.

1. Type zzproj.
2. Press Tab.
3. Press Escape.
4. Press F2.
   You hear: A server of Claude Code's is renamed with claude mcp remove and claude mcp add.
5. Press Delete.
   You hear: Only Your servers are removed here; a server of Claude Code's goes with claude mcp remove.
6. Type zzremote.
7. Press Right.
   You hear: http servers need a network connection of axKit's own, which this version does not make.
8. Press Enter.
   You hear: http servers need a network connection of axKit's own, which this version does not make.
9. Press Ctrl+N.
   You hear: A server of your own: a name for the list.
10. Type probe.
11. Press Enter.
   You hear the sound Edge.
   You hear: Type the program and its arguments.
12. Type cmd /c echo.
13. Press Tab.
14. Press Tab.
15. Press Shift+Tab.
16. Press Ctrl+Enter.
   You hear: Added probe.
17. Press Tab.
18. Press Down.
19. Press Escape.
20. Press Ctrl+N.
21. Type fake.
22. Press Enter.
   You hear: There is a server named fake already.
23. Press Escape.
24. Press F2.
25. Press Ctrl+A.
26. Type probe two.
27. Press Enter.
   You hear: Renamed to probe two.
28. Press Ctrl+Z (Undo).
   You hear: Undone: renamed probe to probe two.
29. Press Delete.
30. Press Escape.
31. Press Delete.
32. Press Enter.
   You hear: Removed probe.
33. Press Ctrl+Z (Undo).
   You hear: Undone: removed probe.
34. Press Ctrl+Z (Undo).
   You hear: Nothing to undo.
35. Type fake.
36. Press Right.
   You hear: fake, running, Tools 4, Resources 2, Prompts 1.
37. Press Ctrl+N.
   You hear: Servers are added at the top level.
38. Press Ctrl+J.
   You hear: No result yet.
39. Press Right.
   You hear: Tools, 4 tools, echo, tool, Says the text back.
40. Press Tab.
41. Press Down.
42. Press Down.
43. Press Space.
   You hear: loud: done.
44. Press Up.
45. Press Enter.
46. Type hello.
47. Press Enter.
   You hear: text (required): hello.
48. Press Ctrl+Enter.
   You hear: Calling echo.
49. Press Escape.
50. Press Escape.
51. Press Down.
52. Press Ctrl+Enter.
   You hear: Calling fail.
53. Press Escape.
54. Press Down.
55. Press Ctrl+Enter.
   You hear: Calling boom.
56. Press Escape.
57. Press Down.
58. Press Ctrl+Enter.
   You hear: Calling snap.
59. Press Escape.
60. Press Left.
61. Press Down.
62. Press Right.
   You hear: Resources, 2 resources, readme, resource, fake://readme.
63. Press Down.
64. Press Enter.
65. Press Enter.
   You hear: Reading page.
66. Press Escape.
67. Press Left.
68. Press Down.
69. Press Right.
   You hear: Prompts, 1 prompt, greet, prompt, Greets someone.
70. Press Tab.
71. Press Down.
72. Press Ctrl+Enter.
   You hear: who is required.
73. Press Enter.
74. Type Ada.
75. Press Enter.
   You hear: who (required): Ada.
76. Press Ctrl+Enter.
   You hear: Fetching greet.
77. Press Escape.
