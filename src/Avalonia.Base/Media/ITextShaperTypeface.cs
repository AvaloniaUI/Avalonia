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
        /// that support variation (e.g. HarfBuzz via <c>Font.SetVariationCoordsNormalized</c>)
        /// override this property alongside <see cref="WithVariation"/> to
        /// report whatever variation coordinates are configured on the underlying
        /// shaping font. The member is <c>internal</c> because it traffics in normalized
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
        /// The default implementation returns <c>this</c> unchanged — the same no-op
        /// contract used by <see cref="IPlatformTypeface.WithVariation"/>. Shapers
        /// override this to derive a new shaping font configured for the requested
        /// variation while sharing face-level state (HarfBuzz <c>hb_face_t</c>, parsed
        /// shaping tables) with the source.
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
