using System.Collections.Generic;
using Avalonia.Automation.Peers;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Identifies one endpoint of a text range.
    /// </summary>
    public enum TextPatternRangeEndpoint
    {
        Start,
        End,
    }

    /// <summary>
    /// Identifies the granularity a text range is expanded/moved/compared by.
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
    /// Represents a contiguous span of text within an <see cref="ITextProvider"/>, used by UI
    /// Automation clients (screen readers) to track and navigate a control's caret/selection.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    ///   <item>
    ///     <term>Windows</term>
    ///     <description><c>ITextRangeProvider</c></description>
    ///   </item>
    ///   <item>
    ///     <term>macOS</term>
    ///     <description>No mapping.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public interface ITextRangeProvider
    {
        ITextRangeProvider Clone();
        bool Compare(ITextRangeProvider range);
        int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);
        void ExpandToEnclosingUnit(TextUnit unit);
        ITextRangeProvider? FindAttribute(int attribute, object? value, bool backward);
        ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase);

        /// <summary>
        /// Gets the value of the given text attribute across the range, or
        /// <see cref="AutomationTextAttributeNotSupported.Instance"/> if the control does not
        /// support querying that attribute.
        /// </summary>
        object GetAttributeValue(int attribute);

        /// <summary>
        /// Gets the bounding rectangles of the range, in the peer's own top-level coordinate
        /// space (see <see cref="AutomationPeer.GetBoundingRectangle"/>).
        /// </summary>
        IReadOnlyList<Rect> GetBoundingRectangles();
        AutomationPeer GetEnclosingElement();
        string GetText(int maxLength);
        int Move(TextUnit unit, int count);
        int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count);
        void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);
        void Select();
        void AddToSelection();
        void RemoveFromSelection();
        void ScrollIntoView(bool alignToTop);
        IReadOnlyList<AutomationPeer> GetChildren();
    }

    /// <summary>
    /// Sentinel value returned from <see cref="ITextRangeProvider.GetAttributeValue"/> when the
    /// requested attribute is not supported by the control, distinguishing "not supported" from
    /// an attribute whose value is legitimately <see langword="null"/>.
    /// </summary>
    public sealed class AutomationTextAttributeNotSupported
    {
        public static readonly AutomationTextAttributeNotSupported Instance = new();
        private AutomationTextAttributeNotSupported() { }
    }
}
