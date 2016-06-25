// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class ContentPresenterTests
    {
        [Fact]
        public void Should_Register_With_Host_When_TemplatedParent_Set()
        {
            var host = new Mock<IContentPresenterHost>();
            var target = new ContentPresenter();

            target.SetValue(Control.TemplatedParentProperty, host.Object);

            host.Verify(x => x.RegisterContentPresenter(target));
        }

        [Fact]
        public void Setting_Content_To_Control_Should_Set_Child()
        {
            var target = new ContentPresenter();
            var child = new Border();

            target.Content = child;

            Assert.Null(target.Child);
            target.UpdateChild();
            Assert.Equal(child, target.Child);
        }

        [Fact]
        public void Setting_Content_To_String_Should_Create_TextBlock()
        {
            var target = new ContentPresenter();

            target.Content = "Foo";

            Assert.Null(target.Child);
            target.UpdateChild();
            Assert.IsType<TextBlock>(target.Child);
            Assert.Equal("Foo", ((TextBlock)target.Child).Text);
        }

        [Fact]
        public void Control_Content_Should_Not_Be_NameScope()
        {
            var target = new ContentPresenter();

            target.Content = new TextBlock();

            Assert.Null(target.Child);
            target.UpdateChild();
            Assert.IsType<TextBlock>(target.Child);
            Assert.Null(NameScope.GetNameScope((Control)target.Child));
        }

        [Fact]
        public void DataTemplate_Created_Control_Should_Be_NameScope()
        {
            var target = new ContentPresenter();

            target.Content = "Foo";

            Assert.Null(target.Child);
            target.UpdateChild();
            Assert.IsType<TextBlock>(target.Child);
            Assert.NotNull(NameScope.GetNameScope((Control)target.Child));
        }

        [Fact]
        public void Should_Set_Childs_Parent_To_TemplatedParent()
        {
            var content = new Border();
            var target = new TestContentControl
            {
                Template = new FuncControlTemplate<TestContentControl>(parent =>
                    new ContentPresenter { Content = parent.Child }),
                Child = content,
            };

            target.ApplyTemplate();
            var presenter = ((ContentPresenter)target.GetVisualChildren().Single());
            presenter.UpdateChild();

            Assert.Same(target, content.Parent);
        }

        [Fact]
        public void Should_Set_Childs_Parent_To_Itself_Outside_Template()
        {
            var content = new Border();
            var target = new ContentPresenter { Content = content };

            target.UpdateChild();

            Assert.Same(target, content.Parent);
        }

        [Fact]
        public void Should_Add_Child_To_Own_LogicalChildren_Outside_Template()
        {
            var content = new Border();
            var target = new ContentPresenter { Content = content };

            target.UpdateChild();

            var logicalChildren = target.GetLogicalChildren();

            Assert.Equal(1, logicalChildren.Count());
            Assert.Equal(content, logicalChildren.First());
        }

        [Fact]
        public void Adding_To_Logical_Tree_Should_Reevaluate_DataTemplates()
        {
            var target = new ContentPresenter
            {
                Content = "Foo",
            };

            target.UpdateChild();
            Assert.IsType<TextBlock>(target.Child);

            var root = new TestRoot
            {
                DataTemplates = new DataTemplates
                {
                    new FuncDataTemplate<string>(x => new Decorator()),
                },
            };

            root.Child = target;
            target.ApplyTemplate();
            Assert.IsType<Decorator>(target.Child);
        }

        [Fact]
        public void Assigning_Control_To_Content_Should_Not_Set_DataContext()
        {
            var target = new ContentPresenter
            {
                Content = new Border(),
            };

            Assert.False(target.IsSet(Control.DataContextProperty));
        }

        [Fact]
        public void Assigning_NonControl_To_Content_Should_Set_DataContext_On_UpdateChild()
        {
            var target = new ContentPresenter
            {
                Content = "foo",
            };

            target.UpdateChild();

            Assert.Equal("foo", target.DataContext);
        }

        [Fact]
        public void Tries_To_Recycle_DataTemplate()
        {
            var target = new ContentPresenter
            {
                DataTemplates = new DataTemplates
                {
                    new FuncDataTemplate<string>(_ => new Border(), true),
                },
                Content = "foo",
            };

            target.UpdateChild();
            var control = target.Child;

            Assert.IsType<Border>(control);

            target.Content = "bar";
            target.UpdateChild();

            Assert.Same(control, target.Child);
        }

        [Fact]
        public void Should_Raise_DetachedFromLogicalTree_On_Content_Changed_OutsideTemplate()
        {
            var target = new ContentPresenter
            {
                ContentTemplate =
                    new FuncDataTemplate<string>(t => new ContentControl() { Content = t }, false)
            };

            var parentMock = new Mock<Control>();
            parentMock.As<IContentPresenterHost>();
            parentMock.As<IStyleRoot>();

            (target as ISetLogicalParent).SetParent(parentMock.Object);

            target.Content = "foo";

            target.UpdateChild();

            var foo = target.Child as ContentControl;

            bool foodetached = false;

            Assert.NotNull(foo);
            Assert.Equal("foo", foo.Content);

            foo.DetachedFromLogicalTree += delegate { foodetached = true; };

            target.Content = "bar";
            target.UpdateChild();

            var bar = target.Child as ContentControl;

            Assert.NotNull(bar);
            Assert.True(bar != foo);
            Assert.False((foo as IControl).IsAttachedToLogicalTree);
            Assert.True(foodetached);
        }

        [Fact]
        public void Should_Raise_DetachedFromLogicalTree_In_ContentControl_On_Content_Changed_OutsideTemplate()
        {
            var contentControl = new ContentControl
            {
                Template = new FuncControlTemplate<ContentControl>(c => new ContentPresenter()
                {
                    Name = "PART_ContentPresenter",
                    [~ContentPresenter.ContentProperty] = c[~ContentControl.ContentProperty],
                    [~ContentPresenter.ContentTemplateProperty] = c[~ContentControl.ContentTemplateProperty]
                }),
                ContentTemplate =
                    new FuncDataTemplate<string>(t => new ContentControl() { Content = t }, false)
            };

            var parentMock = new Mock<Control>();
            parentMock.As<IStyleRoot>();
            parentMock.As<ILogical>().SetupGet(l => l.IsAttachedToLogicalTree).Returns(true);

            (contentControl as ISetLogicalParent).SetParent(parentMock.Object);

            contentControl.ApplyTemplate();
            var target = contentControl.Presenter as ContentPresenter;

            contentControl.Content = "foo";

            target.UpdateChild();

            var tbfoo = target.Child as ContentControl;

            bool foodetached = false;

            Assert.NotNull(tbfoo);
            Assert.Equal("foo", tbfoo.Content);

            tbfoo.DetachedFromLogicalTree += delegate { foodetached = true; };

            contentControl.Content = "bar";
            target.UpdateChild();

            var tbbar = target.Child as ContentControl;

            Assert.NotNull(tbbar);
            Assert.True(tbbar != tbfoo);
            Assert.False((tbfoo as IControl).IsAttachedToLogicalTree);
            Assert.True(foodetached);
        }

        [Fact]
        public void Should_Raise_DetachedFromLogicalTree_On_Detached_OutsideTemplate()
        {
            var target = new ContentPresenter
            {
                ContentTemplate =
                    new FuncDataTemplate<string>(t => new ContentControl() { Content = t }, false)
            };

            var parentMock = new Mock<Control>();
            parentMock.As<IContentPresenterHost>();
            parentMock.As<IStyleRoot>();

            (target as ISetLogicalParent).SetParent(parentMock.Object);

            target.Content = "foo";

            target.UpdateChild();

            var foo = target.Child as ContentControl;

            bool foodetached = false;

            Assert.NotNull(foo);
            Assert.Equal("foo", foo.Content);

            foo.DetachedFromLogicalTree += delegate { foodetached = true; };

            (target as ISetLogicalParent).SetParent(null);

            Assert.False((foo as IControl).IsAttachedToLogicalTree);
            Assert.True(foodetached);
        }

        [Fact]
        public void Should_Remove_Old_Child_From_LogicalChildren_On_ContentChanged_OutsideTemplate()
        {
            var target = new ContentPresenter
            {
                ContentTemplate =
                    new FuncDataTemplate<string>(t => new ContentControl() { Content = t }, false)
            };

            target.Content = "foo";

            target.UpdateChild();

            var foo = target.Child as ContentControl;

            Assert.NotNull(foo);

            var logicalChildren = target.GetLogicalChildren();

            Assert.Equal(1, logicalChildren.Count());

            target.Content = "bar";
            target.UpdateChild();

            Assert.Equal(null, foo.Parent);

            logicalChildren = target.GetLogicalChildren();

            Assert.Equal(1, logicalChildren.Count());
            Assert.NotEqual(foo, logicalChildren.First());
        }

        private class TestContentControl : ContentControl
        {
            public IControl Child { get; set; }
        }
    }
}