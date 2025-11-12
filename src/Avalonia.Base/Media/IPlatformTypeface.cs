using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    [NotClientImplementable]
    public interface IPlatformTypeface : IFontMemory
    {
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
        /// Returns the font file stream represented by the <see cref="IGlyphTypeface"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>Returns <c>true</c> if the stream can be obtained, otherwise <c>false</c>.</returns>
        bool TryGetStream([NotNullWhen(true)] out Stream? stream);
    }
}
