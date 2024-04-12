using System;
using System.Collections.Generic;
using Xunit;

namespace Avalonia.Base.UnitTests;

public class PixelSizeTests
{
    [Theory]
    [MemberData(nameof(ParseArguments))]
    public void Parse(string source, PixelSize expected, Exception exception)
    {
        Exception error = null;
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
    public void TryParse(string source, PixelSize? expected, Exception exception)
    {
        Exception error = null;
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

    public static IEnumerable<object[]> ParseArguments()
    {
        yield return new object[]
        {
            "1024,768",
            new PixelSize(1024, 768),
            null,
        };
        yield return new object[]
        {
            "1024x768",
            default(PixelSize),
            new FormatException("Invalid PixelSize."),
        };
    }

    public static IEnumerable<object[]> TryParseArguments()
    {
        yield return new object[]
        {
            "1024,768",
            new PixelSize(1024, 768),
            null,
        };
        yield return new object[]
        {
            "1024x768",
            PixelSize.Empty,
            null,
        };
    }
}
