using System;
using Avalonia.Controls;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class StyledElementTests_SetInheritanceParent
    {
        [Fact]
        public void Can_Set_InheritanceParent()
        {
            var child = new Border();
            var parent = new Border();

            ((ISetInheritanceParent)child).SetParent(parent);

            Assert.Same(parent, child.GetInheritanceParent());
        }

        [Fact]
        public void Can_Set_LogicalParent_After_InheritanceParent()
        {
            var child = new Border();
            var parent = new Border();
            var anotherParent = new Border();

            ((ISetInheritanceParent)child).SetParent(parent);

            anotherParent.Child = child;

            Assert.Equal(1, parent.GetInheritanceChildCount());
            Assert.Equal(0, anotherParent.GetInheritanceChildCount());
        }

        [Fact]
        public void Can_Set_InheritanceParent_After_LogicalParent()
        {
            var child = new Border();
            var parent = new Border();
            var anotherParent = new Border();

            anotherParent.Child = child;
            ((ISetInheritanceParent)child).SetParent(parent);

            Assert.Equal(1, parent.GetInheritanceChildCount());
            Assert.Equal(0, anotherParent.GetInheritanceChildCount());
        }

        [Fact]
        public void Setting_InheritanceParent_Adds_To_Parent_InheritanceChildren()
        {
            var child = new Border();
            var parent = new Border();

            ((ISetInheritanceParent)child).SetParent(parent);

            Assert.Equal(1, parent.GetInheritanceChildCount());
            Assert.Same(child, parent.GetInheritanceChild(0));
        }

        [Fact]
        public void Setting_InheritanceParent_Adds_To_Logical_InheritanceChildren()
        {
            var child1 = new Border();
            var child2 = new Border();
            var parent = new Border { Child = child1 };

            ((ISetInheritanceParent)child2).SetParent(parent);

            Assert.Equal(2, parent.GetInheritanceChildCount());
            Assert.Same(child1, parent.GetInheritanceChild(0));
            Assert.Same(child2, parent.GetInheritanceChild(1));
        }

        [Fact]
        public void Can_Clear_InheritanceParent()
        {
            var child = new Border();
            var parent = new Border();

            ((ISetInheritanceParent)child).SetParent(parent);
            ((ISetInheritanceParent)child).ClearParent();

            Assert.Null(child.GetInheritanceParent());
        }

        [Fact]
        public void Clearing_InheritanceParent_Removes_From_Parent_InheritanceChildren()
        {
            var child = new Border();
            var parent = new Border();

            ((ISetInheritanceParent)child).SetParent(parent);
            ((ISetInheritanceParent)child).ClearParent();

            Assert.Equal(0, parent.GetInheritanceChildCount());
        }
    }
}
