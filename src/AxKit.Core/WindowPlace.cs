using System.Drawing;

namespace AxKit.Core;

/// <summary>
/// Where a window stored in a settings file may be put (repairs.md R-58): a place a damaged or hand-edited file
/// gives (a huge size, a corner that touches a screen by one pixel) is fitted into a screen, so that a window never
/// opens where it cannot be found. Pure; the window base asks it with the working areas of the screens.
/// </summary>
public static class WindowPlace
{
    /// <summary>
    /// The place fitted into the working area it shares the most with: no larger than that area, and wholly inside
    /// it. Null when it meets no working area at all (a screen that is gone): the window then opens where Windows puts it.
    /// </summary>
    public static Rectangle? Fit(Rectangle place, IReadOnlyList<Rectangle> workingAreas)
    {
        Rectangle? best = null;
        long most = 0;
        foreach (var area in workingAreas)
        {
            var shared = Rectangle.Intersect(area, place);
            var size = (long)shared.Width * shared.Height;
            if (size > most)
            {
                most = size;
                best = area;
            }
        }

        if (best is not { } screen)
        {
            return null;
        }

        var width = Math.Min(place.Width, screen.Width);
        var height = Math.Min(place.Height, screen.Height);
        var x = Math.Clamp(place.X, screen.Left, screen.Right - width);
        var y = Math.Clamp(place.Y, screen.Top, screen.Bottom - height);
        return new Rectangle(x, y, width, height);
    }
}
