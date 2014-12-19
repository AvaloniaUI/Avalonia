// -----------------------------------------------------------------------
// <copyright file="DecoratorTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System.Collections.Specialized;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DecoratorTests
    {
        [TestMethod]
        public void Setting_Content_Should_Set_Child_Controls_Parent()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Content = child;

            Assert.AreEqual(child.Parent, decorator);
            Assert.AreEqual(((ILogical)child).LogicalParent, decorator);
        }

        [TestMethod]
        public void Clearing_Content_Should_Clear_Child_Controls_Parent()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Content = child;
            decorator.Content = null;

            Assert.IsNull(child.Parent);
            Assert.IsNull(((ILogical)child).LogicalParent);
        }

        [TestMethod]
        public void Content_Control_Should_Appear_In_LogicalChildren()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Content = child;

            CollectionAssert.AreEqual(new[] { child }, ((ILogical)decorator).LogicalChildren.ToList());
        }

        [TestMethod]
        public void Clearing_Content_Should_Remove_From_LogicalChildren()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Content = child;
            decorator.Content = null;

            CollectionAssert.AreEqual(new ILogical[0], ((ILogical)decorator).LogicalChildren.ToList());
        }

        [TestMethod]
        public void Setting_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var decorator = new Decorator();
            var child = new Control();
            var called = false;

            ((ILogical)decorator).LogicalChildren.CollectionChanged += (s, e) => 
                called = e.Action == NotifyCollectionChangedAction.Add;

            decorator.Content = child;

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Clearing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var decorator = new Decorator();
            var child = new Control();
            var called = false;

            decorator.Content = child;

            ((ILogical)decorator).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            decorator.Content = null;

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Changing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var decorator = new Decorator();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            decorator.Content = child1;

            ((ILogical)decorator).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Replace;

            decorator.Content = child2;

            Assert.IsTrue(called);
        }
    }
}
