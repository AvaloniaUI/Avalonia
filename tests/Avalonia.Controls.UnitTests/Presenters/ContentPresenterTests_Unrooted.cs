// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    /// <summary>
    /// Tests for ContentControls that are not attached to a logical tree.
    /// </summary>
    public class ContentPresenterTests_Unrooted
    {
        [Fact]
        public void Setting_Content_To_Control_Should_Not_Set_Child_Unless_UpdateChild_Called()
        {
            var target = new ContentPresenter();
            var child = new Border();

            target.Content = child;
            Assert.Null(target.Child);

            target.ApplyTemplate();
            Assert.Null(target.Child);

            target.UpdateChild();
            Assert.Equal(child, target.Child);
        }

        [Fact]
        public void Setting_Content_To_String_Should_Not_Create_TextBlock_Unless_UpdateChild_Called()
        {
            var target = new ContentPresenter();

            target.Content = "Foo";
            Assert.Null(target.Child);

            target.ApplyTemplate();
            Assert.Null(target.Child);

            target.UpdateChild();
            Assert.IsType<TextBlock>(target.Child);
            Assert.Equal("Foo", ((TextBlock)target.Child).Text);
        }

        [Fact]
        public void Clearing_Control_Content_Should_Remove_Child_Immediately()
        {
            var target = new ContentPresenter();
            var child = new Border();

            target.Content = child;
            target.UpdateChild();
            Assert.Equal(child, target.Child);

            target.Content = null;
            Assert.Null(target.Child);
        }

        [Fact]
        public void Clearing_String_Content_Should_Remove_Child_Immediately()
        {
            var target = new ContentPresenter();

            target.Content = "Foo";
            target.UpdateChild();
            Assert.IsType<TextBlock>(target.Child);

            target.Content = null;
            Assert.Null(target.Child);
        }

        [Fact]
        public void Adding_To_Logical_Tree_Should_Reevaluate_DataTemplates()
        {
            var root = new TestRoot();
            var target = new ContentPresenter();

            target.Content = "Foo";
            Assert.Null(target.Child);

            root.Child = target;
            target.ApplyTemplate();
            Assert.IsType<TextBlock>(target.Child);

            root.Child = null;
            root = new TestRoot
            {
                DataTemplates =
                {
                    new FuncDataTemplate<string>((x, _) => new Decorator()),
                },
            };

            root.Child = target;
            target.ApplyTemplate();
            Assert.IsType<Decorator>(target.Child);
        }
    }
}
