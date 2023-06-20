using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// Represents the sequence of dashes and gaps that will be applied by an
    /// <see cref="ImmutablePen"/>.
    /// </summary>
    public class ImmutableDashStyle : IDashStyle, IEquatable<IDashStyle>
    {
        private readonly double[] _dashes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableDashStyle"/> class.
        /// </summary>
        /// <param name="dashes">The dashes collection.</param>
        /// <param name="offset">The dash sequence offset.</param>
        public ImmutableDashStyle(IEnumerable<double>? dashes, double offset)
        {
            _dashes = dashes?.ToArray() ?? Array.Empty<double>();
            Offset = offset;
        }

        /// <inheritdoc/>
        public IReadOnlyList<double> Dashes => _dashes;

        /// <inheritdoc/>
        public double Offset { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as IDashStyle);

        /// <inheritdoc/>
        public bool Equals(IDashStyle? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other is not null && Offset == other.Offset && SequenceEqual(_dashes, other.Dashes);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 717868523;
            hashCode = hashCode * -1521134295 + Offset.GetHashCode();

            foreach (var i in _dashes)
            {
                hashCode = hashCode * -1521134295 + i.GetHashCode();
            }

            return hashCode;
        }

        private static bool SequenceEqual(double[] left, IReadOnlyList<double>? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (right is null || left.Length != right.Count)
            {
                return false;
            }

            for (var c = 0; c < left.Length; c++)
            {
                if (left[c] != right[c])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
