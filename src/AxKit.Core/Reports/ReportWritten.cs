namespace AxKit.Core.Reports;

/// <summary>The file of <see cref="ReportWritten"/>: the names, null until the list is kept, and the settings' seal over them.</summary>
public sealed class ReportWrittenList
{
    public List<string>? Names { get; set; }

    public string Seal { get; set; } = string.Empty;
}

/// <summary>
/// The report files axKit's report form wrote (docs/axKit/reports.md RP-6.9; repairs.md R-57), kept under the
/// settings' seal (<see cref="SettingsSeal"/>) so that the sender sends those and no other: an item file another
/// program put into the reports folder stays where it is and never goes out with the user's next report.
/// Each report is on the list twice: by its file's name, and by the id the form wrote into the file
/// (<see cref="ReportFiles.IdKey"/>, an entry that starts with <see cref="IdMark"/>, which no file name can). Not by
/// content: a report the user changed in axTask is still theirs, and by its id it stays so when axTask renamed its
/// file with its title. The list is "not kept yet" until the first report this version saves, so that a report an
/// earlier version saved and could not send is not left behind: until then the sender sends as it did, and the first
/// save takes the reports that wait at that moment onto the list, by name alone, since they carry no id. Tested in
/// <c>tests/AxKit.Tests/ReportSenderTests</c>.
/// </summary>
public static class ReportWritten
{
    /// <summary>What the log says when the list's seal does not check; the list is then empty, and nothing it does not name is sent.</summary>
    public const string ListNotUsed = "The list of the reports axKit's report form wrote was changed outside axKit and is not used: reports saved before now are not sent.";

    /// <summary>What an id's entry of the list starts with; a colon, so that it is no file's name.</summary>
    public const string IdMark = "id:";

    /// <summary>
    /// The list's entries, in lower case: file names and ids; null while the list is not kept yet. A list this
    /// install has sealed and that is gone, or whose seal does not check, is kept and empty: taking the list away
    /// does not bring back the time before it.
    /// </summary>
    public static IReadOnlyList<string>? Names(string file)
    {
        var list = SettingsFile.Load<ReportWrittenList>(file, out var error);
        if (list.Names is null)
        {
            return SettingsSeal.WasSealed(file) ? [] : null;
        }

        if (!SettingsSeal.Checks(file, list.Seal, SealedText(list.Names), SealedText(null)))
        {
            SettingsSeal.Refused(error, ListNotUsed);
            return [];
        }

        return list.Names.Where(name => name is not null).Select(name => name.ToLowerInvariant()).ToList();
    }

    /// <summary>
    /// Whether a report file is one the form wrote: the list is not kept yet, or its name is on the list, or the id
    /// it carries is (<paramref name="id"/>, <see cref="ReportFiles.IdOf"/>; null for a file without one).
    /// </summary>
    public static bool Holds(IReadOnlyList<string>? names, string path, string? id) =>
        names is null
        || names.Contains(Path.GetFileName(path).ToLowerInvariant())
        || (id is not null && names.Contains(IdMark + id.ToLowerInvariant()));

    /// <summary>
    /// A report file's name and its id put on the list, before the file itself is written
    /// (<see cref="ReportFiles.Save"/>). The first time, the list starts with the reports that wait in
    /// <paramref name="folder"/> at that moment (those an earlier version saved). Throws as
    /// <see cref="File.WriteAllText(string, string)"/> does.
    /// </summary>
    public static void Add(string file, string path, string folder, string id)
    {
        var names = Names(file)?.ToList()
            ?? ReportFiles.Unsent(folder).Select(unsent => Path.GetFileName(unsent.Path).ToLowerInvariant()).ToList();
        foreach (var entry in new[] { Path.GetFileName(path).ToLowerInvariant(), IdMark + id.ToLowerInvariant() })
        {
            if (!names.Contains(entry))
            {
                names.Add(entry);
            }
        }

        var list = new ReportWrittenList { Names = names, Seal = SettingsSeal.Of(file, SealedText(names)) };
        if (SettingsFile.Save(file, list))
        {
            SettingsSeal.Remember(file);
        }
    }

    /// <summary>What the user is told beside what was sent (RP-6.9); empty when every waiting file was the form's.</summary>
    public static string NotSentSentence(int count) => count switch
    {
        0 => string.Empty,
        1 => "1 file in the reports folder was not written by axKit's report form and was not sent.",
        _ => $"{count} files in the reports folder were not written by axKit's report form and were not sent.",
    };

    /// <summary>The sealed text: "kept" and the entries; nothing while the list is not kept, the default no seal is asked for.</summary>
    private static string SealedText(IReadOnlyList<string>? names) =>
        names is null ? SettingsSeal.Text([]) : SettingsSeal.Text(["kept", .. names]);
}
