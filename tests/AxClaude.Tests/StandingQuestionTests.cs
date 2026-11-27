using AxClaude.Core.Transcript;
using static AxClaude.Tests.TestHelpers;

namespace AxClaude.Tests;

/// <summary>
/// What a question shows while it stands, in every recording of Claude Code's screen that has its chunk index
/// (SPEC.md D33; the third review of 2026-11-27). The window tells a question from the one that replaced it under
/// the same title with the same answers by everything it shows between the two (<see cref="Question.Content"/>,
/// <see cref="AnsweredWait.Replaced"/>), and binds a typed answer to it the same way (<see cref="QueuedWrite"/>).
/// That is only right while one question standing never changes those rows by itself: a row that did (a counter, a
/// line drawn late) would make the window ask the same question again, chime and all, and drop an answer being
/// typed. So the recordings are replayed frame by frame here, and the test fails when a question that stands, with
/// nothing else between, shows other rows than the frame before; it then names the recording and the rows, and the
/// comparison has to leave that kind of row out, as the reader leaves out the tip Claude Code puts inside a
/// permission question.
/// </summary>
public sealed class StandingQuestionTests
{
    private static IEnumerable<string> Names() =>
        Directory.GetFiles(Fixtures.Folder(), "*claude*.vt").Order(StringComparer.Ordinal)
            .Where(file => File.Exists(file + ".chunks.txt"))
            .Select(file => Path.GetFileName(file)!);

    public static TheoryData<string> Recordings()
    {
        var data = new TheoryData<string>();
        foreach (var fixture in Names())
        {
            data.Add(fixture);
        }

        return data;
    }

    /// <summary>The recording replayed by its chunks with the app's quiet rule; what changed in a standing question, and in how many frames a question stood as in the frame before.</summary>
    private static (List<string> Changes, int Stood) Replay(string fixture)
    {
        var (columns, rows) = FixtureSize(fixture);
        var model = new SessionModel(columns, rows);
        var bytes = Fixture(fixture);
        var changes = new List<string>();
        var stood = 0;
        Question? standing = null;
        var previousMs = 0;
        model.Changed += () =>
        {
            var now = model.PendingQuestion;
            if (standing is not null && now is not null && now.Signature == standing.Signature)
            {
                stood++;
                if (now.Content != standing.Content)
                {
                    changes.Add($"\"{standing.Title}\": [{string.Join(" | ", standing.Text)}] became [{string.Join(" | ", now.Text)}]");
                }
            }

            standing = now;
        };
        foreach (var chunk in Chunks(fixture))
        {
            if (chunk.Ms - previousMs >= 100)
            {
                model.EndFrame();
            }

            model.Feed(bytes.AsSpan(chunk.Offset, chunk.Length));
            previousMs = chunk.Ms;
        }

        model.EndFrame();
        return (changes, stood);
    }

    [Theory]
    [MemberData(nameof(Recordings))]
    public void A_question_that_stands_shows_the_same_rows_in_every_frame(string fixture)
    {
        var changes = Replay(fixture).Changes;
        Assert.True(changes.Count == 0, $"{fixture}: a question that stood changed what it shows between its title and its answers: {string.Join("; ", changes)}");
    }

    /// <summary>The check above looks at something: the recordings hold questions that stand over several frames, a permission question among them.</summary>
    [Fact]
    public void The_recordings_hold_questions_that_stand_over_several_frames()
    {
        var stood = Names().ToDictionary(fixture => fixture, fixture => Replay(fixture).Stood);
        Assert.True(stood.Values.Sum() >= 20, $"Only {stood.Values.Sum()} frames of all recordings show a question that stood: the check holds too little.");
        Assert.Contains(stood, pair => pair.Key.StartsWith("permission-prompt", StringComparison.Ordinal) && pair.Value > 0);
    }
}
