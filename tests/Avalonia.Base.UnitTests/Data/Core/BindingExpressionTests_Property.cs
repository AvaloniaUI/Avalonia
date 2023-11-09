using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes.Reflection;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Microsoft.Reactive.Testing;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class BindingExpressionTests_Property
    {
        [Fact]
        public async Task Should_Get_Simple_Property_Value()
        {
            var data = new { Foo = "foo" };
            var target = BindingExpression.Create(data, o => o.Foo).ToObservable();
            var result = await target.Take(1);

            Assert.Equal("foo", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_Simple_Property_Value_Null()
        {
            var data = new { Foo = (string)null };
            var target = BindingExpression.Create(data, o => o.Foo).ToObservable();
            var result = await target.Take(1);

            Assert.Null(result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_Simple_Property_From_Base_Class()
        {
            var data = new Class3 { Foo = "foo" };
            var target = BindingExpression.Create(data, o => o.Foo).ToObservable();
            var result = await target.Take(1);

            Assert.Equal("foo", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_Error_For_Root_Null()
        {
            var target = BindingExpression.Create(default(Class3), o => o.Foo).ToObservable();
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                        new BindingChainException("Binding Source is null.", "Foo", "(source)"),
                        BindingErrorType.Error,
                        AvaloniaProperty.UnsetValue),
                result);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_Error_For_Root_UnsetValue()
        {
            var target = BindingExpression.Create(AvaloniaProperty.UnsetValue, o => (o as Class3).Foo).ToObservable();
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                        new BindingChainException("Binding Source is null.", "Foo", "(source)"),
                        BindingErrorType.Error,
                        AvaloniaProperty.UnsetValue),
                result);
        }

        [Fact]
        public async Task Should_Get_Simple_Property_Chain()
        {
            var data = new { Foo = new { Bar = new { Baz = "baz" } } };
            var target = BindingExpression.Create(data, o => o.Foo.Bar.Baz).ToObservable();
            var result = await target.Take(1);

            Assert.Equal("baz", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Return_BindingNotification_Error_For_Chain_With_Null_Value()
        {
            var data = new { Foo = default(Class1) };
            var target = BindingExpression.Create(data, o => o.Foo.Foo.Length);
            var result = new List<object>();

            target.ToObservable().Subscribe(x => result.Add(x));

            Assert.Equal(
                new[]
                {
                            new BindingNotification(
                                new BindingChainException("Value is null.", "Foo.Foo.Length", "Foo"),
                                BindingErrorType.Error,
                                AvaloniaProperty.UnsetValue),
                },
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Simple_Property_Value()
        {
            var data = new Class1 { Foo = "foo" };
            var target = BindingExpression.Create(data, o => o.Foo);
            var result = new List<object>();

            var sub = target.ToObservable().Subscribe(x => result.Add(x));
            data.Foo = "bar";

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();

            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Trigger_PropertyChanged_On_Null_Or_Empty_String()
        {
            var data = new Class1 { Bar = "foo" };
            var target = BindingExpression.Create(data, o => o.Bar);
            var result = new List<object>();

            var sub = target.ToObservable().Subscribe(x => result.Add(x));

            Assert.Equal(new[] { "foo" }, result);

            data.Bar = "bar";

            Assert.Equal(new[] { "foo" }, result);

            data.RaisePropertyChanged(string.Empty);

            Assert.Equal(new[] { "foo", "bar" }, result);

            data.SetBarWithoutRaising("baz");

            Assert.Equal(new[] { "foo", "bar" }, result);

            data.RaisePropertyChanged(null);

            Assert.Equal(new[] { "foo", "bar", "baz" }, result);

            sub.Dispose();

            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_End_Of_Property_Chain_Changing()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = BindingExpression.Create(data, o => (o.Next as Class2).Bar);
            var result = new List<object>();

            var sub = target.ToObservable().Subscribe(x => result.Add(x));
            ((Class2)data.Next).Bar = "baz";
            ((Class2)data.Next).Bar = null;

            Assert.Equal(new[] { "bar", "baz", null }, result);

            sub.Dispose();
            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Changing()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = BindingExpression.Create(data, o => (o.Next as Class2).Bar);
            var result = new List<object>();

            var sub = target.ToObservable().Subscribe(x => result.Add(x));
            var old = data.Next;
            data.Next = new Class2 { Bar = "baz" };
            data.Next = new Class2 { Bar = null };

            Assert.Equal(new[] { "bar", "baz", null }, result);

            sub.Dispose();

            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);
            Assert.Equal(0, old.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Breaking_With_Null_Then_Mending()
        {
            var data = new Class1
            {
                Next = new Class2
                {
                    Next = new Class2
                    {
                        Bar = "bar"
                    }
                }
            };

            var target = BindingExpression.Create(data, o => ((o.Next as Class2).Next as Class2).Bar);
            var result = new List<object>();

            var sub = target.ToObservable().Subscribe(x => result.Add(x));
            var old = data.Next;
            data.Next = new Class2 { Bar = "baz" };
            data.Next = old;

            Assert.Equal(
                new object[]
                {
                            "bar",
                            new BindingNotification(
                                new BindingChainException("Value is null.", "Next.Next.Bar", "Next"),
                                BindingErrorType.Error,
                                AvaloniaProperty.UnsetValue),
                            "bar"
                },
                result);

            sub.Dispose();

            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);
            Assert.Equal(0, old.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Breaking_With_Missing_Member_Then_Mending()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = BindingExpression.Create(data, o => (o.Next as Class2).Bar);
            var result = new List<object>();

            var sub = target.ToObservable().Subscribe(x => result.Add(x));
            var old = data.Next;
            var breaking = new WithoutBar();
            data.Next = breaking;
            data.Next = new Class2 { Bar = "baz" };

            Assert.Equal(
                new object[]
                {
                            "bar",
                            new BindingNotification(
                                new BindingChainException(
                                    $"Could not find a matching property accessor for '{nameof(Class2.Bar)}' on '{typeof(WithoutBar)}'.",
                                    "Next.Bar",
                                    "Bar"),
                                BindingErrorType.Error),
                            "baz",
                },
                result);

            sub.Dispose();

            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);
            Assert.Equal(0, breaking.PropertyChangedSubscriptionCount);
            Assert.Equal(0, old.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive()
        {
            Func<Tuple<BindingExpression, WeakReference>> run = () =>
            {
                var source = new Class1 { Foo = "foo" };
                var target = BindingExpression.Create(source, o => o.Foo);
                return Tuple.Create(target, new WeakReference(source));
            };

            var result = run();
            result.Item1.ToObservable().Subscribe(x => { });

            // Mono trickery
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            GC.WaitForPendingFinalizers();
            GC.Collect(2);


            Assert.Null(result.Item2.Target);
        }

        [Fact]
        public void Should_Not_Throw_Exception_On_Duplicate_Properties()
        {
            // Repro of https://github.com/AvaloniaUI/Avalonia/issues/4733.
            var source = new MyViewModel();
            var target = BindingExpression.Create(source, x => x.Name);
            var result = new List<object>();

            target.ToObservable().Subscribe(x => result.Add(x));

            Assert.Equal(new[] { "NewName" }, result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Can_Convert_Int_To_Enum_Two_Way(bool allowReflection)
        {
            var data = new Class3 {  IntValue = 1 };
            var control = new Control();
            var target = BindingExpression.Create(
                data, 
                o => o.IntValue,
                mode: BindingMode.TwoWay,
                allowReflection: allowReflection);
            var instance = new InstancedBinding(
                target,
                BindingMode.TwoWay,
                BindingPriority.LocalValue);

            BindingOperations.Apply(control, DockPanel.DockProperty, instance);

            Assert.Equal(Dock.Bottom, DockPanel.GetDock(control));

            DockPanel.SetDock(control, Dock.Right);

            Assert.Equal(2, data.IntValue);

            GC.KeepAlive(data);
        }

        public class MyViewModelBase { public object Name => "Name"; }

        public class MyViewModel : MyViewModelBase { public new string Name => "NewName"; }

        private interface INext
        {
            int PropertyChangedSubscriptionCount { get; }
        }

        private class Class1 : NotifyingBase
        {
            private string _foo;
            private INext _next;

            public string Foo
            {
                get { return _foo; }
                set
                {
                    _foo = value;
                    RaisePropertyChanged(nameof(Foo));
                }
            }

            private string _bar;
            public string Bar
            {
                get { return _bar; }
                set { _bar = value; }
            }

            public INext Next
            {
                get { return _next; }
                set
                {
                    _next = value;
                    RaisePropertyChanged(nameof(Next));
                }
            }

            public void SetBarWithoutRaising(string value) => _bar = value;
        }

        private class Class2 : NotifyingBase, INext
        {
            private string _bar;
            private INext _next;

            public string Bar
            {
                get { return _bar; }
                set
                {
                    _bar = value;
                    RaisePropertyChanged(nameof(Bar));
                }
            }

            public INext Next
            {
                get { return _next; }
                set
                {
                    _next = value;
                    RaisePropertyChanged(nameof(Next));
                }
            }
        }

        private class Class3 : Class1
        {
            private int _intValue;

            public int IntValue
            {
                get => _intValue;
                set
                {
                    _intValue = value;
                    RaisePropertyChanged(nameof(IntValue));
                }
            }
        }

        private class WithoutBar : NotifyingBase, INext
        {
        }

        private static Recorded<Notification<T>> OnNext<T>(long time, T value)
        {
            return new Recorded<Notification<T>>(time, Notification.CreateOnNext<T>(value));
        }
    }
}
