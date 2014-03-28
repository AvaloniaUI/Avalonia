// -----------------------------------------------------------------------
// <copyright file="ContentControlTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
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
    using Perspex.Themes.Default;
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

        private void ApplyTemplate(IVisual visual)
        {
            foreach (IVisual child in visual.VisualChildren)
            {
                this.ApplyTemplate(child);
            }
        }

        private ControlTemplate GetTemplate()
        {
            return new ControlTemplate(parent =>
            {
                Border border = new Border();
                border.Background = new Perspex.Media.SolidColorBrush(0xffffffff);
                ContentPresenter contentPresenter = new ContentPresenter();
                contentPresenter.Bind(
                    ContentPresenter.ContentProperty, 
                    parent.GetObservable(ContentControl.ContentProperty),
                    BindingPriority.TemplatedParent);
                border.Content = contentPresenter;
                return border;
            });
        }
    }
}
