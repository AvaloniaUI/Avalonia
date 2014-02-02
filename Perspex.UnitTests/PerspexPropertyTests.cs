// -----------------------------------------------------------------------
// <copyright file="PerspexPropertyTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests
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
                false);

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
                false);

            Assert.AreEqual("Foo", target.GetDefaultValue<Class1>());
        }

        [TestMethod]
        public void GetDefaultValue_Returns_Registered_Value_For_Not_Overridden_Class()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class1),
                "Foo",
                false);

            Assert.AreEqual("Foo", target.GetDefaultValue<Class2>());
        }

        [TestMethod]
        public void GetDefaultValue_Returns_Registered_Value_For_Unrelated_Class()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class3),
                "Foo",
                false);

            Assert.AreEqual("Foo", target.GetDefaultValue<Class2>());
        }

        [TestMethod]
        public void GetDefaultValue_Returns_Overridden_Value()
        {
            PerspexProperty<string> target = new PerspexProperty<string>(
                "test",
                typeof(Class1),
                "Foo",
                false);

            target.OverrideDefaultValue(typeof(Class2), "Bar");

            Assert.AreEqual("Bar", target.GetDefaultValue<Class2>());
        }

        private class Class1 : PerspexObject
        {
        }

        private class Class2 : Class1
        {
        }

        private class Class3
        {
        }
    }
}
