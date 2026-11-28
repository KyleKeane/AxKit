using System.Text.RegularExpressions;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// Rule 11 of the root CLAUDE.md: the words that say an AI wrote or asks for something, and that it can be wrong, come
/// from AxKit.Core/AiCaution and are never written afresh (docs/axKit/repairs.json R-98; two sentences were, until
/// 2026-11-25). Read off the sources: no string of src but AiCaution's says ", an AI" or "can be wrong".
/// </summary>
public class AiCautionWordsTests
{
    [Fact]
    public void The_caution_words_are_written_in_AiCaution_alone()
    {
        var root = SourceTexts.RepositoryRoot();
        var words = new Regex(@"""[^""\r\n]*(, an AI\b|can be wrong)[^""\r\n]*""");
        var found = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !Regex.IsMatch(file, @"[\\/](bin|obj)[\\/]") && Path.GetFileName(file) != "AiCaution.cs")
            .SelectMany(file => File.ReadAllLines(file)
                .Select((line, index) => (line, index))
                .Where(pair => !pair.line.TrimStart().StartsWith("//", StringComparison.Ordinal) && words.IsMatch(pair.line))
                .Select(pair => $"{Path.GetRelativePath(root, file)}:{pair.index + 1}: {pair.line.Trim()}"))
            .ToList();
        Assert.True(found.Count == 0, "Caution words written outside AxKit.Core/AiCaution (rule 11: take them from there):\n" + string.Join("\n", found));
    }

    /// <summary>
    /// The eighth review of 2026-11-28, M4: the one sentence in front of a line with an offered command that axClaude
    /// writes out unsent holds all of rule 11, with a command's care (docs/axKit/SPEC.md AX-11.12). Until then two
    /// sentences said it, and the one of the /feedback line said neither that axKit checked nothing nor whose choice
    /// running it is.
    /// </summary>
    [Fact]
    public void The_line_not_sent_says_all_of_rule_11()
    {
        var words = AiCaution.LineNotSent("Claude");
        Assert.Contains("Claude wrote the command", words, StringComparison.Ordinal);
        Assert.Contains("it can be wrong", words, StringComparison.Ordinal);
        Assert.Contains("axKit has checked none of it", words, StringComparison.Ordinal);
        Assert.Contains("permission or safety checks refused it", words, StringComparison.Ordinal);
        Assert.Contains("read every detail before you run it", words, StringComparison.Ordinal);
        Assert.Contains("running it is your own choice", words, StringComparison.Ordinal);
    }
}
