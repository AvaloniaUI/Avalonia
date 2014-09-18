// -----------------------------------------------------------------------
// <copyright file="ContentControlTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Perspex.Controls;
    using Perspex.Layout;
    using Perspex.Styling;
    using Splat;

    [TestClass]
    public class ContentControlTests
    {
        [TestMethod]
        public void Template_Should_Be_Instantiated()
        {
            var target = new ContentControl();
            target.Content = "Foo";
            target.Template = this.GetTemplate();

            var child = ((IVisual)target).VisualChildren.Single();
            Assert.IsInstanceOfType(child, typeof(Border));
            child = child.VisualChildren.Single();
            Assert.IsInstanceOfType(child, typeof(ContentPresenter));
            child = child.VisualChildren.Single();
            Assert.IsInstanceOfType(child, typeof(TextBlock));
        }

        [TestMethod]
        public void Templated_Children_Should_Be_Styled()
        {
            using (Locator.CurrentMutable.WithResolver())
            {
                var root = new TestRoot();
                var target = new ContentControl();
                var styler = new Mock<IStyler>();

                Locator.CurrentMutable.Register(() => styler.Object, typeof(IStyler));
                target.Content = "Foo";
                target.Template = this.GetTemplate();
                root.Content = target;

                this.ApplyTemplate(target);

                styler.Verify(x => x.ApplyStyles(It.IsAny<ContentControl>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<Border>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<ContentPresenter>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<TextBlock>()), Times.Once());
            }
        }

        [TestMethod]
        public void Setting_Content_To_Control_Should_Set_Parent()
        {
            throw new NotImplementedException();
            ////var target = new ContentControl();
            ////var child = new Border();

            ////target.Content = child;

            ////Assert.AreEqual(child.Parent, target);
            ////Assert.AreEqual(((IVisual)child).VisualParent, target);
            ////Assert.AreEqual(((ILogical)child).LogicalParent, target);
        }

        [TestMethod]
        public void Setting_Content_To_Control_Should_Set_Logical_Child()
        {
            throw new NotImplementedException();
            ////var target = new ContentControl();
            ////var child = new Border();

            ////target.Content = child;

            ////Assert.AreEqual(child, ((ILogical)target).LogicalChildren.Single());
        }

        [TestMethod]
        public void Removing_Control_From_Content_Should_Clear_Parent()
        {
            throw new NotImplementedException();
            ////var target = new ContentControl();
            ////var child = new Border();

            ////target.Content = child;
            ////target.Content = "foo";

            ////Assert.IsNull(child.Parent);
            ////Assert.IsNull(((IVisual)child).VisualParent);
            ////Assert.IsNull(((ILogical)child).LogicalParent);
        }

        [TestMethod]
        public void Removing_Control_From_Content_Should_Clear_Logical_Child()
        {
            throw new NotImplementedException();
            ////var target = new ContentControl();
            ////var child = new Border();

            ////target.Content = child;
            ////target.Content = "foo";

            ////Assert.IsFalse(((ILogical)target).LogicalChildren.Any());
        }

        [TestMethod]
        public void ContentPresenter_Should_Have_TemplatedParent_Set()
        {
            var target = new ContentControl();
            var child = new Border();

            target.Template = this.GetTemplate();
            target.Content = child;
            this.ApplyTemplate(target);

            var contentPresenter = child.GetVisualParent<ContentPresenter>();
            Assert.AreEqual(target, contentPresenter.TemplatedParent);
        }

        [TestMethod]
        public void Content_Should_Have_TemplatedParent_Set_To_Null()
        {
            var target = new ContentControl();
            var child = new Border();

            target.Template = this.GetTemplate();
            target.Content = child;
            this.ApplyTemplate(target);

            Assert.IsNull(child.TemplatedParent);
        }

        private void ApplyTemplate(IVisual visual)
        {
            var c = visual.GetVisualDescendents().ToList();
        }

        private ControlTemplate GetTemplate()
        {
            return new ControlTemplate(parent =>
            {
                Border border = new Border();
                border.Background = new Perspex.Media.SolidColorBrush(0xffffffff);
                ContentPresenter contentPresenter = new ContentPresenter();
                ////contentPresenter[~ContentPresenter.ContentProperty] = parent[~ContentPresenter.ContentProperty];
                border.Content = contentPresenter;
                return border;
            });
        }
    }
}
