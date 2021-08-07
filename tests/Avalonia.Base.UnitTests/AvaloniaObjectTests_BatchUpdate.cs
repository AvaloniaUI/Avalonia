using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Data;
using Avalonia.Layout;
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
        public void Binding_Completion_Should_Not_Raise_Property_Changes_During_Batch_Update()
        {
            var target = new TestClass();
            var observable = new TestObservable<string>("foo");
            var raised = new List<string>();

            target.Bind(TestClass.FooProperty, observable, BindingPriority.LocalValue);
            target.GetObservable(TestClass.FooProperty).Skip(1).Subscribe(x => raised.Add(x));
            target.BeginBatchUpdate();
            observable.OnCompleted();

            Assert.Empty(raised);
        }

        [Fact]
        public void Binding_Disposal_Should_Not_Raise_Property_Changes_During_Batch_Update()
        {
            var target = new TestClass();
            var observable = new TestObservable<string>("foo");
            var raised = new List<string>();

            var sub = target.Bind(TestClass.FooProperty, observable, BindingPriority.LocalValue);
            target.GetObservable(TestClass.FooProperty).Skip(1).Subscribe(x => raised.Add(x));
            target.BeginBatchUpdate();
            sub.Dispose();

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
        public void SetValue_Change_Should_Be_Raised_After_Batch_Update_3()
        {
            var target = new TestClass();
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.SetValue(TestClass.BazProperty, Orientation.Horizontal, BindingPriority.LocalValue);
            target.EndBatchUpdate();

            Assert.Equal(1, raised.Count);
            Assert.Equal(TestClass.BazProperty, raised[0].Property);
            Assert.Equal(Orientation.Vertical, raised[0].OldValue);
            Assert.Equal(Orientation.Horizontal, raised[0].NewValue);
            Assert.Equal(Orientation.Horizontal, target.Baz);
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
        public void Binding_Change_Should_Be_Raised_After_Batch_Update_3()
        {
            var target = new TestClass();
            var observable = new TestObservable<Orientation>(Orientation.Horizontal);
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.Bind(TestClass.BazProperty, observable, BindingPriority.LocalValue);
            target.EndBatchUpdate();

            Assert.Equal(1, raised.Count);
            Assert.Equal(TestClass.BazProperty, raised[0].Property);
            Assert.Equal(Orientation.Vertical, raised[0].OldValue);
            Assert.Equal(Orientation.Horizontal, raised[0].NewValue);
            Assert.Equal(Orientation.Horizontal, target.Baz);
        }

        [Fact]
        public void Binding_Completion_Should_Be_Raised_After_Batch_Update()
        {
            var target = new TestClass();
            var observable = new TestObservable<string>("foo");
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.Bind(TestClass.FooProperty, observable, BindingPriority.LocalValue);
            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            observable.OnCompleted();
            target.EndBatchUpdate();

            Assert.Equal(1, raised.Count);
            Assert.Null(target.Foo);
            Assert.Equal("foo", raised[0].OldValue);
            Assert.Null(raised[0].NewValue);
            Assert.Equal(BindingPriority.Unset, raised[0].Priority);
        }

        [Fact]
        public void Binding_Disposal_Should_Be_Raised_After_Batch_Update()
        {
            var target = new TestClass();
            var observable = new TestObservable<string>("foo");
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            var sub = target.Bind(TestClass.FooProperty, observable, BindingPriority.LocalValue);
            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            sub.Dispose();
            target.EndBatchUpdate();

            Assert.Equal(1, raised.Count);
            Assert.Null(target.Foo);
            Assert.Equal("foo", raised[0].OldValue);
            Assert.Null(raised[0].NewValue);
            Assert.Equal(BindingPriority.Unset, raised[0].Priority);
        }

        [Fact]
        public void ClearValue_Change_Should_Be_Raised_After_Batch_Update_1()
        {
            var target = new TestClass();
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.Foo = "foo";
            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.ClearValue(TestClass.FooProperty);
            target.EndBatchUpdate();

            Assert.Equal(1, raised.Count);
            Assert.Null(target.Foo);
            Assert.Equal("foo", raised[0].OldValue);
            Assert.Null(raised[0].NewValue);
            Assert.Equal(BindingPriority.Unset, raised[0].Priority);
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
        public void LocalValue_Bindings_Should_Be_Subscribed_During_Batch_Update()
        {
            var target = new TestClass();
            var observable1 = new TestObservable<string>("foo");
            var observable2 = new TestObservable<string>("bar");
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            // We need to subscribe to LocalValue bindings even if we've got a batch operation
            // in progress because otherwise we don't know whether the binding or a subsequent
            // SetValue with local priority will win. Notifications however shouldn't be sent.
            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable1, BindingPriority.LocalValue);
            target.Bind(TestClass.FooProperty, observable2, BindingPriority.LocalValue);

            Assert.Equal(1, observable1.SubscribeCount);
            Assert.Equal(1, observable2.SubscribeCount);
            Assert.Empty(raised);
        }

        [Fact]
        public void Style_Bindings_Should_Not_Be_Subscribed_During_Batch_Update()
        {
            var target = new TestClass();
            var observable1 = new TestObservable<string>("foo");
            var observable2 = new TestObservable<string>("bar");

            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable1, BindingPriority.Style);
            target.Bind(TestClass.FooProperty, observable2, BindingPriority.StyleTrigger);

            Assert.Equal(0, observable1.SubscribeCount);
            Assert.Equal(0, observable2.SubscribeCount);
        }

        [Fact]
        public void Active_Style_Binding_Should_Be_Subscribed_After_Batch_Uppdate_1()
        {
            var target = new TestClass();
            var observable1 = new TestObservable<string>("foo");
            var observable2 = new TestObservable<string>("bar");

            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable1, BindingPriority.Style);
            target.Bind(TestClass.FooProperty, observable2, BindingPriority.Style);
            target.EndBatchUpdate();

            Assert.Equal(0, observable1.SubscribeCount);
            Assert.Equal(1, observable2.SubscribeCount);
        }

        [Fact]
        public void Active_Style_Binding_Should_Be_Subscribed_After_Batch_Uppdate_2()
        {
            var target = new TestClass();
            var observable1 = new TestObservable<string>("foo");
            var observable2 = new TestObservable<string>("bar");

            target.BeginBatchUpdate();
            target.Bind(TestClass.FooProperty, observable1, BindingPriority.StyleTrigger);
            target.Bind(TestClass.FooProperty, observable2, BindingPriority.Style);
            target.EndBatchUpdate();

            Assert.Equal(1, observable1.SubscribeCount);
            Assert.Equal(0, observable2.SubscribeCount);
        }

        [Fact]
        public void Change_Can_Be_Triggered_By_Ending_Batch_Update_1()
        {
            var target = new TestClass();
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.Foo = "foo";

            target.PropertyChanged += (s, e) =>
            {
                if (e.Property == TestClass.FooProperty && (string)e.NewValue == "foo")
                    target.Bar = "bar";
            };

            target.EndBatchUpdate();

            Assert.Equal("foo", target.Foo);
            Assert.Equal("bar", target.Bar);
            Assert.Equal(2, raised.Count);
            Assert.Equal(TestClass.FooProperty, raised[0].Property);
            Assert.Equal(TestClass.BarProperty, raised[1].Property);
        }

        [Fact]
        public void Change_Can_Be_Triggered_By_Ending_Batch_Update_2()
        {
            var target = new TestClass();
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.Foo = "foo";
            target.Bar = "baz";

            target.PropertyChanged += (s, e) =>
            {
                if (e.Property == TestClass.FooProperty && (string)e.NewValue == "foo")
                    target.Bar = "bar";
            };

            target.EndBatchUpdate();

            Assert.Equal("foo", target.Foo);
            Assert.Equal("bar", target.Bar);
            Assert.Equal(2, raised.Count);
        }

        [Fact]
        public void Batch_Update_Can_Be_Triggered_By_Ending_Batch_Update()
        {
            var target = new TestClass();
            var raised = new List<AvaloniaPropertyChangedEventArgs>();

            target.PropertyChanged += (s, e) => raised.Add(e);

            target.BeginBatchUpdate();
            target.Foo = "foo";
            target.Bar = "baz";

            // Simulates the following scenario:
            // - A control is added to the logical tree
            // - A batch update is started to apply styles
            // - Ending the batch update triggers something which removes the control from the logical tree
            // - A new batch update is started to detach styles
            target.PropertyChanged += (s, e) =>
            {
                if (e.Property == TestClass.FooProperty && (string)e.NewValue == "foo")
                {
                    target.BeginBatchUpdate();
                    target.ClearValue(TestClass.FooProperty);
                    target.ClearValue(TestClass.BarProperty);
                    target.EndBatchUpdate();
                }
            };

            target.EndBatchUpdate();

            Assert.Null(target.Foo);
            Assert.Null(target.Bar);
            Assert.Equal(2, raised.Count);
            Assert.Equal(TestClass.FooProperty, raised[0].Property);
            Assert.Null(raised[0].OldValue);
            Assert.Equal("foo", raised[0].NewValue);
            Assert.Equal(TestClass.FooProperty, raised[1].Property);
            Assert.Equal("foo", raised[1].OldValue);
            Assert.Null(raised[1].NewValue);
        }

        [Fact]
        public void Can_Set_Cleared_Value_When_Ending_Batch_Update()
        {
            var target = new TestClass();
            var raised = 0;

            target.Foo = "foo";

            target.BeginBatchUpdate();
            target.ClearValue(TestClass.FooProperty);
            target.PropertyChanged += (sender, e) =>
            {
                if (e.Property == TestClass.FooProperty && e.NewValue is null)
                {
                    target.Foo = "bar";
                    ++raised;
                }
            };
            target.EndBatchUpdate();

            Assert.Equal("bar", target.Foo);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Can_Bind_Cleared_Value_When_Ending_Batch_Update()
        {
            var target = new TestClass();
            var raised = 0;
            var notifications = new List<AvaloniaPropertyChangedEventArgs>();

            target.Foo = "foo";

            target.BeginBatchUpdate();
            target.ClearValue(TestClass.FooProperty);
            target.PropertyChanged += (sender, e) =>
            {
                if (e.Property == TestClass.FooProperty && e.NewValue is null)
                {
                    target.Bind(TestClass.FooProperty, new TestObservable<string>("bar"));
                    ++raised;
                }

                notifications.Add(e);
            };
            target.EndBatchUpdate();

            Assert.Equal("bar", target.Foo);
            Assert.Equal(1, raised);
            Assert.Equal(2, notifications.Count);
            Assert.Equal(null, notifications[0].NewValue);
            Assert.Equal("bar", notifications[1].NewValue);
        }

        [Fact]
        public void Can_Bind_Completed_Binding_Back_To_Original_Value_When_Ending_Batch_Update()
        {
            var target = new TestClass();
            var raised = 0;
            var notifications = new List<AvaloniaPropertyChangedEventArgs>();
            var observable1 = new TestObservable<string>("foo");
            var observable2 = new TestObservable<string>("foo");

            target.Bind(TestClass.FooProperty, observable1);

            target.BeginBatchUpdate();
            observable1.OnCompleted();
            target.PropertyChanged += (sender, e) =>
            {
                if (e.Property == TestClass.FooProperty && e.NewValue is null)
                {
                    target.Bind(TestClass.FooProperty, observable2);
                    ++raised;
                }

                notifications.Add(e);
            };
            target.EndBatchUpdate();

            Assert.Equal("foo", target.Foo);
            Assert.Equal(1, raised);
            Assert.Equal(2, notifications.Count);
            Assert.Equal(null, notifications[0].NewValue);
            Assert.Equal("foo", notifications[1].NewValue);
        }

        public class TestClass : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<TestClass, string>(nameof(Foo));

            public static readonly StyledProperty<string> BarProperty =
                AvaloniaProperty.Register<TestClass, string>(nameof(Bar));

            public static readonly StyledProperty<Orientation> BazProperty =
                AvaloniaProperty.Register<TestClass, Orientation>(nameof(Bar), Orientation.Vertical);

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

            public Orientation Baz
            {
                get => GetValue(BazProperty);
                set => SetValue(BazProperty, value);
            }
        }

        public class TestObservable<T> : ObservableBase<BindingValue<T>>
        {
            private readonly T _value;
            private IObserver<BindingValue<T>> _observer;

            public TestObservable(T value) => _value = value;

            public int SubscribeCount { get; private set; }

            public void OnCompleted() => _observer.OnCompleted();
            public void OnError(Exception e) => _observer.OnError(e);

            protected override IDisposable SubscribeCore(IObserver<BindingValue<T>> observer)
            {
                ++SubscribeCount;
                _observer = observer;
                observer.OnNext(_value);
                return Disposable.Empty;
            }
        }
    }
}
