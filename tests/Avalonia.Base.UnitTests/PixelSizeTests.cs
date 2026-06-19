using System;
using System.Collections.Generic;
using Xunit;

namespace Avalonia.Base.UnitTests;

public class PixelSizeTests
{
    [Theory]
    [MemberData(nameof(ParseArguments))]
    public void Parse(string source, PixelSize expected, Exception? exception)
    {
        Exception? error = null;
        PixelSize result = default;
        try
        {
            result = PixelSize.Parse(source);
        }
        catch (Exception ex)
        {
            error = ex;
        }
        Assert.Equal(exception?.Message, error?.Message);
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(TryParseArguments))]
    public void TryParse(string source, PixelSize? expected, Exception? exception)
    {
        Exception? error = null;
        PixelSize result = PixelSize.Empty;
        try
        {

            PixelSize.TryParse(source, out result);
        }
        catch (Exception ex)
        {
            error = ex;
        }
        Assert.Equal(exception?.Message, error?.Message);
        Assert.Equal(expected, result);
    }

    public static IEnumerable<object?[]> ParseArguments()
    {
        yield return
        [
            "1024,768",
            new PixelSize(1024, 768),
            null
        ];
        yield return
        [
            "1024x768",
            default(PixelSize),
            new FormatException("Invalid PixelSize.")
        ];
    }

    public static IEnumerable<object?[]> TryParseArguments()
    {
        yield return
        [
            "1024,768",
            new PixelSize(1024, 768),
            null
        ];
        yield return
        [
            "1024x768",
            PixelSize.Empty,
            null
        ];
    }

    [Theory]
    [InlineData(10, 1.0, 10)]
    [InlineData(10, 1.25, 13)]
    [InlineData(10, 1.5, 15)]
    [InlineData(10, 1.75, 18)]
    [InlineData(10, 2.0, 20)]
    [InlineData(10, 1.125, 12)]
    [InlineData(8, 1.5, 12)]
    [InlineData(0, 1.5, 0)]
    [InlineData(1, 2.5, 3)]
    public void FromSizeCeiling_Computes_Expected_Pixels(int logical, double scale, int expected)
    {
        var pixel = PixelSize.FromSizeCeiling(new Size(logical, logical), scale);
        Assert.Equal(expected, pixel.Width);
        Assert.Equal(expected, pixel.Height);
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(2.0)]
    [InlineData(3.0)]
    public void FromSizeCeiling_Snaps_When_Within_Epsilon(double scale)
    {
        // Pick a logical size where logical * scale is an exact integer; perturbing it by a tiny
        // amount in either direction must still produce that integer (no spurious +1 from ceiling).
        const int logical = 10;
        var exact = logical * scale;
        var below = exact - 1e-9;
        var above = exact + 1e-9;
        var roundedBelow = below / scale;
        var roundedAbove = above / scale;

        var p1 = PixelSize.FromSizeCeiling(new Size(roundedBelow, roundedBelow), scale);
        var p2 = PixelSize.FromSizeCeiling(new Size(roundedAbove, roundedAbove), scale);
        Assert.Equal((int)exact, p1.Width);
        Assert.Equal((int)exact, p2.Width);
    }
}
