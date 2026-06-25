using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Automation.Provider
{
    /// <summary>Describes the text-selection capability a control exposes to accessibility clients.</summary>
    [Unstable]
    public enum SupportedTextSelection
    {
        /// <summary>The control does not support text selection.</summary>
        None,

        /// <summary>The control supports a single contiguous selection.</summary>
        Single,

        /// <summary>The control supports multiple, discontiguous selections.</summary>
        Multiple
    }

    /// <summary>
    /// Exposes a control's text to accessibility clients (UIA TextPattern, AT-SPI Text) as movable
    /// <see cref="ITextRangeProvider"/> ranges over its document.
    /// </summary>
    [Unstable]
    public interface ITextProvider
    {
        /// <summary>A range spanning the whole document.</summary>
        ITextRangeProvider DocumentRange { get; }

        /// <summary>The selection capability of the control.</summary>
        SupportedTextSelection SupportedTextSelection { get; }

        /// <summary>The currently selected ranges (one for a single-selection control).</summary>
        IReadOnlyList<ITextRangeProvider> GetSelection();

        /// <summary>
        /// A degenerate range at the position nearest <paramref name="point"/> (in top-level
        /// coordinates), or null when the control has no layout. The platform layer converts screen
        /// coordinates to top-level before calling.
        /// </summary>
        ITextRangeProvider? RangeFromPoint(Point point);
    }
}
