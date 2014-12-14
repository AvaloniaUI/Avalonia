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
    using Perspex.Controls.Presenters;
    using Perspex.Layout;
    using Perspex.Platform;
    using Perspex.Styling;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Splat;

    [TestClass]
    public class ContentControlTests
    {
        [TestMethod]
        public void Template_Should_Be_Instantiated()
        {
            using (var ctx = this.RegisterServices())
            {
                var target = new ContentControl();
                target.Content = "Foo";
                target.Template = this.GetTemplate();

                target.Measure(new Size(100, 100));

                var child = ((IVisual)target).VisualChildren.Single();
                Assert.IsInstanceOfType(child, typeof(Border));
                child = child.VisualChildren.Single();
                Assert.IsInstanceOfType(child, typeof(ContentPresenter));
                child = child.VisualChildren.Single();
                Assert.IsInstanceOfType(child, typeof(TextBlock));
            }
        }

        [TestMethod]
        public void Templated_Children_Should_Be_Styled()
        {
            using (var ctx = this.RegisterServices())
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

        private void ApplyTemplate(ILayoutable control)
        {
            control.Measure(new Size(100, 100));
        }

        private ControlTemplate GetTemplate()
        {
            return ControlTemplate.Create<ContentControl>(parent =>
            {
                return new Border
                {
                    Background = new Perspex.Media.SolidColorBrush(0xffffffff),
                    Content = new ContentPresenter
                    {
                        [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                    }
                };
            });
        }

        private IDisposable RegisterServices()
        {
            var result = Locator.CurrentMutable.WithResolver();
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var renderInterface = fixture.Create<IPlatformRenderInterface>();
            Locator.CurrentMutable.RegisterConstant(renderInterface, typeof(IPlatformRenderInterface));
            return result;
        }
    }
}
