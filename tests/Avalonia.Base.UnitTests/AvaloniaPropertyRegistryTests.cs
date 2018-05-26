// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Reactive.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaPropertyRegistryTests
    {
        public AvaloniaPropertyRegistryTests(ITestOutputHelper s)
        {
            // Ensure properties are registered.
            AvaloniaProperty p;
            p = Class1.FooProperty;
            p = Class2.BarProperty;
            p = AttachedOwner.AttachedProperty;
        }

        [Fact]
        public void GetRegistered_Returns_Registered_Properties()
        {
            string[] names = AvaloniaPropertyRegistry.Instance.GetRegistered(typeof(Class1))
                .Select(x => x.Name)
                .ToArray();

            Assert.Equal(new[] { "Foo", "Baz", "Qux" }, names);
        }

        [Fact]
        public void GetRegistered_Returns_Registered_Properties_For_Base_Types()
        {
            string[] names = AvaloniaPropertyRegistry.Instance.GetRegistered(typeof(Class2))
                .Select(x => x.Name)
                .ToArray();

            Assert.Equal(new[] { "Bar", "Flob", "Fred", "Foo", "Baz", "Qux" }, names);
        }

        [Fact]
        public void GetRegisteredAttached_Returns_Registered_Properties()
        {
            string[] names = AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(typeof(Class1))
                .Select(x => x.Name)
                .ToArray();

            Assert.Equal(new[] { "Attached" }, names);
        }

        [Fact]
        public void GetRegisteredAttached_Returns_Registered_Properties_For_Base_Types()
        {
            string[] names = AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(typeof(Class2))
                .Select(x => x.Name)
                .ToArray();

            Assert.Equal(new[] { "Attached" }, names);
        }

        [Fact]
        public void FindRegistered_Finds_Property()
        {
            var result = AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(Class1), "Foo");

            Assert.Equal(Class1.FooProperty, result);
        }

        [Fact]
        public void FindRegistered_Doesnt_Find_Nonregistered_Property()
        {
            var result = AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(Class1), "Bar");

            Assert.Null(result);
        }

        [Fact]
        public void FindRegistered_Finds_Unqualified_Attached_Property_On_Registering_Type()
        {
            var result = AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(AttachedOwner), "Attached");

            Assert.Same(AttachedOwner.AttachedProperty, result);
        }

        [Fact]
        public void FindRegistered_Finds_AddOwnered_Attached_Property()
        {
            var result = AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(Class3), "Attached");

            Assert.Same(AttachedOwner.AttachedProperty, result);
        }

        [Fact]
        public void FindRegistered_Doesnt_Find_Non_AddOwnered_Attached_Property()
        {
            var result = AvaloniaPropertyRegistry.Instance.FindRegistered(typeof(Class2), "Attached");

            Assert.Null(result);
        }

        [Fact]
        public void FindRegisteredAttached_Finds_Property()
        {
            var result = AvaloniaPropertyRegistry.Instance.FindRegisteredAttached(
                typeof(Class1),
                typeof(AttachedOwner),
                "Attached");

            Assert.Equal(AttachedOwner.AttachedProperty, result);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo");

            public static readonly StyledProperty<string> BazProperty =
                AvaloniaProperty.Register<Class1, string>("Baz");

            public static readonly StyledProperty<int> QuxProperty =
                AvaloniaProperty.Register<Class1, int>("Qux");
        }

        private class Class2 : Class1
        {
            public static readonly StyledProperty<string> BarProperty =
                AvaloniaProperty.Register<Class2, string>("Bar");

            public static readonly StyledProperty<double> FlobProperty =
                AvaloniaProperty.Register<Class2, double>("Flob");

            public static readonly StyledProperty<double?> FredProperty =
                AvaloniaProperty.Register<Class2, double?>("Fred");
        }

        private class Class3 : Class1
        {
            public static readonly AttachedProperty<string> AttachedProperty =
                AttachedOwner.AttachedProperty.AddOwner<Class3>();
        }

        private class AttachedOwner : Class1
        {
            public static readonly AttachedProperty<string> AttachedProperty =
                AvaloniaProperty.RegisterAttached<AttachedOwner, Class1, string>("Attached");
        }

        private class AttachedOwner2 : AttachedOwner
        {
        }
    }
}
