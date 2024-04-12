using System.Globalization;
using Avalonia.Data.Converters;
using Xunit;

namespace Avalonia.Base.UnitTests.Data;

public class ObjectConvertersTests_IsNull
{
    [Fact]
    public void Returns_True_If_Value_Is_Null()
    {
        var result = ObjectConverters.IsNull.Convert(null, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.IsType<bool>(result);
        Assert.True(result is true);
    }

    [Fact]
    public void Returns_False_If_Value_Is_Not_Null()
    {
        var result = ObjectConverters.IsNull.Convert(new object(), typeof(object), null, CultureInfo.InvariantCulture);

        Assert.IsType<bool>(result);
        Assert.True(result is false);
    }
}
