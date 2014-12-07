// -----------------------------------------------------------------------
// <copyright file="PerspexPropertyTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Base.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PerspexPropertyTests
    {
        [TestMethod]
        public void Constructor_Sets_Properties()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class1),
                "Foo",
                false,
                BindingMode.OneWay,
                null);

            Assert.AreEqual("test", target.Name);
            Assert.AreEqual(typeof(string), target.PropertyType);
            Assert.AreEqual(typeof(Class1), target.OwnerType);
            Assert.AreEqual(false, target.Inherits);
        }

        [TestMethod]
        public void GetDefaultValue_Returns_Registered_Value()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class1),
                "Foo",
                false,
                BindingMode.OneWay,
                null);

            Assert.AreEqual("Foo", target.GetDefaultValue<Class1>());
        }

        [TestMethod]
        public void GetDefaultValue_Returns_Registered_Value_For_Not_Overridden_Class()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class1),
                "Foo",
                false,
                BindingMode.OneWay,
                null);

            Assert.AreEqual("Foo", target.GetDefaultValue<Class2>());
        }

        [TestMethod]
        public void GetDefaultValue_Returns_Registered_Value_For_Unrelated_Class()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class3),
                "Foo",
                false,
                BindingMode.OneWay,
                null);

            Assert.AreEqual("Foo", target.GetDefaultValue<Class2>());
        }

        [TestMethod]
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

            Assert.AreEqual("Bar", target.GetDefaultValue<Class2>());
        }

        [TestMethod]
        public void Initialized_Observable_Fired()
        {
            string value = null;

            Class1.FooProperty.Initialized.Subscribe(x => value = (string)x.NewValue);
            var target = new Class1();

            Assert.AreEqual("default", value);
        }

        [TestMethod]
        public void Changed_Observable_Fired()
        {
            var target = new Class1();
            string value = null;

            Class1.FooProperty.Changed.Subscribe(x => value = (string)x.NewValue);
            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.AreEqual("newvalue", value);
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
