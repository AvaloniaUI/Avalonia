// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Specialized;
using System.Linq;
using Moq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.LogicalTree;
using Perspex.Styling;
using Perspex.UnitTests;
using Perspex.VisualTree;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class ContentControlTests
    {
        [Fact]
        public void Template_Should_Be_Instantiated()
        {
            var target = new ContentControl();
            target.Content = "Foo";
            target.Template = GetTemplate();
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            var child = ((IVisual)target).VisualChildren.Single();
            Assert.IsType<Border>(child);
            child = child.VisualChildren.Single();
            Assert.IsType<ContentPresenter>(child);
            child = child.VisualChildren.Single();
            Assert.IsType<TextBlock>(child);
        }

        [Fact]
        public void Templated_Children_Should_Be_Styled()
        {
            var root = new TestRoot();
            var target = new ContentControl();
            var styler = new Mock<IStyler>();

            PerspexLocator.CurrentMutable.Bind<IStyler>().ToConstant(styler.Object);
            target.Content = "Foo";
            target.Template = GetTemplate();
            root.Child = target;

            target.ApplyTemplate();

            styler.Verify(x => x.ApplyStyles(It.IsAny<ContentControl>()), Times.Once());
            styler.Verify(x => x.ApplyStyles(It.IsAny<Border>()), Times.Once());
            styler.Verify(x => x.ApplyStyles(It.IsAny<ContentPresenter>()), Times.Once());
            styler.Verify(x => x.ApplyStyles(It.IsAny<TextBlock>()), Times.Once());
        }

        [Fact]
        public void ContentPresenter_Should_Have_TemplatedParent_Set()
        {
            var target = new ContentControl();
            var child = new Border();

            target.Template = GetTemplate();
            target.Content = child;
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            var contentPresenter = child.GetVisualParent<ContentPresenter>();
            Assert.Equal(target, contentPresenter.TemplatedParent);
        }

        [Fact]
        public void Content_Should_Have_TemplatedParent_Set_To_Null()
        {
            var target = new ContentControl();
            var child = new Border();

            target.Template = GetTemplate();
            target.Content = child;
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            Assert.Null(child.TemplatedParent);
        }

        [Fact]
        public void Control_Content_Should_Be_Logical_Child_Before_ApplyTemplate()
        {
            var target = new ContentControl
            {
                Template = GetTemplate(),
            };

            var child = new Control();
            target.Content = child;

            Assert.Equal(child.Parent, target);
            Assert.Equal(child.GetLogicalParent(), target);
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Control_Content_Should_Be_Logical_Child_After_ApplyTemplate()
        {
            var target = new ContentControl
            {
                Template = GetTemplate(),
            };

            var child = new Control();
            target.Content = child;
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            Assert.Equal(child.Parent, target);
            Assert.Equal(child.GetLogicalParent(), target);
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void DataTemplate_Created_Control_Should_Be_Logical_Child_After_ApplyTemplate()
        {
            var target = new ContentControl
            {
                Template = GetTemplate(),
            };

            target.Content = "Foo";
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            var child = target.Presenter.Child;

            Assert.NotNull(child);
            Assert.Equal(target, child.Parent);
            Assert.Equal(target, child.GetLogicalParent());
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Clearing_Content_Should_Clear_Logical_Child()
        {
            var target = new ContentControl();
            var child = new Control();

            target.Content = child;

            Assert.Equal(new[] { child }, target.GetLogicalChildren());

            target.Content = null;

            Assert.Null(child.Parent);
            Assert.Null(child.GetLogicalParent());
            Assert.Empty(target.GetLogicalChildren());
        }

        [Fact]
        public void Setting_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ContentControl();
            var child = new Control();
            var called = false;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            target.Template = GetTemplate();
            target.Content = child;
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            Assert.True(called);
        }

        [Fact]
        public void Clearing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ContentControl();
            var child = new Control();
            var called = false;

            target.Template = GetTemplate();
            target.Content = child;
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            target.Content = null;
            ((ContentPresenter)target.Presenter).UpdateChild();

            Assert.True(called);
        }

        [Fact]
        public void Changing_Content_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ContentControl();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            target.Template = GetTemplate();
            target.Content = child1;
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            target.Content = child2;
            target.Presenter.ApplyTemplate();

            Assert.True(called);
        }

        [Fact]
        public void Changing_Content_Should_Update_Presenter()
        {
            var target = new ContentControl();

            target.Template = GetTemplate();
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            target.Content = "Foo";
            ((ContentPresenter)target.Presenter).UpdateChild();
            Assert.Equal("Foo", ((TextBlock)target.Presenter.Child).Text);
            target.Content = "Bar";
            ((ContentPresenter)target.Presenter).UpdateChild();
            Assert.Equal("Bar", ((TextBlock)target.Presenter.Child).Text);
        }

        [Fact]
        public void DataContext_Should_Be_Set_For_DataTemplate_Created_Content()
        {
            var target = new ContentControl();

            target.Template = GetTemplate();
            target.Content = "Foo";
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            Assert.Equal("Foo", target.Presenter.Child.DataContext);
        }

        [Fact]
        public void DataContext_Should_Not_Be_Set_For_Control_Content()
        {
            var target = new ContentControl();

            target.Template = GetTemplate();
            target.Content = new TextBlock();
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            Assert.Null(target.Presenter.Child.DataContext);
        }

        private FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ContentControl>(parent =>
            {
                return new Border
                {
                    Background = new Media.SolidColorBrush(0xffffffff),
                    Child = new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                    }
                };
            });
        }
    }
}
