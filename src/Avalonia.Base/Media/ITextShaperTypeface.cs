using System;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    [NotClientImplementable]
    public interface ITextShaperTypeface : IDisposable
    {
        /// <summary>
        /// Gets the variation point this shaper typeface is bound to. Equals
        /// <c>default(NormalizedVariationPosition)</c> for static fonts or for variable
        /// fonts shaping at the default instance.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <c>default</c>. Shaper implementations
        /// that support variation (the HarfBuzz shaper, through
        /// <c>hb_font_set_var_coords_normalized</c>) override this property alongside
        /// <see cref="WithVariation"/> to report the variation coordinates configured on
        /// the underlying shaping font. The member is <c>internal</c> because it traffics in normalized
        /// (font-relative) coordinates — only Avalonia's own shaper backends implement
        /// it; user code speaks user-space <see cref="FontVariationSettings"/> at the
        /// <see cref="GlyphTypeface"/> layer.
        /// </remarks>
        internal NormalizedVariationPosition VariationPosition => default;

        /// <summary>
        /// Returns a shaper typeface bound to the same underlying face but at a different
        /// variation point.
        /// </summary>
        /// <param name="variation">
        /// The desired normalized variation coordinates. Pass
        /// <c>default(NormalizedVariationPosition)</c> for the default instance.
        /// </param>
        /// <remarks>
        /// <para>
        /// The default implementation returns <c>this</c> unchanged, the same contract
        /// used by <see cref="IPlatformTypeface.WithVariation"/>: a shaper without
        /// variation support shapes every variation at the default instance. The
        /// HarfBuzz shaper returns <c>this</c> for its own position and otherwise a new
        /// instance owning an <c>hb_font_t</c> sub-font with the requested normalized
        /// coordinates, so shaped advances include HVAR deltas.
        /// </para>
        /// <para>
        /// A distinct returned instance is owned by the caller: disposing it releases only
        /// its own shaping font, never the face state shared with <c>this</c>.
        /// </para>
        /// <para>
        /// Overrides must share face-level resources between the returned instance and
        /// <c>this</c>; returning a fully-independent instance defeats per-variation
        /// caching at the <see cref="GlyphTypeface"/> layer.
        /// </para>
        /// </remarks>
        internal ITextShaperTypeface WithVariation(NormalizedVariationPosition variation) => this;
    }
}
