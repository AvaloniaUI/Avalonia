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
        public void GetValue_On_Unregistered_Property_Throws_Exception()
        {
            var target = new Class2();

            Assert.Throws<ArgumentException>(() => target.GetValue(Class1.BarProperty));
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
        public void SetValue_NonGeneric_Coerces_UnsetValue_To_Default_Value()
        {
            var target = new Class1();

            target.SetValue((PerspexProperty)Class1.BazProperty, PerspexProperty.UnsetValue);

            Assert.Equal(0, target.Baz);
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
        public void SetValue_Raises_Changed()
        {
            var target = new Class1();
            bool raised = false;

            Class1.FooProperty.Changed.Subscribe(e =>
                raised = e.Property == Class1.FooProperty &&
                         (string)e.OldValue == "initial" &&
                         (string)e.NewValue == "newvalue" &&
                         e.Priority == BindingPriority.LocalValue);

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.True(raised);
        }

        [Fact]
        public void SetValue_On_Unregistered_Property_Throws_Exception()
        {
            var target = new Class2();

            Assert.Throws<ArgumentException>(() => target.SetValue(Class1.BarProperty, "value"));
        }

        [Fact]
        public void GetObservable_Returns_Values()
        {
            var target = new Class1();
            List<string> values = new List<string>();

            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            target.Foo = "newvalue";

            Assert.Equal(new[] { "initial", "newvalue" }, values);
        }

        [Fact]
        public void Bind_Binds_Property_Value()
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
        public void Bind_Binds_Property_Value_NonGeneric()
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
        public void Bind_NonGeneric_Coerces_UnsetValue()
        {
            var target = new Class1();
            var source = new Subject<object>();

            var sub = target.Bind((PerspexProperty)Class1.BazProperty, source);

            Assert.Equal(5, target.Baz);
            source.OnNext(6);
            Assert.Equal(6, target.Baz);
            source.OnNext(PerspexProperty.UnsetValue);
            Assert.Equal(0, target.Baz);
        }

        [Fact]
        public void Bind_Handles_Wrong_Type()
        {
            var target = new Class1();
            var source = new Subject<object>();

            var sub = target.Bind(Class1.FooProperty, source);

            source.OnNext(45);

            Assert.Equal(null, target.Foo);
        }

        [Fact]
        public void Bind_Handles_Wrong_Value_Type()
        {
            var target = new Class1();
            var source = new Subject<object>();

            var sub = target.Bind(Class1.BazProperty, source);

            source.OnNext("foo");

            Assert.Equal(0, target.Baz);
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

        [Fact]
        public void GetValue_Gets_Value_On_AddOwnered_Property()
        {
            var target = new Class2();

            Assert.Equal("initial2", target.GetValue(Class2.FooProperty));
        }

        [Fact]
        public void GetValue_Gets_Value_On_AddOwnered_Property_Using_Original()
        {
            var target = new Class2();

            Assert.Equal("initial2", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Gets_Value_On_AddOwnered_Property_Using_Original_NonGeneric()
        {
            var target = new Class2();

            Assert.Equal("initial2", target.GetValue((PerspexProperty)Class1.FooProperty));
        }

        [Fact]
        public void SetValue_Sets_Value_On_AddOwnered_Property_Using_Original()
        {
            var target = new Class2();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.Foo);
        }

        [Fact]
        public void SetValue_Sets_Value_On_AddOwnered_Property_Using_Original_NonGeneric()
        {
            var target = new Class2();

            target.SetValue((PerspexProperty)Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.Foo);
        }

        [Fact]
        public void Bind_Binds_AddOwnered_Property_Value()
        {
            var target = new Class2();
            var source = new Subject<string>();

            var sub = target.Bind(Class1.FooProperty, source);

            Assert.Equal("initial2", target.Foo);
            source.OnNext("first");
            Assert.Equal("first", target.Foo);
            source.OnNext("second");
            Assert.Equal("second", target.Foo);

            sub.Dispose();

            source.OnNext("third");
            Assert.Equal("second", target.Foo);
        }

        [Fact]
        public void Bind_Binds_AddOwnered_Property_Value_NonGeneric()
        {
            var target = new Class2();
            var source = new Subject<string>();

            var sub = target.Bind((PerspexProperty)Class1.FooProperty, source);

            Assert.Equal("initial2", target.Foo);
            source.OnNext("first");
            Assert.Equal("first", target.Foo);
            source.OnNext("second");
            Assert.Equal("second", target.Foo);

            sub.Dispose();

            source.OnNext("third");
            Assert.Equal("second", target.Foo);
        }

        [Fact]
        public void Property_Notifies_Initialized()
        {
            Class1 target;
            bool raised = false;

            Class1.FooProperty.Initialized.Subscribe(e =>
                raised = e.Property == Class1.FooProperty &&
                         e.OldValue == PerspexProperty.UnsetValue &&
                         (string)e.NewValue == "initial" &&
                         e.Priority == BindingPriority.Unset);

            target = new Class1();

            Assert.True(raised);
        }

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<string> FooProperty =
                PerspexProperty.RegisterDirect<Class1, string>("Foo", o => o.Foo, (o, v) => o.Foo = v);

            public static readonly PerspexProperty<string> BarProperty =
                PerspexProperty.RegisterDirect<Class1, string>("Bar", o => o.Bar);

            public static readonly PerspexProperty<int> BazProperty =
                PerspexProperty.RegisterDirect<Class1, int>("Bar", o => o.Baz, (o,v) => o.Baz = v);

            private string _foo = "initial";
            private readonly string _bar = "bar";
            private int _baz = 5;

            public string Foo
            {
                get { return _foo; }
                set { SetAndRaise(FooProperty, ref _foo, value); }
            }

            public string Bar
            {
                get { return _bar; }
            }

            public int Baz
            {
                get { return _baz; }
                set { SetAndRaise(BazProperty, ref _baz, value); }
            }
        }

        private class Class2 : PerspexObject
        {
            public static readonly PerspexProperty<string> FooProperty =
                Class1.FooProperty.AddOwner<Class2>(o => o.Foo, (o, v) => o.Foo = v);

            private string _foo = "initial2";

            static Class2()
            {
            }

            public string Foo
            {
                get { return _foo; }
                set { SetAndRaise(FooProperty, ref _foo, value); }
            }
        }
    }
}
