using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how a variable font (one with an <c>fvar</c> table) should be configured
    /// for rendering: a set of normalized axis coordinates and/or a named variation instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances are immutable and have value-style equality. Two instances are equal when
    /// they reference the same named instance and the same set of axis coordinates.
    /// </para>
    /// <para>
    /// Construction is currently internal while public APIs that consume axis configuration
    /// are still being wired up; pass <see cref="Default"/> or <c>null</c> from outside the
    /// assembly to mean "no variation applied".
    /// </para>
    /// </remarks>
    public sealed class FontVariationSettings : IEquatable<FontVariationSettings>
    {
        /// <summary>
        /// Singleton instance representing "no variation": no axis coordinates and no
        /// named instance.
        /// </summary>
        public static FontVariationSettings Default { get; } =
            new FontVariationSettings(FrozenDictionary<OpenTypeTag, float>.Empty, instanceIndex: null);

        private readonly FrozenDictionary<OpenTypeTag, float> _normalizedCoordinates;

        private FontVariationSettings(
            FrozenDictionary<OpenTypeTag, float> normalizedCoordinates,
            int? instanceIndex)
        {
            _normalizedCoordinates = normalizedCoordinates;
            InstanceIndex = instanceIndex;
        }

        /// <summary>
        /// Gets the normalized variation coordinates per axis tag.
        /// </summary>
        /// <remarks>
        /// Values are in the OpenType-normalized range <c>[-1.0, 1.0]</c>, as produced by
        /// applying the <c>avar</c> table to user-space axis values. The returned view is
        /// backed by an immutable, defensively-copied store; callers cannot mutate it.
        /// Empty when no coordinates were provided.
        /// </remarks>
        public IReadOnlyDictionary<OpenTypeTag, float> NormalizedCoordinates => _normalizedCoordinates;

        /// <summary>
        /// Gets the optional named-instance index from the font's <c>fvar</c> table.
        /// </summary>
        /// <remarks>
        /// When set, the named instance supplies the baseline coordinates for axes that are
        /// not also present in <see cref="NormalizedCoordinates"/>; per-axis entries in
        /// <see cref="NormalizedCoordinates"/> override the corresponding instance values.
        /// </remarks>
        public int? InstanceIndex { get; }

        /// <summary>
        /// Creates a <see cref="FontVariationSettings"/> from an axis-tag → normalized-coordinate
        /// map, optionally combined with a named-instance index.
        /// </summary>
        /// <param name="normalizedCoordinates">Axis coordinates in <c>[-1.0, 1.0]</c>. The
        /// dictionary is defensively copied; mutations to the caller's instance after this
        /// call have no effect on the returned settings.</param>
        /// <param name="instanceIndex">Optional named-instance index. Must be non-negative if set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="normalizedCoordinates"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A coordinate value is <c>NaN</c> or outside <c>[-1, 1]</c>, or
        /// <paramref name="instanceIndex"/> is negative.
        /// </exception>
        internal static FontVariationSettings FromCoordinates(
            IReadOnlyDictionary<OpenTypeTag, float> normalizedCoordinates,
            int? instanceIndex = null)
        {
            if (normalizedCoordinates is null)
            {
                throw new ArgumentNullException(nameof(normalizedCoordinates));
            }

            if (instanceIndex is { } idx && idx < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(instanceIndex), idx, "Instance index must be non-negative.");
            }

            foreach (var kvp in normalizedCoordinates)
            {
                if (float.IsNaN(kvp.Value) || kvp.Value < -1f || kvp.Value > 1f)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(normalizedCoordinates), kvp.Value,
                        $"Normalized coordinate for axis '{kvp.Key}' must be in [-1, 1]; was {kvp.Value}.");
                }
            }

            if (normalizedCoordinates.Count == 0 && instanceIndex is null)
            {
                return Default;
            }

            return new FontVariationSettings(normalizedCoordinates.ToFrozenDictionary(), instanceIndex);
        }

        /// <summary>
        /// Creates a <see cref="FontVariationSettings"/> that selects a named variation
        /// instance from the font's <c>fvar</c> table without any per-axis overrides.
        /// </summary>
        /// <param name="instanceIndex">Index of the named instance. Must be non-negative.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="instanceIndex"/> is negative.</exception>
        internal static FontVariationSettings FromInstance(int instanceIndex)
        {
            if (instanceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(instanceIndex), instanceIndex, "Instance index must be non-negative.");
            }

            return new FontVariationSettings(FrozenDictionary<OpenTypeTag, float>.Empty, instanceIndex);
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

            if (InstanceIndex != other.InstanceIndex)
            {
                return false;
            }

            if (_normalizedCoordinates.Count != other._normalizedCoordinates.Count)
            {
                return false;
            }

            foreach (var kvp in _normalizedCoordinates)
            {
                if (!other._normalizedCoordinates.TryGetValue(kvp.Key, out var v) || v != kvp.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is FontVariationSettings other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(InstanceIndex);

            // FrozenDictionary's enumeration order is implementation-defined, so sort by
            // axis tag for a hash that depends only on the (key, value) set.
            foreach (var kvp in _normalizedCoordinates.OrderBy(k => (uint)k.Key))
            {
                hash.Add(kvp.Key);
                hash.Add(kvp.Value);
            }

            return hash.ToHashCode();
        }

        public static bool operator ==(FontVariationSettings? left, FontVariationSettings? right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(FontVariationSettings? left, FontVariationSettings? right) => !(left == right);
    }
}
