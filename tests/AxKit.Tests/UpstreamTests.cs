using System.Text.Json;

namespace AxKit.Tests;

/// <summary>
/// The upstream watch (docs/axKit/automation.md, "When an outside program changes"): docs/axKit/upstream.json records
/// the version of each outside program that axKit's checks last passed with, and tools/upstream.ps1 compares it with
/// the installed one at the start of a session. Read off the sources, as <see cref="HookTests"/> reads the hooks: the
/// record parses, names the program of every access layer, and names only checks that exist. The installed versions
/// are never asked here: the build servers have other versions.
/// </summary>
public sealed class UpstreamTests
{
    private static readonly string[] TextFields = ["id", "name", "verified", "date"];

    private static readonly string[] ListFields = ["places", "arguments", "tests", "scripts"];

    private static List<JsonElement> Programs()
    {
        var path = Path.Combine(LayoutTests.RepositoryRoot(), "docs", "axKit", "upstream.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("programs").EnumerateArray().Select(entry => entry.Clone()).ToList();
    }

    [Fact]
    public void The_record_names_the_program_of_every_access_layer()
    {
        var ids = Programs().Select(entry => entry.GetProperty("id").GetString()).ToList();

        // axClaude and AI help, axGIT, axPass, axTerminal's shells (the root CLAUDE.md, rule 12). A new access layer's
        // program joins the record and this list.
        foreach (var id in new[] { "claude", "git", "keepassxc", "powershell", "pwsh", "cmd", "bash" })
        {
            Assert.Contains(id, ids);
        }

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Every_entry_has_its_fields_and_names_checks_that_exist()
    {
        var root = LayoutTests.RepositoryRoot();
        foreach (var entry in Programs())
        {
            var id = entry.GetProperty("id").GetString();
            foreach (var field in TextFields)
            {
                Assert.True(entry.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String && value.GetString()!.Trim().Length > 0, $"{id}: {field} is missing");
            }

            foreach (var field in ListFields)
            {
                Assert.True(entry.TryGetProperty(field, out var list) && list.ValueKind == JsonValueKind.Array, $"{id}: {field} is not a list");
            }

            Assert.True(entry.GetProperty("places").GetArrayLength() > 0, $"{id}: no place to look for the program");
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", entry.GetProperty("date").GetString());

            var tests = entry.GetProperty("tests").EnumerateArray().Select(test => test.GetString()!).ToList();
            var scripts = entry.GetProperty("scripts").EnumerateArray().Select(script => script.GetString()!).ToList();
            Assert.True(tests.Count + scripts.Count > 0, $"{id}: no check verifies it");
            foreach (var test in tests)
            {
                Assert.True(Directory.Exists(Path.Combine(root, "tests", test + ".Tests")), $"{id}: no test project tests/{test}.Tests");
            }

            foreach (var script in scripts)
            {
                var path = Path.Combine(root, "tests", "scripts", script.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(Directory.Exists(path) || File.Exists(path), $"{id}: no tests/scripts/{script}");
            }
        }
    }

    [Fact]
    public void The_script_is_ASCII_keeps_its_modes_and_is_run_by_the_tests_and_named_in_the_notes()
    {
        var root = LayoutTests.RepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "tools", "upstream.ps1"));

        // Windows PowerShell 5.1 reads a script without a byte order mark as ANSI: the script stays ASCII.
        Assert.DoesNotContain(script, c => c > 127);
        foreach (var piece in new[] { "[switch]$Verify", "[switch]$Check", "[string]$File" })
        {
            Assert.Contains(piece, script, StringComparison.Ordinal);
        }

        // The record itself stays ASCII too: the script writes it back whole.
        Assert.DoesNotContain(File.ReadAllText(Path.Combine(root, "docs", "axKit", "upstream.json")), c => c > 127);

        // test.ps1 and CI check the record's shape; every session reads when to run the watch.
        Assert.Contains("tools\\upstream.ps1') -Check", File.ReadAllText(Path.Combine(root, "test.ps1")), StringComparison.Ordinal);
        Assert.Contains("./tools/upstream.ps1 -Check", File.ReadAllText(Path.Combine(root, ".github", "workflows", "build.yml")), StringComparison.Ordinal);
        Assert.Contains("tools\\upstream.ps1", File.ReadAllText(Path.Combine(root, "CLAUDE.md")), StringComparison.Ordinal);
    }
}
