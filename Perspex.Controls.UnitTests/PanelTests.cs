// -----------------------------------------------------------------------
// <copyright file="PanelTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PanelTests
    {
        [TestMethod]
        public void Adding_Control_To_Panel_Should_Set_Child_Controls_Parent()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children.Add(child);

            Assert.AreEqual(child.Parent, panel);
            Assert.AreEqual(((ILogical)child).LogicalParent, panel);
        }

        [TestMethod]
        public void Removing_Control_From_Panel_Should_Clear_Child_Controls_Parent()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children.Add(child);
            panel.Children.Remove(child);

            Assert.IsNull(child.Parent);
            Assert.IsNull(((ILogical)child).LogicalParent);
        }

        [TestMethod]
        public void Clearing_Panel_Children_Should_Clear_Child_Controls_Parent()
        {
            var panel = new Panel();
            var child1 = new Control();
            var child2 = new Control();

            panel.Children.Add(child1);
            panel.Children.Add(child2);
            panel.Children.Clear();

            Assert.IsNull(child1.Parent);
            Assert.IsNull(((ILogical)child1).LogicalParent);
            Assert.IsNull(child2.Parent);
            Assert.IsNull(((ILogical)child2).LogicalParent);
        }

        [TestMethod]
        public void Child_Control_Should_Appear_In_Panel_Children()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children.Add(child);

            CollectionAssert.AreEqual(new[] { child }, panel.Children);
            CollectionAssert.AreEqual(new[] { child }, ((ILogical)panel).LogicalChildren.ToList());
        }

        [TestMethod]
        public void Removing_Child_Control_Should_Remove_From_Panel_Children()
        {
            var panel = new Panel();
            var child = new Control();

            panel.Children.Add(child);
            panel.Children.Remove(child);

            CollectionAssert.AreEqual(new Control[0], panel.Children);
            CollectionAssert.AreEqual(new ILogical[0], ((ILogical)panel).LogicalChildren.ToList());
        }
    }
}
