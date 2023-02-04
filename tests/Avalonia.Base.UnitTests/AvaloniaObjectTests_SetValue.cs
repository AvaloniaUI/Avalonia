using System;
using Avalonia.Data;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_SetValue
    {
        [Fact]
        public void ClearValue_Clears_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");
            target.ClearValue(Class1.FooProperty);

            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void ClearValue_Resets_Value_To_Style_value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "style", BindingPriority.Style);
            target.SetValue(Class1.FooProperty, "local");

            Assert.Equal("local", target.GetValue(Class1.FooProperty));

            target.ClearValue(Class1.FooProperty);

            Assert.Equal("style", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void ClearValue_Raises_PropertyChanged()
        {
            Class1 target = new Class1();
            var raised = 0;

            target.SetValue(Class1.FooProperty, "newvalue");
            target.PropertyChanged += (s, e) =>
            {
                Assert.Same(target, s);
                Assert.Equal(BindingPriority.Unset, e.Priority);
                Assert.Equal(Class1.FooProperty, e.Property);
                Assert.Equal("newvalue", (string)e.OldValue);
                Assert.Equal("foodefault", (string)e.NewValue);
                ++raised;
            };

            target.ClearValue(Class1.FooProperty);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void IsSet_Returns_False_For_Unset_Property()
        {
            var target = new Class1();

            Assert.False(target.IsSet(Class1.FooProperty));
        }

        [Fact]
        public void IsSet_Returns_False_For_Set_Property()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "foo");

            Assert.True(target.IsSet(Class1.FooProperty));
        }

        [Fact]
        public void IsSet_Returns_False_For_Cleared_Property()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "foo");
            target.SetValue(Class1.FooProperty, AvaloniaProperty.UnsetValue);

            Assert.False(target.IsSet(Class1.FooProperty));
        }

        [Fact]
        public void SetValue_Sets_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void SetValue_Sets_Attached_Value()
        {
            Class2 target = new Class2();

            target.SetValue(AttachedOwner.AttachedProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(AttachedOwner.AttachedProperty));
        }

        [Fact]
        public void SetValue_Raises_PropertyChanged()
        {
            Class1 target = new Class1();
            bool raised = false;

            target.PropertyChanged += (s, e) =>
            {
                raised = s == target &&
                         e.Property == Class1.FooProperty &&
                         (string)e.OldValue == "foodefault" &&
                         (string)e.NewValue == "newvalue";
            };

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.True(raised);
        }

        [Fact]
        public void SetValue_Style_Priority_Raises_PropertyChanged()
        {
            Class1 target = new Class1();
            bool raised = false;

            target.PropertyChanged += (s, e) =>
            {
                raised = s == target &&
                         e.Property == Class1.FooProperty &&
                         (string)e.OldValue == "foodefault" &&
                         (string)e.NewValue == "newvalue";
            };

            target.SetValue(Class1.FooProperty, "newvalue", BindingPriority.Style);

            Assert.True(raised);
        }

        [Fact]
        public void SetValue_Doesnt_Raise_PropertyChanged_If_Value_Not_Changed()
        {
            Class1 target = new Class1();
            bool raised = false;

            target.SetValue(Class1.FooProperty, "bar");

            target.PropertyChanged += (s, e) =>
            {
                raised = true;
            };

            target.SetValue(Class1.FooProperty, "bar");

            Assert.False(raised);
        }

        [Fact]
        public void SetValue_Doesnt_Raise_PropertyChanged_If_Value_Not_Changed_From_Default()
        {
            Class1 target = new Class1();
            bool raised = false;

            target.PropertyChanged += (s, e) =>
            {
                raised = true;
            };

            target.SetValue(Class1.FooProperty, "foodefault");

            Assert.False(raised);
        }

        [Fact]
        public void SetValue_Allows_Setting_Unregistered_Property()
        {
            Class1 target = new Class1();

            Assert.False(AvaloniaPropertyRegistry.Instance.IsRegistered(target, Class2.BarProperty));

            target.SetValue(Class2.BarProperty, "bar");

            Assert.Equal("bar", target.GetValue(Class2.BarProperty));
        }

        [Fact]
        public void SetValue_Allows_Setting_Unregistered_Attached_Property()
        {
            Class1 target = new Class1();

            Assert.False(AvaloniaPropertyRegistry.Instance.IsRegistered(target, AttachedOwner.AttachedProperty));

            target.SetValue(AttachedOwner.AttachedProperty, "bar");

            Assert.Equal("bar", target.GetValue(AttachedOwner.AttachedProperty));
        }

        [Fact]
        public void SetValue_Throws_Exception_For_Invalid_Value_Type()
        {
            Class1 target = new Class1();

            Assert.Throws<ArgumentException>(() =>
            {
                target.SetValue(Class1.FooProperty, 123);
            });
        }

        [Fact]
        public void SetValue_Of_Integer_On_Double_Property_Works()
        {
            Class2 target = new Class2();

            target.SetValue((AvaloniaProperty)Class2.FlobProperty, 4);

            var value = target.GetValue(Class2.FlobProperty);
            Assert.IsType<double>(value);
            Assert.Equal(4, value);
        }

        [Fact]
        public void SetValue_Respects_Implicit_Conversions()
        {
            Class2 target = new Class2();

            target.SetValue((AvaloniaProperty)Class2.FlobProperty, new ImplicitDouble(4));

            var value = target.GetValue(Class2.FlobProperty);
            Assert.IsType<double>(value);
            Assert.Equal(4, value);
        }

        [Fact]
        public void SetValue_Can_Convert_To_Nullable()
        {
            Class2 target = new Class2();

            target.SetValue((AvaloniaProperty)Class2.FredProperty, 4.0);

            var value = target.GetValue(Class2.FredProperty);
            Assert.IsType<double>(value);
            Assert.Equal(4, value);
        }

        [Fact]
        public void SetValue_Respects_Priority()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "one", BindingPriority.Template);
            Assert.Equal("one", target.GetValue(Class1.FooProperty));
            target.SetValue(Class1.FooProperty, "two", BindingPriority.Style);
            Assert.Equal("one", target.GetValue(Class1.FooProperty));
            target.SetValue(Class1.FooProperty, "three", BindingPriority.StyleTrigger);
            Assert.Equal("three", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void SetValue_Style_Doesnt_Override_LocalValue()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "one", BindingPriority.LocalValue);
            Assert.Equal("one", target.GetValue(Class1.FooProperty));
            target.SetValue(Class1.FooProperty, "two", BindingPriority.Style);
            Assert.Equal("one", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void SetValue_LocalValue_Overrides_Style()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "one", BindingPriority.Style);
            Assert.Equal("one", target.GetValue(Class1.FooProperty));
            target.SetValue(Class1.FooProperty, "two", BindingPriority.LocalValue);
            Assert.Equal("two", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void SetValue_Animation_Overrides_LocalValue()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "one", BindingPriority.LocalValue);
            Assert.Equal("one", target.GetValue(Class1.FooProperty));
            target.SetValue(Class1.FooProperty, "two", BindingPriority.Animation);
            Assert.Equal("two", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Setting_UnsetValue_Reverts_To_Default_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");
            target.SetValue(Class1.FooProperty, AvaloniaProperty.UnsetValue);

            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Setting_Object_Property_To_UnsetValue_Reverts_To_Default_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FrankProperty, "newvalue");
            target.SetValue(Class1.FrankProperty, AvaloniaProperty.UnsetValue);

            Assert.Equal("Kups", target.GetValue(Class1.FrankProperty));
        }

        [Fact]
        public void Setting_Object_Property_To_DoNothing_Does_Nothing()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FrankProperty, "newvalue");
            target.SetValue(Class1.FrankProperty, BindingOperations.DoNothing);

            Assert.Equal("newvalue", target.GetValue(Class1.FrankProperty));
        }

        [Fact]
        public void Disposing_Style_SetValue_Reverts_To_DefaultValue()
        {
            Class1 target = new Class1();

            var d = target.SetValue(Class1.FooProperty, "foo", BindingPriority.Style);
            d.Dispose();

            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Disposing_Style_SetValue_Reverts_To_Previous_Style_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "foo", BindingPriority.Style);
            var d = target.SetValue(Class1.FooProperty, "bar", BindingPriority.Style);
            d.Dispose();

            Assert.Equal("foo", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Disposing_Animation_SetValue_Reverts_To_Previous_Local_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "foo", BindingPriority.LocalValue);
            var d = target.SetValue(Class1.FooProperty, "bar", BindingPriority.Animation);
            d.Dispose();

            Assert.Equal("foo", target.GetValue(Class1.FooProperty));
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "foodefault");

            public static readonly StyledProperty<object> FrankProperty =
                AvaloniaProperty.Register<Class1, object>("Frank", "Kups");
        }

        private class Class2 : Class1
        {
            public static readonly StyledProperty<string> BarProperty =
                AvaloniaProperty.Register<Class2, string>("Bar", "bardefault");

            public static readonly StyledProperty<double> FlobProperty =
                AvaloniaProperty.Register<Class2, double>("Flob");

            public static readonly StyledProperty<double?> FredProperty =
                AvaloniaProperty.Register<Class2, double?>("Fred");

            public Class1 Parent
            {
                get { return (Class1)InheritanceParent; }
                set { InheritanceParent = value; }
            }
        }

        private class AttachedOwner
        {
            public static readonly AttachedProperty<string> AttachedProperty =
                AvaloniaProperty.RegisterAttached<AttachedOwner, Class2, string>("Attached");
        }

        private class ImplicitDouble
        {
            public ImplicitDouble(double value)
            {
                Value = value;
            }

            public double Value { get; }

            public static implicit operator double (ImplicitDouble v)
            {
                return v.Value;
            }
        }
    }
}
