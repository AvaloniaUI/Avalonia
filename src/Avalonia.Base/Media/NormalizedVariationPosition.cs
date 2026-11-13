using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    /// <summary>
    /// A single axis tag / normalized-coordinate pair within a
    /// <see cref="NormalizedVariationPosition"/>.
    /// </summary>
    /// <param name="Axis">The OpenType axis tag (e.g. <c>wght</c>, <c>wdth</c>).</param>
    /// <param name="NormalizedValue">
    /// The axis position in the OpenType normalized range <c>[-1.0, 1.0]</c>, as
    /// produced by applying the font's <c>avar</c> table to a user-space value.
    /// </param>
    internal readonly record struct NormalizedVariationCoordinate(OpenTypeTag Axis, float NormalizedValue);

    /// <summary>
    /// A variable font's position in OpenType normalized coordinate space: the internal
    /// currency between a typeface and its variation tables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Normalized coordinates are font-relative — the same value means different
    /// user-space positions under different <c>fvar</c> ranges and <c>avar</c> maps — so
    /// they are deliberately not public API. The public currency is the user-space
    /// <see cref="FontVariationSettings"/>; a position is derived from it per font, and
    /// keys the per-typeface variation caches.
    /// </para>
    /// <para>
    /// <see cref="NormalizedVariationPosition"/> is a value type with structural
    /// equality. The all-zero <see langword="default"/> value represents "no variation"
    /// — the font's design defaults. The <see cref="IsDefault"/> property tests for this
    /// case.
    /// </para>
    /// <para>
    /// Coordinates are stored in a single <see cref="ImmutableArray{T}"/> sorted by
    /// axis tag. Equality, hash and axis lookup are all linear scans over the array;
    /// for typical axis counts (one to a handful) this is faster than a hash-based
    /// dictionary and allocates nothing on the lookup path. The hash code is computed
    /// at construction and cached.
    /// </para>
    /// </remarks>
    internal readonly struct NormalizedVariationPosition : IEquatable<NormalizedVariationPosition>
    {
        private readonly ImmutableArray<NormalizedVariationCoordinate> _coordinates;
        private readonly int _hashCode;

        private NormalizedVariationPosition(ImmutableArray<NormalizedVariationCoordinate> sortedCoordinates)
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
        public ImmutableArray<NormalizedVariationCoordinate> Coordinates =>
            _coordinates.IsDefault ? ImmutableArray<NormalizedVariationCoordinate>.Empty : _coordinates;

        /// <summary>
        /// Gets a value indicating whether this is the default ("no variation")
        /// position — equivalent to <c>default(NormalizedVariationPosition)</c>.
        /// </summary>
        public bool IsDefault => _coordinates.IsDefaultOrEmpty;

        /// <summary>
        /// Creates a <see cref="NormalizedVariationPosition"/> from an axis-tag →
        /// normalized-coordinate map.
        /// </summary>
        /// <param name="normalizedCoordinates">
        /// Axis coordinates in the OpenType-normalized range <c>[-1.0, 1.0]</c>. The
        /// dictionary is copied into a sorted internal store.
        /// </param>
        /// <returns>
        /// <c>default(NormalizedVariationPosition)</c> when the input is empty or every
        /// coordinate is <c>0</c> (the axis default); otherwise a position carrying the
        /// sorted non-zero coordinates.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="normalizedCoordinates"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A coordinate value is <c>NaN</c> or outside <c>[-1, 1]</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The dictionary enumerates two entries for the same axis.
        /// </exception>
        public static NormalizedVariationPosition FromCoordinates(
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

            var builder = ImmutableArray.CreateBuilder<NormalizedVariationCoordinate>(normalizedCoordinates.Count);

            foreach (var kvp in normalizedCoordinates)
            {
                ValidateCoordinate(kvp.Value, kvp.Key, nameof(normalizedCoordinates));
                builder.Add(new NormalizedVariationCoordinate(kvp.Key, kvp.Value));
            }

            return CreateFromValidated(builder, nameof(normalizedCoordinates));
        }

        /// <summary>
        /// Creates a <see cref="NormalizedVariationPosition"/> from a span of coordinates.
        /// </summary>
        /// <param name="normalizedCoordinates">
        /// Axis coordinates in the OpenType-normalized range <c>[-1.0, 1.0]</c>. Each
        /// axis must appear at most once; the span is copied into a sorted internal
        /// store.
        /// </param>
        /// <returns>
        /// <c>default(NormalizedVariationPosition)</c> when the span is empty or every
        /// coordinate is <c>0</c> (the axis default); otherwise a position carrying the
        /// sorted non-zero coordinates.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A coordinate value is <c>NaN</c> or outside <c>[-1, 1]</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The span contains two entries for the same axis.
        /// </exception>
        public static NormalizedVariationPosition FromCoordinates(
            ReadOnlySpan<NormalizedVariationCoordinate> normalizedCoordinates)
        {
            if (normalizedCoordinates.IsEmpty)
            {
                return default;
            }

            var builder = ImmutableArray.CreateBuilder<NormalizedVariationCoordinate>(normalizedCoordinates.Length);

            foreach (var coord in normalizedCoordinates)
            {
                ValidateCoordinate(coord.NormalizedValue, coord.Axis, nameof(normalizedCoordinates));
                builder.Add(coord);
            }

            return CreateFromValidated(builder, nameof(normalizedCoordinates));
        }

        private static NormalizedVariationPosition CreateFromValidated(
            ImmutableArray<NormalizedVariationCoordinate>.Builder builder, string paramName)
        {
            builder.Sort(static (a, b) => ((uint)a.Axis).CompareTo((uint)b.Axis));

            for (var i = 1; i < builder.Count; i++)
            {
                if (builder[i].Axis == builder[i - 1].Axis)
                {
                    throw new ArgumentException(
                        $"Duplicate axis '{builder[i].Axis}' in coordinates.",
                        paramName);
                }
            }

            // A normalized value of 0 is the axis default: dropping it keeps explicitly-default
            // positions structurally equal to positions that omit the axis, so both produce one
            // cache key (and one variation clone) instead of two. Done after the duplicate check
            // so that duplicates still throw regardless of their values.
            for (var i = builder.Count - 1; i >= 0; i--)
            {
                if (builder[i].NormalizedValue == 0f)
                {
                    builder.RemoveAt(i);
                }
            }

            if (builder.Count == 0)
            {
                return default;
            }

            return new NormalizedVariationPosition(builder.ToImmutable());
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
        public bool Equals(NormalizedVariationPosition other)
        {
            // Cheap reject via the cached hash first; then an allocation-free element-wise
            // compare. Coordinates normalizes default → Empty, so the spans are always valid,
            // and the span overload (not LINQ SequenceEqual) avoids boxing the ImmutableArray
            // to IEnumerable — this type is used as a dictionary key. NormalizedVariationCoordinate
            // is a record struct, so the per-element compare uses its (Axis, NormalizedValue)
            // value equality.
            if (_hashCode != other._hashCode)
            {
                return false;
            }

            return Coordinates.AsSpan().SequenceEqual(other.Coordinates.AsSpan());
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is NormalizedVariationPosition other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => _hashCode;

        public static bool operator ==(NormalizedVariationPosition left, NormalizedVariationPosition right) => left.Equals(right);

        public static bool operator !=(NormalizedVariationPosition left, NormalizedVariationPosition right) => !left.Equals(right);

        private static void ValidateCoordinate(float value, OpenTypeTag axis, string paramName)
        {
            if (float.IsNaN(value) || value < -1f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    paramName, value,
                    $"Normalized coordinate for axis '{axis}' must be in [-1, 1]; was {value}.");
            }
        }

        private static int ComputeHashCode(ImmutableArray<NormalizedVariationCoordinate> coordinates)
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
