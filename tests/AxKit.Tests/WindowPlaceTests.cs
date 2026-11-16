using System.Drawing;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>A stored window place is fitted into a screen (repairs.md R-58).</summary>
public sealed class WindowPlaceTests
{
    private static readonly Rectangle Left = new(0, 0, 1920, 1040);
    private static readonly Rectangle Right = new(1920, 0, 1280, 984);

    [Fact]
    public void A_place_inside_a_screen_stays_as_it_is()
    {
        var place = new Rectangle(100, 80, 900, 600);
        Assert.Equal(place, WindowPlace.Fit(place, [Left, Right]));
    }

    [Fact]
    public void A_place_that_touches_a_screen_by_a_corner_is_moved_wholly_inside()
    {
        Assert.Equal(new Rectangle(1020, 440, 900, 600), WindowPlace.Fit(new Rectangle(1919, 1039, 900, 600), [Left]));
        Assert.Equal(new Rectangle(0, 0, 900, 600), WindowPlace.Fit(new Rectangle(-899, -599, 900, 600), [Left]));
    }

    [Fact]
    public void A_place_larger_than_the_screen_is_made_the_screens_size()
    {
        Assert.Equal(Left, WindowPlace.Fit(new Rectangle(50, 50, 100000, 100000), [Left]));
    }

    [Fact]
    public void A_place_across_two_screens_goes_to_the_one_it_shares_the_most_with()
    {
        Assert.Equal(new Rectangle(1920, 100, 900, 600), WindowPlace.Fit(new Rectangle(1700, 100, 900, 600), [Left, Right]));
        Assert.Equal(new Rectangle(1020, 100, 900, 600), WindowPlace.Fit(new Rectangle(1200, 100, 900, 600), [Left, Right]));
    }

    [Fact]
    public void A_place_on_no_screen_gives_none()
    {
        Assert.Null(WindowPlace.Fit(new Rectangle(5000, 5000, 900, 600), [Left, Right]));
        Assert.Null(WindowPlace.Fit(new Rectangle(100, 100, 900, 600), []));
    }
}
