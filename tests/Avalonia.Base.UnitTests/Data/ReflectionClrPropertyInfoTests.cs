using Avalonia.Data.Core;
using Xunit;

namespace Avalonia.Base.UnitTests.Data;

public class ReflectionClrPropertyInfoTests
{
    public class TestClass
    {
        public string Test { get; set; }
    }

    [Fact]
    public void Can_Compile()
    {
        var propertyInfo = new ReflectionClrPropertyInfo(
            typeof(TestClass).GetProperty(nameof(TestClass.Test))!);
        var target = new TestClass();
        const string result = "qwerty";
        propertyInfo.Set(target, result);
        Assert.Equal(result, target.Test);
        Assert.Equal(result, (string)propertyInfo.Get(target));
    }
}
