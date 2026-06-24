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
    }
}
