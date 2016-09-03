// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TabItemTests
    {
        [Fact]
        public void Logical_Child_Should_Be_Content_Control()
        {
            var target = new TabItem
            {
                Header = new Canvas(),
                Content = new Border(),
                Template = Template(),
            };

            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            var logicalChildren = ((ILogical)target).LogicalChildren;

            Assert.Equal(1, logicalChildren.Count);
            Assert.IsType<Border>(logicalChildren[0]);
        }

        [Fact]
        public void Logical_Child_Should_Be_DataTemplated_Content()
        {
            var target = new TabItem
            {
                Header = "header",
                Content = "content",
                Template = Template(),
            };

            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            var logicalChildren = ((ILogical)target).LogicalChildren;

            Assert.Equal(1, logicalChildren.Count);
            Assert.IsType<TextBlock>(logicalChildren[0]);
            Assert.Equal("content", ((TextBlock)logicalChildren[0]).Text);
        }

        private static IControlTemplate Template()
        {
            return new FuncControlTemplate<TabItem>(parent =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!TabItem.HeaderProperty],
                });
        }
    }
}
