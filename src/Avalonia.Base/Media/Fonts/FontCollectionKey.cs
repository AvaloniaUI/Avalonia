using System;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Represents a unique key for identifying a font inside a font collection based on
    /// style, weight, stretch, and (for variable fonts) the active variation settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this key to efficiently look up or group fonts in a collection by their style,
    /// weight, stretch, and variation characteristics. Keys are ordered lexicographically by
    /// <see cref="Style"/>, then <see cref="Weight"/>, then <see cref="Stretch"/>.
    /// </para>
    /// <para>
    /// For static fonts and variable fonts at the default instance, <see cref="Variation"/>
    /// is <c>default(FontVariationSettings)</c>, so an unspecified <c>Variation</c> is
    /// the zero-cost common case. Callers constructing keys for varied typefaces set
    /// <c>Variation</c> via initializer: <c>new FontCollectionKey(s, w, st) { Variation = settings }</c>.
    /// </para>
    /// </remarks>
    /// <param name="Style">The font style to use when constructing the key.</param>
    /// <param name="Weight">The font weight to use when constructing the key.</param>
    /// <param name="Stretch">The font stretch to use when constructing the key.</param>
    public readonly record struct FontCollectionKey(FontStyle Style, FontWeight Weight, FontStretch Stretch)
        : IComparable<FontCollectionKey>, IComparable
    {
        /// <summary>
        /// Gets the variation point for variable fonts. Defaults to
        /// <c>default(FontVariationSettings)</c> — the canonical "no variation" value
        /// that represents the font's default instance and stays equal across static
        /// fonts.
        /// </summary>
        public FontVariationSettings Variation { get; init; }

        /// <inheritdoc />
        public int CompareTo(FontCollectionKey other)
        {
            var cmp = ((int)Style).CompareTo((int)other.Style);

            if (cmp != 0)
            {
                return cmp;
            }

            cmp = ((int)Weight).CompareTo((int)other.Weight);

            if (cmp != 0)
            {
                return cmp;
            }

            return ((int)Stretch).CompareTo((int)other.Stretch);
        }

        /// <inheritdoc />
        public int CompareTo(object? obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is FontCollectionKey other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException($"Object must be of type {nameof(FontCollectionKey)}.", nameof(obj));
        }

        public static bool operator <(FontCollectionKey left, FontCollectionKey right) => left.CompareTo(right) < 0;

        public static bool operator <=(FontCollectionKey left, FontCollectionKey right) => left.CompareTo(right) <= 0;

        public static bool operator >(FontCollectionKey left, FontCollectionKey right) => left.CompareTo(right) > 0;

        public static bool operator >=(FontCollectionKey left, FontCollectionKey right) => left.CompareTo(right) >= 0;
    }
}
