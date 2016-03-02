// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Moq;
using Perspex.Styling;
using Perspex.UnitTests;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class ControlTests
    {
        [Fact]
        public void Classes_Should_Initially_Be_Empty()
        {
            var target = new Control();

            Assert.Equal(0, target.Classes.Count);
        }

        [Fact]
        public void LogicalParent_Should_Be_Set_To_Parent()
        {
            var parent = new Decorator();
            var target = new TestControl();

            parent.Child = target;

            Assert.Equal(parent, target.InheritanceParent);
        }

        [Fact]
        public void LogicalParent_Should_Be_Cleared_When_Removed_From_Parent()
        {
            var parent = new Decorator();
            var target = new TestControl();

            parent.Child = target;
            parent.Child = null;

            Assert.Null(target.InheritanceParent);
        }

        [Fact]
        public void AttachedToLogicalParent_Should_Be_Called_When_Added_To_Tree()
        {
            var root = new TestRoot();
            var parent = new Border();
            var child = new Border();
            var grandchild = new Border();
            var parentRaised = false;
            var childRaised = false;
            var grandchildRaised = false;

            parent.AttachedToLogicalTree += (s, e) => parentRaised = true;
            child.AttachedToLogicalTree += (s, e) => childRaised = true;
            grandchild.AttachedToLogicalTree += (s, e) => grandchildRaised = true;

            parent.Child = child;
            child.Child = grandchild;

            Assert.False(parentRaised);
            Assert.False(childRaised);
            Assert.False(grandchildRaised);

            root.Child = parent;

            Assert.True(parentRaised);
            Assert.True(childRaised);
            Assert.True(grandchildRaised);
        }

        [Fact]
        public void AttachedToLogicalParent_Should_Be_Called_Before_Parent_Change_Signalled()
        {
            var root = new TestRoot();
            var child = new Border();
            var raised = new List<string>();

            child.AttachedToLogicalTree += (s, e) =>
            {
                Assert.Equal(root, child.Parent);
                raised.Add("attached");
            };

            child.GetObservable(Control.ParentProperty).Skip(1).Subscribe(_ => raised.Add("parent"));

            root.Child = child;

            Assert.Equal(new[] { "attached", "parent" }, raised);
        }

        [Fact]
        public void DetachedToLogicalParent_Should_Be_Called_When_Removed_From_Tree()
        {
            var root = new TestRoot();
            var parent = new Border();
            var child = new Border();
            var grandchild = new Border();
            var parentRaised = false;
            var childRaised = false;
            var grandchildRaised = false;

            parent.Child = child;
            child.Child = grandchild;
            root.Child = parent;

            parent.DetachedFromLogicalTree += (s, e) => parentRaised = true;
            child.DetachedFromLogicalTree += (s, e) => childRaised = true;
            grandchild.DetachedFromLogicalTree += (s, e) => grandchildRaised = true;

            root.Child = null;

            Assert.True(parentRaised);
            Assert.True(childRaised);
            Assert.True(grandchildRaised);
        }

        [Fact]
        public void Adding_Tree_To_IStyleRoot_Should_Style_Controls()
        {
            using (PerspexLocator.EnterScope())
            {
                var root = new TestRoot();
                var parent = new Border();
                var child = new Border();
                var grandchild = new Control();
                var styler = new Mock<IStyler>();

                PerspexLocator.CurrentMutable.Bind<IStyler>().ToConstant(styler.Object);

                parent.Child = child;
                child.Child = grandchild;

                styler.Verify(x => x.ApplyStyles(It.IsAny<IStyleable>()), Times.Never());

                root.Child = parent;

                styler.Verify(x => x.ApplyStyles(parent), Times.Once());
                styler.Verify(x => x.ApplyStyles(child), Times.Once());
                styler.Verify(x => x.ApplyStyles(grandchild), Times.Once());
            }
        }

        [Fact]
        public void Styles_Not_Applied_Until_Initialization_Finished()
        {
            using (PerspexLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();
                var styler = new Mock<IStyler>();

                PerspexLocator.CurrentMutable.Bind<IStyler>().ToConstant(styler.Object);

                ((ISupportInitialize)child).BeginInit();
                root.Child = child;
                styler.Verify(x => x.ApplyStyles(It.IsAny<IStyleable>()), Times.Never());

                ((ISupportInitialize)child).EndInit();
                styler.Verify(x => x.ApplyStyles(child), Times.Once());
            }
        }

        [Fact]
        public void Adding_To_Logical_Tree_Should_Register_With_NameScope()
        {
            using (PerspexLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();

                child.Name = "foo";
                root.Child = child;

                Assert.Same(root.FindControl<Border>("foo"), child);
            }
        }

        [Fact]
        public void Name_Cannot_Be_Set_After_Added_To_Logical_Tree()
        {
            using (PerspexLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();

                root.Child = child;

                Assert.Throws<InvalidOperationException>(() => child.Name = "foo");
            }
        }

        [Fact]
        public void Name_Can_Be_Set_While_Initializing()
        {
            using (PerspexLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();

                ((ISupportInitialize)child).BeginInit();
                root.Child = child;
                child.Name = "foo";
                Assert.Null(root.FindControl<Border>("foo"));
                ((ISupportInitialize)child).EndInit();

                Assert.Same(root.FindControl<Border>("foo"), child);
            }
        }

        private class TestControl : Control
        {
            public new PerspexObject InheritanceParent => base.InheritanceParent;
        }
    }
}
