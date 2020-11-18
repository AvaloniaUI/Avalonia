using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Data;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_BatchUpdate
    {
        [Fact]
        public void SetValue_Should_Not_Raise_Property_Changes_During_Batch_Update()
        {
            var target = new TestClass();
            var raised = new List<string>();

            target.GetObservable(TestClass.FooProperty).Skip(1).Subscribe(x => raised.Add(x));
            target.BeginBatchUpdate();
            target.SetValue(TestClass.FooProperty, "foo", BindingPriority.LocalValue);

            Assert.Empty(raised);
        }

        [Fact]
        public void Binding_Should_Not_Raise_Property_Changes_During_Batch_Update()
        {
            var target = new TestClass();
            var observable = new TestObservable<string>("foo");
            var raised = new List<string>();

            target.GetObservable(TestClass.FooProperty).Skip(1).Subscribe(x => raised.Add(x));
            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable, BindingPriority.LocalValue);

            Assert.Empty(raised);
        }

        [Fact]
        public void SetValue_Change_Should_Be_Raised_After_Batch_Update_1()
        {
            var target = new TestClass();
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.SetValue(TestClass.FooProperty, "foo", BindingPriority.LocalValue);
            target.EndBatchUpdate();

            Assert.Equal(1, raised.Count);
            Assert.Equal("foo", target.Foo);
            Assert.Null(raised[0].OldValue);
            Assert.Equal("foo", raised[0].NewValue);
        }

        [Fact]
        public void SetValue_Change_Should_Be_Raised_After_Batch_Update_2()
        {
            var target = new TestClass();
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.SetValue(TestClass.FooProperty, "foo", BindingPriority.LocalValue);
            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.SetValue(TestClass.FooProperty, "bar", BindingPriority.LocalValue);
            target.SetValue(TestClass.FooProperty, "baz", BindingPriority.LocalValue);
            target.EndBatchUpdate();

            Assert.Equal(1, raised.Count);
            Assert.Equal("baz", target.Foo);
        }

        [Fact]
        public void SetValue_Changes_Should_Be_Raised_In_Correct_Order_After_Batch_Update()
        {
            var target = new TestClass();
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.SetValue(TestClass.FooProperty, "foo", BindingPriority.LocalValue);
            target.SetValue(TestClass.BarProperty, "bar", BindingPriority.LocalValue);
            target.SetValue(TestClass.FooProperty, "baz", BindingPriority.LocalValue);
            target.EndBatchUpdate();

            Assert.Equal(2, raised.Count);
            Assert.Equal(TestClass.BarProperty, raised[0].Property);
            Assert.Equal(TestClass.FooProperty, raised[1].Property);
            Assert.Equal("baz", target.Foo);
            Assert.Equal("bar", target.Bar);
        }

        [Fact]
        public void SetValue_And_Binding_Changes_Should_Be_Raised_In_Correct_Order_After_Batch_Update_1()
        {
            var target = new TestClass();
            var observable = new TestObservable<string>("baz");
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.SetValue(TestClass.FooProperty, "foo", BindingPriority.LocalValue);
            target.SetValue(TestClass.BarProperty, "bar", BindingPriority.LocalValue);
            target.Bind(TestClass.FooProperty, observable, BindingPriority.LocalValue);
            target.EndBatchUpdate();

            Assert.Equal(2, raised.Count);
            Assert.Equal(TestClass.BarProperty, raised[0].Property);
            Assert.Equal(TestClass.FooProperty, raised[1].Property);
            Assert.Equal("baz", target.Foo);
            Assert.Equal("bar", target.Bar);
        }

        [Fact]
        public void SetValue_And_Binding_Changes_Should_Be_Raised_In_Correct_Order_After_Batch_Update_2()
        {
            var target = new TestClass();
            var observable = new TestObservable<string>("foo");
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable, BindingPriority.LocalValue);
            target.SetValue(TestClass.BarProperty, "bar", BindingPriority.LocalValue);
            target.SetValue(TestClass.FooProperty, "baz", BindingPriority.LocalValue);
            target.EndBatchUpdate();

            Assert.Equal(2, raised.Count);
            Assert.Equal(TestClass.BarProperty, raised[0].Property);
            Assert.Equal(TestClass.FooProperty, raised[1].Property);
            Assert.Equal("baz", target.Foo);
            Assert.Equal("bar", target.Bar);
        }

        [Fact]
        public void SetValue_And_Binding_Changes_Should_Be_Raised_In_Correct_Order_After_Batch_Update_3()
        {
            var target = new TestClass();
            var observable1 = new TestObservable<string>("foo");
            var observable2 = new TestObservable<string>("qux");
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable2, BindingPriority.LocalValue);
            target.Bind(TestClass.FooProperty, observable1, BindingPriority.LocalValue);
            target.SetValue(TestClass.BarProperty, "bar", BindingPriority.LocalValue);
            target.SetValue(TestClass.FooProperty, "baz", BindingPriority.LocalValue);
            target.EndBatchUpdate();

            Assert.Equal(2, raised.Count);
            Assert.Equal(TestClass.BarProperty, raised[0].Property);
            Assert.Equal(TestClass.FooProperty, raised[1].Property);
            Assert.Equal("baz", target.Foo);
            Assert.Equal("bar", target.Bar);
        }

        [Fact]
        public void Binding_Change_Should_Be_Raised_After_Batch_Update_1()
        {
            var target = new TestClass();
            var observable = new TestObservable<string>("foo");
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable, BindingPriority.LocalValue);
            target.EndBatchUpdate();

            Assert.Equal(1, raised.Count);
            Assert.Equal("foo", target.Foo);
            Assert.Null(raised[0].OldValue);
            Assert.Equal("foo", raised[0].NewValue);
        }

        [Fact]
        public void Binding_Change_Should_Be_Raised_After_Batch_Update_2()
        {
            var target = new TestClass();
            var observable1 = new TestObservable<string>("bar");
            var observable2 = new TestObservable<string>("baz");
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.SetValue(TestClass.FooProperty, "foo", BindingPriority.LocalValue);
            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable1, BindingPriority.LocalValue);
            target.Bind(TestClass.FooProperty, observable2, BindingPriority.LocalValue);
            target.EndBatchUpdate();

            Assert.Equal(1, raised.Count);
            Assert.Equal("baz", target.Foo);
            Assert.Equal("foo", raised[0].OldValue);
            Assert.Equal("baz", raised[0].NewValue);
        }

        [Fact]
        public void Bindings_Should_Be_Subscribed_Before_Batch_Update()
        {
            var target = new TestClass();
            var observable1 = new TestObservable<string>("foo");
            var observable2 = new TestObservable<string>("bar");

            target.Bind(TestClass.FooProperty, observable1, BindingPriority.LocalValue);
            target.Bind(TestClass.FooProperty, observable2, BindingPriority.LocalValue);

            Assert.Equal(1, observable1.SubscribeCount);
            Assert.Equal(1, observable2.SubscribeCount);
        }

        [Fact]
        public void Non_Active_Binding_Should_Not_Be_Subscribed_Before_Batch_Update()
        {
            var target = new TestClass();
            var observable1 = new TestObservable<string>("foo");
            var observable2 = new TestObservable<string>("bar");

            target.Bind(TestClass.FooProperty, observable1, BindingPriority.LocalValue);
            target.Bind(TestClass.FooProperty, observable2, BindingPriority.Style);

            Assert.Equal(1, observable1.SubscribeCount);
            Assert.Equal(0, observable2.SubscribeCount);
        }

        [Fact]
        public void Bindings_Should_Not_Be_Subscribed_During_Batch_Update()
        {
            var target = new TestClass();
            var observable1 = new TestObservable<string>("foo");
            var observable2 = new TestObservable<string>("bar");
            var observable3 = new TestObservable<string>("baz");

            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable1, BindingPriority.LocalValue);
            target.Bind(TestClass.FooProperty, observable2, BindingPriority.LocalValue);
            target.Bind(TestClass.FooProperty, observable3, BindingPriority.Style);

            Assert.Equal(0, observable1.SubscribeCount);
            Assert.Equal(0, observable2.SubscribeCount);
            Assert.Equal(0, observable3.SubscribeCount);
        }

        [Fact]
        public void Active_Binding_Should_Be_Subscribed_After_Batch_Uppdate()
        {
            var target = new TestClass();
            var observable1 = new TestObservable<string>("foo");
            var observable2 = new TestObservable<string>("bar");
            var observable3 = new TestObservable<string>("baz");

            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable1, BindingPriority.LocalValue);
            target.Bind(TestClass.FooProperty, observable2, BindingPriority.LocalValue);
            target.Bind(TestClass.FooProperty, observable3, BindingPriority.Style);
            target.EndBatchUpdate();

            Assert.Equal(0, observable1.SubscribeCount);
            Assert.Equal(1, observable2.SubscribeCount);
            Assert.Equal(0, observable3.SubscribeCount);
        }

        public class TestClass : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<TestClass, string>(nameof(Foo));

            public static readonly StyledProperty<string> BarProperty =
                AvaloniaProperty.Register<TestClass, string>(nameof(Bar));

            public string Foo
            {
                get => GetValue(FooProperty);
                set => SetValue(FooProperty, value);
            }

            public string Bar
            {
                get => GetValue(BarProperty);
                set => SetValue(BarProperty, value);
            }
        }

        public class TestObservable<T> : ObservableBase<BindingValue<T>>
        {
            private readonly T _value;

            public TestObservable(T value) => _value = value;

            public int SubscribeCount { get; private set; }

            protected override IDisposable SubscribeCore(IObserver<BindingValue<T>> observer)
            {
                ++SubscribeCount;
                observer.OnNext(_value);
                return Disposable.Empty;
            }
        }
    }
}
