using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Inheritance
    {
        [Fact]
        public void GetValue_Returns_Inherited_Value_1()
        {
            Class1 parent = new Class1();
            parent.SetValue(Class1.BazProperty, "changed");

            Class2 child = new Class2 { Parent = parent };
            Assert.Equal("changed", child.GetValue(Class1.BazProperty));
        }

        [Fact]
        public void GetValue_Returns_Inherited_Value_2()
        {
            Class1 parent = new Class1();
            Class2 child = new Class2 { Parent = parent };

            parent.SetValue(Class1.BazProperty, "changed");

            Assert.Equal("changed", child.GetValue(Class1.BazProperty));
        }

        [Fact]
        public void ClearValue_Clears_Inherited_Value()
        {
            Class1 parent = new Class1();
            Class2 child = new Class2 { Parent = parent };

            parent.SetValue(Class1.BazProperty, "changed");

            Assert.Equal("changed", child.GetValue(Class1.BazProperty));

            parent.ClearValue(Class1.BazProperty);
            
            Assert.Equal("bazdefault", parent.GetValue(Class1.BazProperty));
            Assert.Equal("bazdefault", child.GetValue(Class1.BazProperty));
        }

        [Fact]
        public void ClearValue_On_Parent_Raises_PropertyChanged_On_Child()
        {
            Class1 parent = new Class1();
            Class2 child = new Class2 { Parent = parent };
            var raised = 0;

            parent.SetValue(Class1.BazProperty, "changed");

            child.PropertyChanged += (s, e) =>
            {
                Assert.Same(child, e.Sender);
                Assert.Equal("changed", e.OldValue);
                Assert.Equal("bazdefault", e.NewValue);
                Assert.Equal(BindingPriority.Inherited, e.Priority);
                ++raised;
            };

            parent.ClearValue(Class1.BazProperty);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void ClearValue_On_Child_Raises_PropertyChanged_With_Inherited_Parent_Value()
        {
            Class1 parent = new Class1();
            Class2 child = new Class2 { Parent = parent };
            var raised = 0;

            parent.SetValue(Class1.BazProperty, "parent");
            child.SetValue(Class1.BazProperty, "child");

            child.PropertyChanged += (s, e) =>
            {
                Assert.Same(child, e.Sender);
                Assert.Equal("child", e.OldValue);
                Assert.Equal("parent", e.NewValue);
                Assert.Equal(BindingPriority.Inherited, e.Priority);
                ++raised;
            };

            child.ClearValue(Class1.BazProperty);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void ClearValue_On_Parent_Raises_PropertyChanged_On_Child_With_Inherited_Grandparent_Value()
        {
            var grandparent = new Class1();
            var parent = new Class2 { Parent = grandparent };
            var child = new Class2 { Parent = parent };
            var raised = 0;

            grandparent.SetValue(Class1.BazProperty, "grandparent");
            parent.SetValue(Class1.BazProperty, "parent");

            child.PropertyChanged += (s, e) =>
            {
                Assert.Same(child, e.Sender);
                Assert.Equal("parent", e.OldValue);
                Assert.Equal("grandparent", e.NewValue);
                Assert.Equal(BindingPriority.Inherited, e.Priority);
                ++raised;
            };

            parent.ClearValue(Class1.BazProperty);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Setting_InheritanceParent_Raises_PropertyChanged_When_Parent_Has_Value_Set()
        {
            bool raised = false;

            Class1 parent = new Class1();
            parent.SetValue(Class1.BazProperty, "changed");

            Class2 child = new Class2();
            child.PropertyChanged += (s, e) =>
                raised = s == child &&
                         e.Property == Class1.BazProperty &&
                         (string)e.OldValue == "bazdefault" &&
                         (string)e.NewValue == "changed" &&
                         e.Priority == BindingPriority.Inherited;

            child.Parent = parent;

            Assert.True(raised);
            Assert.Equal("changed", child.GetValue(Class1.BazProperty));
        }

        [Fact]
        public void Setting_InheritanceParent_Raises_PropertyChanged_When_Parent_And_Grandparent_Has_Value_Set()
        {
            Class1 grandparent = new Class1();
            Class2 parent = new Class2 { Parent = grandparent };
            bool raised = false;

            grandparent.SetValue(Class1.BazProperty, "changed1");
            parent.SetValue(Class1.BazProperty, "changed2");

            Class2 child = new Class2();
            child.PropertyChanged += (s, e) =>
                raised = s == child &&
                         e.Property == Class1.BazProperty &&
                         (string)e.OldValue == "bazdefault" &&
                         (string)e.NewValue == "changed2" &&
                         e.Priority == BindingPriority.Inherited;

            child.Parent = parent;

            Assert.True(raised);
            Assert.Equal("changed2", child.GetValue(Class1.BazProperty));
        }

        [Fact]
        public void Setting_InheritanceParent_Raises_PropertyChanged_For_Attached_Property_When_Parent_Has_Value_Set()
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
            Assert.Equal("changed", child.GetValue(AttachedOwner.AttachedProperty));
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
            Assert.Equal("localvalue", child.GetValue(Class1.BazProperty));
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
            Assert.Equal("changed", child.GetValue(Class1.BazProperty));
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
            Assert.Equal("changed", child.GetValue(AttachedOwner.AttachedProperty));
        }

        [Fact]
        public void Clearing_Value_In_InheritanceParent_Raises_PropertyChanged()
        {
            bool raised = false;

            Class1 parent = new Class1();
            parent.SetValue(Class1.BazProperty, "changed");

            Class2 child = new Class2 { Parent = parent };

            child.PropertyChanged += (s, e) =>
                raised = s == child &&
                         e.Property == Class1.BazProperty &&
                         (string)e.OldValue == "changed" &&
                         (string)e.NewValue == "bazdefault";

            parent.ClearValue(Class1.BazProperty);

            Assert.True(raised);
            Assert.Equal("bazdefault", child.GetValue(Class1.BazProperty));
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

        [Fact]
        public void Reparenting_Raises_PropertyChanged_For_Old_And_New_Inherited_Values()
        {
            var oldParent = new Class1();
            oldParent.SetValue(Class1.BazProperty, "oldvalue");

            var newParent = new Class1();
            newParent.SetValue(Class1.BazProperty, "newvalue");

            var child = new Class2 { Parent = oldParent };
            var raised = 0;

            child.PropertyChanged += (s, e) =>
            {
                Assert.Equal(child, e.Sender);
                Assert.Equal("oldvalue", e.GetOldValue<string>());
                Assert.Equal("newvalue", e.GetNewValue<string>());
                Assert.Equal(BindingPriority.Inherited, e.Priority);
                ++raised;
            };

            child.Parent = newParent;

            Assert.Equal(1, raised);
            Assert.Equal("newvalue", child.GetValue(Class1.BazProperty));
        }

        [Fact]
        public void Reparenting_Raises_PropertyChanged_On_GrandChild_For_Old_And_New_Inherited_Values()
        {
            var oldParent = new Class1();
            oldParent.SetValue(Class1.BazProperty, "oldvalue");

            var newParent = new Class1();
            newParent.SetValue(Class1.BazProperty, "newvalue");

            var child = new Class2 { Parent = oldParent };
            var grandchild = new Class2 { Parent = child };
            var raised = 0;

            grandchild.PropertyChanged += (s, e) =>
            {
                Assert.Equal(grandchild, e.Sender);
                Assert.Equal("oldvalue", e.GetOldValue<string>());
                Assert.Equal("newvalue", e.GetNewValue<string>());
                Assert.Equal(BindingPriority.Inherited, e.Priority);
                ++raised;
            };

            child.Parent = newParent;

            Assert.Equal(1, raised);
            Assert.Equal("newvalue", grandchild.GetValue(Class1.BazProperty));
        }

        [Fact]
        public void Reparenting_Retains_Inherited_Property_Set_On_Child()
        {
            var oldParent = new Class1();
            oldParent.SetValue(Class1.BazProperty, "oldvalue");

            var newParent = new Class1();
            newParent.SetValue(Class1.BazProperty, "newvalue");

            var child = new Class2 { Parent = oldParent };
            child.SetValue(Class1.BazProperty, "childvalue");

            var grandchild = new Class2 { Parent = child };
            var raised = 0;

            grandchild.PropertyChanged += (s, e) => ++raised;

            child.Parent = newParent;

            Assert.Equal(0, raised);
            Assert.Equal("childvalue", child.GetValue(Class1.BazProperty));
            Assert.Equal("childvalue", grandchild.GetValue(Class1.BazProperty));
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
