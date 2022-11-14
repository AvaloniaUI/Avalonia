namespace Avalonia.Media
{
    /// <summary>
    /// Font fallback definition that is used to override the default fallback lookup of the current <see cref="FontManager"/>
    /// </summary>
    public class FontFallback
    {
        /// <summary>
        /// Get or set the fallback <see cref="FontFamily"/>
        /// </summary>
        public FontFamily FontFamily { get; set; } = FontFamily.Default;

        /// <summary>
        /// Get or set the <see cref="UnicodeRange"/> that is covered by the fallback.
        /// </summary>
        public UnicodeRange UnicodeRange { get; set; } = UnicodeRange.Default;
    }
}
