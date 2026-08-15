using System.Collections.Generic;
using Avalonia.Automation.Peers;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Indicates the degree to which a control supports text selection.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    ///   <item>
    ///     <term>Windows</term>
    ///     <description><c>SupportedTextSelection</c></description>
    ///   </item>
    ///   <item>
    ///     <term>macOS</term>
    ///     <description>No mapping.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public enum SupportedTextSelection
    {
        None,
        Single,
        Multiple,
    }

    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to controls
    /// that expose content as text, allowing clients to track the caret and selection and to
    /// navigate the text a character/word/line at a time.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    ///   <item>
    ///     <term>Windows</term>
    ///     <description><c>ITextProvider</c></description>
    ///   </item>
    ///   <item>
    ///     <term>macOS</term>
    ///     <description>No mapping.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public interface ITextProvider
    {
        /// <summary>
        /// Gets the currently selected text ranges. If nothing is selected, this returns a
        /// single degenerate (zero-length) range at the current caret position rather than an
        /// empty collection, so that screen readers can track caret-only movement.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITextProvider.GetSelection</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        IReadOnlyList<ITextRangeProvider> GetSelection();

        /// <summary>
        /// Gets the currently visible text ranges.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITextProvider.GetVisibleRanges</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        IReadOnlyList<ITextRangeProvider> GetVisibleRanges();

        /// <summary>
        /// Gets the text range enclosing the given child element, or null if the control has no
        /// child text elements.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITextProvider.RangeFromChild</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        ITextRangeProvider? RangeFromChild(AutomationPeer childElement);

        /// <summary>
        /// Gets the degenerate text range nearest to the given point, in the peer's own
        /// top-level coordinate space (see <see cref="AutomationPeer.GetBoundingRectangle"/>).
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITextProvider.RangeFromPoint</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        ITextRangeProvider RangeFromPoint(Point point);

        /// <summary>
        /// Gets a text range enclosing the entire content of the control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITextProvider.GetDocumentRange</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        ITextRangeProvider DocumentRange { get; }

        /// <summary>
        /// Gets a value that specifies the type of text selection that is supported by the
        /// control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITextProvider.GetSupportedTextSelection</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        SupportedTextSelection SupportedTextSelection { get; }
    }
}
