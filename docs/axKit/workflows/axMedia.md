# axMedia: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Build and move through a play queue

Starts with: Open axMedia.

1. Press Enter.
    You hear: Queue is empty.
2. Press Delete.
    You hear: Queue is empty.
3. Press Ctrl+I.
    You hear: Queue is empty.
4. Press Ctrl+Q (What is playing; Actions menu).
    You hear: Nothing playing.
5. Press Space.
    You hear: Nothing playing.
6. Close the window.
7. Open axMedia with the folder empty.
    You hear: No sound or video files in empty.
8. Close the window.
9. Open axMedia with the folder ch.
    You hear: Playing alpha.wav.
10. Press Space.
    You hear words such as: Paused at.
11. Press Down.
    It reads beta.wav.
12. Press End.
    It reads gamma.wav.
13. Press Home.
    It reads alpha.wav (playing).
14. Type: g
    It reads gamma.wav.
15. Type: b
    It reads beta.wav.
16. Press Tab.
17. Press Shift+Tab.
18. Press F6 (Back to the list).
19. Press Ctrl+Tab (Back to the list).
20. Press Ctrl+F (Find in the queue).
21. Type: elt
22. Press Enter.
23. Press Ctrl+F (Find in the queue).
24. Type: ta.w
25. Press Enter.
26. Press F3 2 times (Find next).
27. Press Ctrl+F (Find in the queue).
28. Type: zzz
29. Press Enter.
    You hear: Not found.
30. Press Home.
31. Press Down.
32. Press Enter.
    You hear: Playing beta.wav.
33. Press Down.
34. Press Up.
35. Press Ctrl+I.
    You hear: beta.wav, 2 of 4, playing, 0:0.
36. Press Down.
37. Press Ctrl+I.
    You hear: delta.wav, 3 of 4.
38. Press Delete.
    You hear: Removed delta.wav.
39. Press Up.
40. Press Delete.
    You hear: Removed beta.wav.
    You hear: Playing gamma.wav.
41. Choose Clear queue in the Session menu.
    You hear: Queue cleared.
42. Press Ctrl+Q (What is playing; Actions menu).
    You hear: Nothing playing.

## Add files to the queue from another app

Starts with: Open axMedia with --add the folder ch.

1. Press Ctrl+Q (What is playing; Actions menu).
    You hear: Nothing playing.
2. Press Ctrl+I.
    You hear: alpha.wav, 1 of 3.
3. Close the window.
4. Open axMedia with the file ch\beta.wav.
    You hear: Playing beta.wav.
5. Start axMedia again with the file ch\alpha.wav.
    You hear: Playing alpha.wav.
6. Start axMedia again with --add the folder ch.
7. Open axMedia with --new the file ch\gamma.wav.
    You hear: Playing gamma.wav.
8. Go to the other axMedia window.

## Play, pause and seek

Starts with: Open axMedia with the folder p.

You hear: Playing first.wav.
1. Press Space.
    You hear: Paused at 0:0.
2. Press Space.
    You hear: Playing.
3. Press Ctrl+P (Pause or resume; Actions menu).
    You hear: Paused at 0:0.
4. Press Ctrl+P (Pause or resume; Actions menu).
    You hear: Playing.
5. Press Ctrl+T (Go to time; Navigation menu).
6. Press Ctrl+P (Pause or resume; Actions menu).
7. Press Escape.
8. Press Space.
    You hear: Paused at 0:0.
9. Press Ctrl+Right (Forward 15 seconds; Actions menu).
    You hear: 0:1.
10. Press Ctrl+Left (Back 15 seconds; Actions menu).
    You hear: 0:0.
11. Press Ctrl+T (Go to time; Navigation menu).
12. Type: 0:30
13. Press Enter.
    You hear: 0:30.
14. Press Space.
    You hear: Playing.
15. Press Ctrl+Up (Faster; Actions menu).
    You hear: Speed 1.25.
16. Press Ctrl+Up (Faster; Actions menu).
    You hear: Speed 1.5.
17. Choose Faster in the Actions menu (key: Ctrl+Up).
    You hear: Speed 1.75.
18. Press Ctrl+Down (Slower; Actions menu).
    You hear: Speed 1.5.
19. Choose Slower in the Actions menu (key: Ctrl+Down).
    You hear: Speed 1.25.
20. Press Ctrl+Up (Faster; Actions menu).
    You hear: Speed 1.5.
21. Choose Normal speed in the Actions menu.
    You hear: Speed 1.
