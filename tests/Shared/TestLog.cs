using System.Runtime.CompilerServices;
using AxKit.Core;

namespace AxKit.TestShared;

/// <summary>
/// The log of a test process goes to a folder of its own under the temporary path, never into the user's
/// (<see cref="AppPaths.LogsFolder"/>): a test run is no window of a run, so without this its lines, among them the
/// errors of the tests that hand a changed settings file to the code, landed in the user's axKit.log and read as if his
/// settings had been tampered with (found 2026-11-17). Set as the test assembly loads, before any test can write a
/// line. Compiled into every test project through tests/Directory.Build.props.
/// </summary>
internal static class TestLog
{
    /// <summary>The folder every test process logs into.</summary>
    public static string Folder { get; } = Path.Combine(Path.GetTempPath(), "axkit-test-logs");

#pragma warning disable CA2255 // A module initializer in a test assembly is meant: it runs before any test.
    [ModuleInitializer]
    internal static void UseTestFolder() => Log.UseFolder(Folder);
#pragma warning restore CA2255
}
