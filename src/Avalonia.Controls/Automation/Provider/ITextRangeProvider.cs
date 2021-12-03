using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Identified the endpoints of a <see cref="ITextRangeProvider"/>.
    /// </summary>
    public enum TextPatternRangeEndpoint
    {
        /// <summary>
        /// Identifies the starting point of the range.
        /// </summary>
        Start = 0,

        /// <summary>
        /// Identifies the ending point of the range.
        /// </summary>
        End = 1,
    }

    /// <summary>
    /// Represents pre-defined units of text for the purposes of navigation within a document.
    /// </summary>
    public enum TextUnit
    {
        Character,
        Format,
        Word,
        Line,
        Paragraph,
        Page,
        Document,
    }

    /// <summary>
    /// Provides access to a span of continuous text in a text container that implements
    /// <see cref="ITextProvider"/>. 
    /// </summary>
    public interface ITextRangeProvider
    {
        /// <summary>
        /// Gets the 0-based character index of the start of the range.
        /// </summary>
        int Start { get; }

        /// <summary>
        /// Gets the number of characters in the range.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets the 0-based line index for the start of the range.
        /// </summary>
        int StartLineIndex { get; }

        /// <summary>
        /// Gets the 0-based line index for the end of the range.
        /// </summary>
        int EndLineIndex { get; }

        /// <summary>
        /// Returns a new ITextRangeProvider identical to the original <see cref="ITextRangeProvider"/>.
        /// </summary>
        ITextRangeProvider Clone();

        /// <summary>
        /// Retrieves a value that specifies whether this text range has the same endpoints as
        /// another text range.
        /// </summary>
        /// <param name="range">The text range to compare with this one.</param>
        /// <returns>
        /// True if the text ranges have the same endpoints, or false if they do not.
        /// </returns>
        bool Compare(ITextRangeProvider range);

        /// <summary>
        /// Returns a value that specifies whether two text ranges have identical endpoints.
        /// </summary>
        /// <param name="endpoint">The endpoint on this object to compare.</param>
        /// <param name="targetRange">The text range to be compared.</param>
        /// <param name="targetEndpoint">
        /// The endpoint on <paramref name="targetRange"/> to compare.
        /// </param>
        /// <returns>
        /// A negative value if the this object's endpoint occurs earlier in the text than the
        /// target endpoint, zero if the this object's endpoint is at the same location as the
        /// target endpoint, or a positive value if the this object's endpoint occurs later in
        /// the text than the target endpoint.
        /// </returns>
        int CompareEndpoints(
            TextPatternRangeEndpoint endpoint,
            ITextRangeProvider targetRange,
            TextPatternRangeEndpoint targetEndpoint);

        /// <summary>
        /// Normalizes the text range by the specified text unit.
        /// </summary>
        /// <param name="unit">The text unit.</param>
        /// <remarks>
        /// Nnormalizes a text range by moving the endpoints so that the range encompasses the
        /// specified text unit. The range is expanded if it is smaller than the specified unit,
        /// or shortened if it is longer than the specified unit.
        /// 
        /// The start of the range should first be moved backwards to the start of the specified
        /// unit and then the end of the range moved forward by the specified unit.
        /// 
        /// If the specified text unit is not supported by the control, then the next largest
        /// suuported text unit should be used.
        /// </remarks>
        void ExpandToEnclosingUnit(TextUnit unit);

        /// <summary>
        /// Returns a text range subset that has the specified text attribute value.
        /// </summary>
        /// <param name="property">The property which describes the text attribute.</param>
        /// <param name="value">The attribute value.</param>
        /// <param name="backward">The direction of the search.</param>
        /// <returns>
        /// A text range, or null if the specified property is not supported or not found.
        /// </returns>
        /// <remarks>
        /// Supported properties for <paramref name="property"/> include visual text properties such
        /// <see cref="TextBlock.ForegroundProperty"/> and <see cref="TextBlock.FontSizeProperty"/>.
        /// </remarks>
        ITextRangeProvider? FindAttribute(AvaloniaProperty property, object value, bool backward);

        /// <summary>
        /// Returns a text range subset that contains the specified text.
        /// </summary>
        /// <param name="text">The text to search for.</param>
        /// <param name="backward">The search direction.</param>
        /// <param name="ignoreCase">Whether the search should be case insensitive.</param>
        /// <returns>
        /// A text range matching the specified text, or null of the text was not found.
        /// </returns>
        ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase);

        /// <summary>
        /// Retrieves the value of the specified text attribute across the text range.
        /// </summary>
        /// <param name="property">The property which describes the text attribute.</param>
        /// <returns></returns>
        object? GetAttributeValue(AvaloniaProperty property);

        /// <summary>
        /// Retrieves a collection of bounding rectangles for each fully or partially visible line
        /// of text in a text range.
        /// </summary>
        /// <returns>A collection of <see cref="Rect"/>s in top-level coordinates.</returns>
        IReadOnlyList<Rect> GetBoundingRectangles();

        /// <summary>
        /// Retrieves a collection of all elements that are both contained (either partially or
        /// completely) within the specified text range, and are child elements of the enclosing
        /// element for the specified text range.
        /// </summary>
        /// <returns>A collection of automation peers representing the elements.</returns>
        IReadOnlyList<AutomationPeer> GetChildren();

        /// <summary>
        /// Returns the innermost element that encloses the specified text range.
        /// </summary>
        /// <returns>An automation peer which represents the element.</returns>
        AutomationPeer GetEnclosingElement();

        /// <summary>
        /// Retrieves the plain text of the range.
        /// </summary>
        /// <param name="maxLength">
        /// The maximum length of the string to return, or -1 if no limit is required.
        /// </param>
        /// <returns>
        /// The plain text of the text range, possibly truncated at the specified maximum length.
        /// </returns>
        string GetText(int maxLength);

        /// <summary>
        /// Moves the text range forward or backward by the specified number of text units.
        /// </summary>
        /// <param name="unit">The text unit.</param>
        /// <param name="count">
        /// The number of text units to move. A negative value moves the text range backward.
        /// </param>
        /// <returns>
        /// The number of text units actually moved. This can be less than the number requested if
        /// either of the new text range endpoints is greater than or less than the endpoints
        /// retrieved by the <see cref="ITextProvider.DocumentRange"/>. The value can be negative
        /// if navigation is happening in the backward direction.
        /// </returns>
        int Move(TextUnit unit, int count);

        /// <summary>
        /// Moves one endpoint of the current text range to the specified endpoint of a second text
        /// range.
        /// </summary>
        /// <param name="endpoint">The endpoint on this object to move.</param>
        /// <param name="targetRange">The target range.</param>
        /// <param name="targetEndpoint">
        /// The endpoint on <paramref name="targetRange"/> to move <paramref name="endpoint"/> to.
        /// </param>
        /// <remarks>
        /// If the endpoint being moved crosses the other endpoint of the same text range, that
        /// other endpoint is moved also, resulting in a degenerate (empty) range and ensuring the
        /// correct ordering of the endpoints (that is, the start is always less than or equal to
        /// the end).
        /// </remarks>
        void MoveEndpointByRange(
            TextPatternRangeEndpoint endpoint,
            ITextRangeProvider targetRange,
            TextPatternRangeEndpoint targetEndpoint);

        /// <summary>
        /// Moves one endpoint of the text range the specified number of TextUnit units within the
        /// document range.
        /// </summary>
        /// <param name="endpoint">The endpoint on this object to move.</param>
        /// <param name="unit">The text unit.</param>
        /// <param name="count">
        /// The number of text units to move. A negative value moves the text range backward.
        /// </param>
        /// <returns>
        /// The number of text units actually moved. This can be less than the number requested if
        /// either of the new text range endpoints is greater than or less than the endpoints
        /// retrieved by the <see cref="ITextProvider.DocumentRange"/>. The value can be negative
        /// if navigation is happening in the backward direction.
        /// </returns>
        int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count);

        /// <summary>
        /// Adds the text range to the collection of selected text ranges in a control that
        /// supports multiple, disjoint spans of selected text.
        /// </summary>
        /// <remarks>
        /// The text insertion point moves to the area of the new selection. If this method is
        /// called on a degenerate text range, the text insertion point moves to the location of
        /// the text range but no text is selected.
        /// </remarks>
        void AddToSelection();

        /// <summary>
        /// Removes the text range from the collection of selected text ranges in a control that
        /// supports multiple, disjoint spans of selected text.
        /// </summary>
        /// <remarks>
        /// The text insertion point moves to the area of the removed selection. If this method is
        /// called on a degenerate text range, the text insertion point moves to the location of
        /// the text range but no text is selected.
        /// </remarks>
        void RemoveFromSelection();

        /// <summary>
        /// Causes the text control to scroll vertically until the text range is visible in the
        /// viewport.
        /// </summary>
        /// <param name="alignToTop">
        /// True if the text control should be scrolled so the text range is flush with the top of
        /// the viewport; false if it should be flush with the bottom of the viewport.
        /// </param>
        void ScrollIntoView(bool alignToTop);

        /// <summary>
        /// Selects the span of text that corresponds to this text range, and removes any previous
        /// selection.
        /// </summary>
        /// <remarks>
        /// Providing a degenerate text range will move the text insertion point.
        /// </remarks>
        void Select();
    }
}
