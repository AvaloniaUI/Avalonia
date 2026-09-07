using System;
using System.Collections.Generic;
using Avalonia.Input.TextInput;
using Avalonia.Metadata;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Extends <see cref="ITextNavigation"/> with the editing and geometry operations the accessibility
    /// text providers (UIA TextPattern, AT-SPI) need beyond read-only navigation. A navigation that
    /// implements it lets an accessibility text range act on the owning control.
    /// </summary>
    /// <remarks>
    /// Accessibility clients read text only through <see cref="GetPlainText"/>, never through
    /// <see cref="ITextNavigation.GetText"/> or a linear scope's text, so a control can withhold content
    /// it does not draw, such as a masked password. For the same reason they follow
    /// <see cref="PlainTextChanged"/> rather than <see cref="ITextNavigation.TextChanged"/>.
    /// <para>
    /// Inside one <see cref="ILinearTextScope"/>, the plain text of a range has exactly one character per
    /// scope code unit: masking replaces characters one for one, and block separators appear only
    /// between scopes. An index into the plain text of a range within a scope is therefore an offset
    /// from the range start in that scope.
    /// </para>
    /// </remarks>
    [Unstable]
    public interface IAccessibleText : ITextNavigation
    {
        /// <summary>
        /// Sets the control's selection. The anchor stays fixed and the active end becomes the caret.
        /// </summary>
        void SetSelection(DirectedTextRange selection);

        /// <summary>
        /// The current selection with its direction: the active end is where the caret is. A collapsed
        /// selection sits at the caret.
        /// </summary>
        DirectedTextRange GetSelection();

        /// <summary>
        /// The current point of action. Empty for an insertion caret; non-empty when the caret covers
        /// text, as an overtype caret or an active composition does.
        /// </summary>
        ITextRange GetCaret();

        /// <summary>
        /// Raised once the selection or the caret has moved and settled: once per action, never for an
        /// intermediate state the control passes through while it updates, and not at all when the
        /// action leaves both where they were.
        /// </summary>
        event EventHandler? SelectionChanged;

        /// <summary>
        /// Raised when the text <see cref="GetPlainText"/> returns changes, with the range that now holds
        /// the new text. That covers every document edit, and also changes that leave the document alone
        /// but alter what is drawn, such as revealing or masking a password, for which the range is the
        /// whole document.
        /// </summary>
        event EventHandler<TextChange>? PlainTextChanged;

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
        /// The text of <paramref name="range"/> as an accessibility client may read it: as drawn, so
        /// masked content comes back masked, with a line feed at each block boundary the way the control
        /// serializes plain text (e.g. for the clipboard). Defaults to the document text, which is correct
        /// for a single-block control that draws all of its content.
        /// </summary>
        string GetPlainText(ITextRange range) => GetText(range);
    }
}
