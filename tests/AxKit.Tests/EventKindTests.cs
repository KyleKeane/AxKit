using System.Text.Json;
using AxKit.Core.Events;

namespace AxKit.Tests;

/// <summary>
/// The kinds of event and their sounds in code (<see cref="EventKind"/>, <see cref="EventSound"/>,
/// <see cref="EventPresenter"/>; docs/axKit/events.md EV-3, EV-5.2, EV-6.4) against docs/axKit/families.json, where
/// they are written for people: the same kinds in the same order, the same sounds for each, both ways. A kind or a
/// sound added in one place alone fails here.
/// </summary>
public sealed class EventKindTests
{
    /// <summary>The kinds of the file in its order, each with the sounds the file gives it.</summary>
    private static IReadOnlyList<(string Id, IReadOnlyList<string> Sounds)> KindsOfTheFile()
    {
        using var document = Families();
        var kinds = new List<(string Id, IReadOnlyList<string> Sounds)>();
        foreach (var family in document.RootElement.GetProperty("events").EnumerateArray())
        {
            foreach (var member in family.GetProperty("members").EnumerateArray())
            {
                var sounds = member.TryGetProperty("sounds", out var named)
                    ? named.EnumerateArray().Select(sound => sound.GetString()!).ToList()
                    : [];
                kinds.Add((member.GetProperty("id").GetString()!, sounds));
            }
        }

        return kinds;
    }

    private static JsonDocument Families() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "docs", "axKit", "families.json")));

    [Fact]
    public void The_kinds_in_code_are_the_files_kinds_in_its_order()
    {
        var file = KindsOfTheFile().Select(kind => kind.Id).ToList();
        var code = Enum.GetValues<EventKind>().Select(EventPresenter.Id).ToList();
        Assert.True(file.SequenceEqual(code),
            "The kinds of event in AxKit.Core/Events/EventKind.cs and in docs/axKit/families.json differ. Only in the file: "
            + string.Join(", ", file.Except(code)) + ". Only in the code: " + string.Join(", ", code.Except(file))
            + ". When both lists hold the same kinds, their order differs: the enumeration follows the file's.");
    }

    [Fact]
    public void Each_kind_has_in_the_table_the_sounds_the_file_gives_it()
    {
        foreach (var (id, sounds) in KindsOfTheFile())
        {
            var kind = EventPresenter.Of(id);
            Assert.True(kind is not null, $"The file's kind '{id}' is no value of EventKind.");
            Assert.True(sounds.SequenceEqual(EventPresenter.SoundsOf(kind!.Value).Select(sound => sound.ToString())),
                $"'{id}': docs/axKit/families.json gives it the sounds [{string.Join(", ", sounds)}] and EventPresenter.SoundsOf gives [{string.Join(", ", EventPresenter.SoundsOf(kind.Value))}]. A sound is given to a kind in both places, by the maintainer's ear.");
        }
    }

    [Fact]
    public void The_sounds_in_code_are_the_files_sounds()
    {
        using var document = Families();
        var file = document.RootElement.GetProperty("sounds").EnumerateArray()
            .SelectMany(family => family.GetProperty("members").EnumerateArray())
            .Select(member => member.GetProperty("id").GetString()!)
            .Order(StringComparer.Ordinal).ToList();
        var code = Enum.GetNames<EventSound>().Order(StringComparer.Ordinal).ToList();
        Assert.Equal(file, code);
    }

    [Fact]
    public void Every_sound_is_the_sound_of_a_kind()
    {
        var used = Enum.GetValues<EventKind>().SelectMany(EventPresenter.SoundsOf).ToHashSet();
        Assert.DoesNotContain(Enum.GetValues<EventSound>(), sound => !used.Contains(sound));
    }

    /// <summary>
    /// EV-3.3: the kinds that still sound two ways, exactly these, each with its two sounds in the file's order. One
    /// leaves this list when the maintainer has decided it by ear (one sound, or two kinds), and none joins it.
    /// </summary>
    [Fact]
    public void Three_kinds_still_have_two_sounds_and_no_other_kind_has()
    {
        var two = Enum.GetValues<EventKind>().Where(kind => EventPresenter.SoundsOf(kind).Count > 1)
            .Select(kind => $"{kind}: {string.Join(", ", EventPresenter.SoundsOf(kind))}").ToList();
        Assert.Equal(
            [
                "NothingFound: Nothing, Edge",
                "NoCommand: Unassigned, Edge",
                "ProgramAsks: Notice, Question",
            ],
            two);
    }

    [Fact]
    public void A_kind_has_the_sounds_of_the_table_and_no_other()
    {
        Assert.Equal([EventSound.Edge], EventPresenter.SoundsOf(EventKind.Edge));
        Assert.Equal([EventSound.Nothing, EventSound.Edge], EventPresenter.SoundsOf(EventKind.NothingFound));
        Assert.Empty(EventPresenter.SoundsOf(EventKind.Landed));
        Assert.Empty(EventPresenter.SoundsOf(EventKind.Upkeep));
        Assert.True(EventPresenter.Allows(EventKind.Edge, EventSound.Edge));
        Assert.True(EventPresenter.Allows(EventKind.NothingFound, EventSound.Edge));
        Assert.True(EventPresenter.Allows(EventKind.NothingFound, EventSound.Nothing));
        Assert.False(EventPresenter.Allows(EventKind.NothingFound, EventSound.Notice));
        Assert.False(EventPresenter.Allows(EventKind.Landed, EventSound.Edge));

        // A number that is no kind has no sound, and asking is no fault.
        Assert.Empty(EventPresenter.SoundsOf((EventKind)(-1)));
        Assert.Empty(EventPresenter.SoundsOf((EventKind)10_000));
        Assert.False(EventPresenter.Allows((EventKind)10_000, EventSound.Edge));
    }

    /// <summary>The table is made once and asked by the kind's number: the same list every time, and the kinds numbered from 0 in the order they are written.</summary>
    [Fact]
    public void The_table_is_made_once_and_the_kinds_are_numbered_in_their_order()
    {
        var kinds = Enum.GetValues<EventKind>();
        for (var i = 0; i < kinds.Length; i++)
        {
            Assert.Equal(i, (int)kinds[i]);
            Assert.Same(EventPresenter.SoundsOf(kinds[i]), EventPresenter.SoundsOf(kinds[i]));
        }
    }

    [Fact]
    public void An_id_is_the_name_in_small_letters_with_hyphens_and_names_its_kind_again()
    {
        Assert.Equal("edge", EventPresenter.Id(EventKind.Edge));
        Assert.Equal("landed-on-heading", EventPresenter.Id(EventKind.LandedOnHeading));
        Assert.Equal("ai-asks-to-run", EventPresenter.Id(EventKind.AiAsksToRun));
        Assert.Equal("turn-self-started", EventPresenter.Id(EventKind.TurnSelfStarted));
        foreach (var kind in Enum.GetValues<EventKind>())
        {
            Assert.Equal(kind, EventPresenter.Of(EventPresenter.Id(kind)));
        }

        Assert.Null(EventPresenter.Of("no-such-kind"));
        Assert.Null(EventPresenter.Of("Edge"));
        Assert.Null(EventPresenter.Of(string.Empty));
    }
}
