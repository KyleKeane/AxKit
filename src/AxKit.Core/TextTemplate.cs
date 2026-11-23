namespace AxKit.Core;

/// <summary>What a template makes at the caret: the text that goes in, and where in it the caret lands (an offset into <paramref name="Text"/>).</summary>
public sealed record TemplateText(string Text, int Caret);

/// <summary>
/// A template of an editor (docs/axDown/SPEC.md AD-13, 2026-11-23; the maintainer: "the simple template injection
/// with cursor placement ... please build the simeple framework and then we can extend it later"): plain text put in
/// at the caret, in the place of the selection, with three marks of its own and nothing else to learn:
/// <list type="bullet">
/// <item><c>{caret}</c>: where the caret lands. The first one counts; without one the caret lands after the text.</item>
/// <item><c>{selection}</c>: the text that was selected when the template was chosen, empty when nothing was. A
/// link's template is <c>[{selection}]({caret})</c>: the selected words become the link's text and the caret
/// waits where the address goes.</item>
/// <item><c>{clipboard}</c>: the clipboard's text at that moment, empty when it holds none.</item>
/// </list>
/// Every other character is the template's own, braces included. What comes in through a mark is put in as it is
/// and never read for marks itself: a selection that holds "{caret}" stays those seven characters. Line endings
/// become the edit control's, CR LF. Placeholders to fill and to find with a key, and a form behind a template,
/// are not here: paused at the maintainer's word.
/// </summary>
public static class TextTemplate
{
    public const string CaretMark = "{caret}";
    public const string SelectionMark = "{selection}";
    public const string ClipboardMark = "{clipboard}";

    /// <summary>The template's text with its marks filled, and the caret's place in it.</summary>
    public static TemplateText Expand(string template, string selection, string clipboard)
    {
        var text = new System.Text.StringBuilder();
        var caret = -1;
        var at = 0;
        var source = Lines(template);
        while (at < source.Length)
        {
            if (source[at] == '{')
            {
                if (Starts(source, at, CaretMark))
                {
                    // The first mark is the caret's place; a second one is dropped, never left in the text.
                    caret = caret < 0 ? text.Length : caret;
                    at += CaretMark.Length;
                    continue;
                }

                if (Starts(source, at, SelectionMark))
                {
                    text.Append(Lines(selection));
                    at += SelectionMark.Length;
                    continue;
                }

                if (Starts(source, at, ClipboardMark))
                {
                    text.Append(Lines(clipboard));
                    at += ClipboardMark.Length;
                    continue;
                }
            }

            text.Append(source[at]);
            at++;
        }

        return new TemplateText(text.ToString(), caret < 0 ? text.Length : caret);
    }

    /// <summary>The name a template file gives its menu item: the file's name without its extension.</summary>
    public static string NameOf(string path) => Path.GetFileNameWithoutExtension(path);

    private static bool Starts(string text, int at, string mark) => string.CompareOrdinal(text, at, mark, 0, mark.Length) == 0;

    /// <summary>Every line ending as the edit control has it.</summary>
    private static string Lines(string text) => text.ReplaceLineEndings("\r\n");
}
