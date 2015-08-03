// -----------------------------------------------------------------------
// <copyright file="PopupTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests.Primitives
{
    using System.Collections.Specialized;
    using System.Linq;
    using Perspex.Controls.Primitives;
    using Perspex.LogicalTree;
    using Xunit;

    public class PopupTests
    {
        [Fact]
        public void Setting_Child_Should_Set_Child_Controls_LogicalParent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Equal(child.Parent, target);
            Assert.Equal(((ILogical)child).LogicalParent, target);
        }

        [Fact]
        public void Clearing_Child_Should_Clear_Child_Controls_Parent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;
            target.Child = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Child_Control_Should_Appear_In_LogicalChildren()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Clearing_Child_Should_Remove_From_LogicalChildren()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;
            target.Child = null;

            Assert.Equal(new ILogical[0], ((ILogical)target).LogicalChildren.ToList());
        }

        [Fact]
        public void Setting_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child = new Control();
            var called = false;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            target.Child = child;

            Assert.True(called);
        }

        [Fact]
        public void Clearing_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child = new Control();
            var called = false;

            target.Child = child;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            target.Child = null;

            Assert.True(called);
        }

        [Fact]
        public void Changing_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            target.Child = child1;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            target.Child = child2;

            Assert.True(called);
        }

        [Fact]
        public void Setting_Child_Should_Not_Set_Childs_VisualParent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Null(((IVisual)child).VisualParent);
        }
    }
}
