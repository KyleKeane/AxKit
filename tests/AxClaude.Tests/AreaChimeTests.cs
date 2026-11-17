using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>The chime of what the control area shows (FR-7.3, FR-2.12): the notice chime gives way to the louder alert in the background; an offered command's chime never does.</summary>
public class AreaChimeTests
{
    [Theory]
    // A question of Claude's: the notice chime, unless the alert takes its place in the background.
    [InlineData(true, true, false, true, false, true)]
    [InlineData(true, true, false, false, false, true)]
    [InlineData(true, true, true, true, false, true)]
    [InlineData(true, true, true, false, false, false)]
    // An offered command: its own chime wherever the window is, the alert on or off.
    [InlineData(true, true, false, true, true, true)]
    [InlineData(true, true, false, false, true, true)]
    [InlineData(true, true, true, true, true, true)]
    [InlineData(true, true, true, false, true, true)]
    // Never for what the user caused, and never with the chimes off.
    [InlineData(false, true, false, true, false, false)]
    [InlineData(false, true, true, false, true, false)]
    [InlineData(true, false, false, true, false, false)]
    [InlineData(true, false, true, false, true, false)]
    public void The_chime_plays_as_the_rule_says(bool unprompted, bool chimesOn, bool backgroundAlert, bool windowHasFocus, bool offer, bool plays) =>
        Assert.Equal(plays, AreaChime.Plays(unprompted, chimesOn, backgroundAlert, windowHasFocus, offer));
}
