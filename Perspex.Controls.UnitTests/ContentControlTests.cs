// -----------------------------------------------------------------------
// <copyright file="ContentControlTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System;
    using System.Collections.Specialized;
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

        [TestMethod]
        public void Setting_Content_Should_Set_Child_Controls_Parent()
        {
            var target = new ContentControl();
            var child = new Control();

            target.Content = child;

            Assert.AreEqual(child.Parent, target);
            Assert.AreEqual(((ILogical)child).LogicalParent, target);
        }

        [TestMethod]
        public void Clearing_Content_Should_Clear_Child_Controls_Parent()
        {
            var target = new ContentControl();
            var child = new Control();

            target.Content = child;
            target.Content = null;

            Assert.IsNull(child.Parent);
            Assert.IsNull(((ILogical)child).LogicalParent);
        }

        [TestMethod]
        public void Setting_Content_To_Control_Should_Make_Control_Appear_In_LogicalChildren()
        {
            var target = new ContentControl();
            var child = new Control();

            target.Content = child;

            CollectionAssert.AreEqual(new[] { child }, ((ILogical)target).LogicalChildren.ToList());
        }

        [TestMethod]
        public void Clearing_Content_Should_Remove_From_LogicalChildren()
        {
            var contentControl = new ContentControl();
            var child = new Control();

            contentControl.Content = child;
            contentControl.Content = null;

            CollectionAssert.AreEqual(new ILogical[0], ((ILogical)contentControl).LogicalChildren.ToList());
        }

        [TestMethod]
        public void Setting_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var contentControl = new ContentControl();
            var child = new Control();
            var called = false;

            ((ILogical)contentControl).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            contentControl.Content = child;

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Clearing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var contentControl = new ContentControl();
            var child = new Control();
            var called = false;

            contentControl.Content = child;

            ((ILogical)contentControl).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            contentControl.Content = null;

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Changing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var contentControl = new ContentControl();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            contentControl.Content = child1;

            ((ILogical)contentControl).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Replace;

            contentControl.Content = child2;

            Assert.IsTrue(called);
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
