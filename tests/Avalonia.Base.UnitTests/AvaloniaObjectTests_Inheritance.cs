// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Inheritance
    {
        [Fact]
        public void GetValue_Returns_Inherited_Value()
        {
            Class1 parent = new Class1();
            Class2 child = new Class2 { Parent = parent };

            parent.SetValue(Class1.BazProperty, "changed");

            Assert.Equal("changed", child.GetValue(Class1.BazProperty));
        }

        [Fact]
        public void Setting_InheritanceParent_Raises_PropertyChanged_When_Value_Changed_In_Parent()
        {
            bool raised = false;

            Class1 parent = new Class1();
            parent.SetValue(Class1.BazProperty, "changed");

            Class2 child = new Class2();
            child.PropertyChanged += (s, e) =>
                raised = s == child &&
                         e.Property == Class1.BazProperty &&
                         (string)e.OldValue == "bazdefault" &&
                         (string)e.NewValue == "changed";

            child.Parent = parent;

            Assert.True(raised);
        }

        [Fact]
        public void Setting_InheritanceParent_Raises_PropertyChanged_For_Attached_Property_When_Value_Changed_In_Parent()
        {
            bool raised = false;

            Class1 parent = new Class1();
            parent.SetValue(AttachedOwner.AttachedProperty, "changed");

            Class2 child = new Class2();
            child.PropertyChanged += (s, e) =>
                raised = s == child &&
                         e.Property == AttachedOwner.AttachedProperty &&
                         (string)e.OldValue == null &&
                         (string)e.NewValue == "changed";

            child.Parent = parent;

            Assert.True(raised);
        }

        [Fact]
        public void Setting_InheritanceParent_Doesnt_Raise_PropertyChanged_When_Local_Value_Set()
        {
            bool raised = false;

            Class1 parent = new Class1();
            parent.SetValue(Class1.BazProperty, "changed");

            Class2 child = new Class2();
            child.SetValue(Class1.BazProperty, "localvalue");
            child.PropertyChanged += (s, e) => raised = true;

            child.Parent = parent;

            Assert.False(raised);
        }

        [Fact]
        public void Setting_Value_In_InheritanceParent_Raises_PropertyChanged()
        {
            bool raised = false;

            Class1 parent = new Class1();

            Class2 child = new Class2();
            child.PropertyChanged += (s, e) =>
                raised = s == child &&
                         e.Property == Class1.BazProperty &&
                         (string)e.OldValue == "bazdefault" &&
                         (string)e.NewValue == "changed";
            child.Parent = parent;

            parent.SetValue(Class1.BazProperty, "changed");

            Assert.True(raised);
        }

        [Fact]
        public void Setting_Value_Of_Attached_Property_In_InheritanceParent_Raises_PropertyChanged()
        {
            bool raised = false;

            Class1 parent = new Class1();

            Class2 child = new Class2();
            child.PropertyChanged += (s, e) =>
                raised = s == child &&
                         e.Property == AttachedOwner.AttachedProperty &&
                         (string)e.OldValue == null &&
                         (string)e.NewValue == "changed";
            child.Parent = parent;

            parent.SetValue(AttachedOwner.AttachedProperty, "changed");

            Assert.True(raised);
        }

        [Fact]
        public void PropertyChanged_Is_Raised_In_Parent_Before_Child()
        {
            var parent = new Class1();
            var child = new Class2 { Parent = parent };
            var result = new List<object>();

            parent.PropertyChanged += (s, e) => result.Add(parent);
            child.PropertyChanged += (s, e) => result.Add(child);

            parent.SetValue(Class1.BazProperty, "changed");

            Assert.Equal(new[] { parent, child }, result);
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

        private class AttachedOwner : AvaloniaObject
        {
            public static readonly AttachedProperty<string> AttachedProperty =
                AvaloniaProperty.RegisterAttached<AttachedOwner, Class1, string>("Attached", inherits: true);
        }
    }
}
