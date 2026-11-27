using AxClaude.Core.Transcript;
using AxKit.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// Claude Code's row "You've used 76% of your weekly limit · resets …" under the mode line, in the place of the effort
/// row (2.1.332, 2026-11-26; ClaudeCodeTexts.UsageLimitRow). Unknown, it closed the block of interface rows under it:
/// at the idle prompt the mode line above it stood in the conversation, the next message's block was placed under
/// those rows, and the next frame wrote the reply's first row there, in front of the block's output marker, so that
/// no reply line was spoken and no "Responding" was said (tests/scripts/axClaude/17-what-is-spoken.txt, "Done. green
/// apples" heard as "Done"; 06-queue.txt found "# Input 2" too early the same evening). The fixture draws the row while
/// the fourth turn runs and keeps it under the mode line after it; its reset is masked (Redaction.MaskRecording).
/// </summary>
public class UsageLimitRowTests
{
    private const string Fixture = "usage-limit-claude2.1.332-240x50.vt";

    private const string TwoLines = "Reply with exactly two lines and nothing else: first line the words pelican feathers, second line the words green apples.";

    private static readonly string[] Messages = ["Reply with the single word one.", "Reply with the single word two.", "Reply with the single word three.", TwoLines];

    [Fact]
    public void The_usage_row_is_interface_and_the_reply_stands_under_its_output_marker()
    {
        var (columns, rows) = FixtureSize(Fixture);
        var model = new SessionModel(columns, rows);
        var arrivals = new Arrivals();
        var spoken = new List<string>();
        var responding = 0;
        var ready = false;
        model.ResponseStarted += () => responding++;
        model.Changed += () =>
        {
            if (arrivals.Update(model.Lines, model.Working, ready ? ReplySpeechMode.All : ReplySpeechMode.None, toolCalls: false).Speech is { } speech)
            {
                spoken.Add(speech);
            }

            if (!ready && model.Mode is not null && !model.Working && !model.PromptPending)
            {
                ready = true;
                arrivals.Skip(model.Lines);
            }
        };
        Replay(model, Fixture, Messages);
        var texts = Visible(model);

        Assert.DoesNotContain(texts, text => text.StartsWith("You've used", StringComparison.Ordinal) || text.StartsWith("manual mode on", StringComparison.Ordinal));
        var output = texts.IndexOf("# Output 4 Reply from Claude");
        Assert.True(output > 0 && texts.IndexOf("claude: pelican feathers") > output, string.Join(" | ", texts));
        Assert.Equal("green apples", texts[texts.IndexOf("claude: pelican feathers") + 1]);

        // Every turn's reply is spoken, the two lines of the last one included, and each says Responding.
        Assert.Equal(["claude: one", "claude: two", "claude: three"], spoken.Take(3));
        Assert.Equal("claude: pelican feathers. green apples", string.Join(". ", spoken.Skip(3)));
        Assert.Equal(4, responding);
    }

    [Theory]
    [InlineData("You've used 76% of your weekly limit · resets 11pm (Europe/Lisbon)")]
    [InlineData("You've used 90% of your session limit · resets 3:30pm")]
    [InlineData("You've used 80% of your weekly limit")]
    public void The_usage_row_is_chrome(string row) => Assert.True(LineClassifier.IsChrome(row));

    [Theory]
    [InlineData("You've used the wrong file.")]
    [InlineData("claude: You've used 76% of your weekly limit")]
    public void A_reply_about_usage_is_not_chrome(string row) => Assert.False(LineClassifier.IsChrome(row));
}
