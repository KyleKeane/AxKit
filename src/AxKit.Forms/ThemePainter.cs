using System.Runtime.InteropServices;
using AxKit.Core;

namespace AxKit.Forms;

/// <summary>A control's class of the theme when its type does not say it (the console field and its band), or <see cref="ThemePainter.Fixed"/> for one the theme never paints.</summary>
public sealed record ThemeMark(string Class);

/// <summary>
/// Paints a window from the theme (docs/axKit/theme.md TH-2, TH-3): one walk over the control tree, each control's class
/// from its type or its <see cref="ThemeMark"/>, each class's two colours from the theme or, for <c>system</c> and for
/// no theme at all, the class's default, which is what every window had before themes existed (AX-7.7). The caption
/// goes through the desktop window manager (Windows 11; older Windows ignores the call). Nothing here moves the focus
/// or speaks. This is the one file of the bundle that names a colour (<c>ThemeTests</c>), axMedia's video surface apart.
/// </summary>
public static class ThemePainter
{
    public const string Window = "window";
    public const string Text = "text";
    public const string Transcript = "transcript";
    public const string List = "list";
    public const string Field = "field";
    public const string Band = "band";
    public const string Notice = "notice";
    public const string Status = "status";
    public const string Menu = "menu";
    public const string Button = "button";
    public const string TitleBar = "titleBar";

    /// <summary>A control the theme leaves as it is (axMedia's black video surface, AM-2.4).</summary>
    public const string Fixed = "fixed";

    // The console field's and its band's defaults (2026-11-08): a tint on the Window colour and a deeper one behind the
    // name, so the active field is told from the read-only view; picked for a light or a dark Window colour.
    private static readonly Color LightField = Color.FromArgb(0xEA, 0xF0, 0xFA);
    private static readonly Color DarkField = Color.FromArgb(0x33, 0x38, 0x40);
    private static readonly Color LightBand = Color.FromArgb(0xD2, 0xDD, 0xEF);
    private static readonly Color DarkBand = Color.FromArgb(0x23, 0x27, 0x2D);

    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;
    private const uint DWMWA_COLOR_DEFAULT = 0xFFFFFFFF;

    /// <summary>Gives <paramref name="control"/> a class its type does not say.</summary>
    public static void Mark(Control control, string themeClass) => control.Tag = new ThemeMark(themeClass);

    /// <summary>
    /// Paints <paramref name="root"/> and everything in it; with no theme, a theme switched off or Windows' high contrast
    /// on (TH-3.3), every class gets its default.
    /// </summary>
    public static void Apply(Control root, Theme? theme)
    {
        var active = theme is { Enabled: true } && !SystemInformation.HighContrast ? theme : null;
        Paint(root, active);
        if (root is Form form)
        {
            PaintCaption(form, active);
        }
    }

    private static void Paint(Control control, Theme? theme)
    {
        if (control.Tag is ThemeMark { Class: Fixed })
        {
            return;
        }

        switch (control)
        {
            case TranscriptView:
                PaintText(control, theme, Transcript);
                break;
            case TextBox:
                PaintText(control, theme, Marked(control) ?? Text);
                break;
            case ListBox:
                PaintText(control, theme, List);
                break;
            case System.Windows.Forms.Button button:
                PaintButton(button, theme);
                break;
            case MenuStrip menu:
                PaintMenu(menu, theme);
                break;
            case StatusStrip status:
                PaintStatus(status, theme);
                break;
            case ProgressBar or ToolStrip:
                break;
            case Label or CheckBox or RadioButton:
                PaintLabel(control, theme, Marked(control) ?? SurfaceClass(control));
                break;
            default:
                PaintSurface(control, theme, Marked(control) ?? SurfaceClass(control));
                break;
        }

        foreach (Control child in control.Controls)
        {
            Paint(child, theme);
        }
    }

    private static string? Marked(Control control) => control.Tag is ThemeMark mark ? mark.Class : null;

    /// <summary>The class of the surface a control sits on: the notice panel inside a notice, the window elsewhere.</summary>
    private static string SurfaceClass(Control control)
    {
        for (var c = control; c is not null; c = c.Parent)
        {
            if (c is OverlayPanel)
            {
                return Notice;
            }
        }

        return Window;
    }

    private static void PaintText(Control control, Theme? theme, string themeClass)
    {
        control.BackColor = Background(theme, themeClass);
        control.ForeColor = Foreground(theme, themeClass);
    }

    private static void PaintSurface(Control control, Theme? theme, string themeClass)
    {
        if (theme?.Color(themeClass + ".background") is { IsSystem: false } colour)
        {
            control.BackColor = ToColor(colour);
        }
        else if (control is OverlayPanel)
        {
            control.BackColor = SystemColors.Control;
        }
        else
        {
            control.ResetBackColor();
        }
    }

    private static void PaintLabel(Control label, Theme? theme, string themeClass)
    {
        if (theme?.Color(themeClass + ".background") is { IsSystem: false } back)
        {
            label.BackColor = ToColor(back);
        }
        else if (themeClass == Band)
        {
            label.BackColor = Background(null, Band);
        }
        else
        {
            label.ResetBackColor();
        }

        if (theme?.Color(themeClass + ".foreground") is { IsSystem: false } fore)
        {
            label.ForeColor = ToColor(fore);
        }
        else if (themeClass == Band)
        {
            label.ForeColor = SystemColors.WindowText;
        }
        else
        {
            label.ResetForeColor();
        }
    }

