using System;
using System.Reactive.Subjects;
using Avalonia.Data;
using Xunit;

#nullable enable

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

            target.Bind(property, new BehaviorSubject<BindingValue<string>>("newvalue"));

            Assert.Equal("newvalue", target.GetValue(property));
        }

        [Fact]
        public void GetValue_Doesnt_Throw_Exception_For_Unregistered_Property()
        {
            var target = new Class3();

            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValueByPriority_Style_LocalValue_Ignores_Default_Value()
        {
            var target = new Class3();
            var source = new BehaviorSubject<BindingValue<string>>("animated");

            target.Bind(Class1.FooProperty, source, BindingPriority.Animation);
            Assert.False(target.GetValueByPriority(Class1.FooProperty, BindingPriority.Style, BindingPriority.LocalValue).HasValue);
        }

        [Fact]
        public void GetValueByPriority_Style_LocalValue_Returns_Local_Value()
        {
            var target = new Class3();
            var source = new BehaviorSubject<BindingValue<string>>("animated");

            target.SetValue(Class1.FooProperty, "local");
            target.Bind(Class1.FooProperty, source, BindingPriority.Animation);
            Assert.Equal("local", target.GetValueByPriority(Class1.FooProperty, BindingPriority.Style, BindingPriority.LocalValue).Value);
        }

        [Fact]
        public void GetValueByPriority_Style_LocalValue_Returns_Style_Value()
        {
            var target = new Class3();
            var source1 = new BehaviorSubject<BindingValue<string>>("style");
            var source2 = new BehaviorSubject<BindingValue<string>>("animated");

            target.Bind(Class1.FooProperty, source1, BindingPriority.Style);
            target.Bind(Class1.FooProperty, source2, BindingPriority.Animation);
            Assert.Equal("style", target.GetValueByPriority(Class1.FooProperty, BindingPriority.Style, BindingPriority.LocalValue).Value);
        }

        [Fact]
        public void GetValueByPriority_LocalValue_LocalValue_Ignores_Style_Value()
        {
            var target = new Class3();
            var source = new BehaviorSubject<BindingValue<string>>("style");

            target.Bind(Class1.FooProperty, source, BindingPriority.Style);
            Assert.False(target.GetValueByPriority(Class1.FooProperty, BindingPriority.LocalValue, BindingPriority.LocalValue).HasValue);
        }

        [Fact]
        public void GetValueByPriority_Style_Style_Ignores_LocalValue_Animated_Value()
        {
            var target = new Class3();
            var source = new BehaviorSubject<BindingValue<string>>("animated");

            target.Bind(Class1.FooProperty, source, BindingPriority.Animation);
            target.SetValue(Class1.FooProperty, "local");
            Assert.False(target.GetValueByPriority(Class1.FooProperty, BindingPriority.Style, BindingPriority.Style).HasValue);
        }

        [Fact]
        public void GetValueByPriority_Style_Style_Returns_Style_Value()
        {
            var target = new Class3();
            var source = new BehaviorSubject<BindingValue<string>>("style");

            target.SetValue(Class1.FooProperty, "local");
            target.Bind(Class1.FooProperty, source, BindingPriority.Style);
            target.Bind(Class1.FooProperty, new BehaviorSubject<string>("animated"), BindingPriority.Animation);
            Assert.Equal("style", target.GetValueByPriority(Class1.FooProperty, BindingPriority.Style, BindingPriority.Style));
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
        }

        private class Class3 : AvaloniaObject
        {
        }
    }
}
