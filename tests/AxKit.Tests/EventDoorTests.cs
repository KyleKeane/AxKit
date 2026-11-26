using System.Text.RegularExpressions;
using AxKit.Core.Events;

namespace AxKit.Tests;

/// <summary>
/// The door holds (docs/axKit/events.md EV-5.1, EV-6.2): read off the sources, as <see cref="SecondWindowTests"/>
/// reads them. No file under <c>src</c> plays a sound but the door; every raise names its kind in so many words, so
/// that each can be found and checked here; a sound is named at a raise only for the kinds that still have two; and
/// the door sends each sound to the method of its own name. The apps cannot reach <c>Sounds</c> at all, which is
/// internal to AxKit.Forms: for them the compiler holds the first of these.
/// </summary>
public sealed partial class EventDoorTests
{
    private static readonly string Door = Path.Combine("src", "AxKit.Forms", "EventDoor.cs");
    private static readonly string SoundsFile = Path.Combine("src", "AxKit.Forms", "Sounds.cs");

    /// <summary>The automation channel records each sound played, by the one event of <c>Sounds</c> it listens to.</summary>
    private static readonly string Channel = Path.Combine("src", "AxKit.Forms", "AutomationChannel.cs");

    [GeneratedRegex(@"\bSounds\.(\w+)")]
    private static partial Regex SoundsMember();

    /// <summary>A raise, from its name to the bracket that closes it; a raise is written on one line.</summary>
    [GeneratedRegex(@"\bEventDoor\.Raise\(([^()]*)\)")]
    private static partial Regex RaiseCall();

    [GeneratedRegex(@"\bEventDoor\.Raise\b")]
    private static partial Regex RaiseName();

    /// <summary>What a raise may hold between its brackets: the kind by name, and for a kind with two sounds the sound by name.</summary>
    [GeneratedRegex(@"^\s*EventKind\.(\w+)\s*(?:,\s*EventSound\.(\w+)\s*)?$")]
    private static partial Regex RaiseArguments();

    [GeneratedRegex(@"case EventSound\.(\w+): Sounds\.(\w+)\(\); break;")]
    private static partial Regex DoorCase();

