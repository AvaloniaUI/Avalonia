using System;

namespace Avalonia.Media
{
    /// <summary>
    /// A platform typeface that can be a named instance of a variable font which the platform
    /// applied inside the typeface, such as the SemiBold face DirectWrite or fontconfig lists for
    /// Bahnschrift. The font tables of such a typeface describe the default instance, so a
    /// <see cref="GlyphTypeface"/> finds the instance from what the platform reports and draws.
    /// </summary>
    internal interface IPlatformVariableInstance
    {
        /// <summary>
        /// Gets whether the platform may have applied a named instance inside the typeface. A
        /// typeface created from a font stream is always the default instance.
        /// </summary>
        bool MayBeNamedInstance { get; }

        /// <summary>
        /// Gets the weight of the face the platform matched, before any simulation.
        /// </summary>
        FontWeight InstanceWeight { get; }

        /// <summary>
        /// Gets the stretch of the face the platform matched, before any simulation.
        /// </summary>
        FontStretch InstanceStretch { get; }

        /// <summary>
        /// Gets the index of the fvar named instance the platform applied, when the platform
        /// identifies it.
        /// </summary>
        /// <param name="instanceIndex">The zero-based index into the font's named instances.</param>
        /// <returns><c>true</c> when the platform identified a named instance.</returns>
        bool TryGetNamedInstanceIndex(out int instanceIndex);

        /// <summary>
        /// Gets the advances the platform draws the glyphs with, in font design units.
        /// </summary>
        /// <param name="glyphIndices">The glyphs to measure.</param>
        /// <param name="advances">Receives one advance per glyph.</param>
        /// <returns><c>true</c> when the advances could be measured.</returns>
        bool TryGetGlyphAdvances(ReadOnlySpan<ushort> glyphIndices, Span<float> advances);
    }
}
