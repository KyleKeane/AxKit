using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AxAutomate.Core;

/// <summary>What an expectation took: the events, and whether the text was found later in the announcement the last one matched.</summary>
public sealed record Taken(List<Event> Events, bool Within);

/// <summary>One event of a window's timeline: when, and what (an announcement, a sound, a focus move, a value change).</summary>
public sealed record Event(long Ms, string Kind, string Text, bool Interrupt)
{
    /// <summary>As a line of the report and as the text an expectation matches: "! " an interrupting announcement, "♪ " a sound, "→ " a focus move, "· " a value.</summary>
    public string Spoken => Kind switch
    {
        "say" => (Interrupt ? "! " : string.Empty) + Text,
        "sound" => "♪ " + Text,
        "focus" => "→ " + Text,
        _ => "· " + Text,
    };
}
