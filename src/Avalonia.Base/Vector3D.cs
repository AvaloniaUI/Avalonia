using System;
using System.Globalization;
using System.Numerics;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Utilities;

namespace Avalonia;

public readonly record struct Vector3D(double X, double Y, double Z)
{
    /// <summary>
    /// Parses a <see cref="Vector"/> string.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>The <see cref="Vector3D"/>.</returns>
    public static Vector3D Parse(string s)
    {
        using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid Vector."))
        {
            return new Vector3D(
                tokenizer.ReadDouble(),
                tokenizer.ReadDouble(),
                tokenizer.ReadDouble()
            );
        }
    }

    internal Vector3 ToVector3() => new Vector3((float)X, (float)Y, (float)Z);

    internal Vector3D(Vector3 v) : this(v.X, v.Y, v.Z)
    {
        
    }

    public static implicit operator Vector3D(Vector3 vector) => new(vector);
    
    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    public static double Dot(Vector3D vector1, Vector3D vector2) =>
        (vector1.X * vector2.X)
        + (vector1.Y * vector2.Y)
        + (vector1.Z * vector2.Z);

    /// <summary>
    /// Adds the second to the first vector
    /// </summary>
    public static Vector3D Add(Vector3D left, Vector3D right) =>
        new Vector3D(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    /// <summary>
    /// Adds the second to the first vector
    /// </summary>
    public static Vector3D operator +(Vector3D left, Vector3D right) => Add(left, right);

    /// <summary>
    /// Subtracts the second from the first vector
    /// </summary>
    public static Vector3D Substract(Vector3D left, Vector3D right) =>
        new Vector3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    
    /// <summary>
    /// Subtracts the second from the first vector
    /// </summary>
    public static Vector3D operator -(Vector3D left, Vector3D right) => Substract(left, right);

    /// <summary>
    /// Negates the vector
    /// </summary>
    public static Vector3D operator -(Vector3D v) => new(-v.X, -v.Y, -v.Z);

    /// <summary>
    /// Multiplies the first vector by the second.
    /// </summary>
    public static Vector3D Multiply(Vector3D left, Vector3D right) =>
        new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);

    /// <summary>
    /// Multiplies the vector by the given scalar.
    /// </summary>
    public static Vector3D Multiply(Vector3D left, double right) =>
        new(left.X * right, left.Y * right, left.Z * right);

    /// <summary>
    /// Multiplies the vector by the given scalar.
    /// </summary>
    public static Vector3D operator *(Vector3D left, double right) => Multiply(left, right);
    
    /// <summary>
    /// Divides the first vector by the second.
    /// </summary>
    public static Vector3D Divide(Vector3D left, Vector3D right) =>
        new(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    
    /// <summary>
    /// Divides the vector by the given scalar.
    /// </summary>
    public static Vector3D Divide(Vector3D left, double right) =>
        new(left.X / right, left.Y / right, left.Z / right);

    /// <summary>Returns a vector whose elements are the absolute values of each of the specified vector's elements.</summary>
    public Vector3D Abs() => new(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));

    /// <summary>
    /// Restricts a vector between a minimum and a maximum value.
    /// </summary>
    public static Vector3D Clamp(Vector3D value, Vector3D min, Vector3D max) => 
        Min(Max(value, min), max);

    /// <summary>
    /// Returns a vector whose elements are the maximum of each of the pairs of elements in two specified vectors
    /// </summary>
    public static Vector3D Max(Vector3D left, Vector3D right) =>
        new(Math.Max(left.X, right.X), Math.Max(left.Y, right.Y), Math.Max(left.Z, right.Z));
        
    /// <summary>
    /// Returns a vector whose elements are the minimum of each of the pairs of elements in two specified vectors
    /// </summary>
    public static Vector3D Min(Vector3D left, Vector3D right) =>
        new(Math.Min(left.X, right.X), Math.Min(left.Y, right.Y), Math.Min(left.Z, right.Z));

    /// <summary>
    /// Length of the vector.
    /// </summary>
    public double Length => Math.Sqrt(Dot(this, this));
    
    /// <summary>
    /// Returns a normalized version of this vector.
    /// </summary>
    public static Vector3D Normalize(Vector3D value) => Divide(value, value.Length);
    
    /// <summary>
    /// Computes the squared Euclidean distance between the two given points.
    /// </summary>
    public static double DistanceSquared(Vector3D value1, Vector3D value2)
    {
        var difference = Vector3D.Substract(value1, value2);
        return Dot(difference, difference);
    }
    
    /// <summary>
    /// Computes the Euclidean distance between the two given points.
    /// </summary>
    public static double Distance(Vector3D value1, Vector3D value2) => Math.Sqrt(DistanceSquared(value1, value2));
}
