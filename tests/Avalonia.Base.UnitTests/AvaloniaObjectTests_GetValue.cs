using System;
using System.Reactive.Subjects;
using Avalonia.Data;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_GetValue
    {
        [Fact]
        public void GetValue_Returns_Default_Value()
        {
            Class1 target = new Class1();

            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Returns_Overridden_Default_Value()
        {
            Class2 target = new Class2();

            Assert.Equal("foooverride", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Returns_Set_Value()
        {
            var target = new Class1();
            var property = Class1.FooProperty;

            target.SetValue(property, "newvalue");

            Assert.Equal("newvalue", target.GetValue(property));
        }

        [Fact]
        public void GetValue_Returns_Bound_Value()
        {
            var target = new Class1();
            var property = Class1.FooProperty;

            target.Bind(property, new BehaviorSubject<string>("newvalue"));

            Assert.Equal("newvalue", target.GetValue(property));
        }

        [Fact]
        public void GetValue_Returns_Inherited_Value()
        {
            Class1 parent = new Class1();
            Class2 child = new Class2 { Parent = parent };

            parent.SetValue(Class1.BazProperty, "changed");

            Assert.Equal("changed", child.GetValue(Class1.BazProperty));
        }

        [Fact]
        public void GetValue_Doesnt_Throw_Exception_For_Unregistered_Property()
        {
            var target = new Class3();

            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetBaseValue_Ignores_Default_Value()
        {
            var target = new Class3();

            target.SetValue(Class1.FooProperty, "animated", BindingPriority.Animation);
            Assert.False(target.GetBaseValue(Class1.FooProperty).HasValue);
        }

        [Fact]
        public void GetBaseValue_Returns_Local_Value()
        {
            var target = new Class3();

            target.SetValue(Class1.FooProperty, "local");
            target.SetValue(Class1.FooProperty, "animated", BindingPriority.Animation);
            Assert.Equal("local", target.GetBaseValue(Class1.FooProperty).Value);
        }

        [Fact]
        public void GetBaseValue_Returns_Style_Value()
        {
            var target = new Class3();

            target.SetValue(Class1.FooProperty, "style", BindingPriority.Style);
            target.SetValue(Class1.FooProperty, "animated", BindingPriority.Animation);
            Assert.Equal("style", target.GetBaseValue(Class1.FooProperty).Value);
        }

        [Fact]
        public void GetBaseValue_Returns_Style_Value_Set_Via_Untyped_Setters()
        {
            var target = new Class3();

            target.SetValue(Class1.FooProperty, (object)"style", BindingPriority.Style);
            target.SetValue(Class1.FooProperty, (object)"animated", BindingPriority.Animation);
            Assert.Equal("style", target.GetBaseValue(Class1.FooProperty).Value);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "foodefault");

            public static readonly StyledProperty<string> BazProperty =
                AvaloniaProperty.Register<Class1, string>("Baz", "bazdefault", true);
        }

        private class Class2 : Class1
        {
            static Class2()
            {
                FooProperty.OverrideDefaultValue(typeof(Class2), "foooverride");
            }

            public Class1 Parent
            {
                get { return (Class1)InheritanceParent; }
                set { InheritanceParent = value; }
            }
        }

        private class Class3 : AvaloniaObject
        {
        }
    }
}
