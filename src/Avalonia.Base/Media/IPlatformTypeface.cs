using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    [NotClientImplementable]
    public interface IPlatformTypeface : IFontMemory
    {
        /// <summary>
        /// Gets the font family name.
        /// </summary>
        /// <remarks>
        /// The family name should be the same as the one used to create the typeface via the platform font manager. 
        /// It can be different from the actaual family name because an alias or a fallback name could have been used.
        /// </remarks>
        string FamilyName { get; }

        /// <summary>
        /// Gets the designed weight of the font represented by the <see cref="IPlatformTypeface"/> object.
        /// </summary>
        FontWeight Weight { get; }

        /// <summary>
        /// Gets the style for the <see cref="IPlatformTypeface"/> object.
        /// </summary>
        FontStyle Style { get; }

        /// <summary>
        /// Gets the <see cref="FontStretch"/> value for the <see cref="IPlatformTypeface"/> object.
        /// </summary>
        FontStretch Stretch { get; }

        /// <summary>
        ///     Gets the algorithmic style simulations applied to <see cref="IPlatformTypeface"/> object.
        /// </summary>
        FontSimulations FontSimulations { get; }

        /// <summary>
        /// Returns the font file stream represented by the <see cref="GlyphTypeface"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>Returns <c>true</c> if the stream can be obtained, otherwise <c>false</c>.</returns>
        bool TryGetStream([NotNullWhen(true)] out Stream? stream);
    }
}
