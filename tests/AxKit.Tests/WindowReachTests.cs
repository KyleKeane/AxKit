using System.Text.RegularExpressions;

namespace AxKit.Tests;

/// <summary>
/// The windows and processes of other axKit processes are found in one place, <c>AxKit.Forms/BundleWindows</c>, which
/// leaves out, in a test run, everything that is not the run's own (RunMembership; docs/axKit/automation.md "Only the
/// run's own windows", 2026-11-15: a script's Close all windows reached the user's own window in the NVDA tier). Read
/// off the sources: a file of <c>src</c> that lists the system's windows or finds processes by name itself fails here,
/// unless it is one of the files below, each with why it cannot reach a window of the user's from a run.
/// </summary>
public sealed class WindowReachTests
{
    /// <summary>The calls that find windows or processes this process did not make.</summary>
    private static readonly Regex Finds = new(@"\b(EnumWindows|EnumDesktopWindows|EnumThreadWindows|FindWindowExW?|FindWindowW?|GetProcessesByName|GetProcesses)\s*\(", RegexOptions.Compiled);

    /// <summary>The files allowed to, with why.</summary>
    private static readonly IReadOnlyDictionary<string, string> Allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [Path.Combine("src", "AxKit.Forms", "BundleWindows.cs")] = "the one place, filtered by RunMembership",
        // The runner, not a window: Adopt joins a process only through its pipe with the run's secret, which no window
        // outside the run accepts; the windows of one process it started itself (ShownWindow); the run's own desktop.
        [Path.Combine("src", "AxAutomate.Core", "Run.cs")] = "the runner: the run's secret decides",
        [Path.Combine("src", "AxAutomate.Core", "Native.cs")] = "the runner: a process it started, or its own desktop",
        // Whether NVDA runs, for a report's lines: read, never messaged.
        [Path.Combine("src", "AxKit.Forms", "ReportFlow.cs")] = "reads whether NVDA runs",
        // axProcess is the list of every process on the computer; what it ends is the entry the user chose.
        [Path.Combine("src", "AxProcess.Core", "ProcessTable.cs")] = "axProcess lists every process",
        [Path.Combine("src", "AxProcess", "ProcessForm.cs")] = "axProcess counts every process",
    };

    [Fact]
    public void Other_processes_windows_are_found_in_the_one_filtered_place()
    {
        var root = SourceTexts.RepositoryRoot();
        var offenders = new List<string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                var code = line.TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal) || !Finds.IsMatch(code))
                {
                    continue;
                }

                if (Allowed.ContainsKey(relative))
                {
                    used.Add(relative);
                }
                else
                {
                    offenders.Add($"{relative}:{number}: {code}");
                }
            }
        }

        Assert.True(offenders.Count == 0, "Windows or processes of other programs found outside AxKit.Forms/BundleWindows, where a test run's window is kept from the user's own (RunMembership): " + string.Join("; ", offenders));
        // An entry no file needs any more goes: the list stays the true one.
        Assert.Equal(Allowed.Keys.Order(StringComparer.OrdinalIgnoreCase), used.Order(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void The_one_place_asks_the_runs_membership_wherever_it_finds_a_window()
    {
        var source = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "BundleWindows.cs"));
        // Visible filters the processes, Titled and Reachable go through the private Reachable.
        Assert.Contains("Where(RunMembership.Reaches)", source, StringComparison.Ordinal);
        Assert.Contains("ThisProgram.Runs(processId) && RunMembership.Reaches((int)processId)", source, StringComparison.Ordinal);
        Assert.Contains("owner != process && Reachable(owner)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void A_hand_over_is_sent_and_taken_only_through_that_check()
    {
        var source = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "AppMessage.cs"));
        Assert.Contains("private static bool IsThisProgram(nint window) => BundleWindows.Reachable(window);", source, StringComparison.Ordinal);
        // Both ends: the window a hand-over goes to, and the window it says it came from.
        Assert.Contains("if (!IsThisProgram(target))", source, StringComparison.Ordinal);
        Assert.Contains("IsThisProgram(m.WParam)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void A_process_of_a_run_takes_its_mark_before_anything_else_of_the_program()
    {
        var source = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit", "Program.cs"));
        var capture = source.IndexOf("AutomationEnvironment.Capture();", StringComparison.Ordinal);
        var join = source.IndexOf("RunMembership.Join();", StringComparison.Ordinal);
        var decide = source.IndexOf("Dispatch.Decide(args)", StringComparison.Ordinal);
        Assert.True(capture >= 0 && join > capture && decide > join, "Program.Main reads the run's variables, takes the run's mark, and only then decides which app starts.");
    }
}
