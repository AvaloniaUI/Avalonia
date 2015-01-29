// -----------------------------------------------------------------------
// <copyright file="VisualTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.SceneGraph.UnitTests
{
    using System.Linq;
    using Perspex.VisualTree;
    using Xunit;

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

        [Fact]
        public void ParentChanged_Attached_Methods_Should_Be_Called_In_Right_Order()
        {
            var target = new TestRoot();
            var child = new TestVisual();
            int changed = 0;
            int attched = 0;
            int i = 1;

            child.VisualParentChangedCalled += (s, e) => changed = i++;
            child.AttachedToVisualTreeCalled += (s, e) => attched = i++;

            target.AddChild(child);

            Assert.Equal(1, changed);
            Assert.Equal(2, attched);
        }

        [Fact]
        public void ParentChanged_Detached_Methods_Should_Be_Called_In_Right_Order()
        {
            var target = new TestRoot();
            var child = new TestVisual();
            int changed = 0;
            int detached = 0;
            int i = 1;

            target.AddChild(child);

            child.VisualParentChangedCalled += (s, e) => changed = i++;
            child.DetachedFromVisualTreeCalled += (s, e) => detached = i++;

            target.ClearChildren();

            Assert.Equal(1, changed);
            Assert.Equal(2, detached);
        }
    }
}
