using System;
using Avalonia.Media.Fonts.Tables.OpenTypeMath;
using Avalonia.Media.Fonts.Tables.Variation;

namespace Avalonia.Media
{
    /// <summary>
    /// The OpenType MATH table of a <see cref="GlyphTypeface"/>: the layout constants, per-glyph
    /// information and glyph variants a math layout engine needs from a math font.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The table reports font data only; laying out formulas is left to the caller. Lengths are in
    /// design units, like <see cref="GlyphMetrics"/>, and are scaled by
    /// <c>emSize / Metrics.DesignEmHeight</c>.
    /// </para>
    /// <para>
    /// Values are those of the typeface's variation instance: an instance created by
    /// <see cref="GlyphTypeface.WithVariations"/> applies the font's GDEF variation deltas at its
    /// position in design space. Values are as designed: synthetic bold and oblique simulations do
    /// not change them, and per-ppem device adjustments are ignored.
    /// </para>
    /// <para>
    /// Glyphs the table has no data for return 0, <c>false</c> or an empty span, and so do parts of
    /// the table that are malformed. Instances are immutable and safe to use from any thread.
    /// </para>
    /// <para>
    /// Reference: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math"/>.
    /// </para>
    /// </remarks>
    public sealed class OpenTypeMathTable
    {
        private readonly ItemVariationStore? _store;
        private readonly float[]? _regionScalers;
        private readonly int[] _constants;

        internal OpenTypeMathTable(MathTable data, ItemVariationStore? store, float[]? regionScalers)
        {
            Data = data;
            _store = store;
            _regionScalers = regionScalers;
            _constants = new int[MathTable.ConstantCount];

            data.ResolveConstants(_constants, store, regionScalers);
        }

        /// <summary>
        /// Gets the parsed table, shared by every variation instance of the font.
        /// </summary>
        internal MathTable Data { get; }

        /// <summary>
        /// Gets the minimum overlap of connecting parts in a glyph assembly, in design units.
        /// </summary>
        public int MinConnectorOverlap => Data.Variants?.MinConnectorOverlap ?? 0;

        /// <summary>
        /// Gets a layout constant.
        /// </summary>
        /// <param name="constant">The constant to get.</param>
        /// <returns>
        /// A percentage for <see cref="MathConstant.ScriptPercentScaleDown"/>,
        /// <see cref="MathConstant.ScriptScriptPercentScaleDown"/> and
        /// <see cref="MathConstant.RadicalDegreeBottomRaisePercent"/>, otherwise a length in design units.
        /// All constants are 0 when the font's table has no usable constants.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="constant"/> is not a defined value.</exception>
        public int GetConstant(MathConstant constant)
        {
            if ((uint)constant >= (uint)_constants.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(constant));
            }

            return _constants[(int)constant];
        }

        /// <summary>
        /// Gets the italics correction of a glyph: the extra space after it before an upright glyph or
        /// a superscript, in design units.
        /// </summary>
        /// <param name="glyph">The glyph id.</param>
        /// <returns>The italics correction, or 0 when the font defines none for the glyph.</returns>
        public int GetItalicsCorrection(ushort glyph)
            => Data.GlyphInfo is { } glyphInfo && glyphInfo.TryGetItalicsCorrection(glyph, _store, _regionScalers, out var value)
                ? value
                : 0;

        /// <summary>
        /// Gets the horizontal position, in design units from the glyph origin, at which an accent
        /// placed over the glyph is centered.
        /// </summary>
        /// <param name="glyph">The glyph id.</param>
        /// <param name="x">The attachment position, or 0 when the method returns <c>false</c>.</param>
        /// <returns>
        /// <c>false</c> when the font defines no attachment for the glyph. The layout then picks its own
        /// position, usually half the advance width.
        /// </returns>
        public bool TryGetTopAccentAttachment(ushort glyph, out int x)
        {
            x = 0;

            return Data.GlyphInfo is { } glyphInfo && glyphInfo.TryGetTopAccentAttachment(glyph, _store, _regionScalers, out x);
        }

        /// <summary>
        /// Gets whether a glyph is an extended shape, such as a large delimiter variant, whose ink
        /// extends beyond the ascent and descent of the font and which takes scripts by its ink bounds.
        /// </summary>
        /// <param name="glyph">The glyph id.</param>
        public bool IsExtendedShape(ushort glyph) => Data.GlyphInfo?.IsExtendedShape(glyph) ?? false;

        /// <summary>
        /// Gets the kern between a glyph and a script at one of its corners, in design units.
        /// </summary>
        /// <param name="glyph">The glyph id.</param>
        /// <param name="corner">The corner at which the script attaches.</param>
        /// <param name="correctionHeight">
        /// The height, in design units relative to the baseline, at which the script meets the glyph.
        /// </param>
        /// <returns>
        /// The kern of the height band containing <paramref name="correctionHeight"/>, or 0 when the
        /// font defines no kern for the glyph at that corner. A height equal to one of the font's
        /// correction heights belongs to the band below it.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="corner"/> is not a defined value.</exception>
        public int GetKerning(ushort glyph, MathKernCorner corner, int correctionHeight)
        {
            if ((uint)corner > (uint)MathKernCorner.BottomLeft)
            {
                throw new ArgumentOutOfRangeException(nameof(corner));
            }

            return Data.GlyphInfo?.GetKerning(glyph, (int)corner, correctionHeight, _store, _regionScalers) ?? 0;
        }

        /// <summary>
        /// Gets the size variants of a glyph in a direction, in increasing size. The first variant is
        /// usually the glyph itself.
        /// </summary>
        /// <param name="glyph">The glyph id.</param>
        /// <param name="direction">The direction in which the glyph grows.</param>
        /// <returns>The variants, or an empty span when the glyph has none in <paramref name="direction"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="direction"/> is not a defined value.</exception>
        public ReadOnlySpan<MathGlyphVariant> GetGlyphVariants(ushort glyph, MathDirection direction)
        {
            ValidateDirection(direction);

            return Data.Variants is { } variants ? variants.GetGlyphVariants(glyph, direction) : default;
        }

        /// <summary>
        /// Gets the parts from which a glyph is assembled in a direction when its largest size variant
        /// is not large enough.
        /// </summary>
        /// <param name="glyph">The glyph id.</param>
        /// <param name="direction">The direction in which the glyph grows.</param>
        /// <param name="assembly">The assembly, or <c>default</c> when the method returns <c>false</c>.</param>
        /// <returns><c>false</c> when the glyph has no assembly in <paramref name="direction"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="direction"/> is not a defined value.</exception>
        public bool TryGetGlyphAssembly(ushort glyph, MathDirection direction, out MathGlyphAssembly assembly)
        {
            ValidateDirection(direction);

            assembly = default;

            return Data.Variants is { } variants &&
                   variants.TryGetGlyphAssembly(glyph, direction, _store, _regionScalers, out assembly);
        }

        private static void ValidateDirection(MathDirection direction)
        {
            if (direction != MathDirection.Horizontal && direction != MathDirection.Vertical)
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}
