// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Xunit;

namespace Perspex.Base.UnitTests
{
    public class PerspexPropertyTests
    {
        [Fact]
        public void Constructor_Sets_Properties()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class1),
                "Foo",
                false,
                BindingMode.OneWay,
                null);

            Assert.Equal("test", target.Name);
            Assert.Equal(typeof(string), target.PropertyType);
            Assert.Equal(typeof(Class1), target.OwnerType);
            Assert.Equal(false, target.Inherits);
        }

        [Fact]
        public void GetDefaultValue_Returns_Registered_Value()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class1),
                "Foo",
                false,
                BindingMode.OneWay,
                null);

            Assert.Equal("Foo", target.GetDefaultValue<Class1>());
        }

        [Fact]
        public void GetDefaultValue_Returns_Registered_Value_For_Not_Overridden_Class()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class1),
                "Foo",
                false,
                BindingMode.OneWay,
                null);

            Assert.Equal("Foo", target.GetDefaultValue<Class2>());
        }

        [Fact]
        public void GetDefaultValue_Returns_Registered_Value_For_Unrelated_Class()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class3),
                "Foo",
                false,
                BindingMode.OneWay,
                null);

            Assert.Equal("Foo", target.GetDefaultValue<Class2>());
        }

        [Fact]
        public void GetDefaultValue_Returns_Overridden_Value()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class1),
                "Foo",
                false,
                BindingMode.OneWay,
                null);

            target.OverrideDefaultValue(typeof(Class2), "Bar");

            Assert.Equal("Bar", target.GetDefaultValue<Class2>());
        }

        [Fact]
        public void Initialized_Observable_Fired()
        {
            bool invoked = false;

            Class1.FooProperty.Initialized.Subscribe(x =>
            {
                Assert.Equal(PerspexProperty.UnsetValue, x.OldValue);
                Assert.Equal("default", x.NewValue);
                Assert.Equal(BindingPriority.Unset, x.Priority);
                invoked = true;
            });

            var target = new Class1();

            Assert.True(invoked);
        }

        [Fact]
        public void Changed_Observable_Fired()
        {
            var target = new Class1();
            string value = null;

            Class1.FooProperty.Changed.Subscribe(x => value = (string)x.NewValue);
            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", value);
        }

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo", "default");
        }

        private class Class2 : Class1
        {
        }

        private class Class3
        {
        }
    }
}
