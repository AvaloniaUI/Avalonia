using System.Collections.Generic;
using Avalonia.Data;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Inheritance
    {
        [Fact]
        public void GetValue_Returns_Already_Set_Inherited_Value()
        {
            var parent = new Class1 { Foo = "changed" };
            var child = new Class1();

            child.Parent = parent;

            Assert.Equal("changed", child.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Returns_New_Inherited_Value()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };

            parent.Foo = "changed";

            Assert.Equal("changed", child.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Returns_Already_Set_Inherited_Value_From_Grandparent()
        {
            var grandparent = new Class1 { Foo = "changed" };
            var parent = new Class1 { Parent = grandparent };
            var child = new Class1 { Parent = parent };

            Assert.Equal("changed", child.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Returns_New_Inherited_Value_From_Grandparent()
        {
            var grandparent = new Class1();
            var parent = new Class1 { Parent = grandparent };
            var child = new Class1 { Parent = parent };

            grandparent.Foo = "changed";

            Assert.Equal("changed", child.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Returns_Already_Set_Inherited_Value_From_Grandparent_With_Another_Inherited_Value_Set_In_Parent()
        {
            var grandparent = new Class1 { Foo = "changed" };
            var parent = new Class1 { Parent = grandparent, Baz = "baz" };
            var child = new Class1 { Parent = parent };

            Assert.Equal("changed", child.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Returns_New_Inherited_Value_From_Grandparent_With_Another_Inherited_Value_Set_In_Parent()
        {
            var grandparent = new Class1();
            var parent = new Class1 { Parent = grandparent, Baz = "baz" };
            var child = new Class1 { Parent = parent };

            grandparent.Foo = "changed";

            Assert.Equal("changed", child.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Setting_Parent_Raises_PropertyChanged_When_Value_Changed_In_Parent()
        {
            var parent = new Class1 { Foo = "changed" };
            var child = new Class1();
            var raised = 0;

            child.PropertyChanged += (s, e) =>
            {
                Assert.Same(s, child);
                Assert.Equal(e.Property, Class1.FooProperty);
                Assert.Equal("foodefault", e.OldValue);
                Assert.Equal("changed", e.NewValue);
                Assert.Equal(BindingPriority.Inherited, e.Priority);
                ++raised;
            };

            child.Parent = parent;

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Setting_Parent_Doesnt_Raise_PropertyChanged_When_Local_Value_Set()
        {
            var parent = new Class1 { Foo = "changed" };
            var child = new Class1 { Foo = "localvalue " };
            var raised = 0;

            child.PropertyChanged += (s, e) => ++raised;
            child.Parent = parent;

            Assert.Equal(0, raised);
        }

        [Fact]
        public void Setting_Value_In_Parent_Raises_PropertyChanged()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };
            var raised = 0;

            child.PropertyChanged += (s, e) =>
            {
                Assert.Same(s, child);
                Assert.Equal(e.Property, Class1.FooProperty);
                Assert.Equal("foodefault", e.OldValue);
                Assert.Equal("changed", e.NewValue);
                Assert.Equal(BindingPriority.Inherited, e.Priority);
                ++raised;
            };

            parent.Foo = "changed";

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Clearing_Value_In_Parent_Raises_PropertyChanged()
        {
            var parent = new Class1 { Foo = "foo" };
            var child = new Class1 { Parent = parent };
            var raised = 0;

            child.PropertyChanged += (s, e) =>
            {
                Assert.Same(s, child);
                Assert.Equal(e.Property, Class1.FooProperty);
                Assert.Equal("foo", e.OldValue);
                Assert.Equal("foodefault", e.NewValue);
                Assert.Equal(BindingPriority.Unset, e.Priority);
                ++raised;
            };

            parent.ClearValue(Class1.FooProperty);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Setting_Value_In_GrandParent_Raises_PropertyChanged()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };
            var grandchild = new Class1 { Parent = child };
            var raised = 0;

            grandchild.PropertyChanged += (s, e) =>
            {
                Assert.Same(s, grandchild);
                Assert.Equal(e.Property, Class1.FooProperty);
                Assert.Equal("foodefault", e.OldValue);
                Assert.Equal("changed", e.NewValue);
                Assert.Equal(BindingPriority.Inherited, e.Priority);
                ++raised;
            };

            parent.Foo = "changed";

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Setting_Value_In_GrandParent_With_Another_Inherited_Value_Set_In_Parent_Raises_PropertyChanged()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };
            var grandchild = new Class1 { Parent = child };
            var raised = 0;

            child.Baz = "baz";

            grandchild.PropertyChanged += (s, e) =>
            {
                Assert.Same(s, grandchild);
                Assert.Equal(e.Property, Class1.FooProperty);
                Assert.Equal("foodefault", e.OldValue);
                Assert.Equal("changed", e.NewValue);
                Assert.Equal(BindingPriority.Inherited, e.Priority);
                ++raised;
            };

            parent.Foo = "changed";

            Assert.Equal(1, raised);
        }

        [Fact]
        public void PropertyChanged_Is_Raised_In_Parent_Before_Child()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };
            var result = new List<object>();

            parent.PropertyChanged += (s, e) => result.Add(parent);
            child.PropertyChanged += (s, e) => result.Add(child);

            parent.Foo = "changed";

            Assert.Equal(new[] { parent, child }, result);
        }

        [Fact]
        public void Setting_LocalValue_Overrides_Inherited_Value()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };

            child.Foo = "changed";

            Assert.Equal("changed", child.Foo);
            Assert.Equal("foodefault", parent.Foo);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string?> FooProperty =
                AvaloniaProperty.Register<Class1, string?>("Foo", "foodefault", inherits: true);
            public static readonly StyledProperty<string?> BazProperty =
                AvaloniaProperty.Register<Class1, string?>("Baz", "bazdefault", inherits: true);
            private Class1? _parent;
            private List<Class1> _inheritanceChildren = new();

            public string? Foo
            {
                get => GetValue(FooProperty);
                set => SetValue(FooProperty, value);
            }

            public string? Baz
            {
                get => GetValue(BazProperty);
                set => SetValue(BazProperty, value);
            }

            public Class1? Parent
            {
                get { return _parent; }
                set
                {
                    if (_parent != value)
                    {
                        _parent?._inheritanceChildren.Remove(this);
                        _parent = value;
                        _parent?._inheritanceChildren.Add(this);
                        InheritanceParentChanged();
                    }
                }
            }

            protected internal override int GetInheritanceChildCount() => _inheritanceChildren.Count;
            protected internal override AvaloniaObject GetInheritanceChild(int index) => _inheritanceChildren[index];
            protected internal override AvaloniaObject? GetInheritanceParent() => _parent;
        }
    }
}
