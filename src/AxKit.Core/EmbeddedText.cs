using System.Reflection;

namespace AxKit.Core;

/// <summary>
/// A text file embedded in an assembly: each app's user guide (Help → User guide) and the bundle's axKit guide
/// (AX-9.9). Read whole, or one line that says where it is in the source when the build left it out.
/// </summary>
public static class EmbeddedText
{
    public static string Read(Assembly assembly, string name, string sourcePath)
    {
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return $"{name} is not included in this build. It is {sourcePath} in the source repository.";
        }

        using var reader = new StreamReader(stream);
        return Shown(reader.ReadToEnd());
    }

    /// <summary>
    /// The text as a window shows it: without the lines that are an HTML comment and nothing else, the markers a
    /// script writes between (a guide's Menus section, tools/guide-menus.ps1), which are for the scripts and would
    /// otherwise be read aloud.
    /// </summary>
    public static string Shown(string text)
    {
        var lines = text.Split('\n');
        var kept = lines.Where(line => !(line.TrimStart().StartsWith("<!--", StringComparison.Ordinal) && line.TrimEnd().EndsWith("-->", StringComparison.Ordinal)));
        return string.Join('\n', kept);
    }
}
