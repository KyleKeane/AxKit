using System.Text.RegularExpressions;

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
}
