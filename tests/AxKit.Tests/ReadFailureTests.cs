using System.Text.RegularExpressions;
using AxKit.TestShared;

namespace AxKit.Tests;

/// <summary>
/// Two rules of the sources about a file that cannot be read, from the review of 2026-11-27: one unreadable
/// <c>.json</c> in the Templates folder took every template away without a word, because
/// <c>TextDocument.Read</c> says "not a text file" and "too large" with an <c>InvalidOperationException</c>, which
/// the folder's readers did not catch, and their tasks were started and never looked at again.
/// </summary>
public sealed partial class ReadFailureTests
{
    /// <summary>The calls that read a file through <c>TextDocument.Read</c>, by themselves or one step away.</summary>
    [GeneratedRegex(@"\b(TextDocument\.Read|Changes\.ReadOrNull|Changes\.CheckUndo|ListConversion\.Plan|ListConversion\.Prepare)\(")]
    private static partial Regex ReadsAFile();

    /// <summary>A task started and dropped: <c>_ = ReadFolderAsync();</c>. Its exception is seen by no one.</summary>
    [GeneratedRegex(@"^\s*_\s*=\s*[\w.]+Async\(", RegexOptions.Multiline)]
    private static partial Regex DroppedTask();

    /// <summary>The files that call such a reader and leave its failure to their caller, each with where it is caught.</summary>
    private static readonly Dictionary<string, string> CaughtElsewhere = new()
    {
        ["src/AxPreview.Core/Renderers.cs"] = "a renderer of the viewer: caught where the window reads a document, TextEditorForm.ReadDocument",
    };

    private static IEnumerable<(string Relative, string Text)> Sources()
    {
        var root = SourceTexts.RepositoryRoot();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (!relative.Contains("/obj/", StringComparison.Ordinal) && !relative.Contains("/bin/", StringComparison.Ordinal))
            {
                yield return (relative, File.ReadAllText(file));
            }
        }
    }

    /// <summary>
    /// A file that reads through <c>TextDocument.Read</c> names the exception it throws for what is no text. The
    /// check is by the file, not by the call: it cannot see that every call is inside the right <c>try</c>, only
    /// that whoever wrote the file knew of the exception; a new reader that does not is found here.
    /// </summary>
    [Fact]
    public void A_file_that_reads_a_text_file_knows_what_the_reading_throws()
    {
        var missing = Sources()
            .Where(source => ReadsAFile().IsMatch(source.Text) && !source.Text.Contains("InvalidOperationException", StringComparison.Ordinal) && !CaughtElsewhere.ContainsKey(source.Relative))
            .Select(source => source.Relative)
            .ToList();
        Assert.True(missing.Count == 0, "These files read a file through TextDocument.Read (itself, Changes.ReadOrNull, Changes.CheckUndo or ListConversion's plan) and never name InvalidOperationException, which it throws for a file that is no text or too large. Catch it where the read's other failures are caught, or name the file in CaughtElsewhere with where it is caught:\n" + string.Join("\n", missing));
    }

    /// <summary>
    /// No task is started and dropped. A window runs its tasks through the window base's <c>Do</c>, which turns an
    /// escaped exception into a notice and a log line; anything else awaits its task.
    /// </summary>
    [Fact]
    public void No_task_is_started_and_dropped()
    {
        var dropped = Sources()
            .SelectMany(source => DroppedTask().Matches(source.Text).Select(match => $"{source.Relative}:{source.Text.AsSpan(0, match.Index).Count('\n') + 1}: {match.Value.Trim()}"))
            .ToList();
        Assert.True(dropped.Count == 0, "A task started with \"_ =\" loses its exception: in a window run it through Do(() => ...), elsewhere await it:\n" + string.Join("\n", dropped));
    }
}
