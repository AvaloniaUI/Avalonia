// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Collections;
using Avalonia.LogicalTree;
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

            Assert.Equal(child.Parent, panel);
            Assert.Equal(child.GetLogicalParent(), panel);
        }

        [Fact]
        public void Setting_Controls_Should_Set_Child_Controls_Parent()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children = new Controls { child };

            Assert.Equal(child.Parent, panel);
            Assert.Equal(child.GetLogicalParent(), panel);
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
            Assert.Null(child2.Parent);
            Assert.Null(child2.GetLogicalParent());
        }

        [Fact]
        public void Resetting_Panel_Children_Should_Clear_Child_Controls_Parent()
        {
            var panel = new Panel();
            var child1 = new Control();
            var child2 = new Control();

            panel.Children.Add(child1);
            panel.Children.Add(child2);
            panel.Children = new Controls();

            Assert.Null(child1.Parent);
            Assert.Null(child1.GetLogicalParent());
            Assert.Null(child2.Parent);
            Assert.Null(child2.GetLogicalParent());
        }

        [Fact]
        public void Setting_Children_Should_Make_Controls_Appear_In_Panel_Children()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children = new Controls { child };

            Assert.Equal(new[] { child }, panel.Children);
            Assert.Equal(new[] { child }, panel.GetLogicalChildren());
        }

        [Fact]
        public void Child_Control_Should_Appear_In_Panel_Children()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children.Add(child);

            Assert.Equal(new[] { child }, panel.Children);
            Assert.Equal(new[] { child }, panel.GetLogicalChildren());
        }

        [Fact]
        public void Removing_Child_Control_Should_Remove_From_Panel_Children()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children.Add(child);
            panel.Children.Remove(child);

            Assert.Equal(new Control[0], panel.Children);
            Assert.Equal(new ILogical[0], panel.GetLogicalChildren());
        }
    }
}
