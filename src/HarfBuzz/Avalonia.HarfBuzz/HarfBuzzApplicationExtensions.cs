using Avalonia.Harfbuzz;
using Avalonia.Platform;

namespace Avalonia
{

    /// <summary>
    /// Configures the application to use HarfBuzz for text shaping.
    /// </summary>
    /// <remarks>This method adds a HarfBuzz-based text shaper implementation to the application, enabling
    /// advanced text shaping capabilities.</remarks>
    public static class HarfBuzzApplicationExtensions
    {
        /// <summary>
        /// Configures the application to use HarfBuzz for text shaping.
        /// </summary>
        /// <remarks>This method integrates HarfBuzz, a text shaping engine, into the application,
        /// enabling advanced text layout and rendering capabilities.</remarks>
        /// <param name="builder">The <see cref="AppBuilder"/> instance to configure.</param>
        /// <returns>The configured <see cref="AppBuilder"/> instance.</returns>
        public static AppBuilder UseHarfBuzz(this AppBuilder builder)
        {
            return builder.With<ITextShaperImpl>(new HarfBuzzTextShaper());
        }
    }

}
