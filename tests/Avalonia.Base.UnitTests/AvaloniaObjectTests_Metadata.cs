using System.Runtime.CompilerServices;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Metadata
    {
        public AvaloniaObjectTests_Metadata()
        {
            // Ensure properties are registered.
            RuntimeHelpers.RunClassConstructor(typeof(Class1).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(Class2).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(Class3).TypeHandle);
        }

        public class StyledProperty : AvaloniaObjectTests_Metadata
        {
            [Fact]
            public void Default_Value_Can_Be_Overridden_In_Derived_Class()
            {
                var baseValue = Class1.StyledProperty.GetDefaultValue(typeof(Class1));
                var derivedValue = Class1.StyledProperty.GetDefaultValue(typeof(Class2));

                Assert.Equal("foo", baseValue);
                Assert.Equal("bar", derivedValue);
            }

            [Fact]
            public void Default_Value_Can_Be_Overridden_In_AddOwnered_Property()
            {
                var baseValue = Class1.StyledProperty.GetDefaultValue(typeof(Class1));
                var addOwneredValue = Class1.StyledProperty.GetDefaultValue(typeof(Class3));

                Assert.Equal("foo", baseValue);
                Assert.Equal("baz", addOwneredValue);
            }
        }

        public class DirectProperty : AvaloniaObjectTests_Metadata
        {
            [Fact]
            public void Unset_Value_Can_Be_Overridden_In_Derived_Class()
            {
                var baseValue = Class1.DirectProperty.GetUnsetValue(typeof(Class1));
                var derivedValue = Class1.DirectProperty.GetUnsetValue(typeof(Class2));

                Assert.Equal("foo", baseValue);
                Assert.Equal("bar", derivedValue);
            }

            [Fact]
            public void Unset_Value_Can_Be_Overridden_In_AddOwnered_Property()
            {
                var baseValue = Class1.DirectProperty.GetUnsetValue(typeof(Class1));
                var addOwneredValue = Class3.DirectProperty.GetUnsetValue(typeof(Class3));

                Assert.Equal("foo", baseValue);
                Assert.Equal("baz", addOwneredValue);
            }
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> StyledProperty =
                AvaloniaProperty.Register<Class1, string>("Styled", "foo");

            public static readonly DirectProperty<Class1, string> DirectProperty =
                AvaloniaProperty.RegisterDirect<Class1, string>("Styled", o => o.Direct, unsetValue: "foo");

            private string _direct = default;

            public string Direct
            {
                get => _direct;
            }
        }

        private class Class2 : Class1
        {
            static Class2()
            {
                StyledProperty.OverrideDefaultValue<Class2>("bar");
                DirectProperty.OverrideMetadata<Class2>(new DirectPropertyMetadata<string>("bar"));
            }
        }

        private class Class3 : AvaloniaObject
        {
            public static readonly StyledProperty<string> StyledProperty =
                Class1.StyledProperty.AddOwner<Class3>();

            public static readonly DirectProperty<Class3, string> DirectProperty =
                Class1.DirectProperty.AddOwner<Class3>(o => o.Direct, unsetValue: "baz");

            private string _direct = default;

            static Class3()
            {
                StyledProperty.OverrideDefaultValue<Class3>("baz");
            }

            public string Direct
            {
                get => _direct;
            }
        }
    }
}
