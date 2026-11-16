using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// The stack (the root CLAUDE.md, Stack and constraints; docs/axKit/SPEC.md AX-7.1, B-10): WinForms alone, with its native
/// Win32 controls that NVDA reads reliably, and nothing from outside .NET: no NuGet package, no WPF, no web view.
/// Read off the project files and the sources, as <see cref="LayoutTests"/> reads the references. The test projects
/// are the one exception: xunit and its runner are packages, named once in tests/Directory.Build.props and held by
/// the lock files.
/// </summary>
public sealed partial class StackTests
{
    [Fact]
    public void No_project_outside_the_tests_takes_a_package_or_WPF()
    {
        var root = LayoutTests.RepositoryRoot();
        var problems = new List<string>();
        foreach (var file in FilesOutsideTests(root, "*.csproj", "*.props", "*.targets"))
        {
            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (Forbidden().IsMatch(line))
                {
                    problems.Add($"{Path.GetRelativePath(root, file)}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(problems.Count == 0, "A package, WPF or a web view in a project that ships or builds axKit (zero third-party dependencies, WinForms alone): " + string.Join("; ", problems));
    }

    [Fact]
    public void No_source_outside_the_tests_makes_a_web_view()
    {
        var root = LayoutTests.RepositoryRoot();
        var problems = new List<string>();
        foreach (var file in FilesOutsideTests(root, "*.cs"))
        {
            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                if (!line.TrimStart().StartsWith("//", StringComparison.Ordinal) && WebView().IsMatch(line))
                {
                    problems.Add($"{Path.GetRelativePath(root, file)}:{number}: {line.Trim()}");
                }
            }
        }

        Assert.True(problems.Count == 0, "A web view (NVDA reads native controls reliably; a browser inside a window it does not): " + string.Join("; ", problems));
    }

    /// <summary>The files of the patterns under <c>src</c> and <c>tools</c>, and those in the repository's root folder itself; build output left out.</summary>
    private static IEnumerable<string> FilesOutsideTests(string root, params string[] patterns)
    {
        var separator = Path.DirectorySeparatorChar;
        foreach (var pattern in patterns)
        {
            foreach (var file in Directory.GetFiles(root, pattern, SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }

            foreach (var folder in new[] { "src", "tools" })
            {
                foreach (var file in Directory.GetFiles(Path.Combine(root, folder), pattern, SearchOption.AllDirectories))
                {
                    if (!file.Contains($"{separator}obj{separator}", StringComparison.Ordinal) && !file.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
                        && !file.Contains($"{separator}tools{separator}sdk{separator}", StringComparison.Ordinal))
                    {
                        yield return file;
                    }
                }
            }
        }
    }

    [GeneratedRegex(@"<PackageReference\b|<UseWPF\b|WebView", RegexOptions.IgnoreCase)]
    private static partial Regex Forbidden();

    /// <summary>WinForms' browser control and WebView2, as a type made or declared.</summary>
    [GeneratedRegex(@"\b(WebBrowser|WebView2?)\b")]
    private static partial Regex WebView();
}
