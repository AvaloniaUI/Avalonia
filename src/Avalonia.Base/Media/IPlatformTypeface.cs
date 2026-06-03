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
        /// Gets the variation point this platform typeface is bound to. Equals
        /// <c>default(FontVariationSettings)</c> for static fonts or for variable fonts
        /// that haven't been transformed via <see cref="WithVariation"/>.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <c>default</c>. Platforms that support
        /// variable fonts (e.g. Skia via <c>SKTypeface.Clone</c>) override this property
        /// alongside <see cref="WithVariation"/> to return whatever variation point was
        /// applied at clone time.
        /// </remarks>
        FontVariationSettings VariationSettings => default;

        /// <summary>
        /// Returns a platform typeface bound to the same underlying font face but at a
        /// different variation point.
        /// </summary>
        /// <param name="variation">
        /// The desired normalized variation coordinates. Pass
        /// <c>default(FontVariationSettings)</c> to request the default instance.
        /// </param>
        /// <remarks>
        /// <para>
        /// The default implementation returns <c>this</c> unchanged. This is the
        /// <b>no-op contract</b> that keeps the platform-neutral
        /// <see cref="GlyphTypeface.WithVariation"/> API working even on platforms that
        /// haven't yet implemented variation cloning — see the PR4 planning doc, section
        /// "Default implementation policy". Platforms (e.g. SkiaTypeface in PR4e1)
        /// override this method to actually clone the underlying face at the new
        /// variation point.
        /// </para>
        /// <para>
        /// Overrides <b>must</b> share the underlying face resources (table memory,
        /// platform face handles) between the returned instance and <c>this</c>.
        /// Returning a fully-independent instance defeats per-variation caching at the
        /// <see cref="GlyphTypeface"/> layer.
        /// </para>
        /// <para>
        /// Overrides should also return <c>this</c> when <paramref name="variation"/>
        /// equals <see cref="VariationSettings"/> to avoid pointless clones along no-op
        /// paths.
        /// </para>
        /// </remarks>
        IPlatformTypeface WithVariation(FontVariationSettings variation) => this;

        /// <summary>
        /// Returns the font file stream represented by the <see cref="GlyphTypeface"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>Returns <c>true</c> if the stream can be obtained, otherwise <c>false</c>.</returns>
        bool TryGetStream([NotNullWhen(true)] out Stream? stream);
    }
}
