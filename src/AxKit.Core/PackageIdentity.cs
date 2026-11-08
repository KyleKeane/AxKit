using System.Runtime.InteropServices;
using System.Text;

namespace AxKit.Core;

/// <summary>
/// Whether this process runs from the MSIX package, the Microsoft Store's copy of axKit
/// (docs/axkit/store-distribution.md 3.5), and, if so, which entry of the package started it. Windows answers both
/// through kernel32 (docs/axkit/SPEC.md 5.4): a process without package identity gets APPMODEL_ERROR_NO_PACKAGE. Asked
/// once per process; a development build and the zip's copy are never packaged.
/// </summary>
public static class PackageIdentity
{
    /// <summary>APPMODEL_ERROR_NO_PACKAGE: the process has no package identity.</summary>
    private const int NoPackage = 15700;

    private static readonly Lazy<string?> FullNameValue = new(ReadFullName);
    private static readonly Lazy<string?> ApplicationIdValue = new(ReadApplicationId);

    /// <summary>The package's full name ("axKit.axKit_5.2.0.0_x64__…"), null outside a package.</summary>
    public static string? FullName => FullNameValue.Value;

    /// <summary>True in the Store's copy: the updater is off and Windows owns the installation (AX-3).</summary>
    public static bool IsPackaged => FullName is not null;

    /// <summary>
    /// The id of the package entry this process was started from ("AxDown", "AxStartSignIn"; tools/make-manifest.ps1
    /// names them), the part of the application user model id after the "!"; null outside a package or when Windows
    /// does not say.
    /// </summary>
    public static string? ApplicationId => ApplicationIdValue.Value;

    /// <summary>
    /// The package's console command, <c>axkit.exe</c> in the user's WindowsApps folder, which starts this program with
    /// package identity from anywhere (the manifest's app execution alias): the path to record for later use, since the
    /// program file's own path, under <c>C:\Program Files\WindowsApps\</c>, changes with every update.
    /// </summary>
    public static string AliasPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "axkit.exe");

    /// <summary>
    /// How another program starts this one: the alias in the Store's copy, the program file itself otherwise (the AI
    /// tools' command line, AxKitForm.Mcp.cs). Null when the process path is unknown outside a package.
    /// </summary>
    public static string? ProgramPath => IsPackaged ? AliasPath : Environment.ProcessPath;

    /// <summary>
    /// The first word of the command line as Windows holds it (<c>GetCommandLineW</c>): the console command's own
    /// path when the process was started through one (<c>"…\WindowsApps\axcalc.exe"</c>, AX-13.2), which .NET's
    /// <see cref="Environment.GetCommandLineArgs"/> replaces by the program file's path. Quoted or up to the first
    /// space; null when empty.
    /// </summary>
    public static string? CommandLineProgram
    {
        get
        {
            var line = Marshal.PtrToStringUni(GetCommandLineW())?.TrimStart();
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            if (line[0] == '"')
            {
                var end = line.IndexOf('"', 1);
                return end > 1 ? line[1..end] : line[1..];
            }

            var space = line.IndexOf(' ');
            return space < 0 ? line : line[..space];
        }
    }

    private static string? ReadFullName()
    {
        uint length = 0;
        var result = GetCurrentPackageFullName(ref length, null);
        if (result == NoPackage || length == 0)
        {
            return null;
        }

        var buffer = new StringBuilder((int)length);
        return GetCurrentPackageFullName(ref length, buffer) == 0 ? buffer.ToString() : null;
    }

    private static string? ReadApplicationId()
    {
        if (!IsPackaged)
        {
            return null;
        }

        uint length = 0;
        var result = GetCurrentApplicationUserModelId(ref length, null);
        if (result == NoPackage || length == 0)
        {
            return null;
        }

        var buffer = new StringBuilder((int)length);
        if (GetCurrentApplicationUserModelId(ref length, buffer) != 0)
        {
            return null;
        }

        var id = buffer.ToString();
        var separator = id.IndexOf('!');
        return separator < 0 ? null : id[(separator + 1)..];
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, StringBuilder? packageFullName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetCurrentApplicationUserModelId(ref uint applicationUserModelIdLength, StringBuilder? applicationUserModelId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr GetCommandLineW();
}
