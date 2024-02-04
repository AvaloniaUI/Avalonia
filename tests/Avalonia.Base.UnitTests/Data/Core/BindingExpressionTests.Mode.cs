using Avalonia.Data;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

public partial class BindingExpressionTests
{
    [Fact]
    public void OneTime_Binding_Sets_Target_Only_Once()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTargetWithSource(data, x => x.StringValue, mode: BindingMode.OneTime);

        Assert.Equal("foo", target.String);

        data.StringValue = "bar";
        Assert.Equal("foo", target.String);
    }

    [Fact]
    public void OneTime_Binding_Waits_For_DataContext()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTarget<ViewModel, string?>(
            x => x.StringValue,
            mode: BindingMode.OneTime);

        Assert.Null(target.String);
    }

    [Fact]
    public void OneTime_Binding_Waits_For_DataContext_With_Matching_Property_Name()
    {
        var data1 = new { Baz = "baz" };
        var data2 = new ViewModel { StringValue = "foo" };
        var target = CreateTarget<ViewModel, string?>(
            x => x.StringValue,
            dataContext: data1,
            mode: BindingMode.OneTime);

        Assert.Null(target.String);

        target.DataContext = data2;
        Assert.Equal("foo", target.String);

        data2.StringValue = "bar";
        Assert.Equal("foo", target.String);
    }

    [Fact]
    public void OneTime_Binding_Waits_For_DataContext_With_Matching_Property_Type()
    {
        var data1 = new { DoubleValue = new object() };
        var data2 = new ViewModel { DoubleValue = 0.5 };
        var target = CreateTarget<ViewModel, double>(
            x => x.DoubleValue,
            dataContext: data1,
            mode: BindingMode.OneTime);

        Assert.Equal(0, target.Double);

        target.DataContext = data2;
        Assert.Equal(0.5, target.Double);

        data2.DoubleValue = 0.2;
        Assert.Equal(0.5, target.Double);
    }
}
