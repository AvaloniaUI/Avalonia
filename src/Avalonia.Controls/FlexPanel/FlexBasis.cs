using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Avalonia.Controls
{
    /// <summary>
    /// Specifies the initial size of a flex item.
    /// </summary>
    public readonly struct FlexBasis : IEquatable<FlexBasis>
    {
        public double Value { get; }

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

        public static FlexBasis Auto => new(0.0, FlexBasisKind.Auto);

        public bool IsAuto => Kind == FlexBasisKind.Auto;
    
        public bool IsAbsolute => Kind == FlexBasisKind.Absolute;
    
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
            return str.ToUpperInvariant() switch
            {
                "AUTO" => Auto,
                var s when s.EndsWith("%") => new FlexBasis(ParseDouble(s.TrimEnd('%').TrimEnd()) / 100, FlexBasisKind.Relative),
                _ => new FlexBasis(ParseDouble(str), FlexBasisKind.Absolute),
            };
            double ParseDouble(string s) => double.Parse(s, CultureInfo.InvariantCulture);
        }
    }
}
