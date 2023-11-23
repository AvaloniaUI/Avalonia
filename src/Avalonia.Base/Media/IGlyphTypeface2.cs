using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Avalonia.Media
{
    internal interface IGlyphTypeface2 : IGlyphTypeface
    {

        /// <summary>
        /// Returns the font file stream represented by the <see cref="IGlyphTypeface"/> object.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>Returns <c>true</c> if the stream can be obtained, otherwise <c>false</c>.</returns>
        bool TryGetStream([NotNullWhen(true)] out Stream? stream);
    }
}
