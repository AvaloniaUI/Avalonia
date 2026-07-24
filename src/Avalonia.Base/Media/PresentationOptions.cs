using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// Options which control how the rendered content is presented to the display.
    /// </summary>
    [Unstable]
    public class PresentationOptions
    {
        /// <summary>
        /// Gets or sets the color space that content should be presented in.
        /// </summary>
        /// <remarks>
        /// This is a request and not a guarantee. A platform which can not honor it presents like it
        /// would do otherwise, and reports that via
        /// <see cref="Avalonia.Platform.IColorManagedPresentation"/>.
        /// </remarks>
        public PresentationColorSpace PreferredColorSpace { get; set; } = PresentationColorSpace.Unspecified;
    }
}
