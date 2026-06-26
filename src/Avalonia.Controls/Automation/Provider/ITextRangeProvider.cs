using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Input.TextInput;
using Avalonia.Metadata;

namespace Avalonia.Automation.Provider
{
    /// <summary>Identifies an endpoint of an <see cref="ITextRangeProvider"/>.</summary>
    [Unstable]
    public enum TextRangeEndpoint
    {
        /// <summary>The start of the range.</summary>
        Start,

        /// <summary>The end of the range.</summary>
        End
    }

    /// <summary>
    /// A movable text range over an <see cref="ITextNavigation"/> - the cross-platform basis for the
    /// Win32 UIA TextPattern range and the AT-SPI text range, which each wrap it for their protocol.
    /// </summary>
    /// <remarks>
    /// The range is a mutable cursor: <see cref="ExpandToEnclosingUnit"/>, <see cref="Move"/> and
    /// <see cref="MoveEndpointByUnit"/> change it in place, while <see cref="Clone"/> produces an
    /// independent copy. Geometry, attribute and embedded-object queries are added by the accessibility
    /// layer; this contract covers navigation and text extraction only.
    /// </remarks>
    [Unstable]
    public interface ITextRangeProvider
    {
        /// <summary>Returns an independent copy of this range.</summary>
        ITextRangeProvider Clone();

        /// <summary>Returns whether <paramref name="other"/> spans the same span as this range.</summary>
        bool Compare(ITextRangeProvider other);

        /// <summary>
        /// Compares an endpoint of this range with an endpoint of <paramref name="other"/>; negative,
        /// zero or positive as this endpoint precedes, equals or follows the other in document order.
        /// </summary>
        int CompareEndpoints(TextRangeEndpoint endpoint, ITextRangeProvider other, TextRangeEndpoint otherEndpoint);

        /// <summary>Normalizes the range to the <paramref name="unit"/> enclosing its start.</summary>
        void ExpandToEnclosingUnit(TextUnit unit);

        /// <summary>The text of the range, truncated to <paramref name="maxLength"/> (negative for no limit).</summary>
        string GetText(int maxLength);

        /// <summary>Moves the whole range by <paramref name="count"/> units; returns the units actually moved.</summary>
        int Move(TextUnit unit, int count);

        /// <summary>Moves one endpoint by <paramref name="count"/> units; returns the units actually moved.</summary>
        int MoveEndpointByUnit(TextRangeEndpoint endpoint, TextUnit unit, int count);

        /// <summary>Moves one endpoint of this range to an endpoint of <paramref name="other"/>.</summary>
        void MoveEndpointByRange(TextRangeEndpoint endpoint, ITextRangeProvider other, TextRangeEndpoint otherEndpoint);

        /// <summary>Selects this range in the owning control, if the control supports it.</summary>
        void Select();

        /// <summary>The screen-coordinate rectangles covering the range (one per line).</summary>
        Rect[] GetBoundingRectangles();

        /// <summary>
        /// The value of <paramref name="attribute"/> when it is uniform across the range, or null when
        /// the control does not expose it or it varies across the range. Values are boxed per the
        /// <see cref="TextAttribute"/> vocabulary.
        /// </summary>
        object? GetAttributeValue(TextAttribute attribute);

        /// <summary>
        /// The first occurrence of <paramref name="text"/> within this range, searching toward the
        /// document start when <paramref name="backward"/> is set, or null if not found.
        /// </summary>
        ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase);

        /// <summary>Scrolls the owning control so this range is visible, if it supports scrolling.</summary>
        void ScrollIntoView(bool alignToTop);

        /// <summary>
        /// The embedded automation elements contained in this range (hyperlinks, images...), in document
        /// order; empty when the owning control exposes none.
        /// </summary>
        IReadOnlyList<AutomationPeer> GetChildren();

        /// <summary>
        /// The innermost embedded element that encloses this range, or null when only the text container
        /// encloses it - the platform layer substitutes the container element in that case.
        /// </summary>
        AutomationPeer? GetEnclosingElement();
    }
}
