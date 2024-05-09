using System;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

public partial class BindingExpressionTests
{
    [Fact]
    public void Should_Not_Get_Task_Result_Without_StreamBinding()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var tcs = new TaskCompletionSource<string>();
        var data = new { Foo = tcs.Task };
        var target = CreateTargetWithSource(data, o => o.Foo);

        Assert.Null(target.String);

        tcs.SetResult("foo");
        sync.ExecutePostedCallbacks();

        Assert.Null(target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Get_Completed_Task_Value()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var data = new { Foo = Task.FromResult("foo") };
        var target = CreateTargetWithSource(data, o => o.Foo.StreamBinding());

        Assert.Equal("foo", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Get_Property_Value_From_Task()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var tcs = new TaskCompletionSource<ViewModel>();
        var data = new ViewModel { NextTask = tcs.Task };
        var target = CreateTargetWithSource(data, o => o.NextTask.StreamBinding().StringValue);

        tcs.SetResult(new ViewModel { StringValue = "foo" });
        sync.ExecutePostedCallbacks();

        Assert.Equal("foo", target.String);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Update_Data_Validation_On_Task_Exception()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var tcs = new TaskCompletionSource<string>();
        var data = new { Foo = tcs.Task };
        var target = CreateTargetWithSource(
            data,
            o => o.Foo.StreamBinding(),
            enableDataValidation: true);

        tcs.SetException(new NotSupportedException());
        sync.ExecutePostedCallbacks();

        AssertBindingError(
            target,
            TargetClass.StringProperty,
            new BindingChainException("Specified method is not supported.", "Foo^", "^"),
            BindingErrorType.Error);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Update_Data_Validation_On_Faulted_Task()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var data = new { Foo = TaskFromException(new NotSupportedException()) };
        var target = CreateTargetWithSource(
            data,
            o => o.Foo.StreamBinding(),
            enableDataValidation: true);

        AssertBindingError(
            target,
            TargetClass.StringProperty,
            new BindingChainException("Specified method is not supported.", "Foo^", "^"),
            BindingErrorType.Error);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Should_Get_Simple_Task_Value_With_Data_DataValidation_Enabled()
    {
        using var sync = UnitTestSynchronizationContext.Begin();
        var tcs = new TaskCompletionSource<string>();
        var data = new { Foo = tcs.Task };
        var target = CreateTargetWithSource(
            data,
            o => o.Foo.StreamBinding(),
            enableDataValidation: true);

        tcs.SetResult("foo");
        sync.ExecutePostedCallbacks();

        // What does it mean to have data validation on a Task? Without a use-case it's
        // hard to know what to do here so for the moment the value is returned.
        Assert.Equal("foo", target.String);

        GC.KeepAlive(data);
    }

    private static Task<string> TaskFromException(Exception e)
    {
        var tcs = new TaskCompletionSource<string>();
        tcs.SetException(e);
        return tcs.Task;
    }
}
