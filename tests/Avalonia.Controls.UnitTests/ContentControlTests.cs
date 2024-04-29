using System;
using System.Collections.Specialized;
using System.Linq;
using Moq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;
using Avalonia.Markup.Data;
using Avalonia.Data;
using System.Collections.Generic;

namespace Avalonia.Controls.UnitTests
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
            target.Presenter.UpdateChild();

            var child = target.VisualChildren.Single();
            Assert.IsType<Border>(child);
            child = child.VisualChildren.Single();
            Assert.IsType<ContentPresenter>(child);
            child = child.VisualChildren.Single();
            Assert.IsType<TextBlock>(child);
        }

        [Fact]
        public void Templated_Children_Should_Be_Styled()
        {
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.Is<Control>())
                    {
                        Setters = { new Setter(Control.TagProperty, "foo") }
                    }
                }
            };

            var target = new ContentControl();

            target.Content = "Foo";
            target.Template = GetTemplate();
            root.Child = target;

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            foreach (Control child in target.GetTemplateChildren())
                Assert.Equal("foo", child.Tag);
        }

        [Fact]
        public void ContentPresenter_Should_Have_TemplatedParent_Set()
        {
            var target = new ContentControl();
            var child = new Border();

            target.Template = GetTemplate();
            target.Content = child;
            target.ApplyTemplate();
            target.Presenter.UpdateChild();

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
            target.Presenter.UpdateChild();

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
            target.Presenter.UpdateChild();

            Assert.Equal(child.Parent, target);
            Assert.Equal(child.GetLogicalParent(), target);
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Should_Use_ContentTemplate_To_Create_Control()
        {
            var target = new ContentControl
            {
                Template = GetTemplate(),
                ContentTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
            };

            target.Content = "Foo";
            target.ApplyTemplate();
            target.Presenter.UpdateChild();

            var child = target.Presenter.Child;

            Assert.IsType<Canvas>(child);
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
            target.Presenter.UpdateChild();

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
            target.Presenter.UpdateChild();

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
            target.Presenter.UpdateChild();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            target.Content = null;
            target.Presenter.UpdateChild();

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
            target.Presenter.UpdateChild();

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
            target.Presenter.UpdateChild();

            target.Content = "Foo";
            target.Presenter.UpdateChild();
            Assert.Equal("Foo", ((TextBlock)target.Presenter.Child).Text);
            target.Content = "Bar";
            target.Presenter.UpdateChild();
            Assert.Equal("Bar", ((TextBlock)target.Presenter.Child).Text);
        }

        [Fact]
        public void DataContext_Should_Be_Set_For_DataTemplate_Created_Content()
        {
            var target = new ContentControl();

            target.Template = GetTemplate();
            target.Content = "Foo";
            target.ApplyTemplate();
            target.Presenter.UpdateChild();

            Assert.Equal("Foo", target.Presenter.Child.DataContext);
        }

        [Fact]
        public void DataContext_Should_Not_Be_Set_For_Control_Content()
        {
            var target = new ContentControl();

            target.Template = GetTemplate();
            target.Content = new TextBlock();
            target.ApplyTemplate();
            target.Presenter.UpdateChild();

            Assert.Null(target.Presenter.Child.DataContext);
        }

        [Fact]
        public void Binding_ContentTemplate_After_Content_Does_Not_Leave_Orpaned_TextBlock()
        {
            // Test for #1271.
            var children = new List<Control>();
            var presenter = new ContentPresenter();

            // The content and then the content template property need to be bound with delayed bindings
            // as they are in Avalonia.Markup.Xaml.
            DelayedBinding.Add(presenter, ContentPresenter.ContentProperty, new Binding("Content")
            {
                Priority = BindingPriority.Template,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
            });

            DelayedBinding.Add(presenter, ContentPresenter.ContentTemplateProperty, new Binding("ContentTemplate")
            {
                Priority = BindingPriority.Template,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
            });

            presenter.GetObservable(ContentPresenter.ChildProperty).Subscribe(children.Add);

            var target = new ContentControl
            {
                Template = new FuncControlTemplate<ContentControl>((_, __) => presenter),
                ContentTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
                Content = "foo",
            };
                        
            // The control must be rooted.
            var root = new TestRoot
            {
                Child = target,
            };

            target.ApplyTemplate();

            // When the template is applied, the Content property is bound before the ContentTemplate
            // property, causing a TextBlock to be created by the default template before ContentTemplate
            // is bound.
            Assert.Collection(
                children,
                x => Assert.Null(x),
                x => Assert.IsType<TextBlock>(x),
                x => Assert.IsType<Canvas>(x));

            var textBlock = (TextBlock)children[1];

            // The leak in #1271 was caused by the TextBlock's logical parent not being cleared when
            // it is replaced by the Canvas.
            Assert.Null(textBlock.GetLogicalParent());
        }

        [Fact]
        public void Should_Set_Child_LogicalParent_After_Removing_And_Adding_Back_To_Logical_Tree()
        {
            var target = new ContentControl();
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<ContentControl>())
                    {
                        Setters =
                        {
                            new Setter(ContentControl.TemplateProperty, GetTemplate()),
                        }
                    }
                },
                Child = target
            };

            target.Content = "Foo";
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.Equal(target, target.Presenter.Child.GetLogicalParent());
            Assert.Equal(new[] { target.Presenter.Child }, target.LogicalChildren);

            root.Child = null;

            target.Content = null;

            Assert.Empty(target.LogicalChildren);

            root.Child = target;
            target.Content = "Bar";

            Assert.Equal(target, target.Presenter.Child.GetLogicalParent());
            Assert.Equal(new[] { target.Presenter.Child }, target.LogicalChildren);
        }

        private static FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ContentControl>((parent, scope) =>
            {
                return new Border
                {
                    Background = new Media.SolidColorBrush(0xffffffff),
                    Child = new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                        [~ContentPresenter.ContentTemplateProperty] = parent[~ContentControl.ContentTemplateProperty],
                    }.RegisterInNameScope(scope)
                };
            });
        }
    }
}
