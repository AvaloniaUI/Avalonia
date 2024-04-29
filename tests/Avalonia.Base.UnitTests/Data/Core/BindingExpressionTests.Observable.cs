using System;
using System.Reactive.Subjects;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core;

public partial class BindingExpressionTests
{
    [Fact]
    public void Should_Not_Get_Observable_Value_Without_Streaming()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var source = new BehaviorSubject<string>("foo");
        var data = new { Foo = source };
        var target = CreateTargetWithSource(data, o => o.Foo);
        
        Assert.Same(source, target.Object);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Get_Simple_Observable_Value()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var source = new BehaviorSubject<string>("foo");
        var data = new { Foo = source };
        var target = CreateTargetWithSource(data, o => o.Foo.StreamBinding());

        Assert.Equal("foo", target.String);

        source.OnNext("bar");

        Assert.Equal("bar", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Get_Property_Value_From_Observable()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var source = new BehaviorSubject<ViewModel>(new() { StringValue = "foo" });
        var data = new ViewModel { NextObservable = source };
        var target = CreateTargetWithSource(data, o => o.NextObservable.StreamBinding().StringValue);

        Assert.Equal("foo", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Get_Simple_Observable_Value_With_DataValidation_Enabled()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var source = new BehaviorSubject<string>("foo");
        var data = new { Foo = source };
        var target = CreateTargetWithSource(
            data,
            o => o.Foo.StreamBinding(),
            enableDataValidation: true);

        Assert.Equal("foo", target.String);

        source.OnNext("bar");

        Assert.Equal("bar", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Work_With_Value_Type()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var source = new BehaviorSubject<int>(1);
        var data = new { Foo = source };
        var target = CreateTargetWithSource(data, o => o.Foo.StreamBinding());

        Assert.Equal(1, target.Int);

        source.OnNext(42);

        Assert.Equal(42, target.Int);

        GC.KeepAlive(data);
    }
}
