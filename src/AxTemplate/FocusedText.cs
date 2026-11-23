using System.Runtime.InteropServices;
using AxKit.Core;

namespace AxTemplate;

/// <summary>
/// The text of the field that has the focus in another program, through UI Automation, the channel a screen reader
/// reads it by (docs/axTemplate/SPEC.md TP-2.3, TP-D10; the maintainer, 2026-11-23: "insert the full template and
/// then search for each pattern to select it ... or place the caret there by deleting the content that is there").
/// After a paste the template's caret mark is searched for back from the caret, so the nearest one, the one just
/// pasted, and selected; the caller then sends Delete, which leaves the caret in its place. A program that gives no
/// text this way (a terminal, many older programs) is told by <see cref="Readable"/> before the paste, and gets the
/// Left keys instead. Every call runs on a thread of the pool (UI Automation's client wants one without a window
/// of its own, never the UI thread), and every failure is false, logged without the text.
/// </summary>
public static class FocusedText
{
    private const int TextPatternId = 10014;
    private const int EndpointStart = 0;
    private const int EndpointEnd = 1;

    /// <summary>Whether the focused field gives its text and its caret: the mark can then be found after the paste.</summary>
    public static bool Readable()
    {
        try
        {
            var text = Pattern();
            return text?.GetSelection()?.Length > 0;
        }
        catch (COMException ex)
        {
            Log.Info("The focused field's text could not be read: " + ex.GetType().Name);
            return false;
        }
    }

    /// <summary>
    /// The text selected in the focused field, read without a key (TP-4.2; the maintainer, 2026-11-23: "{selection}
    /// is not working as expected"): empty when nothing is selected, null when the field gives no text. No copy key
    /// is sent, so a terminal's running command is never stopped by it.
    /// </summary>
    public static string? SelectedText()
    {
        try
        {
            return Pattern()?.GetSelection() is { Length: > 0 } selection ? selection.GetElement(0).GetText(-1) ?? string.Empty : null;
        }
        catch (COMException ex)
        {
            Log.Info("The focused field's selection could not be read: " + ex.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// The mark nearest before the caret selected, once the paste has put it there: tried every 50 ms for
    /// <paramref name="maxMs"/>, so that a program that pastes slowly is waited for. False when it never came.
    /// </summary>
    public static bool SelectMarkBeforeCaret(string mark, int maxMs)
    {
        for (var waited = 0; waited <= maxMs; waited += 50)
        {
            try
            {
                if (Pattern() is { } text && text.GetSelection() is { Length: > 0 } selection)
                {
                    var before = text.DocumentRange.Clone();
                    before.MoveEndpointByRange(EndpointEnd, selection.GetElement(0), EndpointStart);
                    if (before.FindText(mark, true, false) is { } found)
                    {
                        found.Select();
                        return true;
                    }
                }
            }
            catch (COMException ex)
            {
                Log.Info("The caret's mark could not be searched for: " + ex.GetType().Name);
                return false;
            }

            Thread.Sleep(50);
        }

        return false;
    }

    private static IUIAutomationTextPattern? Pattern()
    {
        var automation = (IUIAutomation)new CUIAutomation();
        var element = automation.GetFocusedElement();
        return element?.GetCurrentPattern(TextPatternId) as IUIAutomationTextPattern;
    }

    // The UI Automation client's interfaces, declared in vtable order (UIAutomationClient.h); a method this class
    // never calls holds its slot with no arguments.

    [ComImport]
    [Guid("ff48dba4-60ef-4201-aa87-54103eef594e")]
    private class CUIAutomation
    {
    }

    [ComImport]
    [Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IUIAutomation
    {
        void CompareElements();

        void CompareRuntimeIds();

        void GetRootElement();

        void ElementFromHandle();

        void ElementFromPoint();

        IUIAutomationElement GetFocusedElement();
    }

    [ComImport]
    [Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IUIAutomationElement
    {
        void SetFocus();

        void GetRuntimeId();

        void FindFirst();

        void FindAll();

        void FindFirstBuildCache();

        void FindAllBuildCache();

        void BuildUpdatedCache();

        void GetCurrentPropertyValue();

        void GetCurrentPropertyValueEx();

        void GetCachedPropertyValue();

        void GetCachedPropertyValueEx();

        void GetCurrentPatternAs();

        void GetCachedPatternAs();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object? GetCurrentPattern(int patternId);
    }

    [ComImport]
    [Guid("32eba289-3583-42c9-9c59-3b6d9a1e9b6a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IUIAutomationTextPattern
    {
        void RangeFromPoint();

        void RangeFromChild();

        IUIAutomationTextRangeArray? GetSelection();

        void GetVisibleRanges();

        IUIAutomationTextRange DocumentRange { get; }
    }

    [ComImport]
    [Guid("ce4ae76a-e717-4c98-81ea-47371d028eb6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IUIAutomationTextRangeArray
    {
        int Length { get; }

        IUIAutomationTextRange GetElement(int index);
    }

    [ComImport]
    [Guid("a543cc6a-f4ae-494b-8239-c814481187a8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IUIAutomationTextRange
    {
        IUIAutomationTextRange Clone();

        void Compare();

        void CompareEndpoints();

        void ExpandToEnclosingUnit();

        void FindAttribute();

        IUIAutomationTextRange? FindText([MarshalAs(UnmanagedType.BStr)] string text, [MarshalAs(UnmanagedType.Bool)] bool backward, [MarshalAs(UnmanagedType.Bool)] bool ignoreCase);

        void GetAttributeValue();

        void GetBoundingRectangles();

        void GetEnclosingElement();

        [return: MarshalAs(UnmanagedType.BStr)]
        string? GetText(int maxLength);

        void Move();

        void MoveEndpointByUnit();

        void MoveEndpointByRange(int endpoint, IUIAutomationTextRange range, int targetEndpoint);

        void Select();
    }
}
