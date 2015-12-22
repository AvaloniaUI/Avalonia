// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Xunit;

namespace Perspex.Controls.UnitTests.Presenters
{
    public class ContentPresenterTests
    {
        [Fact]
        public void Setting_Content_To_Control_Should_Set_Child()
        {
            var target = new ContentPresenter();
            var child = new Border();

            target.Content = child;

            // Child should not update until ApplyTemplate called.
            Assert.Null(target.Child);

            target.ApplyTemplate();

            Assert.Equal(child, target.Child);
        }

        [Fact]
        public void Setting_Content_To_String_Should_Create_TextBlock()
        {
            var target = new ContentPresenter();

            target.Content = "Foo";

            // Child should not update until ApplyTemplate called.
            Assert.Null(target.Child);

            target.ApplyTemplate();

            Assert.IsType<TextBlock>(target.Child);
            Assert.Equal("Foo", ((TextBlock)target.Child).Text);
        }

        [Fact]
        public void Adding_To_Logical_Tree_Should_Reevaluate_DataTemplates()
        {
            var target = new ContentPresenter
            {
                Content = "Foo",
            };

            target.ApplyTemplate();
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
    }
}
