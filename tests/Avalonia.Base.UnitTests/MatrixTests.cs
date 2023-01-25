using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Visuals.UnitTests;

/// <summary>
///  These tests use the "official" Matrix4x4 and Matrix3x2 from the System.Numerics namespace, to validate
///  that Avalonias own implementation of a 3x3 Matrix works correctly.
/// </summary>
public class MatrixTests
{
    /// <summary>
    ///  Because Avalonia is working internally with doubles, but System.Numerics Vector and Matrix implementations
    ///  only make use of floats, we need to reduce precision, comparing them. It should be sufficient to compare
    ///  5 fractional digits to ensure, that the result is correct.
    /// </summary>
    /// <param name="expected">The expected vector</param>
    /// <param name="actual">The actual transformed point</param>
    private static void AssertCoordinatesEqualWithReducedPrecision(Vector2 expected, Point actual)
    {
        double ReducePrecision(double input) => Math.Truncate(input * 10000);
        
        var expectedX = ReducePrecision(expected.X);
        var expectedY = ReducePrecision(expected.Y);
        
        var actualX = ReducePrecision(actual.X);
        var actualY = ReducePrecision(actual.Y);
        
        Assert.Equal(expectedX, actualX);
        Assert.Equal(expectedY, actualY);
    }
    
    [Fact]
    public void Transform_Point_Should_Return_Correct_Value_For_Translated_Matrix()
    {
        var vector2 = Vector2.Transform(
            new Vector2(1, 1), 
            Matrix3x2.CreateTranslation(2, 2));
        var expected = new Point(vector2.X, vector2.Y);
        
        var matrix = Matrix.CreateTranslation(2, 2);
        var point = new Point(1, 1);
        var transformedPoint = matrix.Transform(point);
        
        Assert.Equal(expected, transformedPoint);
    }
    
    [Fact]
    public void Transform_Point_Should_Return_Correct_Value_For_Rotated_Matrix()
    {
        var expected = Vector2.Transform(
            new Vector2(0, 10), 
            Matrix3x2.CreateRotation((float)Matrix.ToRadians(45)));

        var matrix = Matrix.CreateRotation(Matrix.ToRadians(45));
        var point = new Point(0, 10);
        var actual = matrix.Transform(point);

        AssertCoordinatesEqualWithReducedPrecision(expected, actual);
    }
    
    [Fact]
    public void Transform_Point_Should_Return_Correct_Value_For_Scaled_Matrix()
    {
        var vector2 = Vector2.Transform(
            new Vector2(1, 1), 
            Matrix3x2.CreateScale(2, 2));
        var expected = new Point(vector2.X, vector2.Y);
        var matrix = Matrix.CreateScale(2, 2);
        var point = new Point(1, 1);
        var actual = matrix.Transform(point);
        
        Assert.Equal(expected, actual);
    }
    
    [Fact]
    public void Transform_Point_Should_Return_Correct_Value_For_Skewed_Matrix()
    {
        var expected = Vector2.Transform(
            new Vector2(1, 1), 
            Matrix3x2.CreateSkew(30, 20));

        var matrix = Matrix.CreateSkew(30, 20);
        var point = new Point(1, 1);
        var actual = matrix.Transform(point);

        AssertCoordinatesEqualWithReducedPrecision(expected, actual);
    }
}
