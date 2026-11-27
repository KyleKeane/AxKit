namespace AxKit.TestShared;

/// <summary>
/// A fresh folder under the temporary path for one test or one test class ("axkit-test-&lt;prefix&gt;-&lt;guid&gt;"), made at
/// once and deleted with everything in it on Dispose. Compiled into every test project through
/// tests/Directory.Build.props. Every such folder starts with <see cref="Mark"/>, which is in test.ps1's table of
/// the tests' own folders, counted before and after a run (the review's third pass of 2026-11-27, L7).
/// </summary>
internal sealed class TempFolder : IDisposable
{
    /// <summary>What the name of every test's temporary folder starts with.</summary>
    public const string Mark = "axkit-test-";

    public TempFolder(string prefix)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Mark + prefix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>The folder's full path.</summary>
    public string Path { get; }

    /// <summary>Writes the bytes to the file of that name in the folder (a name with folders in it makes them) and returns its path.</summary>
    public string File(string name, byte[] bytes)
    {
        var path = Prepare(name);
        System.IO.File.WriteAllBytes(path, bytes);
        return path;
    }

    /// <summary>Writes the text, as UTF-8 without a byte order mark, to the file of that name in the folder and returns its path.</summary>
    public string File(string name, string text)
    {
        var path = Prepare(name);
        System.IO.File.WriteAllText(path, text);
        return path;
    }

    /// <summary>
    /// The folder deleted with everything in it. A read-only file stops a delete, and git writes every object so:
    /// the mark is taken off every file. A program a test started can hold the folder for a moment after it ended
    /// (the review's third pass of 2026-11-27, L7: 1,027 folders of axkit-approval-scripts and 3 of axmcp-stdio
    /// were left in the temporary folder, each over a git repository or a server's working folder), so the delete
    /// is tried again for up to two seconds. What is still left then shows in test.ps1's count of the tests' own
    /// folders, which must not grow over a run.
    /// </summary>
    public void Dispose()
    {
        for (var tries = 0; tries < 20 && Directory.Exists(Path); tries++)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
                {
                    System.IO.File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(Path, recursive: true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(100);
            }
        }
    }

    private string Prepare(string name)
    {
        var path = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        return path;
    }
}
