namespace AxGit.Core;

/// <summary>
/// Which folder a git folder is, beyond its path (SPEC.md AG-1.6; the review's seventh pass of 2026-11-28, T1): the
/// name of its volume's file system, the volume's serial number and the folder's file ID, read by
/// <see cref="RepositoryFiles.Identity"/>. A trust is kept with it, so that another repository put at the same path (a
/// folder moved there, a clone made again) is asked about as a new one.
/// </summary>
public sealed record FolderIdentity(string FileSystem, string Volume, string FileId)
{
    /// <summary>
    /// Whether the identity lasts between openings: the file system is NTFS or ReFS, which give every folder an ID of
    /// its own for as long as it exists, read here as a share reports it too. Any other (FAT, FAT32, exFAT, UDF, a
    /// share that names none) gives IDs that may change or come again, so no trust is kept for it: it holds while the
    /// repository stays open (<see cref="GitSettings.Trust"/>).
    /// </summary>
    public static bool IsLasting(string fileSystem) =>
        fileSystem.Equals("NTFS", StringComparison.OrdinalIgnoreCase) || fileSystem.Equals("ReFS", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether this identity lasts between openings (<see cref="IsLasting"/>), with a volume and a file ID read.</summary>
    public bool Lasting => IsLasting(FileSystem) && Volume.Length > 0 && FileId.Length > 0;

    /// <summary>The identity as the settings keep it: "NTFS 9CC676A6C6768076 0000000000000000003C0000000B00E3".</summary>
    public string Text => $"{FileSystem} {Volume} {FileId}";

    /// <summary>The file system's name for the notice: "FAT32", or "unknown" where none was read.</summary>
    public static string NameOf(FolderIdentity? identity) => identity is { FileSystem.Length: > 0 } read ? read.FileSystem : "unknown";
}
