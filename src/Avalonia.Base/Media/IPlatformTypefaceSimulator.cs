using System.Diagnostics.CodeAnalysis;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// Optionally implemented by an <see cref="IPlatformTypeface"/> that can create variants of
    /// itself with other <see cref="FontSimulations"/>.
    /// </summary>
    /// <remarks>
    /// A font collection that needs a synthetic bold or oblique face asks the platform typeface it
    /// already holds for a simulated variant. The variant shares the font data of its source, so no
    /// copy of the font file is made. When this method returns <c>false</c>, the collection uses the
    /// source face without the simulations. Only when the platform typeface does not implement this
    /// interface does the collection create the synthetic face from the font stream returned by
    /// <see cref="IPlatformTypeface.TryGetStream"/>.
    /// </remarks>
    [Unstable]
    public interface IPlatformTypefaceSimulator
    {
        /// <summary>
        /// Creates a platform typeface for the same font face with the specified simulations.
        /// </summary>
        /// <param name="fontSimulations">
        /// All simulations of the variant. Simulations this typeface carries are kept only when they
        /// are listed here.
        /// </param>
        /// <param name="style">The style the variant reports.</param>
        /// <param name="weight">The weight the variant reports.</param>
        /// <param name="stretch">The stretch the variant reports.</param>
        /// <param name="typeface">
        /// When this method returns <c>true</c>, the simulated variant. It shares the font data of
        /// this typeface and does not own it, so it must not be used after this typeface is
        /// disposed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the variant could be created; <c>false</c> if the face takes none of the
        /// simulations, such as a colour font face, whose glyphs are artwork that is neither emboldened
        /// nor slanted, or if the variant leaves out a simulation the platform applied inside this
        /// typeface, which cannot be removed.
        /// </returns>
        bool TryCreateSimulatedTypeface(FontSimulations fontSimulations, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? typeface);
    }
}