    private static void PaintButton(System.Windows.Forms.Button button, Theme? theme)
    {
        var back = theme?.Color(Button + ".background");
        var fore = theme?.Color(Button + ".foreground");
        if (back is { IsSystem: false } || fore is { IsSystem: false })
        {
            button.UseVisualStyleBackColor = false;
            button.BackColor = back is { IsSystem: false } b ? ToColor(b) : SystemColors.Control;
            button.ForeColor = fore is { IsSystem: false } f ? ToColor(f) : SystemColors.ControlText;
        }
        else
        {
            button.ResetBackColor();
            button.ResetForeColor();
            button.UseVisualStyleBackColor = true;
        }
    }

    private static void PaintMenu(MenuStrip menu, Theme? theme)
    {
        var back = theme?.Color(Menu + ".background");
        var fore = theme?.Color(Menu + ".foreground");
        if (back is { IsSystem: false } || fore is { IsSystem: false })
        {
            menu.Renderer = new ThemeMenuRenderer(
                back is { IsSystem: false } b ? ToColor(b) : SystemColors.MenuBar,
                fore is { IsSystem: false } f ? ToColor(f) : SystemColors.MenuText);
        }
        else
        {
            menu.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        }
    }

    private static void PaintStatus(StatusStrip status, Theme? theme)
    {
        var back = theme?.Color(Status + ".background");
        var fore = theme?.Color(Status + ".foreground");
        if (back is { IsSystem: false } b)
        {
            status.RenderMode = ToolStripRenderMode.System;
            status.BackColor = ToColor(b);
        }
        else
        {
            status.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            status.ResetBackColor();
        }

        foreach (ToolStripItem item in status.Items)
        {
            if (fore is { IsSystem: false } f)
            {
                item.ForeColor = ToColor(f);
            }
            else
            {
                item.ResetForeColor();
            }
        }
    }

    /// <summary>The caption's colours through the desktop window manager: Windows 11 paints them, older Windows refuses the call, which is ignored.</summary>
    private static void PaintCaption(Form form, Theme? theme)
    {
        if (!form.IsHandleCreated)
        {
            return;
        }

        var back = theme?.Color(TitleBar + ".background") is { IsSystem: false, Rgb: { } b } ? ColorRef(b) : DWMWA_COLOR_DEFAULT;
        var fore = theme?.Color(TitleBar + ".foreground") is { IsSystem: false, Rgb: { } f } ? ColorRef(f) : DWMWA_COLOR_DEFAULT;
        _ = DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref back, sizeof(uint));
        _ = DwmSetWindowAttribute(form.Handle, DWMWA_TEXT_COLOR, ref fore, sizeof(uint));
    }

    /// <summary>The background of a text-like class: the theme's colour, or the class's default (the Window colour; the console tints for the field and its band).</summary>
    private static Color Background(Theme? theme, string themeClass)
    {
        if (theme?.Color(themeClass + ".background") is { IsSystem: false } colour)
        {
            return ToColor(colour);
        }

        var dark = SystemColors.Window.GetBrightness() < 0.5;
        return themeClass switch
        {
            Field => dark ? DarkField : LightField,
            Band => dark ? DarkBand : LightBand,
            _ => SystemColors.Window,
        };
    }

    private static Color Foreground(Theme? theme, string themeClass) =>
        theme?.Color(themeClass + ".foreground") is { IsSystem: false } colour ? ToColor(colour) : SystemColors.WindowText;

    private static Color ToColor(ThemeColor colour) => Color.FromArgb(unchecked((int)0xFF000000) | (colour.Rgb!.Value & 0xFFFFFF));

    /// <summary>An RGB value as a COLORREF, which is 0x00BBGGRR.</summary>
    private static uint ColorRef(int rgb) => (uint)(((rgb & 0xFF) << 16) | (rgb & 0xFF00) | ((rgb >> 16) & 0xFF));

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref uint value, int size);

    /// <summary>The menu bar and its drop-downs in the theme's two colours; the highlight a shade off the background.</summary>
    private sealed class ThemeMenuRenderer(Color back, Color fore) : ToolStripProfessionalRenderer(new ThemeColorTable(back))
    {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? fore : SystemColors.GrayText;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = fore;
            base.OnRenderArrow(e);
        }
    }

    private sealed class ThemeColorTable(Color back) : ProfessionalColorTable
    {
        private readonly Color _highlight = back.GetBrightness() < 0.5 ? ControlPaint.Light(back, 0.4f) : ControlPaint.Dark(back, 0.15f);

        public override Color MenuStripGradientBegin => back;
        public override Color MenuStripGradientEnd => back;
        public override Color ToolStripDropDownBackground => back;
        public override Color ImageMarginGradientBegin => back;
        public override Color ImageMarginGradientMiddle => back;
        public override Color ImageMarginGradientEnd => back;
        public override Color MenuItemSelected => _highlight;
        public override Color MenuItemSelectedGradientBegin => _highlight;
        public override Color MenuItemSelectedGradientEnd => _highlight;
        public override Color MenuItemPressedGradientBegin => _highlight;
        public override Color MenuItemPressedGradientMiddle => _highlight;
        public override Color MenuItemPressedGradientEnd => _highlight;
        public override Color MenuItemBorder => _highlight;
        public override Color MenuBorder => _highlight;
        public override Color SeparatorDark => _highlight;
        public override Color SeparatorLight => back;
        public override Color ToolStripBorder => back;
    }
}
