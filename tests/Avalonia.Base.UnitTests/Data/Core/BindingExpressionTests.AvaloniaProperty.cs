using System;
using System.Collections.Generic;
using Avalonia.Diagnostics;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

public partial class BindingExpressionTests
{
    [Fact]
    public void Should_Get_Simple_AvaloniaProperty_Value()
    {
        var data = new SourceControl { StringValue = "foo" };
        var target = CreateTargetWithSource(data, o => o.StringValue);

        Assert.Equal("foo", target.String);
    }

    [Fact]
    public void Should_Get_Simple_ClrProperty_Value()
    {
        var data = new SourceControl { ClrProperty = "foo" };
        var target = CreateTargetWithSource(data, o => o.ClrProperty);

        Assert.Equal("foo", target.String);
    }

    [Fact]
    public void Should_Track_Simple_AvaloniaProperty_Value()
    {
        var data = new SourceControl { StringValue = "foo" };
        var target = CreateTargetWithSource(data, o => o.StringValue);
        var result = new List<object>();

        Assert.Equal("foo", target.String);

        data.StringValue = "bar";

        Assert.Equal("bar", target.String);
    }

    [Fact]
    public void Should_Unsubscribe_From_AvaloniaProperty_Source()
    {
        var data = new SourceControl { StringValue = "foo" };
        var (target, expression) = CreateTargetAndExpression<SourceControl, object?>(
            o => o.StringValue,
            source: data,
            targetProperty: TargetClass.StringProperty);

        Assert.NotNull(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());

        expression.Dispose();

        Assert.Null(((IAvaloniaObjectDebug)data).GetPropertyChangedSubscribers());
    }

    [Fact]
    public void Should_Not_Keep_AvaloniaProperty_Source_Alive()
    {
        Func<(TargetClass, WeakReference<SourceControl>)> run = () =>
        {
            var source = new SourceControl();
            var target = CreateTargetWithSource(
                source,
                o => o.StringValue,
                targetProperty: TargetClass.StringProperty);
            return (target, new WeakReference<SourceControl>(source));
        };

        var result = run();

        GC.Collect();

        Assert.False(result.Item2.TryGetTarget(out _));
    }
}
