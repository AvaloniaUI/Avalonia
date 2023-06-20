using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.PropertyStore
{
    public class ValueStoreTests_Inheritance
    {
        [Fact]
        public void InheritanceAncestor_Is_Initially_Null()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };
            var grandchild = new Class1 { Parent = child };

            Assert.Null(parent.GetValueStore().InheritanceAncestor);
            Assert.Null(child.GetValueStore().InheritanceAncestor);
            Assert.Null(grandchild.GetValueStore().InheritanceAncestor);
        }

        [Fact]
        public void Setting_Value_In_Parent_Updates_InheritanceAncestor()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };
            var grandchild = new Class1 { Parent = child };

            parent.Foo = "changed";

            var parentStore = parent.GetValueStore();
            Assert.Null(parentStore.InheritanceAncestor);
            Assert.Same(parentStore, child.GetValueStore().InheritanceAncestor);
            Assert.Same(parentStore, grandchild.GetValueStore().InheritanceAncestor);
        }

        [Fact]
        public void Setting_Value_In_Parent_Doesnt_Update_Grandchild_InheritanceAncestor_If_Child_Has_Value_Set()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };
            var grandchild = new Class1 { Parent = child };

            child.Foo = "foochanged";
            parent.Foo = "changed";

            var parentStore = parent.GetValueStore();
            Assert.Null(parentStore.InheritanceAncestor);
            Assert.Same(parentStore, child.GetValueStore().InheritanceAncestor);
            Assert.Same(child.GetValueStore(), grandchild.GetValueStore().InheritanceAncestor);
        }

        [Fact]
        public void Clearing_Value_In_Parent_Updates_InheritanceAncestor()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };
            var grandchild = new Class1 { Parent = child };

            parent.Foo = "changed";
            parent.ClearValue(Class1.FooProperty);

            Assert.Null(parent.GetValueStore().InheritanceAncestor);
            Assert.Null(child.GetValueStore().InheritanceAncestor);
            Assert.Null(grandchild.GetValueStore().InheritanceAncestor);
        }

        [Fact]
        public void Clear_Value_In_Parent_Doesnt_Update_Grandchild_InheritanceAncestor_If_Child_Has_Value_Set()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };
            var grandchild = new Class1 { Parent = child };

            child.Foo = "foochanged";
            parent.Foo = "changed";
            parent.ClearValue(Class1.FooProperty);

            Assert.Null(parent.GetValueStore().InheritanceAncestor);
            Assert.Null(child.GetValueStore().InheritanceAncestor);
            Assert.Same(child.GetValueStore(), grandchild.GetValueStore().InheritanceAncestor);
        }

        [Fact]
        public void Clearing_Value_In_Child_Updates_InheritanceAncestor()
        {
            var parent = new Class1();
            var child = new Class1 { Parent = parent };
            var grandchild = new Class1 { Parent = child };

            parent.Foo = "changed";
            child.Foo = "foochanged";
            child.ClearValue(Class1.FooProperty);

            var parentStore = parent.GetValueStore();
            Assert.Null(parentStore.InheritanceAncestor);
            Assert.Same(parentStore, child.GetValueStore().InheritanceAncestor);
            Assert.Same(parentStore, grandchild.GetValueStore().InheritanceAncestor);
        }

        [Fact]
        public void Child_Notifies_About_Setting_Back_To_Default_Value()
        {
            var parent = new Class1();
            var child = new Class1();

            parent.Foo = "changed";
            child.Parent = parent;

            bool raised = false;
            child.PropertyChanged += (_, args) =>
            {
                raised = args.Property == Class1.FooProperty && args.GetNewValue<string>() == "foodefault";
            };

            Assert.Equal("changed", child.Foo); // inherited from parent.

            child.Foo = "foodefault"; // reset back to default.
            Assert.True(raised); // expect event to be raised, as actual value was changed.
        }

        [Fact]
        public void Adding_Child_Sets_InheritanceAncestor()
        {
            var parent = new Class1();
            var child = new Class1();
            var grandchild = new Class1 { Parent = child };

            parent.Foo = "changed";
            child.Parent = parent;

            var parentStore = parent.GetValueStore();
            Assert.Null(parentStore.InheritanceAncestor);
            Assert.Same(parentStore, child.GetValueStore().InheritanceAncestor);
            Assert.Same(parentStore, grandchild.GetValueStore().InheritanceAncestor);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "foodefault", inherits: true);

            public string Foo
            {
                get => GetValue(FooProperty);
                set => SetValue(FooProperty, value);
            }

            public Class1? Parent
            {
                get { return (Class1?)InheritanceParent; }
                set { InheritanceParent = value; }
            }
        }
    }
}
