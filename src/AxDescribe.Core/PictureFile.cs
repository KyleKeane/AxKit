namespace AxDescribe.Core;

/// <summary>
/// A picture file of the user's that axDescribe hands to Claude (SPEC.md DE-2.7): the kinds Claude Code's Read tool
/// reads, the picker's filter made from them, and the reason a path is refused.
/// </summary>
public static class PictureFile
{
    /// <summary>The kinds Claude Code reads as pictures, with their extensions.</summary>
    public static readonly IReadOnlyList<string> Kinds = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

    /// <summary>The kinds as they are said in a refusal and in the usage text.</summary>
    public const string KindsSpoken = "a .png, .jpg, .jpeg, .gif or .webp file";

    /// <summary>The program a file's description is kept under, where a window's description keeps its program (DE-3.1).</summary>
    public const string Program = "picture file";

    /// <summary>The Windows file picker's filter: the picture kinds first, then every file.</summary>
    public static string Filter =>
        $"Pictures ({string.Join(", ", Kinds.Select(kind => "*" + kind))})|{string.Join(";", Kinds.Select(kind => "*" + kind))}|All files (*.*)|*.*";

    /// <summary>Whether the path's extension is one of the kinds, in any case.</summary>
    public static bool IsPicture(string path) => Kinds.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>Why the path cannot be described, said as it is; null when it is a picture that exists.</summary>
    public static string? Refusal(string path) =>
        !IsPicture(path) ? $"{Path.GetFileName(path)} is not a picture axDescribe can describe: {KindsSpoken}"
        : !File.Exists(path) ? $"{path} was not found"
        : null;
}
