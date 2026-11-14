using AxKit.Core;

namespace AxKit.Tests;

/// <summary>Where the apps' programs are found (docs/axKit/security-audit-2026-10-24.md S-1).</summary>
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

    // docs/axKit/repairs.json R-24: the one command line for a program that may be a batch file.

    [Theory]
    [InlineData(@"C:\work\R&D")]
    [InlineData(@"C:\work\a^b")]
    [InlineData(@"C:\work\it's")]
    [InlineData(@"C:\work\a b")]
    [InlineData(@"C:\work\plain")]
    [InlineData(@"C:\work\a|b<c>d")]
    [InlineData("(1,2;3=4)")]
    public void A_batch_program_gets_every_argument_in_quotes_where_cmd_reads_operators_as_text(string folder)
    {
        Assert.Null(SystemPrograms.BatchRefusal(@"C:\npm\claude.cmd", ["--add-dir", folder]));
        var line = SystemPrograms.LineFor(@"C:\npm\claude.cmd", ["--add-dir", folder]);
        Assert.Null(line.Refusal);
        Assert.Equal(SystemPrograms.Cmd, line.FileName);
        Assert.Equal($"/d /s /c \"\"C:\\npm\\claude.cmd\" \"--add-dir\" \"{folder}\"\"", line.Arguments);
        Assert.Equal($"\"{SystemPrograms.Cmd}\" {line.Arguments}", line.CommandLine);
    }

    [Theory]
    [InlineData(@"C:\work\100%", "a percent sign (%)")]
    [InlineData(@"C:\work\%PATH%", "a percent sign (%)")]
    [InlineData(@"C:\work\wow!", "an exclamation mark (!)")]
    [InlineData("say \"hi\"", "a double quote (\")")]
    [InlineData("one\ntwo", "a line break")]
    public void A_batch_program_is_refused_what_cmd_acts_on_inside_quotes_with_the_character_and_the_argument_named(string argument, string character)
    {
        var line = SystemPrograms.LineFor(@"C:\npm\claude.CMD", ["--add-dir", argument]);
        Assert.NotNull(line.Refusal);
        Assert.Equal(line.Refusal, SystemPrograms.BatchRefusal(@"C:\npm\claude.CMD", ["--add-dir", argument]));
        Assert.StartsWith("claude.CMD is a batch program, which Windows runs through cmd.exe, and ", line.Refusal);
        Assert.Contains(character, line.Refusal, StringComparison.Ordinal);
        Assert.Contains(argument.Replace('\n', ' '), line.Refusal, StringComparison.Ordinal);
        Assert.Equal(string.Empty, line.FileName);
        Assert.Equal(string.Empty, line.Arguments);
        // A .bat is a batch program too.
        Assert.NotNull(SystemPrograms.LineFor(@"C:\tools\run.bat", [argument]).Refusal);
    }

    [Theory]
    [InlineData(@"C:\R&D\claude.cmd", "an ampersand (&)")]
    [InlineData(@"C:\100%\claude.cmd", "a percent sign (%)")]
    [InlineData(@"C:\a^b\claude.cmd", "a caret (^)")]
    public void A_batch_program_whose_own_path_cmd_would_act_on_is_not_started(string program, string character)
    {
        var refusal = SystemPrograms.LineFor(program, ["--version"]).Refusal;
        Assert.NotNull(refusal);
        Assert.Contains("its own path, " + program, refusal, StringComparison.Ordinal);
        Assert.Contains(character, refusal, StringComparison.Ordinal);
        // Parentheses and spaces are plain text inside the quotes.
        Assert.Null(SystemPrograms.LineFor(@"C:\Program Files (x86)\nodejs\claude.cmd", ["--version"]).Refusal);
    }

    [Theory]
    [InlineData(@"C:\work\R&D", @"C:\work\R&D")]
    [InlineData(@"C:\work\100%", @"C:\work\100%")]
    [InlineData(@"C:\work\a^b", @"C:\work\a^b")]
    [InlineData(@"C:\work\wow!", @"C:\work\wow!")]
    [InlineData(@"C:\work\it's", @"C:\work\it's")]
    [InlineData(@"C:\work\a b", @"""C:\work\a b""")]
    [InlineData(@"C:\work\plain", @"C:\work\plain")]
    [InlineData(@"C:\a b\", @"""C:\a b\\""")]
    [InlineData("say \"hi\"", @"""say \""hi\""""")]
    [InlineData("", @"""""")]
    public void A_program_that_is_no_batch_file_takes_any_argument_quoted_only_where_windows_needs_it(string argument, string quoted)
    {
        Assert.Null(SystemPrograms.BatchRefusal(@"C:\R&D 100%\claude.exe", [argument]));
        var line = SystemPrograms.LineFor(@"C:\R&D 100%\claude.exe", ["--add-dir", argument]);
        Assert.Null(line.Refusal);
        Assert.Equal(@"C:\R&D 100%\claude.exe", line.FileName);
        Assert.Equal("--add-dir " + quoted, line.Arguments);
        Assert.Equal(@"""C:\R&D 100%\claude.exe"" --add-dir " + quoted, line.CommandLine);
        Assert.Equal(quoted, SystemPrograms.QuoteArgument(argument));
    }

    [Fact]
    public void A_line_without_arguments_is_the_quoted_program_alone()
    {
        Assert.Equal(@"""C:\bin\claude.exe""", SystemPrograms.LineFor(@"C:\bin\claude.exe", []).CommandLine);
        Assert.Equal(@"/d /s /c """"C:\npm\claude.cmd""""", SystemPrograms.LineFor(@"C:\npm\claude.cmd", []).Arguments);
        Assert.True(SystemPrograms.IsBatch(@"C:\x\RUN.BAT"));
        Assert.False(SystemPrograms.IsBatch(@"C:\x\run.cmd.exe"));
    }

    [Fact]
    public async Task A_real_batch_file_receives_what_was_sent_and_runs_no_second_command()
    {
        // The proof by cmd.exe itself: a batch file in a folder with a space and parentheses writes its arguments,
        // as it got them, to a file; "x&mkdir pwned" would make a folder if cmd read the & as its operator.
        using var temp = new TempFolder("axkit-batch");
        var batch = temp.File(@"a b (x86)\echo args.cmd", "@echo off\r\n>\"%~dp0out.txt\" echo %*\r\n");
        string[] sent = [@"C:\work\R&D", "a^b", "it's", "a b", "plain", "x&mkdir pwned", "(1,2;3=4)", "a|b<c>d", @"C:\dir\"];
        var line = SystemPrograms.LineFor(batch, sent);
        Assert.Null(line.Refusal);
        var start = new System.Diagnostics.ProcessStartInfo(line.FileName, line.Arguments) { WorkingDirectory = temp.Path };
        var result = await ProcessRun.RunAsync(start, [], TimeSpan.FromSeconds(30));
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Error.Trim());
        Assert.Equal(
            @"""C:\work\R&D"" ""a^b"" ""it's"" ""a b"" ""plain"" ""x&mkdir pwned"" ""(1,2;3=4)"" ""a|b<c>d"" ""C:\dir\\""",
            File.ReadAllText(Path.Combine(Path.GetDirectoryName(batch)!, "out.txt")).Trim());
        Assert.False(Directory.Exists(Path.Combine(temp.Path, "pwned")));
        Assert.False(Directory.Exists(Path.Combine(Path.GetDirectoryName(batch)!, "pwned")));
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
