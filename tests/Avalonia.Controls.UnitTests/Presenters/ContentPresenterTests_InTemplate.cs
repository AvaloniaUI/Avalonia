// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    /// <summary>
    /// Tests for ContentControls that are hosted in a control template.
    /// </summary>
    public class ContentPresenterTests_InTemplate
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
            var (target, _) = CreateTarget();
            var child = new Border();

            target.Content = child;

            Assert.Equal(child, target.Child);
        }

        [Fact]
        public void Setting_Content_To_Control_Should_Update_Logical_Tree()
        {
            var (target, parent) = CreateTarget();
            var child = new Border();

            target.Content = child;

            Assert.Equal(parent, child.GetLogicalParent());
            Assert.Equal(new[] { child }, parent.GetLogicalChildren());
        }

        [Fact]
        public void Setting_Content_To_Control_Should_Update_Visual_Tree()
        {
            var (target, _) = CreateTarget();
            var child = new Border();

            target.Content = child;

            Assert.Equal(target, child.GetVisualParent());
            Assert.Equal(new[] { child }, target.GetVisualChildren());
        }

        [Fact]
        public void Setting_Content_To_String_Should_Create_TextBlock()
        {
            var (target, _) = CreateTarget();

            target.Content = "Foo";

            Assert.IsType<TextBlock>(target.Child);
            Assert.Equal("Foo", ((TextBlock)target.Child).Text);
        }

        [Fact]
        public void Setting_Content_To_String_Should_Update_Logical_Tree()
        {
            var (target, parent) = CreateTarget();

            target.Content = "Foo";

            var child = target.Child;
            Assert.Equal(parent, child.GetLogicalParent());
            Assert.Equal(new[] { child }, parent.GetLogicalChildren());
        }

        [Fact]
        public void Setting_Content_To_String_Should_Update_Visual_Tree()
        {
            var (target, _) = CreateTarget();

            target.Content = "Foo";

            var child = target.Child;
            Assert.Equal(target, child.GetVisualParent());
            Assert.Equal(new[] { child }, target.GetVisualChildren());
        }

        [Fact]
        public void Clearing_Control_Content_Should_Update_Logical_Tree()
        {
            var (target, _) = CreateTarget();
            var child = new Border();

            target.Content = child;
            target.Content = null;

            Assert.Null(child.GetLogicalParent());
            Assert.Empty(target.GetLogicalChildren());
        }

        [Fact]
        public void Clearing_Control_Content_Should_Update_Visual_Tree()
        {
            var (target, _) = CreateTarget();
            var child = new Border();

            target.Content = child;
            target.Content = null;

            Assert.Null(child.GetVisualParent());
            Assert.Empty(target.GetVisualChildren());
        }

        [Fact]
        public void Control_Content_Should_Not_Be_NameScope()
        {
            var (target, _) = CreateTarget();

            target.Content = new TextBlock();

            Assert.IsType<TextBlock>(target.Child);
            Assert.Null(NameScope.GetNameScope((Control)target.Child));
        }

        [Fact]
        public void Assigning_Control_To_Content_Should_Not_Set_DataContext()
        {
            var (target, _) = CreateTarget();
            target.Content = new Border();

            Assert.False(target.IsSet(Control.DataContextProperty));
        }

        [Fact]
        public void Assigning_NonControl_To_Content_Should_Set_DataContext_On_UpdateChild()
        {
            var (target, _) = CreateTarget();
            target.Content = "foo";

            Assert.Equal("foo", target.DataContext);
        }

        [Fact]
        public void Should_Use_ContentTemplate_If_Specified()
        {
            var (target, _) = CreateTarget();

            target.ContentTemplate = new FuncDataTemplate<string>((_, __) => new Canvas());
            target.Content = "Foo";

            Assert.IsType<Canvas>(target.Child);
        }

        [Fact]
        public void Should_Update_If_ContentTemplate_Changed()
        {
            var (target, _) = CreateTarget();

            target.Content = "Foo";
            Assert.IsType<TextBlock>(target.Child);

            target.ContentTemplate = new FuncDataTemplate<string>((_, __) => new Canvas());
            Assert.IsType<Canvas>(target.Child);

            target.ContentTemplate = null;
            Assert.IsType<TextBlock>(target.Child);
        }

        [Fact]
        public void Assigning_Control_To_Content_After_NonControl_Should_Clear_DataContext()
        {
            var (target, _) = CreateTarget();

            target.Content = "foo";

            Assert.True(target.IsSet(Control.DataContextProperty));

            target.Content = new Border();

            Assert.False(target.IsSet(Control.DataContextProperty));
        }

        [Fact]
        public void Recycles_DataTemplate()
        {
            var (target, _) = CreateTarget();
            target.DataTemplates.Add(new FuncDataTemplate<string>((_, __) => new Border(), true));

            target.Content = "foo";

            var control = target.Child;
            Assert.IsType<Border>(control);

            target.Content = "bar";
            Assert.Same(control, target.Child);
        }

        [Fact]
        public void Detects_DataTemplate_Doesnt_Match_And_Doesnt_Recycle()
        {
            var (target, _) = CreateTarget();
            target.DataTemplates.Add(new FuncDataTemplate<string>(x => x == "foo", _ => new Border(), true));

            target.Content = "foo";

            var control = target.Child;
            Assert.IsType<Border>(control);

            target.Content = "bar";
            Assert.IsType<TextBlock>(target.Child);
        }

        [Fact]
        public void Detects_DataTemplate_Doesnt_Support_Recycling()
        {
            var (target, _) = CreateTarget();
            target.DataTemplates.Add(new FuncDataTemplate<string>((_, __) => new Border(), false));

            target.Content = "foo";

            var control = target.Child;
            Assert.IsType<Border>(control);

            target.Content = "bar";
            Assert.NotSame(control, target.Child);
        }

        [Fact]
        public void Reevaluates_DataTemplates_When_Recycling()
        {
            var (target, _) = CreateTarget();

            target.DataTemplates.Add(new FuncDataTemplate<string>(x => x == "bar", _ => new Canvas(), true));
            target.DataTemplates.Add(new FuncDataTemplate<string>((_, __) => new Border(), true));

            target.Content = "foo";

            var control = target.Child;
            Assert.IsType<Border>(control);

            target.Content = "bar";
            Assert.IsType<Canvas>(target.Child);
        }


        [Fact]
        public void Should_Not_Bind_Old_Child_To_New_DataContext()
        {
            // Test for issue #1099.
            var textBlock = new TextBlock
            {
                [!TextBlock.TextProperty] = new Binding(),
            };

            var (target, host) = CreateTarget();
            host.DataTemplates.Add(new FuncDataTemplate<string>((_, __) => textBlock));
            host.DataTemplates.Add(new FuncDataTemplate<int>((_, __) => new Canvas()));

            target.Content = "foo";
            Assert.Same(textBlock, target.Child);

            textBlock.PropertyChanged += (s, e) =>
            {
                Assert.NotEqual(e.NewValue, "42");
            };

            target.Content = 42;
        }

        (ContentPresenter presenter, ContentControl templatedParent) CreateTarget()
        {
            var templatedParent = new ContentControl
            {
                Template = new FuncControlTemplate<ContentControl>((_, s) => 
                    new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                    }.RegisterInNameScope(s)),
            };
            var root = new TestRoot { Child = templatedParent };

            templatedParent.ApplyTemplate();

            return ((ContentPresenter)templatedParent.Presenter, templatedParent);
        }

        private class TestContentControl : ContentControl
        {
            public IControl Child { get; set; }
        }
    }
}
