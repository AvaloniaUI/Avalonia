// -----------------------------------------------------------------------
// <copyright file="PerspexObjectTests_Binding.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Base.UnitTests
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Xunit;

    public class PerspexObjectTests_Binding
    {
        [Fact]
        public void Bind_Sets_Current_Value()
        {
            Class1 target = new Class1();
            Class1 source = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.Bind(Class1.FooProperty, source.GetObservable(Class1.FooProperty));

            Assert.Equal("initial", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Bind_NonGeneric_Sets_Current_Value()
        {
            Class1 target = new Class1();
            Class1 source = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.Bind((PerspexProperty)Class1.FooProperty, source.GetObservable(Class1.FooProperty));

            Assert.Equal("initial", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Bind_Throws_Exception_For_Unregistered_Property()
        {
            Class1 target = new Class1();

            Assert.Throws<InvalidOperationException>(() =>
            {
                target.Bind(Class2.BarProperty, Observable.Return("foo"));
            });
        }

        [Fact]
        public void Bind_Sets_Subsequent_Value()
        {
            Class1 target = new Class1();
            Class1 source = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.Bind(Class1.FooProperty, source.GetObservable(Class1.FooProperty));
            source.SetValue(Class1.FooProperty, "subsequent");

            Assert.Equal("subsequent", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Bind_Throws_Exception_For_Invalid_Value_Type()
        {
            Class1 target = new Class1();

            Assert.Throws<InvalidOperationException>(() =>
            {
                target.Bind((PerspexProperty)Class1.FooProperty, Observable.Return((object)123));
            });
        }

        [Fact]
        public void Two_Way_Binding_Works()
        {
            Class1 obj1 = new Class1();
            Class1 obj2 = new Class1();

            obj1.SetValue(Class1.FooProperty, "initial1");
            obj2.SetValue(Class1.FooProperty, "initial2");

            obj1.Bind(Class1.FooProperty, obj2.GetObservable(Class1.FooProperty));
            obj2.Bind(Class1.FooProperty, obj1.GetObservable(Class1.FooProperty));

            Assert.Equal("initial2", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("initial2", obj2.GetValue(Class1.FooProperty));

            obj1.SetValue(Class1.FooProperty, "first");

            Assert.Equal("first", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("first", obj2.GetValue(Class1.FooProperty));

            obj2.SetValue(Class1.FooProperty, "second");

            Assert.Equal("second", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("second", obj2.GetValue(Class1.FooProperty));

            obj1.SetValue(Class1.FooProperty, "third");

            Assert.Equal("third", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("third", obj2.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Two_Way_Binding_With_Priority_Works()
        {
            Class1 obj1 = new Class1();
            Class1 obj2 = new Class1();

            obj1.SetValue(Class1.FooProperty, "initial1", BindingPriority.Style);
            obj2.SetValue(Class1.FooProperty, "initial2", BindingPriority.Style);

            obj1.Bind(Class1.FooProperty, obj2.GetObservable(Class1.FooProperty), BindingPriority.Style);
            obj2.Bind(Class1.FooProperty, obj1.GetObservable(Class1.FooProperty), BindingPriority.Style);

            Assert.Equal("initial2", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("initial2", obj2.GetValue(Class1.FooProperty));

            obj1.SetValue(Class1.FooProperty, "first", BindingPriority.Style);

            Assert.Equal("first", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("first", obj2.GetValue(Class1.FooProperty));

            obj2.SetValue(Class1.FooProperty, "second", BindingPriority.Style);

            Assert.Equal("second", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("second", obj2.GetValue(Class1.FooProperty));

            obj1.SetValue(Class1.FooProperty, "third", BindingPriority.Style);

            Assert.Equal("third", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("third", obj2.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void BindTwoWay_Gets_Initial_Value_From_Source()
        {
            Class1 source = new Class1();
            Class1 target = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.BindTwoWay(Class1.FooProperty, source, Class1.FooProperty);

            Assert.Equal("initial", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void BindTwoWay_Updates_Values()
        {
            Class1 source = new Class1();
            Class1 target = new Class1();

            source.SetValue(Class1.FooProperty, "first");
            target.BindTwoWay(Class1.FooProperty, source, Class1.FooProperty);

            Assert.Equal("first", target.GetValue(Class1.FooProperty));
            source.SetValue(Class1.FooProperty, "second");
            Assert.Equal("second", target.GetValue(Class1.FooProperty));
            target.SetValue(Class1.FooProperty, "third");
            Assert.Equal("third", source.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Local_Binding_Overwrites_Local_Value()
        {
            var target = new Class1();
            var binding = new Subject<string>();

            target.Bind(Class1.FooProperty, binding);

            binding.OnNext("first");
            Assert.Equal("first", target.GetValue(Class1.FooProperty));

            target.SetValue(Class1.FooProperty, "second");
            Assert.Equal("second", target.GetValue(Class1.FooProperty));

            binding.OnNext("third");
            Assert.Equal("third", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void StyleBinding_Overrides_Default_Value()
        {
            Class1 target = new Class1();

            target.Bind(Class1.FooProperty, this.Single("stylevalue"), BindingPriority.Style);

            Assert.Equal("stylevalue", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void this_Operator_Returns_Value_Property()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target[Class1.FooProperty]);
        }

        [Fact]
        public void this_Operator_Sets_Value_Property()
        {
            Class1 target = new Class1();

            target[Class1.FooProperty] = "newvalue";

            Assert.Equal("newvalue", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void this_Operator_Doesnt_Accept_Observable()
        {
            Class1 target = new Class1();

            Assert.Throws<InvalidOperationException>(() =>
            {
                target[Class1.FooProperty] = Observable.Return("newvalue");
            });
        }

        [Fact]
        public void this_Operator_Binds_One_Way()
        {
            Class1 target1 = new Class1();
            Class1 target2 = new Class1();
            Binding binding = Class1.FooProperty.Bind().WithMode(BindingMode.OneWay);

            target1.SetValue(Class1.FooProperty, "first");
            target2[binding] = target1[!Class1.FooProperty];
            target1.SetValue(Class1.FooProperty, "second");

            Assert.Equal("second", target2.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void this_Operator_Binds_Two_Way()
        {
            Class1 target1 = new Class1();
            Class1 target2 = new Class1();
            Binding binding = Class1.FooProperty.Bind().WithMode(BindingMode.TwoWay);

            target1.SetValue(Class1.FooProperty, "first");
            target2[binding] = target1[!Class1.FooProperty];
            Assert.Equal("first", target2.GetValue(Class1.FooProperty));
            target1.SetValue(Class1.FooProperty, "second");
            Assert.Equal("second", target2.GetValue(Class1.FooProperty));
            target2.SetValue(Class1.FooProperty, "third");
            Assert.Equal("third", target1.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void this_Operator_Binds_One_Time()
        {
            Class1 target1 = new Class1();
            Class1 target2 = new Class1();
            Binding binding = Class1.FooProperty.Bind().WithMode(BindingMode.OneTime);

            target1.SetValue(Class1.FooProperty, "first");
            target2[binding] = target1[!Class1.FooProperty];
            target1.SetValue(Class1.FooProperty, "second");

            Assert.Equal("first", target2.GetValue(Class1.FooProperty));
        }

        /// <summary>
        /// Returns an observable that returns a single value but does not complete.
        /// </summary>
        /// <typeparam name="T">The type of the observable.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The observable.</returns>
        private IObservable<T> Single<T>(T value)
        {
            return Observable.Never<T>().StartWith(value);
        }

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo", "foodefault");

            public static readonly PerspexProperty<string> BazProperty =
                PerspexProperty.Register<Class1, string>("Baz", "bazdefault", true);
        }

        private class Class2 : Class1
        {
            public static readonly PerspexProperty<string> BarProperty =
                PerspexProperty.Register<Class2, string>("Bar", "bardefault");
        }
    }
}
