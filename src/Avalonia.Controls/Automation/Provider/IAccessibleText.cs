using System.Collections.Generic;
using Avalonia.Input.TextInput;
using Avalonia.Metadata;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Extends <see cref="ITextNavigation"/> with the editing and geometry operations the accessibility
    /// text providers (UIA TextPattern, AT-SPI) need beyond read-only navigation. A navigation that
    /// implements it lets <see cref="ITextRangeProvider"/> ranges act on the owning control.
    /// </summary>
    [Unstable]
    public interface IAccessibleText : ITextNavigation
    {
        /// <summary>Sets the control's selection to <paramref name="range"/>.</summary>
        void SetSelection(ITextRange range);

        /// <summary>The current selection (a collapsed range at the caret when nothing is selected).</summary>
        ITextRange GetSelection();

        /// <summary>
        /// The top-level-coordinate rectangles covering <paramref name="range"/> (one per line); the
        /// platform accessibility layer converts them to screen coordinates.
        /// </summary>
        Rect[] GetBoundingRectangles(ITextRange range);

        /// <summary>
        /// The position nearest <paramref name="point"/> (in top-level coordinates), or null when the
        /// control has no layout. The inverse of <see cref="GetBoundingRectangles"/>; the platform
        /// accessibility layer converts screen coordinates to top-level before calling.
        /// </summary>
        ITextPointer? GetPositionFromPoint(Point point);

        /// <summary>The text currently visible in the control's viewport, or null when it has no layout.</summary>
        ITextRange? GetVisibleRange();

        /// <summary>Scrolls the control so <paramref name="range"/> is visible, if it supports scrolling.</summary>
        void ScrollIntoView(ITextRange range);

        /// <summary>
        /// The formatting attributes in effect at <paramref name="position"/>, together with the run
        /// over which they are uniform (the whole document for a control with uniform formatting). An
        /// absent key means the control does not expose that attribute; present values are boxed per
        /// the <see cref="TextAttribute"/> vocabulary.
        /// </summary>
        (IReadOnlyDictionary<TextAttribute, object?> Attributes, ITextRange Run) GetTextAttributes(ITextPointer position);

        /// <summary>
        /// The text of <paramref name="range"/> with a line feed at each block boundary, matching how the
        /// control serializes plain text (e.g. for the clipboard). Unlike <see cref="ITextNavigation.GetText"/>,
        /// whose length equals the range's offset span, this is presentation text for accessibility clients.
        /// Defaults to the flat text, which is correct for a control whose content is a single block.
        /// </summary>
        string GetBlockSeparatedText(ITextRange range) => GetText(range);
    }
}
