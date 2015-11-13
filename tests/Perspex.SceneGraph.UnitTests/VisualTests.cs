// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Perspex.Controls;
using Perspex.VisualTree;
using Xunit;

namespace Perspex.SceneGraph.UnitTests
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
        public void Added_Child_Should_Have_InheritanceParent_Set()
        {
            var target = new TestVisual();
            var child = new TestVisual();

            target.AddChild(child);

            Assert.Equal(target, child.InheritanceParent);
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
        public void Removed_Child_Should_Have_InheritanceParent_Cleared()
        {
            var target = new TestVisual();
            var child = new TestVisual();

            target.AddChild(child);
            target.RemoveChild(child);

            Assert.Null(child.InheritanceParent);
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
            var child2 = new TestVisual();
            var child1 = new TestVisual { Child = child2 };
            var root = new TestRoot();
            var called1 = false;
            var called2 = false;

            child1.AttachedToVisualTree += (s, e) => called1 = true;
            child2.AttachedToVisualTree += (s, e) => called2 = true;

            root.Child = child1;

            Assert.True(called1);
            Assert.True(called2);
        }

        [Fact]
        public void Removing_Children_Should_Fire_OnDetachedFromVisualTree()
        {
            var child2 = new TestVisual();
            var child1 = new TestVisual { Child = child2 };
            var root = new TestRoot();
            var called1 = false;
            var called2 = false;

            root.Child = child1;
            child1.DetachedFromVisualTree += (s, e) => called1 = true;
            child2.DetachedFromVisualTree += (s, e) => called2 = true;
            root.Child = null;

            Assert.True(called1);
            Assert.True(called2);
        }

        [Fact]
        public void Controls_Should_Add_Themselves_To_Root_NameScope()
        {
            var child2 = new TestVisual { Name = "bar" };
            var child1 = new TestVisual { Name = "foo", Child = child2 };
            var root = new TestRoot { Child = child1 };

            Assert.Same(child1, root.Find("foo"));
            Assert.Same(child2, root.Find("bar"));
        }

        [Fact]
        public void Controls_Should_Add_Themselves_To_NameScopes_In_Attached_Property()
        {
            var child2 = new TestVisual { Name = "bar", [NameScope.NameScopeProperty] = new NameScope() };
            var child1 = new TestVisual { Name = "foo", Child = child2};
            var root = new TestRoot { Child = child1 };

            Assert.Same(child1, root.Find("foo"));
            Assert.Null(root.Find("bar"));
            Assert.Same(child2, NameScope.GetNameScope(child2).Find("bar"));
        }

        [Fact]
        public void Controls_Should_Remove_Themselves_From_Root_NameScope()
        {
            var child2 = new TestVisual { Name = "bar" };
            var child1 = new TestVisual { Name = "foo", Child = child2 };
            var root = new TestRoot { Child = child1 };

            root.Child = null;

            Assert.Null(root.Find("foo"));
            Assert.Null(root.Find("bar"));
        }

        [Fact]
        public void Controls_Should_Remove_Themselves_From_NameScopes_In_Attached_Property()
        {
            var child2 = new TestVisual { Name = "bar" };
            var child1 = new TestVisual { Name = "foo", Child = child2,[NameScope.NameScopeProperty] = new NameScope() };
            var root = new TestRoot { Child = child1 };

            root.Child = null;

            Assert.Null(root.Find("foo"));
            Assert.Null(root.Find("bar"));
            Assert.Null(NameScope.GetNameScope(child1).Find("bar"));
        }
    }
}
