using System;

namespace Avalonia.Media
{
    /// <summary>
    /// Defines the decorations that can be applied to text.
    /// </summary>
    [Flags]
    public enum TextDecorations
    {
        /// <summary>
        /// No text decorations are applied.
        /// </summary>
        None,

        /// <summary>
        /// Strikethrough is applied.
        /// </summary>
        Strikethrough,

        /// <summary>
        /// Underline is applied.
        /// </summary>
        Underline,
    }
}
