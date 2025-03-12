using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Avalonia.Media.Fonts;

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

        /// <summary>
        /// Gets the typographic family name.
        /// </summary>
        string TypographicFamilyName { get; }

        /// <summary>
        /// Gets the localized family names.
        /// <para>Keys are culture identifiers.</para>
        /// </summary>
        IReadOnlyDictionary<ushort, string> FamilyNames { get; }

        /// <summary>
        /// Gets supported font features.
        /// </summary>
        IReadOnlyList<OpenTypeTag> SupportedFeatures { get; }

        /// <summary>
        /// Gets the localized face names.
        /// <para>Keys are culture identifiers.</para>
        /// </summary>
        IReadOnlyDictionary<ushort, string> FaceNames { get; }
    }
}
