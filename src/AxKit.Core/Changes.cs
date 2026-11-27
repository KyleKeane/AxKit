namespace AxKit.Core;

/// <summary>
/// One part of a change on disk: a file written (<see cref="Before"/> null when it was made, <see cref="After"/> null
/// when it was removed), or a file or folder moved from <see cref="Path"/> to <see cref="To"/>. A file made in a folder
/// the change made too (a new list's <c>list.json</c>) has <see cref="MadeFolder"/>, so that its undo takes the folder
/// away again when nothing else is in it.
/// </summary>
public sealed record FileChange(string Path, string? Before, string? After, string? To = null, bool MadeFolder = false)
{
    public bool IsMove => To is not null;

    public static FileChange Write(string path, string? before, string after) => new(path, before, after);

    /// <summary>A file made in a new folder: the folder made with it, and gone with it on undo.</summary>
    public static FileChange WriteInNewFolder(string path, string after) => new(path, null, after, MadeFolder: true);

    public static FileChange Move(string from, string to) => new(from, null, null, to);
}

/// <summary>A change the user made, in its parts, with its words for Ctrl+Z ("Undone: Done: Fix the tap"; IA-5.4).</summary>
public sealed record Step(IReadOnlyList<FileChange> Parts, string Said);

/// <summary>
/// The steps Ctrl+Z can take back, newest last, the oldest dropped past <paramref name="cap"/> (IA-5.4). One stack for
/// every window that writes its changes as steps (axTask, axTemplate's browser), written twice until 2026-11-24
/// (docs/axKit/repairs.json R-91).
/// </summary>
public sealed class StepHistory(int cap)
{
    private readonly List<Step> _steps = [];

    public int Count => _steps.Count;

    public void Add(Step step)
    {
        _steps.Add(step);
        if (_steps.Count > cap)
        {
            _steps.RemoveAt(0);
        }
    }

    /// <summary>The newest step, taken off the stack; null when there is none.</summary>
    public Step? TakeLast()
    {
        if (_steps.Count == 0)
        {
            return null;
        }

        var step = _steps[^1];
        _steps.RemoveAt(_steps.Count - 1);
        return step;
    }

    public void Clear() => _steps.Clear();
}

/// <summary>
/// The changes on disk and their undo (IA-5.4, IA-5.5). A step is applied part by part, whole or not at all; undone
/// in reverse, and only when every part is still as the step left it, so that an edit made since in axJSON or by a
/// sync is never lost.
/// </summary>
public static class Changes
{
    /// <summary>
    /// Applies the parts in order: writes (folders made as needed) and moves. Whole or not at all: when a part
    /// fails, the parts applied before it are taken back, last first, and the failure is thrown on, so that a
    /// caller that keeps a step for Ctrl+Z only after this returns never leaves half a step on disk with no way
    /// back (until 2026-11-27 the parts before the failure stayed). When taking back fails too, the exception's
    /// message says both.
    /// </summary>
    public static void Apply(IReadOnlyList<FileChange> parts)
    {
        var applied = 0;
        try
        {
            for (; applied < parts.Count; applied++)
            {
                var part = parts[applied];
                if (part.IsMove)
                {
                    MovePath(part.Path, part.To!);
                }
                else
                {
                    WriteOrRemove(part.Path, part.After);
                }
            }
        }
        catch (Exception ex) when (applied > 0 && ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            try
            {
                UndoParts(parts, applied);
            }
            catch (Exception back) when (back is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                throw new IOException($"{ex.Message} What was done before it could not all be taken back: {back.Message}", ex);
            }

            throw;
        }
    }

    /// <summary>Why the step cannot be undone now ("Fix the tap changed on disk"); null when it can.</summary>
    public static string? CheckUndo(Step step)
    {
        // The state each path should be in, walking the parts forward: what the last part to touch it left.
        var expected = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var moved = new List<FileChange>();
        foreach (var part in step.Parts)
        {
            if (part.IsMove)
            {
                moved.Add(part);
                if (expected.Remove(part.Path, out var text))
                {
                    expected[part.To!] = text;
                }
            }
            else
            {
                expected[part.Path] = part.After;
            }
        }

        foreach (var (path, text) in expected)
        {
            var now = ReadOrNull(path);
            if (now != text)
            {
                return $"{System.IO.Path.GetFileNameWithoutExtension(path)} changed on disk";
            }
        }

        foreach (var move in moved)
        {
            if (!File.Exists(move.To) && !Directory.Exists(move.To))
            {
                return $"{System.IO.Path.GetFileNameWithoutExtension(move.To)} is not there any more";
            }

            // A move that changed capitals only (a list renamed from "home" to "Home") is one place under two
            // spellings: what is found under the old one is the thing moved, not something new in its place.
            // Until 2026-11-27 such a rename could never be undone.
            if (!move.Path.Equals(move.To, StringComparison.OrdinalIgnoreCase) && (File.Exists(move.Path) || Directory.Exists(move.Path)))
            {
                return $"There is a {System.IO.Path.GetFileNameWithoutExtension(move.Path)} again where it was";
            }
        }

        return null;
    }

    /// <summary>Undoes the step, last part first; <see cref="CheckUndo"/> first.</summary>
    public static void Undo(Step step) => UndoParts(step.Parts, step.Parts.Count);

    /// <summary>The first <paramref name="count"/> parts undone, last first.</summary>
    private static void UndoParts(IReadOnlyList<FileChange> parts, int count)
    {
        for (var i = count - 1; i >= 0; i--)
        {
            var part = parts[i];
            if (part.IsMove)
            {
                MovePath(part.To!, part.Path);
            }
            else
            {
                WriteOrRemove(part.Path, part.Before);
                if (part.MadeFolder && System.IO.Path.GetDirectoryName(part.Path) is { } folder && Directory.Exists(folder)
                    && !Directory.EnumerateFileSystemEntries(folder).Any())
                {
                    Directory.Delete(folder);
                }
            }
        }
    }

    /// <summary>The file's text; null when there is no such file.</summary>
    public static string? ReadOrNull(string path) =>
        File.Exists(path) ? TextDocument.Read(path).Text : Directory.Exists(path) ? string.Empty : null;

    private static void WriteOrRemove(string path, string? text)
    {
        if (text is null)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return;
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        var document = File.Exists(path) ? TextDocument.Read(path) : TextDocument.Empty();
        // A file found in a Windows code page (written by another program) that cannot hold a character of the new
        // text is written as UTF-8 from now on: in its own encoding the character would become a question mark
        // without a word (AD-3.3a; until 2026-11-27 it did).
        (document.Holds(text) ? document : document.AsUtf8()).Write(path, text);
    }

    private static void MovePath(string from, string to)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(to)!);
        if (Directory.Exists(from))
        {
            Directory.Move(from, to);
        }
        else
        {
            File.Move(from, to);
        }
    }
}
