// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Perspex.Markup.Binding;
using Xunit;

namespace Perspex.Markup.UnitTests.Binding
{
    public class ExpressionObserverTests
    {
        [Fact]
        public async void Should_Get_Simple_Property_Value()
        {
            var data = new { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");
            var result = await target.Take(1);

            Assert.True(result.HasValue);
            Assert.Equal("foo", result.Value);
        }

        [Fact]
        public async void Should_Get_Simple_Property_Chain()
        {
            var data = new { Foo = new { Bar = new { Baz = "baz" } }  };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");
            var result = await target.Take(1);

            Assert.True(result.HasValue);
            Assert.Equal("baz", result.Value);
        }

        [Fact]
        public async void Should_Not_Have_Value_For_Broken_Chain()
        {
            var data = new { Foo = new { Bar = 1 } };
            var target = new ExpressionObserver(data, "Foo.Bar.Baz");
            var result = await target.Take(1);

            Assert.False(result.HasValue);
        }

        [Fact]
        public void Should_Track_Simple_Property_Value()
        {
            var data = new Class1 { Foo = "foo" };
            var target = new ExpressionObserver(data, "Foo");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x.Value));
            data.Foo = "bar";

            Assert.Equal(new[] { "foo", "bar" }, result);

            sub.Dispose();

            Assert.Equal(0, data.SubscriptionCount);
        }

        [Fact]
        public void Should_Track_End_Of_Property_Chain_Changing()
        {
            var data = new Class1 { Class2 = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Class2.Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x.Value));
            data.Class2.Bar = "baz";

            Assert.Equal(new[] { "bar", "baz" }, result);

            sub.Dispose();

            Assert.Equal(0, data.SubscriptionCount);
            Assert.Equal(0, data.Class2.SubscriptionCount);
        }

        [Fact]
        public void Should_Track_Middle_Of_Property_Chain_Changing()
        {
            var data = new Class1 { Class2 = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Class2.Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x.Value));
            var old = data.Class2;
            data.Class2 = new Class2 { Bar = "baz" };

            Assert.Equal(new[] { "bar", "baz" }, result);

            sub.Dispose();

            Assert.Equal(0, data.SubscriptionCount);
            Assert.Equal(0, data.Class2.SubscriptionCount);
            Assert.Equal(0, old.SubscriptionCount);
        }

        [Fact]
        public void Should_Track_Middle_Of_Property_Chain_Breaking_Then_Mending()
        {
            var data = new Class1 { Class2 = new Class2 { Bar = "bar" } };
            var target = new ExpressionObserver(data, "Class2.Bar");
            var result = new List<object>();

            var sub = target.Subscribe(x => result.Add(x.Value));
            var old = data.Class2;
            data.Class2 = null;
            data.Class2 = new Class2 { Bar = "baz" };

            Assert.Equal(new[] { "bar", null, "baz" }, result);

            sub.Dispose();

            Assert.Equal(0, data.SubscriptionCount);
            Assert.Equal(0, data.Class2.SubscriptionCount);
            Assert.Equal(0, old.SubscriptionCount);
        }

        private class Class1 : NotifyingBase
        {
            private string _foo;
            private Class2 _class2;

            public string Foo
            {
                get { return _foo; }
                set
                {
                    _foo = value;
                    RaisePropertyChanged(nameof(Foo));
                }
            }

            public Class2 Class2
            {
                get { return _class2; }
                set
                {
                    _class2 = value;
                    RaisePropertyChanged(nameof(Class2));
                }
            }
        }

        private class Class2 : NotifyingBase
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
    }
}
