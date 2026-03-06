using System.Globalization;
using Avalonia.Data.Converters;
using Xunit;

namespace Avalonia.Base.UnitTests.Data;

public class BoolConvertersTests
{
    [Fact]
    public void BoolConverters_Not_Works_TwoWay()
    {
        var converter = BoolConverters.Not;
        var result = converter.Convert(true, typeof(bool), null, CultureInfo.CurrentCulture);
        Assert.False(Assert.IsType<bool>(result));

        result = converter.ConvertBack(false, typeof(bool), null, CultureInfo.CurrentCulture);
        Assert.True(Assert.IsType<bool>(result));
    }

    [Fact]
    public void BoolConverters_Not_Returns_Unset_On_Invalid_Input()
    {
        var converter = BoolConverters.Not;
        var result = converter.Convert(1234, typeof(bool), null, CultureInfo.CurrentCulture);
        Assert.Equal(AvaloniaProperty.UnsetValue, result);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void BoolConverters_And_Works(bool a, bool b, bool y)
    {
        var converter = BoolConverters.And;
        var result = converter.Convert([a, b], typeof(bool), null, CultureInfo.CurrentCulture);
        Assert.Equal(y, Assert.IsType<bool>(result));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void BoolConverters_Or_Works(bool a, bool b, bool y)
    {
        var converter = BoolConverters.Or;
        var result = converter.Convert([a, b], typeof(bool), null, CultureInfo.CurrentCulture);
        Assert.Equal(y, Assert.IsType<bool>(result));
    }
}