    /// <summary>Every source file under src with its path from the repository's root and its lines without their comments.</summary>
    private static IEnumerable<(string Relative, IReadOnlyList<string> Code)> Sources()
    {
        var root = SourceTexts.RepositoryRoot();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            // A comment may name a sound or a raise to say what happens; a line is read up to its comment.
            yield return (relative, File.ReadLines(file).Select(line => line.Split("//")[0]).ToList());
        }
    }

    [Fact]
    public void A_sound_is_played_by_the_door_alone()
    {
        var offenders = new List<string>();
        foreach (var (relative, code) in Sources())
        {
            if (relative.Equals(Door, StringComparison.OrdinalIgnoreCase) || relative.Equals(SoundsFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            for (var i = 0; i < code.Count; i++)
            {
                foreach (Match match in SoundsMember().Matches(code[i]))
                {
                    if (relative.Equals(Channel, StringComparison.OrdinalIgnoreCase) && match.Groups[1].Value == "Played")
                    {
                        continue;
                    }

                    offenders.Add($"{relative}:{i + 1}: {code[i].Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "A sound played outside the door (docs/axKit/events.md EV-5.1). Raise the kind of event that happened, EventDoor.Raise(EventKind.<kind>), and let the table give it its sound: "
            + string.Join("; ", offenders));
    }

    [Fact]
    public void Every_raise_names_its_kind_and_names_a_sound_only_where_two_are_left()
    {
        var offenders = new List<string>();
        var raises = 0;
        foreach (var (relative, code) in Sources())
        {
            if (relative.Equals(Door, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            for (var i = 0; i < code.Count; i++)
            {
                var named = RaiseName().Matches(code[i]).Count;
                if (named == 0)
                {
                    continue;
                }

                var calls = RaiseCall().Matches(code[i]);
                if (calls.Count != named)
                {
                    offenders.Add($"{relative}:{i + 1}: a raise is written whole on one line, with nothing but names between its brackets: {code[i].Trim()}");
                    continue;
                }

                foreach (Match call in calls)
                {
                    raises++;
                    var arguments = RaiseArguments().Match(call.Groups[1].Value);
                    if (!arguments.Success || !Enum.TryParse<EventKind>(arguments.Groups[1].Value, out var kind))
                    {
                        offenders.Add($"{relative}:{i + 1}: a raise names its kind in so many words, EventDoor.Raise(EventKind.<kind>): {code[i].Trim()}");
                        continue;
                    }

                    var sounds = EventPresenter.SoundsOf(kind);
                    if (!arguments.Groups[2].Success)
                    {
                        if (sounds.Count != 1)
                        {
                            offenders.Add($"{relative}:{i + 1}: {kind} has {sounds.Count} sounds and is raised by its name alone: {code[i].Trim()}");
                        }

                        continue;
                    }

                    if (!Enum.TryParse<EventSound>(arguments.Groups[2].Value, out var sound) || sounds.Count < 2 || !sounds.Contains(sound))
                    {
                        offenders.Add($"{relative}:{i + 1}: a sound is named at a raise only for a kind that still has two, and is one of them: {code[i].Trim()}");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, "Raises the door's rules do not allow (docs/axKit/events.md EV-3.3, EV-5.1): " + string.Join("; ", offenders));
        Assert.True(raises > 0, "No raise was found under src: the pattern this test reads the sources with no longer fits the code.");
    }

    [Fact]
    public void The_door_sends_each_sound_to_the_method_of_its_name()
    {
        var root = SourceTexts.RepositoryRoot();
        var door = File.ReadAllText(Path.Combine(root, Door));
        var cases = DoorCase().Matches(door).Select(match => (Sound: match.Groups[1].Value, Method: match.Groups[2].Value)).ToList();
        Assert.All(cases, pair => Assert.Equal(pair.Sound, pair.Method));

        // Every sound but the two that last, each once.
        var lasting = new[] { nameof(EventSound.Music), nameof(EventSound.BackgroundChord) };
        Assert.Equal(
            Enum.GetNames<EventSound>().Except(lasting).Order(StringComparer.Ordinal),
            cases.Select(pair => pair.Sound).Order(StringComparer.Ordinal));

        // And Sounds has a method of that name for each, which takes nothing.
        var sounds = File.ReadAllText(Path.Combine(root, SoundsFile));
        Assert.All(cases, pair => Assert.Contains($"static void {pair.Method}() =>", sounds, StringComparison.Ordinal));
    }

    /// <summary>
    /// The words go through the door (events.md section 9 point 4, 2026-11-25): the one notification of AX-7.1 is
    /// raised by the door alone, so that every announcement of a window, a notice or a shared control comes there with
    /// its kind; the automation channel and the AI tools' channel only listen to what was said. <c>Announcer</c> is
    /// internal, so the apps cannot reach it; inside the shared pieces this holds it.
    /// </summary>
    [Fact]
    public void Words_are_said_through_the_door_alone()
    {
        var listeners = new[] { Channel, Path.Combine("src", "AxKit.Forms", "McpChannel.cs") };
        var offenders = new List<string>();
        foreach (var (relative, code) in Sources())
        {
            if (relative.Equals(Door, StringComparison.OrdinalIgnoreCase) || relative.Equals(Path.Combine("src", "AxKit.Forms", "Announcer.cs"), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            for (var i = 0; i < code.Count; i++)
            {
                foreach (Match match in AnnouncerMember().Matches(code[i]))
                {
                    if (match.Groups[1].Value == "Raised" && listeners.Any(listener => relative.Equals(listener, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    offenders.Add($"{relative}:{i + 1}: {code[i].Trim()}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "Words said outside the door (docs/axKit/events.md section 10). Say them with their kind, the window's Announce(EventKind.<kind>, ...) or EventDoor.SayOn(EventKind.<kind>, control, ...): "
            + string.Join("; ", offenders));
        var announcer = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "Announcer.cs"));
        Assert.Contains("internal static class Announcer", announcer, StringComparison.Ordinal);
    }

    /// <summary>The door's <c>Speak</c> says words raised at the door already; only the window's own speaking (its joined sentence, what waited for the activation) and the notice it speaks on call it.</summary>
    [Fact]
    public void Words_said_later_were_raised_at_the_door_first()
    {
        var allowed = new[] { Path.Combine("src", "AxKit.Forms", "AxKitForm.Notices.cs"), Path.Combine("src", "AxKit.Forms", "OverlayPanel.cs") };
        var offenders = Sources()
            .Where(source => !allowed.Any(file => source.Relative.Equals(file, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(source => source.Code.Select((line, i) => (source.Relative, Line: line, Number: i + 1)))
            .Where(line => line.Line.Contains("EventDoor.Speak(", StringComparison.Ordinal))
            .Select(line => $"{line.Relative}:{line.Number}")
            .ToList();
        Assert.True(offenders.Count == 0, "EventDoor.Speak called outside the window's own speaking: " + string.Join("; ", offenders));

        // And the window's Announce without a kind raises its words at the door, as said.
        var notices = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), allowed[0]));
        Assert.Contains("protected internal bool Announce(string text, bool interrupt) => EventDoor.Say(EventKind.Said, this, text, interrupt);", notices, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\bAnnouncer\.(\w+)")]
    private static partial Regex AnnouncerMember();

    /// <summary>The apps cannot play a sound themselves: <c>Sounds</c> is not public, so only AxKit.Forms can name it, and there the first test holds.</summary>
    [Fact]
    public void Sounds_is_internal_to_the_shared_window_pieces()
    {
        var sounds = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), SoundsFile));
        Assert.Contains("\ninternal static class Sounds", sounds.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.DoesNotContain("public static class Sounds", sounds, StringComparison.Ordinal);
    }
}
