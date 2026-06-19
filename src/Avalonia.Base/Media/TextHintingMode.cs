namespace Avalonia.Media
{
    /// <summary>
    /// Specifies the level of hinting applied to text glyphs during rendering.
    /// Text hinting adjusts glyph outlines to improve readability and crispness,
    /// especially at small font sizes or low DPI. This enum controls the amount
    /// of grid-fitting and outline adjustment performed.
    /// </summary>
    public enum TextHintingMode : byte
    {
        /// <summary>
        /// Hinting mode is not explicitly specified. The default will be used.
        /// </summary>
        Unspecified,

        /// <summary>
        /// No hinting, outlines are scaled only.
        /// </summary>
        None,

        /// <summary>
        /// Minimal hinting, preserves glyph shape.
        /// </summary>
        Light,

        /// <summary>
        /// Aggressive grid-fitting, maximum crispness at low DPI.
        /// </summary>
        Strong
    }
}
