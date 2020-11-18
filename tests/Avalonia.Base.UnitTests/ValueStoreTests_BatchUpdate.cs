using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests
{
    public class ValueStoreTests_BatchUpdate
    {
        ////[Fact]
        ////public void SetValue_Should_Not_Raise_Property_Changes_During_Batch_Update()
        ////{
        ////    var o = new TestClass();
        ////    var target = CreateTarget(o);
        ////    var raised = new List<string>();

        ////    o.GetObservable(TestClass.FooProperty).Skip(1).Subscribe(x => raised.Add(x));
        ////    target.BeginBatchUpdate();
        ////    target.SetValue(TestClass.FooProperty, "foo", BindingPriority.LocalValue);

        ////    Assert.Empty(raised);
        ////}

        ////[Fact]
        ////public void AddBinding_Should_Not_Raise_Property_Changes_During_Batch_Update()
        ////{
        ////    var o = new TestClass();
        ////    var target = CreateTarget(o);
        ////    var observable = new TestObservable<string>("foo");
        ////    var raised = new List<string>();

        ////    o.GetObservable(TestClass.FooProperty).Skip(1).Subscribe(x => raised.Add(x));
        ////    target.BeginBatchUpdate();
        ////    target.AddBinding(TestClass.FooProperty, observable, BindingPriority.LocalValue);

        ////    Assert.Empty(raised);
        ////}

        ////[Fact]
        ////public void SetValue_Change_Should_Be_Raised_After_Batch_Update()
        ////{
        ////    var o = new TestClass();
        ////    var target = CreateTarget(o);
        ////    var raised = new List<AvaloniaPropertyChangedEventArgs>();

        ////    target.SetValue(TestClass.FooProperty, "foo", BindingPriority.LocalValue);
        ////    o.PropertyChanged += (s, e) => raised.Add(e);

        ////    target.BeginBatchUpdate();
        ////    target.SetValue(TestClass.FooProperty, "bar", BindingPriority.LocalValue);
        ////    target.SetValue(TestClass.FooProperty, "baz", BindingPriority.LocalValue);
        ////    target.EndBatchUpdate();

        ////    Assert.Equal(1, raised.Count);
        ////}

        ////[Fact]
        ////public void Bindings_Should_Be_Subscribed_Before_Batch_Update()
        ////{
        ////    var target = CreateTarget();
        ////    var observable1 = new TestObservable<string>("foo");
        ////    var observable2 = new TestObservable<string>("bar");

        ////    target.AddBinding(Window.TitleProperty, observable1, BindingPriority.LocalValue);
        ////    target.AddBinding(Window.TitleProperty, observable2, BindingPriority.LocalValue);

        ////    Assert.Equal(1, observable1.SubscribeCount);
        ////    Assert.Equal(1, observable2.SubscribeCount);
        ////}

        ////[Fact]
        ////public void Non_Active_Binding_Should_Not_Be_Subscribed_Before_Batch_Update()
        ////{
        ////    var target = CreateTarget();
        ////    var observable1 = new TestObservable<string>("foo");
        ////    var observable2 = new TestObservable<string>("bar");

        ////    target.AddBinding(Window.TitleProperty, observable1, BindingPriority.LocalValue);
        ////    target.AddBinding(Window.TitleProperty, observable2, BindingPriority.Style);

        ////    Assert.Equal(1, observable1.SubscribeCount);
        ////    Assert.Equal(0, observable2.SubscribeCount);
        ////}

        ////[Fact]
        ////public void Bindings_Should_Not_Be_Subscribed_During_Batch_Update()
        ////{
        ////    var target = CreateTarget();
        ////    var observable1 = new TestObservable<string>("foo");
        ////    var observable2 = new TestObservable<string>("bar");
        ////    var observable3 = new TestObservable<string>("baz");

        ////    target.BeginBatchUpdate();
        ////    target.AddBinding(Window.TitleProperty, observable1, BindingPriority.LocalValue);
        ////    target.AddBinding(Window.TitleProperty, observable2, BindingPriority.LocalValue);
        ////    target.AddBinding(Window.TitleProperty, observable3, BindingPriority.Style);

        ////    Assert.Equal(0, observable1.SubscribeCount);
        ////    Assert.Equal(0, observable2.SubscribeCount);
        ////    Assert.Equal(0, observable3.SubscribeCount);
        ////}

        ////[Fact]
        ////public void Active_Binding_Should_Be_Subscribed_After_Batch_Uppdate()
        ////{
        ////    var target = CreateTarget();
        ////    var observable1 = new TestObservable<string>("foo");
        ////    var observable2 = new TestObservable<string>("bar");
        ////    var observable3 = new TestObservable<string>("baz");

        ////    target.BeginBatchUpdate();
        ////    target.AddBinding(Window.TitleProperty, observable1, BindingPriority.LocalValue);
        ////    target.AddBinding(Window.TitleProperty, observable2, BindingPriority.LocalValue);
        ////    target.AddBinding(Window.TitleProperty, observable3, BindingPriority.Style);
        ////    target.EndBatchUpdate();

        ////    Assert.Equal(0, observable1.SubscribeCount);
        ////    Assert.Equal(1, observable2.SubscribeCount);
        ////    Assert.Equal(0, observable3.SubscribeCount);
        ////}

        ////private ValueStore CreateTarget(AvaloniaObject? o = null)
        ////{
        ////    o ??= new TestClass();
        ////    return o.Values;
        ////}

        ////public class TestClass : AvaloniaObject
        ////{
        ////    public static readonly StyledProperty<string> FooProperty =
        ////        AvaloniaProperty.Register<TestClass, string>(nameof(Foo));

        ////    public string Foo
        ////    {
        ////        get => GetValue(FooProperty);
        ////        set => SetValue(FooProperty, value);
        ////    }
        ////}

        ////public class TestObservable<T> : ObservableBase<BindingValue<T>>
        ////{
        ////    private readonly T _value;
            
        ////    public TestObservable(T value) => _value = value;

        ////    public int SubscribeCount { get; private set; }

        ////    protected override IDisposable SubscribeCore(IObserver<BindingValue<T>> observer)
        ////    {
        ////        ++SubscribeCount;
        ////        observer.OnNext(_value);
        ////        return Disposable.Empty;
        ////    }
        ////}
    }
}
