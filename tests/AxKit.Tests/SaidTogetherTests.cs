using AxKit.Core;
using AxKit.Core.Mcp;

namespace AxKit.Tests;

/// <summary>docs/axKit/SPEC.md AX-7.3: several announcements said as one, the last with its own ending.</summary>
public class SaidTogetherTests
{
    [Fact]
    public void The_pieces_are_joined_by_one_full_stop()
    {
        Assert.Equal("Ready. Sent. Done", SaidTogether.Sentence(["Ready", "Sent", "Done"]));

        // A piece followed by another loses its own full stops and spaces: the join never doubles them.
        Assert.Equal("Ready. Sent", SaidTogether.Sentence(["Ready.", "Sent"]));
        Assert.Equal("Ready. Sent", SaidTogether.Sentence(["Ready... ", "Sent"]));
    }

    [Fact]
    public void The_last_piece_arrives_as_it_was_given()
    {
        // mcp.md MC-7.5: a command's own words after the words that say who ran it, its full stop included.
        const string words = "Block quote, A block quote, line one.";
        Assert.Equal(McpRun.Said("axAutomate", "Block quote") + ". " + words, SaidTogether.Sentence([McpRun.Said("axAutomate", "Block quote"), words]));
        Assert.Equal("Ready. Sent.", SaidTogether.Sentence(["Ready.", "Sent."]));
        Assert.Equal("Sent.", SaidTogether.Sentence(["Sent."]));
    }

    [Fact]
    public void A_piece_of_nothing_is_left_out()
    {
        Assert.Equal("Ready. Done", SaidTogether.Sentence(["Ready", string.Empty, " ", ".", "Done"]));

        // Also at the end: the piece before it is then the last, and keeps its ending.
        Assert.Equal("Ready.", SaidTogether.Sentence(["Ready.", string.Empty]));
        Assert.Equal(string.Empty, SaidTogether.Sentence([]));
        Assert.Equal(string.Empty, SaidTogether.Sentence([". "]));
    }
}
