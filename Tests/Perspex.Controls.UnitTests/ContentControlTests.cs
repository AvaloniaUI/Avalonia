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
    using Moq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Templates;
    using Perspex.Layout;
    using Perspex.Platform;
    using Perspex.Styling;
    using Perspex.VisualTree;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Splat;
    using Xunit;

    public class ContentControlTests
    {
        [Fact]
        public void Template_Should_Be_Instantiated()
        {
            using (var ctx = this.RegisterServices())
            {
                var target = new ContentControl();
                target.Content = "Foo";
                target.Template = this.GetTemplate();

                target.Measure(new Size(100, 100));

                var child = ((IVisual)target).VisualChildren.Single();
                Assert.IsType<Border>(child);
                child = child.VisualChildren.Single();
                Assert.IsType<ContentPresenter>(child);
                child = child.VisualChildren.Single();
                Assert.IsType<TextBlock>(child);
            }
        }

        [Fact]
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
                root.Child = target;

                target.ApplyTemplate();

                styler.Verify(x => x.ApplyStyles(It.IsAny<ContentControl>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<Border>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<ContentPresenter>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<TextBlock>()), Times.Once());
            }
        }

        [Fact]
        public void ContentPresenter_Should_Have_TemplatedParent_Set()
        {
            var target = new ContentControl();
            var child = new Border();

            target.Template = this.GetTemplate();
            target.Content = child;
            target.ApplyTemplate();

            var contentPresenter = child.GetVisualParent<ContentPresenter>();
            Assert.Equal(target, contentPresenter.TemplatedParent);
        }

        [Fact]
        public void Content_Should_Have_TemplatedParent_Set_To_Null()
        {
            var target = new ContentControl();
            var child = new Border();

            target.Template = this.GetTemplate();
            target.Content = child;
            target.ApplyTemplate();

            Assert.Null(child.TemplatedParent);
        }

        [Fact]
        public void Setting_Content_To_Control_Should_Set_Child_Controls_Parent()
        {
            var target = new ContentControl
            {
                Template = this.GetTemplate(),
            };

            var child = new Control();
            target.Content = child;
            target.ApplyTemplate();

            Assert.Equal(child.Parent, target);
            Assert.Equal(((ILogical)child).LogicalParent, target);
        }

        [Fact]
        public void Setting_Content_To_String_Should_Set_Child_Controls_Parent()
        {
            var target = new ContentControl
            {
                Template = this.GetTemplate(),
            };

            target.Content = "Foo";
            target.ApplyTemplate();

            var child = target.Presenter.Child;

            Assert.Equal(child.Parent, target);
            Assert.Equal(((ILogical)child).LogicalParent, target);
        }

        [Fact]
        public void Clearing_Content_Should_Clear_Child_Controls_Parent()
        {
            var target = new ContentControl();
            var child = new Control();

            target.Content = child;
            target.Content = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Setting_Content_To_Control_Should_Make_Control_Appear_In_LogicalChildren()
        {
            var target = new ContentControl();
            var child = new Control();

            target.Template = this.GetTemplate();
            target.Content = child;
            target.ApplyTemplate();

            Assert.Equal(new[] { child }, ((ILogical)target).LogicalChildren.ToList());
        }

        [Fact]
        public void Setting_Content_To_String_Should_Make_TextBlock_Appear_In_LogicalChildren()
        {
            var target = new ContentControl();
            var child = new Control();

            target.Template = this.GetTemplate();
            target.Content = "Foo";
            target.ApplyTemplate();

            var logical = (ILogical)target;
            Assert.Equal(1, logical.LogicalChildren.Count);
            Assert.IsType<TextBlock>(logical.LogicalChildren[0]);
        }

        [Fact]
        public void Clearing_Content_Should_Remove_From_LogicalChildren()
        {
            var target = new ContentControl();
            var child = new Control();

            target.Template = this.GetTemplate();
            target.Content = child;
            target.ApplyTemplate();

            target.Content = null;

            // Need to call ApplyTemplate on presenter for LogocalChildren to be updated.
            var presenter = target.GetTemplateChildren().Single(x => x.Name == "contentPresenter");
            presenter.ApplyTemplate();

            Assert.Equal(new ILogical[0], ((ILogical)target).LogicalChildren.ToList());
        }

        [Fact]
        public void Setting_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var contentControl = new ContentControl();
            var child = new Control();
            var called = false;

            ((ILogical)contentControl).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            contentControl.Template = this.GetTemplate();
            contentControl.Content = child;
            contentControl.ApplyTemplate();

            // Need to call ApplyTemplate on presenter for CollectionChanged to be called.
            var presenter = contentControl.GetTemplateChildren().Single(x => x.Name == "contentPresenter");
            presenter.ApplyTemplate();

            Assert.True(called);
        }

        [Fact]
        public void Clearing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var contentControl = new ContentControl();
            var child = new Control();
            var called = false;

            contentControl.Template = this.GetTemplate();
            contentControl.Content = child;
            contentControl.ApplyTemplate();

            ((ILogical)contentControl).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            contentControl.Content = null;

            // Need to call ApplyTemplate on presenter for CollectionChanged to be called.
            var presenter = contentControl.GetTemplateChildren().Single(x => x.Name == "contentPresenter");
            presenter.ApplyTemplate();

            Assert.True(called);
        }

        [Fact]
        public void Changing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var contentControl = new ContentControl();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            contentControl.Template = this.GetTemplate();
            contentControl.Content = child1;
            contentControl.ApplyTemplate();

            ((ILogical)contentControl).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Replace;

            contentControl.Content = child2;

            // Need to call ApplyTemplate on presenter for CollectionChanged to be called.
            var presenter = contentControl.GetTemplateChildren().Single(x => x.Name == "contentPresenter");
            presenter.ApplyTemplate();

            Assert.True(called);
        }

        [Fact]
        public void Changing_Content_Should_Update_Presenter()
        {
            var target = new ContentControl();

            target.Template = this.GetTemplate();
            target.ApplyTemplate();

            target.Content = "Foo";
            target.Presenter.ApplyTemplate();
            Assert.Equal("Foo", ((TextBlock)target.Presenter.Child).Text);
            target.Content = "Bar";
            target.Presenter.ApplyTemplate();
            Assert.Equal("Bar", ((TextBlock)target.Presenter.Child).Text);
        }

        private ControlTemplate GetTemplate()
        {
            return new ControlTemplate<ContentControl>(parent =>
            {
                return new Border
                {
                    Background = new Perspex.Media.SolidColorBrush(0xffffffff),
                    Child = new ContentPresenter
                    {
                        Name = "contentPresenter",
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
