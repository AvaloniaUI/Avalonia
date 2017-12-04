// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Avalonia.UnitTests;
using Xunit;
using System.Threading.Tasks;

namespace Avalonia.Markup.UnitTests.Data
{
    public class ExpressionObserverTests_Property
    {
        [Fact]
        public async Task Should_Get_Simple_Property_Value()
        {
            var data = new { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");
            var result = await target.Take(1);

            Assert.Equal("foo", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Get_Simple_Property_Value_Type()
        {
            var data = new { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");

            target.Subscribe(_ => { });

            Assert.Equal(typeof(string), target.ResultType);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_Simple_Property_Value_Null()
        {
            var data = new { Foo = (string)null };
            var target = new ExpressionObserver(data, "Foo");
            var result = await target.Take(1);

            Assert.Null(result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_Simple_Property_From_Base_Class()
        {
            var data = new Class3 { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");
            var result = await target.Take(1);

            Assert.Equal("foo", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_Error_For_Root_Null()
        {
            var data = new Class3 { Foo = "foo" };
            var target = new ExpressionObserver(default(object), "Foo");
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                        new MarkupBindingChainException("Null value", "Foo", string.Empty),
                        BindingErrorType.Error,
                        AvaloniaProperty.UnsetValue),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_Error_For_Root_UnsetValue()
        {
            var data = new Class3 { Foo = "foo" };
            var target = new ExpressionObserver(AvaloniaProperty.UnsetValue, "Foo");
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                        new MarkupBindingChainException("Null value", "Foo", string.Empty),
                        BindingErrorType.Error,
                        AvaloniaProperty.UnsetValue),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_Error_For_Observable_Root_Null()
        {
            var data = new Class3 { Foo = "foo" };
            var target = new ExpressionObserver(Observable.Return(default(object)), "Foo");
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                        new MarkupBindingChainException("Null value", "Foo", string.Empty),
                        BindingErrorType.Error,
                        AvaloniaProperty.UnsetValue),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async void Should_Return_BindingNotification_Error_For_Observable_Root_UnsetValue()
        {
            var data = new Class3 { Foo = "foo" };
            var target = new ExpressionObserver(Observable.Return(AvaloniaProperty.UnsetValue), "Foo");
            var result = await target.Take(1);

            Assert.Equal(
                new BindingNotification(
                        new MarkupBindingChainException("Null value", "Foo", string.Empty),
                        BindingErrorType.Error,
                        AvaloniaProperty.UnsetValue),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Get_Simple_Property_Chain()
        {
            var data = new { Foo = new { Bar = new { Baz = "baz" } } };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");
            var result = await target.Take(1);

            Assert.Equal("baz", result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Get_Simple_Property_Chain_Type()
        {
            var data = new { Foo = new { Bar = new { Baz = "baz" } } };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");

            target.Subscribe(_ => { });

            Assert.Equal(typeof(string), target.ResultType);

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Should_Return_BindingNotification_Error_For_Broken_Chain()
        {
            var data = new { Foo = new { Bar = 1 } };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");
            var result = await target.Take(1);

            Assert.IsType<BindingNotification>(result);

            Assert.Equal(
                new BindingNotification(
                    new MissingMemberException("Could not find CLR property 'Baz' on '1'"), BindingErrorType.Error),
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Return_BindingNotification_Error_For_Chain_With_Null_Value()
        {
            var data = new { Foo = default(object) };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");
            var result = new List<object>();

            target.Subscribe(x => result.Add(x));

            Assert.Equal(
                new[]
                {
                    new BindingNotification(
                        new MarkupBindingChainException("Null value", "Foo.Bar.Baz", "Foo"),
                        BindingErrorType.Error,
                        AvaloniaProperty.UnsetValue),
                },
                result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Have_Null_ResultType_For_Broken_Chain()
        {
            var data = new { Foo = new { Bar = 1 } };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");

            Assert.Null(target.ResultType);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Simple_Property_Value()
        {
            var data = new Class1 { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            data.Foo = "bar";

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Trigger_PropertyChanged_On_Null_Or_Empty_String()
        {
            var data = new Class1 { Bar = "foo" };
            var target = new ExpressionObserver(data, "Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));

            Assert.Equal(new[] { "foo" }, result);

            data.Bar = "bar";

            Assert.Equal(new[] { "foo" }, result);

            data.RaisePropertyChanged(string.Empty);

            Assert.Equal(new[] { "foo", "bar" }, result);

            data.RaisePropertyChanged(null);

            Assert.Equal(new[] { "foo", "bar", "bar" }, result);

            sub.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_End_Of_Property_Chain_Changing()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Next.Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            ((Class2)data.Next).Bar = "baz";
            ((Class2)data.Next).Bar = null;

            Assert.Equal(new[] { "bar", "baz", null }, result);

            sub.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Changing()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Next.Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            var old = data.Next;
            data.Next = new Class2 { Bar = "baz" };
            data.Next = new Class2 { Bar = null };

            Assert.Equal(new[] { "bar", "baz", null }, result);

            sub.Dispose();

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

            var target = new ExpressionObserver(data, "Next.Next.Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            var old = data.Next;
            data.Next = new Class2 { Bar = "baz" };
            data.Next = old;

            Assert.Equal(
                new object[]
                {
                    "bar",
                    new BindingNotification(
                        new MarkupBindingChainException("Null value", "Next.Next.Bar", "Next.Next"),
                        BindingErrorType.Error,
                        AvaloniaProperty.UnsetValue),
                    "bar"
                },
                result);

            sub.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);
            Assert.Equal(0, old.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Breaking_With_Missing_Member_Then_Mending()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Next.Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            var old = data.Next;
            var breaking = new WithoutBar();
            data.Next = breaking;
            data.Next = new Class2 { Bar = "baz" };

            Assert.Equal(
                new object[]
                {
                    "bar",
                    new BindingNotification(
                        new MissingMemberException("Could not find CLR property 'Bar' on 'Avalonia.Markup.UnitTests.Data.ExpressionObserverTests_Property+WithoutBar'"),
                        BindingErrorType.Error),
                    "baz",
                },
                result);

            sub.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);
            Assert.Equal(0, data.Next.PropertyChangedSubscriptionCount);
            Assert.Equal(0, breaking.PropertyChangedSubscriptionCount);
            Assert.Equal(0, old.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Empty_Expression_Should_Track_Root()
        {
            var data = new Class1 { Foo = "foo" };
            var update = new Subject<Unit>();
            var target = new ExpressionObserver(() => data.Foo, "", update);
            var result = new List<object>();

            target.Subscribe(x => result.Add(x));

            data.Foo = "bar";
            update.OnNext(Unit.Default);

            Assert.Equal(new[] { "foo", "bar" }, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Value_From_Observable_Root()
        {
            var scheduler = new TestScheduler();
            var source = scheduler.CreateColdObservable(
                OnNext(1, new Class1 { Foo = "foo" }),
                OnNext(2, new Class1 { Foo = "bar" }));
            var target = new ExpressionObserver(source, "Foo");
            var result = new List<object>();

            using (target.Subscribe(x => result.Add(x)))
            {
                scheduler.Start();
            }

            Assert.Equal(new[] { "foo", "bar" }, result);
            Assert.All(source.Subscriptions, x => Assert.NotEqual(Subscription.Infinite, x.Unsubscribe));
        }

        [Fact]
        public void Subscribing_Multiple_Times_Should_Return_Values_To_All()
        {
            var data = new Class1 { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");
            var result1 = new List<object>();
            var result2 = new List<object>();
            var result3 = new List<object>();

            target.Subscribe(x => result1.Add(x));
            target.Subscribe(x => result2.Add(x));

            data.Foo = "bar";

            target.Subscribe(x => result3.Add(x));

            Assert.Equal(new[] { "foo", "bar" }, result1);
            Assert.Equal(new[] { "foo", "bar" }, result2);
            Assert.Equal(new[] { "bar" }, result3);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Subscribing_Multiple_Times_Should_Only_Add_PropertyChanged_Handlers_Once()
        {
            var data = new Class1 { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");

            var sub1 = target.Subscribe(x => { });
            var sub2 = target.Subscribe(x => { });

            Assert.Equal(1, data.PropertyChangedSubscriptionCount);

            sub1.Dispose();
            sub2.Dispose();

            Assert.Equal(0, data.PropertyChangedSubscriptionCount);

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Set_Simple_Property_Value()
        {
            var data = new Class1 { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");

            using (target.Subscribe(_ => { }))
            {
                Assert.True(target.SetValue("bar"));
            }

            Assert.Equal("bar", data.Foo);

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Set_Property_At_The_End_Of_Chain()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Next.Bar");

            using (target.Subscribe(_ => { }))
            {
                Assert.True(target.SetValue("baz"));
            }

            Assert.Equal("baz", ((Class2)data.Next).Bar);

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Return_False_For_Missing_Property()
        {
            var data = new Class1 { Next = new WithoutBar() };
            var target = new ExpressionObserver(data, "Next.Bar");

            using (target.Subscribe(_ => { }))
            {
                Assert.False(target.SetValue("baz"));
            }

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Notify_New_Value_With_Inpc()
        {
            var data = new Class1();
            var target = new ExpressionObserver(data, "Foo");
            var result = new List<object>();

            target.Subscribe(x => result.Add(x));
            target.SetValue("bar");

            Assert.Equal(new[] { null, "bar" }, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Notify_New_Value_Without_Inpc()
        {
            var data = new Class1();
            var target = new ExpressionObserver(data, "Bar");
            var result = new List<object>();

            target.Subscribe(x => result.Add(x));
            target.SetValue("bar");

            Assert.Equal(new[] { null, "bar" }, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Return_False_For_Missing_Object()
        {
            var data = new Class1();
            var target = new ExpressionObserver(data, "Next.Bar");

            using (target.Subscribe(_ => { }))
            {
                Assert.False(target.SetValue("baz"));
            }

            GC.KeepAlive(data);
        }

        [Fact]
        public void Can_Replace_Root()
        {
            var first = new Class1 { Foo = "foo" };
            var second = new Class1 { Foo = "bar" };
            var root = first;
            var update = new Subject<Unit>();
            var target = new ExpressionObserver(() => root, "Foo", update);
            var result = new List<object>();
            var sub = target.Subscribe(x => result.Add(x));

            root = second;
            update.OnNext(Unit.Default);
            root = null;
            update.OnNext(Unit.Default);

            Assert.Equal(
                new object[]
                {
                    "foo",
                    "bar",
                    new BindingNotification(
                        new MarkupBindingChainException("Null value", "Foo", string.Empty),
                        BindingErrorType.Error,
                        AvaloniaProperty.UnsetValue)
                },
                result);

            Assert.Equal(0, first.PropertyChangedSubscriptionCount);
            Assert.Equal(0, second.PropertyChangedSubscriptionCount);

            GC.KeepAlive(first);
            GC.KeepAlive(second);
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive()
        {
            Func<Tuple<ExpressionObserver, WeakReference>> run = () =>
            {
                var source = new Class1 { Foo = "foo" };
                var target = new ExpressionObserver(source, "Foo");
                return Tuple.Create(target, new WeakReference(source));
            };

            var result = run();
            result.Item1.Subscribe(x => { });

            GC.Collect();

            Assert.Null(result.Item2.Target);
        }

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
        }

        private class WithoutBar : NotifyingBase, INext
        {
        }

        private Recorded<Notification<object>> OnNext(long time, object value)
        {
            return new Recorded<Notification<object>>(time, Notification.CreateOnNext<object>(value));
        }
    }
}