22. Choose Faster in the Actions menu (key: Ctrl+Up).
23. Choose Faster in the Actions menu (key: Ctrl+Up).
    You hear: Speed 1.5.
24. Press Ctrl+Q (What is playing; Actions menu).
    You hear: Playing first.wav, 0:3.
25. Press Ctrl+Q (What is playing; Actions menu).
    You hear: of 1:00, speed 1.5.
26. Press Ctrl+Shift+Right (Next file; Navigation menu).
    You hear: Playing second.wav.
27. Press Ctrl+Shift+Left (Previous file; Navigation menu).
    You hear: Playing first.wav.
28. Press Ctrl+Shift+Left (Previous file; Navigation menu).
    You hear: First file.
29. Press Ctrl+Shift+Right (Next file; Navigation menu).
    You hear: Playing second.wav.
30. Press Ctrl+T (Go to time; Navigation menu).
31. Type: 9:00
32. Press Enter.
    You hear: 1:00.
    You hear: Playing third.wav.
33. Press Ctrl+Shift+Right (Next file; Navigation menu).
    You hear: End of queue.
34. Press Ctrl+T (Go to time; Navigation menu).
35. Type: 0:50
36. Press Enter.
    You hear: 0:50.
37. Press Ctrl+Right (Forward 15 seconds; Actions menu).
    You hear: 1:00.
    You hear the sound Done.
    You hear: End of queue.
38. Press Ctrl+Q (What is playing; Actions menu).
    You hear: Nothing playing.
39. Close the window.
40. Open axMedia with the folder q.
    You hear: Playing s1.wav.
41. Press Ctrl+Q (What is playing; Actions menu).
    You hear words such as: speed 1.5.
42. Choose Normal speed in the Actions menu.
    You hear: Speed 1.
    You hear: Playing s2.wav.
43. Choose Say each file as it starts in the Options menu.
    You hear the sound Done.
    You hear: End of queue.
44. Press Ctrl+Q (What is playing; Actions menu).
    You hear: Nothing playing.
45. Close the window.
46. Open axMedia with the folder p.
    You hear: Playing first.wav.
47. Press Ctrl+T (Go to time; Navigation menu).
48. Type: abc
49. Press Enter.
    You hear: Not a time.
50. Press Escape.

## Hear why a file cannot be played

Starts with: Open axMedia with the folder c.

You hear words such as: Cannot play a-broken.mp3.
You hear: Playing b-good.wav.
1. Press Home.
2. Press Enter.
    You hear words such as: Cannot play a-broken.mp3.
    You hear: Playing b-good.wav.

## Use axMedia's text, notices and axKit menu

Starts with: Open axMedia with the folder ch.

You hear: Playing alpha.wav.
1. Press Space.
    You hear words such as: Paused at.
2. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
    You hear: Text size.
3. Choose Smaller text in the Options menu, Text (key: Ctrl+Minus).
    You hear: Text size.
4. Choose Windows text size in the Options menu, Text.
    You hear: Windows text size.
5. Press F1 (Keyboard shortcuts; Help menu).
6. Press Escape.
7. Choose User guide in the Help menu.
8. Press Escape.
9. Press Ctrl+S.
    You hear: Nothing to save here.

## Use axMedia's Context menu

Starts with: Open axMedia.

1. Choose Nothing in the queue in the Context menu.
    You hear: Nothing in the queue.
2. Close the window.
3. Open axMedia with the folder ch.
    You hear: Playing alpha.wav.
4. Choose the menu item Information, Context.
    You hear: alpha.wav, 1 of 2, playing, 0:0.
5. Press Down.
6. Choose the menu item Information, Context.
    You hear: beta.wav, 2 of 2.
7. Choose the menu item Play, Context.
    You hear: Playing beta.wav.
8. Press Ctrl+Shift+P (Command palette; Help menu).
9. Type: remove
    You hear: Remove, Context, Delete.
10. Press Enter.
    You hear: Removed beta.wav.
11. Choose the menu item Remove, Context.
    You hear: Removed alpha.wav.

## Add a recent folder to the queue from the command palette

Starts with: Open axMedia with the folder chapters.

You hear: Playing alpha.wav.
1. Choose Clear queue in the Session menu.
    You hear: Queue cleared.
2. Press Ctrl+Shift+P (Command palette; Help menu).
3. Type: chapt
    You hear words such as: 1. chapters.
    You hear: Session, Open recent.
4. Press Enter.
    You hear: Added 2.
