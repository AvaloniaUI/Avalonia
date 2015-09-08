// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Reactive.Linq;
using Xunit;

namespace Perspex.Base.UnitTests
{
    public class PerspexObjectTests_Metadata
    {
        public PerspexObjectTests_Metadata()
        {
            // Ensure properties are registered.
            PerspexProperty p;
            p = Class1.FooProperty;
            p = Class2.BarProperty;
        }

        [Fact]
        public void GetProperties_Returns_Registered_Properties()
        {
            string[] names = PerspexObject.GetProperties(typeof(Class1)).Select(x => x.Name).ToArray();

            Assert.Equal(new[] { "Foo", "Baz", "Qux" }, names);
        }

        [Fact]
        public void GetProperties_Returns_Registered_Properties_For_Base_Types()
        {
            string[] names = PerspexObject.GetProperties(typeof(Class2)).Select(x => x.Name).ToArray();

            Assert.Equal(new[] { "Bar", "Flob", "Fred", "Foo", "Baz", "Qux" }, names);
        }

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo");

            public static readonly PerspexProperty<string> BazProperty =
                PerspexProperty.Register<Class1, string>("Baz");

            public static readonly PerspexProperty<int> QuxProperty =
                PerspexProperty.Register<Class1, int>("Qux");
        }

        private class Class2 : Class1
        {
            public static readonly PerspexProperty<string> BarProperty =
                PerspexProperty.Register<Class2, string>("Bar");

            public static readonly PerspexProperty<double> FlobProperty =
                PerspexProperty.Register<Class2, double>("Flob");

            public static readonly PerspexProperty<double?> FredProperty =
                PerspexProperty.Register<Class2, double?>("Fred");
        }
    }
}
