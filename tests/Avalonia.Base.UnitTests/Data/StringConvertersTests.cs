using System.Globalization;
using Avalonia.Data.Converters;
using Xunit;

namespace Avalonia.Base.UnitTests.Data;

public class StringConvertersTests
{
    [Theory]
    [InlineData("hello", false)]
    [InlineData("", true)]
    [InlineData(null, true)]
    public void StringConverters_IsNullOrEmpty_Works(string? input, bool expected)
    {
        var converter = StringConverters.IsNullOrEmpty;
        var result = converter.Convert(input, typeof(bool), null, CultureInfo.CurrentCulture);
        Assert.Equal(expected, Assert.IsType<bool>(result));
    }

    [Theory]
    [InlineData("hello", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void StringConverters_IsNotNullOrEmpty_Works(string? input, bool expected)
    {
        var converter = StringConverters.IsNotNullOrEmpty;
        var result = converter.Convert(input, typeof(bool), null, CultureInfo.CurrentCulture);
        Assert.Equal(expected, Assert.IsType<bool>(result));
    }

}
