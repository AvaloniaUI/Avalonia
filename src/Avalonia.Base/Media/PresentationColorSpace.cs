using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// Defines the color space that rendered content is presented in.
    /// </summary>
    [Unstable]
    public enum PresentationColorSpace
    {
        /// <summary>
        /// Content is presented without an explicit color space and is not converted.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Content is presented as sRGB.
        /// </summary>
        /// <remarks>
        /// On a wide gamut display this prevents sRGB content from being shown over-saturated.
        /// </remarks>
        Srgb = 1,

        /// <summary>
        /// Content is presented as Display P3, the sRGB transfer function with the wider DCI-P3
        /// primaries.
        /// </summary>
        /// <remarks>
        /// Allows to show colors which can not be represented in sRGB. Drawn content is converted
        /// into Display P3, so existing sRGB content keeps looking the same.
        /// </remarks>
        DisplayP3 = 2,

        /// <summary>
        /// Content is presented in the widest gamut the platform can offer.
        /// </summary>
        /// <remarks>
        /// This is a request only and is never reported back as a result. Which color space it ends
        /// up being depends on the platform, because they do not offer the same ones. Use it when
        /// the application renders wide gamut content and does not need one specific encoding, and
        /// read <see cref="Avalonia.Platform.IColorManagedPresentation.CurrentColorSpace"/> to find
        /// out what was really applied.
        /// </remarks>
        WideGamut = 3,
    }
}
