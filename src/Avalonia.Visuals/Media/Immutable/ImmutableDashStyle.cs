using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// Represents the sequence of dashes and gaps that will be applied by an
    /// <see cref="ImmutablePen"/>.
    /// </summary>
    public class ImmutableDashStyle : IDashStyle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableDashStyle"/> class.
        /// </summary>
        /// <param name="dashes">The dashes collection.</param>
        /// <param name="offset">The dash sequence offset.</param>
        public ImmutableDashStyle(IEnumerable<double> dashes, double offset)
        {
            Dashes = (IReadOnlyList<double>)dashes?.ToList() ?? Array.Empty<double>();
            Offset = offset;
        }

        /// <inheritdoc/>
        public IReadOnlyList<double> Dashes { get; }

        /// <inheritdoc/>
        public double Offset { get; }
    }
}
