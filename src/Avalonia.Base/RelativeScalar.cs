using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia;

/// <summary>
/// Defines a scalar value that may be defined relative to a containing element.
/// </summary>
public struct RelativeScalar : IEquatable<RelativeScalar>
{
    private readonly double _scalar;

    private readonly RelativeUnit _unit;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelativeScalar"/> struct.
    /// </summary>
    /// <param name="scalar">The scalar value.</param>
    /// <param name="unit">The unit.</param>
    public RelativeScalar(double scalar, RelativeUnit unit)
    {
        _scalar = scalar;
        _unit = unit;
    }
    
    /// <summary>
    /// Gets the scalar.
    /// </summary>
    public double Scalar => _scalar;

    /// <summary>
    /// Gets the unit.
    /// </summary>
    public RelativeUnit Unit => _unit;

    /// <summary>
    /// The value at the beginning of the range
    /// </summary>
    public static RelativeScalar Beginning { get; } = new RelativeScalar(0, RelativeUnit.Relative); 
    
    /// <summary>
    /// The value at the middle of the range
    /// </summary>
    public static RelativeScalar Middle { get; } = new RelativeScalar(0.5, RelativeUnit.Relative); 
        
    /// <summary>
    /// The value at the end of the range
    /// </summary>
    public static RelativeScalar End { get; } = new RelativeScalar(1, RelativeUnit.Relative); 

    public bool Equals(RelativeScalar other)
    {
        return _scalar.Equals(other._scalar) && _unit == other._unit;
    }

    public override bool Equals(object? obj)
    {
        return obj is RelativeScalar other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _scalar.GetHashCode() ^ (int)_unit;
    }

    public static bool operator ==(RelativeScalar left, RelativeScalar right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RelativeScalar left, RelativeScalar right)
    {
        return !left.Equals(right);
    }
    
        /// <summary>
        /// Converts a <see cref="RelativeScalar"/> into a final value.
        /// </summary>
        /// <returns>The origin point in pixels.</returns>
        public double ToValue(double size)
        {
            return _unit == RelativeUnit.Absolute
                ? _scalar
                : size * _scalar;
        }

        /// <summary>
        /// Parses a <see cref="RelativeScalar"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The parsed <see cref="RelativeScalar"/>.</returns>
        public static RelativeScalar Parse(string s)
        {
            var trimmed = s.Trim();
            if (trimmed.EndsWith("%"))
                return new RelativeScalar(double.Parse(trimmed.TrimEnd('%'), CultureInfo.InvariantCulture) * 0.01,
                    RelativeUnit.Relative);

            return new RelativeScalar(double.Parse(trimmed, CultureInfo.InvariantCulture), RelativeUnit.Absolute);
        }

        /// <summary>
        /// Returns a String representing this RelativeScalar instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return _unit == RelativeUnit.Absolute
                ? _scalar.ToString(CultureInfo.InvariantCulture)
                : string.Format(CultureInfo.InvariantCulture, "{0}%", _scalar * 100);
        }
}