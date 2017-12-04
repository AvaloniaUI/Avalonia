// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests
{
    public class VisualTests
    {
        [Fact]
        public void Added_Child_Should_Have_VisualParent_Set()
        {
            var target = new TestVisual();
            var child = new Visual();

            target.AddChild(child);

            Assert.Equal(target, child.GetVisualParent());
        }

        [Fact]
        public void Added_Child_Should_Notify_VisualParent_Changed()
        {
            var target = new TestVisual();
            var child = new TestVisual();
            var parents = new List<IVisual>();

            child.GetObservable(Visual.VisualParentProperty).Subscribe(x => parents.Add(x));
            target.AddChild(child);
            target.RemoveChild(child);

            Assert.Equal(new IVisual[] { null, target, null }, parents);
        }

        [Fact]
        public void Removed_Child_Should_Have_VisualParent_Cleared()
        {
            var target = new TestVisual();
            var child = new Visual();

            target.AddChild(child);
            target.RemoveChild(child);

            Assert.Null(child.GetVisualParent());
        }

        [Fact]
        public void Clearing_Children_Should_Clear_VisualParent()
        {
            var children = new[] { new Visual(), new Visual() };
            var target = new TestVisual();

            target.AddChildren(children);
            target.ClearChildren();

            var result = children.Select(x => x.GetVisualParent()).ToList();

            Assert.Equal(new Visual[] { null, null }, result);
        }

        [Fact]
        public void Adding_Children_Should_Fire_OnAttachedToVisualTree()
        {
            var child2 = new Decorator();
            var child1 = new Decorator { Child = child2 };
            var root = new TestRoot();
            var called1 = false;
            var called2 = false;

            child1.AttachedToVisualTree += (s, e) =>
            {
                Assert.Equal(e.Parent, root);
                Assert.Equal(e.Root, root);
                called1 = true;
            };

            child2.AttachedToVisualTree += (s, e) =>
            {
                Assert.Equal(e.Parent, root);
                Assert.Equal(e.Root, root);
                called2 = true;
            };

            root.Child = child1;

            Assert.True(called1);
            Assert.True(called2);
        }

        [Fact]
        public void Removing_Children_Should_Fire_OnDetachedFromVisualTree()
        {
            var child2 = new Decorator();
            var child1 = new Decorator { Child = child2 };
            var root = new TestRoot();
            var called1 = false;
            var called2 = false;

            root.Child = child1;

            child1.DetachedFromVisualTree += (s, e) =>
            {
                Assert.Equal(e.Parent, root);
                Assert.Equal(e.Root, root);
                called1 = true;
            };

            child2.DetachedFromVisualTree += (s, e) =>
            {
                Assert.Equal(e.Parent, root);
                Assert.Equal(e.Root, root);
                called2 = true;
            };

            root.Child = null;

            Assert.True(called1);
            Assert.True(called2);
        }

        [Fact]
        public void Root_Should_Retun_Self_As_VisualRoot()
        {
            var root = new TestRoot();

            Assert.Same(root, ((IVisual)root).VisualRoot);
        }

        [Fact]
        public void Descendants_Should_RetunVisualRoot()
        {
            var root = new TestRoot();
            var child1 = new Decorator();
            var child2 = new Decorator();

            root.Child = child1;
            child1.Child = child2;

            Assert.Same(root, ((IVisual)child1).VisualRoot);
            Assert.Same(root, ((IVisual)child2).VisualRoot);
        }

        [Fact]
        public void Attaching_To_Visual_Tree_Should_Invalidate_Visual()
        {
            var renderer = new Mock<IRenderer>();
            var child = new Decorator();
            var root = new TestRoot
            {
                Renderer = renderer.Object,
            };

            root.Child = child;

            renderer.Verify(x => x.AddDirty(child));
        }

        [Fact]
        public void Detaching_From_Visual_Tree_Should_Invalidate_Visual()
        {
            var renderer = new Mock<IRenderer>();
            var child = new Decorator();
            var root = new TestRoot
            {
                Renderer = renderer.Object,
            };

            root.Child = child;
            renderer.ResetCalls();
            root.Child = null;

            renderer.Verify(x => x.AddDirty(child));
        }

        [Fact]
        public void Adding_Already_Parented_Control_Should_Throw()
        {
            var root1 = new TestRoot();
            var root2 = new TestRoot();
            var child = new Canvas();

            root1.Child = child;

            Assert.Throws<InvalidOperationException>(() => root2.Child = child);
            Assert.Empty(root2.GetVisualChildren());
        }
    }
}
