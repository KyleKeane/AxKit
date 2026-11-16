# axMCP: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Start an MCP server and use its tools

Starts with: Open axMCP with the test folder.

1. Type: fake
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
11. Type: hello
12. Press Enter.
    You hear: text (required): hello.
13. Press Ctrl+Enter.
    You hear: Calling echo.
14. Press Escape.
15. Press Ctrl+J.
16. A new window opens: go to it.
17. Close the window.
18. Go to the axMCP window.
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
26. Press Left 2 times.

## Add, rename and remove a server of your own in axMCP

Starts with: Open axMCP with the test folder.

1. Type: zzproj
2. Press Tab.
3. Press Escape.
4. Press F2.
    You hear: A server of Claude Code's is renamed with claude mcp remove and claude mcp add.
5. Press Delete.
    You hear: Only Your servers are removed here; a server of Claude Code's goes with claude mcp remove.
6. Type: zzremote
7. Press Right.
    You hear: http servers need a network connection of axKit's own, which this version does not make.
8. Press Enter.
    You hear: http servers need a network connection of axKit's own, which this version does not make.
9. Press Ctrl+N.
    You hear: A server of your own: a name for the list.
10. Type: probe
11. Press Enter.
    You hear the sound Edge.
    You hear: Type the program and its arguments.
12. Type: cmd /c echo
13. Press Tab 2 times.
14. Press Shift+Tab.
15. Press Ctrl+Enter.
    You hear: Added probe.
16. Press Tab.
17. Press Down.
18. Press Escape.
19. Press Ctrl+N.
20. Type: fake
21. Press Enter.
    You hear: There is a server named fake already.
22. Press Escape.
23. Press F2.
24. Press Ctrl+A.
25. Type: probe two
26. Press Enter.
    You hear: Renamed to probe two.
27. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: renamed probe to probe two.
28. Press Delete.
29. Press Escape.
30. Press Delete.
31. Press Enter.
    You hear: Removed probe.
32. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: removed probe.
33. Press Ctrl+Z (Undo; Actions menu).
    You hear: Nothing to undo.
34. Type: fake
35. Press Right.
    You hear: fake, running, Tools 4, Resources 2, Prompts 1.
36. Press Ctrl+N.
    You hear: Servers are added at the top level.
37. Press Ctrl+J.
    You hear: No result yet.
38. Press Right.
    You hear: Tools, 4 tools, echo, tool, Says the text back.
39. Press Tab.
40. Press Down 2 times.
41. Press Space.
    You hear: loud: done.
42. Press Up.
43. Press Enter.
44. Type: hello
45. Press Enter.
    You hear: text (required): hello.
46. Press Ctrl+Enter.
    You hear: Calling echo.
47. Press Escape 2 times.
48. Press Down.
49. Press Ctrl+Enter.
    You hear: Calling fail.
50. Press Escape.
51. Press Down.
52. Press Ctrl+Enter.
    You hear: Calling boom.
53. Press Escape.
54. Press Down.
55. Press Ctrl+Enter.
    You hear: Calling snap.
56. Press Escape.
57. Press Left.
58. Press Down.
59. Press Right.
    You hear: Resources, 2 resources, readme, resource, fake://readme.
60. Press Down.
61. Press Enter 2 times.
    You hear: Reading page.
62. Press Escape.
63. Press Left.
64. Press Down.
65. Press Right.
    You hear: Prompts, 1 prompt, greet, prompt, Greets someone.
66. Press Tab.
67. Press Down.
68. Press Ctrl+Enter.
    You hear: who is required.
69. Press Enter.
70. Type: Ada
71. Press Enter.
    You hear: who (required): Ada.
72. Press Ctrl+Enter.
    You hear: Fetching greet.
73. Press Escape.
