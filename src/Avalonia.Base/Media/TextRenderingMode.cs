namespace Avalonia.Media
{
    /// <summary>
    /// Specifies how text glyphs are rendered in Avalonia.
    /// Controls the smoothing and antialiasing applied during text rasterization.
    /// </summary>
    public enum TextRenderingMode : byte
    {
        /// <summary>
        /// Rendering mode is not explicitly specified.
        /// The system or platform default will be used.
        /// </summary>
        Unspecified,

        /// <summary>
        /// Glyphs are rendered with subpixel antialiasing.
        /// This provides higher apparent resolution on LCD screens
        /// by using the individual red, green, and blue subpixels.
        /// </summary>
        SubpixelAntialias,

        /// <summary>
        /// Glyphs are rendered with standard grayscale antialiasing.
        /// This smooths edges without using subpixel information,
        /// preserving shape fidelity across different display types.
        /// </summary>
        Antialias,

        /// <summary>
        /// Glyphs are rendered without antialiasing.
        /// This produces sharp, aliased edges and may be useful
        /// for pixel-art aesthetics or low-DPI environments.
        /// </summary>
        Alias
    }
}
