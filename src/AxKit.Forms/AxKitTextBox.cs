namespace AxKit.Forms;

/// <summary>
/// The edit control of every app (docs/axKit/SPEC.md AX-8.9, 2026-11-09): a <see cref="TextBox"/> with what every
/// field of the bundle had been given one by one, so that a new field gets it by being made and nothing else.
/// No length cap (the control's own 32 767 characters cut a long paste short, axClaude FR-2.1); and in a multi-line
/// control the bundle's page and paragraph keys (<see cref="EditKeys"/>, AX-8.2), after the window's own handlers,
/// so that a window's key on the same stroke comes first (axTerminal's Ctrl+Up is its history) and the shared keys act
/// where nothing else did. Word wrap stays the control's (on); the editors alone take it from the theme file's
/// display settings (docs/axKit/theme.md TH-6). <c>TextBoxTests</c> fails on a plain <c>TextBox</c> made in an app
/// and on a second subscription of the keys.
/// </summary>
public class AxKitTextBox : TextBox
{
    public AxKitTextBox()
    {
        MaxLength = 0;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && Multiline)
        {
            EditKeys.Handle(this, e);
        }
    }
}
