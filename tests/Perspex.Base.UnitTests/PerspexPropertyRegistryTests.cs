// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Reactive.Linq;
using Xunit;

namespace Perspex.Base.UnitTests
{
    public class PerspexPropertyRegistryTests
    {
        public PerspexPropertyRegistryTests()
        {
            // Ensure properties are registered.
            PerspexProperty p;
            p = Class1.FooProperty;
            p = Class2.BarProperty;
            p = AttachedOwner.AttachedProperty;
        }

        [Fact]
        public void GetRegistered_Returns_Registered_Properties()
        {
            string[] names = PerspexPropertyRegistry.Instance.GetRegistered(typeof(Class1))
                .Select(x => x.Name)
                .ToArray();

            Assert.Equal(new[] { "Foo", "Baz", "Qux", "Attached", "ValidationStatus" }, names);
        }

        [Fact]
        public void GetRegistered_Returns_Registered_Properties_For_Base_Types()
        {
            string[] names = PerspexPropertyRegistry.Instance.GetRegistered(typeof(Class2))
                .Select(x => x.Name)
                .ToArray();

            Assert.Equal(new[] { "Bar", "Flob", "Fred", "Foo", "Baz", "Qux", "Attached", "ValidationStatus" }, names);
        }

        [Fact]
        public void GetAttached_Returns_Registered_Properties_For_Base_Types()
        {
            string[] names = PerspexPropertyRegistry.Instance.GetAttached(typeof(AttachedOwner)).Select(x => x.Name).ToArray();

            Assert.Equal(new[] { "Attached" }, names);
        }

        [Fact]
        public void FindRegistered_Finds_Untyped_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class1), "Foo");

            Assert.Equal(Class1.FooProperty, result);
        }

        [Fact]
        public void FindRegistered_Finds_Typed_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class1), "Class1.Foo");

            Assert.Equal(Class1.FooProperty, result);
        }

        [Fact]
        public void FindRegistered_Finds_Typed_Inherited_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class2), "Class1.Foo");

            Assert.Equal(Class2.FooProperty, result);
        }

        [Fact]
        public void FindRegistered_Finds_Inherited_Property_With_Derived_Type_Name()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class2), "Class2.Foo");

            Assert.Equal(Class2.FooProperty, result);
        }

        [Fact]
        public void FindRegistered_Finds_Attached_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class2), "AttachedOwner.Attached");

            Assert.Equal(AttachedOwner.AttachedProperty, result);
        }

        [Fact]
        public void FindRegistered_Doesnt_Finds_Unqualified_Attached_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class2), "Attached");

            Assert.Null(result);
        }

        [Fact]
        public void FindRegistered_Finds_Unqualified_Attached_Property_On_Registering_Type()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(AttachedOwner), "Attached");

            Assert.True(AttachedOwner.AttachedProperty == result);
        }

        [Fact]
        public void FindRegistered_Finds_AddOwnered_Untyped_Attached_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class3), "Attached");

            Assert.True(AttachedOwner.AttachedProperty == result);
        }

        [Fact]
        public void FindRegistered_Finds_AddOwnered_Typed_Attached_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class3), "Class3.Attached");

            Assert.True(AttachedOwner.AttachedProperty == result);
        }

        [Fact]
        public void FindRegistered_Finds_AddOwnered_AttachedTyped_Attached_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class3), "AttachedOwner.Attached");

            Assert.True(AttachedOwner.AttachedProperty == result);
        }

        [Fact]
        public void FindRegistered_Finds_AddOwnered_BaseTyped_Attached_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class3), "Class1.Attached");

            Assert.True(AttachedOwner.AttachedProperty == result);
        }

        [Fact]
        public void FindRegistered_Doesnt_Find_Nonregistered_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class1), "Bar");

            Assert.Null(result);
        }

        [Fact]
        public void FindRegistered_Doesnt_Find_Nonregistered_Attached_Property()
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(typeof(Class4), "AttachedOwner.Attached");

            Assert.Null(result);
        }

        private class Class1 : PerspexObject
        {
            public static readonly StyledProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo");

            public static readonly StyledProperty<string> BazProperty =
                PerspexProperty.Register<Class1, string>("Baz");

            public static readonly StyledProperty<int> QuxProperty =
                PerspexProperty.Register<Class1, int>("Qux");
        }

        private class Class2 : Class1
        {
            public static readonly StyledProperty<string> BarProperty =
                PerspexProperty.Register<Class2, string>("Bar");

            public static readonly StyledProperty<double> FlobProperty =
                PerspexProperty.Register<Class2, double>("Flob");

            public static readonly StyledProperty<double?> FredProperty =
                PerspexProperty.Register<Class2, double?>("Fred");
        }

        private class Class3 : Class1
        {
            public static readonly StyledProperty<string> AttachedProperty =
                AttachedOwner.AttachedProperty.AddOwner<Class3>();
        }

        public class Class4 : PerspexObject
        {
        }

        private class AttachedOwner : Class1
        {
            public static readonly AttachedProperty<string> AttachedProperty =
                PerspexProperty.RegisterAttached<AttachedOwner, Class1, string>("Attached");
        }
    }
}
