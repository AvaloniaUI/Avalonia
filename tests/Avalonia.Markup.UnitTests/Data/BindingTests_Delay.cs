using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Markup.UnitTests.Data;

public class BindingTests_Delay : ScopedTestBase, IDisposable
{
    private const int DelayMilliseconds = 10;
    private const string InitialFooValue = "foo";

    private readonly ManualTimerDispatcher _dispatcher;
    private readonly IDisposable _app;
    private readonly BindingTests.Source _source;
    private readonly TextBox _target;
    private readonly Binding _binding;
    private readonly BindingExpressionBase _bindingExpr;
    
    public BindingTests_Delay()
    {
        _dispatcher = new ManualTimerDispatcher();
        _app = UnitTestApplication.Start(new(dispatcherImpl: _dispatcher, keyboardDevice: () => new KeyboardDevice()));

        _source = new BindingTests.Source { Foo = InitialFooValue };
        _target = new TextBox { DataContext = _source };
        _binding = new Binding(nameof(_source.Foo), BindingMode.TwoWay) { Delay = DelayMilliseconds };

        _bindingExpr = _target.Bind(TextBox.TextProperty, _binding);

        Assert.Equal(_source.Foo, _target.Text);
    }

    public override void Dispose()
    {
        _app.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Delayed_Binding_Should_Set_Value_Only_After_Delay_Elapsed()
    {
        _target.Text = "bar";
        Assert.Equal(InitialFooValue, _source.Foo);

        SetTimeAndExecuteTimers(DelayMilliseconds / 2);
        Assert.Equal(InitialFooValue, _source.Foo);

        SetTimeAndExecuteTimers(DelayMilliseconds + 1);

        Assert.Equal("bar", _source.Foo);
    }

    [Fact]
    public void Delayed_Binding_Should_Not_Set_Value_After_Being_Disposed()
    {
        _target.Text = "bar";
        Assert.Equal(InitialFooValue, _source.Foo);

        _bindingExpr.Dispose();

        SetTimeAndExecuteTimers(DelayMilliseconds + 1);

        Assert.Equal(InitialFooValue, _source.Foo);
    }

    [Fact]
    public void Delayed_Binding_Should_Restart_If_Value_Changes_During_Delay()
    {
        _target.Text = "bar";
        Assert.Equal(InitialFooValue, _source.Foo);

        SetTimeAndExecuteTimers(DelayMilliseconds / 2);

        _target.Text = "baz";

        SetTimeAndExecuteTimers(DelayMilliseconds + 1); // we set a new value half-way through the delay, so the delay is still in effect at this timestamp

        Assert.Equal(InitialFooValue, _source.Foo);

        SetTimeAndExecuteTimers(DelayMilliseconds * 2);

        Assert.Equal("baz", _source.Foo);
    }

    [Fact]
    public void Delayed_Binding_Should_Not_Execute_If_Value_Returns_To_Original()
    {
        _target.Text = "bar";
        Assert.Equal(InitialFooValue, _source.Foo);

        SetTimeAndExecuteTimers(DelayMilliseconds / 2);

        _target.Text = InitialFooValue;

        SetTimeAndExecuteTimers(DelayMilliseconds * 2);

        Assert.Equal(InitialFooValue, _source.Foo);
        Assert.Equal(1, _source.FooSetCount);
    }

    [Fact]
    public void Delayed_Binding_UpdateSource_Call_Should_Update_Source_Immediately()
    {
        _target.Text = "bar";
        _bindingExpr.UpdateSource();

        Assert.Equal("bar", _source.Foo);
    }

    [Fact]
    public void Delayed_Binding_UpdateTrigger_LostFocus_Should_Update_Source_Immediately()
    {
        var secondBox = new TextBox();

        new TestRoot() { Child = new Panel() { Children = { _target, secondBox } } };

        _target.Bind(TextBox.TextProperty, new Binding(nameof(_source.Foo), BindingMode.TwoWay) { Delay = DelayMilliseconds, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });

        Assert.True(_target.Focus());
        _target.Text = "bar";

        Assert.Equal(InitialFooValue, _source.Foo);

        Assert.True(secondBox.Focus());
        Assert.Equal("bar", _source.Foo);
    }

    [Fact]
    public void Delayed_Binding_OneWayToSource_DataContext_Change_Should_Update_Source_Immediately()
    {
        _target.Bind(TextBlock.TextProperty, new Binding(nameof(_source.Foo), BindingMode.OneWayToSource) { Delay = DelayMilliseconds });

        _target.Text = "bar";

        var newSource = new BindingTests.Source();

        _target.DataContext = newSource;

        Assert.Equal("bar", newSource.Foo);
    }

    [Fact]
    public void Delayed_Binding_Should_Update_Target_Immediately()
    {
        _source.Foo = "bar";
        Assert.Equal("bar", _target.Text);
    }

    private void SetTimeAndExecuteTimers(long time)
    {
        _dispatcher.Now = time;
        _dispatcher.RaiseTimerEvent();
    }

    private class ManualTimerDispatcher : IDispatcherImpl
    {
        public bool CurrentThreadIsLoopThread => true;
        public long Now { get; set; }

        public event Action? Signaled;
        public event Action? Timer;

        public void Signal() { Signaled?.Invoke(); }

        public void UpdateTimer(long? dueTimeInMs) { }

        public void RaiseTimerEvent() => Timer?.Invoke();
    }
}
