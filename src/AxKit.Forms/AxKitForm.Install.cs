using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// Installing a program an app needs (docs/axKit/SPEC.md AX-2.9, 2026-10-24): the window's one member for it; the
/// work and the installer that runs are <see cref="ProgramInstaller"/>'s.
/// </summary>
public abstract partial class AxKitForm
{
    /// <summary>Made in the constructor (AxKitForm.cs).</summary>
    private readonly ProgramInstaller _installer;

    /// <summary>
    /// Installs <paramref name="program"/>: a progress notice, the installer run, the PATH read again, then
    /// <paramref name="installed"/> asked; true runs <paramref name="then"/> after "Git is installed", false says what
    /// happened with the program's page to open. Without a way to install it here (no winget), the page opens at once.
    /// <paramref name="target"/> is what the consent is recorded for, the program's own unless the caller connects to it
    /// under another name (axTerminal installs Git for Windows as its shell "Git Bash").
    /// </summary>
    protected internal void InstallProgram(ProgramInstall program, Func<bool> installed, Action then, ConsentTarget? target = null) =>
        _installer.InstallProgram(program, installed, then, target);
}
