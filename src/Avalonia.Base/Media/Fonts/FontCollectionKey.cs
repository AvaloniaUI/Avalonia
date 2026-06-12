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
    /// <see cref="Style"/>, then <see cref="Weight"/>, then <see cref="Stretch"/>, then by
    /// the <see cref="Variation"/> coordinates.
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

            cmp = ((int)Stretch).CompareTo((int)other.Stretch);

            if (cmp != 0)
            {
                return cmp;
            }

            // Keep the ordering consistent with the synthesized record equality, which includes
            // Variation: keys that differ only in variation must not compare equal. Coordinates
            // are sorted by axis and zero-canonicalized at construction, so an element-wise
            // lexicographic comparison is a total order that agrees with Equals.
            return CompareVariations(Variation, other.Variation);
        }

        private static int CompareVariations(FontVariationSettings left, FontVariationSettings right)
        {
            var a = left.Coordinates;
            var b = right.Coordinates;
            var sharedLength = Math.Min(a.Length, b.Length);

            for (var i = 0; i < sharedLength; i++)
            {
                var cmp = ((uint)a[i].Axis).CompareTo((uint)b[i].Axis);

                if (cmp != 0)
                {
                    return cmp;
                }

                cmp = a[i].NormalizedValue.CompareTo(b[i].NormalizedValue);

                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return a.Length.CompareTo(b.Length);
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
