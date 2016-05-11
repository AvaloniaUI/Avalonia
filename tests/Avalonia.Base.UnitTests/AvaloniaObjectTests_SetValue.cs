// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        public void SetValue_Sets_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.FooProperty));
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
        public void SetValue_Throws_Exception_For_Unregistered_Property()
        {
            Class1 target = new Class1();

            Assert.Throws<ArgumentException>(() =>
            {
                target.SetValue(Class2.BarProperty, "invalid");
            });
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

            target.SetValue((AvaloniaProperty)Class2.FlobProperty, new ImplictDouble(4));

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

            target.SetValue(Class1.FooProperty, "one", BindingPriority.TemplatedParent);
            Assert.Equal("one", target.GetValue(Class1.FooProperty));
            target.SetValue(Class1.FooProperty, "two", BindingPriority.Style);
            Assert.Equal("one", target.GetValue(Class1.FooProperty));
            target.SetValue(Class1.FooProperty, "three", BindingPriority.StyleTrigger);
            Assert.Equal("three", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Setting_UnsetValue_Reverts_To_Default_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");
            target.SetValue(Class1.FooProperty, AvaloniaProperty.UnsetValue);

            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "foodefault");
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

        private class ImplictDouble
        {
            public ImplictDouble(double value)
            {
                Value = value;
            }

            public double Value { get; }

            public static implicit operator double (ImplictDouble v)
            {
                return v.Value;
            }
        }
    }
}
