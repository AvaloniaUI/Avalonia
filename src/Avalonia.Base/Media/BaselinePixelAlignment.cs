namespace Avalonia.Media
{
    /// <summary>
    /// Specifies the baseline pixel alignment options for rendering text or graphics.
    /// </summary>
    /// <remarks>Use this enumeration to control whether the baseline of rendered content is aligned to the
    /// pixel grid, which can affect visual crispness and positioning. The value may influence rendering quality,
    /// especially at small font sizes or when precise alignment is required.</remarks>
    public enum BaselinePixelAlignment : byte
    {
        /// <summary>
        /// The baseline pixel alignment is unspecified.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The baseline is aligned to the pixel grid.
        /// </summary>
        Aligned,

        /// <summary>
        /// The baseline is not aligned to the pixel grid.
        /// </summary>
        Unaligned
    }
}
