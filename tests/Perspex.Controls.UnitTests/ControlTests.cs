// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Moq;
using Perspex.Layout;
using Perspex.Platform;
using Perspex.Rendering;
using Perspex.Styling;
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
        public void Adding_Control_To_IRenderRoot_Should_Style_Control()
        {
            using (PerspexLocator.EnterScope())
            {
                var root = new TestRoot();
                var target = new Control();
                var styler = new Mock<IStyler>();

                PerspexLocator.CurrentMutable.Bind<IStyler>().ToConstant(styler.Object);

                root.Child = target;

                styler.Verify(x => x.ApplyStyles(target), Times.Once());
            }
        }

        [Fact]
        public void Adding_Tree_To_ILayoutRoot_Should_Style_Controls()
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

        private class TestRoot : Decorator, ILayoutRoot, IRenderRoot
        {
            public Size ClientSize
            {
                get { throw new NotImplementedException(); }
            }

            public ILayoutManager LayoutManager
            {
                get { throw new NotImplementedException(); }
            }

            public IRenderingViewport Viewport
            {
                get { throw new NotImplementedException(); }
            }

            public IRenderQueueManager RenderQueueManager
            {
                get { throw new NotImplementedException(); }
            }

            public Point TranslatePointToScreen(Point p)
            {
                throw new NotImplementedException();
            }
        }
    }
}
