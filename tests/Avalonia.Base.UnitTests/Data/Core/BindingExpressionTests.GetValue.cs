using System;
using Avalonia.Data;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

public abstract partial class BindingExpressionTests
{
    [Fact]
    public void Should_Get_Source_Value()
    {
        var data = "foo";
        var target = CreateTargetWithSource(data, o => o);

        Assert.Equal("foo", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Convert_String_To_Double()
    {
        var data = new ViewModel { StringValue = $"{5.6}" };
        var target = CreateTargetWithSource(
            data, 
            o => o.StringValue,
            targetProperty: TargetClass.DoubleProperty);

        Assert.Equal(5.6, target.Double);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Convert_Double_To_String()
    {
        var data = new ViewModel { DoubleValue = 5.6 };
        var target = CreateTargetWithSource(
            data,
            o => o.DoubleValue,
            targetProperty: TargetClass.StringProperty);

        Assert.Equal($"{5.6}", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Use_FallbackValue_For_NonConvertible_Target_Value()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTargetWithSource(
            data,
            o => o.StringValue,
            fallbackValue: 42,
            targetProperty: TargetClass.IntProperty);

        Assert.Equal(42, target.Int);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Pass_ConverterParameter_To_Converter()
    {
        var data = new ViewModel { DoubleValue = 5.6 };
        var converter = new PrefixConverter();
        var target = CreateTargetWithSource(
            data,
            o => o.DoubleValue,         
            converter: converter,
            converterParameter: "foo",
            targetProperty: TargetClass.StringProperty);

        Assert.Equal("foo5.6", target.String);
    }

    [Fact]
    public void TargetNullValue_Should_Be_Used_When_Source_String_Is_Null()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTargetWithSource(
            data, 
            o => o.StringValue,
            targetNullValue: "bar");

        Assert.Equal("foo", target.String);

        data.StringValue = null;
        Assert.Equal("bar", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Can_Use_UpdateTarget_To_Update_From_Non_INPC_Data()
    {
        var data = new PodViewModel { StringValue = "foo" };
        var (target, expression) = CreateTargetAndExpression<PodViewModel, string?>(
            o => o.StringValue,
            source: data);

        Assert.Equal("foo", target.String);

        data.StringValue = "bar";
        Assert.Equal("foo", target.String);

        expression.UpdateTarget();
        Assert.Equal("bar", target.String);
    }

    [Fact]
    public void Should_Use_Converter_For_RelativeSource_Self_Binding_With_No_Path()
    {
        var converter = new PrefixConverter();
        var target = CreateTarget<TargetClass, TargetClass>(
            o => o,
            converter: converter,
            converterParameter: "foo",
            relativeSource: new RelativeSource(RelativeSourceMode.Self),
            targetProperty: TargetClass.StringProperty);

        Assert.Equal("fooTargetClass", target.String);
    }

    [Fact]
    public void Should_Not_Pass_UnsetValue_To_Converter_Until_First_Value_Produced()
    {
        var data = new ViewModel { StringValue = "Bar" };
        var converter = new PrefixConverter();
        var target = CreateTarget<ViewModel, string?>(
            o => o.StringValue,
            converter: converter,
            converterParameter: "foo");

        Assert.Null(target.String);

        target.DataContext = data;

        Assert.Equal("fooBar", target.String);
    }
}
