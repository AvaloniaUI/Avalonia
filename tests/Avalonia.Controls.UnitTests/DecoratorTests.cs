// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Specialized;
using System.Linq;
using Avalonia.LogicalTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class DecoratorTests
    {
        [Fact]
        public void Setting_Content_Should_Set_Child_Controls_Parent()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Child = child;

            Assert.Equal(child.Parent, decorator);
            Assert.Equal(((ILogical)child).LogicalParent, decorator);
        }

        [Fact]
        public void Clearing_Content_Should_Clear_Child_Controls_Parent()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Child = child;
            decorator.Child = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Content_Control_Should_Appear_In_LogicalChildren()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Child = child;

            Assert.Equal(new[] { child }, ((ILogical)decorator).LogicalChildren.ToList());
        }

        [Fact]
        public void Clearing_Content_Should_Remove_From_LogicalChildren()
        {
            var decorator = new Decorator();
            var child = new Control();

            decorator.Child = child;
            decorator.Child = null;

            Assert.Equal(new ILogical[0], ((ILogical)decorator).LogicalChildren.ToList());
        }

        [Fact]
        public void Setting_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var decorator = new Decorator();
            var child = new Control();
            var called = false;

            ((ILogical)decorator).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            decorator.Child = child;

            Assert.True(called);
        }

        [Fact]
        public void Clearing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var decorator = new Decorator();
            var child = new Control();
            var called = false;

            decorator.Child = child;

            ((ILogical)decorator).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            decorator.Child = null;

            Assert.True(called);
        }

        [Fact]
        public void Changing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var decorator = new Decorator();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            decorator.Child = child1;

            ((ILogical)decorator).LogicalChildren.CollectionChanged += (s, e) => called = true;

            decorator.Child = child2;

            Assert.True(called);
        }

        [Fact]
        public void Measure_Should_Return_Padding_When_No_Child_Present()
        {
            var target = new Decorator
            {
                Padding = new Thickness(8),
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(16, 16), target.DesiredSize);
        }
    }
}
