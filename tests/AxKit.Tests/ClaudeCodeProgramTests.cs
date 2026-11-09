using AxKit.Core;

namespace AxKit.Tests;

/// <summary>Where Claude Code is looked for (axClaude FR-1.2, mcp.md MC-6.8), one search for axClaude and the AI tools registration.</summary>
public sealed class ClaudeCodeProgramTests
{
    [Fact]
    public void An_explicit_path_is_the_only_place_tried()
    {
        Assert.Equal([@"C:\tools\claude.cmd"], ClaudeCodeProgram.Candidates(@"C:\tools\claude.cmd", @"C:\one;C:\two"));
    }

    [Fact]
    public void The_PATH_folders_come_first_with_both_names_then_the_installer_folder_then_npm()
    {
        var candidates = ClaudeCodeProgram.Candidates(null, @"C:\one;relative;C:\two");
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        Assert.Equal(
        [
            @"C:\one\claude.exe", @"C:\one\claude.cmd",
            @"C:\two\claude.exe", @"C:\two\claude.cmd",
            Path.Combine(profile, ".local", "bin", "claude.exe"),
            Path.Combine(appData, "npm", "claude.cmd"),
        ], candidates);
    }

    [Fact]
    public void Find_takes_the_first_candidate_that_exists_and_a_cmd_earlier_on_the_PATH_wins()
    {
        using var first = new TempFolder("axkit-claude-first");
        using var second = new TempFolder("axkit-claude-second");
        File.WriteAllText(Path.Combine(first.Path, "claude.cmd"), "@echo off");
        File.WriteAllText(Path.Combine(second.Path, "claude.exe"), string.Empty);
        Assert.Equal(Path.Combine(first.Path, "claude.cmd"), ClaudeCodeProgram.Find(null, $"{first.Path};{second.Path}"));
        Assert.Equal(Path.Combine(second.Path, "claude.exe"), ClaudeCodeProgram.Find(null, $"{second.Path};{first.Path}"));
    }

    [Fact]
    public void A_PATH_entry_with_forbidden_characters_is_skipped_not_thrown()
    {
        using var folder = new TempFolder("axkit-claude");
        File.WriteAllText(Path.Combine(folder.Path, "claude.exe"), string.Empty);
        Assert.Equal(Path.Combine(folder.Path, "claude.exe"), ClaudeCodeProgram.Find(null, $"C:\\bad<name;{folder.Path}"));
    }
}
