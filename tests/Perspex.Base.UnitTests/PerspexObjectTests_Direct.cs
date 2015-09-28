// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Xunit;

namespace Perspex.Base.UnitTests
{
    public class PerspexObjectTests_Direct
    {
        [Fact]
        public void GetValue_Gets_Value()
        {
            var target = new Class1();

            Assert.Equal("initial", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Gets_Value_NonGeneric()
        {
            var target = new Class1();

            Assert.Equal("initial", target.GetValue((PerspexProperty)Class1.FooProperty));
        }

        [Fact]
        public void SetValue_Sets_Value()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.Foo);
        }

        [Fact]
        public void SetValue_Sets_Value_NonGeneric()
        {
            var target = new Class1();

            target.SetValue((PerspexProperty)Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.Foo);
        }

        [Fact]
        public void SetValue_Raises_PropertyChanged()
        {
            var target = new Class1();
            bool raised = false;

            target.PropertyChanged += (s, e) =>
                raised = e.Property == Class1.FooProperty &&
                         (string)e.OldValue == "initial" &&
                         (string)e.NewValue == "newvalue" &&
                         e.Priority == BindingPriority.LocalValue;

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.True(raised);
        }

        [Fact]
        public void Direct_Property_Works_As_Binding_Source()
        {
            var target = new Class1();
            List<string> values = new List<string>();

            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            target.Foo = "newvalue";

            Assert.Equal(new[] { "initial", "newvalue" }, values);
        }

        [Fact]
        public void Direct_Property_Can_Be_Bound()
        {
            var target = new Class1();
            var source = new Subject<string>();

            var sub = target.Bind(Class1.FooProperty, source);

            Assert.Equal("initial", target.Foo);
            source.OnNext("first");
            Assert.Equal("first", target.Foo);
            source.OnNext("second");
            Assert.Equal("second", target.Foo);

            sub.Dispose();

            source.OnNext("third");
            Assert.Equal("second", target.Foo);
        }

        [Fact]
        public void Direct_Property_Can_Be_Bound_NonGeneric()
        {
            var target = new Class1();
            var source = new Subject<string>();

            var sub = target.Bind((PerspexProperty)Class1.FooProperty, source);

            Assert.Equal("initial", target.Foo);
            source.OnNext("first");
            Assert.Equal("first", target.Foo);
            source.OnNext("second");
            Assert.Equal("second", target.Foo);

            sub.Dispose();

            source.OnNext("third");
            Assert.Equal("second", target.Foo);
        }

        [Fact]
        public void ReadOnly_Property_Cannot_Be_Set()
        {
            var target = new Class1();

            Assert.Throws<ArgumentException>(() => 
                target.SetValue(Class1.BarProperty, "newvalue"));
        }

        [Fact]
        public void ReadOnly_Property_Cannot_Be_Set_NonGeneric()
        {
            var target = new Class1();

            Assert.Throws<ArgumentException>(() => 
                target.SetValue((PerspexProperty)Class1.BarProperty, "newvalue"));
        }

        [Fact]
        public void ReadOnly_Property_Cannot_Be_Bound()
        {
            var target = new Class1();
            var source = new Subject<string>();

            Assert.Throws<ArgumentException>(() => 
                target.Bind(Class1.BarProperty, source));
        }

        [Fact]
        public void ReadOnly_Property_Cannot_Be_Bound_NonGeneric()
        {
            var target = new Class1();
            var source = new Subject<string>();

            Assert.Throws<ArgumentException>(() =>
                target.Bind(Class1.BarProperty, source));
        }

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<string> FooProperty =
                PerspexProperty.RegisterDirect<Class1, string>("Foo", o => o.Foo, (o, v) => o.Foo = v);

            public static readonly PerspexProperty<string> BarProperty =
                PerspexProperty.RegisterDirect<Class1, string>("Bar", o => o.Bar);

            private string _foo = "initial";

            private string _bar = "bar";

            public string Foo
            {
                get { return _foo; }
                set { SetAndRaise(FooProperty, ref _foo, value); }
            }

            public string Bar
            {
                get { return _bar; }
            }
        }
    }
}
