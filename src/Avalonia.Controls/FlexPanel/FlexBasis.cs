using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Avalonia.Controls
{
    /// <summary>
    /// Specifies the initial size of a flex item.
    /// </summary>
    public readonly partial struct FlexBasis : IEquatable<FlexBasis>
    {
        /// <summary>
        /// Gets the value of the <see cref="FlexBasis"/>. The meaning of this value depends on the <see cref="FlexBasisKind"/>
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Gets the <see cref="FlexBasisKind"/>. This determines how the value affects the size of the flex item
        /// </summary>
        public FlexBasisKind Kind { get; }

        /// <summary>
        /// Initializes an instance of <see cref="FlexBasis"/> and sets the value and <see cref="FlexBasisKind"/>
        /// </summary>
        /// <param name="value">The value of the <see cref="FlexBasis"/></param>
        /// <param name="kind">The <see cref="FlexBasisKind">. This determines how the value affects the size of the flex item</see>/></param>
        /// <exception cref="ArgumentException"></exception>
        public FlexBasis(double value, FlexBasisKind kind)
        {
            if (value < 0 || double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException($"Invalid basis value: {value}", nameof(value));
            if (kind < FlexBasisKind.Auto || kind > FlexBasisKind.Relative)
                throw new ArgumentException($"Invalid basis kind: {kind}", nameof(kind));
            Value = value;
            Kind = kind;
        }

        /// <summary>
        /// Initializes an instance of <see cref="FlexBasis"/> and sets the absolute value
        /// </summary>
        /// <param name="value">The absolute value of the <see cref="FlexBasis"/></param>
        /// <exception cref="ArgumentException"></exception>
        public FlexBasis(double value) : this(value, FlexBasisKind.Absolute) { }

        /// <summary>
        /// Gets a <see cref="FlexBasis"/> instance that represents the "auto" value, which means the size of the flex item is determined by its content or other factors.
        /// </summary>
        public static FlexBasis Auto => new(0.0, FlexBasisKind.Auto);

        /// <summary>
        /// Gets a value indicating whether the <see cref="FlexBasis"/> is set to "auto".
        /// </summary>
        public bool IsAuto => Kind == FlexBasisKind.Auto;

        /// <summary>
        /// Gets a value indicating whether the <see cref="FlexBasis"/> is set to an absolute value.
        /// </summary>
        public bool IsAbsolute => Kind == FlexBasisKind.Absolute;

        /// <summary>
        /// Gets a value indicating whether the <see cref="FlexBasis"/> is set to a relative value.
        /// </summary>
        public bool IsRelative => Kind == FlexBasisKind.Relative;

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public bool Equals(FlexBasis other) =>
            (IsAuto && other.IsAuto) || (Value == other.Value && Kind == other.Kind);

        public override bool Equals(object? obj) =>
            obj is FlexBasis other && Equals(other);

        public override int GetHashCode() =>
            (Value, Kind).GetHashCode();

        public static bool operator ==(FlexBasis left, FlexBasis right) =>
            left.Equals(right);

        public static bool operator !=(FlexBasis left, FlexBasis right) =>
            !left.Equals(right);

        public override string ToString()
        {
            return Kind switch
            {
                FlexBasisKind.Auto => "Auto",
                FlexBasisKind.Absolute => FormattableString.Invariant($"{Value:G17}"),
                FlexBasisKind.Relative => FormattableString.Invariant($"{Value * 100:G17}%"),
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Converts a string flex-basis value to a <see cref="FlexBasis"/> instance.
        /// </summary>
        /// <param name="str">The value to parse.</param>
        /// <returns></returns>
        public static FlexBasis Parse(string str)
        {
            var span = str.AsSpan().Trim();
            if (string.Equals(str, "AUTO", StringComparison.OrdinalIgnoreCase))
            {
                return Auto;
            }
            else if (span.EndsWith("%") && double.TryParse(span[..^1], CultureInfo.InvariantCulture, out var val))
            {
                return new FlexBasis(val / 100, FlexBasisKind.Relative);
            }
            else if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return new FlexBasis(value, FlexBasisKind.Absolute);
            }

            throw new ArgumentException($"Value '{str}' is not a valid flex-basis value. Valid values are 'auto', a number (e.g., '100'), or a percentage (e.g., '50%').", nameof(str));
        }
    }
}
