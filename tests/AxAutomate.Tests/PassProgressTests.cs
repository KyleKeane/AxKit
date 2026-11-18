using AxAutomate.Core;

namespace AxAutomate.Tests;

/// <summary>
/// What Windows' voice says while an NVDA pass runs (PassProgress; the user's ask of 2026-11-18): the test's number,
/// how many come after it, and about how long the rest takes, from the times the last pass left.
/// </summary>
public sealed class PassProgressTests
{
    [Fact]
    public void The_warning_names_the_test_the_tests_after_it_and_the_time_left()
    {
        Assert.Equal(
            "Running NVDA test 3 of 172. Please do not touch the keyboard or mouse. 169 tests remaining. About 1 hour 10 minutes left.",
            PassProgress.Words(3, 172, 4200));
        Assert.Equal(
            "Running NVDA test 8 of 9. Please do not touch the keyboard or mouse. 1 test remaining. About 2 minutes left.",
            PassProgress.Words(8, 9, 110));
        Assert.Equal(
            "Running NVDA test 9 of 9. Please do not touch the keyboard or mouse. This is the last test. Less than a minute left.",
            PassProgress.Words(9, 9, 20));
    }

    [Fact]
    public void Without_a_time_the_warning_says_none()
    {
        Assert.Equal(
            "Running NVDA test 1 of 9. Please do not touch the keyboard or mouse. 8 tests remaining.",
            PassProgress.Words(1, 9, null));
        Assert.Equal("Running NVDA tests. Please do not touch the keyboard or mouse.", PassProgress.Starting);
    }

    [Theory]
    [InlineData(0, "Less than a minute left.")]
    [InlineData(44, "Less than a minute left.")]
    [InlineData(45, "About 1 minute left.")]
    [InlineData(89, "About 1 minute left.")]
    [InlineData(90, "About 2 minutes left.")]
    [InlineData(59 * 60, "About 59 minutes left.")]
    [InlineData(60 * 60, "About 1 hour left.")]
    [InlineData(62 * 60, "About 1 hour left.")]
    [InlineData(63 * 60, "About 1 hour 5 minutes left.")]
    [InlineData(75 * 60, "About 1 hour 15 minutes left.")]
    [InlineData(118 * 60, "About 2 hours left.")]
    [InlineData(125 * 60, "About 2 hours 5 minutes left.")]
    public void A_time_is_said_to_the_minute_and_from_an_hour_on_to_five(int seconds, string words) =>
        Assert.Equal(words, PassProgress.Left(seconds));

    [Fact]
    public void The_time_left_is_what_the_coming_scripts_took_the_last_time()
    {
        var remembered = PassProgress.Read("""{"axClaude\\01-start.txt":120,"axCalc\\01-basics.txt":15.5,"axDown\\01-start.txt":30}""");

        // The one that starts now is among them; a name is found whatever its case.
        Assert.Equal(135.5, PassProgress.SecondsLeft(["AXCLAUDE\\01-start.txt", "axCalc\\01-basics.txt"], remembered, 0, 0));
        Assert.Equal(0, PassProgress.SecondsLeft([], remembered, 0, 0));
    }

    [Fact]
    public void A_script_without_a_kept_time_counts_as_the_average_of_this_run_or_else_of_what_is_kept()
    {
        var remembered = PassProgress.Read("""{"a.txt":10,"b.txt":30}""");

        // This run has finished two scripts in 100 seconds: a new script counts as 50.
        Assert.Equal(60, PassProgress.SecondsLeft(["a.txt", "new.txt"], remembered, 2, 100));
        // Nothing finished yet: a new script counts as the average of what is kept, 20.
        Assert.Equal(30, PassProgress.SecondsLeft(["a.txt", "new.txt"], remembered, 0, 0));
        // The first pass on a computer, from its second script on: the average of this run alone.
        Assert.Equal(80, PassProgress.SecondsLeft(["x.txt", "y.txt"], PassProgress.Read(null), 1, 40));
    }

    [Fact]
    public void With_nothing_known_no_time_is_given()
    {
        Assert.Null(PassProgress.SecondsLeft(["x.txt", "y.txt"], PassProgress.Read(null), 0, 0));
    }

    [Fact]
    public void The_times_are_written_in_the_order_of_their_names_and_read_back()
    {
        var times = new Dictionary<string, double> { ["zz-nvda\\b.txt"] = 12.34, ["axCalc\\01-basics.txt"] = 15, ["axClaude\\37-stream-turn.txt"] = 41.96 };

        var text = PassProgress.Write(times);
        var read = PassProgress.Read(text);

        Assert.True(text.IndexOf("axCalc", StringComparison.Ordinal) < text.IndexOf("axClaude", StringComparison.Ordinal));
        Assert.True(text.IndexOf("axClaude", StringComparison.Ordinal) < text.IndexOf("zz-nvda", StringComparison.Ordinal));
        Assert.Equal(3, read.Count);
        Assert.Equal(12.3, read["zz-nvda\\b.txt"]);
        Assert.Equal(15, read["axcalc\\01-BASICS.txt"]);
        Assert.Equal(42, read["axClaude\\37-stream-turn.txt"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("""{"a.txt":12""")]
    [InlineData("""["a.txt",12]""")]
    [InlineData("12")]
    public void A_text_that_is_not_the_file_gives_no_times(string? text) =>
        Assert.Empty(PassProgress.Read(text));

    [Fact]
    public void A_value_that_is_not_a_time_is_passed_over()
    {
        var read = PassProgress.Read("""{"a.txt":12,"b.txt":"long","c.txt":-3,"d.txt":0,"e.txt":999999,"f.txt":null,"g.txt":{"s":1}}""");

        Assert.Equal(["a.txt"], read.Keys);
    }
}
