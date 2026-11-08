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
}
