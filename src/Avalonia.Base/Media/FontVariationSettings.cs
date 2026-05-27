using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    /// <summary>
    /// A single axis tag / normalized-coordinate pair within a
    /// <see cref="FontVariationSettings"/>.
    /// </summary>
    /// <param name="Axis">The OpenType axis tag (e.g. <c>wght</c>, <c>wdth</c>).</param>
    /// <param name="NormalizedValue">
    /// The axis position in the OpenType normalized range <c>[-1.0, 1.0]</c>, as
    /// produced by applying the font's <c>avar</c> table to a user-space value.
    /// </param>
    public readonly record struct FontVariationCoordinate(OpenTypeTag Axis, float NormalizedValue);

    /// <summary>
    /// Describes how a variable font (one with an <c>fvar</c> table) should be configured
    /// for rendering: a set of normalized axis coordinates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="FontVariationSettings"/> is a value type with structural equality.
    /// The all-zero <see langword="default"/> value represents "no variation" — pass
    /// <c>default(FontVariationSettings)</c> (or rely on a parameter default) to leave
    /// the font at its design defaults. The <see cref="IsDefault"/> property tests for
    /// this case.
    /// </para>
    /// <para>
    /// Settings are typeface-agnostic: the renderer maps coordinates to whichever axes
    /// the active font exposes, and silently ignores axes the font does not have. The
    /// same settings value can be shared across a variable-font family.
    /// </para>
    /// <para>
    /// To construct settings from human-readable user-space axis values (e.g.
    /// <c>wght = 700</c>), use <c>GlyphTypeface.CreateVariationSettings</c>; it reads
    /// the font's <c>fvar</c> / <c>avar</c> tables and normalizes the values. Use
    /// <see cref="FromCoordinates(IReadOnlyDictionary{OpenTypeTag, float})"/> or the
    /// span overload directly only when the normalized values are already known.
    /// </para>
    /// <para>
    /// Coordinates are stored in a single <see cref="ImmutableArray{T}"/> sorted by
    /// axis tag. Equality, hash and axis lookup are all linear scans over the array;
    /// for typical axis counts (one to a handful) this is faster than a hash-based
    /// dictionary and allocates nothing on the lookup path. The hash code is computed
    /// at construction and cached.
    /// </para>
    /// </remarks>
    public readonly struct FontVariationSettings : IEquatable<FontVariationSettings>
    {
        private readonly ImmutableArray<FontVariationCoordinate> _coordinates;
        private readonly int _hashCode;

        private FontVariationSettings(ImmutableArray<FontVariationCoordinate> sortedCoordinates)
        {
            _coordinates = sortedCoordinates;
            _hashCode = ComputeHashCode(sortedCoordinates);
        }

        /// <summary>
        /// Gets the axis coordinates, sorted by <see cref="OpenTypeTag"/> ascending.
        /// </summary>
        /// <remarks>
        /// Always returns a non-default (possibly empty) <see cref="ImmutableArray{T}"/>.
        /// Callers can iterate, index, or pass it to span-based APIs without first
        /// checking <see cref="ImmutableArray{T}.IsDefault"/>.
        /// </remarks>
        public ImmutableArray<FontVariationCoordinate> Coordinates =>
            _coordinates.IsDefault ? ImmutableArray<FontVariationCoordinate>.Empty : _coordinates;

        /// <summary>
        /// Gets a value indicating whether these are the default ("no variation")
        /// settings — equivalent to <c>default(FontVariationSettings)</c>.
        /// </summary>
        public bool IsDefault => _coordinates.IsDefaultOrEmpty;

        /// <summary>
        /// Creates a <see cref="FontVariationSettings"/> from an axis-tag → normalized-coordinate
        /// map.
        /// </summary>
        /// <param name="normalizedCoordinates">
        /// Axis coordinates in the OpenType-normalized range <c>[-1.0, 1.0]</c>. The
        /// dictionary is copied into a sorted internal store.
        /// <para>
        /// Prefer <c>GlyphTypeface.CreateVariationSettings</c> when you have user-space
        /// axis values (e.g. <c>wght = 700</c>) — it normalizes them via the font's
        /// <c>fvar</c> / <c>avar</c> tables automatically.
        /// </para>
        /// </param>
        /// <returns>
        /// <c>default(FontVariationSettings)</c> when the input is empty; otherwise a
        /// settings value carrying the sorted coordinates.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="normalizedCoordinates"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A coordinate value is <c>NaN</c> or outside <c>[-1, 1]</c>.
        /// </exception>
        public static FontVariationSettings FromCoordinates(
            IReadOnlyDictionary<OpenTypeTag, float> normalizedCoordinates)
        {
            if (normalizedCoordinates is null)
            {
                throw new ArgumentNullException(nameof(normalizedCoordinates));
            }

            if (normalizedCoordinates.Count == 0)
            {
                return default;
            }

            var builder = ImmutableArray.CreateBuilder<FontVariationCoordinate>(normalizedCoordinates.Count);

            foreach (var kvp in normalizedCoordinates)
            {
                ValidateCoordinate(kvp.Value, kvp.Key, nameof(normalizedCoordinates));
                builder.Add(new FontVariationCoordinate(kvp.Key, kvp.Value));
            }

            builder.Sort(static (a, b) => ((uint)a.Axis).CompareTo((uint)b.Axis));

            return new FontVariationSettings(builder.MoveToImmutable());
        }

        /// <summary>
        /// Creates a <see cref="FontVariationSettings"/> from a span of coordinates.
        /// </summary>
        /// <param name="normalizedCoordinates">
        /// Axis coordinates in the OpenType-normalized range <c>[-1.0, 1.0]</c>. Each
        /// axis must appear at most once; the span is copied into a sorted internal
        /// store.
        /// </param>
        /// <returns>
        /// <c>default(FontVariationSettings)</c> when the span is empty; otherwise a
        /// settings value carrying the sorted coordinates.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A coordinate value is <c>NaN</c> or outside <c>[-1, 1]</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The span contains two entries for the same axis.
        /// </exception>
        public static FontVariationSettings FromCoordinates(
            ReadOnlySpan<FontVariationCoordinate> normalizedCoordinates)
        {
            if (normalizedCoordinates.IsEmpty)
            {
                return default;
            }

            var builder = ImmutableArray.CreateBuilder<FontVariationCoordinate>(normalizedCoordinates.Length);

            foreach (var coord in normalizedCoordinates)
            {
                ValidateCoordinate(coord.NormalizedValue, coord.Axis, nameof(normalizedCoordinates));
                builder.Add(coord);
            }

            builder.Sort(static (a, b) => ((uint)a.Axis).CompareTo((uint)b.Axis));

            for (var i = 1; i < builder.Count; i++)
            {
                if (builder[i].Axis == builder[i - 1].Axis)
                {
                    throw new ArgumentException(
                        $"Duplicate axis '{builder[i].Axis}' in coordinates.",
                        nameof(normalizedCoordinates));
                }
            }

            return new FontVariationSettings(builder.MoveToImmutable());
        }

        /// <summary>
        /// Looks up the normalized value for a single axis.
        /// </summary>
        /// <param name="axis">The axis tag to look up.</param>
        /// <param name="normalizedValue">The axis's normalized value, or <c>0</c> when
        /// the axis is not present.</param>
        /// <returns><c>true</c> when the axis is present; <c>false</c> otherwise.</returns>
        public bool TryGetCoordinate(OpenTypeTag axis, out float normalizedValue)
        {
            if (!_coordinates.IsDefault)
            {
                foreach (var coord in _coordinates)
                {
                    if (coord.Axis == axis)
                    {
                        normalizedValue = coord.NormalizedValue;
                        return true;
                    }
                }
            }

            normalizedValue = 0f;
            return false;
        }

        /// <summary>
        /// Returns the normalized value for a single axis, or <paramref name="fallback"/>
        /// if the axis is not present.
        /// </summary>
        public float GetCoordinateOrDefault(OpenTypeTag axis, float fallback = 0f)
            => TryGetCoordinate(axis, out var value) ? value : fallback;

        /// <inheritdoc/>
        public bool Equals(FontVariationSettings other)
        {
            if (_hashCode != other._hashCode)
            {
                return false;
            }

            var a = _coordinates;
            var b = other._coordinates;

            var aLen = a.IsDefault ? 0 : a.Length;
            var bLen = b.IsDefault ? 0 : b.Length;

            if (aLen != bLen)
            {
                return false;
            }

            for (var i = 0; i < aLen; i++)
            {
                if (a[i].Axis != b[i].Axis || a[i].NormalizedValue != b[i].NormalizedValue)
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is FontVariationSettings other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => _hashCode;

        public static bool operator ==(FontVariationSettings left, FontVariationSettings right) => left.Equals(right);

        public static bool operator !=(FontVariationSettings left, FontVariationSettings right) => !left.Equals(right);

        private static void ValidateCoordinate(float value, OpenTypeTag axis, string paramName)
        {
            if (float.IsNaN(value) || value < -1f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    paramName, value,
                    $"Normalized coordinate for axis '{axis}' must be in [-1, 1]; was {value}.");
            }
        }

        private static int ComputeHashCode(ImmutableArray<FontVariationCoordinate> coordinates)
        {
            if (coordinates.IsDefaultOrEmpty)
            {
                return 0;
            }

            var hash = new HashCode();
            foreach (var coord in coordinates)
            {
                hash.Add(coord.Axis);
                hash.Add(coord.NormalizedValue);
            }
            return hash.ToHashCode();
        }
    }
}
