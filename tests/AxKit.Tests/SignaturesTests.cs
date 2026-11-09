using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// WinVerifyTrust as the bundle asks it before a secret goes to a program (security audit S-19): a file nobody signed
/// is not trusted, nor a missing one. The check is of the file alone, so a Windows program signed through a catalog
/// answers false too; a positive case needs a file with its signature inside, which no computer is sure to have.
/// </summary>
public sealed class SignaturesTests
{
    [Fact]
    public void An_unsigned_or_missing_file_is_not_trusted()
    {
        using var temp = new TempFolder("axkit-signatures");
        var file = temp.File("tool.exe", "MZ not a program");
        Assert.False(Signatures.IsTrusted(file));
        Assert.False(Signatures.IsTrusted(Path.Combine(temp.Path, "missing.exe")));
    }
}