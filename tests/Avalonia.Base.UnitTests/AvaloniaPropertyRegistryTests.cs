using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;

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
        public void Registered_Properties_Count_Reflects_Newly_Added_Attached_Property()
        {
            var registry = new AvaloniaPropertyRegistry();
            var metadata = new StyledPropertyMetadata<int>();
            var property = new AttachedProperty<int>("test", typeof(object), typeof(object), metadata, true);
            registry.Register(typeof(object), property);
            registry.RegisterAttached(typeof(AvaloniaPropertyRegistryTests), property);
            property.AddOwner<Class4>();

            Assert.Equal(1, registry.Properties.Count);
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

            Assert.Contains("Attached", names);
        }

        [Fact]
        public void GetRegisteredAttached_Returns_Registered_Properties_For_Base_Types()
        {
            string[] names = AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(typeof(Class2))
                .Select(x => x.Name)
                .ToArray();

            Assert.Contains("Attached", names);
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
        public void UnregisterByModule_Removes_Properties_And_Invalidates_Surviving_Owner_Caches()
        {
            var registry = new AvaloniaPropertyRegistry();
            var owner = typeof(Class4);
            var host = typeof(Class1);
            var styled = new StyledProperty<int>("UnloadStyled", owner, owner, new(1));
            var attached = new AttachedProperty<int>("UnloadAttached", owner, host, new(2), inherits: true);
            var direct = new DirectProperty<Class4, int>("UnloadDirect", _ => 3, null, new(3));
            var surviving = new StyledProperty<int>("Surviving", host, host, new(4));
            registry.Register(owner, styled);
            registry.Register(host, styled);
            registry.Register(owner, attached);
            registry.RegisterAttached(host, attached);
            registry.Register(owner, direct);
            registry.Register(host, surviving);
            surviving.OverrideDefaultValue<Class4>(5);

            Assert.Contains(styled, registry.GetRegistered(host));
            Assert.Contains(attached, registry.GetRegisteredAttached(host));
            Assert.Contains(direct, registry.GetRegisteredDirect(owner));
            Assert.Contains(attached, registry.GetRegisteredInherited(host));
            Assert.Equal(5, surviving.GetDefaultValue(owner));

            Assert.True(registry.UnregisterByModule(new[] { owner }));
            Assert.True(registry.UnregisterByModule(new[] { owner }));

            Assert.Equal(new[] { surviving }, registry.Properties);
            Assert.Equal(new[] { surviving }, registry.GetRegistered(host));
            Assert.Empty(registry.GetRegisteredAttached(host));
            Assert.Empty(registry.GetRegisteredDirect(owner));
            Assert.Empty(registry.GetRegisteredInherited(host));
            Assert.Empty(registry.GetRegistered(owner));
            Assert.Equal(4, surviving.GetDefaultValue(owner));
            Assert.Null(registry.FindRegistered(styled.Id));
            Assert.Null(registry.FindRegistered(attached.Id));
            Assert.Null(registry.FindRegistered(direct.Id));
            Assert.Same(surviving, registry.FindRegistered(surviving.Id));
        }

        [Fact]
        public void UnregisterByModule_Releases_Collectible_Owner_Type()
        {
            var registry = new AvaloniaPropertyRegistry();
            var reference = RegisterAndUnregisterCollectibleType(registry);

            for (var i = 0; i < 10 && reference.IsAlive; ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.False(reference.IsAlive);
            GC.KeepAlive(registry);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference RegisterAndUnregisterCollectibleType(AvaloniaPropertyRegistry registry)
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("UnloadPropertyOwner"), AssemblyBuilderAccess.RunAndCollect);
            var type = assembly.DefineDynamicModule("Main").DefineType(
                "CollectibleOwner", TypeAttributes.Public, typeof(AvaloniaObject)).CreateType()!;
            var property = new AttachedProperty<int>("Collectible", type, typeof(AvaloniaObject), new(1), inherits: true);
            registry.Register(type, property);
            registry.RegisterAttached(typeof(AvaloniaObject), property);
            registry.GetRegistered(type);
            registry.GetRegisteredInherited(typeof(AvaloniaObject));

            // AddOwner creates a separate instance with the same ID. Its metadata cache must also be cleared.
            var direct = new DirectProperty<Class1, int>("SharedDirect", _ => 0, null, new());
            var copy = direct.AddOwner<Class4>(_ => 0);
            copy.OverrideMetadata(type, new DirectPropertyMetadata<int>(9));
            copy.GetMetadata(type);
            registry.Register(typeof(Class1), direct);
            registry.Register(typeof(Class4), copy);

            Assert.True(registry.UnregisterByModule(new[] { type }));
            Assert.Same(direct, registry.FindRegistered(direct.Id));
            Assert.Contains(copy, registry.GetRegisteredDirect(typeof(Class4)));
            return new WeakReference(type);
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

        private class Class4 : AvaloniaObject
        {
        }
    }
}
