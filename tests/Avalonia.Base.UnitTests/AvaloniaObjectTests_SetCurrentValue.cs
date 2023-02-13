using System;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Reactive;
using Xunit;
using Observable = Avalonia.Reactive.Observable;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_SetCurrentValue
    {
        [Fact]
        public void SetCurrentValue_Sets_Unset_Value()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.FooProperty));
            Assert.Equal(BindingPriority.Unset, GetPriority(target, Class1.FooProperty));
            Assert.True(IsOverridden(target, Class1.FooProperty));
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        [InlineData(BindingPriority.Animation)]
        public void SetCurrentValue_Overrides_Existing_Value(BindingPriority priority)
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "oldvalue", priority);
            target.SetCurrentValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.FooProperty));
            Assert.Equal(priority, GetPriority(target, Class1.FooProperty));
            Assert.True(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void SetCurrentValue_Overrides_Inherited_Value()
        {
            var parent = new Class1();
            var target = new Class1 { InheritanceParent = parent };

            parent.SetValue(Class1.InheritedProperty, "inheritedvalue");
            target.SetCurrentValue(Class1.InheritedProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.InheritedProperty));
            Assert.Equal(BindingPriority.Unset, GetPriority(target, Class1.InheritedProperty));
            Assert.True(IsOverridden(target, Class1.InheritedProperty));
        }

        [Fact]
        public void SetCurrentValue_Is_Inherited()
        {
            var parent = new Class1();
            var target = new Class1 { InheritanceParent = parent };

            parent.SetCurrentValue(Class1.InheritedProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.InheritedProperty));
            Assert.Equal(BindingPriority.Inherited, GetPriority(target, Class1.InheritedProperty));
            Assert.False(IsOverridden(target, Class1.InheritedProperty));
        }

        [Fact]
        public void ClearValue_Clears_CurrentValue_With_Unset_Priority()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, "newvalue");
            target.ClearValue(Class1.FooProperty);

            Assert.Equal("foodefault", target.Foo);
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void ClearValue_Clears_CurrentValue_With_Inherited_Priority()
        {
            var parent = new Class1();
            var target = new Class1 { InheritanceParent = parent };

            parent.SetValue(Class1.InheritedProperty, "inheritedvalue");
            target.SetCurrentValue(Class1.InheritedProperty, "newvalue");
            target.ClearValue(Class1.InheritedProperty);

            Assert.Equal("inheritedvalue", target.Inherited);
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void ClearValue_Clears_CurrentValue_With_LocalValue_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "localvalue");
            target.SetCurrentValue(Class1.FooProperty, "newvalue");
            target.ClearValue(Class1.FooProperty);

            Assert.Equal("foodefault", target.Foo);
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void ClearValue_Clears_CurrentValue_With_Style_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "stylevalue", BindingPriority.Style);
            target.SetCurrentValue(Class1.FooProperty, "newvalue");
            target.ClearValue(Class1.FooProperty);

            Assert.Equal("stylevalue", target.Foo);
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void SetCurrentValue_Can_Be_Coerced()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.CoercedProperty, 60);
            Assert.Equal(60, target.GetValue(Class1.CoercedProperty));

            target.CoerceMax = 50;
            target.CoerceValue(Class1.CoercedProperty);
            Assert.Equal(50, target.GetValue(Class1.CoercedProperty));

            target.CoerceMax = 100;
            target.CoerceValue(Class1.CoercedProperty);
            Assert.Equal(60, target.GetValue(Class1.CoercedProperty));
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        [InlineData(BindingPriority.Animation)]
        public void SetValue_Overrides_CurrentValue_With_Unset_Priority(BindingPriority priority)
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, "current");
            target.SetValue(Class1.FooProperty, "setvalue", priority);

            Assert.Equal("setvalue", target.Foo);
            Assert.Equal(priority, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void Animation_Value_Overrides_CurrentValue_With_LocalValue_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "localvalue");
            target.SetCurrentValue(Class1.FooProperty, "current");
            target.SetValue(Class1.FooProperty, "setvalue", BindingPriority.Animation);

            Assert.Equal("setvalue", target.Foo);
            Assert.Equal(BindingPriority.Animation, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void StyleTrigger_Value_Overrides_CurrentValue_With_Style_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "style", BindingPriority.Style);
            target.SetCurrentValue(Class1.FooProperty, "current");
            target.SetValue(Class1.FooProperty, "setvalue", BindingPriority.StyleTrigger);

            Assert.Equal("setvalue", target.Foo);
            Assert.Equal(BindingPriority.StyleTrigger, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        [InlineData(BindingPriority.Animation)]
        public void Binding_Overrides_CurrentValue_With_Unset_Priority(BindingPriority priority)
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, "current");
            
            var s = target.Bind(Class1.FooProperty, Observable.SingleValue("binding"), priority);

            Assert.Equal("binding", target.Foo);
            Assert.Equal(priority, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));

            s.Dispose();

            Assert.Equal("foodefault", target.Foo);
        }

        [Fact]
        public void Animation_Binding_Overrides_CurrentValue_With_LocalValue_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "localvalue");
            target.SetCurrentValue(Class1.FooProperty, "current");

            var s = target.Bind(Class1.FooProperty, Observable.SingleValue("binding"), BindingPriority.Animation);

            Assert.Equal("binding", target.Foo);
            Assert.Equal(BindingPriority.Animation, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));

            s.Dispose();

            Assert.Equal("current", target.Foo);
        }

        [Fact]
        public void StyleTrigger_Binding_Overrides_CurrentValue_With_Style_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "style", BindingPriority.Style);
            target.SetCurrentValue(Class1.FooProperty, "current");
            
            var s = target.Bind(Class1.FooProperty, Observable.SingleValue("binding"), BindingPriority.StyleTrigger);

            Assert.Equal("binding", target.Foo);
            Assert.Equal(BindingPriority.StyleTrigger, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));

            s.Dispose();

            Assert.Equal("style", target.Foo);
        }

        private BindingPriority GetPriority(AvaloniaObject target, AvaloniaProperty property)
        {
            return target.GetDiagnostic(property).Priority;
        }

        private bool IsOverridden(AvaloniaObject target, AvaloniaProperty property)
        {
            return target.GetDiagnostic(property).IsOverriddenCurrentValue;
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Foo), "foodefault");
            public static readonly StyledProperty<string> InheritedProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Inherited), "inheriteddefault", inherits: true);
            public static readonly StyledProperty<double> CoercedProperty =
                AvaloniaProperty.Register<Class1, double>(nameof(Coerced), coerce: Coerce);

            public string Foo => GetValue(FooProperty);
            public string Inherited => GetValue(InheritedProperty);
            public double Coerced => GetValue(CoercedProperty);
            public double CoerceMax { get; set; } = 100;

            private static double Coerce(AvaloniaObject sender, double value)
            {
                return Math.Min(value, ((Class1)sender).CoerceMax);
            }
        }
    }
}
