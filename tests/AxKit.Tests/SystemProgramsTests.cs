using AxKit.Core;

namespace AxKit.Tests;

/// <summary>Where the apps' programs are found (docs/axkit/security-audit-2026-10-24.md S-1).</summary>
public sealed class SystemProgramsTests
{
    [Fact]
    public void Windows_programs_are_named_by_their_full_paths()
    {
        Assert.All([SystemPrograms.Explorer, SystemPrograms.Cmd, SystemPrograms.PowerShell], path => Assert.True(Path.IsPathFullyQualified(path)));
    }

    [Fact]
    public void The_current_folder_is_kept_out_of_program_searches_for_this_process_and_its_children()
    {
        // S-1: the switch cmd.exe and Windows read; Program.Main sets it first thing, and every child inherits the environment.
        SystemPrograms.KeepCurrentFolderOffSearches();
        Assert.Equal("1", Environment.GetEnvironmentVariable(SystemPrograms.NoCurrentFolderSearch));
        Assert.Contains("SystemPrograms.KeepCurrentFolderOffSearches();", File.ReadAllText(Path.Combine(LayoutTests.RepositoryRoot(), "src", "AxKit", "Program.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void A_relative_path_entry_is_never_searched_as_it_would_be_the_current_folder()
    {
        using var temp = new TempFolder("axkit-path");
        var folder = temp.Path;
        var current = Environment.CurrentDirectory;
        try
        {
            File.WriteAllText(Path.Combine(folder, "planted.exe"), string.Empty);
            Environment.CurrentDirectory = folder;
            Assert.Null(SystemPrograms.OnPath("planted.exe", @".;sub;"""""));
            Assert.Equal(Path.Combine(folder, "planted.exe"), SystemPrograms.OnPath("planted.exe", $"relative;\"{folder}\""));
        }
        finally
        {
            Environment.CurrentDirectory = current;
        }
    }
}
