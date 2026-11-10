using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AxDescribe;

/// <summary>
/// The picture of a window (SPEC.md DE-2.1): what is on the screen inside the window's rectangle, as a PNG file,
/// taken before axDescribe's own window shows so that nothing covers it. A window that gives no rectangle (gone,
/// minimised, the desktop) means the whole screen it was on. The process is per-monitor DPI aware, so the rectangle
/// and the copy are in the same physical pixels.
/// </summary>
public static class ScreenPicture
{
    /// <summary>Takes the picture into <paramref name="path"/>, the folder made; the reason when it could not be taken, null when it was.</summary>
    public static string? Capture(nint window, string path)
    {
        try
        {
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1024, 768);
            if (window != 0 && GetWindowRect(window, out var rect) && rect.Right > rect.Left && rect.Bottom > rect.Top)
            {
                var area = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
                // Kept to the screen the window is on: a window dragged half off it gives a rectangle past the edge.
                bounds = Rectangle.Intersect(area, Screen.FromHandle(window).Bounds);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    bounds = Screen.FromHandle(window).Bounds;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
            }

            bitmap.Save(path, ImageFormat.Png);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or ArgumentException or ExternalException)
        {
            return ex.Message;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT rect);
}
