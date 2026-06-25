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

        /// <summary>
        /// The top-level-coordinate rectangles covering <paramref name="range"/> (one per line); the
        /// platform accessibility layer converts them to screen coordinates.
        /// </summary>
        Rect[] GetBoundingRectangles(ITextRange range);
    }
}
