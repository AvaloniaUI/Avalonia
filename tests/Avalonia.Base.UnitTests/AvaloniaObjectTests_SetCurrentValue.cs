using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Animation;
using Avalonia.Base.UnitTests.Animation;
using Avalonia.Data;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_SetCurrentValue
    {
        private const string NewValue = "newvalue";
        private const string OtherValue = "otherValue";

        [Fact]
        public void SetCurrentValue_Sets_Value()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, NewValue);

            Assert.Equal(NewValue, target.Foo);
        }

        [Fact]
        public void ClearValue_Clears_CurrentValue()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, NewValue);
            target.ClearValue(Class1.FooProperty);

            Assert.Equal(Class1.FooProperty.GetDefaultValue(typeof(Class1)), target.Foo);
        }

        [Fact]
        public void Setting_UnsetValue_Reverts_To_Default_Value()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, NewValue);
            target.SetCurrentValue(Class1.FooProperty, AvaloniaProperty.UnsetValue);

            Assert.Equal(Class1.FooProperty.GetDefaultValue(typeof(Class1)), target.Foo);
            Assert.Equal(BindingPriority.Unset, target.GetDiagnosticInternal(Class1.InheritedProperty).Priority);
        }

        [Fact]
        public void CurrentValue_Inherited()
        {
            var parent = new Class1();
            var target = new Class1()
            {
                InheritanceParent = parent,
            };

            parent.SetCurrentValue(Class1.InheritedProperty, NewValue);

            Assert.Equal(NewValue, target.Inherited);
        }

        [Fact]
        public void Inherited_Property_Upgraded_To_Internal()
        {
            var parent = new Class1();
            var target = new Class1()
            {
                InheritanceParent = parent,
            };

            parent.SetCurrentValue(Class1.InheritedProperty, NewValue);

            Assert.Equal(BindingPriority.Inherited, target.GetDiagnosticInternal(Class1.InheritedProperty).Priority);

            target.SetCurrentValue(Class1.InheritedProperty, OtherValue);

            Assert.Equal(OtherValue, target.Inherited);
            Assert.Equal(BindingPriority.Internal, target.GetDiagnosticInternal(Class1.InheritedProperty).Priority);
        }

        [Fact]
        public void Unset_Property_Upgraded_To_Internal()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, NewValue);

            Assert.Equal(BindingPriority.Internal, target.GetDiagnosticInternal(Class1.FooProperty).Priority);
        }

        [Fact]
        public void Setting_Object_Property_To_DoNothing_Does_Nothing()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FrankProperty, NewValue);
            target.SetCurrentValue(Class1.FrankProperty, BindingOperations.DoNothing);

            Assert.Equal(NewValue, target.Frank);
        }

        [Fact]
        public void Setting_Higher_Priority_Value_Clears_CurrentValue()
        {
            var target = new Class1();

            target.SetValue(Class1.FrankProperty, NewValue, BindingPriority.Style);

            target.SetCurrentValue(Class1.FrankProperty, OtherValue);

            var undoSet = target.SetValue(Class1.FrankProperty, "StyleTriggerValue", BindingPriority.StyleTrigger);

            undoSet!.Dispose();

            Assert.Equal(NewValue, target.Frank);
        }

        [Fact]
        public void CurrentValue_Overwritten_By_Animation()
        {
            var animatable = new TestAnimatable();
            var clock = new TestClock();
            var duration = TimeSpan.FromSeconds(1);

            var transition = new DoubleTransition
            {
                Duration = duration,
                Property = TestAnimatable.NumericProperty,
            };

            transition.Apply(animatable, clock, 0.0, 1.0);

            clock.Step(TimeSpan.Zero);
            clock.Step(duration * 0.5);

            Assert.Equal(0.5, animatable.Numeric);

            animatable.SetCurrentValue(TestAnimatable.NumericProperty, 256);

            Assert.Equal(256, animatable.Numeric);

            clock.Step(duration * 0.75);

            Assert.Equal(0.75, animatable.Numeric);
        }

        [Theory, MemberData(nameof(WriteableBindingPriorities))]
        public void Binding_Update_Clears_CurrentValue(BindingPriority priority)
        {
            var target = new Class1();
            var source = new Class1();

            target.Bind(Class1.FrankProperty, source.GetBindingObservable(Class1.FrankProperty), priority);

            target.SetCurrentValue(Class1.FrankProperty, OtherValue);

            source.SetValue(Class1.FrankProperty, NewValue);
            
            Assert.Equal(NewValue, target.Frank);
        }

        [Theory, MemberData(nameof(WriteableBindingPriorities))]
        public void SetCurrentValue_Sets_Value_Without_Changing_Priority(BindingPriority priority)
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, priority.ToString(), priority);

            target.SetCurrentValue(Class1.FooProperty, NewValue);

            Assert.Equal(NewValue, target.Foo);

            Assert.Equal(priority, target.GetDiagnosticInternal(Class1.FooProperty).Priority);
        }

        [Theory, MemberData(nameof(WriteableBindingPriorities))]
        public void SetCurrentValue_Sets_Value_Without_Terminating_Bindings(BindingPriority priority)
        {
            var target = new Class1();
            var source = new Class1();

            source.SetValue(Class1.FrankProperty, NewValue);
            target.Bind(Class1.FrankProperty, source.GetObservable(Class1.FrankProperty), priority);

            Assert.Equal(NewValue, target.Frank);

            target.SetCurrentValue(Class1.FrankProperty, OtherValue);

            Assert.Equal(OtherValue, target.Frank);

            source.SetValue(Class1.FrankProperty, "thirdValue");

            Assert.Equal("thirdValue", target.Frank);
        }

        public static TheoryData<BindingPriority> WriteableBindingPriorities()
        {
            var data = new TheoryData<BindingPriority>();
            foreach (var value in Enum.GetValues<BindingPriority>())
            {
                if (value is BindingPriority.Unset or BindingPriority.Inherited)
                {
                    continue;
                }
                data.Add(value);
            }
            return data;
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Foo), "foodefault");

            public static readonly StyledProperty<object> FrankProperty =
                AvaloniaProperty.Register<Class1, object>(nameof(Frank), "Kups");

            public static readonly StyledProperty<string> InheritedProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Inherited), inherits: true);

            public string Foo => GetValue(FooProperty);
            public object Frank => GetValue(FrankProperty);
            public object Inherited => GetValue(InheritedProperty);
        }

        private class TestAnimatable : Animatable
        {
            public static readonly StyledProperty<double> NumericProperty =
                AvaloniaProperty.Register<Class1, double>(nameof(Numeric), -1);

            public double Numeric => GetValue(NumericProperty);
        }
    }
}
