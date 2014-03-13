// -----------------------------------------------------------------------
// <copyright file="ControlTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests.Controls
{
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Perspex.Controls;
    using Perspex.Layout;
    using Perspex.Styling;
    using Splat;

    [TestClass]
    public class ControlTests
    {
        [TestMethod]
        public void Classes_Should_Initially_Be_Empty()
        {
            var target = new Control();

            Assert.AreEqual(0, target.Classes.Count);
        }

        [TestMethod]
        public void Setting_IsMouseOver_Should_Add_Mouseover_Class()
        {
            var target = new Control();

            target.IsMouseOver = true;

            CollectionAssert.AreEqual(new[] { ":mouseover" }, target.Classes.ToArray());
        }

        [TestMethod]
        public void Clearing_IsMouseOver_Should_Remove_Mouseover_Class()
        {
            var target = new Control();

            target.IsMouseOver = true;
            target.IsMouseOver = false;

            Assert.AreEqual(0, target.Classes.Count);
        }

        [TestMethod]
        public void Adding_Control_To_ILayoutRoot_Should_Style_Control()
        {
            using (Locator.CurrentMutable.WithResolver())
            {
                var root = new TestRoot();
                var target = new Control();
                var styler = new Mock<IStyler>();

                Locator.CurrentMutable.Register(() => styler.Object, typeof(IStyler));

                root.Content = target;

                styler.Verify(x => x.ApplyStyles(target), Times.Once());
            }
        }

        [TestMethod]
        public void Adding_Tree_To_ILayoutRoot_Should_Style_Controls()
        {
            using (Locator.CurrentMutable.WithResolver())
            {
                var root = new TestRoot();
                var parent = new Border();
                var child = new Border();
                var grandchild = new Control();
                var styler = new Mock<IStyler>();

                Locator.CurrentMutable.Register(() => styler.Object, typeof(IStyler));

                parent.Content = child;
                child.Content = grandchild;

                styler.Verify(x => x.ApplyStyles(It.IsAny<IStyleable>()), Times.Never());

                root.Content = parent;

                styler.Verify(x => x.ApplyStyles(parent), Times.Once());
                styler.Verify(x => x.ApplyStyles(child), Times.Once());
                styler.Verify(x => x.ApplyStyles(grandchild), Times.Once());
            }
        }

        private class TestRoot : Decorator, ILayoutRoot
        {
            public Size ClientSize
            {
                get { throw new System.NotImplementedException(); }
            }

            public ILayoutManager LayoutManager
            {
                get { throw new System.NotImplementedException(); }
            }
        }
    }
}
