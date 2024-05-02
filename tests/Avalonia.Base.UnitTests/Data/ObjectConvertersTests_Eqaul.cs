using System.Globalization;
using Avalonia.Data.Converters;
using Xunit;

namespace Avalonia.Base.UnitTests.Data;

public class ObjectConvertersTests_Equal
{
    [Fact]
    public void Returns_True_If_Value_And_Parameter_Are_Null()
    {
        var result = ObjectConverters.Equal.Convert(null, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.IsType<bool>(result);
        Assert.True(result is true);
    }

    [Fact]
    public void Returns_False_If_Value_Is_Null_And_Parameter_Is_Not_Null()
    {
        var result = ObjectConverters.Equal.Convert(null, typeof(object), new object(), CultureInfo.InvariantCulture);

        Assert.IsType<bool>(result);
        Assert.True(result is false);
    }

    [Fact]
    public void Returns_False_If_Value_And_Parameter_Are_Different_Objects()
    {
        var result = ObjectConverters.Equal.Convert(new object(), typeof(object), new object(), CultureInfo.InvariantCulture);

        Assert.IsType<bool>(result);
        Assert.True(result is false);
    }

    [Fact]
    public void Returns_True_If_Value_And_Parameter_Are_Same_Object()
    {
        var target = new object();
        var result = ObjectConverters.Equal.Convert(target, typeof(object), target, CultureInfo.InvariantCulture);

        Assert.IsType<bool>(result);
        Assert.True(result is true);
    }
}
