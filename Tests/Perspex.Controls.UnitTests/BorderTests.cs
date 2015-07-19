// -----------------------------------------------------------------------
// <copyright file="BorderTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System.Collections.Specialized;
    using System.Linq;
    using Xunit;

    public class BorderTests
    {
        [Fact]
        public void Setting_Content_Should_Set_Child_Controls_Parent()
        {
            var target = new Border();
            var child = new Control();

            target.Content = child;

            Assert.Equal(child.Parent, target);
            Assert.Equal(((ILogical)child).LogicalParent, target);
        }

        [Fact]
        public void Clearing_Content_Should_Clear_Child_Controls_Parent()
        {
            var target = new Border();
            var child = new Control();

            target.Content = child;
            target.Content = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Content_Control_Should_Appear_In_LogicalChildren()
        {
            var target = new Border();
            var child = new Control();

            target.Content = child;

            Assert.Equal(new[] { child }, ((ILogical)target).LogicalChildren.ToList());
        }

        [Fact]
        public void Clearing_Content_Should_Remove_From_LogicalChildren()
        {
            var target = new Border();
            var child = new Control();

            target.Content = child;
            target.Content = null;

            Assert.Equal(new ILogical[0], ((ILogical)target).LogicalChildren.ToList());
        }

        [Fact]
        public void Setting_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Border();
            var child = new Control();
            var called = false;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => 
                called = e.Action == NotifyCollectionChangedAction.Add;

            target.Content = child;

            Assert.True(called);
        }

        [Fact]
        public void Clearing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Border();
            var child = new Control();
            var called = false;

            target.Content = child;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            target.Content = null;

            Assert.True(called);
        }

        [Fact]
        public void Changing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Border();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            target.Content = child1;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Replace;

            target.Content = child2;

            Assert.True(called);
        }

        [Fact]
        public void Measure_Should_Return_BorderThickness_Plus_Padding_When_No_Child_Present()
        {
            var target = new Border
            {
                Padding = new Thickness(6),
                BorderThickness = 4,
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(20, 20), target.DesiredSize);
        }
    }
}
