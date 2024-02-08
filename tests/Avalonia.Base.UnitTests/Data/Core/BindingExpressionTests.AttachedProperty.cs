using System;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

public partial class BindingExpressionTests
{
    [Fact]
    public void Should_Get_Attached_Property_Value()
    {
        var data = new SourceControl { [AttachedProperties.AttachedStringProperty] = "foo" };
        var target = CreateTargetWithSource(
            data,
            o => o[AttachedProperties.AttachedStringProperty],
            targetProperty: TargetClass.StringProperty);

        Assert.Equal("foo", target.String);
    }

    [Fact]
    public void Should_Get_Chained_Attached_Property_Value()
    {
        var data = new SourceControl 
        { 
            Next = new() { [AttachedProperties.AttachedStringProperty] = "foo" }
        };

        var target = CreateTargetWithSource(
            data,
            o => o.Next![AttachedProperties.AttachedStringProperty],
            targetProperty: TargetClass.StringProperty);

        Assert.Equal("foo", target.String);
    }

    [Fact]
    public void Should_Track_Simple_Attached_Value()
    {
        var data = new SourceControl { [AttachedProperties.AttachedStringProperty] = "foo" };
        var target = CreateTargetWithSource(
            data,
            o => o[AttachedProperties.AttachedStringProperty],
            targetProperty: TargetClass.StringProperty);

        Assert.Equal("foo", target.String);

        data.SetValue(AttachedProperties.AttachedStringProperty, "bar");

        Assert.Equal("bar", target.String);
    }

    [Fact]
    public void Should_Track_Chained_Attached_Value()
    {
        var data = new SourceControl
        {
            Next = new() { [AttachedProperties.AttachedStringProperty] = "foo" }
        };

        var target = CreateTargetWithSource(
            data,
            o => o.Next![AttachedProperties.AttachedStringProperty],
            targetProperty: TargetClass.StringProperty);

        Assert.Equal("foo", target.String);

        data.Next!.SetValue(AttachedProperties.AttachedStringProperty, "bar");

        Assert.Equal("bar", target.String);
    }

    [Fact]
    public void Should_Unsubscribe_From_AttachedProperty_Source()
    {
        var data = new SourceControl { [AttachedProperties.AttachedStringProperty] = "foo" };
        var (target, expression) = CreateTargetAndExpression<SourceControl, object?>(
            o => o[AttachedProperties.AttachedStringProperty],
            source: data,
            targetProperty: TargetClass.StringProperty);

        Assert.NotNull(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
        
        expression.Dispose();

        Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
    }

    [Fact]
    public void Should_Unsubscribe_From_Chained_Source()
    {
        var data = new SourceControl
        {
            Next = new() { [AttachedProperties.AttachedStringProperty] = "foo" }
        };

        var (target, expression) = CreateTargetAndExpression<SourceControl, object?>(
            o => o.Next![AttachedProperties.AttachedStringProperty],
            source: data,
            targetProperty: TargetClass.StringProperty);

        Assert.NotNull(((IAvaloniaObjectDebug)data.Next).GetPropertyChangedSubscribers());

        expression.Dispose();

        Assert.Null(((IAvaloniaObjectDebug)data.Next).GetPropertyChangedSubscribers());
    }

    [Fact]
    public void Should_Not_Keep_Attached_Property_Source_Alive()
    {
        Func<(TargetClass, WeakReference<SourceControl>)> run = () =>
        {
            var source = new SourceControl();
            var target = CreateTargetWithSource(
                source,
                o => o.Next![AttachedProperties.AttachedStringProperty],
                targetProperty: TargetClass.StringProperty);
            return (target, new WeakReference<SourceControl>(source));
        };

        var result = run();

        GC.Collect();

        Assert.False(result.Item2.TryGetTarget(out _));
        GC.KeepAlive(result.Item1);
    }
}
