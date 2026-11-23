using AxKit.Core;

namespace AxAutomate.Core;

/// <summary>What a <c>template</c> step types: the template's text with its marks filled, by lines, and how many Left keys put the caret back at its mark.</summary>
public sealed record TemplateTyping(IReadOnlyList<string> Lines, int LeftKeys);

/// <summary>
/// A workflow's <c>template</c> step (docs/axTemplate/SPEC.md TP-6): <c>template &lt;name&gt;</c>, and optionally
/// <c>| &lt;selection&gt;</c> and <c>| &lt;clipboard&gt;</c>, the texts its marks stand for. The template is found by its
/// name, without regard to case, among the templates of the folder (<see cref="TemplateStore"/>); its marks are
/// filled (<see cref="TextTemplate"/>); and the step types the text where the window's focus is, as a <c>type</c>
/// step does, its lines with Enter between them, then the Left keys that put the caret back at <c>{caret}</c>. A
/// name no template has fails the step with the name (TP-6.2). Pure but for <see cref="Read"/>, which reads the folder.
/// </summary>
public static class TemplateStep
{
    /// <summary>The step's parts: the name, then the selection's and the clipboard's texts, empty when not given.</summary>
    public static (string Name, string Selection, string Clipboard) Parts(string rest)
    {
        var parts = rest.Split('|');
        string Part(int i) => i < parts.Length ? parts[i].Trim() : string.Empty;
        return (Part(0), Part(1), Part(2));
    }

    /// <summary>What the step types, or why it fails ("No template is called Thanks"), given the folder's templates.</summary>
    public static (TemplateTyping? Typing, string? Error) Of(string rest, IReadOnlyList<Template> templates)
    {
        var (name, selection, clipboard) = Parts(rest);
        if (name.Length == 0)
        {
            return (null, "A template step names a template: template <name>");
        }

        if (templates.FirstOrDefault(template => string.Equals(template.Title, name, StringComparison.OrdinalIgnoreCase)) is not { } found)
        {
            return (null, $"No template is called {name}");
        }

        var made = TextTemplate.Expand(found.Text, selection, clipboard);
        var lines = made.Text.ReplaceLineEndings("\n").Split('\n');
        return (new TemplateTyping(lines, TemplateStore.LeftKeys(made)), null);
    }

    /// <summary>The templates of a run's home: its <c>Documents\axKit\Templates</c>, where a script's <c>file</c> steps put them (bundle AX-5.11).</summary>
    public static IReadOnlyList<Template> Read(string home) =>
        TemplateStore.Read(Path.Combine(home, "Documents", "axKit", "Templates")).Templates;
}
