using Avalonia.Data;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

public partial class BindingExpressionTests
{
    [Fact]
    public void OneTime_Binding_Sets_Target_Only_Once_If_Data_Context_Does_Not_Change()
    {
        var data = new ViewModel { Next = new ViewModel { StringValue = "foo" } };
        var target = CreateTarget<ViewModel, string?>(x => x.Next!.StringValue, mode: BindingMode.OneTime);
        target.DataContext = data;

        Assert.Equal("foo", target.String);

        data.Next!.StringValue = "bar";
        Assert.Equal("foo", target.String);

        data.Next = new ViewModel { StringValue = "baz" };
        Assert.Equal("foo", target.String);
    }

    [Fact]
    public void OneTime_Binding_With_Simple_Path_Sets_Target_When_Data_Context_Changes()
    {
        var data1 = new ViewModel { StringValue = "foo" };
        var target = CreateTarget<ViewModel, string?>(x => x.StringValue, mode: BindingMode.OneTime);
        target.DataContext = data1;

        Assert.Equal("foo", target.String);

        var data2 = new ViewModel { StringValue = "bar" };
        target.DataContext = data2;
        Assert.Equal("bar", target.String);
    }

    [Fact]
    public void OneTime_Binding_With_Complex_Path_Sets_Target_When_Data_Context_Changes()
    {
        var data1 = new ViewModel { Next = new ViewModel { StringValue = "foo" } };
        var target = CreateTarget<ViewModel, string?>(x => x.Next!.StringValue, mode: BindingMode.OneTime);
        target.DataContext = data1;

        Assert.Equal("foo", target.String);

        var data2 = new ViewModel { Next = new ViewModel { StringValue = "bar" } };
        target.DataContext = data2;
        Assert.Equal("bar", target.String);
    }

    [Fact]
    public void OneTime_Binding_Without_Path_Sets_Target_When_Data_Context_Changes()
    {
        var target = CreateTarget<string, string?>(x => x, mode: BindingMode.OneTime);
        target.DataContext = "foo";

        Assert.Equal("foo", target.String);

        target.DataContext = "bar";
        Assert.Equal("bar", target.String);
    }

    [Fact]
    public void OneTime_Binding_Waits_For_DataContext()
    {
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

    [Fact]
    public void OneTime_Binding_Waits_For_DataContext_Without_Property_Path()
    {
        var target = CreateTarget<string?, string?>(
            x => x,
            mode: BindingMode.OneTime);

        target.DataContext = "foo";

        Assert.Equal("foo", target.String);
    }

    [Fact]
    public void OneTime_Binding_Waits_For_DataContext_Without_Property_Path_With_StringFormat()
    {
        var target = CreateTarget<string?, string?>(
            x => x,
            mode: BindingMode.OneTime,
            stringFormat: "bar: {0}");

        target.DataContext = "foo";

        Assert.Equal("bar: foo", target.String);
    }

    [Fact]
    public void OneWayToSource_Binding_Updates_Source_When_Target_Changes()
    {
        var data = new ViewModel();
        var target = CreateTarget<ViewModel, string?>(
            x => x.StringValue,
            dataContext: data,
            mode: BindingMode.OneWayToSource);

        Assert.Null(data.StringValue);

        target.String = "foo";

        Assert.Equal("foo", data.StringValue);
    }

    [Fact]
    public void OneWayToSource_Binding_Does_Not_Update_Target_When_Source_Changes()
    {
        var data = new ViewModel();
        var target = CreateTarget<ViewModel, string?>(
            x => x.StringValue,
            dataContext: data,
            mode: BindingMode.OneWayToSource);

        target.String = "foo";
        Assert.Equal("foo", data.StringValue);

        data.StringValue = "bar";
        Assert.Equal("foo", target.String);
    }

    [Fact]
    public void OneWayToSource_Binding_Updates_Source_When_DataContext_Changes()
    {
        var data1 = new ViewModel();
        var data2 = new ViewModel();
        var target = CreateTarget<ViewModel, string?>(
            x => x.StringValue,
            dataContext: data1,
            mode: BindingMode.OneWayToSource);

        target.String = "foo";
        Assert.Equal("foo", data1.StringValue);

        target.DataContext = data2;
        Assert.Equal("foo", data2.StringValue);
    }

    [Fact]
    public void Can_Bind_Readonly_Property_OneWayToSource()
    {
        var data = new ViewModel();
        var target = CreateTarget<ViewModel, string?>(
            x => x.StringValue,
            dataContext: data,
            mode: BindingMode.OneWayToSource,
            targetProperty: TargetClass.ReadOnlyStringProperty);

        Assert.Equal("readonly", data.StringValue);

        target.SetReadOnlyString("foo");

        Assert.Equal("foo", data.StringValue);
    }
}
