using Avalonia.Metadata;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Extends <see cref="ITextProvider"/> with caret exposure (UIA TextPattern2). Screen readers use
    /// the caret range to track the reading and editing position independently of the selection, which
    /// may be empty or span away from the active end.
    /// </summary>
    [Unstable]
    public interface ITextProvider2 : ITextProvider
    {
        /// <summary>
        /// Gets the caret as a degenerate range, or null when the provider currently has no caret.
        /// </summary>
        /// <param name="isActive">
        /// True when the caret lives in this provider's text - the element (or the element it reports
        /// automation focus for) holds keyboard focus.
        /// </param>
        ITextRangeProvider? GetCaretRange(out bool isActive);
    }
}
