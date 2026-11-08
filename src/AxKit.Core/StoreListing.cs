namespace AxKit.Core;

/// <summary>
/// axKit's listing in the Microsoft Store (docs/axkit/store-distribution.md 3.0; reserved 2026-11-08): the Store ID and
/// the addresses made from it. The Store's copy of axKit (<see cref="PackageIdentity.IsPackaged"/>) offers the Store
/// page in place of the updater (AX-13), and the documents name the winget command.
/// </summary>
public static class StoreListing
{
    /// <summary>The product's Store ID, Partner Center's and winget's name for it.</summary>
    public const string ProductId = "9PK01L2K5000";

    /// <summary>The listing in the Microsoft Store app, where Update appears when the Store has a newer version than this copy.</summary>
    public const string PageUri = "ms-windows-store://pdp/?productid=" + ProductId;

    /// <summary>The listing on the web, for a document or a browser.</summary>
    public const string WebUrl = "https://apps.microsoft.com/detail/" + ProductId;

    /// <summary>The command that installs axKit from the Store source of Windows Package Manager.</summary>
    public const string WingetCommand = "winget install " + ProductId + " -s msstore";
}
