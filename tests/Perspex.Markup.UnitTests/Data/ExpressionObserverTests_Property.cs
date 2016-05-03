// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Perspex.Data;
using Perspex.Markup.Data;
using Perspex.UnitTests;
using Xunit;

namespace Perspex.Markup.UnitTests.Data
{
    public class ExpressionObserverTests_Property
    {
        [Fact]
        public async void Should_Get_Simple_Property_Value()
        {
            var data = new { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");
            var result = await target.Take(1);

            Assert.Equal("foo", result);
        }

        [Fact]
        public void Should_Get_Simple_Property_Value_Type()
        {
            var data = new { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");

            Assert.Equal(typeof(string), target.ResultType);
        }

        [Fact]
        public async void Should_Get_Simple_Property_Value_Null()
        {
            var data = new { Foo = (string)null };
            var target = new ExpressionObserver(data, "Foo");
            var result = await target.Take(1);

            Assert.Null(result);
        }

        [Fact]
        public async void Should_Get_Simple_Property_From_Base_Class()
        {
            var data = new Class3 { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");
            var result = await target.Take(1);

            Assert.Equal("foo", result);
        }

        [Fact]
        public async void Should_Get_Simple_Property_Chain()
        {
            var data = new { Foo = new { Bar = new { Baz = "baz" } }  };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");
            var result = await target.Take(1);

            Assert.Equal("baz", result);
        }

        [Fact]
        public void Should_Get_Simple_Property_Chain_Type()
        {
            var data = new { Foo = new { Bar = new { Baz = "baz" } } };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");

            Assert.Equal(typeof(string), target.ResultType);
        }

        [Fact]
        public async void Should_Return_BindingError_For_Broken_Chain()
        {
            var data = new { Foo = new { Bar = 1 } };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");
            var result = await target.Take(1);

            Assert.IsType<BindingError>(result);

            var error = result as BindingError;
            Assert.IsType<MissingMemberException>(error.Exception);
            Assert.Equal("Could not find CLR property 'Baz' on '1'", error.Exception.Message);
        }

        [Fact]
        public void Should_Have_Null_ResultType_For_Broken_Chain()
        {
            var data = new { Foo = new { Bar = 1 } };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");

            Assert.Null(target.ResultType);
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

            Assert.Equal(0, data.SubscriptionCount);
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

            Assert.Equal(0, data.SubscriptionCount);
        }

        [Fact]
        public void Should_Track_End_Of_Property_Chain_Changing()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Next.Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            ((Class2)data.Next).Bar = "baz";

            Assert.Equal(new[] { "bar", "baz" }, result);

            sub.Dispose();

            Assert.Equal(0, data.SubscriptionCount);
            Assert.Equal(0, data.Next.SubscriptionCount);
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

            Assert.Equal(new[] { "bar", "baz" }, result);

            sub.Dispose();

            Assert.Equal(0, data.SubscriptionCount);
            Assert.Equal(0, data.Next.SubscriptionCount);
            Assert.Equal(0, old.SubscriptionCount);
        }

        [Fact]
        public void Should_Track_Property_Chain_Breaking_With_Null_Then_Mending()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Next.Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            var old = data.Next;
            data.Next = null;
            data.Next = new Class2 { Bar = "baz" };

            Assert.Equal(new[] { "bar", PerspexProperty.UnsetValue, "baz" }, result);

            sub.Dispose();

            Assert.Equal(0, data.SubscriptionCount);
            Assert.Equal(0, data.Next.SubscriptionCount);
            Assert.Equal(0, old.SubscriptionCount);
        }

        [Fact]
        public void Should_Track_Property_Chain_Breaking_With_Object_Then_Mending()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Next.Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x));
            var old = data.Next;
            var breaking = new WithoutBar();
            data.Next = breaking;
            data.Next = new Class2 { Bar = "baz" };

            Assert.Equal(3, result.Count);
            Assert.Equal("bar", result[0]);
            Assert.IsType<BindingError>(result[1]);
            Assert.Equal("baz", result[2]);

            sub.Dispose();

            Assert.Equal(0, data.SubscriptionCount);
            Assert.Equal(0, data.Next.SubscriptionCount);
            Assert.Equal(0, breaking.SubscriptionCount);
            Assert.Equal(0, old.SubscriptionCount);
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

            Assert.Equal(new[] { PerspexProperty.UnsetValue, "foo", "bar" }, result);
            Assert.All(source.Subscriptions, x => Assert.NotEqual(Subscription.Infinite, x.Unsubscribe));
        }

        [Fact]
        public void SetValue_Should_Set_Simple_Property_Value()
        {
            var data = new Class1 { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");

            Assert.True(target.SetValue("bar"));
            Assert.Equal("bar", data.Foo);
        }

        [Fact]
        public void SetValue_Should_Set_Property_At_The_End_Of_Chain()
        {
            var data = new Class1 { Next = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Next.Bar");

            Assert.True(target.SetValue("baz"));
            Assert.Equal("baz", ((Class2)data.Next).Bar);
        }

        [Fact]
        public void SetValue_Should_Return_False_For_Missing_Property()
        {
            var data = new Class1 { Next = new WithoutBar()};
            var target = new ExpressionObserver(data, "Next.Bar");

            Assert.False(target.SetValue("baz"));
        }

        [Fact]
        public void SetValue_Should_Return_False_For_Missing_Object()
        {
            var data = new Class1();
            var target = new ExpressionObserver(data, "Next.Bar");

            Assert.False(target.SetValue("baz"));
        }

        [Fact]
        public async void Should_Handle_Null_Root()
        {
            var target = new ExpressionObserver((object)null, "Foo");
            var result = await target.Take(1);

            Assert.Equal(PerspexProperty.UnsetValue, result);
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

            Assert.Equal(new[] { "foo", "bar", PerspexProperty.UnsetValue }, result);

            Assert.Equal(0, first.SubscriptionCount);
            Assert.Equal(0, second.SubscriptionCount);
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
            int SubscriptionCount { get; }
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

            public string Bar
            {
                get { return _bar; }
                set
                {
                    _bar = value;
                    RaisePropertyChanged(nameof(Bar));
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
