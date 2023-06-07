using System;
using System.Linq;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class PanelTests
    {
        [Fact]
        public void Adding_Control_To_Panel_Should_Set_Child_Controls_Parent()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children.Add(child);

            Assert.Same(child.Parent, panel);
            Assert.Same(child.GetLogicalParent(), panel);
            Assert.Same(child.GetVisualParent(), panel);
        }

        [Fact]
        public void Removing_Control_From_Panel_Should_Clear_Child_Controls_Parent()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children.Add(child);
            panel.Children.Remove(child);

            Assert.Null(child.Parent);
            Assert.Null(child.GetLogicalParent());
            Assert.Null(child.GetVisualParent());
        }

        [Fact]
        public void Clearing_Panel_Children_Should_Clear_Child_Controls_Parent()
        {
            var panel = new Panel();
            var child1 = new Control();
            var child2 = new Control();

            panel.Children.Add(child1);
            panel.Children.Add(child2);
            panel.Children.Clear();

            Assert.Null(child1.Parent);
            Assert.Null(child1.GetLogicalParent());
            Assert.Null(child1.GetVisualParent());
            Assert.Null(child2.Parent);
            Assert.Null(child2.GetLogicalParent());
            Assert.Null(child2.GetVisualParent());
        }

        [Fact]
        public void Replacing_Panel_Children_Should_Clear_And_Set_Control_Parent()
        {
            var panel = new Panel();
            var child1 = new Control();
            var child2 = new Control();

            panel.Children.Add(child1);
            panel.Children[0] = child2;

            Assert.Null(child1.Parent);
            Assert.Null(child1.GetLogicalParent());
            Assert.Null(child1.GetVisualParent());
            Assert.Same(child2.Parent, panel);
            Assert.Same(child2.GetLogicalParent(), panel);
            Assert.Same(child2.GetVisualParent(), panel);
        }

        [Fact]
        public void Child_Control_Should_Appear_In_Panel_Logical_And_Visual_Children()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children.Add(child);

            Assert.Equal(new[] { child }, panel.Children);
            Assert.Equal(new[] { child }, panel.GetLogicalChildren());
            Assert.Equal(new[] { child }, panel.GetVisualChildren());
        }

        [Fact]
        public void Removing_Child_Control_Should_Remove_From_Panel_Logical_And_Visual_Children()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children.Add(child);
            panel.Children.Remove(child);

            Assert.Equal(new Control[0], panel.Children);
            Assert.Empty(panel.GetLogicalChildren());
            Assert.Empty(panel.GetVisualChildren());
        }

        [Fact]
        public void Moving_Panel_Children_Should_Reoder_Logical_And_Visual_Children()
        {
            var panel = new Panel();
            var child1 = new Control();
            var child2 = new Control();

            panel.Children.Add(child1);
            panel.Children.Add(child2);
            panel.Children.Move(1, 0);

            Assert.Equal(new[] { child2, child1 }, panel.GetLogicalChildren());
            Assert.Equal(new[] { child2, child1 }, panel.GetVisualChildren());
        }
        
        [Fact]
        public void Adding_Null_Child_Should_Throw()
        {
            var panel = new Panel();
            Assert.Throws<ArgumentNullException>(() => panel.Children.Add(null!));
        }

        [Fact]
        public void Adding_Control_To_Items_Host_Panel_Should_Not_Affect_Logical_Children()
        {
            var child = new Control();
            var realParent = new ContentControl { Content = child };
            var panel = new Panel { IsItemsHost = true };

            panel.Children.Add(child);

            Assert.Empty(panel.LogicalChildren);
            Assert.Same(child.Parent, realParent);
            Assert.Same(child.GetLogicalParent(), realParent);
            Assert.Same(child.GetVisualParent(), panel);
        }
    }
}
