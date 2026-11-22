using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// The program a <c>run</c> action names, found (docs/axTask/forms-framework.md AF-3.2, AF-4.3): a name is looked
/// for in the PATH's folders and nowhere else, never in the list's own folder; a path is taken only when it is a
/// full one. What is found must be a program file on a drive of this computer (S-5's rule) and outside the list's
/// folder: a list may be a shared folder, or one a program wrote, and whoever can write there must not be able to
/// supply the program its actions start.
/// </summary>
public static class ActionProgram
{
    /// <summary>The endings of a file Windows starts as a program; a name without one is tried with each, in this order.</summary>
    private static readonly string[] Endings = [".exe", ".com", ".cmd", ".bat"];

    /// <summary>
    /// The full path of the program, or why nothing is started, a clause that follows the action's name ("is not a
    /// program on this computer's search path"). <paramref name="path"/> is the PATH to search, the process's own by
    /// default; <paramref name="isLocal"/> says whether a path is on a drive of this computer.
    /// </summary>
    public static (string? Path, string? Refused) Find(string program, string listFolder, string? path = null, Func<string, bool>? isLocal = null)
    {
        var name = program.Trim();
        string? found;
        if (name.IndexOfAny(['\\', '/', ':']) >= 0)
        {
            if (!Path.IsPathFullyQualified(name))
            {
                return (null, $"names its program by a path that is not a full one ({name}); a program is a name on the search path, or a full path");
            }

            found = File.Exists(name) ? Path.GetFullPath(name) : null;
        }
        else if (Path.HasExtension(name))
        {
            found = SystemPrograms.OnPath(name, path ?? Environment.GetEnvironmentVariable("PATH"));
        }
        else
        {
            found = Endings.Select(ending => SystemPrograms.OnPath(name + ending, path ?? Environment.GetEnvironmentVariable("PATH"))).FirstOrDefault(candidate => candidate is not null);
        }

        if (found is null)
        {
            return (null, $"names the program {name}, which is not there: not on this computer's search path, or not at that path");
        }

        if (!Endings.Contains(Path.GetExtension(found), StringComparer.OrdinalIgnoreCase))
        {
            return (null, $"names {found}, which is no program file (.exe, .com, .cmd or .bat)");
        }

        if (Projects.Inside(found, listFolder))
        {
            return (null, $"names a program inside the list's own folder ({found}), where an action's program is never taken from");
        }

        return (isLocal ?? LocalPaths.IsLocal)(found)
            ? (found, null)
            : (null, $"names a program that is not on a drive of this computer ({found})");
    }
}
