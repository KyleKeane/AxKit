using System.Reflection;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The hold on the answering keys (<see cref="KeyHold"/>, docs/axKit/SPEC.md AX-6.4), with a clock of the test's own:
/// what appears by itself holds for its moment whatever came before, and a hold that runs is never shortened. The
/// question area's use of it, on the control itself, is <see cref="ControlAreaHoldTests"/>.
/// </summary>
public sealed class KeyHoldTests
{
    [Fact]
    public void Nothing_is_held_before_a_hold_starts_and_everything_until_it_ends()
    {
        var hold = new KeyHold();
        Assert.False(hold.Holding(now: 5_000));
        hold.Start(now: 5_000);
        Assert.True(hold.Holding(5_000));
        Assert.True(hold.Holding(5_000 + KeyHold.DefaultMs - 1));
        Assert.False(hold.Holding(5_000 + KeyHold.DefaultMs));
    }

    /// <summary>
    /// The review of 2026-11-27: after an answer the keys were held for three seconds; the next question came 600 ms
    /// later, shown as "the user's doing", which cleared the hold, and a doubled Enter approved it unheard.
    /// </summary>
    [Fact]
    public void A_hold_that_runs_is_never_shortened_by_what_starts_after_it()
    {
        var hold = new KeyHold();
        hold.Start(now: 10_000, milliseconds: 3_000);
        // The next question is shown 600 ms after the answer: its own moment ends before the hold that runs.
        hold.Start(now: 10_600);
        Assert.True(hold.Holding(11_700));
        Assert.True(hold.Holding(12_999));
        Assert.False(hold.Holding(13_000));
        // A hold of no length, or one that would end in the past, changes nothing either.
        hold.Start(now: 20_000, milliseconds: 3_000);
        hold.Start(now: 20_100, milliseconds: 0);
        hold.Start(now: 20_200, milliseconds: -5_000);
        Assert.True(hold.Holding(22_999));
    }

    [Fact]
    public void What_is_shown_after_a_hold_has_ended_holds_for_its_own_moment()
    {
        var hold = new KeyHold();
        hold.Start(now: 10_000, milliseconds: 3_000);
        // Four seconds after the answer nothing is held; a question that arrives then is held from its own arrival.
        Assert.False(hold.Holding(14_000));
        hold.Start(now: 14_000);
        Assert.True(hold.Holding(14_000 + KeyHold.DefaultMs - 1));
        Assert.False(hold.Holding(14_000 + KeyHold.DefaultMs));
    }

    /// <summary>
    /// The hold is the one thing between a doubled Enter and a yes the user never heard, once every question starts
    /// on its confirming answer: it has no member that ends a hold early, and none may be added. A new public member
    /// fails here, to be looked at with that in mind.
    /// </summary>
    [Fact]
    public void The_hold_has_no_way_to_end_early()
    {
        var members = typeof(KeyHold).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(member => member.MemberType is not MemberTypes.Constructor)
            .Select(member => member.Name)
            .Order(StringComparer.Ordinal);
        Assert.Equal(["DefaultMs", "Holding", "Start"], members);
    }

    [Fact]
    public void No_order_of_starts_ever_moves_the_end_of_a_hold_earlier()
    {
        // Every order of a few starts: after each, the keys are held at least as long as they were before it.
        var random = new Random(20261127);
        for (var round = 0; round < 500; round++)
        {
            var hold = new KeyHold();
            long now = 1_000;
            long heldUntil = 0;
            for (var step = 0; step < 6; step++)
            {
                now += random.Next(0, 2_500);
                var milliseconds = random.Next(-500, 4_000);
                hold.Start(now, milliseconds);
                heldUntil = Math.Max(heldUntil, now + milliseconds);
                Assert.Equal(now < heldUntil, hold.Holding(now));
                Assert.True(heldUntil <= now || hold.Holding(heldUntil - 1));
                Assert.False(hold.Holding(Math.Max(heldUntil, now)));
            }
        }
    }
}
