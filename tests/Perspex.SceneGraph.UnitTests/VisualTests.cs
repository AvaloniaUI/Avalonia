// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
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
    }
}
