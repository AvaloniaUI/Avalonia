using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    /// <summary>
    /// A single variation axis setting in designer units, e.g. <c>wght = 700</c>.
    /// </summary>
    /// <param name="Tag">The OpenType axis tag (e.g. <c>wght</c>, <c>wdth</c>, <c>opsz</c>).</param>
    /// <param name="Value">
    /// The axis position in the font's user coordinate space — the same units the font's
    /// <c>fvar</c> table declares for the axis (weight 100–900, width percentages, optical
    /// sizes in points). Values are clamped to the axis range and normalized per font when
    /// the settings are applied; axes a font does not declare are ignored.
    /// </param>
    public readonly record struct FontVariation(OpenTypeTag Tag, double Value)
    {
        /// <summary>Returns the <c>tag=value</c> form, e.g. <c>wght=700</c>.</summary>
        public override string ToString() =>
            string.Create(CultureInfo.InvariantCulture, $"{Tag}={Value}");
    }

    /// <summary>
    /// An immutable, order-independent set of user-space variation axis values that
    /// configures variable fonts (fonts with an <c>fvar</c> table) for rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Values are expressed in the font's user coordinate space (<c>wght = 700</c>), the
    /// same space CSS <c>font-variation-settings</c>, DirectWrite and HarfBuzz use.
    /// Normalization to the OpenType <c>[-1, 1]</c> range — including the <c>avar</c>
    /// mapping — happens per font when the settings are applied, so one settings value is
    /// meaningful across fonts: each font clamps to its own axis ranges and ignores axes
    /// it does not declare.
    /// </para>
    /// <para>
    /// Instances have structural equality with a cached hash code and are usable as cache
    /// keys. Variations are stored sorted by axis tag, so equality is order-independent;
    /// when the same tag is given more than once, the last value wins (CSS behavior).
    /// <c>null</c> and <see cref="Empty"/> both mean "design defaults".
    /// </para>
    /// <para>
    /// The string form accepted by <see cref="Parse"/> (and produced by
    /// <see cref="ToString"/>) is a comma-separated list of <c>tag=value</c> pairs, e.g.
    /// <c>"wght=700, wdth=85"</c>, usable directly in XAML.
    /// </para>
    /// </remarks>
    public sealed class FontVariationSettings : IEquatable<FontVariationSettings>
    {
        private readonly ImmutableArray<FontVariation> _variations;
        private readonly int _hashCode;

        private FontVariationSettings(ImmutableArray<FontVariation> sortedVariations)
        {
            _variations = sortedVariations;
            _hashCode = ComputeHashCode(sortedVariations);
        }

        /// <summary>Gets the empty settings — the font's design defaults.</summary>
        public static FontVariationSettings Empty { get; } =
            new(ImmutableArray<FontVariation>.Empty);

        /// <summary>
        /// Gets the variations, sorted by axis tag ascending. Duplicate tags passed at
        /// construction have already been collapsed to their last value.
        /// </summary>
        public ImmutableArray<FontVariation> Variations => _variations;

        /// <summary>Gets a value indicating whether no axis is set.</summary>
        public bool IsEmpty => _variations.IsEmpty;

        /// <summary>
        /// Creates settings from variation values. When a tag appears more than once, the
        /// last occurrence wins.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="variations"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">A value is <c>NaN</c> or infinite.</exception>
        public FontVariationSettings(IEnumerable<FontVariation> variations)
        {
            if (variations is null)
            {
                throw new ArgumentNullException(nameof(variations));
            }

            var builder = ImmutableArray.CreateBuilder<FontVariation>();

            foreach (var variation in variations)
            {
                if (double.IsNaN(variation.Value) || double.IsInfinity(variation.Value))
                {
                    throw new ArgumentException(
                        $"Value for axis '{variation.Tag}' must be finite; was {variation.Value}.",
                        nameof(variations));
                }

                // Last-wins for duplicate tags, matching CSS font-variation-settings.
                var replaced = false;

                for (var i = 0; i < builder.Count; i++)
                {
                    if (builder[i].Tag == variation.Tag)
                    {
                        builder[i] = variation;
                        replaced = true;
                        break;
                    }
                }

                if (!replaced)
                {
                    builder.Add(variation);
                }
            }

            builder.Sort(static (a, b) => ((uint)a.Tag).CompareTo((uint)b.Tag));

            _variations = builder.ToImmutable();
            _hashCode = ComputeHashCode(_variations);
        }

        /// <summary>
        /// Looks up the value for an axis.
        /// </summary>
        /// <param name="tag">The axis tag.</param>
        /// <param name="value">The axis value, or <c>0</c> when the axis is not set.</param>
        /// <returns><c>true</c> when the axis is set; <c>false</c> otherwise.</returns>
        public bool TryGetValue(OpenTypeTag tag, out double value)
        {
            foreach (var variation in _variations)
            {
                if (variation.Tag == tag)
                {
                    value = variation.Value;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses a comma-separated list of <c>tag=value</c> pairs, e.g.
        /// <c>"wght=700, wdth=85"</c>. Whitespace around pairs, tags and values is
        /// ignored; an empty string yields <see cref="Empty"/>; duplicate tags collapse
        /// to the last value.
        /// </summary>
        /// <exception cref="FormatException">A pair is not <c>tag=value</c>, a tag is not
        /// a valid four-character OpenType tag, or a value is not a finite invariant
        /// number.</exception>
        public static FontVariationSettings Parse(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (string.IsNullOrWhiteSpace(s))
            {
                return Empty;
            }

            var variations = new List<FontVariation>();

            foreach (var part in s.Split(','))
            {
                var pair = part.AsSpan().Trim();

                if (pair.IsEmpty)
                {
                    continue;
                }

                var separator = pair.IndexOf('=');

                if (separator <= 0 || separator == pair.Length - 1)
                {
                    throw new FormatException(
                        $"Invalid font variation '{pair.ToString()}': expected tag=value.");
                }

                var tagText = pair.Slice(0, separator).Trim();
                var valueText = pair.Slice(separator + 1).Trim();

                if (tagText.IsEmpty || tagText.Length > 4)
                {
                    throw new FormatException(
                        $"Invalid font variation axis tag '{tagText.ToString()}'.");
                }

                if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                    double.IsNaN(value) || double.IsInfinity(value))
                {
                    throw new FormatException(
                        $"Invalid font variation value '{valueText.ToString()}' for axis '{tagText.ToString()}'.");
                }

                variations.Add(new FontVariation(OpenTypeTag.Parse(tagText.ToString()), value));
            }

            return variations.Count == 0 ? Empty : new FontVariationSettings(variations);
        }

        /// <summary>Returns the parseable string form, e.g. <c>wght=700,wdth=85</c>.</summary>
        public override string ToString()
        {
            if (_variations.IsEmpty)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var variation in _variations)
            {
                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(variation.ToString());
            }

            return builder.ToString();
        }

        /// <inheritdoc/>
        public bool Equals(FontVariationSettings? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (_hashCode != other._hashCode)
            {
                return false;
            }

            return _variations.AsSpan().SequenceEqual(other._variations.AsSpan());
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as FontVariationSettings);

        /// <inheritdoc/>
        public override int GetHashCode() => _hashCode;

        private static int ComputeHashCode(ImmutableArray<FontVariation> variations)
        {
            var hash = new HashCode();

            foreach (var variation in variations)
            {
                hash.Add(variation.Tag);
                hash.Add(variation.Value);
            }

            return hash.ToHashCode();
        }
    }
}
