namespace AxPass.Core;

/// <summary>
/// The one table of the words of KeePassXC's command-line tool (root CLAUDE.md rule 12): its commands, its options,
/// the marks of its paths and the names <c>show</c> prints, as constants, each with what it is. What was tried of the
/// tool is docs/axPass/kdbx-notes.md section 8. The classes beside it keep the rules and read the words from here:
/// <see cref="KeePassCli"/> finds and runs the tool and builds a command's arguments, <see cref="PassTree"/> reads
/// <c>ls</c>, <see cref="EntryFields"/> reads <c>show</c> and builds <c>edit</c>'s options, <see cref="Backups"/>
/// knows which commands change the database. The window names a command and an option by its constant and reads the
/// records; a test (<c>KeePassCliTextsTests</c>) fails when a command or an option is written in a string anywhere
/// else. When the tool renames a word, this file is adjusted and nothing the user hears or presses moves.
/// No prompt and no error text of the tool is matched anywhere: every command runs with <see cref="Quiet"/>, the
/// passwords go in on standard input in a fixed order, a failure is the exit code, and the tool's own words are said
/// to the user as they are (<see cref="CliResult.Why"/>). Nothing here is an abstraction over programs.
/// </summary>
public static class KeePassCliTexts
{
    // ---- The publisher ----

    /// <summary>
    /// The names on the certificate KeePassXC's releases for Windows are signed with (read from the installed
    /// keepassxc-cli.exe on 2026-11-16). A tool found outside Program Files gets the master password only when one of
    /// these signed it (<see cref="KeePassCli.SignedByKeePassXC"/>, repairs.md R-55). When KeePassXC changes its
    /// publisher, the new name joins this list.
    /// </summary>
    public static readonly IReadOnlyList<string> Publishers = ["DroidMonkey Apps, LLC"];

    // ---- Commands ----

    /// <summary>Lists groups and entries; with <see cref="ListOptions"/> the whole database, one full path per line.</summary>
    public const string List = "ls";

    /// <summary>Prints an entry's attributes as "Name: value" lines.</summary>
    public const string Show = "show";

    /// <summary>Changes an entry: its title, user name, URL, notes or password.</summary>
    public const string Edit = "edit";

    /// <summary>Makes an entry.</summary>
    public const string Add = "add";

    /// <summary>Moves an entry to the Recycle Bin, or deletes one that is in it.</summary>
    public const string Remove = "rm";

    /// <summary>The same for a group.</summary>
    public const string RemoveGroup = "rmdir";

    /// <summary>Makes a group.</summary>
    public const string MakeGroup = "mkdir";

    /// <summary>Moves an entry or a group into another group.</summary>
    public const string Move = "mv";

    /// <summary>Makes a random password; needs no database.</summary>
    public const string Generate = "generate";

    /// <summary>Makes a new database.</summary>
    public const string CreateDatabase = "db-create";

    /// <summary>Prints what the database is: its cipher, its dates, its counts, and whether it has a Recycle Bin (<see cref="RecycleBinOn"/>).</summary>
    public const string Info = "db-info";

    // ---- Options ----

    /// <summary>On every command on a database: the password prompt goes nowhere, so that nothing of it is read as output.</summary>
    public const string Quiet = "-q";

    /// <summary><see cref="List"/>: every group below too (-R), each line a full path (-f).</summary>
    public static readonly IReadOnlyList<string> ListOptions = ["-R", "-f"];

    /// <summary><see cref="Edit"/>: the new title.</summary>
    public const string TitleOption = "-t";

    /// <summary><see cref="Edit"/>: the new user name.</summary>
    public const string UserNameOption = "-u";

    /// <summary><see cref="Edit"/>: the new URL.</summary>
    public const string UrlOption = "--url";

    /// <summary><see cref="Edit"/>: the new notes.</summary>
    public const string NotesOption = "--notes";

    /// <summary>
    /// <see cref="Edit"/> and <see cref="CreateDatabase"/>: the tool asks for the new password, twice, which axPass
    /// answers on standard input after the master password (for a new database the new password is the master password).
    /// </summary>
    public const string PasswordPromptOption = "-p";

    /// <summary><see cref="Show"/>: one attribute's value alone (-a), a protected one in clear text (-s). How a password is read for the clipboard.</summary>
    public static IReadOnlyList<string> ShowValueOptions(string attribute) => ["-s", "-a", attribute];

    /// <summary>The whole command that makes a password: twenty characters (-L 20) of lower case (-l), upper case (-U), digits (-n) and special characters (-s).</summary>
    public static readonly IReadOnlyList<string> GenerateArguments = [Generate, "-L", "20", "-l", "-U", "-n", "-s"];

    // ---- Paths, as the tool prints and takes them ----

    /// <summary>What separates a group from what is in it ("Email/Gmail"), and what a group's line of <see cref="List"/> ends with.</summary>
    public const char PathSeparator = '/';

    /// <summary>The top group as a destination of <see cref="Move"/>, and what goes in front of a path that starts with "-" (S-6).</summary>
    public const string Root = "/";

    /// <summary>What <see cref="List"/> prints in place of an entry inside an empty group ("Email/[empty]").</summary>
    public const string EmptyMark = "[empty]";

    /// <summary>
    /// The name the tool gives the group it makes for deleted entries and groups, at the top of the database, with
    /// the first <see cref="Remove"/>. A name and no more: the database marks its Recycle Bin by the group's own
    /// identity, which the tool prints nowhere, so a database made by a KeePassXC in another language has its bin
    /// under another name, and a group of this name need not be the bin (the review of 2026-11-27). What axPass
    /// takes for the bin, and when, is <see cref="PassBin"/>.
    /// </summary>
    public const string RecycleBin = "Recycle Bin";

    /// <summary>
    /// The line of <see cref="Info"/> that says the database has a Recycle Bin, whole, as KeePassXC 2.8.0 prints it;
    /// a database without one prints another line. Read as a line of output, like the names <see cref="Show"/>
    /// prints, and only for one answer: when the line is not there, axPass does not know that a deleted entry can
    /// come back, and asks first. So a tool that words it otherwise makes axPass ask more, never less.
    /// </summary>
    public const string RecycleBinOn = "Recycle bin is enabled.";

    // ---- The names Show prints in front of an entry's attributes ----

    public const string TitleField = "Title";
    public const string UserNameField = "UserName";
    public const string PasswordField = "Password";
    public const string UrlField = "URL";
    public const string NotesField = "Notes";

    /// <summary>The two attributes <see cref="Show"/> prints that are not fields of the entry.</summary>
    public const string UuidField = "Uuid";
    public const string TagsField = "Tags";
}
